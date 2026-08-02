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
        
        # Remove any existing global PlatformTarget/Prefer32Bit (except in Condition blocks)
        # We will inject it exactly once at the top PropertyGroup
        
        # Simple string replace for first PropertyGroup
        $idx = $content.IndexOf("<PropertyGroup>")
        if ($idx -ge 0) {
            $insertIdx = $idx + "<PropertyGroup>".Length
            # Only insert if not already inserted exactly here
            $checkStr = "`r`n    <PlatformTarget>x64</PlatformTarget>`r`n    <Prefer32Bit>false</Prefer32Bit>"
            if ($content.Substring($insertIdx, $checkStr.Length) -ne $checkStr) {
                 # Also remove any instances of these globally just in case, but let's just insert
                 $content = $content.Insert($insertIdx, $checkStr)
                 Set-Content $proj -Value $content -Encoding UTF8
                 Write-Host "Injected properly into $proj"
            } else {
                 Write-Host "Already proper in $proj"
            }
        }
    }
}
