#!/usr/bin/env bash
set -euo pipefail
if grep -R --line-number --ignore-case 'hstore' src/Emma.Infrastructure/Migrations; then
  echo "ERROR: 'hstore' found in migrations"
  exit 1
fi
echo "OK: no hstore in migrations"
