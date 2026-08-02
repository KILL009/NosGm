$sln = "NosGm.sln"
$content = Get-Content $sln -Raw

$guids = @(
    "EC300279-181E-4E6E-A94C-E8BAF0EAB2AD",
    "AB3A47F3-3B2A-484A-B768-7C6CF5C83386",
    "9AA91BF5-88E7-4130-9F42-73AE206E2916",
    "A09A8666-2598-4F86-8F5E-9A2708354F37",
    "9DC21067-A102-419A-993C-4AA10C0B4708"
)

foreach ($guid in $guids) {
    # Debug|Any CPU
    $content = $content -replace "\{$guid\}\.Debug\|Any CPU\.ActiveCfg = Debug\|Any CPU", "{$guid}.Debug|Any CPU.ActiveCfg = Debug|x64"
    $content = $content -replace "\{$guid\}\.Debug\|Any CPU\.Build\.0 = Debug\|Any CPU", "{$guid}.Debug|Any CPU.Build.0 = Debug|x64"
    
    # Release|Any CPU
    $content = $content -replace "\{$guid\}\.Release\|Any CPU\.ActiveCfg = Release\|Any CPU", "{$guid}.Release|Any CPU.ActiveCfg = Release|x64"
    $content = $content -replace "\{$guid\}\.Release\|Any CPU\.Build\.0 = Release\|Any CPU", "{$guid}.Release|Any CPU.Build.0 = Release|x64"
}

Set-Content $sln -Value $content -Encoding UTF8
Write-Host "Updated NosGm.sln"
