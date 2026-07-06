using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadFamilies
    {
        public static void Load()
        {
            ServerManager.Instance.FamilyList = new ThreadSafeSortedList<long, Family>();
            foreach (var familyDto in DAOFactory.FamilyDAO.LoadAll())
            {
                var family = new Family(familyDto)
                {
                    FamilyCharacters = new List<FamilyCharacter>()
                };
                foreach (var famchar in DAOFactory.FamilyCharacterDAO.LoadByFamilyId(family.FamilyId)
                    .ToList())
                {
                    family.FamilyCharacters.Add(new FamilyCharacter(famchar));
                }
                foreach (var famskill in DAOFactory.FamilySkillMissionDAO.LoadByFamilyId(family.FamilyId).ToList())
                {
                    family.FamilySkillMissions.Add(new FamilySkillMission(famskill));
                }

                var familyCharacter =
                    family.FamilyCharacters.Find(s => s.Authority == FamilyAuthority.Head);
                if (familyCharacter != null)
                {
                    family.Warehouse = new Inventory(new Character(familyCharacter.Character));
                    foreach (var inventory in DAOFactory.ItemInstanceDAO
                        .LoadByCharacterId(familyCharacter.CharacterId)
                        .Where(s => s.Type == InventoryType.FamilyWareHouse).ToList())
                    {
                        inventory.CharacterId = familyCharacter.CharacterId;
                        family.Warehouse[inventory.Id] = new ItemInstance(inventory);
                    }
                }

                family.FamilyLogs = DAOFactory.FamilyLogDAO.LoadByFamilyId(family.FamilyId).ToList();
                ServerManager.Instance.FamilyList[family.FamilyId] = family;
                LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.FamilyList.Count} Families - Status: Successful", LogType.LOAD);
            }
        }
    }
}
