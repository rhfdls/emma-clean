param(
  [string]$Target = "Emma.sln"
)

$ErrorActionPreference = "Stop"

dotnet list $Target package --include-transitive | Out-File -FilePath packages.txt -Encoding utf8

$driverMatches   = Select-String -Path packages.txt -Pattern '(^|\s)Npgsql\s+9\.' -AllMatches
$providerMatches = Select-String -Path packages.txt -Pattern 'Npgsql\.EntityFrameworkCore\.PostgreSQL\s+9\.' -AllMatches

if ($driverMatches -or $providerMatches) {
  Write-Host '❌ PostgreSQL stack drift detected (expected EF8 + Provider8 + Driver8). Offending lines:'
  if ($driverMatches)   { $driverMatches   | ForEach-Object { $_.Line } }
  if ($providerMatches) { $providerMatches | ForEach-Object { $_.Line } }
  throw 'Npgsql 9.x and/or Provider 9.x detected.'
} else {
  Write-Host '✅ OK: PostgreSQL stack locked to 8.x (driver + EF provider).'
}
