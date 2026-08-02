$slnPath = "NosGm.sln"
$slnText = Get-Content $slnPath -Raw

$guids = @(
    "AB3A47F3-3B2A-484A-B768-7C6CF5C83386",
    "9AA91BF5-88E7-4130-9F42-73AE206E2916",
    "9DC21067-A102-419A-993C-4AA10C0B4708",
    "EC300279-181E-4E6E-A94C-E8BAF0EAB2AD",
    "75EE87F2-7FA7-4CB5-9EBD-C727DED13F93"
)

foreach ($guid in $guids) {
    # Match the lines like:
    # {GUID}.Release|x64.ActiveCfg = Release|Any CPU
    # {GUID}.Release|x64.Build.0 = Release|Any CPU
    # {GUID}.Debug|x64.ActiveCfg = Debug|Any CPU
    # {GUID}.Debug|x64.Build.0 = Debug|Any CPU
    $slnText = $slnText -replace "(\{$guid\}\.(?:Debug|Release)\|x64\.(?:ActiveCfg|Build\.0) = (?:Debug|Release)\|)Any CPU", "`$1x64"
    
    # We also need to map the Any CPU solution platform to the x64 project platform if desired?
    # No, the user only complained about Release|x64 in solution mapping to Release|Any CPU in project.
}

Set-Content $slnPath -Value $slnText
Write-Host "Updated NosGm.sln"
