using NosGm.DAL;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadNpcMonster
    {
        public static void Load()
        {
            var bcards = DAOFactory.BCardDAO.LoadAll().ToArray().Where(s => s.NpcMonsterVNum.HasValue);
            foreach (var npcMonster in DAOFactory.NpcMonsterDAO.LoadAll().ToArray())
            {
                var tmp = new NpcMonster(npcMonster);

                if (!(tmp is NpcMonster monster))
                {
                    continue;
                }

                // TODO: remove that after
                monster.Initialize();
                monster.BCards = new List<BCard>();

                foreach (var s in bcards.Where(s =>
                    s.NpcMonsterVNum == (monster.OriginalNpcMonsterVNum > 0
                        ? npcMonster.OriginalNpcMonsterVNum
                        : monster.NpcMonsterVNum)))
                {
                    monster.BCards.Add(new BCard(s));
                }

                ServerManager.Npcs.Add(monster);
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Npcs.Count} NPC/Monster - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
