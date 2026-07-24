using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NosGm.Packets.Packets.ClientPackets;

namespace NosGm.Handler.Packets.CharScreenPackets
{
    public class SelectPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession Session;

        #endregion

        #region Instantiation

        public SelectPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Methods

        public void SelectCharacter(SelectPacket selectPacket)
        {
            try
            {
                #region Validate Session

                if (Session?.Account == null
                    || Session.HasSelectedCharacter)
                {
                    return;
                }

                #endregion

                #region Load Character

                CharacterDTO characterDTO = DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, selectPacket.Slot);

                if (characterDTO == null)
                {
                    return;
                }

                Character character = new Character(characterDTO);

                #endregion

                #region Unban Character

                if (ServerManager.Instance.BannedCharacters.Contains(character.CharacterId))
                {
                    ServerManager.Instance.BannedCharacters.RemoveAll(s => s == character.CharacterId);
                }

                #endregion

                #region Initialize Character

                character.Initialize();

                short MapId = character.MapId;
                short MapX = character.MapX;
                short MapY = character.MapY;
                if (ServerManager.Instance.ChannelId == 51)
                {
                    if (character.Faction == FactionType.Angel)
                        MapId = 130;
                    else
                        MapId = 131;

                    MapX = 40;
                    MapY = 42;
                }

                character.MapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(MapId);
                character.PositionX = MapX;
                character.PositionY = MapY;
                character.Authority = Session.Account.Authority;

                Session.SetCharacter(character);

                #endregion

                #region General Logs

                // GeneralLog is an append-only audit table. Loading the complete account
                // history here caused large EF materialization and GC spikes during login.
                character.GeneralLogs = new ThreadSafeGenericList<GeneralLogDTO>();

                #endregion

                #region Limitations

                // If there are > 3 accounts connected, kick this session.
                if (CommunicationServiceClient.Instance.RetrieveOnlineCharacters(character.CharacterId).Count() > 3)
                {
                    Session.Disconnect();
                }

                #endregion

                #region FishingLogs

                DAOFactory.CharacterFishDAO.LoadByCharacterId(Session.Character.CharacterId)?.ToList().ForEach(s => Session.Character.FishingLogs.Add(s));

                #endregion

                #region Other Character Stuff

                Session.Character.Respawns = DAOFactory.RespawnDAO.LoadByCharacter(Session.Character.CharacterId).ToList();
                Session.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(Session.Character.CharacterId).ToList();
                Session.Character.LoadInventory();
                Session.Character.LoadQuicklists();
                Session.Character.GenerateMiniland();

                #endregion

                #region Quests

                if (!DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.IsMainQuest) && !DAOFactory.QuestLogDAO.LoadByCharacterId(Session.Character.CharacterId).Any(s => s.QuestId == 1997))
                {
                    CharacterQuestDTO firstQuest = new CharacterQuestDTO
                    {
                        CharacterId = Session.Character.CharacterId,
                        //QuestId = 1997,
                        //IsMainQuest = false
                    };

                    DAOFactory.CharacterQuestDAO.InsertOrUpdate(firstQuest);
                }

                DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(qst => Session.Character.Quests.Add(new CharacterQuest(qst)));

                //DAOFactory.CharacterQuestDAO.LoadByCharacterId(Session.Character.CharacterId).ToList()
                //    .ForEach(qst => Session.Character.Quests.Add(new CharacterQuest(qst)));

                #endregion

                #region Fix Partner Slots

                if (character.MaxPartnerCount < 3)
                {
                    character.MaxPartnerCount = 3;
                }

                #endregion

                #region Load Mates

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

                #endregion

                #region Load Permanent Buff

                Session.Character.LastPermBuffRefresh = DateTime.Now;

                #endregion

                #region CharacterLife

                Session.Character.Life = Observable.Interval(TimeSpan.FromMilliseconds(300))
                    .Subscribe(x => Session.Character.CharacterLife());

                #endregion

                #region Title

                DAOFactory.CharacterTitleDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                {
                    Session.Character.Title.Add(s);
                });

                #endregion

                #region Battle Pass
                //DAOFactory.CharacterBattlePassDAO.LoadByCharacterId(Session.Character.CharacterId).ToList().ForEach(s =>
                //{
                //    Session.Character.CharacterBattlePass.Add(s);
                //});
                #endregion

                #region Load Amulet

                Observable.Timer(TimeSpan.FromSeconds(1))
                    .Subscribe(o =>
                    {
                        ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);

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
                Logger.Error("Failed selecting the character.", ex);
            }
            finally
            {
                // Suspicious activity detected -- kick!
                if (Session != null && (!Session.HasSelectedCharacter || Session.Character == null))
                {
                    Session.Disconnect();
                }
            }
        }

        #endregion
    }
}
