$ErrorActionPreference = 'Stop'
$hits = Select-String -Path "src\Emma.Infrastructure\Migrations\**\*.cs" -Pattern 'hstore' -SimpleMatch -ErrorAction SilentlyContinue
if ($hits) {
  Write-Host "ERROR: 'hstore' found in migrations" -ForegroundColor Red
  $hits | Format-Table Path, LineNumber, Line -AutoSize | Out-String | Write-Host
  exit 1
} else {
  Write-Host "OK: no hstore in migrations" -ForegroundColor Green
}
