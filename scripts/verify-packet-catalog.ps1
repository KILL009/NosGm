$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

$root = 'Tools/NosGM.PacketCatalog'
$requiredFiles = @(
    "$root/NosGM.PacketCatalog.csproj",
    "$root/Program.cs",
    "$root/CatalogAnalyzer.cs",
    "$root/SyntaxHelpers.cs",
    "$root/ReportWriter.cs",
    "$root/SelfTest.cs",
    "$root/README.md",
    "$root/NOTICE.md",
    "$root/THIRD_PARTY_LICENSES.md"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        Fail "Required packet catalog file is missing: $file"
    }
}

$notice = Get-Content -LiteralPath "$root/NOTICE.md" -Raw
foreach ($text in @(
    'BlowaXD/SaltyEmu',
    '2588cfdc64789a7952c781faaafdf1026ac73e9d',
    '7f849171da82feee1b9fae851a45b3eef9a9cd68',
    'Blowa',
    'GNU General Public License version 3'
)) {
    if ($notice -notmatch [regex]::Escape($text)) {
        Fail "Packet catalog notice is missing required attribution: $text"
    }
}

$thirdParty = Get-Content -LiteralPath "$root/THIRD_PARTY_LICENSES.md" -Raw
foreach ($text in @(
    'Microsoft.CodeAnalysis.CSharp 5.6.0',
    'Copyright (c) .NET Foundation and Contributors',
    'The MIT License'
)) {
    if ($thirdParty -notmatch [regex]::Escape($text)) {
        Fail "Packet catalog third-party notice is missing: $text"
    }
}

$project = Get-Content -LiteralPath "$root/NosGM.PacketCatalog.csproj" -Raw
foreach ($text in @(
    'net9.0',
    'GPL-3.0-only',
    'TreatWarningsAsErrors',
    'Microsoft.CodeAnalysis.CSharp',
    '5.6.0'
)) {
    if ($project -notmatch [regex]::Escape($text)) {
        Fail "Packet catalog project is missing: $text"
    }
}

$allSource = Get-ChildItem -LiteralPath $root -Filter '*.cs' -File | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
$joined = $allSource -join "`n"
foreach ($forbidden in @(
    'Assembly.Load',
    'AssemblyLoadContext',
    'CSharpCompilation.Create',
    'Process.Start',
    'HttpClient',
    'Socket',
    'WebClient',
    'DllImport',
    'VirtualProtect',
    'WriteProcessMemory',
    'SqlConnection',
    'MongoClient',
    'RabbitMQ',
    'MQTT'
)) {
    if ($joined -match [regex]::Escape($forbidden)) {
        Fail "Forbidden runtime, network, database or process primitive found: $forbidden"
    }
}

foreach ($required in @(
    'CSharpSyntaxTree.ParseText',
    'PacketHeader',
    'PacketIndex',
    'Attributes(method.AttributeLists, "Packet")',
    'SourceReference',
    'SerializeToEnd',
    'HDL004'
)) {
    if ($joined -notmatch [regex]::Escape($required)) {
        Fail "Expected packet catalog safeguard or feature is missing: $required"
    }
}

if (Test-Path -LiteralPath 'NosGm.sln') {
    $solution = Get-Content -LiteralPath 'NosGm.sln' -Raw
    if ($solution -match 'NosGM.PacketCatalog') {
        Fail 'NosGM.PacketCatalog must remain outside NosGm.sln.'
    }
}

$trackedProprietary = @(& git ls-files '*.NOS' '*.nos' '*.pak' '*.bin' '*.dat' '*.lst' 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail 'git ls-files failed while checking proprietary resources.'
}
$toolTracked = $trackedProprietary | Where-Object { $_ -like "$root/*" }
if ($toolTracked.Count -gt 0) {
    Fail "Packet catalog must not contain client archives or extracted resources: $($toolTracked -join ', ')"
}

Write-Host 'NosGM.PacketCatalog attribution and safety checks passed.'
