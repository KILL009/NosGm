using Frostvein.Core;
using Frostvein.DAL;
using System.Linq;

namespace Frostvein.Parser.Import
{
    public class ImportNpcMonsterData : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportNpcMonsterData(ImportConfiguration importConfiguration)
        {
            _configuration = importConfiguration;
        }

        public void Import()
        {
            var existingNpcMonsters = DAOFactory.NpcMonsterDAO.LoadAll().ToDictionary(x => x.NpcMonsterVNum, x => x);

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("e_info") && o[1].Equals("10")))
            {
                if (currentPacket.Length <= 25) continue;

                if (!existingNpcMonsters.TryGetValue(short.Parse(currentPacket[2]), out var npcMonster)) continue;

                npcMonster.AttackClass = byte.Parse(currentPacket[5]);
                npcMonster.AttackUpgrade = byte.Parse(currentPacket[7]);
                npcMonster.DamageMinimum = short.Parse(currentPacket[8]);
                npcMonster.DamageMaximum = short.Parse(currentPacket[9]);
                npcMonster.Concentrate = short.Parse(currentPacket[10]);
                npcMonster.CriticalChance = byte.Parse(currentPacket[11]);
                npcMonster.CriticalRate = short.Parse(currentPacket[12]);
                npcMonster.DefenceUpgrade = byte.Parse(currentPacket[13]);
                npcMonster.CloseDefence = short.Parse(currentPacket[14]);
                npcMonster.DefenceDodge = short.Parse(currentPacket[15]);
                npcMonster.DistanceDefence = short.Parse(currentPacket[16]);
                npcMonster.DistanceDefenceDodge = short.Parse(currentPacket[17]);
                npcMonster.MagicDefence = short.Parse(currentPacket[18]);
                npcMonster.FireResistance = short.Parse(currentPacket[19]);
                npcMonster.WaterResistance = short.Parse(currentPacket[20]);
                npcMonster.LightResistance = short.Parse(currentPacket[21]);
                npcMonster.DarkResistance = short.Parse(currentPacket[22]);

                DAOFactory.NpcMonsterDAO.InsertOrUpdate(ref npcMonster);
            }

            Logger.Log.Info("X NpcMonsters STATS parsed");
        }
    }
}