$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$launcher = Join-Path $root "Launcher"

$required = @(
    "LICENSE",
    "NOTICE.md",
    "README.md",
    "RELEASING.md",
    "NosGM.Launcher.sln",
    "scripts/publish-launcher.ps1",
    "scripts/verify-launcher-package.ps1",
    "src/NosGM.Updater.Core/NosGM.Updater.Core.csproj",
    "src/NosGM.ManifestBuilder/NosGM.ManifestBuilder.csproj",
    "src/NosGM.Launcher/NosGM.Launcher.csproj",
    "src/NosGM.Launcher/TrustedChannel.Placeholder.cs",
    "src/NosGM.Launcher/LauncherAuthenticationClient.cs",
    "src/NosGM.Launcher/GameforgeInstallationId.cs",
    "src/NosGM.Launcher/GameforgeJsonRpcPipeServer.cs",
    "src/NosGM.Launcher/SteamClientPatcher.cs",
    "src/NosGM.Launcher/ModernGameLauncher.cs",
    "src/NosGM.Launcher/LauncherLoginDialog.cs",
    "src/NosGM.SteamAuthStub/NosGM.SteamAuthStub.csproj",
    "src/NosGM.SteamAuthStub/SteamAuthStub.cs",
    "tests/NosGM.Updater.SelfTest/NosGM.Updater.SelfTest.csproj",
    "tests/NosGM.SteamClient.SelfTest/NosGM.SteamClient.SelfTest.csproj"
)

foreach ($path in $required) {
    if (-not (Test-Path (Join-Path $launcher $path))) {
        throw "Missing required launcher file: $path"
    }
}

$trackedRelativePaths = @(& git -C $root ls-files -- "Launcher")
if ($LASTEXITCODE -ne 0 -or $trackedRelativePaths.Count -eq 0) {
    throw "Could not enumerate tracked Launcher files."
}

$trackedFiles = @($trackedRelativePaths | ForEach-Object {
    $fullPath = Join-Path $root $_
    if (Test-Path $fullPath -PathType Leaf) {
        Get-Item $fullPath
    }
})

