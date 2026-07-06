using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadSkills
    {
        public static void Load()
        {
            IEnumerable<ComboDTO> combos = DAOFactory.ComboDAO.LoadAll().ToArray();
            var bcards = DAOFactory.BCardDAO.LoadAll().ToArray().Where(s => s.SkillVNum.HasValue);
            foreach (var skillItem in DAOFactory.SkillDAO.LoadAll().ToArray())
            {
                var tmp = new Skill(skillItem);
                if (!(tmp is Skill skillObj))
                {
                    return;
                }

                skillObj.Combos.AddRange(combos.Where(s => s.SkillVNum == skillObj.SkillVNum).ToList());
                skillObj.BCards = new List<BCard>();

                foreach (var o in bcards.Where(s => s.SkillVNum == skillObj.SkillVNum))
                {
                    skillObj.BCards.Add(new BCard(o));
                }

                ServerManager.Skills.Add(skillObj);
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Skills.Count} Skills - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
