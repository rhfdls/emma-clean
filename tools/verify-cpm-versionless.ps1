$ErrorActionPreference = "Stop"
$projects = Get-ChildItem -Recurse -Filter *.csproj | Where-Object { $_.FullName -notmatch '\\archive\\' }
$hits = @()
foreach ($p in $projects) {
  $m = Select-String -Path $p.FullName -Pattern '<PackageReference[^>]*Version=' -SimpleMatch
  if ($m) { $hits += $m }
}
if ($hits.Count -gt 0) {
  $hits | ForEach-Object { Write-Host $_.Path ":" $_.LineNumber ":" $_.Line }
  Write-Error "Found per-project <PackageReference Version=...>. All centrally pinned packages must be versionless."
} else {
  Write-Host "OK: All PackageReferences are versionless for centrally pinned packages."
}
