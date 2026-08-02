$projects = @(
    "Data\NosGm.Program\NosGm.Login\NosGm.Login.csproj",
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj",
    "Data\NosGm.Program\NosGm.Parser\NosGm.Parser.csproj",
    "Data\NosGm.Program\NosGm.World\NosGm.World.csproj",
    "Data\NosGm.Program\NosGm.Logger\NosGm.LogServer.csproj"
)

foreach ($proj in $projects) {
    $content = Get-Content $proj -Raw
    
    # Match property groups with Condition that includes x64 and inject PlatformTarget if missing
    $content = [regex]::Replace($content, '(<PropertyGroup Condition="[^"]*x64[^"]*">)(?![\s\S]*?</PropertyGroup>)', {
        param($match)
        # We need to inject <PlatformTarget>x64</PlatformTarget> right after the PropertyGroup opening tag
        # ONLY if it doesn't already exist in this group.
        # This regex replace might be tricky. Let's just do it manually by finding each group.
    })
    
    # Simpler approach: split by <PropertyGroup, then for each piece, if it has Condition="...|x64" and doesn't have PlatformTarget, inject it.
    $parts = $content -split '<PropertyGroup'
    for ($i = 1; $i -lt $parts.Length; $i++) {
        $part = $parts[$i]
        $groupEnd = $part.IndexOf('</PropertyGroup>')
        if ($groupEnd -gt 0) {
            $groupContent = $part.Substring(0, $groupEnd)
            if ($groupContent -match 'Condition="[^"]*x64[^"]*"') {
                if ($groupContent -notmatch '<PlatformTarget>') {
                    $idx = $part.IndexOf('>')
                    $parts[$i] = $part.Substring(0, $idx + 1) + "`r`n    <PlatformTarget>x64</PlatformTarget>`r`n    <Prefer32Bit>false</Prefer32Bit>" + $part.Substring($idx + 1)
                } elseif ($groupContent -notmatch '<Prefer32Bit>') {
                    $idx = $part.IndexOf('>')
                    $parts[$i] = $part.Substring(0, $idx + 1) + "`r`n    <Prefer32Bit>false</Prefer32Bit>" + $part.Substring($idx + 1)
                }
            }
        }
    }
    
    $newContent = $parts -join '<PropertyGroup'
    Set-Content $proj -Value $newContent -Encoding UTF8
    Write-Host "Updated $proj"
}
