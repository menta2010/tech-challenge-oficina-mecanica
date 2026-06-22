#!/usr/bin/env bash
# Scan de vulnerabilidades do projeto. Requer .NET 8 SDK; Trivy opcional.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> Restaurando pacotes"
dotnet restore

echo "==> Pacotes vulneraveis (diretos e transitivos)"
dotnet list package --vulnerable --include-transitive || true

echo "==> Pacotes obsoletos (informativo)"
dotnet list package --outdated || true

if command -v trivy >/dev/null 2>&1; then
  echo "==> Trivy: scan do filesystem"
  trivy fs --scanners vuln,secret,misconfig . || true
  echo "==> (Opcional) build da imagem e scan"
  echo "    docker build -t oficina-api . && trivy image oficina-api"
else
  echo "Trivy nao instalado. Instale: https://aquasecurity.github.io/trivy/"
fi

echo "==> Concluido. Cole a saida no RELATORIO-VULNERABILIDADES.md"
