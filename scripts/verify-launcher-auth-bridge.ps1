param(
    [string]$BridgePath = "Data/NosGm.Program/NosGm.Master.Server/LauncherAuthBridge.cs",
    [string]$MasterProgramPath = "Data/NosGm.Program/NosGm.Master.Server/Program.cs",
    [string]$MasterProjectPath = "Data/NosGm.Program/NosGm.Master.Server/NosGm.Master.Server.csproj",
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs",
    [string]$SettingsPath = "Launcher/src/NosGM.Launcher/LauncherSettings.cs",
    [string]$InstallationIdentityPath = "Launcher/src/NosGM.Launcher/GameforgeInstallationId.cs",
    [string]$AuthenticationClientPath = "Launcher/src/NosGM.Launcher/LauncherAuthenticationClient.cs",
    [string]$PipePath = "Launcher/src/NosGM.Launcher/GameforgeJsonRpcPipeServer.cs",
    [string]$ModernLauncherPath = "Launcher/src/NosGM.Launcher/ModernGameLauncher.cs",
    [string]$MainWindowPath = "Launcher/src/NosGM.Launcher/MainWindow.xaml.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Required([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing launcher-authentication file: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Launcher authentication contract failed: $Description"
    }
}

function Forbid([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Launcher authentication contract failed: $Description"
    }
}

function Require-Ordered([string]$Content, [string[]]$Needles, [string]$Description) {
    $position = 0
    foreach ($needle in $Needles) {
        $next = $Content.IndexOf($needle, $position, [StringComparison]::Ordinal)
        if ($next -lt 0) {
            throw "Launcher authentication contract failed: $Description. Missing or out of order: $needle"
        }
        $position = $next + $needle.Length
    }
}

$bridge = Read-Required $BridgePath
$masterProgram = Read-Required $MasterProgramPath
$masterProject = Read-Required $MasterProjectPath
$configuration = Read-Required $ConfigurationPath
$settings = Read-Required $SettingsPath
$installationIdentity = Read-Required $InstallationIdentityPath
$authenticationClient = Read-Required $AuthenticationClientPath
$pipe = Read-Required $PipePath
$modernLauncher = Read-Required $ModernLauncherPath
$mainWindow = Read-Required $MainWindowPath

Require $configuration 'public static bool EnableLauncherAuthBridge = false;' 'The HTTP bridge must remain disabled by default.'
Require $configuration 'LauncherAuthBridgePrefix = "http://127.0.0.1:8081/"' 'The default bridge listener must be loopback-only.'
Require $configuration 'LauncherAuthBridgeMaxAttemptsPerWindow = 10' 'The authentication attempt limiter is missing.'

Require $masterProject '<Reference Include="System.Runtime.Serialization" />' 'Master must reference the bounded JSON serializer assembly.'
Require $masterProject '<Compile Include="LauncherAuthBridge.cs" />' 'Master does not compile the authentication bridge.'
Require-Ordered $masterProgram @(
    'server.Start();',
    'StartLauncherAuthBridge();'
) 'Master must start SCS before the optional HTTP bridge.'
Require $masterProgram 'EnableGameforgeTokenLogin must be true before the launcher authentication bridge can start.' 'The bridge must not run while modern Login is disabled.'

Require $bridge 'private const string TicketPath = "/api/v1/launcher/ticket";' 'The versioned ticket route changed unexpectedly.'
Require $bridge 'MaximumRequestBytes = 8192' 'The bridge request body is not bounded.'
Require $bridge '!string.Equals(context.Request.HttpMethod, "POST"' 'The bridge must reject non-POST requests.'
Require $bridge 'StartsWith("application/json"' 'The bridge must require JSON.'
Require $bridge 'TryConsumeAttempt(limiterKey)' 'The bridge must rate-limit credentials.'
Require-Ordered $bridge @(
    'DAOFactory.AccountDAO.LoadByName(request.AccountName)',
    'PasswordHashService.VerifyPassword(',
    'PasswordHashService.TryHashPassword(',
    'GameforgeAuthTicketStore.Instance.TryIssue('
) 'The bridge must verify and optionally upgrade the password before issuing the ticket.'
Require $bridge 'Guid.NewGuid().ToString("D")' 'Authorization codes must be generated cryptographically through Guid.NewGuid.'
Require $bridge 'Cache-Control"] = "no-store"' 'Authentication responses must not be cached.'
Require $bridge 'uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback' 'Plain HTTP must be restricted to loopback.'
Require $bridge 'TryWriteErrorAsync' 'Bridge error handling must tolerate disconnected clients.'
Forbid $bridge 'context.Response.Abort()' 'The bridge must not rely on unavailable response-abort APIs.'
Forbid $bridge 'Logger.Info(request.Password' 'The bridge must never log a password.'
Forbid $bridge 'Logger.Info(authorizationCode' 'The bridge must never log an authorization code.'

