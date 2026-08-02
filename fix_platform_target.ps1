$projects = @(
    "Data\NosGm.Program\NosGm.Login\NosGm.Login.csproj",
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj",
    "Data\NosGm.Program\NosGm.Parser\NosGm.Parser.csproj",
    "Data\NosGm.Program\NosGm.World\NosGm.World.csproj",
    "Data\NosGm.Program\NosGm.Logger\NosGm.LogServer.csproj"
)

foreach ($proj in $projects) {
    $content = Get-Content $proj -Raw
    
    # Ensure default platform is x64 and uses $(Platform) syntax as user requested
    $content = $content -replace '<Platform Condition=" ''\$\(Platform\)'' == '''' ">x86</Platform>', '<Platform Condition=" ''$(Platform)'' == '''' ">x64</Platform>'
    $content = $content -replace '<Platform Condition=" ''\$\(Platform\)'' == '''' ">AnyCPU</Platform>', '<Platform Condition=" ''$(Platform)'' == '''' ">x64</Platform>'
    
    # Inject PlatformTarget if missing in the unconditional block
    if ($content -notmatch '<Platform Condition=" ''\$\(Platform\)'' == '''' ">x64</Platform>\s*<PlatformTarget>x64</PlatformTarget>') {
        $content = $content -replace '(<Platform Condition=" ''\$\(Platform\)'' == '''' ">x64</Platform>)', "`$1`r`n    <PlatformTarget>x64</PlatformTarget>"
    }
    
    Set-Content $proj -Value $content -Encoding UTF8
    Write-Host "Updated $proj"
}
