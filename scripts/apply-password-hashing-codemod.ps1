$ErrorActionPreference = "Stop"

function Normalize-NewLines {
    param(
        [string]$Value,
        [string]$NewLine
    )

    return [regex]::Replace($Value, "`r`n|`n|`r", $NewLine)
}

function Replace-LiteralOnce {
    param(
        [string]$Path,
        [string]$OldValue,
        [string]$NewValue,
        [string]$Description,
        [string]$AppliedMarker
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Source file not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }
    $oldNormalized = Normalize-NewLines $OldValue $newLine
    $newNormalized = Normalize-NewLines $NewValue $newLine
    $first = $content.IndexOf($oldNormalized, [StringComparison]::Ordinal)

    if ($first -lt 0) {
        if ($content.Contains($AppliedMarker)) {
            Write-Host "Already applied: $Description"
            return
        }

        throw "Unable to find expected source for: $Description"
    }

    $second = $content.IndexOf($oldNormalized, $first + $oldNormalized.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match for: $Description"
    }

    $updated = $content.Substring(0, $first) + $newNormalized + $content.Substring($first + $oldNormalized.Length)
    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $Path),
        $updated,
        (New-Object Text.UTF8Encoding($true)))
    Write-Host "Applied: $Description"
}

$coreProject = "Data/NosGm.Core/NosGm.Core.csproj"
$accountInterface = "Data/NosGm.DAL/NosGm.DAL.Interface/IAccountDAO.cs"
$accountDao = "Data/NosGm.DAL/NosGm.DAL.DAO/AccountDAO.cs"
$loginHandler = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs"
$entryHandler = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs"
$deleteHandler = "Data/NosGm.Handler/PacketHandler/CharScreen/DeleteCharacterPacketHandler.cs"
$importAccounts = "Data/NosGm.Program/NosGm.Parser/Import/ImportAccounts.cs"
$roadmap = "docs/NOSGM_NEXT.md"

Replace-LiteralOnce $coreProject @'
    <Compile Include="Cryptography\LoginCryptography.cs" />
'@ @'
    <Compile Include="Cryptography\LoginCryptography.cs" />
    <Compile Include="Security\PasswordHashService.cs" />
'@ "compile the password hash service" 'Compile Include="Security\PasswordHashService.cs"'

Replace-LiteralOnce $accountInterface @'
        SaveResult InsertOrUpdate(ref AccountDTO account);

        bool ContainsAccounts();
'@ @'
        SaveResult InsertOrUpdate(ref AccountDTO account);

        bool TryUpgradePassword(long accountId, string expectedPassword, string upgradedPassword);

        bool ContainsAccounts();
'@ "expose atomic password upgrades" "TryUpgradePassword(long accountId"

Replace-LiteralOnce $accountDao @'
        public async Task WriteGeneralLog(long accountId, string ipAddress, long? characterId, GeneralLogType logType,
            string logData)
'@ @'
        public bool TryUpgradePassword(long accountId, string expectedPassword, string upgradedPassword)
        {
            if (accountId <= 0 || expectedPassword == null || string.IsNullOrWhiteSpace(upgradedPassword) ||
                upgradedPassword.Length > 255)
            {
                return false;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    Account entity = context.Account.FirstOrDefault(a => a.AccountId.Equals(accountId));
                    if (entity == null ||
                        !string.Equals(entity.Password, expectedPassword, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    entity.Password = upgradedPassword;
                    return context.SaveChanges() == 1;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync(
                    $"Unable to upgrade password hash for AccountId={accountId}. Message: {e.Message} | Source: {e.Source}",
                    LogType.ERROR);
                return false;
            }
        }

        public async Task WriteGeneralLog(long accountId, string ipAddress, long? characterId, GeneralLogType logType,
            string logData)
'@ "persist password upgrades without rewriting the account" "public bool TryUpgradePassword(long accountId"

Replace-LiteralOnce $loginHandler @'
            UserDTO user = new UserDTO
            {
                Name = loginPacket.Name,
                Password = ServerConfiguration.UseOldCrypto
                    ? CryptographyBase.Sha512(LoginCryptography.GetPassword(loginPacket.Password)).ToUpperInvariant()
                    : loginPacket.Password
            };

            AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(user.Name);
'@ @'
            if (!TryGetClearPassword(loginPacket.Password, out string clearPassword))
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Invalid password payload");
                return;
            }

            string username = loginPacket.Name;
            AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(username);
'@ "recover the clear credential once" "TryGetClearPassword(loginPacket.Password"

Replace-LiteralOnce $loginHandler @'
            if (!string.Equals(loadedAccount.Name, user.Name, StringComparison.Ordinal))
'@ @'
            if (!string.Equals(loadedAccount.Name, username, StringComparison.Ordinal))
'@ "preserve username casing checks" "loadedAccount.Name, username"

Replace-LiteralOnce $loginHandler @'
            if (!PasswordMatches(loadedAccount.Password, user.Password))
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Wrong credentials");
                return;
            }
