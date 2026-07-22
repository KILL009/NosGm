using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class ShopSkillDAO : IShopSkillDAO
    {
        #region Methods

        public ShopSkillDTO Insert(ShopSkillDTO shopSkill)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new ShopSkill();
                    ShopSkillMapper.ToShopSkill(shopSkill, entity);
                    context.ShopSkill.Add(entity);
                    context.SaveChanges();
                    if (ShopSkillMapper.ToShopSkillDTO(entity, shopSkill)) return shopSkill;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void Insert(List<ShopSkillDTO> skills)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Skill in skills)
                    {
                        var entity = new ShopSkill();
                        ShopSkillMapper.ToShopSkill(Skill, entity);
                        context.ShopSkill.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public IEnumerable<ShopSkillDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ShopSkillDTO>();
                foreach (var entity in context.ShopSkill)
                {
                    var dto = new ShopSkillDTO();
                    ShopSkillMapper.ToShopSkillDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<ShopSkillDTO> LoadByShopId(int shopId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ShopSkillDTO>();
                foreach (var ShopSkill in context.ShopSkill.Where(s => s.ShopId.Equals(shopId)))
                {
                    var dto = new ShopSkillDTO();
                    ShopSkillMapper.ToShopSkillDTO(ShopSkill, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}