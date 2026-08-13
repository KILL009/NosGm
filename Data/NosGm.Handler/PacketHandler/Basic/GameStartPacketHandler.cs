using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;

using static System.Collections.Specialized.BitVector32;
using System.Net.Mail;
using System.Threading;
using NosGm.Configuration;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Extension.Reputation;
using NosGm.GameObject.ThreadEnum;
using NosGm.GameObject.NosGm.Thread.System;
using System.Windows.Forms;
using NosGm.GameObject.Threads.WorkerThreads.Battle.Buff;
using NosGm.GameObject.Plugin.Load.Handler;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class GameStartPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GameStartPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        public SeasonType Season { get; }

        #endregion

        #region Methods

        public void StartGame(GameStartPacket gameStartPacket)
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            void Start()
            {
                if (Session?.Character == null || Session.IsOnMap || !Session.HasSelectedCharacter)
                {
                    // character should have been selected in SelectCharacter
                    return;
                }

                bool shouldRespawn = false;



                if (Session.Character.MapInstance?.Map?.MapTypes != null)
                {
                    if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4)
                        && ServerManager.Instance.ChannelId != 51)
                    {
                        if (ServerManager.Instance.IsAct4Online())
                        {
                            Session.Character.ChangeChannel(ServerConfiguration.IPAddress, Convert.ToInt32(ServerConfiguration.GlacernonServerPort), 2);
                            return;
                        }

                        shouldRespawn = true;
                    }
                }

                Session.CurrentMapInstance = Session.Character.MapInstance;

                BuffThread.RemoveGroupBuff(Session);
                GameStartThread.BattlePass(Session);
                GameStartThread.Configuration(Session);
                GameStartThread.GetIp(Session);
                GameStartThread.GenerateBn(Session);
                GameStartThread.EditorMode(Session);
                switch (GameConfiguration.Season)
                {
                    case 1:
                        Session.SendPacket($"msg 5 Current Raid Season: Elements");
                        break;

                    case 2:
                        Session.SendPacket($"msg 5 Current Raid Season: Enlightment");
                        break;

                    case 3:
                        Session.SendPacket($"msg 5 Current Raid Season: Hope");
                        break;

                    case 4:
                        Session.SendPacket($"msg 5 Current Raid Season: Despair");
                        break;
                }

                LoggerService.LogServer.Logger.LogAsync($"[LogIn] {Session.Character.Name} just logged in", LogType.INFO);

                PlayerCountThread.UpdatePlayerCount(PlayerCountType.Increased);

                // Load mail only for the character entering the World. Loading every
                // connected player's mailbox here creates O(N²) database work during ramps.
                LoadMail.LoadMailProcess(Session);

                Session.SendPacket($"lf 1 {DateTime.Now.ToString("HH:mm")}");
                //Session.SendPacket($"qnaml 7 #guri^505 Duel System\n\nDo you want to queue up for a Duel?");
                Session.SendPacket($"qnaml 5 #guri^507 Sistema de Teletransporte\n\nQuieres abrir el menú de Teletransporte");
                Session.Character.GenerateMastery();
                Session.Character.SendWorldInformation();

                RewardExtension.DailyReward(Session);
                MiniPetExtension.GenerateMiniPet(Session);
                RefreshExtension.DuelCountRefresh(Session);
                RefreshExtension.RefreshPrimalQuest(Session);
                RefreshExtension.IceFlowerRefresh(Session);

                Session.Character.LoadSpeed();
                Session.Character.LoadSkills();
                Session.Character.LoadPartnerSkills();
                Session.SendPacket(Session.Character.GenerateNowTime());
                Session.SendPacket(Session.Character.GenerateSpPoint());
                Session.SendPacket("rsfi 1 1 0 9 0 9");

                // Fishing
                Session.SendPacket(CharacterExtension.GenerateFishPacket(Session, FishPacketType.Login, 0, 0));

                // Title
                Session.SendPacket(Session.Character.GenerateTitle());
                Session.SendPacket(Session.Character.GenerateTitInfo());
                Session.Character.GetTitleFromLevel();
                Session.Character.GetEffectFromTitle();

                Session.Character.Quests?.Where(q => q?.Quest?.TargetMap != null).ToList()
                    .ForEach(qst => Session.SendPacket(qst.Quest.TargetPacket()));

                if (Session.Character.Hp <= 0 && (!Session.Character.IsSeal || ServerManager.Instance.ChannelId != 51))
                {
                    ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
                }
                else
                {
                    if (shouldRespawn)
                    {
                        RespawnMapTypeDTO resp = Session.Character.Respawn;
                        short x = (short)(resp.DefaultX + ServerManager.RandomNumber(-3, 3));
                        short y = (short)(resp.DefaultY + ServerManager.RandomNumber(-3, 3));
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, resp.DefaultMapId, x, y);
                    }
                    else
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId);
                    }
                }

                #region Restore Login Health

                // Current HP is persisted, but maximum HP is rebuilt from the active
                // equipment, cellon options, titles and buffs. Refresh those derived
                // values after entering the map and always begin the login at full HP.
                Session.Character.GenerateEquipment();
                Session.Character.Hp = (int)Session.Character.HPLoad();
                Session.SendPacket(Session.Character.GenerateStat());

                #endregion



                Session.SendPacket(Session.Character.GenerateSki());
                Session.SendPacket(
                    $"fd {Session.Character.Reputation} 0 {(int)Session.Character.Dignity} {Math.Abs(Session.Character.GetDignityIco())}");
                Session.SendPacket(Session.Character.GenerateFd());
                Session.SendPacket("rage 0 250000");
                Session.SendPacket("rank_cool 0 0 18000");
                ItemInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType(8, InventoryType.Wear);
                Session.SendPacket(Session.Character.GenerateEq());

                StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

                if (Session.Character.HasEreniaMedal())
                {
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOGIN_MEDAL_ERENIA"), 12));
                }

                if (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
                {
                    Session.SendPacket("ib 1278 1");
                }

                if (Session.Character.MapInstance?.Map?.MapTypes?.Any(m => m.MapTypeId == (short)MapTypeEnum.CleftOfDarkness) == true)
                {
                    Session.SendPacket("bc 0 0 0");
                }

                if (specialistInstance != null)
                {
                    Session.SendPacket(Session.Character.GenerateSpPoint());
                }


                Session.SendPacket(Session.Character.GenerateExts());
                Session.SendPacket(Session.Character.GenerateMlinfo());
                Session.SendPacket(UserInterfaceHelper.GeneratePClear());

                Session.SendPacket(Session.Character.GeneratePetskill());

                foreach (var mate in Session.Character.Mates.Where(s => s.IsTeamMember))
                {
                    mate.AddTeamMember(true);
                }

                Session.SendPacket(FamilySystemExtensions.GenerateFmi(Session));
                Session.SendPacket(FamilySystemExtensions.GenerateFmp(Session));
                Session.SendPacket(Session.Character.GeneratePinit());
                Session.SendPackets(Session.Character.GeneratePst());

                Session.SendPacket("zzim");
                Session.SendPacket(
                 $"twk 1 {Session.Character.CharacterId} {Session.Account.Name} {Session.Character.Name} shtmxpdlfeoqkr");

                long? familyId = DAOFactory.FamilyCharacterDAO.LoadByCharacterId(Session.Character.CharacterId)?.FamilyId;
                if (familyId.HasValue)
                {
                    Session.Character.Family = ServerManager.Instance.FamilyList[familyId.Value];
                }

                if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
                {
                    if (Session.Character.Faction != (FactionType)Session.Character.Family.FamilyFaction)
                    {
                        Session.Character.Faction
                            = (FactionType)Session.Character.Family.FamilyFaction;
                    }

                    Session.SendPacket(Session.Character.GenerateGInfo());
                    Session.SendPackets(Session.Character.GetFamilyHistory());
                    Session.SendPacket(Session.Character.GenerateFamilyMember());
                    Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
                    Session.SendPacket(Session.Character.GenerateFamilyMemberExp());
                    Session.SendPacket($"gcon {Session.Character.CharacterId}|1|0");

                    if (!string.IsNullOrWhiteSpace(Session.Character.Family.FamilyMessage))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo("--- Family Message ---\n" +
                                                             Session.Character.Family.FamilyMessage));
                    }
                }

                Session.SendPacket(Session.Character.GetSqst());
                Session.SendPacket("act6");
                Session.SendPacket(Session.Character.GenerateFaction());
                Session.SendPackets(Session.Character.GenerateScP());
                Session.SendPackets(Session.Character.GenerateScN());
