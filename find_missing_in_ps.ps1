$psFiles = Get-ChildItem -Filter *.csproj -Recurse | Where-Object { (Get-Content $_.FullName -Raw).Contains('<TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>') } | Select-Object -ExpandProperty FullName
$gitFiles = git grep -l '<TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>' HEAD
foreach ($g in $gitFiles) {
    $full = Join-Path $PWD ($g -replace '/', '\')
    if ($psFiles -notcontains $full) {
        Write-Host "Missing in PS: $full"
    }
}
