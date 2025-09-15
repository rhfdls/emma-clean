$ErrorActionPreference = "Stop"

# Robust argument handling without param block (some runners misparse param at top)
$Target = "Emma.sln"
for ($i = 0; $i -lt $args.Length; $i++) {
  if ($args[$i] -eq "-Target" -and ($i + 1) -lt $args.Length) {
    $Target = $args[$i + 1]
    break
  }
}
if ([string]::IsNullOrWhiteSpace($Target) -and $env:TARGET) { $Target = $env:TARGET }

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
