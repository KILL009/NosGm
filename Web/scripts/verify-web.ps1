$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$tracked = @(git -C $root ls-files -- .)
if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed' }

$forbiddenExtensions = @('.exe', '.dll', '.nos', '.pak', '.pfx', '.p12', '.key', '.pem')
foreach ($path in $tracked) {
    if ($forbiddenExtensions -contains [IO.Path]::GetExtension($path).ToLowerInvariant()) {
        throw "Forbidden web artifact: $path"
    }
}

$sourceFiles = Get-ChildItem (Join-Path $root 'src') -Recurse -File | Where-Object { $_.FullName -notmatch '[\/](bin|obj)[\/]' }
$source = ($sourceFiles | Get-Content -Raw) -join "`n"
$forbidden = @(
    'api.noswings',
    'static.noswings',
    'dropbox.com',
    'hashedPassword',
    "createHash('md5'",
    'Gameforest',
    'ThemeForest',
    'UseForwardedHeaders',
    'ConnectionStrings',
    '-----BEGIN PRIVATE KEY-----',
    '-----BEGIN EC PRIVATE KEY-----'
)
foreach ($needle in $forbidden) {
    if ($source.Contains($needle, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Forbidden web source marker: $needle"
    }
}

$projectFiles = Get-ChildItem $root -Recurse -Filter *.csproj | Get-Content -Raw
if (($projectFiles -join "`n") -match '<PackageReference') {
    throw 'NosGM Web foundation must remain package-free.'
}

$required = @(
    "default-src 'none'",
    "frame-ancestors 'none'",
    'AddRateLimiter',
    'UseAntiforgery',
    'MaxRequestBodySize',
    'SafeDemoPortalDataSource',
    'ValidateCatalogs'
)
foreach ($needle in $required) {
    if (-not $source.Contains($needle, [StringComparison]::Ordinal)) {
        throw "Missing web safety marker: $needle"
    }
}

Write-Host 'NosGM Web safety and provenance checks passed.'
