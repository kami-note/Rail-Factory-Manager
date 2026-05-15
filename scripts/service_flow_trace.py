#!/usr/bin/env python3
import re
import sys
import json
from pathlib import Path
from collections import defaultdict, deque

if len(sys.argv) < 2:
    print('usage: python3 scripts/service_flow_trace.py <service-path> [entry-file]')
    sys.exit(1)

ROOT = Path(sys.argv[1])
ENTRY_FILE = Path(sys.argv[2]) if len(sys.argv) > 2 else (ROOT / 'Program.cs')

EXCLUDE_PARTS = {'/obj/', '/bin/'}
EXCLUDE_SUFFIXES = ('.Designer.cs',)

TYPE_RE = re.compile(r'\b(?:public|internal|private|protected)?\s*(?:sealed|static|partial|abstract)?\s*(class|record|interface)\s+(\w+)')
METHOD_RE = re.compile(r'\b(?:public|internal|private|protected)\s+(?:static\s+)?(?:async\s+)?(?:[\w<>,\[\]\?\.]+\s+)?(\w+)\s*\(')
MAP_RE = re.compile(r'\.Map(?:Get|Post|Put|Delete|Patch)\(([^,]+),\s*(\w+)\)')
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
    return sp.endswith('.cs') and not any(p in sp for p in EXCLUDE_PARTS) and not path.name.endswith(EXCLUDE_SUFFIXES)

def cs_files():
    return [p for p in ROOT.rglob('*.cs') if should_scan(p)]

def extract_methods(path: Path):
    text = path.read_text(encoding='utf-8')
    lines = text.splitlines()
    type_name = 'Global'
    methods = []

    for i, line in enumerate(lines):
        t = TYPE_RE.search(line)
        if t: type_name = t.group(2)
        m = METHOD_RE.search(line)
        if not m: continue

        name = m.group(1)
        method = Method(str(path), type_name, name, i+1)

        sig_lines = [line]
        j = i
        while ')' not in sig_lines[-1] and j+1 < len(lines):
            j += 1
            sig_lines.append(lines[j])
        signature = ' '.join(sig_lines)

        params_part = signature.split('(',1)[1].rsplit(')',1)[0]
        for raw in [p.strip() for p in params_part.split(',') if p.strip()]:
            raw = re.sub(r'\[[^\]]+\]', '', raw)
            toks = [t for t in raw.split() if t not in {'out','ref','in'}]
            if len(toks) >= 2:
                ptype = toks[-2].split('<')[0].split('?')[0]
                method.param_types.append(ptype)

        if '=>' in signature:
            method.body = signature.split('=>',1)[1]
        else:
            k = i
            found_open = False
            depth = 0
            body = []
            while k < len(lines):
                ln = lines[k]
                opens = ln.count('{'); closes = ln.count('}')
                if opens > 0: found_open = True
                if found_open: body.append(ln)
                depth += opens - closes
                if found_open and depth <= 0: break
                k += 1
            method.body = '\n'.join(body)

        for c in CALL_RE.finditer(method.body):
            l, r = c.group(1), c.group(2)
            method.calls.add(r if r else l)
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
                for iface in [p.strip().split('<')[0] for p in im.group(2).split(',')]:
                    impl_map[iface].add(cls)

    by_name = defaultdict(list)
    by_type = defaultdict(list)
    for m in methods:
        by_name[m.name].append(m)
        by_type[m.type_name].append(m)

    roots = set()
    entry_text = ENTRY_FILE.read_text(encoding='utf-8') if ENTRY_FILE.exists() else ''

    for h in re.findall(r'Handle\w+', entry_text):
        for m in by_name.get(h, []): roots.add(m.id)

    for mm in MAP_RE.finditer(entry_text):
        handler = mm.group(2)
        for m in by_name.get(handler, []): roots.add(m.id)

    # Program.cs minimal APIs can be lambdas: seed from called app service methods used in mappings
    for call in re.findall(r'\b(\w+)\.ExecuteAsync\(', entry_text):
        for m in by_name.get('ExecuteAsync', []):
            if m.type_name == call:
                roots.add(m.id)

    # add composition roots
    for n in ['AddTenancyModule','AddIamModule','AddIamHosting','UseIamHosting','MapIamEndpoints']:
        for m in by_name.get(n,[]): roots.add(m.id)

    reachable = set()
    q = deque([m for m in methods if m.id in roots])
    while q:
        cur = q.popleft()
        if cur.id in reachable: continue
        reachable.add(cur.id)

        for c in cur.calls:
            for nxt in by_name.get(c,[]):
                if nxt.id not in reachable: q.append(nxt)

        for p in cur.param_types:
            ctypes = {p} | impl_map.get(p,set())
            for ct in ctypes:
                for m in by_type.get(ct,[]):
                    if (m.name.startswith('Execute') or m.name.startswith('Get') or m.name.startswith('List') or m.name.startswith('Seed') or m.name==ct) and m.id not in reachable:
                        q.append(m)

    type_decls = {}
    for file,text in text_by_file.items():
        for i,line in enumerate(text.splitlines(),1):
            t=TYPE_RE.search(line)
            if t: type_decls[t.group(2)]={'file':file,'line':i}

    unused_methods=[m for m in methods if m.id not in reachable]
    used_types=set(m.type_name for m in methods if m.id in reachable)
    unused_types=sorted(set(type_decls)-used_types)

    constants=dict(CONST_RE.findall(entry_text))
    endpoints=[]
    for ln in entry_text.splitlines():
        mm=re.search(r'Map(Get|Post|Delete|Put|Patch)\(([^,]+),\s*(\w+)',ln)
        if mm:
            p=mm.group(2).strip().strip('"'); p=constants.get(p,p)
            endpoints.append({'verb':mm.group(1).upper(),'path':p,'handler':mm.group(3)})

    print(json.dumps({
        'service_root': str(ROOT),
        'entry_file': str(ENTRY_FILE),
        'roots': sorted(roots),
        'endpoints': endpoints,
        'reachable_method_count': len(reachable),
        'reachable_methods': sorted(reachable),
        'unused_method_count': len(unused_methods),
        'unused_methods':[{'id':m.id,'file':m.file,'line':m.start_line} for m in sorted(unused_methods,key=lambda x:(x.file,x.start_line))],
        'unused_type_count': len(unused_types),
        'unused_types':[{'type':t,'file':type_decls[t]['file'],'line':type_decls[t]['line']} for t in unused_types]
    }, indent=2, ensure_ascii=True))

if __name__=='__main__':
    main()
