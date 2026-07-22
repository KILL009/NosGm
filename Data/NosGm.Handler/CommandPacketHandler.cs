using NosTale.Packets.Packets.CommandPackets;
using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.CommandPackets;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Log.Networking;
using OpenNos.Log.Shared;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OpenNos.Handler
{
    public class CommandPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CommandPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        /// $Act4 Command
        /// </summary>
        /// <param name="act4Packet"></param>
        public void Act4(Act4Packet act4Packet)
        {
            if (act4Packet != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Act4]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Act4]"
                    });
                }

                if (ServerManager.Instance.IsAct4Online())
                {
                    switch (Session.Character.Faction)
                    {
                        case FactionType.None:
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, 51, 41);
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo("You need to be part of a faction to join Act 4"));
                            return;

                        case FactionType.Angel:
                            Session.Character.MapId = 130;
                            Session.Character.MapX = 12;
                            Session.Character.MapY = 40;
                            break;

                        case FactionType.Demon:
                            Session.Character.MapId = 131;
                            Session.Character.MapX = 12;
                            Session.Character.MapY = 40;
                            break;
                    }

                    Session.Character.ChangeChannel(ServerManager.Instance.Configuration.Act4IP, ServerManager.Instance.Configuration.Act4Port, 1);
                }
                else
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 145, 51, 41);
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ACT4_OFFLINE")));
                }
            }

            Session.Character.GenerateSay(Act4Packet.ReturnHelp(), 10);
        }

        public void Act4Stat(Act4StatPacket packet)
        {
            if (packet != null && ServerManager.Instance.ChannelId == 51)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Act4Stat]Faction: {packet.Faction} Value: {packet.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = packet.OriginalHeader
                    });
                }
                switch (packet.Faction)
                {
                    case 1:
                        ServerManager.Instance.Act4AngelStat.Percentage = packet.Value;
                        break;

                    case 2:
                        ServerManager.Instance.Act4DemonStat.Percentage = packet.Value;
                        break;
                }
                Parallel.ForEach(ServerManager.Instance.Sessions, sess => sess.SendPacket(sess.Character.GenerateFc()));
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Act4StatPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddMonster Command
        /// </summary>
        /// <param name="addMonsterPacket"></param>
        public void AddMonster(AddMonsterPacket addMonsterPacket)
        {
            if (addMonsterPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddMonster]NpcMonsterVNum: {addMonsterPacket.MonsterVNum} IsMoving: {addMonsterPacket.IsMoving}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddMonster]NpcMonsterVNum: {addMonsterPacket.MonsterVNum} IsMoving: {addMonsterPacket.IsMoving}"
                    });
                }

                if (!Session.HasCurrentMapInstance)
                {
                    return;
                }

                NpcMonster npcmonster = ServerManager.GetNpcMonster(addMonsterPacket.MonsterVNum);
                if (npcmonster == null)
                {
                    return;
                }

                MapMonsterDTO monst = new MapMonsterDTO
                {
                    MonsterVNum = addMonsterPacket.MonsterVNum,
                    MapY = Session.Character.PositionY,
                    MapX = Session.Character.PositionX,
                    MapId = Session.Character.MapInstance.Map.MapId,
                    Position = Session.Character.Direction,
                    IsMoving = addMonsterPacket.IsMoving,
                    MapMonsterId = ServerManager.Instance.GetNextMobId()
                };
                if (!DAOFactory.MapMonsterDAO.DoesMonsterExist(monst.MapMonsterId))
                {
                    DAOFactory.MapMonsterDAO.Insert(monst);
                    if (DAOFactory.MapMonsterDAO.LoadById(monst.MapMonsterId) is MapMonsterDTO monsterDTO)
                    {
                        MapMonster monster = new MapMonster(monsterDTO);
                        monster.Initialize(Session.CurrentMapInstance);
                        Session.CurrentMapInstance.AddMonster(monster);
                        Session.CurrentMapInstance?.Broadcast(monster.GenerateIn());
                    }
                }

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddMonsterPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddNpc Command
        /// </summary>
        /// <param name="addNpcPacket"></param>
        public void AddNpc(AddNpcPacket addNpcPacket)
        {
            if (addNpcPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddNpc]NpcMonsterVNum: {addNpcPacket.NpcVNum} IsMoving: {addNpcPacket.IsMoving}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddNpc]NpcMonsterVNum: {addNpcPacket.NpcVNum} IsMoving: {addNpcPacket.IsMoving}"
                    });
                }

                if (!Session.HasCurrentMapInstance)
                {
                    return;
                }

                NpcMonster npcmonster = ServerManager.GetNpcMonster(addNpcPacket.NpcVNum);
                if (npcmonster == null)
                {
                    return;
                }

                MapNpcDTO newNpc = new MapNpcDTO
                {
                    NpcVNum = addNpcPacket.NpcVNum,
                    MapY = Session.Character.PositionY,
                    MapX = Session.Character.PositionX,
                    MapId = Session.Character.MapInstance.Map.MapId,
                    Position = Session.Character.Direction,
                    IsMoving = addNpcPacket.IsMoving,
                    MapNpcId = ServerManager.Instance.GetNextNpcId()
                };
                if (!DAOFactory.MapNpcDAO.DoesNpcExist(newNpc.MapNpcId))
                {
                    DAOFactory.MapNpcDAO.Insert(newNpc);
                    if (DAOFactory.MapNpcDAO.LoadById(newNpc.MapNpcId) is MapNpcDTO npcDTO)
                    {
                        MapNpc npc = new MapNpc(npcDTO);
                        npc.Initialize(Session.CurrentMapInstance);
                        Session.CurrentMapInstance.AddNPC(npc);
                        Session.CurrentMapInstance?.Broadcast(npc.GenerateIn());
                    }
                }

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddNpcPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddPartner Command
        /// </summary>
        /// <param name="addPartnerPacket"></param>
        public void AddPartner(AddPartnerPacket addPartnerPacket)
        {
            if (addPartnerPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddPartner]NpcMonsterVNum: {addPartnerPacket.MonsterVNum} Level: {addPartnerPacket.Level}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddPartner]NpcMonsterVNum: {addPartnerPacket.MonsterVNum} Level: {addPartnerPacket.Level}"
                    });
                }

                AddMate(addPartnerPacket.MonsterVNum, addPartnerPacket.Level, MateType.Partner);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddPartnerPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddPet Command
        /// </summary>
        /// <param name="addPetPacket"></param>
        public void AddPet(AddPetPacket addPetPacket)
        {
            if (addPetPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddPet]NpcMonsterVNum: {addPetPacket.MonsterVNum} Level: {addPetPacket.Level}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddPet]NpcMonsterVNum: {addPetPacket.MonsterVNum} Level: {addPetPacket.Level}"
                    });
                }

                AddMate(addPetPacket.MonsterVNum, addPetPacket.Level, MateType.Pet);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddPartnerPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddPortal Command
        /// </summary>
        /// <param name="addPortalPacket"></param>
        public void AddPortal(AddPortalPacket addPortalPacket)
        {
            if (addPortalPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddPortal]DestinationMapId: {addPortalPacket.DestinationMapId} DestinationMapX: {addPortalPacket.DestinationX} DestinationY: {addPortalPacket.DestinationY}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddPortal]DestinationMapId: {addPortalPacket.DestinationMapId} DestinationMapX: {addPortalPacket.DestinationX} DestinationY: {addPortalPacket.DestinationY}"
                    });
                }

                AddPortal(addPortalPacket.DestinationMapId, addPortalPacket.DestinationX, addPortalPacket.DestinationY,
                    addPortalPacket.PortalType == null ? (short)-1 : (short)addPortalPacket.PortalType, true);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddPortalPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddQuest
        /// </summary>
        /// <param name="addQuestPacket"></param>
        public void AddQuest(AddQuestPacket addQuestPacket)
        {
            if (addQuestPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                                       $"[AddQuest]QuestId: {addQuestPacket.QuestId}");

             if (ServerManager.Instance.Configuration.UseLogService) 
{
             LogServiceClient.Instance.LogPacket(new PacketLogEntry() { Sender =
             Session.Character.Name, SenderId = Session.Character.CharacterId, PacketType =
             LogType.GMCommand, Packet = $"[AddQuest]QuestId: {addQuestPacket.QuestId}" }); }

             if (ServerManager.Instance.Quests.Any(q => q.QuestId == addQuestPacket.QuestId)) {
             Session.Character.AddQuest(addQuestPacket.QuestId, false); return; }

                Session.SendPacket(Session.Character.GenerateSay("This Quest doesn't exist", 11));
            }
        }

        /// <summary>
        /// $AddShellEffect Command
        /// </summary>
        /// <param name="addShellEffectPacket"></param>
        public void AddShellEffect(AddShellEffectPacket addShellEffectPacket)
        {
            if (addShellEffectPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddShellEffect]Slot: {addShellEffectPacket.Slot} EffectLevel: {addShellEffectPacket.EffectLevel} Effect: {addShellEffectPacket.Effect} Value: {addShellEffectPacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddShellEffect]Slot: {addShellEffectPacket.Slot} EffectLevel: {addShellEffectPacket.EffectLevel} Effect: {addShellEffectPacket.Effect} Value: {addShellEffectPacket.Value}"
                    });
                }

                try
                {
                    ItemInstance instance =
                        Session.Character.Inventory.LoadBySlotAndType(addShellEffectPacket.Slot,
                            InventoryType.Equipment);
                    if (instance != null)
                    {
                        instance.ShellEffects.Add(new ShellEffectDTO
                        {
                            EffectLevel = (ShellEffectLevelType)addShellEffectPacket.EffectLevel,
                            Effect = addShellEffectPacket.Effect,
                            Value = addShellEffectPacket.Value,
                            EquipmentSerialId = instance.EquipmentSerialId
                        });
                    }
                }
                catch (Exception)
                {
                    Session.SendPacket(Session.Character.GenerateSay(AddShellEffectPacket.ReturnHelp(), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddShellEffectPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $AddSkill Command
        /// </summary>
        /// <param name="addSkillPacket"></param>
        public void AddSkill(AddSkillPacket addSkillPacket)
        {
            if (addSkillPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[AddSkill]SkillVNum: {addSkillPacket.SkillVNum}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[AddSkill]SkillVNum: {addSkillPacket.SkillVNum}"
                    });
                }

                Session.Character.AddSkill(addSkillPacket.SkillVNum);
                Session.SendPacket(Session.Character.GenerateSki());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddSkillPacket.ReturnHelp(), 10));
            }
        }

        public void AddUserLog(AddUserLogPacket addUserLogPacket)
        {
            if (addUserLogPacket == null
                || string.IsNullOrEmpty(addUserLogPacket.Username))
            {
                return;
            }

            ClientSession.UserLog.Add(addUserLogPacket.Username);

            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $ArenaWinner Command
        /// </summary>
        /// <param name="arenaWinner"></param>
        public void ArenaWinner(ArenaWinnerPacket arenaWinner)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[ArenaWinner]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[ArenaWinner]"
                });
            }

            Session.Character.ArenaWinner = Session.Character.ArenaWinner == 0 ? 1 : 0;
            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $AddMonster Command
        /// </summary>
        /// <param name="backMobPacket"></param>
        public void BackMob(BackMobPacket backMobPacket)
        {
            if (backMobPacket != null)
            {
                if (!Session.HasCurrentMapInstance)
                {
                    return;
                }

                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[BackMob]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[BackMob]"
                    });
                }

                object lastObject = Session.CurrentMapInstance.RemovedMobNpcList.LastOrDefault();

                if (lastObject is MapMonster mapMonster)
                {
                    MapMonsterDTO backMonst = new MapMonsterDTO
                    {
                        MonsterVNum = mapMonster.MonsterVNum,
                        MapX = mapMonster.MapX,
                        MapY = mapMonster.MapY,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        Position = Session.Character.Direction,
                        IsMoving = mapMonster.IsMoving,
                        MapMonsterId = ServerManager.Instance.GetNextMobId()
                    };
                    if (!DAOFactory.MapMonsterDAO.DoesMonsterExist(backMonst.MapMonsterId))
                    {
                        DAOFactory.MapMonsterDAO.Insert(backMonst);
                        if (DAOFactory.MapMonsterDAO.LoadById(backMonst.MapMonsterId) is MapMonsterDTO monsterDTO)
                        {
                            MapMonster monster = new MapMonster(monsterDTO);
                            monster.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddMonster(monster);
                            Session.CurrentMapInstance?.Broadcast(monster.GenerateIn());
                            Session.CurrentMapInstance.RemovedMobNpcList.Remove(mapMonster);
                            Session.SendPacket(Session.Character.GenerateSay($"MapMonster VNum: {backMonst.MonsterVNum} recovered sucessfully", 10));
                        }
                    }
                }
                else if (lastObject is MapNpc mapNpc)
                {
                    MapNpcDTO backNpc = new MapNpcDTO
                    {
                        NpcVNum = mapNpc.NpcVNum,
                        MapX = mapNpc.MapX,
                        MapY = mapNpc.MapY,
                        MapId = Session.Character.MapInstance.Map.MapId,
                        Position = Session.Character.Direction,
                        IsMoving = mapNpc.IsMoving,
                        MapNpcId = ServerManager.Instance.GetNextMobId()
                    };
                    if (!DAOFactory.MapNpcDAO.DoesNpcExist(backNpc.MapNpcId))
                    {
                        DAOFactory.MapNpcDAO.Insert(backNpc);
                        if (DAOFactory.MapNpcDAO.LoadById(backNpc.MapNpcId) is MapNpcDTO npcDTO)
                        {
                            MapNpc npc = new MapNpc(npcDTO);
                            npc.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddNPC(npc);
                            Session.CurrentMapInstance?.Broadcast(npc.GenerateIn());
                            Session.CurrentMapInstance.RemovedMobNpcList.Remove(mapNpc);
                            Session.SendPacket(Session.Character.GenerateSay($"MapNpc VNum: {backNpc.NpcVNum} recovered sucessfully", 10));
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BackMobPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Ban Command
        /// </summary>
        /// <param name="banPacket"></param>
        public void Ban(BanPacket banPacket)
        {
            if (banPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Ban]CharacterName: {banPacket.CharacterName} Reason: {banPacket.Reason} Until: {(banPacket.Duration == 0 ? DateTime.Now.AddYears(15) : DateTime.Now.AddDays(banPacket.Duration))}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Ban]CharacterName: {banPacket.CharacterName} Reason: {banPacket.Reason} Until: {(banPacket.Duration == 0 ? DateTime.Now.AddYears(15) : DateTime.Now.AddDays(banPacket.Duration))}"
                    });
                }

                BanMethod(banPacket.CharacterName, banPacket.Duration, banPacket.Reason);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BanPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Bank Command
        /// </summary>
        /// <param name="bankPacket"></param>
        /*public void BankManagement(BankPacket bankPacket)
        {
            if (Session.Account.IsLimited)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("LIMITED_ACCOUNT")));
                return;
            }

            if (bankPacket != null)
            {
                switch (bankPacket.Mode?.ToLower())
                {
                    case "balance":
                        {
                            Logger.LogEvent("BANK",
                                $"[{Session.GenerateIdentity()}][Balance]Balance: {Session.Character.GoldBank}");

                            if (ServerManager.Instance.Configuration.UseLogService)
                            {
                                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                {
                                    Sender = Session.Character.Name,
                                    SenderId = Session.Character.CharacterId,
                                    PacketType = LogType.UserCommand,
                                    Packet = $"[{Session.GenerateIdentity()}][Balance]Balance: {Session.Character.GoldBank}"
                                });
                            }

                            Session.SendPacket(
                                Session.Character.GenerateSay($"Current Balance: {Session.Character.GoldBank} Gold.", 10));
                            return;
                        }
                    case "deposit":
                        {
                            if (bankPacket.Param1 != null
                                && (long.TryParse(bankPacket.Param1, out long amount) || string.Equals(bankPacket.Param1,
                                     "all", StringComparison.OrdinalIgnoreCase)))
                            {
                                if (string.Equals(bankPacket.Param1, "all", StringComparison.OrdinalIgnoreCase)
                                    && Session.Character.Gold > 0)
                                {
                                    Logger.LogEvent("BANK",
                                        $"[{Session.GenerateIdentity()}][Deposit]Amount: {Session.Character.Gold} OldBank: {Session.Character.GoldBank} NewBank: {Session.Character.GoldBank + Session.Character.Gold}");

                                    if (ServerManager.Instance.Configuration.UseLogService)
                                    {
                                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                        {
                                            Sender = Session.Character.Name,
                                            SenderId = Session.Character.CharacterId,
                                            PacketType = LogType.UserCommand,
                                            Packet = $"[{Session.GenerateIdentity()}][Deposit]Amount: {Session.Character.Gold} OldBank: {Session.Character.GoldBank} NewBank: {Session.Character.GoldBank + Session.Character.Gold}"
                                        });
                                    }

                                    Session.SendPacket(
                                        Session.Character.GenerateSay($"Deposited ALL({Session.Character.Gold}) Gold.",
                                            10));
                                    Session.Character.GoldBank += Session.Character.Gold;
                                    Session.Character.Gold = 0;
                                    Session.SendPacket(Session.Character.GenerateGold());
                                    Session.SendPacket(
                                        Session.Character.GenerateSay($"New Balance: {Session.Character.GoldBank} Gold.",
                                            10));
                                }
                                else if (amount <= Session.Character.Gold && Session.Character.Gold > 0)
                                {
                                    if (amount < 1)
                                    {
                                        Logger.LogEvent("BANK",
                                            $"[{Session.GenerateIdentity()}][Illegal]Mode: {bankPacket.Mode} Param1: {bankPacket.Param1} Param2: {bankPacket.Param2}");

                                        if (ServerManager.Instance.Configuration.UseLogService)
                                        {
                                            LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                            {
                                                Sender = Session.Character.Name,
                                                SenderId = Session.Character.CharacterId,
                                                PacketType = LogType.UserCommand,
                                                Packet = $"[{Session.GenerateIdentity()}][Illegal]Mode: {bankPacket.Mode} Param1: {bankPacket.Param1} Param2: {bankPacket.Param2}"
                                            });
                                        }

                                        Session.SendPacket(Session.Character.GenerateSay(
                                            "I'm afraid I can't let you do that. This incident has been logged.", 10));
                                    }
                                    else
                                    {
                                        Logger.LogEvent("BANK",
                                            $"[{Session.GenerateIdentity()}][Deposit]Amount: {amount} OldBank: {Session.Character.GoldBank} NewBank: {Session.Character.GoldBank + amount}");

                                        if (ServerManager.Instance.Configuration.UseLogService)
                                        {
                                            LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                            {
                                                Sender = Session.Character.Name,
                                                SenderId = Session.Character.CharacterId,
                                                PacketType = LogType.UserCommand,
                                                Packet = $"[{Session.GenerateIdentity()}][Deposit]Amount: {amount} OldBank: {Session.Character.GoldBank} NewBank: {Session.Character.GoldBank + amount}"
                                            });
                                        }

                                        Session.SendPacket(Session.Character.GenerateSay($"Deposited {amount} Gold.", 10));
                                        Session.Character.GoldBank += amount;
                                        Session.Character.Gold -= amount;
                                        Session.SendPacket(Session.Character.GenerateGold());
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                $"New Balance: {Session.Character.GoldBank} Gold.", 10));
                                    }
                                }
                            }

                            return;
                        }
                    case "withdraw":
                        {
                            if (bankPacket.Param1 != null && long.TryParse(bankPacket.Param1, out long amount)
                                && amount <= Session.Character.GoldBank && Session.Character.GoldBank > 0
                                && (Session.Character.Gold + amount) <= ServerManager.Instance.Configuration.MaxGold)
                            {
                                if (amount < 1)
                                {
                                    Logger.LogEvent("BANK",
                                        $"[{Session.GenerateIdentity()}][Illegal]Mode: {bankPacket.Mode} Param1: {bankPacket.Param1} Param2: {bankPacket.Param2}");

                                    if (ServerManager.Instance.Configuration.UseLogService)
                                    {
                                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                        {
                                            Sender = Session.Character.Name,
                                            SenderId = Session.Character.CharacterId,
                                            PacketType = LogType.UserCommand,
                                            Packet = $"[{Session.GenerateIdentity()}][Illegal]Mode: {bankPacket.Mode} Param1: {bankPacket.Param1} Param2: {bankPacket.Param2}"
                                        });
                                    }

                                    Session.SendPacket(Session.Character.GenerateSay(
                                        "I'm afraid I can't let you do that. This incident has been logged.", 10));
                                }
                                else
                                {
                                    Logger.LogEvent("BANK",
                                        $"[{Session.GenerateIdentity()}][Withdraw]Amount: {amount} OldBank: {Session.Character.GoldBank} NewBank: {Session.Character.GoldBank - amount}");

                                    if (ServerManager.Instance.Configuration.UseLogService)
                                    {
                                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                        {
                                            Sender = Session.Character.Name,
                                            SenderId = Session.Character.CharacterId,
                                            PacketType = LogType.UserCommand,
                                            Packet = $"[{Session.GenerateIdentity()}][Withdraw]Amount: {amount} OldBank: {Session.Character.GoldBank} NewBank: {Session.Character.GoldBank - amount}"
                                        });
                                    }

                                    Session.SendPacket(Session.Character.GenerateSay($"Withdrawn {amount} Gold.", 10));
                                    Session.Character.GoldBank -= amount;
                                    Session.Character.Gold += amount;
                                    Session.SendPacket(Session.Character.GenerateGold());
                                    Session.SendPacket(
                                        Session.Character.GenerateSay($"New Balance: {Session.Character.GoldBank} Gold.",
                                            10));
                                }
                            }

                            return;
                        }
                    case "send":
                        {
                            if (bankPacket.Param1 != null)
                            {
                                long amount = bankPacket.Param2;
                                ClientSession receiver =
                                    ServerManager.Instance.GetSessionByCharacterName(bankPacket.Param1);
                                if (amount <= Session.Character.GoldBank && Session.Character.GoldBank > 0
                                    && receiver != null)
                                {
                                    if (amount < 1)
                                    {
                                        Logger.LogEvent("BANK",
                                            $"[{Session.GenerateIdentity()}][Illegal]Mode: {bankPacket.Mode} Param1: {bankPacket.Param1} Param2: {bankPacket.Param2}");

                                        if (ServerManager.Instance.Configuration.UseLogService)
                                        {
                                            LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                            {
                                                Sender = Session.Character.Name,
                                                SenderId = Session.Character.CharacterId,
                                                PacketType = LogType.UserCommand,
                                                Packet = $"[{Session.GenerateIdentity()}][Illegal]Mode: {bankPacket.Mode} Param1: {bankPacket.Param1} Param2: {bankPacket.Param2}"
                                            });
                                        }

                                        Session.SendPacket(Session.Character.GenerateSay(
                                            "I'm afraid I can't let you do that. This incident has been logged.", 10));
                                    }
                                    else
                                    {
                                        Logger.LogEvent("BANK",
                                            $"[{Session.GenerateIdentity()}][Send]Amount: {amount} OldBankSender: {Session.Character.GoldBank} NewBankSender: {Session.Character.GoldBank - amount} OldBankReceiver: {receiver.Character.GoldBank} NewBankReceiver: {receiver.Character.GoldBank + amount}");

                                        if (ServerManager.Instance.Configuration.UseLogService)
                                        {
                                            LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                            {
                                                Sender = Session.Character.Name,
                                                SenderId = Session.Character.CharacterId,
                                                PacketType = LogType.UserCommand,
                                                Packet = $"[{Session.GenerateIdentity()}][Send]Amount: {amount} OldBankSender: {Session.Character.GoldBank} NewBankSender: {Session.Character.GoldBank - amount} OldBankReceiver: {receiver.Character.GoldBank} NewBankReceiver: {receiver.Character.GoldBank + amount}"
                                            });
                                        }

                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                $"Sent {amount} Gold to {receiver.Character.Name}", 10));
                                        receiver.SendPacket(
                                            Session.Character.GenerateSay(
                                                $"Received {amount} Gold from {Session.Character.Name}", 10));
                                        Session.Character.GoldBank -= amount;
                                        receiver.Character.GoldBank += amount;
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                $"New Balance: {Session.Character.GoldBank} Gold.", 10));
                                        receiver.SendPacket(
                                            Session.Character.GenerateSay(
                                                $"New Balance: {receiver.Character.GoldBank} Gold.", 10));
                                    }
                                }
                            }

                            return;
                        }
                    default:
                        {
                            Session.SendPacket(Session.Character.GenerateSay(BankPacket.ReturnHelp(), 10));
                            return;
                        }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BankPacket.ReturnHelp(), 10));
            }
        }*/

        /// <summary>
        /// $BlockExp Command
        /// </summary>
        /// <param name="blockExpPacket"></param>
        public void BlockExp(BlockExpPacket blockExpPacket)
        {
            if (blockExpPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[BlockExp]CharacterName: {blockExpPacket.CharacterName} Reason: {blockExpPacket.Reason} Until: {DateTime.Now.AddMinutes(blockExpPacket.Duration)}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[BlockExp]CharacterName: {blockExpPacket.CharacterName} Reason: {blockExpPacket.Reason} Until: {DateTime.Now.AddMinutes(blockExpPacket.Duration)}"
                    });
                }

                if (blockExpPacket.Duration == 0)
                {
                    blockExpPacket.Duration = 60;
                }

                blockExpPacket.Reason = blockExpPacket.Reason?.Trim();
                CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(blockExpPacket.CharacterName);
                if (character != null)
                {
                    ClientSession session =
                        ServerManager.Instance.Sessions.FirstOrDefault(s =>
                            s.Character?.Name == blockExpPacket.CharacterName);
                    session?.SendPacket(blockExpPacket.Duration == 1
                        ? UserInterfaceHelper.GenerateInfo(
                            string.Format(Language.Instance.GetMessageFromKey("MUTED_SINGULAR"), blockExpPacket.Reason))
                        : UserInterfaceHelper.GenerateInfo(string.Format(
                            Language.Instance.GetMessageFromKey("MUTED_PLURAL"), blockExpPacket.Reason,
                            blockExpPacket.Duration)));
                    PenaltyLogDTO log = new PenaltyLogDTO
                    {
                        AccountId = character.AccountId,
                        Reason = blockExpPacket.Reason,
                        Penalty = PenaltyType.BlockExp,
                        DateStart = DateTime.Now,
                        DateEnd = DateTime.Now.AddMinutes(blockExpPacket.Duration),
                        AdminName = Session.Character.Name
                    };
                    Character.InsertOrUpdatePenalty(log);
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BlockExpPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $BlockFExp Command
        /// </summary>
        /// <param name="blockFExpPacket"></param>
        public void BlockFExp(BlockFExpPacket blockFExpPacket)
        {
            if (blockFExpPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[BlockFExp]CharacterName: {blockFExpPacket.CharacterName} Reason: {blockFExpPacket.Reason} Until: {DateTime.Now.AddMinutes(blockFExpPacket.Duration)}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[BlockFExp]CharacterName: {blockFExpPacket.CharacterName} Reason: {blockFExpPacket.Reason} Until: {DateTime.Now.AddMinutes(blockFExpPacket.Duration)}"
                    });
                }

                if (blockFExpPacket.Duration == 0)
                {
                    blockFExpPacket.Duration = 60;
                }

                blockFExpPacket.Reason = blockFExpPacket.Reason?.Trim();
                CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(blockFExpPacket.CharacterName);
                if (character != null)
                {
                    ClientSession session =
                        ServerManager.Instance.Sessions.FirstOrDefault(s =>
                            s.Character?.Name == blockFExpPacket.CharacterName);
                    session?.SendPacket(blockFExpPacket.Duration == 1
                        ? UserInterfaceHelper.GenerateInfo(
                            string.Format(Language.Instance.GetMessageFromKey("MUTED_SINGULAR"),
                                blockFExpPacket.Reason))
                        : UserInterfaceHelper.GenerateInfo(string.Format(
                            Language.Instance.GetMessageFromKey("MUTED_PLURAL"), blockFExpPacket.Reason,
                            blockFExpPacket.Duration)));
                    PenaltyLogDTO log = new PenaltyLogDTO
                    {
                        AccountId = character.AccountId,
                        Reason = blockFExpPacket.Reason,
                        Penalty = PenaltyType.BlockFExp,
                        DateStart = DateTime.Now,
                        DateEnd = DateTime.Now.AddMinutes(blockFExpPacket.Duration),
                        AdminName = Session.Character.Name
                    };
                    Character.InsertOrUpdatePenalty(log);
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BlockFExpPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $BlockPM Command
        /// </summary>
        /// <param name="blockPmPacket"></param>
        public void BlockPm(BlockPMPacket blockPmPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[BlockPM]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[BlockPM]"
                });
            }

            if (!Session.Character.GmPvtBlock)
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GM_BLOCK_ENABLE"),
                    10));
                Session.Character.GmPvtBlock = true;
            }
            else
            {
                Session.SendPacket(
                    Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GM_BLOCK_DISABLE"), 10));
                Session.Character.GmPvtBlock = false;
            }
        }

        /// <summary>
        /// $BlockRep Command
        /// </summary>
        /// <param name="blockRepPacket"></param>
        public void BlockRep(BlockRepPacket blockRepPacket)
        {
            if (blockRepPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[BlockRep]CharacterName: {blockRepPacket.CharacterName} Reason: {blockRepPacket.Reason} Until: {DateTime.Now.AddMinutes(blockRepPacket.Duration)}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[BlockRep]CharacterName: {blockRepPacket.CharacterName} Reason: {blockRepPacket.Reason} Until: {DateTime.Now.AddMinutes(blockRepPacket.Duration)}"
                    });
                }

                if (blockRepPacket.Duration == 0)
                {
                    blockRepPacket.Duration = 60;
                }

                blockRepPacket.Reason = blockRepPacket.Reason?.Trim();
                CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(blockRepPacket.CharacterName);
                if (character != null)
                {
                    ClientSession session =
                        ServerManager.Instance.Sessions.FirstOrDefault(s =>
                            s.Character?.Name == blockRepPacket.CharacterName);
                    session?.SendPacket(blockRepPacket.Duration == 1
                        ? UserInterfaceHelper.GenerateInfo(
                            string.Format(Language.Instance.GetMessageFromKey("MUTED_SINGULAR"), blockRepPacket.Reason))
                        : UserInterfaceHelper.GenerateInfo(string.Format(
                            Language.Instance.GetMessageFromKey("MUTED_PLURAL"), blockRepPacket.Reason,
                            blockRepPacket.Duration)));
                    PenaltyLogDTO log = new PenaltyLogDTO
                    {
                        AccountId = character.AccountId,
                        Reason = blockRepPacket.Reason,
                        Penalty = PenaltyType.BlockRep,
                        DateStart = DateTime.Now,
                        DateEnd = DateTime.Now.AddMinutes(blockRepPacket.Duration),
                        AdminName = Session.Character.Name
                    };
                    Character.InsertOrUpdatePenalty(log);
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BlockRepPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Buff packet
        /// </summary>
        /// <param name="buffPacket"></param>
        public void Buff(BuffPacket buffPacket)
        {
            if (buffPacket != null)
            {
                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Buff]CardId: {buffPacket.CardId} Level: {buffPacket.Level}"
                    });
                }

                Buff buff = new Buff(buffPacket.CardId, buffPacket.Level ?? (byte)1);
                Session.Character.AddBuff(buff, Session.Character.BattleEntity);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BuffPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ChangeClass Command
        /// </summary>
        /// <param name="changeClassPacket"></param>
        public void ChangeClass(ChangeClassPacket changeClassPacket)
        {
            if (changeClassPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[ChangeClass]Class: {changeClassPacket.ClassType}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ChangeClass]Class: {changeClassPacket.ClassType}"
                    });
                }
                Session.Character.ChangeClass(changeClassPacket.ClassType, true);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeClassPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ChangeDignity Command
        /// </summary>
        /// <param name="changeDignityPacket"></param>
        public void ChangeDignity(ChangeDignityPacket changeDignityPacket)
        {
            if (changeDignityPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[ChangeDignity]Dignity: {changeDignityPacket.Dignity}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ChangeDignity]Dignity: {changeDignityPacket.Dignity}"
                    });
                }

                if (changeDignityPacket.Dignity >= -1000 && changeDignityPacket.Dignity <= 100)
                {
                    Session.Character.Dignity = changeDignityPacket.Dignity;
                    Session.SendPacket(Session.Character.GenerateFd());
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("DIGNITY_CHANGED"), 12));
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_DIGNITY"), 11));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeDignityPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $FLvl Command
        /// </summary>
        /// <param name="changeFairyLevelPacket"></param>
        public void ChangeFairyLevel(ChangeFairyLevelPacket changeFairyLevelPacket)
        {
            ItemInstance fairy =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (changeFairyLevelPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[FLvl]FairyLevel: {changeFairyLevelPacket.FairyLevel}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[FLvl]FairyLevel: {changeFairyLevelPacket.FairyLevel}"
                    });
                }

                if (fairy != null)
                {
                    short fairylevel = changeFairyLevelPacket.FairyLevel;
                    fairylevel -= fairy.Item.ElementRate;
                    fairy.ElementRate = fairylevel;
                    fairy.XP = 0;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("FAIRY_LEVEL_CHANGED"), fairy.Item.Name),
                        10));
                    Session.SendPacket(Session.Character.GeneratePairy());
                }
                else
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAIRY"),
                        10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeFairyLevelPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ChangeSex Command
        /// </summary>
        /// <param name="changeSexPacket"></param>
        public void ChangeGender(ChangeSexPacket changeSexPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[ChangeSex]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[ChangeSex]"
                });
            }

            Session.Character.ChangeSex();
        }

        /// <summary>
        /// $HeroLvl Command
        /// </summary>
        /// <param name="changeHeroLevelPacket"></param>
        public void ChangeHeroLevel(ChangeHeroLevelPacket changeHeroLevelPacket)
        {
            if (changeHeroLevelPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[HeroLvl]HeroLevel: {changeHeroLevelPacket.HeroLevel}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[HeroLvl]HeroLevel: {changeHeroLevelPacket.HeroLevel}"
                    });
                }

                if (changeHeroLevelPacket.HeroLevel <= 255)
                {
                    Session.Character.HeroLevel = changeHeroLevelPacket.HeroLevel;
                    Session.Character.HeroXp = 0;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HEROLEVEL_CHANGED"), 0));
                    Session.SendPacket(Session.Character.GenerateLev());
                    Session.SendPackets(Session.Character.GenerateStatChar());
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(),
                        ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(),
                        ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 6),
                        Session.Character.PositionX, Session.Character.PositionY);
                    Session.CurrentMapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 198),
                        Session.Character.PositionX, Session.Character.PositionY);
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeHeroLevelPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $JLvl Command
        /// </summary>
        /// <param name="changeJobLevelPacket"></param>
        public void ChangeJobLevel(ChangeJobLevelPacket changeJobLevelPacket)
        {
            if (changeJobLevelPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[JLvl]JobLevel: {changeJobLevelPacket.JobLevel}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[JLvl] JobLevel: {changeJobLevelPacket.JobLevel}"
                    });
                }

                if (((Session.Character.Class == 0 && changeJobLevelPacket.JobLevel <= 20)
                    || (Session.Character.Class != 0 && changeJobLevelPacket.JobLevel <= 255))
                    && changeJobLevelPacket.JobLevel > 0)
                {
                    Session.Character.JobLevel = changeJobLevelPacket.JobLevel;
                    Session.Character.JobLevelXp = 0;
                    Session.Character.ResetSkills();
                    Session.SendPacket(Session.Character.GenerateLev());
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("JOBLEVEL_CHANGED"), 0));
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(), ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 8), Session.Character.PositionX, Session.Character.PositionY);
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeJobLevelPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Lvl Command
        /// </summary>
        /// <param name="changeLevelPacket"></param>
        public void ChangeLevel(ChangeLevelPacket changeLevelPacket)
        {
            if (changeLevelPacket != null && !Session.Character.IsSeal)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Lvl]Level: {changeLevelPacket.Level}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Lvl]Level: {changeLevelPacket.Level}"
                    });
                }

                if (changeLevelPacket.Level > 0)
                {
                    Session.Character.Level = Math.Min(changeLevelPacket.Level,
                        ServerManager.Instance.Configuration.MaxLevel);
                    Session.Character.LevelXp = 0;
                    Session.Character.Hp = (int)Session.Character.HPLoad();
                    Session.Character.Mp = (int)Session.Character.MPLoad();
                    Session.SendPacket(Session.Character.GenerateStat());
                    Session.SendPackets(Session.Character.GenerateStatChar());
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LEVEL_CHANGED"), 0));
                    Session.SendPacket(Session.Character.GenerateLev());
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(),
                        ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(),
                        ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 6),
                        Session.Character.PositionX, Session.Character.PositionY);
                    Session.CurrentMapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 198),
                        Session.Character.PositionX, Session.Character.PositionY);
                    ServerManager.Instance.UpdateGroup(Session.Character.CharacterId);
                    if (Session.Character.Family != null)
                    {
                        ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                        CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                        {
                            DestinationCharacterId = Session.Character.Family.FamilyId,
                            SourceCharacterId = Session.Character.CharacterId,
                            SourceWorldId = ServerManager.Instance.WorldId,
                            Message = "fhis_stc",
                            Type = MessageType.Family
                        });
                    }
                    Session.Character.LevelRewards(Session.Character.Level);
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeLevelPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ChangeRep Command
        /// </summary>
        /// <param name="changeReputationPacket"></param>
        public void ChangeReputation(ChangeReputationPacket changeReputationPacket)
        {
            if (changeReputationPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[ChangeRep]Reputation: {changeReputationPacket.Reputation}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ChangeRep]Reputation: {changeReputationPacket.Reputation}"
                    });
                }

                if (changeReputationPacket.Reputation > 0)
                {
                    Session.Character.Reputation = changeReputationPacket.Reputation;
                    Session.SendPacket(Session.Character.GenerateFd());
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REP_CHANGED"), 0));
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeReputationPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ChangeShopName Packet
        /// </summary>
        /// <param name="changeShopNamePacket"></param>
        public void ChangeShopName(ChangeShopNamePacket changeShopNamePacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                if (!string.IsNullOrEmpty(changeShopNamePacket.Name))
                {
                    if (Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId) is MapNpc npc)
                    {
                        if (npc.Shop is Shop shop)
                        {
                            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                                $"[ChangeShopName]ShopId: {shop.ShopId} Name: {changeShopNamePacket.Name}");

                            if (ServerManager.Instance.Configuration.UseLogService)
                            {
                                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                                {
                                    Sender = Session.Character.Name,
                                    SenderId = Session.Character.CharacterId,
                                    PacketType = LogType.GMCommand,
                                    Packet = $"[ChangeShopName]ShopId: {shop.ShopId} Name: {changeShopNamePacket.Name}"
                                });
                            }

                            if (DAOFactory.ShopDAO.LoadById(shop.ShopId) is ShopDTO shopDTO)
                            {
                                shop.Name = changeShopNamePacket.Name;
                                shopDTO.Name = changeShopNamePacket.Name;
                                DAOFactory.ShopDAO.Update(ref shopDTO);

                                Session.CurrentMapInstance.Broadcast($"shop 2 {npc.MapNpcId} {npc.Shop.ShopId} {npc.Shop.MenuType} {npc.Shop.ShopType} {npc.Shop.Name}");
                            }
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NPCMONSTER_NOT_FOUND"), 11));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(ChangeShopNamePacket.ReturnHelp(), 10));
                }
            }
        }

        /// <summary>
        /// $SPLvl Command
        /// </summary>
        /// <param name="changeSpecialistLevelPacket"></param>
        public void ChangeSpecialistLevel(ChangeSpecialistLevelPacket changeSpecialistLevelPacket)
        {
            if (changeSpecialistLevelPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[SPLvl]SpecialistLevel: {changeSpecialistLevelPacket.SpecialistLevel}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[SPLvl]SpecialistLevel: {changeSpecialistLevelPacket.SpecialistLevel}"
                    });
                }

                ItemInstance sp =
                    Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                if (sp != null && Session.Character.UseSp)
                {
                    if (changeSpecialistLevelPacket.SpecialistLevel <= 255
                        && changeSpecialistLevelPacket.SpecialistLevel > 0)
                    {
                        sp.SpLevel = changeSpecialistLevelPacket.SpecialistLevel;
                        sp.XP = 0;
                        Session.SendPacket(Session.Character.GenerateLev());
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SPLEVEL_CHANGED"), 0));
                        Session.Character.LearnSPSkill();
                        Session.SendPacket(Session.Character.GenerateSki());
                        Session.SendPackets(Session.Character.GenerateQuicklist());
                        Session.Character.SkillsSp.ForEach(s => s.LastUse = DateTime.Now.AddDays(-1));
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(InEffect: 1),
                            ReceiverType.AllExceptMe);
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(),
                            ReceiverType.AllExceptMe);
                        Session.CurrentMapInstance?.Broadcast(
                            StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 8),
                            Session.Character.PositionX, Session.Character.PositionY);
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                    }
                }
                else
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"),
                        0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ChangeSpecialistLevelPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ChannelInfo Command
        /// </summary>
        /// <param name="channelInfoPacket"></param>
        public void ChannelInfo(ChannelInfoPacket channelInfoPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[ChannelInfo]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[ChannelInfo]"
                });
            }

            Session.SendPacket(Session.Character.GenerateSay(
                $"-----------Channel Info-----------\n-------------Channel:{ServerManager.Instance.ChannelId}-------------",
                11));
            foreach (ClientSession session in ServerManager.Instance.Sessions)
            {
                Session.SendPacket(
                    Session.Character.GenerateSay(
                        $"CharacterName: {session.Character.Name} | CharacterId: {session.Character.CharacterId} | SessionId: {session.SessionId}", 12));
            }

            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
        }

        /// <summary>
        /// $CharEdit Command
        /// </summary>
        /// <param name="characterEditPacket"></param>
        public void CharacterEdit(CharacterEditPacket characterEditPacket)
        {
            if (characterEditPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[CharEdit]Property: {characterEditPacket.Property} Value: {characterEditPacket.Data}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[CharEdit]Property: {characterEditPacket.Property} Value: {characterEditPacket.Data}"
                    });
                }

                if (characterEditPacket.Property != null && !string.IsNullOrEmpty(characterEditPacket.Data))
                {
                    PropertyInfo propertyInfo = Session.Character.GetType().GetProperty(characterEditPacket.Property);
                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(Session.Character,
                            Convert.ChangeType(characterEditPacket.Data, propertyInfo.PropertyType));
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId);
                        Session.Character.Save();
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"),
                            10));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(CharacterEditPacket.ReturnHelp(), 10));
            }
        }

        

        

        /// <summary>
        /// $Guide Command
        /// </summary>
        /// <param name="guidePacket"></param>
        public void Guide(GuidePacket guidePacket)
        {

            DateTime LastCommand = Session.Character.LastCommand;
            DateTime time = Session.Character.LastCommand.AddSeconds(10);

            if (DateTime.Now <= time) // Anti spam $Roll
            {
                return;
            }
            Session.Character.LastCommand = DateTime.Now;

            Session.SendPacket(Session.Character.GenerateSay("-------------Things you should know-------------", 10));
            Session.SendPacket(Session.Character.GenerateSay($"1) Follow the level progression by going on the right level-gap maps", 12));
            Session.SendPacket(Session.Character.GenerateSay($"2) You will get free rewards every 10 levels starting from 10 to 99", 12));
            Session.SendPacket(Session.Character.GenerateSay($"3) Act.4 is the best way to get reputation points!", 12));
            Session.SendPacket(Session.Character.GenerateSay($"4) Every raid contains special items, don't skip them!", 12));
            Session.SendPacket(Session.Character.GenerateSay($"5) Don't miss events like Meteorite Game, Solo IC, Talent Arena.", 12));
            Session.SendPacket(Session.Character.GenerateSay($"5) You can farm R7 PvP shells in Yertirand or Act.4 raids.", 12));
            Session.SendPacket(Session.Character.GenerateSay($"Last Update: 25/09/2019 by Wentworth", 12));
            Session.SendPacket(Session.Character.GenerateSay("-------------------------------------------", 10));
        }


        /// <summary>
        /// $Schedule Command
        /// </summary>
        /// <param name="schedulePacket"></param>
        public void Schedule(SchedulePacket schedulePacket)
        {

            DateTime LastSchedule = Session.Character.LastSchedule;
            DateTime time = Session.Character.LastSchedule.AddSeconds(10);

            if (DateTime.Now <= time) // Anti spam $Roll
            {
                return;
            }
            Session.Character.LastSchedule = DateTime.Now;


            Session.SendPacket(Session.Character.GenerateSay("---------Event List---------", 10));
            Session.SendPacket(Session.Character.GenerateSay($"LOD: 17:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"LOD: 19:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"LOD: 21:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"LOD: 23:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"METEORITE GAME: 18:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"METEORITE GAME: 20:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"METEORITE GAME: 22:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"TALENTARENA: 15:00 ", 12));
            Session.SendPacket(Session.Character.GenerateSay($"TALENTARENA: 18:30 ", 12));
            Session.SendPacket(Session.Character.GenerateSay($"SOLO INSTANTBATTLE: 16:30 && 19:30", 12));
            Session.SendPacket(Session.Character.GenerateSay($"ICEBREAKER = TO DEFINE", 12));
            Session.SendPacket(Session.Character.GenerateSay($"CALIGOR: 17:00", 12));
            Session.SendPacket(Session.Character.GenerateSay($"CALIGOR: 21:00", 12));
            Session.SendPacket(Session.Character.GenerateSay("---------------------------------", 10));
        }

        public void Roll(RollPacket pa)
        {
            DateTime Lastroll = Session.Character.LastRoll;
            DateTime time = Session.Character.LastRoll.AddSeconds(10);

            if (DateTime.Now <= time) // Anti spam $Roll
            {
                return;
            }
            Session.Character.LastRoll = DateTime.Now; // ISSOU 

            int rndm = ServerManager.RandomNumber();
            string fun = rndm >= 50 ? "Woah, you are the boss!" : "Soo gooood!";
            string message = $"{Session.Character.Name} rolled <{rndm}>" + fun.ToString();

            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), 1), ReceiverType.AllExceptMe);

            foreach (ClientSession sess in ServerManager.Instance.Sessions.Where(s => s.Account.Authority >= AuthorityType.User))

            {
                if (sess.HasSelectedCharacter)
                {
                    sess.SendPacket(sess.Character.GenerateSay($"[ROLL-Channel {Session.Character.Name}]: rolled {rndm}", 12));
                }

            }
        }

        /// <summary>
        /// $CharStat Command
        /// </summary>
        /// <param name="characterStatsPacket"></param>
        public void CharStat(CharacterStatsPacket characterStatsPacket)
        {
            string returnHelp = CharacterStatsPacket.ReturnHelp();
            if (characterStatsPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[CharStat]CharacterName: {characterStatsPacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[CharStat]CharacterName: {characterStatsPacket.CharacterName}"
                    });
                }

                string name = characterStatsPacket.CharacterName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (ServerManager.Instance.GetSessionByCharacterName(name) != null)
                    {
                        Character character = ServerManager.Instance.GetSessionByCharacterName(name).Character;
                        SendStats(character);
                    }
                    else if (DAOFactory.CharacterDAO.LoadByName(name) != null)
                    {
                        CharacterDTO characterDto = DAOFactory.CharacterDAO.LoadByName(name);
                        SendStats(characterDto);
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
            }
        }

        /// <summary>
        /// $ClassPack
        /// </summary>
        /// <param name="classPackPacket"></param>
        public void ClassPack(ClassPackPacket classPackPacket)
        {
            if (classPackPacket != null)
            {
                if (classPackPacket.Class < 1 || classPackPacket.Class > 3)
                {
                    Session.SendPacket(Session.Character.GenerateSay("Invalid class", 11));
                    Session.SendPacket(Session.Character.GenerateSay(ClassPackPacket.ReturnHelp(), 10));
                    return;
                }

                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                                       $"[ClassPack]Class: {classPackPacket.Class}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ClassPack]Class: {classPackPacket.Class}"
                    });
                }

                switch (classPackPacket.Class)
                {
                    case 1:
                        Session.Character.Inventory.AddNewToInventory(4075, 1);
                        Session.Character.Inventory.AddNewToInventory(4076, 1);
                        Session.Character.Inventory.AddNewToInventory(4129, 1);
                        Session.Character.Inventory.AddNewToInventory(4130, 1);
                        Session.Character.Inventory.AddNewToInventory(4131, 1);
                        Session.Character.Inventory.AddNewToInventory(4132, 1);
                        Session.Character.Inventory.AddNewToInventory(1685, 999);
                        Session.Character.Inventory.AddNewToInventory(1686, 999);
                        Session.Character.Inventory.AddNewToInventory(5087, 999);
                        Session.Character.Inventory.AddNewToInventory(5203, 999);
                        Session.Character.Inventory.AddNewToInventory(5372, 999);
                        Session.Character.Inventory.AddNewToInventory(5431, 999);
                        Session.Character.Inventory.AddNewToInventory(5432, 999);
                        Session.Character.Inventory.AddNewToInventory(5498, 999);
                        Session.Character.Inventory.AddNewToInventory(5499, 999);
                        Session.Character.Inventory.AddNewToInventory(5553, 999);
                        Session.Character.Inventory.AddNewToInventory(5560, 999);
                        Session.Character.Inventory.AddNewToInventory(5591, 999);
                        Session.Character.Inventory.AddNewToInventory(5837, 999);
                        Session.Character.Inventory.AddNewToInventory(4875, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(4873, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(2072, 999);
                        Session.Character.Inventory.AddNewToInventory(2071, 999);
                        Session.Character.Inventory.AddNewToInventory(2070, 999);
                        Session.Character.Inventory.AddNewToInventory(2160, 999);
                        Session.Character.Inventory.AddNewToInventory(4138, 1);
                        Session.Character.Inventory.AddNewToInventory(4146, 1);
                        Session.Character.Inventory.AddNewToInventory(4142, 1);
                        Session.Character.Inventory.AddNewToInventory(4150, 1);
                        Session.Character.Inventory.AddNewToInventory(4353, 1);
                        Session.Character.Inventory.AddNewToInventory(4124, 1);
                        Session.Character.Inventory.AddNewToInventory(4172, 1);
                        Session.Character.Inventory.AddNewToInventory(4183, 1);
                        Session.Character.Inventory.AddNewToInventory(4187, 1);
                        Session.Character.Inventory.AddNewToInventory(4283, 1);
                        Session.Character.Inventory.AddNewToInventory(4285, 1);
                        Session.Character.Inventory.AddNewToInventory(4177, 1);
                        Session.Character.Inventory.AddNewToInventory(4179, 1);
                        Session.Character.Inventory.AddNewToInventory(4244, 1);
                        Session.Character.Inventory.AddNewToInventory(4252, 1);
                        Session.Character.Inventory.AddNewToInventory(4256, 1);
                        Session.Character.Inventory.AddNewToInventory(4248, 1);
                        Session.Character.Inventory.AddNewToInventory(3116, 1);
                        Session.Character.Inventory.AddNewToInventory(1277, 999);
                        Session.Character.Inventory.AddNewToInventory(1274, 999);
                        Session.Character.Inventory.AddNewToInventory(1280, 999);
                        Session.Character.Inventory.AddNewToInventory(2419, 999);
                        Session.Character.Inventory.AddNewToInventory(1914, 1);
                        Session.Character.Inventory.AddNewToInventory(1296, 999);
                        Session.Character.Inventory.AddNewToInventory(5916, 999);
                        Session.Character.Inventory.AddNewToInventory(3001, 1);
                        Session.Character.Inventory.AddNewToInventory(3003, 1);
                        Session.Character.Inventory.AddNewToInventory(4490, 1);
                        Session.Character.Inventory.AddNewToInventory(4699, 1);
                        Session.Character.Inventory.AddNewToInventory(4099, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(900, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(907, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(908, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4883, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4889, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4895, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4371, 1);
                        Session.Character.Inventory.AddNewToInventory(4353, 1);
                        Session.Character.Inventory.AddNewToInventory(4277, 1);
                        Session.Character.Inventory.AddNewToInventory(4309, 1);
                        Session.Character.Inventory.AddNewToInventory(4271, 1);
                        Session.Character.Inventory.AddNewToInventory(901, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(902, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(909, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(910, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4500, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4497, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4493, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4489, 1, Upgrade: 15);
                        break;

                    case 2:
                        Session.Character.Inventory.AddNewToInventory(4075, 1);
                        Session.Character.Inventory.AddNewToInventory(4076, 1);
                        Session.Character.Inventory.AddNewToInventory(4129, 1);
                        Session.Character.Inventory.AddNewToInventory(4130, 1);
                        Session.Character.Inventory.AddNewToInventory(4131, 1);
                        Session.Character.Inventory.AddNewToInventory(4132, 1);
                        Session.Character.Inventory.AddNewToInventory(1685, 999);
                        Session.Character.Inventory.AddNewToInventory(1686, 999);
                        Session.Character.Inventory.AddNewToInventory(5087, 999);
                        Session.Character.Inventory.AddNewToInventory(5203, 999);
                        Session.Character.Inventory.AddNewToInventory(5372, 999);
                        Session.Character.Inventory.AddNewToInventory(5431, 999);
                        Session.Character.Inventory.AddNewToInventory(5432, 999);
                        Session.Character.Inventory.AddNewToInventory(5498, 999);
                        Session.Character.Inventory.AddNewToInventory(5499, 999);
                        Session.Character.Inventory.AddNewToInventory(5553, 999);
                        Session.Character.Inventory.AddNewToInventory(5560, 999);
                        Session.Character.Inventory.AddNewToInventory(5591, 999);
                        Session.Character.Inventory.AddNewToInventory(5837, 999);
                        Session.Character.Inventory.AddNewToInventory(4875, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(4873, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(2072, 999);
                        Session.Character.Inventory.AddNewToInventory(2071, 999);
                        Session.Character.Inventory.AddNewToInventory(2070, 999);
                        Session.Character.Inventory.AddNewToInventory(2160, 999);
                        Session.Character.Inventory.AddNewToInventory(4138, 1);
                        Session.Character.Inventory.AddNewToInventory(4146, 1);
                        Session.Character.Inventory.AddNewToInventory(4142, 1);
                        Session.Character.Inventory.AddNewToInventory(4150, 1);
                        Session.Character.Inventory.AddNewToInventory(4353, 1);
                        Session.Character.Inventory.AddNewToInventory(4124, 1);
                        Session.Character.Inventory.AddNewToInventory(4172, 1);
                        Session.Character.Inventory.AddNewToInventory(4183, 1);
                        Session.Character.Inventory.AddNewToInventory(4187, 1);
                        Session.Character.Inventory.AddNewToInventory(4283, 1);
                        Session.Character.Inventory.AddNewToInventory(4285, 1);
                        Session.Character.Inventory.AddNewToInventory(4177, 1);
                        Session.Character.Inventory.AddNewToInventory(4179, 1);
                        Session.Character.Inventory.AddNewToInventory(4244, 1);
                        Session.Character.Inventory.AddNewToInventory(4252, 1);
                        Session.Character.Inventory.AddNewToInventory(4256, 1);
                        Session.Character.Inventory.AddNewToInventory(4248, 1);
                        Session.Character.Inventory.AddNewToInventory(3116, 1);
                        Session.Character.Inventory.AddNewToInventory(1277, 999);
                        Session.Character.Inventory.AddNewToInventory(1274, 999);
                        Session.Character.Inventory.AddNewToInventory(1280, 999);
                        Session.Character.Inventory.AddNewToInventory(2419, 999);
                        Session.Character.Inventory.AddNewToInventory(1914, 1);
                        Session.Character.Inventory.AddNewToInventory(1296, 999);
                        Session.Character.Inventory.AddNewToInventory(5916, 999);
                        Session.Character.Inventory.AddNewToInventory(3001, 1);
                        Session.Character.Inventory.AddNewToInventory(3003, 1);
                        Session.Character.Inventory.AddNewToInventory(4490, 1);
                        Session.Character.Inventory.AddNewToInventory(4699, 1);
                        Session.Character.Inventory.AddNewToInventory(4099, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(900, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(907, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(908, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4885, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4890, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4897, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4372, 1);
                        Session.Character.Inventory.AddNewToInventory(4310, 1);
                        Session.Character.Inventory.AddNewToInventory(4354, 1);
                        Session.Character.Inventory.AddNewToInventory(4279, 1);
                        Session.Character.Inventory.AddNewToInventory(4273, 1);
                        Session.Character.Inventory.AddNewToInventory(903, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(904, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(911, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(912, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4501, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4498, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4488, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4492, 1, Upgrade: 15);
                        break;

                    case 3:
                        Session.Character.Inventory.AddNewToInventory(4075, 1);
                        Session.Character.Inventory.AddNewToInventory(4076, 1);
                        Session.Character.Inventory.AddNewToInventory(4129, 1);
                        Session.Character.Inventory.AddNewToInventory(4130, 1);
                        Session.Character.Inventory.AddNewToInventory(4131, 1);
                        Session.Character.Inventory.AddNewToInventory(4132, 1);
                        Session.Character.Inventory.AddNewToInventory(1685, 999);
                        Session.Character.Inventory.AddNewToInventory(1686, 999);
                        Session.Character.Inventory.AddNewToInventory(5087, 999);
                        Session.Character.Inventory.AddNewToInventory(5203, 999);
                        Session.Character.Inventory.AddNewToInventory(5372, 999);
                        Session.Character.Inventory.AddNewToInventory(5431, 999);
                        Session.Character.Inventory.AddNewToInventory(5432, 999);
                        Session.Character.Inventory.AddNewToInventory(5498, 999);
                        Session.Character.Inventory.AddNewToInventory(5499, 999);
                        Session.Character.Inventory.AddNewToInventory(5553, 999);
                        Session.Character.Inventory.AddNewToInventory(5560, 999);
                        Session.Character.Inventory.AddNewToInventory(5591, 999);
                        Session.Character.Inventory.AddNewToInventory(5837, 999);
                        Session.Character.Inventory.AddNewToInventory(4875, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(4873, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(2072, 999);
                        Session.Character.Inventory.AddNewToInventory(2071, 999);
                        Session.Character.Inventory.AddNewToInventory(2070, 999);
                        Session.Character.Inventory.AddNewToInventory(2160, 999);
                        Session.Character.Inventory.AddNewToInventory(4138, 1);
                        Session.Character.Inventory.AddNewToInventory(4146, 1);
                        Session.Character.Inventory.AddNewToInventory(4142, 1);
                        Session.Character.Inventory.AddNewToInventory(4150, 1);
                        Session.Character.Inventory.AddNewToInventory(4353, 1);
                        Session.Character.Inventory.AddNewToInventory(4124, 1);
                        Session.Character.Inventory.AddNewToInventory(4172, 1);
                        Session.Character.Inventory.AddNewToInventory(4183, 1);
                        Session.Character.Inventory.AddNewToInventory(4187, 1);
                        Session.Character.Inventory.AddNewToInventory(4283, 1);
                        Session.Character.Inventory.AddNewToInventory(4285, 1);
                        Session.Character.Inventory.AddNewToInventory(4177, 1);
                        Session.Character.Inventory.AddNewToInventory(4179, 1);
                        Session.Character.Inventory.AddNewToInventory(4244, 1);
                        Session.Character.Inventory.AddNewToInventory(4252, 1);
                        Session.Character.Inventory.AddNewToInventory(4256, 1);
                        Session.Character.Inventory.AddNewToInventory(4248, 1);
                        Session.Character.Inventory.AddNewToInventory(3116, 1);
                        Session.Character.Inventory.AddNewToInventory(1277, 999);
                        Session.Character.Inventory.AddNewToInventory(1274, 999);
                        Session.Character.Inventory.AddNewToInventory(1280, 999);
                        Session.Character.Inventory.AddNewToInventory(2419, 999);
                        Session.Character.Inventory.AddNewToInventory(1914, 1);
                        Session.Character.Inventory.AddNewToInventory(1296, 999);
                        Session.Character.Inventory.AddNewToInventory(5916, 999);
                        Session.Character.Inventory.AddNewToInventory(3001, 1);
                        Session.Character.Inventory.AddNewToInventory(3003, 1);
                        Session.Character.Inventory.AddNewToInventory(4490, 1);
                        Session.Character.Inventory.AddNewToInventory(4699, 1);
                        Session.Character.Inventory.AddNewToInventory(4099, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(900, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(907, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(908, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4887, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4892, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4899, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4311, 1);
                        Session.Character.Inventory.AddNewToInventory(4373, 1);
                        Session.Character.Inventory.AddNewToInventory(4281, 1);
                        Session.Character.Inventory.AddNewToInventory(4355, 1);
                        Session.Character.Inventory.AddNewToInventory(4275, 1);
                        Session.Character.Inventory.AddNewToInventory(905, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(906, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(913, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(914, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4502, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4499, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4491, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4487, 1, Upgrade: 15);
                        break;
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ClassPackPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Clear Command
        /// </summary>
        /// <param name="clearInventoryPacket"></param>
        public void ClearInventory(ClearInventoryPacket clearInventoryPacket)
        {
            if (clearInventoryPacket != null && clearInventoryPacket.InventoryType != InventoryType.Wear)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Clear]InventoryType: {clearInventoryPacket.InventoryType}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Clear]InventoryType: {clearInventoryPacket.InventoryType}"
                    });
                }

                Parallel.ForEach(Session.Character.Inventory.Where(s => s.Type == clearInventoryPacket.InventoryType),
                    inv =>
                    {
                        Session.Character.Inventory.DeleteById(inv.Id);
                        Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(inv.Type, inv.Slot));
                    });
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ClearInventoryPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ClearMap packet
        /// </summary>
        /// <param name="clearMapPacket"></param>
        public void ClearMap(ClearMapPacket clearMapPacket)
        {
            if (clearMapPacket != null && Session.HasCurrentMapInstance)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[ClearMap]MapId: {Session.CurrentMapInstance.MapInstanceId}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ClearMap]MapId: {Session.CurrentMapInstance.MapInstanceId}"
                    });
                }

                Parallel.ForEach(Session.CurrentMapInstance.Monsters.Where(s => s.ShouldRespawn != true), monster =>
                {
                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                        monster.MapMonsterId));
                    monster.SetDeathStatement();
                    Session.CurrentMapInstance.RemoveMonster(monster);
                });
                Parallel.ForEach(Session.CurrentMapInstance.DroppedList.GetAllItems(), drop =>
                {
                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Object, drop.TransportId));
                    Session.CurrentMapInstance.DroppedList.Remove(drop.TransportId);
                });
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ClearMapPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Clone Command
        /// </summary>
        /// <param name="cloneItemPacket"></param>
        public void CloneItem(CloneItemPacket cloneItemPacket)
        {
            if (cloneItemPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Clone]Slot: {cloneItemPacket.Slot}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Clone]Slot: {cloneItemPacket.Slot}"
                    });
                }

                ItemInstance item =
                    Session.Character.Inventory.LoadBySlotAndType(cloneItemPacket.Slot, InventoryType.Equipment);
                if (item != null)
                {
                    item = item.DeepCopy();
                    item.Id = Guid.NewGuid();
                    Session.Character.Inventory.AddToInventory(item);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(CloneItemPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Help Command
        /// </summary>
        /// <param name="helpPacket"></param>
        public void Command(HelpPacket helpPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[Help]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[Help]"
                });
            }

            // get commands
            List<Type> classes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()).Where(t =>
                t.IsClass && t.Namespace == "Nostale.Packets.Packets.CommandPackets"
                && (((PacketHeaderAttribute)Array.Find(t.GetCustomAttributes(true),
                     ca => ca.GetType().Equals(typeof(PacketHeaderAttribute))))?.Authorities)
                     .Any(c => Session.Account.Authority.Equals(c)
                     || Session.Account.Authority.Equals(AuthorityType.Administrator)
                     || c.Equals(AuthorityType.User) && Session.Account.Authority >= AuthorityType.User
                     || c.Equals(AuthorityType.DSGM) && Session.Account.Authority >= AuthorityType.DSGM && Session.Account.Authority <= AuthorityType.BA
                     || c.Equals(AuthorityType.MOD) && Session.Account.Authority >= AuthorityType.MOD && Session.Account.Authority <= AuthorityType.BA
                     || c.Equals(AuthorityType.SMOD) && Session.Account.Authority >= AuthorityType.SMOD && Session.Account.Authority <= AuthorityType.BA
                     || c.Equals(AuthorityType.TGM) && Session.Account.Authority >= AuthorityType.TGM
                     || c.Equals(AuthorityType.GM) && Session.Account.Authority >= AuthorityType.GM
                     || c.Equals(AuthorityType.SGM) && Session.Account.Authority >= AuthorityType.SGM
                     || c.Equals(AuthorityType.GA) && Session.Account.Authority >= AuthorityType.GA
                     || c.Equals(AuthorityType.TM) && Session.Account.Authority >= AuthorityType.TM
                     || c.Equals(AuthorityType.CM) && Session.Account.Authority >= AuthorityType.CM
                     || c.Equals(AuthorityType.BitchNiggerFaggot) && Session.Account.Authority.Equals(AuthorityType.BitchNiggerFaggot)
                     )).ToList();
            List<string> messages = new List<string>();
            foreach (Type type in classes)
            {
                object classInstance = Activator.CreateInstance(type);
                Type classType = classInstance.GetType();
                MethodInfo method = classType.GetMethod("ReturnHelp");
                if (method != null)
                {
                    messages.Add(method.Invoke(classInstance, null).ToString());
                }
                else
                {
                    var identification = type.GetCustomAttribute<PacketHeaderAttribute>()?.Identification;
                    messages.Add(identification != null && identification.Length > 0 ? identification[0] : string.Empty);
                }

            }

            // send messages
            messages.Sort();
            if (helpPacket.Contents == "*" || string.IsNullOrEmpty(helpPacket.Contents))
            {
                Session.SendPacket(Session.Character.GenerateSay("-------------Commands Info-------------", 11));
                foreach (string message in messages)
                {
                    Session.SendPacket(Session.Character.GenerateSay(message, 12));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay("-------------Command Info-------------", 11));
                foreach (string message in messages.Where(s =>
                    s.IndexOf(helpPacket.Contents, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Session.SendPacket(Session.Character.GenerateSay(message, 12));
                }
            }

            Session.SendPacket(Session.Character.GenerateSay("-----------------------------------------------", 11));
        }

        /// <summary>
        /// $CreateItem Packet
        /// </summary>
        /// <param name="createItemPacket"></param>
        public void CreateItem(CreateItemPacket createItemPacket)
        {
            if (createItemPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[CreateItem]ItemVNum: {createItemPacket.VNum} Amount/Design: {createItemPacket.Design} Upgrade: {createItemPacket.Upgrade}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[CreateItem]ItemVNum: {createItemPacket.VNum} Amount/Design: {createItemPacket.Design} Upgrade: {createItemPacket.Upgrade}"
                    });
                }

                short vnum = createItemPacket.VNum;
                short amount = 1;
                sbyte rare = 0;
                byte upgrade = 0, design = 0;
                if (vnum == 1046)
                {
                    return; // cannot create gold as item, use $Gold instead
                }

                Item iteminfo = ServerManager.GetItem(vnum);
                if (iteminfo != null)
                {
                    if (iteminfo.IsColored || (iteminfo.ItemType == ItemType.Box && iteminfo.ItemSubType == 3))
                    {
                        if (createItemPacket.Design.HasValue)
                        {
                            rare = (sbyte)ServerManager.RandomNumber();
                            if (rare > 90)
                            {
                                rare = 7;
                            }
                            else if (rare > 80)
                            {
                                rare = 6;
                            }
                            else
                            {
                                rare = (sbyte)ServerManager.RandomNumber(1, 6);
                            }
                            design = (byte)createItemPacket.Design.Value;
                        }
                    }
                    else if (iteminfo.Type == 0)
                    {
                        if (createItemPacket.Upgrade.HasValue)
                        {
                            if (iteminfo.EquipmentSlot != EquipmentType.Sp)
                            {
                                upgrade = createItemPacket.Upgrade.Value;
                            }
                            else
                            {
                                design = createItemPacket.Upgrade.Value;
                            }

                            if (iteminfo.EquipmentSlot != EquipmentType.Sp && upgrade == 0
                                && iteminfo.BasicUpgrade != 0)
                            {
                                upgrade = iteminfo.BasicUpgrade;
                            }
                        }

                        if (createItemPacket.Design.HasValue)
                        {
                            if (iteminfo.EquipmentSlot == EquipmentType.Sp)
                            {
                                upgrade = (byte)createItemPacket.Design.Value;
                            }
                            else
                            {
                                rare = (sbyte)createItemPacket.Design.Value;
                            }
                        }
                    }

                    if (createItemPacket.Design.HasValue && !createItemPacket.Upgrade.HasValue)
                    {
                        amount = createItemPacket.Design.Value > 999 ? (short)999 : createItemPacket.Design.Value;
                    }

                    ItemInstance inv = Session.Character.Inventory
                        .AddNewToInventory(vnum, amount, Rare: rare, Upgrade: upgrade, Design: design).FirstOrDefault();
                    if (inv != null)
                    {
                        ItemInstance wearable = Session.Character.Inventory.LoadBySlotAndType(inv.Slot, inv.Type);
                        if (wearable != null)
                        {
                            switch (wearable.Item.EquipmentSlot)
                            {
                                case EquipmentType.Armor:
                                case EquipmentType.MainWeapon:
                                case EquipmentType.SecondaryWeapon:
                                    wearable.SetRarityPoint();
                                    break;

                                case EquipmentType.Boots:
                                case EquipmentType.Gloves:
                                    wearable.FireResistance = (short)(wearable.Item.FireResistance * upgrade);
                                    wearable.DarkResistance = (short)(wearable.Item.DarkResistance * upgrade);
                                    wearable.LightResistance = (short)(wearable.Item.LightResistance * upgrade);
                                    wearable.WaterResistance = (short)(wearable.Item.WaterResistance * upgrade);
                                    break;
                            }
                        }

                        Session.SendPacket(Session.Character.GenerateSay(
                            $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {iteminfo.Name} x {amount}", 12));
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                0));
                    }
                }
                else
                {
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_ITEM"), 0);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(CreateItemPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $CustomNpcMonsterName Packet
        /// </summary>
        /// <param name="changeNpcMonsterNamePacket"></param>
        public void CustomNpcMonsterName(ChangeNpcMonsterNamePacket changeNpcMonsterNamePacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                if (Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId) is MapNpc npc)
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[CustomNpcName]MapNpcId: {npc.MapNpcId} Name: {changeNpcMonsterNamePacket.Name}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[CustomNpcName]MapNpcId: {npc.MapNpcId} Name: {changeNpcMonsterNamePacket.Name}"
                        });
                    }

                    if (DAOFactory.MapNpcDAO.LoadById(npc.MapNpcId) is MapNpcDTO npcDTO)
                    {
                        npc.Name = changeNpcMonsterNamePacket.Name;
                        npcDTO.Name = changeNpcMonsterNamePacket.Name;
                        DAOFactory.MapNpcDAO.Update(ref npcDTO);

                        Session.CurrentMapInstance.Broadcast(npc.GenerateIn());
                    }
                }
                else if (Session.CurrentMapInstance.GetMonsterById(Session.Character.LastNpcMonsterId) is MapMonster monster)
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[CustomNpcName]MapMonsterId: {monster.MapMonsterId} Name: {changeNpcMonsterNamePacket.Name}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[CustomNpcName]MapMonsterId: {monster.MapMonsterId} Name: {changeNpcMonsterNamePacket.Name}"
                        });
                    }

                    if (DAOFactory.MapMonsterDAO.LoadById(monster.MapMonsterId) is MapMonsterDTO monsterDTO)
                    {
                        monster.Name = changeNpcMonsterNamePacket.Name;
                        monsterDTO.Name = changeNpcMonsterNamePacket.Name;
                        DAOFactory.MapMonsterDAO.Update(ref monsterDTO);

                        Session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NPCMONSTER_NOT_FOUND"), 11));
                }
            }
        }

        /// <summary>
        /// $Demote Command
        /// </summary>
        /// <param name="demotePacket"></param>
        public void Demote(DemotePacket demotePacket)
        {
            if (demotePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Demote]CharacterName: {demotePacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Demote]CharacterName: {demotePacket.CharacterName}"
                    });
                }

                string name = demotePacket.CharacterName;
                try
                {
                    AccountDTO account = DAOFactory.AccountDAO.LoadById(DAOFactory.CharacterDAO.LoadByName(name).AccountId);
                    if (account?.Authority > AuthorityType.User)
                    {
                        if (Session.Account.Authority >= account?.Authority)
                        {
                            AuthorityType newAuthority = AuthorityType.User;
                            switch (account.Authority)
                            {
                                case AuthorityType.DEV:
                                    newAuthority = AuthorityType.CM;
                                    break;

                                case AuthorityType.CM:
                                    newAuthority = AuthorityType.TM;
                                    break;

                                case AuthorityType.TM:
                                    newAuthority = AuthorityType.GA;
                                    break;

                                case AuthorityType.GA:
                                    newAuthority = AuthorityType.SGM;
                                    break;

                                case AuthorityType.SGM:
                                    newAuthority = AuthorityType.GM;
                                    break;

                                case AuthorityType.DSGM:
                                    newAuthority = AuthorityType.Supporter;
                                    break;

                                case AuthorityType.Supporter:
                                    newAuthority = AuthorityType.User;
                                    break;

                                default:
                                    newAuthority = AuthorityType.User;
                                    break;
                            }
                            account.Authority = newAuthority;
                            DAOFactory.AccountDAO.InsertOrUpdate(ref account);
                            ClientSession session =
                                ServerManager.Instance.Sessions.FirstOrDefault(s => s.Character?.Name == name);
                            if (session != null)
                            {
                                session.Account.Authority = newAuthority;
                                session.Character.Authority = newAuthority;
                                if (session.Character.InvisibleGm)
                                {
                                    session.Character.Invisible = false;
                                    session.Character.InvisibleGm = false;
                                    Session.Character.Mates.Where(m => m.IsTeamMember).ToList().ForEach(m =>
                                        Session.CurrentMapInstance?.Broadcast(m.GenerateIn(), ReceiverType.AllExceptMe));
                                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(),
                                        ReceiverType.AllExceptMe);
                                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(),
                                        ReceiverType.AllExceptMe);
                                }
                                ServerManager.Instance.ChangeMap(session.Character.CharacterId);
                                DAOFactory.AccountDAO.WriteGeneralLog(session.Account.AccountId, session.IpAddress,
                                    session.Character.CharacterId, GeneralLogType.Demotion, $"by: {Session.Character.Name}");
                            }
                            else
                            {
                                DAOFactory.AccountDAO.WriteGeneralLog(account.AccountId, "127.0.0.1", null,
                                    GeneralLogType.Demotion, $"by: {Session.Character.Name}");
                            }

                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                        }
                        else
                        {
                            Session.SendPacket(
                                Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_THAT"), 10));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                    }
                }
                catch
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(DemotePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Drop Command
        /// </summary>
        /// <param name="dropPacket"></param>
        public void Drop(DropPacket dropPacket)
        {
            if (dropPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                       $"[Drop]ItemVNum: {dropPacket.VNum} Amount: {dropPacket.Amount} Count: {dropPacket.Count} Time: {dropPacket.Time}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Drop]ItemVNum: {dropPacket.VNum} Amount: {dropPacket.Amount} Count: {dropPacket.Count} Time: {dropPacket.Time}"
                    });
                }

                short vnum = dropPacket.VNum;
                short amount = dropPacket.Amount;
                if (amount < 1) { amount = 1; }
                else if (amount > 999) { amount = 999; }
                int count = dropPacket.Count;
                if (count < 1) { count = 1; }
                int time = dropPacket.Time;

                GameObject.MapInstance instance = Session.CurrentMapInstance;

                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(observer =>
                {
                    {
                        for (int i = 0; i < count; i++)
                        {
                            MonsterMapItem droppedItem = new MonsterMapItem(Session.Character.PositionX, Session.Character.PositionY, vnum, amount);
                            instance.DroppedList[droppedItem.TransportId] = droppedItem;
                            instance.Broadcast(
                                $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 -1");

                            System.Threading.Thread.Sleep(time * 1000 / count);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// $DropRate Command
        /// </summary>
        /// <param name="dropRatePacket"></param>
        public void DropRate(DropRatePacket dropRatePacket)
        {
            if (dropRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[DropRate]Value: {dropRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[DropRate]Value: {dropRatePacket.Value}"
                    });
                }

                if (dropRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateDrop = dropRatePacket.Value;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("DROP_RATE_CHANGED"), 0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(DropRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Effect Command
        /// </summary>
        /// <param name="effectCommandpacket"></param>
        public void Effect(EffectCommandPacket effectCommandpacket)
        {
            if (effectCommandpacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Effect]EffectId: {effectCommandpacket.EffectId}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Effect]EffectId: {effectCommandpacket.EffectId}"
                    });
                }

                Session.CurrentMapInstance?.Broadcast(
                    StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId,
                        effectCommandpacket.EffectId), Session.Character.PositionX, Session.Character.PositionY);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(EffectCommandPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Faction Command
        /// </summary>
        /// <param name="factionPacket"></param>
        public void Faction(FactionPacket factionPacket)
        {
            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[Faction]"
                });
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4"),
                        0));
                return;
            }
            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipAngel
                || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipDemon)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4SHIP"),
                        0));
                return;
            }
            if (factionPacket != null)
            {
                Session.SendPacket("scr 0 0 0 0 0 0 0");
                if (Session.Character.Faction == FactionType.Angel)
                {
                    Session.Character.Faction = FactionType.Demon;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey($"GET_PROTECTION_POWER_2"),
                            0));
                }
                else
                {
                    Session.Character.Faction = FactionType.Angel;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey($"GET_PROTECTION_POWER_1"),
                            0));
                }
                Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                    Session.Character.CharacterId, 4799 + (byte)Session.Character.Faction));
                Session.SendPacket(Session.Character.GenerateFaction());
                if (ServerManager.Instance.ChannelId == 51)
                {
                    Session.SendPacket(Session.Character.GenerateFc());
                }
            }
        }

        /// <summary>
        /// $FairyXPRate Command
        /// </summary>
        /// <param name="fairyXpRatePacket"></param>
        public void FairyXpRate(FairyXpRatePacket fairyXpRatePacket)
        {
            if (fairyXpRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[FairyXPRate]Value: {fairyXpRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[FairyXPRate]Value: {fairyXpRatePacket.Value}"
                    });
                }

                if (fairyXpRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateFairyXP = fairyXpRatePacket.Value;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAIRYXP_RATE_CHANGED"),
                            0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(FairyXpRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $FamilyFaction Command
        /// </summary>
        /// <param name="familyFactionPacket"></param>
        public void FamilyFaction(FamilyFactionPacket familyFactionPacket)
        {
            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[FamilyFaction]"
                });
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4"),
                        0));
                return;
            }
            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipAngel
                || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipDemon)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4SHIP"),
                        0));
                return;
            }
            if (familyFactionPacket != null)
            {
                if (String.IsNullOrEmpty(familyFactionPacket.FamilyName) && Session.Character.Family != null)
                {
                    Session.Character.Family.ChangeFaction(Session.Character.Family.FamilyFaction == 1 ? (byte)2 : (byte)1, Session);
                    return;
                }
                Family family = ServerManager.Instance.FamilyList.FirstOrDefault(s => s.Name == familyFactionPacket.FamilyName);
                if (family != null)
                {
                    family.ChangeFaction(family.FamilyFaction == 1 ? (byte)2 : (byte)1, Session);
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay("Family not found.", 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(FamilyFactionPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Gift Command
        /// </summary>
        /// <param name="giftPacket"></param>
        public void Gift(GiftPacket giftPacket)
        {
            if (giftPacket != null)
            {
                short Amount = giftPacket.Amount;

                if (Amount <= 0)
                {
                    Amount = 1;
                }

                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Gift]CharacterName: {giftPacket.CharacterName} ItemVNum: {giftPacket.VNum} Amount: {Amount} Rare: {giftPacket.Rare} Upgrade: {giftPacket.Upgrade}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Gift]CharacterName: {giftPacket.CharacterName} ItemVNum: {giftPacket.VNum} Amount: {Amount} Rare: {giftPacket.Rare} Upgrade: {giftPacket.Upgrade}"
                    });
                }

                if (giftPacket.CharacterName == "*")
                {
                    if (Session.HasCurrentMapInstance)
                    {
                        Parallel.ForEach(Session.CurrentMapInstance.Sessions,
                            session => Session.Character.SendGift(session.Character.CharacterId, giftPacket.VNum,
                                Amount, giftPacket.Rare, giftPacket.Upgrade, giftPacket.Design, false));
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GIFT_SENT"), 10));
                    }
                }
                else if (giftPacket.CharacterName == "ALL")
                {
                    int levelMin = giftPacket.ReceiverLevelMin;
                    int levelMax = giftPacket.ReceiverLevelMax == 0 ? 99 : giftPacket.ReceiverLevelMax;

                    DAOFactory.CharacterDAO.LoadAll().ToList().ForEach(chara =>
                    {
                        if (chara.Level >= levelMin && chara.Level <= levelMax)
                        {
                            Session.Character.SendGift(chara.CharacterId, giftPacket.VNum, Amount,
                                giftPacket.Rare, giftPacket.Upgrade, giftPacket.Design, false);
                        }
                    });
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GIFT_SENT"), 10));
                }
                else
                {
                    CharacterDTO chara = DAOFactory.CharacterDAO.LoadByName(giftPacket.CharacterName);
                    if (chara != null)
                    {
                        Session.Character.SendGift(chara.CharacterId, giftPacket.VNum, Amount,
                            giftPacket.Rare, giftPacket.Upgrade, giftPacket.Design, false);
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("GIFT_SENT"), 10));
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"),
                                0));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GiftPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $GodMode Command
        /// </summary>
        /// <param name="godModePacket"></param>
        public void GodMode(GodModePacket godModePacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[GodMode]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[GodMode]"
                });
            }

            Session.Character.HasGodMode = !Session.Character.HasGodMode;
            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $Gogo Command
        /// </summary>
        /// <param name="gogoPacket"></param>
        public void Gogo(GogoPacket gogoPacket)
        {
            if (gogoPacket != null)
            {
                if (Session.Character.HasShopOpened || Session.Character.InExchangeOrTrade)
                {
                    Session.Character.DisposeShopAndExchange();
                }

                if (Session.Character.IsChangingMapInstance)
                {
                    return;
                }

                if (Session.CurrentMapInstance != null)
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[Gogo]MapId: {Session.CurrentMapInstance.Map.MapId} MapX: {gogoPacket.X} MapY: {gogoPacket.Y}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[Gogo]MapId: {Session.CurrentMapInstance.Map.MapId} MapX: {gogoPacket.X} MapY: {gogoPacket.Y}"
                        });
                    }

                    if (gogoPacket.X == 0 && gogoPacket.Y == 0)
                    {
                        ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, Session.CurrentMapInstance.MapInstanceId);
                    }
                    else
                    {
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, Session.CurrentMapInstance.MapInstanceId, gogoPacket.X, gogoPacket.Y);
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GogoPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Gold Command
        /// </summary>
        /// <param name="goldPacket"></param>
        public void Gold(GoldPacket goldPacket)
        {
            if (goldPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Gold]Amount: {goldPacket.Amount}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Gold]Amount: {goldPacket.Amount}"
                    });
                }

                long gold = goldPacket.Amount;
                long maxGold = ServerManager.Instance.Configuration.MaxGold;
                gold = gold > maxGold ? maxGold : gold;
                if (gold >= 0)
                {
                    Session.Character.Gold = gold;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GOLD_SET"),
                        0));
                    Session.SendPacket(Session.Character.GenerateGold());
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GoldPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $GoldDropRate Command
        /// </summary>
        /// <param name="goldDropRatePacket"></param>
        public void GoldDropRate(GoldDropRatePacket goldDropRatePacket)
        {
            if (goldDropRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[GoldDropRate]Value: {goldDropRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[GoldDropRate]Value: {goldDropRatePacket.Value}"
                    });
                }

                if (goldDropRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateGoldDrop = goldDropRatePacket.Value;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GOLD_DROP_RATE_CHANGED"),
                            0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GoldDropRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $GoldRate Command
        /// </summary>
        /// <param name="goldRatePacket"></param>
        public void GoldRate(GoldRatePacket goldRatePacket)
        {
            if (goldRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[GoldRate]Value: {goldRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[GoldRate]Value: {goldRatePacket.Value}"
                    });
                }

                if (goldRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateGold = goldRatePacket.Value;

                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GOLD_RATE_CHANGED"), 0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GoldRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Guri Command
        /// </summary>
        /// <param name="guriCommandPacket"></param>
        public void Guri(GuriCommandPacket guriCommandPacket)
        {
            if (guriCommandPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Guri]Type: {guriCommandPacket.Type} Value: {guriCommandPacket.Value} Arguments: {guriCommandPacket.Argument}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Guri]Type: {guriCommandPacket.Type} Value: {guriCommandPacket.Value} Arguments: {guriCommandPacket.Argument}"
                    });
                }

                Session.SendPacket(UserInterfaceHelper.GenerateGuri(guriCommandPacket.Type, guriCommandPacket.Argument,
                    Session.Character.CharacterId, guriCommandPacket.Value));
            }

            Session.Character.GenerateSay(GuriCommandPacket.ReturnHelp(), 10);
        }

        /// <summary>
        /// $HairColor Command
        /// </summary>
        /// <param name="hairColorPacket"></param>
        public void Haircolor(HairColorPacket hairColorPacket)
        {
            if (hairColorPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[HairColor]HairColor: {hairColorPacket.HairColor}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[HairColor]HairColor: {hairColorPacket.HairColor}"
                    });
                }

                Session.Character.HairColor = hairColorPacket.HairColor;
                Session.SendPacket(Session.Character.GenerateEq());
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateIn());
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(HairColorPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $HairStyle Command
        /// </summary>
        /// <param name="hairStylePacket"></param>
        public void Hairstyle(HairStylePacket hairStylePacket)
        {
            if (hairStylePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[HairStyle]HairStyle: {hairStylePacket.HairStyle}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[HairStyle]HairStyle: {hairStylePacket.HairStyle}"
                    });
                }

                Session.Character.HairStyle = hairStylePacket.HairStyle;
                Session.SendPacket(Session.Character.GenerateEq());
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateIn());
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(HairStylePacket.ReturnHelp(), 10));
            }
        }

        public void HelpMe(HelpMePacket packet)
        {
            if (packet != null && !string.IsNullOrWhiteSpace(packet.Message))
            {
                int count = 0;
                foreach (ClientSession team in ServerManager.Instance.Sessions.Where(s =>
                    s.Account.Authority >= AuthorityType.GM))
                {
                    if (team.HasSelectedCharacter)
                    {
                        count++;

                        // TODO: move that to resx soo we follow i18n
                        team.SendPacket(team.Character.GenerateSay($"User {Session.Character.Name} needs your help!",
                            12));
                        team.SendPacket(team.Character.GenerateSay($"Reason: {packet.Message}", 12));
                        team.SendPacket(
                            team.Character.GenerateSay("Please inform the family chat when you take care of!", 12));
                        team.SendPacket(Session.Character.GenerateSpk("Click this message to start chatting.", 5));
                        team.SendPacket(
                            UserInterfaceHelper.GenerateMsg($"User {Session.Character.Name} needs your help!", 0));
                    }
                }

                if (count != 0)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        $"{count} Team members were informed! You should get a message shortly.", 10));
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        "Sadly, there are no online team member right now. Please ask for help on our Discord Server at:",
                        10));
                    Session.SendPacket(Session.Character.GenerateSay("https://discord.gg/aujJkHN", 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(HelpMePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $HeroXPRate Command
        /// </summary>
        /// <param name="heroXpRatePacket"></param>
        public void HeroXpRate(HeroXpRatePacket heroXpRatePacket)
        {
            if (heroXpRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[HeroXPRate]Value: {heroXpRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[HeroXPRate]Value: {heroXpRatePacket.Value}"
                    });
                }

                if (heroXpRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateHeroicXP = heroXpRatePacket.Value;
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HEROXP_RATE_CHANGED"), 0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(HeroXpRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Home Command
        /// </summary>
        /// <param name="homePacket"></param>
        public void Home(HomePacket homePacket)
        {
            if (homePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Home]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.ModeratorCommand,
                        Packet = $"[Home]"
                    });
                }

                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 0, 24, 24);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(HomePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Invisible Command
        /// </summary>
        /// <param name="invisiblePacket"></param>
        public void Invisible(InvisiblePacket invisiblePacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[Invisible]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[Invisible]"
                });
            }

            Session.Character.Invisible = !Session.Character.Invisible;
            Session.Character.InvisibleGm = !Session.Character.InvisibleGm;
            Session.SendPacket(Session.Character.GenerateInvisible());
            Session.SendPacket(Session.Character.GenerateEq());
            Session.SendPacket(Session.Character.GenerateCMode());

            if (Session.Character.InvisibleGm)
            {
                Session.Character.Mates.Where(s => s.IsTeamMember).ToList()
                    .ForEach(s => Session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                Session.CurrentMapInstance?.Broadcast(Session,
                    StaticPacketHelper.Out(UserType.Player, Session.Character.CharacterId), ReceiverType.AllExceptMe);
            }
            else
            {
                foreach (Mate teamMate in Session.Character.Mates.Where(m => m.IsTeamMember))
                {
                    teamMate.PositionX = Session.Character.PositionX;
                    teamMate.PositionY = Session.Character.PositionY;
                    teamMate.UpdateBushFire();
                    Parallel.ForEach(Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                    {
                        if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == s.Character.Faction)
                        {
                            s.SendPacket(teamMate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                        }
                        else
                        {
                            s.SendPacket(teamMate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                        }
                    });
                    Session.SendPacket(Session.Character.GeneratePinit());
                    Session.Character.Mates.ForEach(s => Session.SendPacket(s.GenerateScPacket()));
                    Session.SendPackets(Session.Character.GeneratePst());
                }
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(),
                    ReceiverType.AllExceptMe);
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(),
                    ReceiverType.AllExceptMe);
            }
        }

        /// <summary>
        /// $ItemRain Command
        /// </summary>
        /// <param name="itemRainPacket"></param>
        public void ItemRain(ItemRainPacket itemRainPacket)
        {
            if (itemRainPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                       $"[ItemRain]ItemVNum: {itemRainPacket.VNum} Amount: {itemRainPacket.Amount} Count: {itemRainPacket.Count} Time: {itemRainPacket.Time}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ItemRain]ItemVNum: {itemRainPacket.VNum} Amount: {itemRainPacket.Amount} Count: {itemRainPacket.Count} Time: {itemRainPacket.Time}"
                    });
                }

                short vnum = itemRainPacket.VNum;
                short amount = itemRainPacket.Amount;
                if (amount > 999) { amount = 999; }
                int count = itemRainPacket.Count;
                int time = itemRainPacket.Time;

                GameObject.MapInstance instance = Session.CurrentMapInstance;

                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(observer =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        MapCell cell = instance.Map.GetRandomPosition();
                        MonsterMapItem droppedItem = new MonsterMapItem(cell.X, cell.Y, vnum, amount);
                        instance.DroppedList[droppedItem.TransportId] = droppedItem;
                        instance.Broadcast(
                            $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} 0 -1");

                        System.Threading.Thread.Sleep(time * 1000 / count);
                    }
                });
            }
        }

        /// <summary>
        /// $Kick Command
        /// </summary>
        /// <param name="kickPacket"></param>
        public void Kick(KickPacket kickPacket)
        {
            if (kickPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Kick]CharacterName: {kickPacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Kick]CharacterName: {kickPacket.CharacterName}"
                    });
                }

                if (kickPacket.CharacterName == "*")
                {
                    Parallel.ForEach(ServerManager.Instance.Sessions, session => session.Disconnect());
                }

                ServerManager.Instance.Kick(kickPacket.CharacterName);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(KickPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $KickSession Command
        /// </summary>
        /// <param name="kickSessionPacket"></param>
        public void KickSession(KickSessionPacket kickSessionPacket)
        {
            if (kickSessionPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Kick]AccountName: {kickSessionPacket.AccountName} SessionId: {kickSessionPacket.SessionId}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Kick]AccountName: {kickSessionPacket.AccountName} SessionId: {kickSessionPacket.SessionId}"
                    });
                }

                if (kickSessionPacket.SessionId.HasValue) //if you set the sessionId, remove account verification
                {
                    kickSessionPacket.AccountName = "";
                }

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                AccountDTO account = DAOFactory.AccountDAO.LoadByName(kickSessionPacket.AccountName);
                CommunicationServiceClient.Instance.KickSession(account?.AccountId, kickSessionPacket.SessionId);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(KickSessionPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Kill Command
        /// </summary>
        /// <param name="killPacket"></param>
        public void Kill(KillPacket killPacket)
        {
            if (killPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Kill]CharacterName: {killPacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Kill]CharacterName: {killPacket.CharacterName}"
                    });
                }

                ClientSession sess = ServerManager.Instance.GetSessionByCharacterName(killPacket.CharacterName);
                if (sess != null)
                {
                    if (sess.Character.HasGodMode)
                    {
                        return;
                    }

                    if (sess.Character.Hp < 1)
                    {
                        return;
                    }

                    sess.Character.Hp = 0;
                    sess.Character.LastDefence = DateTime.Now;
                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                        Session.Character.CharacterId, 1, sess.Character.CharacterId, 1114, 4, 11, 4260, 0, 0, false, 0, 60000, 3, 0));
                    sess.SendPacket(sess.Character.GenerateStat());
                    if (sess.Character.IsVehicled)
                    {
                        sess.Character.RemoveVehicle();
                    }
                    ServerManager.Instance.AskRevive(sess.Character.CharacterId);
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(KillPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $LeaveAct4 Command
        /// </summary>
        /// <param name="leaveAct4Packet"></param>
        public void LeaveAct4(LeaveAct4Packet leaveAct4Packet)
        {
            if (leaveAct4Packet != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[LeaveAct4]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[LeaveAct4]"
                    });
                }

                if (Session.Character.Channel.ChannelId == 51)
                {
                    string connection = CommunicationServiceClient.Instance.RetrieveOriginWorld(Session.Character.AccountId);
                    if (string.IsNullOrWhiteSpace(connection))
                    {
                        return;
                    }
                    Session.Character.MapId = 145;
                    Session.Character.MapX = 51;
                    Session.Character.MapY = 41;
                    int port = Convert.ToInt32(connection.Split(':')[1]);
                    Session.Character.ChangeChannel(connection.Split(':')[0], port, 3);
                }
            }

            Session.Character.GenerateSay(LeaveAct4Packet.ReturnHelp(), 10);
        }

        /// <summary>
        /// $PenaltyLog Command
        /// </summary>
        /// <param name="penaltyLogPacket"></param>
        public void ListPenalties(PenaltyLogPacket penaltyLogPacket)
        {
            string returnHelp = CharacterStatsPacket.ReturnHelp();
            if (penaltyLogPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[PenaltyLog]CharacterName: {penaltyLogPacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[PenaltyLog]CharacterName: {penaltyLogPacket.CharacterName}"
                    });
                }

                string name = penaltyLogPacket.CharacterName;
                if (!string.IsNullOrEmpty(name))
                {
                    CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(name);
                    if (character != null)
                    {
                        bool separatorSent = false;

                        void WritePenalty(PenaltyLogDTO penalty)
                        {
                            Session.SendPacket(Session.Character.GenerateSay($"Type: {penalty.Penalty}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"AdminName: {penalty.AdminName}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"Reason: {penalty.Reason}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"DateStart: {penalty.DateStart}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"DateEnd: {penalty.DateEnd}", 13));
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                            separatorSent = true;
                        }

                        IEnumerable<PenaltyLogDTO> penaltyLogs = ServerManager.Instance.PenaltyLogs
                            .Where(s => s.AccountId == character.AccountId).ToList();

                        //PenaltyLogDTO penalty = penaltyLogs.LastOrDefault(s => s.DateEnd > DateTime.Now);
                        Session.SendPacket(Session.Character.GenerateSay("----- PENALTIES -----", 13));

                        #region Warnings

                        Session.SendPacket(Session.Character.GenerateSay("----- WARNINGS -----", 13));
                        foreach (PenaltyLogDTO penaltyLog in penaltyLogs.Where(s => s.Penalty == PenaltyType.Warning)
                            .OrderBy(s => s.DateStart))
                        {
                            WritePenalty(penaltyLog);
                        }

                        if (!separatorSent)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                        }

                        separatorSent = false;

                        #endregion

                        #region Mutes

                        Session.SendPacket(Session.Character.GenerateSay("----- MUTES -----", 13));
                        foreach (PenaltyLogDTO penaltyLog in penaltyLogs.Where(s => s.Penalty == PenaltyType.Muted)
                            .OrderBy(s => s.DateStart))
                        {
                            WritePenalty(penaltyLog);
                        }

                        if (!separatorSent)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                        }

                        separatorSent = false;

                        #endregion

                        #region Bans

                        Session.SendPacket(Session.Character.GenerateSay("----- BANS -----", 13));
                        foreach (PenaltyLogDTO penaltyLog in penaltyLogs.Where(s => s.Penalty == PenaltyType.Banned)
                            .OrderBy(s => s.DateStart))
                        {
                            WritePenalty(penaltyLog);
                        }

                        if (!separatorSent)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                        }

                        #endregion

                        Session.SendPacket(Session.Character.GenerateSay("----- SUMMARY -----", 13));
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"Warnings: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Warning)}", 13));
                        Session.SendPacket(
                            Session.Character.GenerateSay(
                                $"Mutes: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Muted)}", 13));
                        Session.SendPacket(
                            Session.Character.GenerateSay(
                                $"Bans: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Banned)}", 13));
                        Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
            }
        }

        /// <summary>
        /// $MapDance Command
        /// </summary>
        /// <param name="mapDancePacket"></param>
        public void MapDance(MapDancePacket mapDancePacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[MapDance]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[MapDance]"
                });
            }

            if (Session.HasCurrentMapInstance)
            {
                Session.CurrentMapInstance.IsDancing = !Session.CurrentMapInstance.IsDancing;
                if (Session.CurrentMapInstance.IsDancing)
                {
                    Session.Character.Dance();
                    Session.CurrentMapInstance?.Broadcast("dance 2");
                }
                else
                {
                    Session.Character.Dance();
                    Session.CurrentMapInstance?.Broadcast("dance");
                }

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
        }

        /// <summary>
        /// $MapPVP Command
        /// </summary>
        /// <param name="mapPvpPacket"></param>
        public void MapPvp(MapPVPPacket mapPvpPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[MapPVP]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[MapPVP]"
                });
            }

            Session.CurrentMapInstance.IsPVP = !Session.CurrentMapInstance.IsPVP;
            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $MapReset Command
        /// </summary>
        /// <param name="mapResetPacket"></param>
        public void MapReset(MapResetPacket mapResetPacket)
        {
            if (mapResetPacket != null)
            {
                if (Session.Character.IsChangingMapInstance)
                {
                    return;
                }
                if (Session.CurrentMapInstance != null)
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[MapReset]MapId: {Session.CurrentMapInstance.Map.MapId}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[MapReset]MapId: {Session.CurrentMapInstance.Map.MapId}"
                        });
                    }

                    GameObject.MapInstance newMapInstance = ServerManager.ResetMapInstance(Session.CurrentMapInstance);

                    Parallel.ForEach(Session.CurrentMapInstance.Sessions, sess =>
                    ServerManager.Instance.ChangeMapInstance(sess.Character.CharacterId, newMapInstance.MapInstanceId, sess.Character.PositionX, sess.Character.PositionY));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MapResetPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $MapStat Command
        /// </summary>
        /// <param name="mapStatPacket"></param>
        public void MapStats(MapStatisticsPacket mapStatPacket)
        {
            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[MapStat]"
                });
            }

            // lower the boilerplate
            void SendMapStats(MapDTO map, GameObject.MapInstance mapInstance)
            {
                if (map != null && mapInstance != null)
                {
                    Session.SendPacket(Session.Character.GenerateSay("-------------MapData-------------", 10));
                    Session.SendPacket(Session.Character.GenerateSay(
                        $"MapId: {map.MapId}\n" +
                        $"MapMusic: {map.Music}\n" +
                        $"MapName: {map.Name}\n" +
                        $"MapShopAllowed: {map.ShopAllowed}", 10));
                    Session.SendPacket(Session.Character.GenerateSay("---------------------------------", 10));
                    Session.SendPacket(Session.Character.GenerateSay("---------MapInstanceData---------", 10));
                    Session.SendPacket(Session.Character.GenerateSay(
                        $"MapInstanceId: {mapInstance.MapInstanceId}\n" +
                        $"MapInstanceType: {mapInstance.MapInstanceType}\n" +
                        $"MapMonsterCount: {mapInstance.Monsters.Count}\n" +
                        $"MapNpcCount: {mapInstance.Npcs.Count}\n" +
                        $"MapPortalsCount: {mapInstance.Portals.Count}\n" +
                        $"MapInstanceUserShopCount: {mapInstance.UserShops.Count}\n" +
                        $"SessionCount: {mapInstance.Sessions.Count()}\n" +
                        $"MapInstanceXpRate: {mapInstance.XpRate}\n" +
                        $"MapInstanceDropRate: {mapInstance.DropRate}\n" +
                        $"MapInstanceMusic: {mapInstance.InstanceMusic}\n" +
                        $"ShopsAllowed: {mapInstance.ShopAllowed}\n" +
                        $"DropAllowed: {mapInstance.DropAllowed}\n" +
                        $"IsPVP: {mapInstance.IsPVP}\n" +
                        $"IsSleeping: {mapInstance.IsSleeping}\n" +
                        $"Dance: {mapInstance.IsDancing}", 10));
                    Session.SendPacket(Session.Character.GenerateSay("---------------------------------", 10));
                }
            }

            if (mapStatPacket != null)
            {
                if (mapStatPacket.MapId.HasValue)
                {
                    MapDTO map = DAOFactory.MapDAO.LoadById(mapStatPacket.MapId.Value);
                    GameObject.MapInstance mapInstance = ServerManager.GetMapInstanceByMapId(mapStatPacket.MapId.Value);
                    if (map != null && mapInstance != null)
                    {
                        SendMapStats(map, mapInstance);
                    }
                }
                else if (Session.HasCurrentMapInstance)
                {
                    MapDTO map = DAOFactory.MapDAO.LoadById(Session.CurrentMapInstance.Map.MapId);
                    if (map != null)
                    {
                        SendMapStats(map, Session.CurrentMapInstance);
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MapStatisticsPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Miniland Command
        /// </summary>
        /// <param name="minilandPacket"></param>
        public void Miniland(MinilandPacket minilandPacket)
        {
            if (minilandPacket != null)
            {
                if (string.IsNullOrEmpty(minilandPacket.CharacterName))
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Miniland]");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[Miniland]"
                        });
                    }

                    ServerManager.Instance.JoinMiniland(Session, Session);
                }
                else
                {
                    ClientSession session = ServerManager.Instance.GetSessionByCharacterName(minilandPacket.CharacterName);
                    if (session != null)
                    {
                        Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Miniland]CharacterName: {minilandPacket.CharacterName}");

                        if (ServerManager.Instance.Configuration.UseLogService)
                        {
                            LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                            {
                                Sender = Session.Character.Name,
                                SenderId = Session.Character.CharacterId,
                                PacketType = LogType.GMCommand,
                                Packet = $"[Miniland]CharacterName: {minilandPacket.CharacterName}"
                            });
                        }

                        ServerManager.Instance.JoinMiniland(Session, session);
                    }
                }
            }

            Session.Character.GenerateSay(MinilandPacket.ReturnHelp(), 10);
        }

        /// <summary>
        /// $Mob Command
        /// </summary>
        /// <param name="mobPacket"></param>
        public void Mob(MobPacket mobPacket)
        {
            if (mobPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Mob]NpcMonsterVNum: {mobPacket.NpcMonsterVNum} Amount: {mobPacket.Amount} IsMoving: {mobPacket.IsMoving}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Mob]NpcMonsterVNum: {mobPacket.NpcMonsterVNum} Amount: {mobPacket.Amount} IsMoving: {mobPacket.IsMoving}"
                    });
                }

                if (Session.IsOnMap && Session.HasCurrentMapInstance)
                {
                    NpcMonster npcmonster = ServerManager.GetNpcMonster(mobPacket.NpcMonsterVNum);
                    if (npcmonster == null)
                    {
                        return;
                    }

                    Random random = new Random();
                    for (int i = 0; i < mobPacket.Amount; i++)
                    {
                        List<MapCell> possibilities = new List<MapCell>();
                        for (short x = -4; x < 5; x++)
                        {
                            for (short y = -4; y < 5; y++)
                            {
                                possibilities.Add(new MapCell { X = x, Y = y });
                            }
                        }

                        foreach (MapCell possibilitie in possibilities.OrderBy(s => random.Next()))
                        {
                            short mapx = (short)(Session.Character.PositionX + possibilitie.X);
                            short mapy = (short)(Session.Character.PositionY + possibilitie.Y);
                            if (!Session.CurrentMapInstance?.Map.IsBlockedZone(mapx, mapy) ?? false)
                            {
                                break;
                            }
                        }

                        if (Session.CurrentMapInstance != null)
                        {
                            MapMonster monster = new MapMonster
                            {
                                MonsterVNum = mobPacket.NpcMonsterVNum,
                                MapY = Session.Character.PositionY,
                                MapX = Session.Character.PositionX,
                                MapId = Session.Character.MapInstance.Map.MapId,
                                Position = Session.Character.Direction,
                                IsMoving = mobPacket.IsMoving,
                                MapMonsterId = Session.CurrentMapInstance.GetNextMonsterId(),
                                ShouldRespawn = false
                            };
                            monster.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddMonster(monster);
                            Session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MobPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $MobRain Command
        /// </summary>
        /// <param name="mobRain"></param>
        public void MobRain(MobRainPacket mobRainPacket)
        {
            if (mobRainPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[MobRain]NpcMonsterVNum: {mobRainPacket.NpcMonsterVNum} Amount: {mobRainPacket.Amount} IsMoving: {mobRainPacket.IsMoving}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[MobRain]NpcMonsterVNum: {mobRainPacket.NpcMonsterVNum} Amount: {mobRainPacket.Amount} IsMoving: {mobRainPacket.IsMoving}"
                    });
                }

                if (Session.IsOnMap && Session.HasCurrentMapInstance)
                {
                    NpcMonster npcmonster = ServerManager.GetNpcMonster(mobRainPacket.NpcMonsterVNum);
                    if (npcmonster == null)
                    {
                        return;
                    }

                    List<MonsterToSummon> SummonParameters = new List<MonsterToSummon>();
                    SummonParameters.AddRange(Session.Character.MapInstance.Map.GenerateMonsters(mobRainPacket.NpcMonsterVNum, mobRainPacket.Amount, mobRainPacket.IsMoving, new List<EventContainer>()));
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(1), new EventContainer(Session.CurrentMapInstance, EventActionType.SPAWNMONSTERS, SummonParameters));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MobRainPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Morph Command
        /// </summary>
        /// <param name="morphPacket"></param>
        public void Morph(MorphPacket morphPacket)
        {
            if (morphPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Morph]MorphId: {morphPacket.MorphId} MorphDesign: {morphPacket.MorphDesign} Upgrade: {morphPacket.Upgrade} MorphId: {morphPacket.ArenaWinner}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Morph]MorphId: {morphPacket.MorphId} MorphDesign: {morphPacket.MorphDesign} Upgrade: {morphPacket.Upgrade} MorphId: {morphPacket.ArenaWinner}"
                    });
                }

                if (morphPacket.MorphId < 30 && morphPacket.MorphId > 0)
                {
                    Session.Character.UseSp = true;
                    Session.Character.Morph = morphPacket.MorphId;
                    Session.Character.MorphUpgrade = morphPacket.Upgrade;
                    Session.Character.MorphUpgrade2 = morphPacket.MorphDesign;
                    Session.Character.ArenaWinner = morphPacket.ArenaWinner;
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
                }
                else if (morphPacket.MorphId > 30)
                {
                    Session.Character.IsVehicled = true;
                    Session.Character.Morph = morphPacket.MorphId;
                    Session.Character.ArenaWinner = morphPacket.ArenaWinner;
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
                }
                else
                {
                    Session.Character.IsVehicled = false;
                    Session.Character.UseSp = false;
                    Session.Character.ArenaWinner = 0;
                    Session.SendPacket(Session.Character.GenerateCond());
                    Session.SendPacket(Session.Character.GenerateLev());
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MorphPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Mute Command
        /// </summary>
        /// <param name="mutePacket"></param>
        public void Mute(MutePacket mutePacket)
        {
            if (mutePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Mute]CharacterName: {mutePacket.CharacterName} Reason: {mutePacket.Reason} Until: {DateTime.Now.AddMinutes(mutePacket.Duration)}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Mute]CharacterName: {mutePacket.CharacterName} Reason: {mutePacket.Reason} Until: {DateTime.Now.AddMinutes(mutePacket.Duration)}"
                    });
                }

                if (mutePacket.Duration == 0)
                {
                    mutePacket.Duration = 60;
                }

                mutePacket.Reason = mutePacket.Reason?.Trim();
                MuteMethod(mutePacket.CharacterName, mutePacket.Reason, mutePacket.Duration);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MutePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $SNPC Command
        /// </summary>
        /// <param name="NpcPacket"></param>
        public void Npc(NPCPacket NpcPacket)
        {
            if (NpcPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[NPC]NpcMonsterVNum: {NpcPacket.NpcMonsterVNum} Amount: {NpcPacket.Amount} IsMoving: {NpcPacket.IsMoving}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[NPC]NpcMonsterVNum: {NpcPacket.NpcMonsterVNum} Amount: {NpcPacket.Amount} IsMoving: {NpcPacket.IsMoving}"
                    });
                }

                if (Session.IsOnMap && Session.HasCurrentMapInstance)
                {
                    NpcMonster npcmonster = ServerManager.GetNpcMonster(NpcPacket.NpcMonsterVNum);
                    if (npcmonster == null)
                    {
                        return;
                    }

                    Random random = new Random();
                    for (int i = 0; i < NpcPacket.Amount; i++)
                    {
                        List<MapCell> possibilities = new List<MapCell>();
                        for (short x = -4; x < 5; x++)
                        {
                            for (short y = -4; y < 5; y++)
                            {
                                possibilities.Add(new MapCell { X = x, Y = y });
                            }
                        }

                        foreach (MapCell possibilitie in possibilities.OrderBy(s => random.Next()))
                        {
                            short mapx = (short)(Session.Character.PositionX + possibilitie.X);
                            short mapy = (short)(Session.Character.PositionY + possibilitie.Y);
                            if (!Session.CurrentMapInstance?.Map.IsBlockedZone(mapx, mapy) ?? false)
                            {
                                break;
                            }
                        }

                        if (Session.CurrentMapInstance != null)
                        {
                            MapNpc npc = new MapNpc
                            {
                                NpcVNum = NpcPacket.NpcMonsterVNum,
                                MapY = Session.Character.PositionY,
                                MapX = Session.Character.PositionX,
                                MapId = Session.Character.MapInstance.Map.MapId,
                                Position = Session.Character.Direction,
                                IsMoving = NpcPacket.IsMoving,
                                ShouldRespawn = false,
                                MapNpcId = Session.CurrentMapInstance.GetNextNpcId()
                            };
                            npc.Initialize(Session.CurrentMapInstance);
                            Session.CurrentMapInstance.AddNPC(npc);
                            Session.CurrentMapInstance.Broadcast(npc.GenerateIn());
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(NPCPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Packet Command
        /// </summary>
        /// <param name="packetCallbackPacket"></param>
        public void PacketCallBack(PacketCallbackPacket packetCallbackPacket)
        {
            if (packetCallbackPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Packet]Packet: {packetCallbackPacket.Packet}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Packet]Packet: {packetCallbackPacket.Packet}"
                    });
                }

                Session.SendPacket(packetCallbackPacket.Packet);
                Session.SendPacket(Session.Character.GenerateSay(packetCallbackPacket.Packet, 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(PacketCallbackPacket.ReturnHelp(), 10));
            }
        }

        public void PartnerSpXp(PartnerSpXpPacket partnerSpXpPacket)
        {
            if (partnerSpXpPacket == null)
            {
                return;
            }

            Mate mate = Session.Character.Mates?.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner);

            if (mate?.Sp != null)
            {
                mate.Sp.FullXp();
                Session.SendPacket(mate.GenerateScPacket());
            }
        }

        /// <summary>
        /// $Maintenance Command
        /// </summary>
        /// <param name="maintenancePacket"></param>
        public void PlanMaintenance(MaintenancePacket maintenancePacket)
        {
            if (maintenancePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Maintenance]Delay: {maintenancePacket.Delay} Duration: {maintenancePacket.Duration} Reason: {maintenancePacket.Reason}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Maintenance]Delay: {maintenancePacket.Delay} Duration: {maintenancePacket.Duration} Reason: {maintenancePacket.Reason}"
                    });
                }

                DateTime dateStart = DateTime.Now.AddMinutes(maintenancePacket.Delay);
                MaintenanceLogDTO maintenance = new MaintenanceLogDTO
                {
                    DateEnd = dateStart.AddMinutes(maintenancePacket.Duration),
                    DateStart = dateStart,
                    Reason = maintenancePacket.Reason
                };
                DAOFactory.MaintenanceLogDAO.Insert(maintenance);
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MaintenancePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $PortalTo Command
        /// </summary>
        /// <param name="portalToPacket"></param>
        public void PortalTo(PortalToPacket portalToPacket)
        {
            if (portalToPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[PortalTo]DestinationMapId: {portalToPacket.DestinationMapId} DestinationMapX: {portalToPacket.DestinationX} DestinationY: {portalToPacket.DestinationY}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[PortalTo]DestinationMapId: {portalToPacket.DestinationMapId} DestinationMapX: {portalToPacket.DestinationX} DestinationY: {portalToPacket.DestinationY}"
                    });
                }

                AddPortal(portalToPacket.DestinationMapId, portalToPacket.DestinationX, portalToPacket.DestinationY,
                    portalToPacket.PortalType == null ? (short)-1 : (short)portalToPacket.PortalType, false);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(PortalToPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Position Command
        /// </summary>
        /// <param name="positionPacket"></param>
        public void Position(PositionPacket positionPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[Position]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = "[Position]"
                });
            }

            Session.SendPacket(Session.Character.GenerateSay(
                $"Map:{Session.Character.MapInstance.Map.MapId} - X:{Session.Character.PositionX} - Y:{Session.Character.PositionY} - Dir:{Session.Character.Direction} - Cell:{Session.CurrentMapInstance.Map.JaggedGrid[Session.Character.PositionX][Session.Character.PositionY]?.Value}",
                12));
        }

        /// <summary>
        /// $Promote Command
        /// </summary>
        /// <param name="promotePacket"></param>
        public void Promote(PromotePacket promotePacket)
        {
            if (promotePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Promote]CharacterName: {promotePacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Promote]CharacterName: {promotePacket.CharacterName}"
                    });
                }

                string name = promotePacket.CharacterName;
                try
                {
                    AccountDTO account = DAOFactory.AccountDAO.LoadById(DAOFactory.CharacterDAO.LoadByName(name).AccountId);
                    if (account?.Authority >= AuthorityType.User || account.Authority.Equals(AuthorityType.BitchNiggerFaggot))
                    {
                        if (account.Authority < Session.Account.Authority)
                        {
                            AuthorityType newAuthority = AuthorityType.User;
                            switch (account.Authority)
                            {
                                case AuthorityType.User:
                                    newAuthority = AuthorityType.Supporter;
                                    break;

                                case AuthorityType.Supporter:
                                    newAuthority = AuthorityType.DSGM;
                                    break;

                                case AuthorityType.GM:
                                    newAuthority = AuthorityType.SGM;
                                    break;

                                case AuthorityType.SGM:
                                    newAuthority = AuthorityType.GA;
                                    break;

                                case AuthorityType.GA:
                                    newAuthority = AuthorityType.TM;
                                    break;

                                case AuthorityType.TM:
                                    newAuthority = AuthorityType.CM;
                                    break;

                                case AuthorityType.CM:
                                    newAuthority = AuthorityType.DEV;
                                    break;

                                default:
                                    newAuthority = account.Authority;
                                    break;
                            }
                            account.Authority = newAuthority;
                            DAOFactory.AccountDAO.InsertOrUpdate(ref account);
                            ClientSession session =
                                ServerManager.Instance.Sessions.FirstOrDefault(s => s.Character?.Name == name);

                            if (session != null)
                            {
                                session.Account.Authority = newAuthority;
                                session.Character.Authority = newAuthority;
                                ServerManager.Instance.ChangeMap(session.Character.CharacterId);
                                DAOFactory.AccountDAO.WriteGeneralLog(session.Account.AccountId, session.IpAddress,
                                    session.Character.CharacterId, GeneralLogType.Promotion, $"by: {Session.Character.Name}");
                            }
                            else
                            {
                                DAOFactory.AccountDAO.WriteGeneralLog(account.AccountId, "127.0.0.1", null,
                                    GeneralLogType.Promotion, $"by: {Session.Character.Name}");
                            }

                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                        }
                        else
                        {
                            Session.SendPacket(
                                Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_THAT"), 10));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                    }
                }
                catch
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(PromotePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Rarify Command
        /// </summary>
        /// <param name="rarifyPacket"></param>
        public void Rarify(RarifyPacket rarifyPacket)
        {
            if (rarifyPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Rarify]Slot: {rarifyPacket.Slot} Mode: {rarifyPacket.Mode} Protection: {rarifyPacket.Protection}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Rarify]Slot: {rarifyPacket.Slot} Mode: {rarifyPacket.Mode} Protection: {rarifyPacket.Protection}"
                    });
                }

                if (rarifyPacket.Slot >= 0)
                {
                    ItemInstance wearableInstance = Session.Character.Inventory.LoadBySlotAndType(rarifyPacket.Slot, 0);
                    wearableInstance?.RarifyItem(Session, rarifyPacket.Mode, rarifyPacket.Protection);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(RarifyPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ReloadSI Command
        /// </summary>
        /// <param name="reloadSIPacket"></param>
        public void ReloadSI(ReloadSIPacket reloadSIPacket)
        {
            if (reloadSIPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[ReloadSI]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ReloadSI]"
                    });
                }

                ServerManager.Instance.LoadScriptedInstances();
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ReloadSIPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $RemoveMob Packet
        /// </summary>
        /// <param name="removeMobPacket"></param>
        public void RemoveMob(RemoveMobPacket removeMobPacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[RemoveMob]NpcMonsterId: {Session.Character.LastNpcMonsterId}");

                MapMonster monster = Session.CurrentMapInstance.GetMonsterById(Session.Character.LastNpcMonsterId);
                MapNpc npc = Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId);
                if (monster != null)
                {
                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[RemoveMob]NpcMonsterId: {Session.Character.LastNpcMonsterId} MonsterVNum: {monster.MonsterVNum} MapId: {monster.MapId} X: {monster.MapX} Y: {monster.MapY}"
                        });
                    }

                    int distance = Map.GetDistance(new MapCell
                    {
                        X = Session.Character.PositionX,
                        Y = Session.Character.PositionY
                    }, new MapCell
                    {
                        X = monster.MapX,
                        Y = monster.MapY
                    });
                    if (distance > 5)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("TOO_FAR")), 11));
                        return;
                    }

                    if (monster.IsAlive)
                    {
                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                            monster.MapMonsterId));
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("MONSTER_REMOVED"), monster.MapMonsterId,
                                monster.Monster.Name, monster.MapId, monster.MapX, monster.MapY), 12));
                        Session.CurrentMapInstance.RemoveMonster(monster);
                        Session.CurrentMapInstance.RemovedMobNpcList.Add(monster);
                        if (DAOFactory.MapMonsterDAO.LoadById(monster.MapMonsterId) != null)
                        {
                            DAOFactory.MapMonsterDAO.DeleteById(monster.MapMonsterId);
                        }
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("MONSTER_NOT_ALIVE")), 11));
                    }
                }
                else if (npc != null)
                {
                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[RemoveMob]NpcMonsterId: {Session.Character.LastNpcMonsterId} NpcVNum: {npc.NpcVNum} MapId: {npc.MapId} X: {npc.MapX} Y: {npc.MapY}"
                        });
                    }

                    int distance = Map.GetDistance(new MapCell
                    {
                        X = Session.Character.PositionX,
                        Y = Session.Character.PositionY
                    }, new MapCell
                    {
                        X = npc.MapX,
                        Y = npc.MapY
                    });
                    if (distance > 5)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("TOO_FAR")), 11));
                        return;
                    }

                    if (!npc.IsMate && !npc.IsDisabled && !npc.IsProtected)
                    {
                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, npc.MapNpcId));
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("NPCMONSTER_REMOVED"), npc.MapNpcId,
                                npc.Npc.Name, npc.MapId, npc.MapX, npc.MapY), 12));
                        Session.CurrentMapInstance.RemoveNpc(npc);
                        Session.CurrentMapInstance.RemovedMobNpcList.Add(npc);
                        if (DAOFactory.ShopDAO.LoadByNpc(npc.MapNpcId) != null)
                        {
                            DAOFactory.ShopDAO.DeleteByNpcId(npc.MapNpcId);
                        }

                        if (DAOFactory.MapNpcDAO.LoadById(npc.MapNpcId) != null)
                        {
                            DAOFactory.MapNpcDAO.DeleteById(npc.MapNpcId);
                        }
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("NPC_CANNOT_BE_REMOVED")), 11));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NPCMONSTER_NOT_FOUND"), 11));
                }
            }
        }

        /// <summary>
        /// $RemovePortal Command
        /// </summary>
        /// <param name="removePortalPacket"></param>
        public void RemovePortal(RemovePortalPacket removePortalPacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                Portal portal = Session.CurrentMapInstance.Portals.Find(s =>
                    s.SourceMapInstanceId == Session.Character.MapInstanceId && Map.GetDistance(
                        new MapCell { X = s.SourceX, Y = s.SourceY },
                        new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY }) < 10);
                if (portal != null)
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[RemovePortal]MapId: {portal.SourceMapId} MapX: {portal.SourceX} MapY: {portal.SourceY}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[RemovePortal]MapId: {portal.SourceMapId} MapX: {portal.SourceX} MapY: {portal.SourceY}"
                        });
                    }

                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("NEAREST_PORTAL"), portal.SourceMapId,
                            portal.SourceX, portal.SourceY), 12));
                    portal.IsDisabled = true;
                    Session.CurrentMapInstance?.Broadcast(portal.GenerateGp());
                    Session.CurrentMapInstance.Portals.Remove(portal);
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_PORTAL_FOUND"), 11));
                }
            }
        }

        public void RemoveUserLog(RemoveUserLogPacket removeUserLogPacket)
        {
            if (removeUserLogPacket == null
                || string.IsNullOrEmpty(removeUserLogPacket.Username))
            {
                return;
            }

            if (ClientSession.UserLog.Contains(removeUserLogPacket.Username))
            {
                ClientSession.UserLog.RemoveAll(username => username == removeUserLogPacket.Username);
            }

            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $ReputationRate Command
        /// </summary>
        /// <param name="reputationRatePacket"></param>
        public void ReputationRate(ReputationRatePacket reputationRatePacket)
        {
            if (reputationRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[ReputationRate]Value: {reputationRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ReputationRate]Value: {reputationRatePacket.Value}"
                    });
                }

                if (reputationRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateReputation = reputationRatePacket.Value;

                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REPUTATION_RATE_CHANGED"), 0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(GoldRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Resize Command
        /// </summary>
        /// <param name="resizePacket"></param>
        public void Resize(ResizePacket resizePacket)
        {
            if (resizePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Resize]Size: {resizePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Resize]Size: {resizePacket.Value}"
                    });
                }

                if (resizePacket.Value >= 0)
                {
                    Session.Character.Size = resizePacket.Value;
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateScal());
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ResizePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Restart Command
        /// </summary>
        /// <param name="restartPacket"></param>
        public void Restart(RestartPacket restartPacket)
        {
            int time = restartPacket.Time > 0 ? restartPacket.Time : 5;

            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Restart]Time: {time}");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[Restart]Time: {time}"
                });
            }

            if (ServerManager.Instance.TaskShutdown != null)
            {
                ServerManager.Instance.ShutdownStop = true;
                ServerManager.Instance.TaskShutdown = null;
            }
            else
            {
                ServerManager.Instance.IsReboot = true;
                ServerManager.Instance.TaskShutdown = ServerManager.Instance.ShutdownTaskAsync(time);
            }

            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $RestartAll Command
        /// </summary>
        /// <param name="restartAllPacket"></param>
        public void RestartAll(RestartAllPacket restartAllPacket)
        {
            if (restartAllPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[RestartAll]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[RestartAll]"
                    });
                }

                string worldGroup = !string.IsNullOrEmpty(restartAllPacket.WorldGroup) ? restartAllPacket.WorldGroup : ServerManager.Instance.ServerGroup;

                int time = restartAllPacket.Time;

                if (time < 1)
                {
                    time = 5;
                }

                CommunicationServiceClient.Instance.Restart(worldGroup, time);

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(RestartAllPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $SearchItem Command
        /// </summary>
        /// <param name="searchItemPacket"></param>
        public void SearchItem(SearchItemPacket searchItemPacket)
        {
            if (searchItemPacket != null)
            {
                string contents = searchItemPacket.Contents;
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[SearchItem]Contents: {(string.IsNullOrEmpty(contents) ? "none" : contents)}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[SearchItem]Contents: {(string.IsNullOrEmpty(contents) ? "none" : contents)}"
                    });
                }

                string name = "";
                byte page = 0;
                if (!string.IsNullOrEmpty(contents))
                {
                    string[] packetsplit = contents.Split(' ');
                    bool withPage = byte.TryParse(packetsplit[0], out page);
                    name = packetsplit.Length == 1 && withPage
                        ? ""
                        : packetsplit.Skip(withPage ? 1 : 0).Aggregate((a, b) => a + ' ' + b);
                }

                IEnumerable<ItemDTO> itemlist = DAOFactory.ItemDAO.FindByName(name).OrderBy(s => s.VNum)
                    .Skip(page * 200).Take(200).ToList();
                if (itemlist.Any())
                {
                    foreach (ItemDTO item in itemlist)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"[SearchItem:{page}]Item: {(string.IsNullOrEmpty(item.Name) ? "none" : item.Name)} VNum: {item.VNum}",
                            12));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_NOT_FOUND"), 11));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SearchItemPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $SearchMonster Command
        /// </summary>
        /// <param name="searchMonsterPacket"></param>
        public void SearchMonster(SearchMonsterPacket searchMonsterPacket)
        {
            if (searchMonsterPacket != null)
            {
                string contents = searchMonsterPacket.Contents;
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[SearchMonster]Contents: {(string.IsNullOrEmpty(contents) ? "none" : contents)}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[SearchMonster]Contents: {(string.IsNullOrEmpty(contents) ? "none" : contents)}"
                    });
                }

                string name = "";
                byte page = 0;
                if (!string.IsNullOrEmpty(contents))
                {
                    string[] packetsplit = contents.Split(' ');
                    bool withPage = byte.TryParse(packetsplit[0], out page);
                    name = packetsplit.Length == 1 && withPage
                        ? ""
                        : packetsplit.Skip(withPage ? 1 : 0).Aggregate((a, b) => a + ' ' + b);
                }

                IEnumerable<NpcMonsterDTO> monsterlist = DAOFactory.NpcMonsterDAO.FindByName(name)
                    .OrderBy(s => s.NpcMonsterVNum).Skip(page * 200).Take(200).ToList();
                if (monsterlist.Any())
                {
                    foreach (NpcMonsterDTO npcMonster in monsterlist)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"[SearchMonster:{page}]Monster: {(string.IsNullOrEmpty(npcMonster.Name) ? "none" : npcMonster.Name)} VNum: {npcMonster.NpcMonsterVNum}",
                            12));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MONSTER_NOT_FOUND"), 11));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SearchMonsterPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ServerInfo Command
        /// </summary>
        /// <param name="serverInfoPacket"></param>
        public void ServerInfo(ServerInfoPacket serverInfoPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[ServerInfo]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[ServerInfo] ChannelId: {serverInfoPacket.ChannelId ?? 0}"
                });
            }

            Session.SendPacket(Session.Character.GenerateSay($"------------Server Info------------", 11));

            long ActualChannelId = 0;

            CommunicationServiceClient.Instance.GetOnlineCharacters().Where(s => serverInfoPacket.ChannelId == null || s[1] == serverInfoPacket.ChannelId).OrderBy(s => s[1]).ToList().ForEach(s =>
            {
                if (s[1] > ActualChannelId)
                {
                    if (ActualChannelId > 0)
                    {
                        Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
                    }
                    ActualChannelId = s[1];
                    Session.SendPacket(Session.Character.GenerateSay($"-------------Channel:{ActualChannelId}-------------", 11));
                }
                CharacterDTO Character = DAOFactory.CharacterDAO.LoadById(s[0]);
                Session.SendPacket(
                    Session.Character.GenerateSay(
                        $"CharacterName: {Character.Name} | CharacterId: {Character.CharacterId} | SessionId: {s[2]}", 12));
            });

            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
        }

        /// <summary>
        /// $SetPerfection Command
        /// </summary>
        /// <param name="setPerfectionPacket"></param>
        public void SetPerfection(SetPerfectionPacket setPerfectionPacket)
        {
            if (setPerfectionPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[SetPerfection]Slot: {setPerfectionPacket.Slot} Type: {setPerfectionPacket.Type} Value: {setPerfectionPacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[SetPerfection]Slot: {setPerfectionPacket.Slot} Type: {setPerfectionPacket.Type} Value: {setPerfectionPacket.Value}"
                    });
                }

                if (setPerfectionPacket.Slot >= 0)
                {
                    ItemInstance specialistInstance =
                        Session.Character.Inventory.LoadBySlotAndType(setPerfectionPacket.Slot, 0);

                    if (specialistInstance != null)
                    {
                        switch (setPerfectionPacket.Type)
                        {
                            case 0:
                                specialistInstance.SpStoneUpgrade = setPerfectionPacket.Value;
                                break;

                            case 1:
                                specialistInstance.SpDamage = setPerfectionPacket.Value;
                                break;

                            case 2:
                                specialistInstance.SpDefence = setPerfectionPacket.Value;
                                break;

                            case 3:
                                specialistInstance.SpElement = setPerfectionPacket.Value;
                                break;

                            case 4:
                                specialistInstance.SpHP = setPerfectionPacket.Value;
                                break;

                            case 5:
                                specialistInstance.SpFire = setPerfectionPacket.Value;
                                break;

                            case 6:
                                specialistInstance.SpWater = setPerfectionPacket.Value;
                                break;

                            case 7:
                                specialistInstance.SpLight = setPerfectionPacket.Value;
                                break;

                            case 8:
                                specialistInstance.SpDark = setPerfectionPacket.Value;
                                break;

                            default:
                                Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(),
                                    10));
                                break;
                        }
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Shout Command
        /// </summary>
        /// <param name="shoutPacket"></param>
        public void Shout(ShoutPacket shoutPacket)
        {
            if (shoutPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Shout]Message: {shoutPacket.Message}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Shout]Message: {shoutPacket.Message}"
                    });
                }

                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = null,
                    SourceCharacterId = Session.Character.CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = shoutPacket.Message,
                    Type = MessageType.Shout
                });
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ShoutPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $ShoutHere Command
        /// </summary>
        /// <param name="shoutHerePacket"></param>
        public void ShoutHere(ShoutHerePacket shoutHerePacket)
        {
            if (shoutHerePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[ShoutHere]Message: {shoutHerePacket.Message}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ShoutHere]Message: {shoutHerePacket.Message}"
                    });
                }

                ServerManager.Shout(shoutHerePacket.Message);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ShoutHerePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Shutdown Command
        /// </summary>
        /// <param name="shutdownPacket"></param>
        public void Shutdown(ShutdownPacket shutdownPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Shutdown]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[Shutdown]"
                });
            }

            if (ServerManager.Instance.TaskShutdown != null)
            {
                ServerManager.Instance.ShutdownStop = true;
                ServerManager.Instance.TaskShutdown = null;
            }
            else
            {
                ServerManager.Instance.TaskShutdown = ServerManager.Instance.ShutdownTaskAsync();
                ServerManager.Instance.TaskShutdown.Start();
            }
        }

        /// <summary>
        /// $ShutdownAll Command
        /// </summary>
        /// <param name="shutdownAllPacket"></param>
        public void ShutdownAll(ShutdownAllPacket shutdownAllPacket)
        {
            if (shutdownAllPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[ShutdownAll]");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[ShutdownAll]"
                    });
                }

                if (!string.IsNullOrEmpty(shutdownAllPacket.WorldGroup))
                {
                    CommunicationServiceClient.Instance.Shutdown(shutdownAllPacket.WorldGroup);
                }
                else
                {
                    CommunicationServiceClient.Instance.Shutdown(ServerManager.Instance.ServerGroup);
                }

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ShutdownAllPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Sort Command
        /// </summary>
        /// <param name="sortPacket"></param>
        public void Sort(SortPacket sortPacket)
        {
            if (sortPacket?.InventoryType.HasValue == true)
            {
                Logger.LogUserEvent("USERCOMMAND", Session.GenerateIdentity(),
                    $"[Sort]InventoryType: {sortPacket.InventoryType}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.UserCommand,
                        Packet = $"[Sort]InventoryType: {sortPacket.InventoryType}"
                    });
                }

                if (sortPacket.InventoryType == InventoryType.Equipment
                    || sortPacket.InventoryType == InventoryType.Etc || sortPacket.InventoryType == InventoryType.Main)
                {
                    Session.Character.Inventory.Reorder(Session, sortPacket.InventoryType.Value);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SortPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Speed Command
        /// </summary>
        /// <param name="speedPacket"></param>
        public void Speed(SpeedPacket speedPacket)
        {
            if (speedPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Speed]Value: {speedPacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Speed]Value: {speedPacket.Value}"
                    });
                }

                if (speedPacket.Value < 60)
                {
                    Session.Character.Speed = speedPacket.Value;
                    Session.Character.IsCustomSpeed = true;
                    Session.SendPacket(Session.Character.GenerateCond());
                }
                if (speedPacket.Value == 0)
                {
                    Session.Character.IsCustomSpeed = false;
                    Session.Character.LoadSpeed();
                    Session.SendPacket(Session.Character.GenerateCond());
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SpeedPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $SPRefill Command
        /// </summary>
        /// <param name="spRefillPacket"></param>
        public void SpRefill(SPRefillPacket spRefillPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[SPRefill]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[SPRefill]"
                });
            }

            Session.Character.SpPoint = 10000;
            Session.Character.SpAdditionPoint = 1000000;
            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_REFILL"), 0));
            Session.SendPacket(Session.Character.GenerateSpPoint());
        }

        /// <summary>
        /// $Event Command
        /// </summary>
        /// <param name="eventPacket"></param>
        public void StartEvent(EventPacket eventPacket)
        {
            if (eventPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Event]EventType: {eventPacket.EventType.ToString()}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Event]EventType: {eventPacket.EventType.ToString()}"
                    });
                }

                if (eventPacket.LvlBracket >= 0)
                {
                    EventHelper.GenerateEvent(eventPacket.EventType, eventPacket.LvlBracket);
                }
                else
                {
                    EventHelper.GenerateEvent(eventPacket.EventType);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(EventPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $GlobalEvent Command
        /// </summary>
        /// <param name="globalEventPacket"></param>
        public void StartGlobalEvent(GlobalEventPacket globalEventPacket)
        {
            if (globalEventPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[GlobalEvent]EventType: {globalEventPacket.EventType.ToString()}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[GlobalEvent]EventType: {globalEventPacket.EventType.ToString()}"
                    });
                }

                CommunicationServiceClient.Instance.RunGlobalEvent(globalEventPacket.EventType);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(EventPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Stat Command
        /// </summary>
        /// <param name="statCommandPacket"></param>
        public void Stat(StatCommandPacket statCommandPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Stat]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[Stat]"
                });
            }

            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("XP_RATE_NOW")}: {ServerManager.Instance.Configuration.RateXP} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("DROP_RATE_NOW")}: {ServerManager.Instance.Configuration.RateDrop} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("GOLD_RATE_NOW")}: {ServerManager.Instance.Configuration.RateGold} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("GOLD_DROPRATE_NOW")}: {ServerManager.Instance.Configuration.RateGoldDrop} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("HERO_XPRATE_NOW")}: {ServerManager.Instance.Configuration.RateHeroicXP} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("FAIRYXP_RATE_NOW")}: {ServerManager.Instance.Configuration.RateFairyXP} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("REPUTATION_RATE_NOW")}: {ServerManager.Instance.Configuration.RateReputation} ",
                13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("SERVER_WORKING_TIME")}: {(Process.GetCurrentProcess().StartTime - DateTime.Now).ToString(@"d\ hh\:mm\:ss")} ",
                13));

            foreach (string message in CommunicationServiceClient.Instance.RetrieveServerStatistics())
            {
                Session.SendPacket(Session.Character.GenerateSay(message, 13));
            }
        }

        /// <summary>
        /// $Sudo Command
        /// </summary>
        /// <param name="sudoPacket"></param>
        public void SudoCommand(SudoPacket sudoPacket)
        {
            if (sudoPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Sudo]CharacterName: {sudoPacket.CharacterName} CommandContents:{sudoPacket.CommandContents}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Sudo]CharacterName: {sudoPacket.CharacterName} CommandContents:{sudoPacket.CommandContents}"
                    });
                }

                if (sudoPacket.CharacterName == "*")
                {
                    foreach (ClientSession sess in Session.CurrentMapInstance.Sessions.ToList().Where(s => s.Character?.Authority <= Session.Character.Authority))
                    {
                        sess.ReceivePacket(sudoPacket.CommandContents, true);
                    }
                }
                else
                {
                    ClientSession session = ServerManager.Instance.GetSessionByCharacterName(sudoPacket.CharacterName);

                    if (session != null && !string.IsNullOrWhiteSpace(sudoPacket.CommandContents))
                    {
                        if (session.Character?.Authority <= Session.Character.Authority)
                        {
                            session.ReceivePacket(sudoPacket.CommandContents, true);
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_DO_THAT"), 0));
                        }
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"), 0));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SudoPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Summon Command
        /// </summary>
        /// <param name="summonPacket"></param>
        public void Summon(SummonPacket summonPacket)
        {
            Random random = new Random();
            if (summonPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Summon]CharacterName: {summonPacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Summon]CharacterName: {summonPacket.CharacterName}"
                    });
                }

                if (summonPacket.CharacterName == "*")
                {
                    Parallel.ForEach(
                        ServerManager.Instance.Sessions.Where(s =>
                            s.Character != null && s.Character.CharacterId != Session.Character.CharacterId), session =>
                        {
                            // clear any shop or trade on target character
                            Session.Character.DisposeShopAndExchange();
                            if (!session.Character.IsChangingMapInstance && Session.HasCurrentMapInstance)
                            {
                                List<MapCell> possibilities = new List<MapCell>();
                                for (short x = -6, y = -6; x < 6 && y < 6; x++, y++)
                                {
                                    possibilities.Add(new MapCell { X = x, Y = y });
                                }

                                short mapXPossibility = Session.Character.PositionX;
                                short mapYPossibility = Session.Character.PositionY;
                                foreach (MapCell possibility in possibilities.OrderBy(s => random.Next()))
                                {
                                    mapXPossibility = (short)(Session.Character.PositionX + possibility.X);
                                    mapYPossibility = (short)(Session.Character.PositionY + possibility.Y);
                                    if (!Session.CurrentMapInstance.Map.IsBlockedZone(mapXPossibility, mapYPossibility))
                                    {
                                        break;
                                    }
                                }

                                if (Session.Character.Miniland == Session.Character.MapInstance)
                                {
                                    ServerManager.Instance.JoinMiniland(session, Session);
                                }
                                else
                                {
                                    ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                        Session.Character.MapInstanceId, mapXPossibility, mapYPossibility);
                                }
                            }
                        });
                }
                else
                {
                    ClientSession targetSession =
                        ServerManager.Instance.GetSessionByCharacterName(summonPacket.CharacterName);
                    if (targetSession?.Character.IsChangingMapInstance == false)
                    {
                        Session.Character.DisposeShopAndExchange();
                        ServerManager.Instance.ChangeMapInstance(targetSession.Character.CharacterId,
                            Session.Character.MapInstanceId, (short)(Session.Character.PositionX + 1),
                            (short)(Session.Character.PositionY + 1));
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"), 0));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SummonPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Teleport Command
        /// </summary>
        /// <param name="teleportPacket"></param>
        public void Teleport(TeleportPacket teleportPacket)
        {
            if (teleportPacket != null)
            {
                if (Session.Character.HasShopOpened || Session.Character.InExchangeOrTrade)
                {
                    Session.Character.DisposeShopAndExchange();
                }

                if (Session.Character.IsChangingMapInstance)
                {
                    return;
                }

                ClientSession session = ServerManager.Instance.GetSessionByCharacterName(teleportPacket.Data);

                if (session != null)
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[Teleport]CharacterName: {teleportPacket.Data}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[Teleport]CharacterName: {teleportPacket.Data}"
                        });
                    }

                    short mapX = session.Character.PositionX;
                    short mapY = session.Character.PositionY;
                    if (session.Character.Miniland == session.Character.MapInstance)
                    {
                        ServerManager.Instance.JoinMiniland(Session, session);
                    }
                    else
                    {
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                            session.Character.MapInstanceId, mapX, mapY);
                    }
                }
                else if (short.TryParse(teleportPacket.Data, out short mapId))
                {
                    Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                        $"[Teleport]MapId: {teleportPacket.Data} MapX: {teleportPacket.X} MapY: {teleportPacket.Y}");

                    if (ServerManager.Instance.Configuration.UseLogService)
                    {
                        LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                        {
                            Sender = Session.Character.Name,
                            SenderId = Session.Character.CharacterId,
                            PacketType = LogType.GMCommand,
                            Packet = $"[Teleport]MapId: {teleportPacket.Data} MapX: {teleportPacket.X} MapY: {teleportPacket.Y}"
                        });
                    }

                    if (ServerManager.GetBaseMapInstanceIdByMapId(mapId) != default)
                    {
                        if (teleportPacket.X == 0 && teleportPacket.Y == 0)
                        {
                            ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, ServerManager.GetBaseMapInstanceIdByMapId(mapId));
                        }
                        else
                        {
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, mapId, teleportPacket.X, teleportPacket.Y);
                        }
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAP_NOT_FOUND"), 0));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(TeleportPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Unban Command
        /// </summary>
        /// <param name="unbanPacket"></param>
        public void Unban(UnbanPacket unbanPacket)
        {
            if (unbanPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Unban]CharacterName: {unbanPacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Unban]CharacterName: {unbanPacket.CharacterName}"
                    });
                }

                string name = unbanPacket.CharacterName;
                CharacterDTO chara = DAOFactory.CharacterDAO.LoadByName(name);
                if (chara != null)
                {
                    PenaltyLogDTO log = ServerManager.Instance.PenaltyLogs.Find(s =>
                        s.AccountId == chara.AccountId && s.Penalty == PenaltyType.Banned && s.DateEnd > DateTime.Now);
                    if (log != null)
                    {
                        log.DateEnd = DateTime.Now.AddSeconds(-1);
                        Character.InsertOrUpdatePenalty(log);
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"),
                            10));
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_BANNED"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UnbanPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Undercover Command
        /// </summary>
        /// <param name="undercoverPacket"></param>
        public void Undercover(UndercoverPacket undercoverPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Undercover]");

            if (ServerManager.Instance.Configuration.UseLogService)
            {
                LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                {
                    Sender = Session.Character.Name,
                    SenderId = Session.Character.CharacterId,
                    PacketType = LogType.GMCommand,
                    Packet = $"[Undercover]"
                });
            }

            Session.Character.Undercover = !Session.Character.Undercover;
            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, Session.CurrentMapInstance.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY);
        }

        /// <summary>
        /// $Unmute Command
        /// </summary>
        /// <param name="unmutePacket"></param>
        public void Unmute(UnmutePacket unmutePacket)
        {
            if (unmutePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Unmute]CharacterName: {unmutePacket.CharacterName}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Unmute]CharacterName: {unmutePacket.CharacterName}"
                    });
                }

                string name = unmutePacket.CharacterName;
                CharacterDTO chara = DAOFactory.CharacterDAO.LoadByName(name);
                if (chara != null)
                {
                    if (ServerManager.Instance.PenaltyLogs.Any(s =>
                        s.AccountId == chara.AccountId && s.Penalty == (byte)PenaltyType.Muted
                        && s.DateEnd > DateTime.Now))
                    {
                        PenaltyLogDTO log = ServerManager.Instance.PenaltyLogs.Find(s =>
                            s.AccountId == chara.AccountId && s.Penalty == (byte)PenaltyType.Muted
                            && s.DateEnd > DateTime.Now);
                        if (log != null)
                        {
                            log.DateEnd = DateTime.Now.AddSeconds(-1);
                            Character.InsertOrUpdatePenalty(log);
                        }

                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"),
                            10));
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_MUTED"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UnmutePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Unstuck Command
        /// </summary>
        /// <param name="unstuckPacket"></param>
        public void Unstuck(UnstuckPacket unstuckPacket)
        {
            if (Session?.Character != null)
            {
                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.UserCommand,
                        Packet = $"[Unstuck]"
                    });
                }

                if (Session.Character.Miniland == Session.Character.MapInstance)
                {
                    ServerManager.Instance.JoinMiniland(Session, Session);
                }
                else if (!Session.Character.IsSeal
                      && !Session.CurrentMapInstance.MapInstanceType.Equals(MapInstanceType.TalentArenaMapInstance)
                      && !Session.CurrentMapInstance.MapInstanceType.Equals(MapInstanceType.IceBreakerInstance))
                {
                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                        Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                        true);
                    Session.SendPacket(StaticPacketHelper.Cancel(2));
                }
            }
        }

        /// <summary>
        /// $Upgrade Command
        /// </summary>
        /// <param name="upgradePacket"></param>
        public void Upgrade(UpgradeCommandPacket upgradePacket)
        {
            if (upgradePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Upgrade]Slot: {upgradePacket.Slot} Mode: {upgradePacket.Mode} Protection: {upgradePacket.Protection}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Upgrade]Slot: {upgradePacket.Slot} Mode: {upgradePacket.Mode} Protection: {upgradePacket.Protection}"
                    });
                }

                if (upgradePacket.Slot >= 0)
                {
                    ItemInstance wearableInstance =
                        Session.Character.Inventory.LoadBySlotAndType(upgradePacket.Slot, 0);
                    wearableInstance?.UpgradeItem(Session, upgradePacket.Mode, upgradePacket.Protection, true);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
            }
        }

        public void UserLog(UserLogPacket userLogPacket)
        {
            if (userLogPacket == null)
            {
                return;
            }

            int n = 1;

            foreach (string username in ClientSession.UserLog)
            {
                Session.SendPacket(Session.Character.GenerateSay($"{n++}- {username}", 12));
            }

            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
        }

        /// <summary>
        /// $Warn Command
        /// </summary>
        /// <param name="warningPacket"></param>
        public void Warn(WarningPacket warningPacket)
        {
            if (warningPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[Warn]CharacterName: {warningPacket.CharacterName} Reason: {warningPacket.Reason}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Warn]CharacterName: {warningPacket.CharacterName} Reason: {warningPacket.Reason}"
                    });
                }

                string characterName = warningPacket.CharacterName;
                CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(characterName);
                if (character != null)
                {
                    ClientSession session = ServerManager.Instance.GetSessionByCharacterName(characterName);
                    session?.SendPacket(UserInterfaceHelper.GenerateInfo(
                        string.Format(Language.Instance.GetMessageFromKey("WARNING"), warningPacket.Reason)));
                    Character.InsertOrUpdatePenalty(new PenaltyLogDTO
                    {
                        AccountId = character.AccountId,
                        Reason = warningPacket.Reason,
                        Penalty = PenaltyType.Warning,
                        DateStart = DateTime.Now,
                        DateEnd = DateTime.Now,
                        AdminName = Session.Character.Name
                    });
                    switch (DAOFactory.PenaltyLogDAO.LoadByAccount(character.AccountId)
                        .Count(p => p.Penalty == PenaltyType.Warning))
                    {
                        case 1:
                            break;

                        case 2:
                            MuteMethod(characterName, "Auto-Warning mute: 2 strikes", 30);
                            break;

                        case 3:
                            MuteMethod(characterName, "Auto-Warning mute: 3 strikes", 60);
                            break;

                        case 4:
                            MuteMethod(characterName, "Auto-Warning mute: 4 strikes", 720);
                            break;

                        case 5:
                            MuteMethod(characterName, "Auto-Warning mute: 5 strikes", 1440);
                            break;

                        case 69:
                            BanMethod(characterName, 7, "LOL SIXTY NINE AMIRITE?");
                            break;

                        default:
                            MuteMethod(characterName, "You've been THUNDERSTRUCK",
                                6969); // imagined number as for I = √(-1), complex z = a + bi
                            break;
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(WarningPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $WigColor Command
        /// </summary>
        /// <param name="wigColorPacket"></param>
        public void WigColor(WigColorPacket wigColorPacket)
        {
            if (wigColorPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                    $"[WigColor]Color: {wigColorPacket.Color}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[WigColor]Color: {wigColorPacket.Color}"
                    });
                }

                ItemInstance wig =
                    Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Hat, InventoryType.Wear);
                if (wig != null)
                {
                    wig.Design = wigColorPacket.Color;
                    Session.SendPacket(Session.Character.GenerateEq());
                    Session.SendPacket(Session.Character.GenerateEquipment());
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateIn());
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_WIG"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(WigColorPacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $XpRate Command
        /// </summary>
        /// <param name="xpRatePacket"></param>
        public void XpRate(XpRatePacket xpRatePacket)
        {
            if (xpRatePacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[XpRate]Value: {xpRatePacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[XpRate]Value: {xpRatePacket.Value}"
                    });
                }

                if (xpRatePacket.Value <= 1000)
                {
                    ServerManager.Instance.Configuration.RateXP = xpRatePacket.Value;

                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("XP_RATE_CHANGED"), 0));
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("WRONG_VALUE"), 0));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(XpRatePacket.ReturnHelp(), 10));
            }
        }

        /// <summary>
        /// $Zoom Command
        /// </summary>
        /// <param name="zoomPacket"></param>
        public void Zoom(ZoomPacket zoomPacket)
        {
            if (zoomPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Zoom]Value: {zoomPacket.Value}");

                if (ServerManager.Instance.Configuration.UseLogService)
                {
                    LogServiceClient.Instance.LogPacket(new PacketLogEntry()
                    {
                        Sender = Session.Character.Name,
                        SenderId = Session.Character.CharacterId,
                        PacketType = LogType.GMCommand,
                        Packet = $"[Zoom]Value: {zoomPacket.Value}"
                    });
                }

                Session.SendPacket(
                    UserInterfaceHelper.GenerateGuri(15, zoomPacket.Value, Session.Character.CharacterId));
            }

            Session.Character.GenerateSay(ZoomPacket.ReturnHelp(), 10);
        }

        /// <summary>
        /// private addMate method
        /// </summary>
        /// <param name="vnum"></param>
        /// <param name="level"></param>
        /// <param name="mateType"></param>
        private void AddMate(short vnum, byte level, MateType mateType)
        {
            NpcMonster mateNpc = ServerManager.GetNpcMonster(vnum);
            if (Session.CurrentMapInstance == Session.Character.Miniland && mateNpc != null)
            {
                level = level == 0 ? (byte)1 : level;
                Mate mate = new Mate(Session.Character, mateNpc, level, mateType);
                Session.Character.AddPet(mate);
            }
            else
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_IN_MINILAND"), 0));
            }
        }

        /// <summary>
        /// private add portal command
        /// </summary>
        /// <param name="destinationMapId"></param>
        /// <param name="destinationX"></param>
        /// <param name="destinationY"></param>
        /// <param name="type"></param>
        /// <param name="insertToDatabase"></param>
        private void AddPortal(short destinationMapId, short destinationX, short destinationY, short type,
            bool insertToDatabase)
        {
            if (Session.HasCurrentMapInstance)
            {
                Portal portal = new Portal
                {
                    SourceMapId = Session.Character.MapId,
                    SourceX = Session.Character.PositionX,
                    SourceY = Session.Character.PositionY,
                    DestinationMapId = destinationMapId,
                    DestinationX = destinationX,
                    DestinationY = destinationY,
                    DestinationMapInstanceId = insertToDatabase ? Guid.Empty :
                        destinationMapId == 20000 ? Session.Character.Miniland.MapInstanceId : Guid.Empty,
                    Type = type
                };
                if (insertToDatabase)
                {
                    DAOFactory.PortalDAO.Insert(portal);
                }

                Session.CurrentMapInstance.Portals.Add(portal);
                Session.CurrentMapInstance?.Broadcast(portal.GenerateGp());
            }
        }

        /// <summary>
        /// private ban method
        /// </summary>
        /// <param name="characterName"></param>
        /// <param name="duration"></param>
        /// <param name="reason"></param>
        private void BanMethod(string characterName, int duration, string reason)
        {
            CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(characterName);
            if (character != null)
            {
                ServerManager.Instance.Kick(characterName);
                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = character.AccountId,
                    Reason = reason?.Trim(),
                    Penalty = PenaltyType.Banned,
                    DateStart = DateTime.Now,
                    DateEnd = duration == 0 ? DateTime.Now.AddYears(15) : DateTime.Now.AddDays(duration),
                    AdminName = Session.Character.Name
                };
                Character.InsertOrUpdatePenalty(log);
                ServerManager.Instance.BannedCharacters.Add(character.CharacterId);
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"),
                    10));
            }
        }

        /// <summary>
        /// private mute method
        /// </summary>
        /// <param name="characterName"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        private void MuteMethod(string characterName, string reason, int duration)
        {
            CharacterDTO characterToMute = DAOFactory.CharacterDAO.LoadByName(characterName);
            if (characterToMute != null)
            {
                ClientSession session = ServerManager.Instance.GetSessionByCharacterName(characterName);
                if (session?.Character.IsMuted() == false)
                {
                    session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        string.Format(Language.Instance.GetMessageFromKey("MUTED_PLURAL"), reason, duration)));
                }

                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = characterToMute.AccountId,
                    Reason = reason,
                    Penalty = PenaltyType.Muted,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddMinutes(duration),
                    AdminName = Session.Character.Name
                };
                Character.InsertOrUpdatePenalty(log);
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"),
                    10));
            }
        }

        /// <summary>
        /// Helper method used for sending stats of desired character
        /// </summary>
        /// <param name="characterDto"></param>
        private void SendStats(CharacterDTO characterDto)
        {
            Session.SendPacket(Session.Character.GenerateSay("----- CHARACTER -----", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Name: {characterDto.Name}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Id: {characterDto.CharacterId}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"State: {characterDto.State}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Gender: {characterDto.Gender}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Class: {characterDto.Class}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Level: {characterDto.Level}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"JobLevel: {characterDto.JobLevel}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"HeroLevel: {characterDto.HeroLevel}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Gold: {characterDto.Gold}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Bio: {characterDto.Biography}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"MapId: {characterDto.MapId}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"MapX: {characterDto.MapX}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"MapY: {characterDto.MapY}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Reputation: {characterDto.Reputation}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Dignity: {characterDto.Dignity}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Rage: {characterDto.RagePoint}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Compliment: {characterDto.Compliment}", 13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"Fraction: {(characterDto.Faction == FactionType.Demon ? Language.Instance.GetMessageFromKey("DEMON") : Language.Instance.GetMessageFromKey("ANGEL"))}",
                13));
            Session.SendPacket(Session.Character.GenerateSay("----- --------- -----", 13));
            AccountDTO account = DAOFactory.AccountDAO.LoadById(characterDto.AccountId);
            if (account != null)
            {
                Session.SendPacket(Session.Character.GenerateSay("----- ACCOUNT -----", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Id: {account.AccountId}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Name: {account.Name}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Authority: {account.Authority}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"RegistrationIP: {account.RegistrationIP}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Email: {account.Email}", 13));
                Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                IEnumerable<PenaltyLogDTO> penaltyLogs = ServerManager.Instance.PenaltyLogs
                    .Where(s => s.AccountId == account.AccountId).ToList();
                PenaltyLogDTO penalty = penaltyLogs.LastOrDefault(s => s.DateEnd > DateTime.Now);
                Session.SendPacket(Session.Character.GenerateSay("----- PENALTY -----", 13));
                if (penalty != null)
                {
                    Session.SendPacket(Session.Character.GenerateSay($"Type: {penalty.Penalty}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"AdminName: {penalty.AdminName}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"Reason: {penalty.Reason}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"DateStart: {penalty.DateStart}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"DateEnd: {penalty.DateEnd}", 13));
                }

                Session.SendPacket(
                    Session.Character.GenerateSay($"Bans: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Banned)}",
                        13));
                Session.SendPacket(
                    Session.Character.GenerateSay($"Mutes: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Muted)}",
                        13));
                Session.SendPacket(
                    Session.Character.GenerateSay(
                        $"Warnings: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Warning)}", 13));
                Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
            }

            Session.SendPacket(Session.Character.GenerateSay("----- SESSION -----", 13));
            foreach (long[] connection in CommunicationServiceClient.Instance.RetrieveOnlineCharacters(characterDto
                .CharacterId))
            {
                if (connection != null)
                {
                    CharacterDTO character = DAOFactory.CharacterDAO.LoadById(connection[0]);
                    if (character != null)
                    {
                        Session.SendPacket(Session.Character.GenerateSay($"Character Name: {character.Name}", 13));
                        Session.SendPacket(Session.Character.GenerateSay($"ChannelId: {connection[1]}", 13));
                        Session.SendPacket(Session.Character.GenerateSay("-----", 13));
                    }
                }
            }

            Session.SendPacket(Session.Character.GenerateSay("----- ------------ -----", 13));
        }

        #endregion
    }
}