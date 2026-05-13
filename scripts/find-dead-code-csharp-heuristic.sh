#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

SEARCH_ROOT="src"
if [[ ! -d "$SEARCH_ROOT" ]]; then
  echo "Directory not found: $SEARCH_ROOT" >&2
  exit 1
fi

echo "Heuristic dead code scan for C#"
echo "Root: $SEARCH_ROOT"
echo

TMP_TYPES="/tmp/rail-factory-csharp-types.tsv"
TMP_PRIVATE="/tmp/rail-factory-csharp-private.tsv"

rg -n --glob '*.cs' '^\s*(public|internal)\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+)*(class|record|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)\b' "$SEARCH_ROOT" \
  | sed -E 's/^([^:]+):([0-9]+):(.*)$/\1\t\2\t\3/' \
  > "$TMP_TYPES" || true

rg -n --glob '*.cs' '^\s*private\s+[^;(=]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\(|\{|=|;)' "$SEARCH_ROOT" \
  | sed -E 's/^([^:]+):([0-9]+):(.*)$/\1\t\2\t\3/' \
  > "$TMP_PRIVATE" || true

echo "Potentially unused public/internal types (name appears once in codebase):"
found_types=0
while IFS=$'\t' read -r file line text; do
  [[ -z "${file:-}" ]] && continue
  name="$(echo "$text" | sed -E 's/^\s*(public|internal)\s+(abstract\s+|sealed\s+|static\s+|partial\s+)*(class|record|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*).*/\4/')"
  [[ -z "$name" ]] && continue
  count="$(rg -w --glob '*.cs' --files-with-matches "$name" "$SEARCH_ROOT" | wc -l | tr -d ' ')"
  if [[ "$count" -le 1 ]]; then
    echo "  - $file:$line ($name)"
    found_types=1
  fi
done < "$TMP_TYPES"

if [[ "$found_types" -eq 0 ]]; then
  echo "  none"
fi

echo
echo "Potentially unused private members (name appears once in file):"
found_private=0
while IFS=$'\t' read -r file line text; do
  [[ -z "${file:-}" ]] && continue
  name="$(echo "$text" | sed -E 's/^\s*private\s+[^;(=]+\s+([A-Za-z_][A-Za-z0-9_]*).*/\1/')"
  [[ -z "$name" ]] && continue
  count="$(rg -n -w "$name" "$file" | wc -l | tr -d ' ')"
  if [[ "$count" -le 1 ]]; then
    echo "  - $file:$line ($name)"
    found_private=1
  fi
done < "$TMP_PRIVATE"

if [[ "$found_private" -eq 0 ]]; then
  echo "  none"
fi

echo
echo "Notes:"
echo "  - This is heuristic and may produce false positives/negatives."
echo "  - It complements (does not replace) Roslyn diagnostics."