Require $settings 'AuthenticationEndpoint' 'Launcher settings must expose the authentication endpoint.'
Require $settings 'AccountName' 'Launcher settings may remember only the account name.'
Forbid $settings 'public string Password' 'Launcher settings must never persist a password.'
Forbid $settings 'public string InstallationId' 'The client installation identity belongs in the Gameforge registry, not launcher settings.'
Require $settings 'Uri.UriSchemeHttps' 'Remote authentication endpoints must use HTTPS.'
Require $settings 'Uri.UriSchemeHttp' 'Loopback development endpoints must remain supported.'
Require $settings 'uri.IsLoopback' 'Plain HTTP must be limited to loopback in launcher settings.'

Require $installationIdentity 'Software\Gameforge4d\TNTClient\MainApp' 'The launcher must use the same registry key as the game client.'
Require $installationIdentity 'private const string ValueName = "InstallationId";' 'The shared Gameforge InstallationId value is missing.'
Require $installationIdentity 'Registry.CurrentUser.CreateSubKey' 'The installation identity must be scoped to the current Windows user.'
Require $installationIdentity 'Guid.NewGuid().ToString("D")' 'A missing installation identity must be created once.'
Require $installationIdentity 'RegistryValueKind.String' 'The InstallationId must be stored in the expected registry format.'

Require $authenticationClient 'AllowAutoRedirect = false' 'Authentication requests must not follow redirects.'
Require $authenticationClient 'CheckCertificateRevocationList = true' 'TLS certificate revocation checking is missing.'
Require $authenticationClient 'MaximumResponseBytes = 16 * 1024' 'Authentication responses must be bounded.'
Require $authenticationClient 'HttpCompletionOption.ResponseHeadersRead' 'Authentication responses must stream with bounds.'
Require $authenticationClient 'GameforgeInstallationId.Resolve()' 'Ticket requests must use the same InstallationId that the client will send.'
Forbid $authenticationClient 'Console.WriteLine(password' 'The launcher must not log the password.'
Forbid $authenticationClient 'AuthorizationCode}' 'The launcher must not interpolate authorization codes into logs.'

foreach ($method in @(
    'ClientLibrary.isClientRunning',
    'ClientLibrary.initSession',
    'ClientLibrary.queryAuthorizationCode',
    'ClientLibrary.queryGameAccountName'
)) {
    Require $pipe $method "Missing Gameforge JSON-RPC method $method."
}
Require $pipe 'PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly' 'The JSON-RPC pipe must be restricted to the current Windows user.'
Require $pipe 'receivedSessionId != _sessionId' 'The pipe must bind initSession to the launcher session ID.'
Require $pipe '_authorizationCode = null;' 'The authorization code must be erased after delivery.'
Require $pipe '!(_authorizationCodeDelivered && _accountNameDelivered)' 'The pipe must deliver both credentials before completing.'
Require $pipe 'MaximumRequestBytes = 16 * 1024' 'JSON-RPC requests must be bounded.'

$expectedRegions = @(
    '["en"] = 0', '["de"] = 1', '["fr"] = 2', '["it"] = 3', '["pl"] = 4',
    '["es"] = 5', '["cz"] = 6', '["ru"] = 7', '["jp"] = 8', '["cn"] = 9'
)
foreach ($mapping in $expectedRegions) {
    Require $modernLauncher $mapping "Missing modern launcher region mapping: $mapping"
}
Require-Ordered $modernLauncher @(
    'RequestTicketAsync(',
    'var pipeTask = pipeServer.RunAsync(',
    'process.Start()',
    'await pipeTask;'
) 'The launcher must obtain a ticket, listen on the pipe, start the process and complete the handshake in that order.'
Require $modernLauncher 'UseShellExecute = false' 'Modern launch must control the child environment directly.'
Require $modernLauncher 'startInfo.ArgumentList.Add("gf")' 'The client must start in Gameforge mode.'
Require $modernLauncher 'startInfo.Environment["_TNT_CLIENT_APPLICATION_ID"]' 'The Gameforge application environment variable is missing.'
Require $modernLauncher 'startInfo.Environment["_TNT_SESSION_ID"]' 'The Gameforge session environment variable is missing.'
Require $modernLauncher 'process.Kill(entireProcessTree: true)' 'A failed handshake must terminate the orphaned client.'

Require $mainWindow 'LauncherLoginDialog.Prompt(' 'The Play button must prompt for credentials when modern Login is configured.'
Require $mainWindow 'ModernGameLauncher.LaunchAsync(' 'The Play button is not connected to the modern launcher.'
Require $mainWindow '_settings = _settings with { AccountName = credentials.AccountName };' 'Only the account name should be remembered after success.'
Forbid $mainWindow 'Password = credentials.Password' 'The password must never be saved to settings.'

Write-Host 'Launcher HTTPS ticket bridge, shared InstallationId, and Gameforge JSON-RPC handshake contracts verified.'
