using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportMonsters : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportMonsters(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }


        public void Import()
        {
            var existingNpcMonsters = DAOFactory.NpcMonsterDAO.LoadAll().Select(x => x.NpcMonsterVNum).ToHashSet();
            var existingMapMonsters = DAOFactory.MapMonsterDAO.LoadAll().Select(x => x.MapMonsterId).ToHashSet();

            var monsterCounter = 0;
            short map = 0;

            var mobMvPacketsList = new List<int>();
            var monsters = new List<MapMonsterDTO>();

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("mv") && o[1].Equals("3")))
                if (!mobMvPacketsList.Contains(Convert.ToInt32(currentPacket[2])))
                    mobMvPacketsList.Add(Convert.ToInt32(currentPacket[2]));

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("in") || o[0].Equals("c_map")))
            {
                if (currentPacket.Length > 3 && currentPacket[0] == "c_map")
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                if (DAOFactory.MapDAO.LoadById(map) == null)
                {
                    continue;
                }

                if (currentPacket.Length <= 7 || currentPacket[0] != "in" || currentPacket[1] != "3")
                {
                    continue;
                }

                var monster = new MapMonsterDTO
                {
                    MapId = map,
                    MonsterVNum = short.Parse(currentPacket[2]),
                    MapMonsterId = int.Parse(currentPacket[3]),
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    Position = (byte)(currentPacket[6] == string.Empty ? 0 : byte.Parse(currentPacket[6])),
                    IsDisabled = false
                };
                monster.IsMoving = mobMvPacketsList.Contains(monster.MapMonsterId);

                if (!existingNpcMonsters.Contains(monster.MonsterVNum)) continue;

                if (existingMapMonsters.Contains(monster.MapMonsterId) ||
                    monsters.Any(x => x.MapMonsterId == monster.MapMonsterId)) continue;

                monsters.Add(monster);
                monsterCounter++;
            }

            DAOFactory.MapMonsterDAO.Insert(monsters);
            Logger.Log.Info($"{monsterCounter} MapMonsters parsed");
        }
    }
}