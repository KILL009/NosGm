using NosGm.Packets.Packets.ClientPackets;

using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using NosGm.Master.Library.Client;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.BasicPacket.CharScreen
{
    internal class SelectCharacterPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SelectCharacterPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion
        
        #region Methods

        public void SelectCharacter(SelectPacket selectPacket)
        {            
            try
            {
                #region Validate Session

                if (Session?.Account == null || Session.HasSelectedCharacter)
                {
                    Session?.Disconnect();
                    return;
                }

                #endregion

                #region Load Character

                var characterDTO = DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, selectPacket.Slot);

                if (characterDTO == null)
                {
                    Session.Disconnect();
                    return;
                }

                var character = new Character(characterDTO);

                #endregion

                #region Unban Character

                if (ServerManager.Instance.BannedCharacters.Contains(character.CharacterId))
                {
                    ServerManager.Instance.BannedCharacters.RemoveAll(s => s == character.CharacterId);
                }

                #endregion

                #region Initialize Character

                character.Initialize();

                character.MapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(character.MapId);
                character.PositionX = character.MapX;
                character.PositionY = character.MapY;
                character.Authority = Session.Account.Authority;

                Session.SetCharacter(character);

                #endregion

                #region Limitations

                // If there are > 3 accounts connected, kick this session.
                if (CommunicationServiceClient.Instance.RetrieveOnlineCharacters(character.CharacterId).Count() > 3 )
                {
                   Session.Disconnect();
                }

                #endregion

                #region General Logs

                // GeneralLog is persisted audit history, not character session state.
                // Keeping this collection empty avoids materializing every account log at login.
                character.GeneralLogs = new ThreadSafeGenericList<GeneralLogDTO>();

                #endregion

                #region Other Character Stuffs

                Session.Character.Respawns = DAOFactory.RespawnDAO.LoadByCharacter(Session.Character.CharacterId).ToList();
                Session.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(Session.Character.CharacterId).ToList();
                Session.Character.LoadInventory();
                Session.Character.LoadQuicklists();
                Session.Character.GenerateMiniland();

                #endregion

                #region Quests

                try
                {
                    DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(qst => Session.Character.Quests.Add(new CharacterQuest(qst)));
                }
                catch (Exception e)
                {
                    //LOGGERServerLog($"[Quest] {e.ToString()}", LogType.ServerError);
                    try
                    {
                        File.AppendAllText("C:\\WORLD_QUESTBUG.txt", e.ToString() + "\n");
                    }
                    catch
                    {
                    }
                }

                #endregion

                #region Title

                DAOFactory.CharacterTitleDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                {
                    Session.Character.Title.Add(s);
                });

                #endregion

                #region Fix Partner Slots

                if (character.MaxPartnerCount < 3)
                {
                    character.MaxPartnerCount = 3;
                }

                #endregion

                #region Load Mates

                try
                {
                    DAOFactory.MateDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                    {
                        Mate mate = new Mate(s)
                        {
                            Owner = Session.Character
                        };

                        mate.GenerateMateTransportId();
                        mate.Monster = ServerManager.GetNpcMonster(s.NpcMonsterVNum);

                        Session.Character.Mates.Add(mate);
                    });
                }
                catch (Exception b)
                {
                    //LOGGERServerLog($"[Mate] {b.ToString()}", LogType.ServerError);
                    try
                    {
                        File.AppendAllText("C:\\WORLD_MATEBUG.txt", b.ToString() + "\n");
                    }
                    catch
                    {
                    }
                }

                #endregion

                #region Load Permanent Buff

                Session.Character.LastPermBuffRefresh = DateTime.Now;

                #endregion

                #region CharacterLife

                Session.Character.Life = Observable.Interval(TimeSpan.FromMilliseconds(300)).Subscribe(x => Session?.Character?.CharacterLife());

                #endregion

                #region Load Amulet

                Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                {
                    if (Session?.Character == null)
                    {
                        return;
                    }

                    var amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);

                    if (amulet?.ItemDeleteTime != null || amulet?.DurabilityPoint > 0)
                    {
                        Session.Character.AddBuff(new Buff(62, Session.Character.Level), Session.Character.BattleEntity);
                    }
                });

                #endregion

                #region Load Static Buff

                foreach (StaticBuffDTO staticBuff in DAOFactory.StaticBuffDAO.LoadByCharacterId(Session.Character.CharacterId))
                {
                    if (staticBuff.CardId != 319 /* Wedding */)
                    {
                        Session.Character.AddStaticBuff(staticBuff);
                    }
                }

                #endregion

                #region RemoveAllDupedShell

                int cleanedShells = DAOFactory.ShellEffectDAO.CleanupDuplicateNonRuneEffects(
                    Session.Character.CharacterId);
                if (cleanedShells > 0)
                {
                    Logger.Warn($"Removed duplicate shell effects from {cleanedShells} equipment serials for CharacterId {Session.Character.CharacterId}.");
                }

                #endregion

                #region Enter the World

                Session.SendPacket("OK");


                CommunicationServiceClient.Instance.ConnectCharacter(ServerManager.Instance.WorldId, character.CharacterId);

                character.Channel = ServerManager.Instance;

                #endregion
            }
            catch (Exception ex)
            {
                //LOGGERServerLog($"[CharacterSelection] {ex.ToString()}", LogType.ServerError);
            }
            finally
            {
                // Suspicious activity detected -- kick!
                if (Session != null && ((!Session.HasSelectedCharacter || Session.Character == null) || (CommunicationServiceClient.Instance.RetrieveOnlineCharacters(Session.Character.CharacterId).Count() >= 4)))
                {
                    Session.Disconnect();
                }
            }
        }

        #endregion
    }
}
