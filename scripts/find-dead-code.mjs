#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const thisFilePath = fileURLToPath(import.meta.url);
const thisDir = path.dirname(thisFilePath);
const projectRoot = path.resolve(thisDir, '..');
const frontendRoot = path.join(projectRoot, 'src', 'RailFactory.Frontend', 'App');
const sourceRoot = path.join(frontendRoot, 'src');

if (!fs.existsSync(sourceRoot)) {
  console.error(`Source directory not found: ${sourceRoot}`);
  process.exit(1);
}

const validExtensions = ['.ts', '.tsx'];
const ignoredFilePatterns = [/\.d\.ts$/i];

function isIgnored(filePath) {
  return ignoredFilePatterns.some((pattern) => pattern.test(filePath));
}

function listSourceFiles(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...listSourceFiles(fullPath));
      continue;
    }

    const extension = path.extname(entry.name);
    if (!validExtensions.includes(extension)) {
      continue;
    }

    if (isIgnored(fullPath)) {
      continue;
    }

    files.push(fullPath);
  }

  return files;
}

function parseImportSpecifiers(fileContent) {
  const specifiers = new Set();
  const regexes = [
    /(?:import|export)\s+[^'"`]*?from\s*['"`]([^'"`]+)['"`]/g,
    /import\s*\(\s*['"`]([^'"`]+)['"`]\s*\)/g,
    /import\s*['"`]([^'"`]+)['"`]/g,
  ];

  for (const regex of regexes) {
    let match;
    while ((match = regex.exec(fileContent)) !== null) {
      const specifier = match[1]?.trim();
      if (specifier) {
        specifiers.add(specifier);
      }
    }
  }

  return [...specifiers];
}

function resolveRelativeModule(fromFile, specifier, allFiles) {
  if (!specifier.startsWith('.')) {
    return null;
  }

  const base = path.resolve(path.dirname(fromFile), specifier);
  const candidates = [
    base,
    `${base}.ts`,
    `${base}.tsx`,
    `${base}.js`,
    `${base}.jsx`,
    path.join(base, 'index.ts'),
    path.join(base, 'index.tsx'),
    path.join(base, 'index.js'),
    path.join(base, 'index.jsx'),
  ];

  for (const candidate of candidates) {
    if (allFiles.has(candidate)) {
      return candidate;
    }
  }

  return null;
}

function buildGraph(files) {
  const allFiles = new Set(files);
  const graph = new Map();

  for (const file of files) {
    const content = fs.readFileSync(file, 'utf8');
    const specifiers = parseImportSpecifiers(content);
    const resolvedImports = [];

    for (const specifier of specifiers) {
      const resolved = resolveRelativeModule(file, specifier, allFiles);
      if (resolved) {
        resolvedImports.push(resolved);
      }
    }

    graph.set(file, resolvedImports);
  }

  return graph;
}

function walkReachable(graph, roots) {
  const visited = new Set();
  const stack = [...roots];

  while (stack.length > 0) {
    const current = stack.pop();
    if (!current || visited.has(current) || !graph.has(current)) {
      continue;
    }

    visited.add(current);

    for (const dependency of graph.get(current)) {
      if (!visited.has(dependency)) {
        stack.push(dependency);
      }
    }
  }

  return visited;
}

function normalize(filePath) {
  return path.relative(projectRoot, filePath).split(path.sep).join('/');
}

const files = listSourceFiles(sourceRoot);
const graph = buildGraph(files);

const runtimeRoots = [
  path.join(sourceRoot, 'main.tsx'),
  path.join(sourceRoot, 'main.ts'),
].filter((candidate) => graph.has(candidate));

if (runtimeRoots.length === 0) {
  console.error('No runtime entry point found (expected src/main.tsx or src/main.ts).');
  process.exit(1);
}

const testRoots = files.filter((file) => /\.test\.(ts|tsx)$/i.test(file) || /\.spec\.(ts|tsx)$/i.test(file));
const runtimeReachable = walkReachable(graph, runtimeRoots);
const runtimeAndTestsReachable = walkReachable(graph, [...runtimeRoots, ...testRoots]);

const runtimeDead = files.filter((file) => !runtimeReachable.has(file));
const totalDead = files.filter((file) => !runtimeAndTestsReachable.has(file));

console.log('Dead code analysis (TS/TSX file reachability)');
console.log(`Frontend root: ${normalize(frontendRoot)}`);
console.log(`Analyzed files: ${files.length}`);
console.log(`Runtime roots: ${runtimeRoots.map(normalize).join(', ')}`);

console.log('\nPotentially dead for runtime (not reachable from runtime entry):');
if (runtimeDead.length === 0) {
  console.log('  none');
} else {
  for (const file of runtimeDead.sort()) {
    console.log(`  - ${normalize(file)}`);
  }
}

console.log('\nPotentially dead overall (not reachable from runtime nor tests):');
if (totalDead.length === 0) {
  console.log('  none');
} else {
  for (const file of totalDead.sort()) {
    console.log(`  - ${normalize(file)}`);
  }
}

console.log('\nNotes:');
console.log('  - This checker is static and conservative.');
console.log('  - Files loaded only by non-relative aliases, codegen, or runtime conventions may appear as false positives.');
