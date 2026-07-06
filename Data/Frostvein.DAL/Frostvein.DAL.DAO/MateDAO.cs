using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Entities;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.DAL.DAO
{
    public class MateDAO : IMateDAO
    {
        #region Methods

        public DeleteResult Delete(long id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mate = context.Mate.FirstOrDefault(c => c.MateId.Equals(id));
                    if (mate != null)
                    {
                        context.Mate.Remove(mate);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_MATE_ERROR"), e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref MateDTO mate)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var MateId = mate.MateId;
                    var entity = context.Mate.FirstOrDefault(c => c.MateId.Equals(MateId));

                    if (entity == null)
                    {
                        mate = insert(mate, context);
                        return SaveResult.Inserted;
                    }

                    mate = update(entity, mate, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), mate, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<MateDTO> LoadByCharacterId(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MateDTO>();
                foreach (var mate in context.Mate.Where(s => s.CharacterId == characterId))
                {
                    var dto = new MateDTO();
                    Mapper.Mappers.MateMapper.ToMateDTO(mate, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        private static MateDTO insert(MateDTO mate, FrostveinContext context)
        {
            var entity = new Mate();
            Mapper.Mappers.MateMapper.ToMate(mate, entity);
            context.Mate.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.MateMapper.ToMateDTO(entity, mate)) return mate;

            return null;
        }

        private static MateDTO update(Mate entity, MateDTO character, FrostveinContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.MateMapper.ToMate(character, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.MateMapper.ToMateDTO(entity, character)) return character;

            return null;
        }

        public async Task<SaveResult> InsertOrUpdateAsync(MateDTO mate)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long MateId = mate.MateId;
                    Mate entity = context.Mate.FirstOrDefault(c => c.MateId.Equals(MateId));

                    if (entity == null)
                    {
                        mate = await InsertAsync(mate, context);
                        return SaveResult.Inserted;
                    }

                    mate = await UpdateAsync(entity, mate, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), mate, e.Message), e);
                return SaveResult.Error;
            }
        }

        private async Task<MateDTO> UpdateAsync(Mate entity, MateDTO mate, FrostveinContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.MateMapper.ToMate(mate, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (Mapper.Mappers.MateMapper.ToMateDTO(entity, mate))
            {
                return mate;
            }

            return null;
        }

        private async Task<MateDTO> InsertAsync(MateDTO mate, FrostveinContext context)
        {
            Mate entity = new Mate();
            Mapper.Mappers.MateMapper.ToMate(mate, entity);
            context.Mate.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            if (Mapper.Mappers.MateMapper.ToMateDTO(entity, mate))
            {
                return mate;
            }

            return null;
        }

        #endregion
    }
}