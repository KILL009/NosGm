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
    public class CharacterSkillDAO : ICharacterSkillDAO
    {
        #region Methods

        public DeleteResult Delete(long characterId, short skillVNum)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var invItem = context.CharacterSkill.FirstOrDefault(i =>
                        i.CharacterId == characterId && i.SkillVNum == skillVNum);
                    if (invItem != null)
                    {
                        context.CharacterSkill.Remove(invItem);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public DeleteResult Delete(Guid id)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var entity = context.Set<CharacterSkill>().FirstOrDefault(i => i.Id == id);
                if (entity != null)
                {
                    context.Set<CharacterSkill>().Remove(entity);
                    context.SaveChanges();
                }

                return DeleteResult.Deleted;
            }
        }

        public IEnumerable<CharacterSkillDTO> InsertOrUpdate(IEnumerable<CharacterSkillDTO> dtos)
        {
            try
            {
                IList<CharacterSkillDTO> results = new List<CharacterSkillDTO>();
                using (var context = DataAccessHelper.CreateContext())
                {
                    foreach (var dto in dtos) results.Add(InsertOrUpdate(context, dto));
                }

                return results;
            }
            catch (Exception e)
            {
                Logger.Error($"Message: {e.Message}", e);
                return Enumerable.Empty<CharacterSkillDTO>();
            }
        }

        public CharacterSkillDTO InsertOrUpdate(CharacterSkillDTO dto)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return InsertOrUpdate(context, dto);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Message: {e.Message}", e);
                return null;
            }
        }

        public IEnumerable<CharacterSkillDTO> LoadByCharacterId(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterSkillDTO>();
                foreach (var entity in context.CharacterSkill.Where(i => i.CharacterId == characterId))
                {
                    var output = new CharacterSkillDTO();
                    CharacterSkillMapper.ToCharacterSkillDTO(entity, output);
                    result.Add(output);
                }

                return result;
            }
        }

        public CharacterSkillDTO LoadById(Guid id)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var characterSkillDTO = new CharacterSkillDTO();
                if (CharacterSkillMapper.ToCharacterSkillDTO(
                    context.CharacterSkill.FirstOrDefault(i => i.Id.Equals(id)), characterSkillDTO))
                    return characterSkillDTO;

                return null;
            }
        }

        public IEnumerable<Guid> LoadKeysByCharacterId(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.CharacterSkill.Where(i => i.CharacterId == characterId).Select(c => c.Id).ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        protected static CharacterSkillDTO Insert(CharacterSkillDTO dto, FrostveinContext context)
        {
            var entity = new CharacterSkill();
            CharacterSkillMapper.ToCharacterSkill(dto, entity);
            context.Set<CharacterSkill>().Add(entity);
            context.SaveChanges();
            if (CharacterSkillMapper.ToCharacterSkillDTO(entity, dto)) return dto;

            return null;
        }

        protected static CharacterSkillDTO InsertOrUpdate(FrostveinContext context, CharacterSkillDTO dto)
        {
            var primaryKey = dto.Id;
            var entity = context.Set<CharacterSkill>().FirstOrDefault(c => c.Id == primaryKey);
            if (entity == null)
                return Insert(dto, context);
            return Update(entity, dto, context);
        }

        protected static CharacterSkillDTO Update(CharacterSkill entity, CharacterSkillDTO inventory,
            FrostveinContext context)
        {
            if (entity != null)
            {
                CharacterSkillMapper.ToCharacterSkill(inventory, entity);
                context.SaveChanges();
            }

            if (CharacterSkillMapper.ToCharacterSkillDTO(entity, inventory)) return inventory;

            return null;
        }

        public async Task<CharacterSkillDTO> InsertOrUpdateAsync(CharacterSkillDTO dto)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return await InsertOrUpdateAsync(context, dto).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Message: {e.Message}", e);
                return null;
            }
        }

        protected static async Task<CharacterSkillDTO> InsertOrUpdateAsync(FrostveinContext context, CharacterSkillDTO dto)
        {
            Guid primaryKey = dto.Id;
            CharacterSkill entity = context.Set<CharacterSkill>().FirstOrDefault(c => c.Id == primaryKey);
            if (entity == null)
            {
                return await InsertAsync(dto, context).ConfigureAwait(false);
            }
            else
            {
                return await UpdateAsync(entity, dto, context).ConfigureAwait(false);
            }
        }

        protected static async Task<CharacterSkillDTO> UpdateAsync(CharacterSkill entity, CharacterSkillDTO inventory, FrostveinContext context)
        {
            if (entity != null)
            {
                CharacterSkillMapper.ToCharacterSkill(inventory, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (CharacterSkillMapper.ToCharacterSkillDTO(entity, inventory))
            {
                return inventory;
            }

            return null;
        }

        protected static async Task<CharacterSkillDTO> InsertAsync(CharacterSkillDTO dto, FrostveinContext context)
        {
            CharacterSkill entity = new CharacterSkill();
            CharacterSkillMapper.ToCharacterSkill(dto, entity);
            context.CharacterSkill.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            if (CharacterSkillMapper.ToCharacterSkillDTO(entity, dto))
            {
                return dto;
            }
            return null;
        }

        #endregion
    }
}