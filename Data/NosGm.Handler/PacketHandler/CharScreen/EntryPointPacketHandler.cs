using NosGm.Packets.Packets.ClientPackets;

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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        public void LoadCharacters(NosGmEntryPointPacket packet)
        {

            string[] loginPacketParts = null;
           

            if (loginPacketParts != null)
            {
                Logger.Info("========== ENTRY ==========");
                Logger.Info(packet.PacketData);

                for (int i = 0; i < loginPacketParts.Length; i++)
                {
                    Logger.Info($"{i} -> {loginPacketParts[i]}");
                }
            }
            if (!string.IsNullOrEmpty(packet.PacketData))
            {
                loginPacketParts = packet.PacketData.Split(' ');
            }
            bool isCrossServerLogin = false;


            // Load account by given SessionId
            if (Session.Account == null)
            {
                bool hasRegisteredAccountLogin = true;
                AccountDTO account = null;
                if (loginPacketParts.Length > 4)
                {
                    if (loginPacketParts.Length > 6 &&
                        loginPacketParts[3] == "DAC" &&
                        loginPacketParts[8] == "CrossServerAuthenticate")
                    {
                        isCrossServerLogin = true;
                        account = DAOFactory.AccountDAO.LoadByName(loginPacketParts[4]);
                    }
                    else
                    {
                        account = DAOFactory.AccountDAO.LoadByName(loginPacketParts[3]);
                    }
                }

                try
                {
                    if (account != null)
                    {
                        if (isCrossServerLogin)
                        {
                            hasRegisteredAccountLogin = CommunicationServiceClient.Instance.IsCrossServerLoginPermitted(account.AccountId, Session.SessionId);
                        }
                        else

                        {
                          
                            hasRegisteredAccountLogin = CommunicationServiceClient.Instance.IsLoginPermitted(account.AccountId, Session.SessionId);
                        }

                        Logger.Info("========== LOGIN CHECK ==========");
                        Logger.Info($"Session.SessionId = {Session.SessionId}");
                        Logger.Info($"Packet Session   = {loginPacketParts[1]}");
                        Logger.Info($"Username         = {loginPacketParts[2]}");
                        Logger.Info($"IsLoginPermitted = {hasRegisteredAccountLogin}");
                    }
                }
                catch (Exception ex)
                {
                    //LOGGERServerLog($"{ex.ToString()}", LogType.ServerError);
                    Session.Disconnect();
                    return;
                }

                if (loginPacketParts.Length <= 3 || !hasRegisteredAccountLogin)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, login has not been registered or Account is already logged in.");
                    Session.Disconnect();
                    return;
                }

                if (account == null)
                {
                    Logger.Debug($"Client {Session.ClientId} forced Disconnection, invalid AccountName.");
                    Session.Disconnect();
                    return;
                }

                if (!account.Password.ToLower().Equals(CryptographyBase.Sha512(loginPacketParts[7])) && !isCrossServerLogin)
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
                if (byte.TryParse(loginPacketParts[6], out var slot))
                {
                    new SelectCharacterPacketHandler(Session).SelectCharacter(new SelectPacket { Slot = slot });
                }
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

        #endregion
    }
}