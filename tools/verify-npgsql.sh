set -euo pipefail

# Allow overriding the target to list (solution or project). Default to Emma.sln at repo root.
TARGET=${1:-Emma.sln}

set +e
dotnet list "$TARGET" package --include-transitive | tee packages.txt
LIST_STATUS=$?
set -e
if [ $LIST_STATUS -ne 0 ]; then
  echo "⚠️ dotnet list returned exit code $LIST_STATUS; continuing with partial results in packages.txt" >&2
fi
if grep -nE "Npgsql[[:space:]]+9\\." packages.txt || grep -nE "Npgsql\\.EntityFrameworkCore\\.PostgreSQL[[:space:]]+9\\." packages.txt; then
  echo "❌ PostgreSQL stack drift detected (expected EF8 + Provider8 + Driver8). Offending lines:" >&2
  grep -nE "Npgsql[[:space:]]+9\\.|Npgsql\\.EntityFrameworkCore\\.PostgreSQL[[:space:]]+9\\." packages.txt >&2
  exit 1
else
  echo "✅ OK: PostgreSQL stack locked to 8.x (driver + EF provider)."
fi
