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
        
        # Inject ONLY into the very first <PropertyGroup>
        # Check if already injected globally
        if ($content -notmatch "<Prefer32Bit>false</Prefer32Bit>") {
            $content = $content -replace "(?s)(<PropertyGroup>.*?)", "`$1`n    <PlatformTarget>x64</PlatformTarget>`n    <Prefer32Bit>false</Prefer32Bit>" -replace "(?s)(<PropertyGroup>)(.*?)(<PlatformTarget>x64</PlatformTarget>`n    <Prefer32Bit>false</Prefer32Bit>)", "`$1`n    <PlatformTarget>x64</PlatformTarget>`n    <Prefer32Bit>false</Prefer32Bit>`$2"
            
            # Since regex with -replace can be tricky for first match only, let's use string manipulation
            $idx = $content.IndexOf("<PropertyGroup>")
            if ($idx -ge 0) {
                $insertIdx = $idx + "<PropertyGroup>".Length
                $content = $content.Insert($insertIdx, "`r`n    <PlatformTarget>x64</PlatformTarget>`r`n    <Prefer32Bit>false</Prefer32Bit>")
                Set-Content $proj -Value $content -Encoding UTF8
                Write-Host "Injected Prefer32Bit safely into $proj"
            }
        }
    }
}
