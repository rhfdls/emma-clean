$modelsPath = "src\Emma.Models\Models"
$files = Get-ChildItem -Path $modelsPath -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    
    # Replace namespace Emma.Data.Models with Emma.Models.Models
    $newContent = $content -replace 'namespace Emma\.Data\.Models', 'namespace Emma.Models.Models'
    
    # Update using directives
    $newContent = $newContent -replace 'using Emma\.Data\.Models', 'using Emma.Models.Models'
    
    # Save the updated content
    Set-Content -Path $file.FullName -Value $newContent -NoNewline
    
    Write-Host "Updated namespaces in $($file.Name)"
}

Write-Host "All model files have been updated with the new namespace."
