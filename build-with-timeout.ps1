$timeout = 300 # Timeout in seconds
$start = Get-Date
$process = Start-Process -FilePath "dotnet" -ArgumentList "build" -WorkingDirectory "c:\Users\david\GitHub\WindsurfProjects\emma" -PassThru -NoNewWindow
while ($process.HasExited -eq $false) {
    Start-Sleep -Seconds 5
    $elapsed = (Get-Date) - $start
    if ($elapsed.TotalSeconds -gt $timeout) {
        Write-Host "Build process is taking too long. Terminating..."
        Stop-Process -Id $process.Id -Force
        break
    }
}
