using NosGm.Packets.Packets.ClientPackets;

using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using NosGm.Handler.Packets.CharScreenPackets;
using NosGm.Master.Library.Client;
using System;
using System.Linq;

namespace NosGm.Handler.BasicPacket.CharScreen
{
    internal class EntryPointPacketHandler : IPacketHandler
    {
        #region Members

        private static readonly object GameforgeAuthSync = new object();

        private static bool _authenticationServiceAuthenticated;

        #endregion

        #region Instantiation

        public EntryPointPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        public void LoadCharacters(NosGmEntryPointPacket packet)
        {
            string[] loginPacketParts = string.IsNullOrWhiteSpace(packet?.PacketData)
                ? Array.Empty<string>()
                : packet.PacketData.Split(' ');
            bool isCrossServerLogin = false;

            if (Session.Account == null)
            {
                if (loginPacketParts.Length <= 3)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, malformed character entry packet.");
                    Session.Disconnect();
                    return;
                }

                AccountDTO account;
                bool hasCrossServerMarker = string.Equals(
                    loginPacketParts[3],
                    "DAC",
                    StringComparison.Ordinal);

                if (hasCrossServerMarker)
                {
                    if (loginPacketParts.Length <= 8 ||
                        !string.Equals(
                            loginPacketParts[8],
                            "CrossServerAuthenticate",
                            StringComparison.Ordinal))
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, malformed cross-server entry packet.");
                        Session.Disconnect();
                        return;
                    }

                    isCrossServerLogin = true;
                    account = LoadAccountByProtocolName(loginPacketParts[4]);
                }
                else
                {
                    if (loginPacketParts.Length <= 7)
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, incomplete character entry packet.");
                        Session.Disconnect();
                        return;
                    }

                    account = LoadAccountByProtocolName(loginPacketParts[3]);
                }

