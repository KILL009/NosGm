$projects = @(
    "Data\NosGm.Program\NosGm.World\NosGm.World.csproj",
    "Data\NosGm.Program\NosGm.Login\NosGm.Login.csproj",
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj",
    "Data\NosGm.Program\NosGm.Logger\NosGm.LogServer.csproj",
    "Data\NosGm.Program\NosGm.Parser\NosGm.Parser.csproj"
)

foreach ($proj in $projects) {
    if (Test-Path $proj) {
        $content = Get-Content $proj -Raw
        
        # Check if already injected
        if ($content -notmatch "<PlatformTarget>x64</PlatformTarget>") {
            # Inject into the first <PropertyGroup> (right after it opens)
            $content = $content -replace "(<PropertyGroup>)", "`$1`n    <PlatformTarget>x64</PlatformTarget>`n    <Prefer32Bit>false</Prefer32Bit>"
            Set-Content $proj -Value $content -Encoding UTF8
            Write-Host "Injected x64 safely into $proj"
        } else {
            Write-Host "Already has PlatformTarget in $proj"
        }
    } else {
        Write-Host "Not found: $proj"
    }
}
