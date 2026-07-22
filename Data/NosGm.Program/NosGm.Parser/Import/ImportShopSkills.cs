using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportShopSkills : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportShopSkills(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Import()
        {
            var shopSkills = new List<ShopSkillDTO>();
            var itemCounter = 0;
            byte type = 0;

            foreach (var currentPacket in _configuration.Packets.Where(o =>
                o[0].Equals("n_inv") || o[0].Equals("shopping")))
            {
                if (!currentPacket[0].Equals("n_inv"))
                {
                    if (currentPacket.Length > 3) type = byte.Parse(currentPacket[1]);

                    continue;
                }

                if (DAOFactory.ShopDAO.LoadByNpc(short.Parse(currentPacket[2])) == null) continue;

                for (var i = 5; i < currentPacket.Length; i++)
                {
                    ShopSkillDTO shopSkill;
                    if (!currentPacket[i].Contains(".") && !currentPacket[i].Contains("|"))
                    {
                        shopSkill = new ShopSkillDTO
                        {
                            ShopId = DAOFactory.ShopDAO.LoadByNpc(short.Parse(currentPacket[2])).ShopId,
                            Type = type,
                            Slot = (byte)(i - 5),
                            SkillVNum = short.Parse(currentPacket[i])
                        };

                        if (shopSkills.Any(s =>
                                s.SkillVNum.Equals(shopSkill.SkillVNum) && s.ShopId.Equals(shopSkill.ShopId)) ||
                            DAOFactory.ShopSkillDAO.LoadByShopId(shopSkill.ShopId)
                                .Any(s => s.SkillVNum.Equals(shopSkill.SkillVNum)))
                            continue;
                        shopSkills.Add(shopSkill);
                        itemCounter++;
                    }
                }
            }

            DAOFactory.ShopSkillDAO.Insert(shopSkills);
            Logger.Log.Info($"{itemCounter} Shops SKILL parsed");
        }
    }
}