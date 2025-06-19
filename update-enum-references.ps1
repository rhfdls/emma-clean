$modelsPath = "src\Emma.Models\Models"
$files = Get-ChildItem -Path $modelsPath -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    
    # Replace using directives for enums
    $newContent = $content -replace 'using Emma\.Data\.Enums', 'using Emma.Models.Enums'
    
    # Save the updated content
    if ($newContent -ne $content) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "Updated enum references in $($file.Name)"
    }
}

Write-Host "All model files have been updated with the new enum references."
