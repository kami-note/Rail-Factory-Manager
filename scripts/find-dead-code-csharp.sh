#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

SOLUTION="${1:-src/RailFactory.Fork.sln}"
if [[ ! -f "$SOLUTION" ]]; then
  auto_solution="$(rg --files -g '*.sln' | head -n 1 || true)"
  if [[ -n "$auto_solution" ]]; then
    SOLUTION="$auto_solution"
  else
    echo "Solution not found. Pass the path as first argument." >&2
    exit 1
  fi
fi

DIAGNOSTICS="CS0162,CS0219,IDE0044,IDE0051,IDE0052,IDE0058,IDE0059,IDE0060"
OUTPUT_FILE="/tmp/rail-factory-deadcode-csharp.log"

echo "Dead code analysis for C# (Roslyn analyzers)"
echo "Solution: $SOLUTION"
echo "Diagnostics: $DIAGNOSTICS"
echo

dotnet build "$SOLUTION" \
  -v:minimal \
  -p:EnforceCodeStyleInBuild=true \
  -p:AnalysisLevel=latest \
  -p:AnalysisMode=AllEnabledByDefault \
  -p:TreatWarningsAsErrors=false \
  -warnaserror:$DIAGNOSTICS \
  > "$OUTPUT_FILE" 2>&1 || true

if rg -n "\b($DIAGNOSTICS)\b" "$OUTPUT_FILE" >/tmp/rail-factory-deadcode-csharp-matches.log; then
  echo "Potential dead/unused C# code found:"
  cat /tmp/rail-factory-deadcode-csharp-matches.log
  echo
  echo "Full build log: $OUTPUT_FILE"
  exit 1
fi

echo "No dead/unused diagnostics were found for the configured rules."
echo "Full build log: $OUTPUT_FILE"
