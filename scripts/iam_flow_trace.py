#!/usr/bin/env python3
import re
import json
from pathlib import Path
from collections import defaultdict, deque

ROOT = Path('src/RailFactory.Iam.Api')
EXCLUDE_PARTS = {'/obj/', '/bin/'}
EXCLUDE_SUFFIXES = ('.Designer.cs',)

TYPE_RE = re.compile(r'\b(?:public|internal|private|protected)?\s*(?:sealed|static|partial|abstract)?\s*(class|record|interface)\s+(\w+)')
METHOD_RE = re.compile(r'\b(?:public|internal|private|protected)\s+(?:static\s+)?(?:async\s+)?(?:[\w<>,\[\]\?\.]+\s+)?(\w+)\s*\(')
MAP_RE = re.compile(r'\.Map(?:Get|Post|Put|Delete|Patch)\([^,]+,\s*(\w+)\)')
CALL_RE = re.compile(r'\b(\w+)\s*(?:\.\s*(\w+))?\s*\(')
IMPLEMENTS_RE = re.compile(r'\bclass\s+(\w+)\s*(?:\([^\)]*\))?\s*:\s*([^\{]+)')
CONST_RE = re.compile(r'private const string\s+(\w+)\s*=\s*"([^"]+)";')


class Method:
    def __init__(self, file, type_name, name, start_line):
        self.file = file
        self.type_name = type_name
        self.name = name
        self.start_line = start_line
        self.body = ''
        self.calls = set()
        self.param_types = []

    @property
    def id(self):
        return f'{self.type_name}.{self.name}'


def should_scan(path: Path) -> bool:
    sp = str(path)
    if not sp.endswith('.cs'):
        return False
    if any(p in sp for p in EXCLUDE_PARTS):
        return False
    if path.name.endswith(EXCLUDE_SUFFIXES):
        return False
    return True


def cs_files():
    return [p for p in ROOT.rglob('*.cs') if should_scan(p)]


def extract_methods(path: Path):
    text = path.read_text(encoding='utf-8')
    lines = text.splitlines()

    type_name = 'Global'
    methods = []

    for i, line in enumerate(lines):
        t = TYPE_RE.search(line)
        if t:
            type_name = t.group(2)

        m = METHOD_RE.search(line)
        if not m:
            continue

        name = m.group(1)
        method = Method(str(path), type_name, name, i + 1)

        sig_lines = [line]
        j = i
        while ')' not in sig_lines[-1] and j + 1 < len(lines):
            j += 1
            sig_lines.append(lines[j])
        signature = ' '.join(sig_lines)
        params_part = signature.split('(', 1)[1].rsplit(')', 1)[0]
        for raw in [p.strip() for p in params_part.split(',') if p.strip()]:
            raw = raw.replace('[FromServices]', '').replace('[AsParameters]', '')
            toks = [t for t in raw.split() if t not in {'out', 'ref', 'in'}]
            if len(toks) >= 2:
                ptype = toks[-2]
                ptype = ptype.split('<')[0].split('?')[0]
                method.param_types.append(ptype)

        # expression-bodied method
        if '=>' in signature:
            expr_part = signature.split('=>', 1)[1]
            method.body = expr_part
        else:
            # block-bodied
            k = i
            found_open = False
            depth = 0
            body_lines = []
            while k < len(lines):
                ln = lines[k]
                opens = ln.count('{')
                closes = ln.count('}')
                if opens > 0:
                    found_open = True
                if found_open:
                    body_lines.append(ln)
                depth += opens
                depth -= closes
                if found_open and depth <= 0:
                    break
                k += 1
            method.body = '\n'.join(body_lines)

        for c in CALL_RE.finditer(method.body):
            left, right = c.group(1), c.group(2)
            method.calls.add(right if right else left)

        methods.append(method)

    return methods, text


def main():
    methods = []
    text_by_file = {}
    impl_map = defaultdict(set)

    for f in cs_files():
        ms, tx = extract_methods(f)
        methods.extend(ms)
        text_by_file[str(f)] = tx
        for line in tx.splitlines():
            im = IMPLEMENTS_RE.search(line)
            if im:
                cls = im.group(1)
                interfaces = [p.strip().split('<')[0] for p in im.group(2).split(',')]
                for iface in interfaces:
                    impl_map[iface].add(cls)

    by_name = defaultdict(list)
    by_type = defaultdict(list)
    for m in methods:
        by_name[m.name].append(m)
        by_type[m.type_name].append(m)

    roots = set()
    endpoint_file = str(ROOT / 'Api/IamEndpoints.cs')
    endpoint_text = text_by_file.get(endpoint_file, '')
    endpoint_handlers = set(MAP_RE.findall(endpoint_text))
    endpoint_handlers.update(re.findall(r'Handle\w+', endpoint_text))
    for h in endpoint_handlers:
        for m in by_name.get(h, []):
            roots.add(m.id)

    for fixed in ['IamModule.AddIamModule', 'IamHostingExtensions.AddIamHosting', 'IamHostingExtensions.UseIamHosting', 'IamEndpoints.MapIamEndpoints']:
        t, n = fixed.split('.')
        for m in by_name.get(n, []):
            if m.type_name == t:
                roots.add(m.id)

    reachable = set()
    q = deque([m for m in methods if m.id in roots])
    while q:
        cur = q.popleft()
        if cur.id in reachable:
            continue
        reachable.add(cur.id)

        for call_name in cur.calls:
            for nxt in by_name.get(call_name, []):
                if nxt.id not in reachable:
                    q.append(nxt)

        for ptype in cur.param_types:
            candidate_types = {ptype} | impl_map.get(ptype, set())
            for ctype in candidate_types:
                for execm in by_type.get(ctype, []):
                    if (execm.name.startswith('Execute') or execm.name.startswith('Start') or execm.name.startswith('Build') or execm.name == ctype) and execm.id not in reachable:
                        q.append(execm)

    type_decls = {}
    for file, text in text_by_file.items():
        for i, line in enumerate(text.splitlines(), start=1):
            mt = TYPE_RE.search(line)
            if mt:
                type_decls[mt.group(2)] = {'file': file, 'line': i}

    used_types = set(m.type_name for m in methods if m.id in reachable)
    all_types = set(type_decls.keys())

    unused_methods = [m for m in methods if m.id not in reachable]
    unused_types = sorted(all_types - used_types)

    constants = dict(CONST_RE.findall(endpoint_text))
    endpoints = []
    for ln in endpoint_text.splitlines():
        mm = re.search(r'Map(Get|Post|Delete|Put|Patch)\(([^,]+),\s*(\w+)', ln)
        if mm:
            p = mm.group(2).strip().strip('"')
            p = constants.get(p, p)
            endpoints.append({'verb': mm.group(1).upper(), 'path': p, 'handler': mm.group(3)})

    output = {
        'roots': sorted(roots),
        'endpoints': endpoints,
        'reachable_method_count': len(reachable),
        'reachable_methods': sorted(reachable),
        'unused_method_count': len(unused_methods),
        'unused_methods': [
            {'id': m.id, 'file': m.file, 'line': m.start_line}
            for m in sorted(unused_methods, key=lambda x: (x.file, x.start_line))
        ],
        'unused_type_count': len(unused_types),
        'unused_types': [
            {'type': t, 'file': type_decls[t]['file'], 'line': type_decls[t]['line']}
            for t in unused_types
        ]
    }

    print(json.dumps(output, indent=2, ensure_ascii=True))


if __name__ == '__main__':
    main()
