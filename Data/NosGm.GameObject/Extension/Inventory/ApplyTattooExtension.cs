using NosGm.DAL;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.GameObject.Extension.Inventory
{
    public static class CraftTattooExtensions
    {
        #region Methods

        public static void CraftTattoo(this ItemInstance e, ClientSession s)
        {
            short goldPrice = 20000;

            if (s.Character.Inventory.CountItem(2411) < 15 ||
                s.Character.Inventory.CountItem(2416) < 20 ||
                s.Character.Inventory.CountItem(2408) < 20 ||
                s.Character.Inventory.CountItem(2460) < 15 ||
                s.Character.Inventory.CountItem(2406) < 10)
            {
                s.SendShopEnd();
                return;
            }

            if (s.Character.Gold < goldPrice)
            {
                s.SendShopEnd();
                return;
            }

            if (s.Character.Skills.Where(t => t.IsTattoo).Count == 2)
            {
                s.SendShopEnd();
                return;
            }

            var skill = DAOFactory.SkillDAO.LoadAll().Where(d => d.Class == 27);
            short castId = 0;
            var rndmSkill = new List<short>();
            switch (e.Item.VNum)
            {
                case 5790:
                    skill.Where(t => t.CastId == 40).ToList().ForEach(z => { rndmSkill.Add(z.SkillVNum); });
                    castId = 40;
                    break;

                case 5791:
                    skill.Where(t => t.CastId == 43).ToList().ForEach(z => { rndmSkill.Add(z.SkillVNum); });
                    castId = 43;
                    break;

                case 5792:
                    skill.Where(t => t.CastId == 41).ToList().ForEach(z => { rndmSkill.Add(z.SkillVNum); });
                    castId = 41;
                    break;

                case 5793:
                    skill.Where(t => t.CastId == 42).ToList().ForEach(z => { rndmSkill.Add(z.SkillVNum); });
                    castId = 42;
                    break;

                default:
                    s.SendShopEnd();
                    return;
            }

            if (s.Character.Skills.Any(t => t.Skill.CastId == castId))
            {
                s.SendShopEnd();
                return;
            }

            var random = new Random();
            var ii = rndmSkill.OrderBy(x => random.Next()).Take(1).First();

            s.Character.Inventory.RemoveItemAmount(2411, 15);
            s.Character.Inventory.RemoveItemAmount(2416, 20);
            s.Character.Inventory.RemoveItemAmount(2408, 20);
            s.Character.Inventory.RemoveItemAmount(2460, 15);
            s.Character.Inventory.RemoveItemAmount(2406, 10);
            s.Character.Inventory.RemoveItemFromInventory(e.Id);
            s.GoldLess(goldPrice);

            var skilll = ServerManager.GetSkill(ii);

            s.Character.Skills[skilll.SkillVNum] = new CharacterSkill
            {
                SkillVNum = skilll.SkillVNum,
                CharacterId = s.Character.CharacterId,
                IsTattoo = true,
                TattooLevel = 0
            };

            s.SendPacket(s.Character.GenerateSki());
            var msg = $"The {skilll.Name} tattoo has been inked";
            s.SendPacket(UserInterfaceHelper.GenerateMsg(msg, 0));
            s.SendPacket(UserInterfaceHelper.GenerateSay(msg, 11));
            s.SendPacket("guri 40 0");
            s.SendShopEnd();
        }

        #endregion
    }
}