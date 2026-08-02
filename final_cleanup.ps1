$projects = @(
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj",
    "Data\NosGm.Program\NosGm.World\NosGm.World.csproj",
    "Data\NosGm.Program\NosGm.Parser\NosGm.Parser.csproj",
    "Data\NosGm.Program\NosGm.Logger\NosGm.LogServer.csproj",
    "Data\NosGm.Program\NosGm.Login\NosGm.Login.csproj"
)

foreach ($proj in $projects) {
    if (Test-Path $proj) {
        $content = Get-Content $proj -Raw
        
        # 1. Clean up RuntimeIdentifiers
        $content = $content -replace '<RuntimeIdentifiers>win;win-x64;win-x86</RuntimeIdentifiers>', '<RuntimeIdentifiers>win;win-x64</RuntimeIdentifiers>'
        
        # 2. Add PlatformTarget and Prefer32Bit to Debug|x64 and Release|x64
        if ($proj -notmatch "NosGm.Login.csproj") {
            # Add to Debug|x64
            $content = $content -replace '(<PropertyGroup Condition=" ''\$\(Configuration\)\|\$\(Platform\)'' == ''Debug\|x64'' ">[\r\n\s]+)', "`$1    <PlatformTarget>x64</PlatformTarget>`r`n    <Prefer32Bit>false</Prefer32Bit>`r`n"
            # Add to Release|x64
            $content = $content -replace '(<PropertyGroup Condition=" ''\$\(Configuration\)\|\$\(Platform\)'' == ''Release\|x64'' ">[\r\n\s]+)', "`$1    <PlatformTarget>x64</PlatformTarget>`r`n    <Prefer32Bit>false</Prefer32Bit>`r`n"
        }
        
        # 3. Login specific cleanup
        if ($proj -match "NosGm.Login.csproj") {
            # Remove empty x86 property group
            $content = $content -replace '(?s)<PropertyGroup Condition=" ''\$\(Platform\)'' == ''x86'' ">\s*</PropertyGroup>\s*', ''
            # Ensure default platform is x64
            $content = $content -replace '<Platform Condition=" ''\$\(Platform\)'' == '''' ">x86</Platform>', '<Platform Condition=" ''$(Platform)'' == '''' ">x64</Platform>'
        }
        
        Set-Content -Path $proj -Value $content
        Write-Host "Processed $proj"
    }
}