$notice = Get-Content (Join-Path $launcher "NOTICE.md") -Raw
foreach ($needle in @(
    "Mati18505/HexTaleLauncher",
    "50aa50580aa35a45b156a1899a340a25e50f7fb5",
    "no HexTaleLauncher source code",
    "ECDSA P-256 / SHA-256",
    "NosCoreIO/NosCore.DeveloperTools",
    "39e2cd2085ff7fc7250966d58893a95262157113",
    "MIT License"
)) {
    if (-not $notice.Contains($needle, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Launcher notice is missing: $needle"
    }
}

$sourceFiles = @($trackedFiles | Where-Object {
    $_.Extension -eq ".cs" -and $_.FullName -notlike "*NosGM.SteamAuthStub*"
})
$source = ($sourceFiles | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"

foreach ($forbidden in @(
    'MD5.Create',
    'HashAlgorithmName.MD5',
    'Verb = "runas"',
    'AllowAutoRedirect = true',
    'WriteProcessMemory',
    'VirtualProtect',
    'DllImport',
    'ffi-napi',
    'hextale.xyz'
)) {
    if ($source.Contains($forbidden, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Forbidden launcher primitive or upstream endpoint found: $forbidden"
    }
}

foreach ($requiredCode in @(
    "ECDSA_P256_SHA256",
    "SHA256",
    "ResolveManagedPath",
    "EnsureNoReparsePoints",
    "AllowAutoRedirect = false",
    "CheckCertificateRevocationList = true",
    "transactions",
    "rollback",
    "journal.json",
    "InstallLock",
    "RecoverLockedAsync",
    "import-pending:",
    "ValidateCatalogs",
    "TrustedChannelConfiguration",
    "PublicKeyBase64",
    "Español",
    "English",
    "Deutsch",
    "Français",
    "Italiano",
    "Polski",
    "Čeština",
    "Русский",
    "日本語",
    "中文",
    "IgnoredDeletes",
    "UseShellExecute = true",
    "UseShellExecute = false",
    "GameforgeClientJSONRPC",
    "PipeOptions.CurrentUserOnly",
    "_TNT_CLIENT_APPLICATION_ID",
    "_TNT_SESSION_ID",
    "_NC_AUTH_CODE",
    "_NC_INSTALLATION_ID",
    "AuthenticationEndpoint",
    "AuthenticationTransport",
    "LoginServerAddress",
    "steam-stub",
    "gameforge-pipe",
    "NostaleClientX_NosGM.exe",
    "noscore_gf.dll",
    "HttpCompletionOption.ResponseHeadersRead",
    "Software\Gameforge4d\TNTClient\MainApp",
    "GameforgeInstallationId.Resolve()",
    "EnsureSteamClientIdentity"
)) {
    if (-not $source.Contains($requiredCode, [System.StringComparison]::Ordinal)) {
        throw "Required launcher safety, release, language, or authentication control missing: $requiredCode"
    }
}

$launcherProjectPath = Join-Path $launcher "src/NosGM.Launcher/NosGM.Launcher.csproj"
$launcherProject = Get-Content $launcherProjectPath -Raw
foreach ($requiredProjectCode in @(
    '<SteamAuthStubDotNetHost Condition="''$(DotNetHostPath)'' != ''''">$(DotNetHostPath)</SteamAuthStubDotNetHost>',
    '<SteamAuthStubDotNetHost Condition="''$(SteamAuthStubDotNetHost)'' == ''''">dotnet</SteamAuthStubDotNetHost>',
    '&quot;$(SteamAuthStubDotNetHost)&quot; publish'
)) {
    if (-not $launcherProject.Contains($requiredProjectCode, [System.StringComparison]::Ordinal)) {
        throw "Launcher project must invoke the Steam NativeAOT publish through the resolved dotnet host: $requiredProjectCode"
    }
}
foreach ($forbiddenProjectCode in @(
    '%PATH%',
    'set &quot;PATH=',
    'set "PATH='
)) {
    if ($launcherProject.Contains($forbiddenProjectCode, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Launcher project must not expand PATH inside the Steam NativeAOT publish command: $forbiddenProjectCode"
    }
}

$stubSourcePath = Join-Path $launcher "src/NosGM.SteamAuthStub/SteamAuthStub.cs"
$stubSource = Get-Content $stubSourcePath -Raw
foreach ($requiredStubCode in @(
    'EntryPoint = "Steam_Init"',
    'EntryPoint = "Steam_GetAuthSessionTicket"',
    'EntryPoint = "Steam_GetSteamLanguage"',
    'Environment.GetEnvironmentVariable("_NC_AUTH_CODE")',
    'Environment.GetEnvironmentVariable("_NC_INSTALLATION_ID")',
    'DllImport("advapi32.dll"',
    'RegCreateKeyExW',
    'RegSetValueExW',
    'RegCloseKey'
)) {
    if (-not $stubSource.Contains($requiredStubCode, [System.StringComparison]::Ordinal)) {
        throw "Steam authentication stub contract missing: $requiredStubCode"
    }
}
foreach ($forbiddenStubCode in @(
    'CreateRemoteThread',
    'OpenProcess',
    'VirtualAllocEx',
    'WriteProcessMemory',
    'LoadLibrary',
    'WinHttp',
    'InternetOpen',
    'WebClient',
    'HttpClient'
)) {
    if ($stubSource.Contains($forbiddenStubCode, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Steam authentication stub contains forbidden injection or network primitive: $forbiddenStubCode"
    }
}

$settingsSource = Get-Content (Join-Path $launcher "src/NosGM.Launcher/LauncherSettings.cs") -Raw
if ($settingsSource.Contains("public string Password", [System.StringComparison]::Ordinal)) {
    throw "Launcher settings must never persist a password."
}
if ($settingsSource.Contains("public string InstallationId", [System.StringComparison]::Ordinal)) {
    throw "Gameforge InstallationId must remain in the registry instead of launcher settings."
}
if (-not $settingsSource.Contains("uri.IsLoopback", [System.StringComparison]::Ordinal) -or
    -not $settingsSource.Contains("Uri.UriSchemeHttps", [System.StringComparison]::Ordinal)) {
    throw "Launcher authentication endpoint validation must require HTTPS or loopback HTTP."
}

$privateKeyMarkers = @(
    "-----BEGIN EC PRIVATE KEY-----",
    "-----BEGIN PRIVATE KEY-----",
    "-----BEGIN ENCRYPTED PRIVATE KEY-----"
)
$textExtensions = @(".cs", ".csproj", ".sln", ".xaml", ".md", ".txt", ".json", ".yml", ".yaml", ".ps1", ".gitignore", "")
foreach ($file in $trackedFiles | Where-Object { $_.Length -le 4MB -and $_.Extension -in $textExtensions }) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    foreach ($marker in $privateKeyMarkers) {
        if ($content -and $content.Contains($marker, [System.StringComparison]::Ordinal)) {
            throw "Private signing key material found in tracked repository file: $($file.FullName)"
        }
    }
}

$proprietary = @($trackedFiles | Where-Object {
    $_.Extension -in @(".exe", ".dll", ".nos", ".pak", ".bin")
})
if ($proprietary.Count -gt 0) {
    throw "Binary or proprietary-looking client material is tracked under Launcher: $($proprietary.FullName -join ', ')"
}

$serverSolution = Get-Content (Join-Path $root "NosGm.sln") -Raw
if ($serverSolution.Contains("NosGM.Launcher", [System.StringComparison]::OrdinalIgnoreCase) -or
    $serverSolution.Contains("NosGM.Updater.Core", [System.StringComparison]::OrdinalIgnoreCase) -or
    $serverSolution.Contains("NosGM.SteamAuthStub", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Launcher projects must remain outside the NosGM server solution."
}

Write-Host "NosGM Launcher attribution, updater, Gameforge pipe, PATH-safe NativeAOT publish and Steam stub safety checks passed."
