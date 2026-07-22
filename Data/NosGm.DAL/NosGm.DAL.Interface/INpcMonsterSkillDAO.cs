using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface INpcMonsterSkillDAO
    {
        #region Methods

        NpcMonsterSkillDTO Insert(ref NpcMonsterSkillDTO npcMonsterSkill);

        void Insert(List<NpcMonsterSkillDTO> skills);

        List<NpcMonsterSkillDTO> LoadAll();

        IEnumerable<NpcMonsterSkillDTO> LoadByNpcMonster(short npcId);

        #endregion
    }
}