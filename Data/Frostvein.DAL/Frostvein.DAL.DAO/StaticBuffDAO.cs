using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.DAL.DAO
{
    public class StaticBuffDAO : IStaticBuffDAO
    {
        #region Methods

        public static StaticBuffDTO LoadById(long sbId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new StaticBuffDTO();
                    if (StaticBuffMapper.ToStaticBuffDTO(context.StaticBuff.FirstOrDefault(s => s.StaticBuffId.Equals(sbId)), dto))
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
                    var bon = context.StaticBuff.FirstOrDefault(c =>
                        c.CardId == bonusToDelete && c.CharacterId == characterId);

                    if (bon != null)
                    {
                        context.StaticBuff.Remove(bon);
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

        public SaveResult InsertOrUpdate(ref StaticBuffDTO staticBuff)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var id = staticBuff.CharacterId;
                    var cardid = staticBuff.CardId;
                    var entity = context.StaticBuff.FirstOrDefault(c => c.CardId == cardid && c.CharacterId == id);

                    if (entity == null)
                    {
                        staticBuff = insert(staticBuff, context);
                        return SaveResult.Inserted;
                    }

                    staticBuff.StaticBuffId = entity.StaticBuffId;
                    staticBuff = update(entity, staticBuff, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<StaticBuffDTO> LoadByCharacterId(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<StaticBuffDTO>();
                foreach (var entity in context.StaticBuff.Where(i => i.CharacterId == characterId))
                {
                    var dto = new StaticBuffDTO();
                    StaticBuffMapper.ToStaticBuffDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<short> LoadByTypeCharacterId(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.StaticBuff.Where(i => i.CharacterId == characterId).Select(qle => qle.CardId)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static StaticBuffDTO insert(StaticBuffDTO sb, FrostveinContext context)
        {
            try
            {
                var entity = new StaticBuff();
                StaticBuffMapper.ToStaticBuff(sb, entity);
                context.StaticBuff.Add(entity);
                context.SaveChanges();
                if (StaticBuffMapper.ToStaticBuffDTO(entity, sb)) return sb;

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static StaticBuffDTO update(StaticBuff entity, StaticBuffDTO sb, FrostveinContext context)
        {
            if (entity != null)
            {
                StaticBuffMapper.ToStaticBuff(sb, entity);
                context.SaveChanges();
            }

            if (StaticBuffMapper.ToStaticBuffDTO(entity, sb)) return sb;

            return null;
        }

        public async Task<SaveResult> InsertOrUpdateAsync (StaticBuffDTO staticBuff)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var id = staticBuff.CharacterId;
                    var cardid = staticBuff.CardId;
                    var entity = context.StaticBuff.FirstOrDefault(c => c.CardId == cardid && c.CharacterId == id);

                    if (entity == null)
                    {
                        staticBuff = await InsertAsync(staticBuff, context).ConfigureAwait(false);
                        return SaveResult.Inserted;
                    }

                    staticBuff.StaticBuffId = entity.StaticBuffId;
                    staticBuff = await UpdateAsync(entity, staticBuff, context).ConfigureAwait(false);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        private async Task<StaticBuffDTO> InsertAsync (StaticBuffDTO sb, FrostveinContext context)
        {
            try
            {
                var entity = new StaticBuff();
                StaticBuffMapper.ToStaticBuff(sb, entity);
                context.StaticBuff.Add(entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
                if (StaticBuffMapper.ToStaticBuffDTO(entity, sb))
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

        private async Task<StaticBuffDTO> UpdateAsync (StaticBuff entity, StaticBuffDTO sb, FrostveinContext context)
        {
            if (entity != null)
            {
                StaticBuffMapper.ToStaticBuff(sb, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (StaticBuffMapper.ToStaticBuffDTO(entity, sb))
            {
                return sb;
            }

            return null;
        }

        #endregion
    }
}