                if (account == null)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid AccountName.");
                    Session.Disconnect();
                    return;
                }

                bool hasRegisteredAccountLogin;
                try
                {
                    hasRegisteredAccountLogin = isCrossServerLogin
                        ? CommunicationServiceClient.Instance.IsCrossServerLoginPermitted(
                            account.AccountId,
                            Session.SessionId)
                        : CommunicationServiceClient.Instance.IsLoginPermitted(
                            account.AccountId,
                            Session.SessionId);
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Character entry validation failed | ClientId={Session.ClientId} AccountId={account.AccountId}",
                        ex);
                    Session.Disconnect();
                    return;
                }

                Logger.Debug(
                    $"Character entry login check | ClientId={Session.ClientId} " +
                    $"AccountId={account.AccountId} CrossServer={isCrossServerLogin} " +
                    $"Permitted={hasRegisteredAccountLogin}");

                if (!hasRegisteredAccountLogin)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, login has not been registered or Account is already logged in.");
                    Session.Disconnect();
                    return;
                }

                bool isGameforgePasswordlessLogin = !isCrossServerLogin &&
                    string.Equals(loginPacketParts[7], "thisisgfmode", StringComparison.Ordinal);
                if (isGameforgePasswordlessLogin)
                {
                    if (!ServerConfiguration.EnableGameforgeTokenLogin ||
                        !EnsureAuthenticationServiceAuthenticated())
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, Gameforge authentication service unavailable.");
                        Session.Disconnect();
                        return;
                    }

                    bool permitValid;
                    try
                    {
                        permitValid = AuthentificationServiceClient.Instance.ConsumeGameforgeWorldPermit(
                            account.AccountId,
                            Session.SessionId,
                            NormalizeRemoteIp(Session.IpAddress));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"Gameforge World-entry validation failed | ClientId={Session.ClientId} AccountId={account.AccountId}",
                            ex);
                        Session.Disconnect();
                        return;
                    }

                    if (!permitValid)
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, Gameforge World permit is invalid or expired.");
                        Session.Disconnect();
                        return;
                    }
                }

                bool passwordValid = isCrossServerLogin ||
                                     isGameforgePasswordlessLogin ||
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

                Session.InitializeAccount(new Account(account), isCrossServerLogin);
                ServerManager.Instance.CharacterScreenSessions[Session.Account.AccountId] = Session;
            }

            if (isCrossServerLogin)
            {
                if (!byte.TryParse(loginPacketParts[6], out byte slot))
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid cross-server character slot.");
                    Session.Disconnect();
                    return;
                }

                new SelectCharacterPacketHandler(Session).SelectCharacter(new SelectPacket { Slot = slot });
            }
            else
            {
                var characters = DAOFactory.CharacterDAO.LoadByAccount(Session.Account.AccountId);
                Session.SendPacket("clist_start 0");

                foreach (CharacterDTO character in characters)
                {
                    var inventory =
                        DAOFactory.ItemInstanceDAO.LoadByType(character.CharacterId, InventoryType.Wear);

                    ItemInstance[] equipment = new ItemInstance[17];

                    foreach (ItemInstanceDTO equipmentEntry in inventory)
                    {
                        ItemInstance currentInstance = new ItemInstance(equipmentEntry);

                        if (currentInstance != null)
                        {
                            equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;
                        }
                    }

                    string petlist = "";
                    var mates = DAOFactory.MateDAO.LoadByCharacterId(character.CharacterId).ToList();

                    for (int i = 0; i < 26; i++)
                    {
                        petlist += (i != 0 ? "." : "") +
                                   (mates.Count > i ? $"{mates[i].Skin}.{mates[i].NpcMonsterVNum}" : "-1");
                    }

                    Session.SendPacket($"clist {character.Slot} {character.Name} 0 {(byte)character.Gender} {(byte)character.HairStyle} {(byte)character.HairColor} 0 {(byte)character.Class} {character.Level} {character.HeroLevel} {equipment[(byte)EquipmentType.Hat]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Armor]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.WeaponSkin]?.ItemVNum ?? (equipment[(byte)EquipmentType.MainWeapon]?.ItemVNum ?? -1)}.{equipment[(byte)EquipmentType.SecondaryWeapon]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Mask]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Fairy]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeSuit]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeHat]?.ItemVNum ?? -1} {character.JobLevel}  1 1 {petlist} {(equipment[(byte)EquipmentType.Hat]?.Item.IsColored == true ? equipment[(byte)EquipmentType.Hat].Design : 0)} 0");
                }

                Session.SendPacket("clist_end");
            }
        }

        private static bool EnsureAuthenticationServiceAuthenticated()
        {
            if (_authenticationServiceAuthenticated)
            {
                return true;
            }

            lock (GameforgeAuthSync)
            {
                if (_authenticationServiceAuthenticated)
                {
                    return true;
                }

                try
                {
                    _authenticationServiceAuthenticated = AuthentificationServiceClient.Instance.Authenticate(
                        ServerConfiguration.AuthServiceKey);
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not authenticate World against the authentication service", ex);
                    _authenticationServiceAuthenticated = false;
                }

                return _authenticationServiceAuthenticated;
            }
        }

        private static string NormalizeRemoteIp(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                return uri.Host;
            }

            string value = endpoint.Trim();
            if (value.StartsWith("[", StringComparison.Ordinal))
            {
                int closingBracket = value.IndexOf(']');
                if (closingBracket > 1)
                {
                    return value.Substring(1, closingBracket - 1);
                }
            }

            int lastColon = value.LastIndexOf(':');
            if (lastColon > 0 && value.IndexOf(':') == lastColon)
            {
                return value.Substring(0, lastColon);
            }

            return value;
        }

        private static AccountDTO LoadAccountByProtocolName(string protocolUsername)
        {
            AccountDTO account = DAOFactory.AccountDAO.LoadByName(protocolUsername);
            if (account != null)
            {
                return account;
            }

            if (!ClientRegionMap.TryStripProtocolPrefix(
                    protocolUsername,
                    out string accountName,
                    out _))
            {
                return null;
            }

            return DAOFactory.AccountDAO.LoadByName(accountName);
        }

        #endregion
    }
}