#pragma warning disable 618
                Session.Character.GenerateStartupInventory();
#pragma warning restore 618

                Session.SendPacket(Session.Character.GenerateGold());
                Session.SendPackets(Session.Character.GenerateQuicklist());


                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

                Session.SendPacket(Session.Character.GenerateFinit());
                Session.SendPacket(Session.Character.GenerateBlinit());

                //RankingExtension.GenerateComplimentRanking(Session);
                //RankingExtension.GeneratePointRanking(Session);
                RankingExtension.GenerateDuelRanking(Session);
                RankingExtension.GenerateReputationRanking(Session);
                RankingExtension.GenerateMonsterRanking(Session);


                Session.Character.LastPVPRevive = DateTime.Now;

                IEnumerable<PenaltyLogDTO> warning = DAOFactory.PenaltyLogDAO.LoadByAccount(Session.Character.AccountId)
                    .Where(p => p.Penalty == PenaltyType.Warning);
                if (warning.Any())
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        string.Format(Language.Instance.GetMessageFromKey("WARNING_INFO"), warning.Count())));
                }

                // finfo - friends info
                //Session.Character.LoadMail();
                Session.Character.LoadSentMail();
                Session.Character.DeleteTimeout();

                if (Session.Character.Quests.Any())
                {
                    Session.SendPacket(Session.Character.GenerateQuestsPacket());
                }

                if (Session.Character.IsSeal)
                {
                    if (ServerManager.Instance.ChannelId == 51)
                    {
                        Session.Character.SetSeal();
                    }
                    else
                    {
                        Session.Character.IsSeal = false;
                    }
                }
            }
        }
        #endregion
    }
}
