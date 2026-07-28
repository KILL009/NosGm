using NosGm.Configuration;
using NosGm.Core;
using NosGm.Core.Handling;
using NosGm.DAL;
using NosGm.DAL.EF;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.Packets.Packets.ClientPackets;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.BasicPacket.Login
{
    public class LoginPacketHandler : IPacketHandler
    {
        private readonly ClientSession _session;

        public LoginPacketHandler(ClientSession session)
        {
            _session = session;
        }

        private string BuildServersPacket(string username, byte regionType, int sessionId, bool ignoreUserName, long accountId)
        {
            string channelPacket = CommunicationServiceClient.Instance.RetrieveRegisteredWorldServers(
                username,
                regionType,
                sessionId,
                ignoreUserName,
                accountId);

            if (!string.IsNullOrWhiteSpace(channelPacket) && channelPacket.Contains(":"))
            {
                return channelPacket;
            }

            Logger.Debug("Client has been removed. Reason: World Server not found");
            _session.SendPacket($"failc {(byte)LoginFailType.CantConnect}");
            return null;
        }

        private static bool IsHexCredential(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (char character in value)
            {
                bool isHex = character >= '0' && character <= '9' || character >= 'a' && character <= 'f' || character >= 'A' && character <= 'F';
                if (!isHex) return false;
            }
            return true;
        }

        public async Task VerifyLoginAsync(LoginPacket loginPacket)
        {
            if (loginPacket == null || string.IsNullOrWhiteSpace(loginPacket.Name) || string.IsNullOrWhiteSpace(loginPacket.Password))
            {
                DisposeLoginPolling();
                return;
            }

            if (!TryResolveClientRegion(out byte resolvedRegionType, out string clientCulture)) return;

            if (loginPacket.RegionType != resolvedRegionType)
            {
                Logger.Debug($"Login RegionType overridden by trusted port | Port={_session.ListeningPort} PacketRegion={loginPacket.RegionType} ResolvedRegion={resolvedRegionType} Culture={clientCulture}");
            }

            string protocolUsername = loginPacket.Name;
            AccountDTO loadedAccount = LoadAccountByLoginName(protocolUsername, resolvedRegionType);
            if (loadedAccount == null)
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Unknown account");
                return;
            }

            bool accountNameMatches = string.Equals(loadedAccount.Name, protocolUsername, StringComparison.Ordinal) ||
                                      ClientRegionMap.IsProtocolUsernameForAccount(protocolUsername, loadedAccount.Name, resolvedRegionType);
            if (!accountNameMatches)
            {
                Reject(LoginFailType.WrongCaps, "Session removed. Reason: Wrong account casing");
                return;
            }

            if (!ValidateAccountStateBeforeCredentials(loadedAccount)) return;

            string packetCredential = loginPacket.Password ?? string.Empty;
            string storedCredential = loadedAccount.Password ?? string.Empty;
            Logger.Info($"Login credential shape | AccountId={loadedAccount.AccountId} UseOldCrypto={ServerConfiguration.UseOldCrypto} PacketLength={packetCredential.Length} PacketIsHex={IsHexCredential(packetCredential)} StoredLength={storedCredential.Length} StoredIsHex={IsHexCredential(storedCredential)} StoredIsVersioned={PasswordHashService.IsVersionedHash(storedCredential)}");

            if (!PasswordHashService.VerifyLoginPayload(
                    loadedAccount.Password,
                    loginPacket.Password,
                    ServerConfiguration.UseOldCrypto,
                    ServerConfiguration.LoginUsesPrehashedSha512,
                    out string clearPassword,
                    out bool passwordNeedsUpgrade))
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Wrong credentials");
                return;
            }

            if (passwordNeedsUpgrade) UpgradePasswordHash(loadedAccount, clearPassword);

            bool hasClientVersion = TryGetClientVersion(loginPacket, out Version clientVersion);
            if (!ValidateClientVersion(hasClientVersion, clientVersion)) return;

            bool ignoreUserName = ServerConfiguration.UseOldCrypto || hasClientVersion && clientVersion.Build >= 0 && clientVersion.Build < 3075;

            await CompleteLoginAsync(
                    loadedAccount,
                    protocolUsername,
                    resolvedRegionType,
                    clientCulture,
                    ignoreUserName,
                    "password",
                    false)
                .ConfigureAwait(false);
        }

        [Packet("NoS0576", "NoS0577")]
        public void VerifyGameforgeLogin(string rawPacket)
        {
            VerifyGameforgeLoginAsync(rawPacket).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task VerifyGameforgeLoginAsync(string rawPacket)
        {
            if (!ServerConfiguration.EnableGameforgeTokenLogin)
            {
                Reject(LoginFailType.CantConnect, "Session removed. Reason: Gameforge token login disabled");
                return;
            }

            if (!GameforgeLoginPacketParser.TryParse(rawPacket, out GameforgeLoginPayload payload, out string parseError))
            {
                Logger.Warn($"Malformed Gameforge login | {DescribePacket(rawPacket)} Reason={parseError}");
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Invalid Gameforge login payload");
                return;
            }

            if (!TryResolveClientRegion(out byte resolvedRegionType, out string clientCulture)) return;

            // The accepted local Login port and the one-use Master ticket are the
            // trusted region boundary. Steam-derived clients may preserve a legacy
            // CountryId inside NoS0577 even when they were launched for another
            // regional port. Keep that value as telemetry, but never let it select
            // the ticket region or account culture.
            if (payload.CountryId != resolvedRegionType)
            {
                Logger.Warn(
                    $"Gameforge CountryId overridden by trusted Login port | " +
                    $"Port={_session.ListeningPort} PacketRegion={payload.CountryId} " +
                    $"ResolvedRegion={resolvedRegionType} Culture={clientCulture}");
            }

            if (!GameforgeLoginPacketParser.TryGetCulture(resolvedRegionType, out string resolvedCulture) ||
                !string.Equals(resolvedCulture, clientCulture, StringComparison.Ordinal))
            {
                Reject(LoginFailType.CantConnect, "Session removed. Reason: Unsupported trusted Login region");
                return;
            }

            if (!ValidateClientVersion(true, payload.ClientVersion)) return;

            if (!string.IsNullOrWhiteSpace(ServerConfiguration.GameforgeClientMd5) &&
                !string.Equals(ServerConfiguration.GameforgeClientMd5, payload.ClientMd5, StringComparison.OrdinalIgnoreCase))
            {
                Reject(LoginFailType.OldClient, "Session removed. Reason: Unsupported client checksum");
                return;
            }

            string accountName;
            try
            {
                accountName = AuthentificationServiceClient.Instance.ConsumeGameforgeAuthTicket(
                    payload.AuthToken,
                    payload.InstallationId.ToString("D"),
                    resolvedRegionType);
            }
            catch (Exception ex)
            {
                Logger.Error("Gameforge ticket resolution failed", ex);
                Reject(LoginFailType.CantConnect, "Session removed. Reason: Authentication service failed");
                return;
            }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Invalid or expired Gameforge ticket");
                return;
            }

            AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(accountName);
            if (loadedAccount == null)
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Ticket account not found");
                return;
            }

            if (!string.Equals(loadedAccount.Name, accountName, StringComparison.Ordinal))
            {
                Reject(LoginFailType.WrongCaps, "Session removed. Reason: Wrong ticket account casing");
                return;
            }

            if (!ValidateAccountStateBeforeCredentials(loadedAccount)) return;

            await CompleteLoginAsync(
                    loadedAccount,
                    loadedAccount.Name,
                    resolvedRegionType,
                    clientCulture,
                    false,
                    payload.Header,
                    true)
                .ConfigureAwait(false);
        }

        private async Task CompleteLoginAsync(
            AccountDTO loadedAccount,
            string protocolUsername,
            byte regionType,
            string culture,
            bool ignoreUserName,
            string authenticationMode,
            bool requiresGameforgeWorldPermit)
        {
            string ipAddress = NormalizeRemoteIp(_session.IpAddress);
            if (DAOFactory.PenaltyLogDAO.LoadByIp(ipAddress).Any())
            {
                Reject(LoginFailType.CantConnect, "Session removed. Reason: IP penalty");
                return;
            }

            if (await CheckIsConnectedAsync(loadedAccount.AccountId).ConfigureAwait(false))
            {
                Reject(LoginFailType.AlreadyConnected, "Session removed. Reason: Already connected");
                return;
            }

            if (CommunicationServiceClient.Instance.IsAccountConnected(loadedAccount.AccountId))
            {
                _session.SendPacket($"failc {(byte)LoginFailType.AlreadyConnected}");
                if (!_session.HasSelectedCharacter)
                {
                    _session.Disconnect();
                    CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);
                    Logger.Info("Session removed. Reason: Already connected");
                    DisposeLoginPolling();
                }
                return;
            }

            PenaltyLogDTO penalty = DAOFactory.PenaltyLogDAO.LoadByAccount(loadedAccount.AccountId)
                .FirstOrDefault(s => s.DateEnd > DateTime.Now && s.Penalty == PenaltyType.Banned);
            if (penalty != null || loadedAccount.Authority == AuthorityType.Banned)
            {
                Reject(LoginFailType.Banned, "Session removed. Reason: Banned");
                return;
            }

            if (!SynchronizeAccountLanguage(loadedAccount, culture))
            {
                Reject(LoginFailType.CantConnect, "Session removed. Reason: Unable to synchronize client language");
                return;
            }

            int newSessionId = SessionFactory.Instance.GenerateSessionId();
            Logger.Info($"{loadedAccount.Name} connected | SessionID={newSessionId} Auth={authenticationMode} RegionType={regionType} Culture={culture}");

            bool accountRegistered = false;
            bool worldPermitRegistered = false;
            try
            {
                CommunicationServiceClient.Instance.RegisterAccountLogin(loadedAccount.AccountId, newSessionId, ipAddress);
                accountRegistered = true;

                if (requiresGameforgeWorldPermit)
                {
                    worldPermitRegistered = AuthentificationServiceClient.Instance.RegisterGameforgeWorldPermit(loadedAccount.AccountId, newSessionId, ipAddress);
                    if (!worldPermitRegistered) throw new InvalidOperationException("Master rejected the Gameforge World permit.");
                }

                string serversPacket = BuildServersPacket(
                    protocolUsername,
                    regionType,
                    newSessionId,
                    ignoreUserName,
                    loadedAccount.AccountId);

                if (string.IsNullOrWhiteSpace(serversPacket))
                {
                    if (worldPermitRegistered)
                    {
                        AuthentificationServiceClient.Instance.RevokeGameforgeWorldPermit(loadedAccount.AccountId, newSessionId);
                    }
                    CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);
                    DisposeLoginPolling();
                    return;
                }

                _session.SendPacket(serversPacket);
                Logger.Info($"Server list sent | Account={loadedAccount.Name} RegionType={regionType} Culture={culture} Auth={authenticationMode}");
                DisposeLoginPolling();
            }
            catch (Exception ex)
            {
                if (worldPermitRegistered)
                {
                    AuthentificationServiceClient.Instance.RevokeGameforgeWorldPermit(loadedAccount.AccountId, newSessionId);
                }
                if (accountRegistered)
                {
                    CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);
                }
                Logger.Error("Login registration failed", ex);
                Reject(LoginFailType.CantConnect, "Session removed. Reason: Login registration failed");
            }
        }

        private bool TryResolveClientRegion(out byte regionType, out string culture)
        {
            if (ClientRegionMap.TryResolveLoginPort(_session.ListeningPort, out regionType, out culture)) return true;
            Reject(LoginFailType.CantConnect, $"Session removed. Reason: Unsupported Login port {_session.ListeningPort}");
            return false;
        }

        private bool ValidateAccountStateBeforeCredentials(AccountDTO loadedAccount)
        {
            if (ServerConfiguration.MaintenanceMode && loadedAccount.Authority < AuthorityType.GM)
            {
                Reject(LoginFailType.Maintenance, "Session removed. Reason: Maintenance mode");
                return false;
            }
            return true;
        }

        private bool ValidateClientVersion(bool hasClientVersion, Version clientVersion)
        {
            if (!ServerConfiguration.GameVersionRequired) return true;
            if (!TryParseVersion(ServerConfiguration.GameVersion, out Version requiredVersion))
            {
                Logger.Error($"Invalid configured game version: '{ServerConfiguration.GameVersion}'");
                Reject(LoginFailType.CantConnect, "Session removed. Reason: Invalid server version configuration");
                return false;
            }
            if (!hasClientVersion || !requiredVersion.Equals(clientVersion))
            {
                Logger.Warn($"Unsupported client version | Received={(hasClientVersion ? clientVersion.ToString() : "unparseable")} Required={requiredVersion}");
                Reject(LoginFailType.OldClient, "Session removed. Reason: Unsupported client version");
                return false;
            }
            return true;
        }

        private async Task<bool> CheckIsConnectedAsync(long accountId)
        {
            const int retryCount = 20;
            const int retryDelayMilliseconds = 200;
            for (int i = 0; i < retryCount; i++)
            {
                if (!CommunicationServiceClient.Instance.IsAccountConnected(accountId)) return false;
                await Task.Delay(retryDelayMilliseconds).ConfigureAwait(false);
            }
            return true;
        }

        private void DisposeLoginPolling()
        {
            _session.PacketHandlerInterval?.Dispose();
        }

        private static string DescribePacket(string rawPacket)
        {
            if (rawPacket == null) return "Header=<null> Length=0 ContainsVerticalTab=False";
            int separator = rawPacket.IndexOf(' ');
            string header = separator < 0 ? rawPacket : rawPacket.Substring(0, separator);
            if (header.Length > 16) header = header.Substring(0, 16);
            for (int i = 0; i < header.Length; i++)
            {
                char character = header[i];
                bool safe = char.IsLetterOrDigit(character) || character == '_' || character == '-' || character == '$';
                if (!safe)
                {
                    header = "<invalid>";
                    break;
                }
            }
            if (string.IsNullOrEmpty(header)) header = "<empty>";
            return $"Header={header} Length={rawPacket.Length} ContainsVerticalTab={rawPacket.IndexOf('\v') >= 0}";
        }

        private static AccountDTO LoadAccountByLoginName(string username, byte resolvedRegionType)
        {
            AccountDTO account = DAOFactory.AccountDAO.LoadByName(username);
            if (account != null) return account;
            if (!ClientRegionMap.TryStripProtocolPrefix(username, out string accountName, out ClientLanguageProfile profile) || profile.RegionType != resolvedRegionType) return null;
            return DAOFactory.AccountDAO.LoadByName(accountName);
        }

        private static string NormalizeRemoteIp(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return string.Empty;
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) && !string.IsNullOrWhiteSpace(uri.Host)) return uri.Host;
            string value = endpoint.Trim();
            if (value.StartsWith("[", StringComparison.Ordinal))
            {
                int closingBracket = value.IndexOf(']');
                if (closingBracket > 1) return value.Substring(1, closingBracket - 1);
            }
            int lastColon = value.LastIndexOf(':');
            if (lastColon > 0 && value.IndexOf(':') == lastColon) return value.Substring(0, lastColon);
            return value;
        }

        private static bool SynchronizeAccountLanguage(AccountDTO account, string culture)
        {
            if (account == null || string.IsNullOrWhiteSpace(culture)) return false;
            if (string.Equals(account.Language, culture, StringComparison.Ordinal)) return true;
            if (!DAOFactory.AccountDAO.TryUpdateLanguage(account.AccountId, culture))
            {
                Logger.Error($"Account language synchronization failed | AccountId={account.AccountId} Culture={culture}");
                return false;
            }
            account.Language = culture;
            Logger.Info($"Account language synchronized | AccountId={account.AccountId} Culture={culture}");
            return true;
        }

        private static void UpgradePasswordHash(AccountDTO account, string clearPassword)
        {
            if (account == null || !PasswordHashService.TryHashPassword(clearPassword, out string upgradedPassword)) return;
            string expectedPassword = account.Password;
            if (DAOFactory.AccountDAO.TryUpgradePassword(account.AccountId, expectedPassword, upgradedPassword))
            {
                account.Password = upgradedPassword;
                Logger.Info($"Password hash upgraded | AccountId={account.AccountId}");
                return;
            }
            Logger.Debug($"Password hash upgrade skipped | AccountId={account.AccountId}");
        }

        private void Reject(LoginFailType failType, string logMessage)
        {
            _session.SendPacket($"failc {(byte)failType}");
            Logger.Info(logMessage);
            DisposeLoginPolling();
        }

        private static bool TryGetClientVersion(LoginPacket loginPacket, out Version version)
        {
            return TryParseVersion(loginPacket?.ClientData, out version) || TryParseVersion(loginPacket?.ClientDataOld, out version);
        }

        private static bool TryParseVersion(string rawValue, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(rawValue)) return false;
            string[] tokens = rawValue.Split(new[] { ' ', '\t', '\v', '\r', '\n', '\\', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string candidate = token.Trim();
                if (Version.TryParse(candidate, out version)) return true;
                int start = -1;
                for (int i = 0; i < candidate.Length; i++)
                {
                    if (char.IsDigit(candidate[i]))
                    {
                        start = i;
                        break;
                    }
                }
                if (start < 0) continue;
                int end = start;
                while (end < candidate.Length && (char.IsDigit(candidate[end]) || candidate[end] == '.')) end++;
                if (end > start && Version.TryParse(candidate.Substring(start, end - start), out version)) return true;
            }
            version = null;
            return false;
        }
    }
}
