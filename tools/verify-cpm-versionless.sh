set -euo pipefail
if grep -R --include="*.csproj" -nE "<PackageReference[^>]*Version=" .; then
  echo "Found per-project <PackageReference Version=...>. All centrally pinned packages must be versionless." >&2
  exit 1
else
  echo "OK: All PackageReferences are versionless for centrally pinned packages."
fi
