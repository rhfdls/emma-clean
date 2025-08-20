set -euo pipefail
dotnet list package --include-transitive | tee packages.txt
if grep -nE "Npgsql[[:space:]]+9\\." packages.txt || grep -nE "Npgsql\\.EntityFrameworkCore\\.PostgreSQL[[:space:]]+9\\." packages.txt; then
  echo "❌ PostgreSQL stack drift detected (expected EF8 + Provider8 + Driver8). Offending lines:" >&2
  grep -nE "Npgsql[[:space:]]+9\\.|Npgsql\\.EntityFrameworkCore\\.PostgreSQL[[:space:]]+9\\." packages.txt >&2
  exit 1
else
  echo "✅ OK: PostgreSQL stack locked to 8.x (driver + EF provider)."
fi
