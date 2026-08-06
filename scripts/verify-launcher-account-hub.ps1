param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required account hub file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Launcher account hub contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Forbid([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Launcher account hub contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

$settings = Read-Source "Launcher/src/NosGM.Launcher/LauncherSettings.cs"
$history = Read-Source "Launcher/src/NosGM.Launcher/LauncherAccountHistory.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/LauncherAccountHubWindow.cs"
$integration = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.AccountHub.cs"
$login = Read-Source "Launcher/src/NosGM.Launcher/LauncherLoginDialog.cs"
$documentation = Read-Source "docs/launcher-account-hub.md"

Require $settings 'public string[] RecentAccountNames' `
    "Launcher settings store a dedicated account-name history"
Require $settings 'MaximumRecentAccounts' `
    "Settings validation shares the bounded history limit"
Require $settings 'AreRecentAccountsValid' `
    "Persisted account history is validated"
Require $settings 'StringComparer.OrdinalIgnoreCase' `
    "Persisted account names are deduplicated case-insensitively"
Require $settings '!value.Any(char.IsControl)' `
    "Account names reject control characters"

Require $history 'MaximumRecentAccounts = 5' `
    "Account history is limited to five entries"
Require $history 'Take(MaximumRecentAccounts)' `
    "All account history mutations remain bounded"
Require $history 'StringComparison.OrdinalIgnoreCase' `
    "Account selection and removal are case-insensitive"
Require $history 'AccountName = string.Empty' `
    "Using another account clears the prepared account"
Require $history 'RecentAccountNames = remaining' `
    "Forgetting an account removes it from local history"
Forbid $history 'Password' `
    "Account history never references passwords"
Forbid $history 'AuthorizationCode' `
    "Account history never references authorization codes"
Forbid $history 'LauncherAuthorizationTicket' `
    "Account history never stores authentication tickets"
Forbid $history 'ProtectedData' `
    "Account history does not introduce a hidden credential vault"
Forbid $history 'Environment.GetEnvironmentVariable' `
    "Account history never reads process secrets"

Require $window 'LauncherAccountHistory.Select' `
    "The account window selects only bounded local history"
Require $window 'LauncherAccountHistory.Forget' `
    "The account window can forget a selected account"
Require $window 'LauncherAccountHistory.UseAnotherAccount' `
    "The account window can clear the prepared account"
Require $window 'Passwords and access tickets are never saved.' `
    "The account window presents its privacy boundary"
Forbid $window 'LauncherAuthenticationClient' `
    "The account window cannot authenticate by itself"
Forbid $window 'ModernGameLauncher.LaunchAsync' `
    "The account window cannot launch the game"
Forbid $window 'PasswordBox' `
    "The account window never collects a password"

Require $integration 'ModernGameLauncher.GameLaunched +=' `
    "Only a successful game launch adds an account to history"
Require $integration 'LauncherAccountHistory.Remember' `
    "The canonical authenticated account is recorded"
Require $integration 'LauncherSettingsStore.SaveAsync' `
    "Account choices are persisted through the existing settings store"
Require $integration 'WindowChrome.SetIsHitTestVisibleInChrome' `
    "The title-bar account button remains interactive"
Require $integration '_accountHubLifetime.Cancel()' `
    "Launcher shutdown cancels account hub initialization"
Forbid $integration 'LauncherCredentials' `
    "Account hub integration never accesses entered credentials"
Forbid $integration 'AuthorizationCode' `
    "Account hub integration never accesses authorization codes"

Require $login 'The password will not be saved' `
    "Existing login dialog still states that passwords are not stored"
Require $documentation 'at most five recent account names' `
    "Documentation explains the bounded history"
Require $documentation 'never stores' `
    "Documentation states the credential exclusion boundary"
Require $documentation 'There is no reusable launcher login session' `
    "Documentation distinguishes prepared account names from sessions"

Write-Host "NosGM launcher account hub security and lifecycle contracts passed."
