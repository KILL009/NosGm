$all = Get-ChildItem -Filter *.csproj -Recurse
foreach ($p in $all) {
    $c = Get-Content $p.FullName -Raw
    if ($c -notmatch '<TargetFrameworks[^>]*>net481;net10\.0</TargetFrameworks>' -and
        !$c.Contains('<TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>') -and
        $c -notmatch '<TargetFramework>net10\.0(?:-windows)?</TargetFramework>' -and
        $c -notmatch '<TargetFramework>([^<]+)</TargetFramework>' -and
        $c -notmatch '<TargetFrameworks>([^<]+)</TargetFrameworks>') {
        Write-Host "Missing: $($p.FullName)"
    }
}