'@ @'
            if (!PasswordHashService.VerifyPassword(
                    loadedAccount.Password,
                    clearPassword,
                    ServerConfiguration.UseOldCrypto,
                    out bool passwordNeedsUpgrade))
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Wrong credentials");
                return;
            }

            if (passwordNeedsUpgrade)
            {
                UpgradePasswordHash(loadedAccount, clearPassword);
            }
'@ "verify legacy and versioned password hashes" "out bool passwordNeedsUpgrade"

Replace-LiteralOnce $loginHandler @'
            Logger.Info($"{user.Name} connected | SessionID: {newSessionId}");
'@ @'
            Logger.Info($"{username} connected | SessionID: {newSessionId}");
'@ "use the validated username in login diagnostics" '{$"{username} connected'

Replace-LiteralOnce $loginHandler @'
            string serversPacket = BuildServersPacket(
                user.Name,
'@ @'
            string serversPacket = BuildServersPacket(
                username,
'@ "send the validated username to the world list" "BuildServersPacket(`r`n                username"

Replace-LiteralOnce $loginHandler @'
        private static bool PasswordMatches(string storedPassword, string suppliedPassword)
        {
            if (storedPassword == null || suppliedPassword == null)
            {
                return false;
            }

            return ServerConfiguration.UseOldCrypto
                ? string.Equals(storedPassword, suppliedPassword, StringComparison.OrdinalIgnoreCase)
                : string.Equals(storedPassword, suppliedPassword, StringComparison.Ordinal);
        }
'@ @'
        private static bool TryGetClearPassword(string packetPassword, out string clearPassword)
        {
            clearPassword = null;
            if (string.IsNullOrWhiteSpace(packetPassword))
            {
                return false;
            }

            try
            {
                clearPassword = ServerConfiguration.UseOldCrypto
                    ? LoginCryptography.GetPassword(packetPassword)
                    : packetPassword;
            }
            catch (Exception)
            {
                return false;
            }

            return clearPassword != null &&
                   clearPassword.Length <= PasswordHashService.MaximumCredentialLength;
        }

        private static void UpgradePasswordHash(AccountDTO account, string clearPassword)
        {
            if (account == null ||
                !PasswordHashService.TryHashPassword(clearPassword, out string upgradedPassword))
            {
                return;
            }

            string expectedPassword = account.Password;
            if (DAOFactory.AccountDAO.TryUpgradePassword(
                    account.AccountId,
                    expectedPassword,
                    upgradedPassword))
            {
                account.Password = upgradedPassword;
                Logger.Info($"Password hash upgraded | AccountId={account.AccountId}");
                return;
            }

            Logger.Debug($"Password hash upgrade skipped | AccountId={account.AccountId}");
        }
'@ "upgrade successful legacy logins" "private static void UpgradePasswordHash(AccountDTO account"

Replace-LiteralOnce $entryHandler @'
                if (!account.Password.ToLower().Equals(CryptographyBase.Sha512(loginPacketParts[7])) && !isCrossServerLogin)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid Password.");
                    Session.Disconnect();
                    return;
                }
'@ @'
                bool passwordValid = isCrossServerLogin ||
                                     loginPacketParts.Length > 7 &&
                                     PasswordHashService.VerifyPassword(
                                         account.Password,
                                         loginPacketParts[7],
                                         true,
                                         out _);
                if (!passwordValid)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid Password.");
                    Session.Disconnect();
                    return;
                }
'@ "support versioned hashes at world entry" "bool passwordValid = isCrossServerLogin"

Replace-LiteralOnce $deleteHandler @'
            if (account.Password.ToLower() == CryptographyBase.Sha512(characterDeletePacket.Password))
'@ @'
            if (PasswordHashService.VerifyPassword(
                    account.Password,
                    characterDeletePacket.Password,
                    true,
                    out _))
'@ "support versioned hashes for character deletion" "characterDeletePacket.Password,`r`n                    true"

Replace-LiteralOnce $importAccounts @'
using System.Collections.Generic;
'@ @'
using System;
using System.Collections.Generic;
'@ "import InvalidOperationException" "using System;"

Replace-LiteralOnce $importAccounts @'
            if (!DAOFactory.AccountDAO.ContainsAccounts())
            {
                accounts.Add(new AccountDTO
                {
'@ @'
            if (!DAOFactory.AccountDAO.ContainsAccounts())
            {
                if (!PasswordHashService.TryHashPassword("HelloImZuya", out string passwordHash))
                {
                    throw new InvalidOperationException("Unable to create the initial account password hash.");
                }

                accounts.Add(new AccountDTO
                {
'@ "hash the initial account password" "out string passwordHash"

Replace-LiteralOnce $importAccounts @'
                    Password = CryptographyBase.Sha512("HelloImZuya")
'@ @'
                    Password = passwordHash
'@ "store the initial account with the versioned hash" "Password = passwordHash"

Replace-LiteralOnce $roadmap @'
- [ ] Add a versioned password-hash migration using a per-account salt and a supported adaptive KDF.
'@ @'
- [x] Add a versioned password-hash migration using a per-account salt and a supported adaptive KDF.
'@ "complete the Phase 0 password milestone" "[x] Add a versioned password-hash migration"

Write-Host "Password hashing codemod applied successfully."
