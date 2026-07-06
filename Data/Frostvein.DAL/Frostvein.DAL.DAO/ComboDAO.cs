using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class ComboDAO : IComboDAO
    {
        #region Methods

        public void Insert(List<ComboDTO> combos)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var combo in combos)
                    {
                        var entity = new Combo();
                        ComboMapper.ToCombo(combo, entity);
                        context.Combo.Add(entity);
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

        public ComboDTO Insert(ComboDTO combo)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Combo();
                    ComboMapper.ToCombo(combo, entity);
                    context.Combo.Add(entity);
                    context.SaveChanges();
                    if (ComboMapper.ToComboDTO(entity, combo))
                    {
                        return combo;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ComboDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ComboDTO>();
                foreach (var combo in context.Combo)
                {
                    var dto = new ComboDTO();
                    ComboMapper.ToComboDTO(combo, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public ComboDTO LoadById(short comboId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new ComboDTO();
                    if (ComboMapper.ToComboDTO(context.Combo.FirstOrDefault(s => s.SkillVNum.Equals(comboId)), dto))
                    {
                        return dto;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ComboDTO> LoadBySkillVnum(short skillVNum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ComboDTO>();
                foreach (var combo in context.Combo.Where(c => c.SkillVNum == skillVNum))
                {
                    var dto = new ComboDTO();
                    ComboMapper.ToComboDTO(combo, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<ComboDTO> LoadByVNumHitAndEffect(short skillVNum, short hit, short effect)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ComboDTO>();
                foreach (var combo in context.Combo.Where(s =>
                    s.SkillVNum == skillVNum && s.Hit == hit && s.Effect == effect))
                {
                    var dto = new ComboDTO();
                    ComboMapper.ToComboDTO(combo, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}