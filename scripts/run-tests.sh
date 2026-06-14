#!/bin/bash
# Enable command echoing/failures if desired, but handle errors gracefully to print the table
set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== INICIANDO EXECUÇÃO CONSOLIDADA DE TESTES ===${NC}"
echo ""

# Get script folder path
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$ROOT_DIR"

# 1. Backend Tests
echo -e "${GREEN}[1/3] Executando testes de Backend (C# / xUnit)...${NC}"
dotnet test src/RailFactory.Fork.sln --logger "console;verbosity=normal" > dotnet_test_results.log 2>&1 || true

PASSED_BE=$(grep -i -E '(aprovados|passed)\s*:\s*[0-9]+' dotnet_test_results.log | grep -o -E '[0-9]+' | awk '{sum+=$1} END {print sum}')
FAILED_BE=$(grep -i -E '(com falha|falhos|failed|falhas)\s*:\s*[0-9]+' dotnet_test_results.log | grep -o -E '[0-9]+' | awk '{sum+=$1} END {print sum}')
TOTAL_BE=$((PASSED_BE + FAILED_BE))

PASSED_BE=${PASSED_BE:-0}
FAILED_BE=${FAILED_BE:-0}
TOTAL_BE=${TOTAL_BE:-0}

# 2. Frontend Unit/Component Tests (Vitest)
echo -e "${GREEN}[2/3] Executando testes unitários do Frontend (TypeScript / Vitest)...${NC}"
cd src/RailFactory.Frontend/App
npm run test > vitest_results.log 2>&1 || true

# Extract Vitest totals
# Example format: "Tests  39 passed (39)"
PASSED_FE=$(grep -o -E 'Tests\s+[0-9]+\s+passed' vitest_results.log | grep -o -E '[0-9]+' | head -n 1)
FAILED_FE=$(grep -o -E 'Tests\s+[0-9]+\s+failed' vitest_results.log | grep -o -E '[0-9]+' | head -n 1)

PASSED_FE=${PASSED_FE:-0}
FAILED_FE=${FAILED_FE:-0}
TOTAL_FE=$((PASSED_FE + FAILED_FE))

# 3. E2E Tests (Playwright)
echo -e "${GREEN}[3/3] Executando testes End-to-End (Playwright)...${NC}"
# Playwright tests require the local host to be running.
if curl -s -o /dev/null -w "%{http_code}" http://localhost:5082/app/inventory --max-time 2 | grep -q "200\|302\|307"; then
  npm run test:e2e > playwright_results.log 2>&1 || true
  # Example: "  29 passed (50s)"
  PASSED_E2E=$(grep -o -E '[0-9]+\s+passed' playwright_results.log | grep -o -E '[0-9]+' | head -n 1)
  FAILED_E2E=$(grep -o -E '[0-9]+\s+failed' playwright_results.log | grep -o -E '[0-9]+' | head -n 1)
  
  PASSED_E2E=${PASSED_E2E:-0}
  FAILED_E2E=${FAILED_E2E:-0}
  TOTAL_E2E=$((PASSED_E2E + FAILED_E2E))
else
  echo -e "${YELLOW}Aviso: http://localhost:5082 não está respondendo. Pulando os testes E2E Playwright.${NC}"
  PASSED_E2E="PULADO"
  FAILED_E2E="PULADO"
  TOTAL_E2E="PULADO"
fi

# Clean up log files
rm -f vitest_results.log
rm -f playwright_results.log
cd "$ROOT_DIR"
rm -f dotnet_test_results.log

# Print Consolidated Results Table
echo ""
echo -e "${GREEN}========================================================================${NC}"
echo -e "${GREEN}                      TABELA CONSOLIDADA DE TESTES                      ${NC}"
echo -e "${GREEN}========================================================================${NC}"
printf "| %-20s | %-16s | %-10s | %-10s | %-10s |\n" "Suíte de Testes" "Ferramenta" "Total" "Aprovados" "Falhas"
printf "|----------------------|------------------|------------|------------|------------|\n"
printf "| %-20s | %-16s | %-10s | %-10s | %-10s |\n" "Backend API" "xUnit (.NET 10)" "$TOTAL_BE" "$PASSED_BE" "$FAILED_BE"
printf "| %-20s | %-16s | %-10s | %-10s | %-10s |\n" "Frontend Unit" "Vitest" "$TOTAL_FE" "$PASSED_FE" "$FAILED_FE"
printf "| %-20s | %-16s | %-10s | %-10s | %-10s |\n" "E2E Integração" "Playwright" "$TOTAL_E2E" "$PASSED_E2E" "$FAILED_E2E"
echo -e "${GREEN}========================================================================${NC}"
echo ""
