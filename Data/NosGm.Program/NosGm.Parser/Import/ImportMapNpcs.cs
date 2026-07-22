using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportMapNpcs : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportMapNpcs(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Import()
        {
            var existingNpcMonsters = DAOFactory.NpcMonsterDAO.LoadAll().Select(x => x.NpcMonsterVNum).ToHashSet();
            var existingMapNpcs = DAOFactory.MapNpcDAO.LoadAll().Select(x => x.MapNpcId).ToHashSet();
            var existingTeleporters = DAOFactory.TeleporterDAO.LoadAll().ToList();

            var npcCounter = 0;
            short map = 0;

            var npcs = new List<MapNpcDTO>();
            var teleporters = new List<TeleporterDTO>();
            var npcMvPacketsList = new List<int>();
            var effPacketsDictionary = new Dictionary<int, short>();

            if (!existingMapNpcs.Contains(0))
            {
                npcs.Add(new MapNpcDTO
                {
                    MapX = 102,
                    MapY = 154,
                    MapId = 5,
                    NpcVNum = 860,
                    Position = 2,
                    IsMoving = false,
                    EffectDelay = 4750,
                    Dialog = 999 // unused dialog
                });
            }

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("mv") && o[1].Equals("2")))
            {
                if (long.Parse(currentPacket[2]) >= 20000) continue;

                if (!npcMvPacketsList.Contains(Convert.ToInt32(currentPacket[2])))
                    npcMvPacketsList.Add(Convert.ToInt32(currentPacket[2]));
            }

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("eff") && o[1].Equals("2")))
            {
                if (long.Parse(currentPacket[2]) >= 20000) continue;

                if (!effPacketsDictionary.ContainsKey(Convert.ToInt32(currentPacket[2])))
                    effPacketsDictionary.Add(Convert.ToInt32(currentPacket[2]), Convert.ToInt16(currentPacket[3]));
            }

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("in") || o[0].Equals("at")))
            {
                if (currentPacket.Length > 5 && currentPacket[0] == "at")
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                if (DAOFactory.MapDAO.LoadById(map) == null)
                {
                    continue;
                }

                if (currentPacket.Length < 7 || currentPacket[0] != "in" || currentPacket[1] != "2") continue;

                var npc = new MapNpcDTO
                {
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    MapId = map,
                    NpcVNum = short.Parse(currentPacket[2])
                };

                if (long.Parse(currentPacket[3]) > 20000) continue;

                npc.MapNpcId = short.Parse(currentPacket[3]);
                if (effPacketsDictionary.ContainsKey(npc.MapNpcId))
                    npc.Effect = (short)(npc.NpcVNum == 453 /*Lod*/ ? 855 : effPacketsDictionary[npc.MapNpcId]);

                npc.EffectDelay = 4750;
                npc.IsMoving = npcMvPacketsList.Contains(npc.MapNpcId);
                npc.Position = byte.Parse(currentPacket[6]);
                npc.Dialog = short.Parse(currentPacket[9]);
                npc.IsSitting = currentPacket[13] != "1";
                npc.IsDisabled = false;

                if (!existingNpcMonsters.Contains(npc.NpcVNum)) continue;

                if (existingMapNpcs.Contains(npc.MapNpcId) || npcs.Any(x => x.MapNpcId == npc.MapNpcId)) continue;

                // Levers teleporters
                if (npc.MapId == 51 && npc.MapX == 90 && npc.MapY == 9)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 18,
                        MapY = 11
                    });
                }
                if (npc.MapId == 51 && npc.MapX == 18 && npc.MapY == 10)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 90,
                        MapY = 10
                    });
                }
                if (npc.MapId == 51 && npc.MapX == 38 && npc.MapY == 19)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 29,
                        MapY = 45
                    });
                }
                if (npc.MapId == 51 && npc.MapX == 29 && npc.MapY == 43)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 38,
                        MapY = 20
                    });
                }
                if (npc.MapId == 85 && npc.MapX == 7 && npc.MapY == 18)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 41,
                        MapY = 33
                    });
                }
                if (npc.MapId == 85 && npc.MapX == 40 && npc.MapY == 32)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 7,
                        MapY = 20
                    });
                }
                if (npc.MapId == 85 && npc.MapX == 45 && npc.MapY == 44)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 6,
                        MapY = 56
                    });
                }
                if (npc.MapId == 85 && npc.MapX == 5 && npc.MapY == 55)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 44,
                        MapY = 45
                    });
                }
                if (npc.MapId == 85 && npc.MapX == 10 && npc.MapY == 69)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 44,
                        MapY = 78
                    });
                }
                if (npc.MapId == 85 && npc.MapX == 43 && npc.MapY == 77)
                {
                    teleporters.Add(new TeleporterDTO
                    {
                        Index = 0,
                        Type = 0,
                        MapNpcId = npc.MapNpcId,
                        MapId = npc.MapId,
                        MapX = 10,
                        MapY = 70
                    });
                }

                npcs.Add(npc);
                npcCounter++;
            }

            DAOFactory.MapNpcDAO.Insert(npcs);
            foreach (var tp in teleporters.Where(tp => !existingTeleporters.Any(x => x.MapNpcId == tp.MapNpcId && x.Index == tp.Index)))
            {
                DAOFactory.TeleporterDAO.Insert(tp);
            }

            Logger.Log.Info($"{npcCounter} MapNpcs parsed");
        }
    }
}