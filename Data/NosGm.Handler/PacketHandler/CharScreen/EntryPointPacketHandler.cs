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
        #region Instantiation

        public EntryPointPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Members

        private static readonly object ModernAuthSync = new object();

        private static bool _modernAuthServiceAuthenticated;

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

            // Load account by given SessionId
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
                    account = DAOFactory.AccountDAO.LoadByName(loginPacketParts[4]);
                }
                else
                {
                    if (loginPacketParts.Length <= 7)
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, incomplete character entry packet.");
                        Session.Disconnect();
                        return;
                    }

                    account = DAOFactory.AccountDAO.LoadByName(loginPacketParts[3]);
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

                bool isModernPasswordlessLogin = !isCrossServerLogin &&
                    string.Equals(loginPacketParts[7], "thisisgfmode", StringComparison.Ordinal);
                if (isModernPasswordlessLogin)
                {
                    if (!EnsureModernAuthServiceAuthenticated())
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, modern authentication service unavailable.");
                        Session.Disconnect();
                        return;
                    }

                    bool modernPermitValid;
                    try
                    {
                        modernPermitValid = AuthentificationServiceClient.Instance.ConsumeModernLoginSession(
                            account.AccountId,
                            Session.SessionId,
                            NormalizeRemoteIp(Session.IpAddress));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"Modern World-entry validation failed | ClientId={Session.ClientId} AccountId={account.AccountId}",
                            ex);
                        Session.Disconnect();
                        return;
                    }

                    if (!modernPermitValid)
                    {
                        Logger.Debug($"Client {Session.ClientId} forced Disconnection, modern World-entry permit is invalid or expired.");
                        Session.Disconnect();
                        return;
                    }
                }

                bool passwordValid = isCrossServerLogin ||
                                     isModernPasswordlessLogin ||
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
                // TODO: Wrap Database access up to GO
                var characters = DAOFactory.CharacterDAO.LoadByAccount(Session.Account.AccountId);

                // load characterlist packet for each character in CharacterDTO
                Session.SendPacket("clist_start 0");

                foreach (CharacterDTO character in characters)
                {
                    var inventory =
                        DAOFactory.ItemInstanceDAO.LoadByType(character.CharacterId, InventoryType.Wear);

                    ItemInstance[] equipment = new ItemInstance[17];

                    foreach (ItemInstanceDTO equipmentEntry in inventory)
                    {
                        // explicit load of iteminstance
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
                        //0.2105.1102.319.0.632.0.333.0.318.0.317.0.9.-1.-1.-1.-1.-1.-1.-1.-1.-1.-1.-1.-1
                        petlist += (i != 0 ? "." : "") + (mates.Count > i ? $"{mates[i].Skin}.{mates[i].NpcMonsterVNum}" : "-1");
                    }

                    // 1 1 before long string of -1.-1 = act completion
                    Session.SendPacket($"clist {character.Slot} {character.Name} 0 {(byte)character.Gender} {(byte)character.HairStyle} {(byte)character.HairColor} 0 {(byte)character.Class} {character.Level} {character.HeroLevel} {equipment[(byte)EquipmentType.Hat]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Armor]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.WeaponSkin]?.ItemVNum ?? (equipment[(byte)EquipmentType.MainWeapon]?.ItemVNum ?? -1)}.{equipment[(byte)EquipmentType.SecondaryWeapon]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Mask]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Fairy]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeSuit]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeHat]?.ItemVNum ?? -1} {character.JobLevel}  1 1 {petlist} {(equipment[(byte)EquipmentType.Hat]?.Item.IsColored == true ? equipment[(byte)EquipmentType.Hat].Design : 0)} 0");
                }

                Session.SendPacket("clist_end");
            }
        }

        private static bool EnsureModernAuthServiceAuthenticated()
        {
            if (_modernAuthServiceAuthenticated)
            {
                return true;
            }

            lock (ModernAuthSync)
            {
                if (_modernAuthServiceAuthenticated)
                {
                    return true;
                }

                try
                {
                    _modernAuthServiceAuthenticated = AuthentificationServiceClient.Instance.Authenticate(
                        ServerConfiguration.AuthServiceKey);
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not authenticate World against the modern authentication service", ex);
                    _modernAuthServiceAuthenticated = false;
                }

                return _modernAuthServiceAuthenticated;
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

        #endregion
    }
}