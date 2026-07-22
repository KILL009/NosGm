using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class StaticBonusDAO : IStaticBonusDAO
    {
        #region Methods

        public static StaticBonusDTO LoadById(long sbId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new StaticBonusDTO();
                    if (StaticBonusMapper.ToStaticBonusDTO(context.StaticBonus.FirstOrDefault(s => s.StaticBonusId.Equals(sbId)), dto))
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

        public void Delete(short bonusToDelete, long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var bon = context.StaticBonus.FirstOrDefault(c =>
                        c.StaticBonusType == (StaticBonusType)bonusToDelete && c.CharacterId == characterId);

                    if (bon != null)
                    {
                        context.StaticBonus.Remove(bon);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), bonusToDelete, e.Message), e);
            }
        }

        public SaveResult InsertOrUpdate(ref StaticBonusDTO staticBonus)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var id = staticBonus.CharacterId;
                    var cardid = staticBonus.StaticBonusType;
                    var entity =
                        context.StaticBonus.FirstOrDefault(c => c.StaticBonusType == cardid && c.CharacterId == id);

                    if (entity == null)
                    {
                        staticBonus = insert(staticBonus, context);
                        return SaveResult.Inserted;
                    }

                    staticBonus.StaticBonusId = entity.StaticBonusId;
                    staticBonus = update(entity, staticBonus, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }
      

        public IEnumerable<StaticBonusDTO> LoadByCharacterId(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<StaticBonusDTO>();
                foreach (var entity in context.StaticBonus.Where(i =>
                    i.CharacterId == characterId && i.DateEnd > DateTime.Now))
                {
                    var dto = new StaticBonusDTO();
                    StaticBonusMapper.ToStaticBonusDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<short> LoadTypeByCharacterId(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.StaticBonus.Where(i => i.CharacterId == characterId)
                        .Select(qle => (short)qle.StaticBonusType).ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static StaticBonusDTO insert(StaticBonusDTO sb, NosGmContext context)
        {
            try
            {
                var entity = new StaticBonus();
                StaticBonusMapper.ToStaticBonus(sb, entity);
                context.StaticBonus.Add(entity);
                context.SaveChanges();
                if (StaticBonusMapper.ToStaticBonusDTO(entity, sb)) return sb;

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static StaticBonusDTO update(StaticBonus entity, StaticBonusDTO sb, NosGmContext context)
        {
            if (entity != null)
            {
                StaticBonusMapper.ToStaticBonus(sb, entity);
                context.SaveChanges();
            }

            if (StaticBonusMapper.ToStaticBonusDTO(entity, sb)) return sb;

            return null;
        }

        public async Task<SaveResult> InsertOrUpdateAsync (StaticBonusDTO staticBonus)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var id = staticBonus.CharacterId;
                    var cardid = staticBonus.StaticBonusType;
                    var entity = context.StaticBonus.FirstOrDefault(c => c.StaticBonusType == cardid && c.CharacterId == id);

                    if (entity == null)
                    {
                        staticBonus = await InsertAsync(staticBonus, context).ConfigureAwait(false);
                        return SaveResult.Inserted;
                    }

                    staticBonus.StaticBonusId = entity.StaticBonusId;
                    staticBonus = await UpdateAsync(entity, staticBonus, context).ConfigureAwait(false);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        private async Task<StaticBonusDTO> InsertAsync (StaticBonusDTO sb, NosGmContext context)
        {
            try
            {
                var entity = new StaticBonus();
                StaticBonusMapper.ToStaticBonus(sb, entity);
                context.StaticBonus.Add(entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
                if (StaticBonusMapper.ToStaticBonusDTO(entity, sb))
                {
                    return sb;
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private async Task<StaticBonusDTO> UpdateAsync (StaticBonus entity, StaticBonusDTO sb, NosGmContext context)
        {
            if (entity != null)
            {
                StaticBonusMapper.ToStaticBonus(sb, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (StaticBonusMapper.ToStaticBonusDTO(entity, sb))
            {
                return sb;
            }

            return null;
        }

        #endregion
    }
}