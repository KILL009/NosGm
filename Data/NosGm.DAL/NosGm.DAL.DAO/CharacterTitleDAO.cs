using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class CharacterTitleDAO : ICharacterTitleDAO
    {
        #region Methods

        public IEnumerable<CharacterTitleDTO> LoadByCharacterId(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterTitleDTO>();
                foreach (var charQuest in context.CharacterTitle.Where(s => s.CharacterId == characterId))
                {
                    var dto = new CharacterTitleDTO();
                    Mapper.Mappers.CharacterTitleMapper.ToTitleDTO(charQuest, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public DeleteResult Delete(long CharacterTitleId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var relation =
                        context.CharacterTitle.SingleOrDefault(c => c.CharacterTitleId.Equals(CharacterTitleId));

                    if (relation != null)
                    {
                        context.CharacterTitle.Remove(relation);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("DELETE_CHARACTER_ERROR"), CharacterTitleId,
                        e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref CharacterTitleDTO CharacterTitle)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var characterId = CharacterTitle.CharacterTitleId;
                    var entity = context.CharacterTitle.FirstOrDefault(c => c.CharacterTitleId.Equals(characterId));

                    if (entity == null)
                    {
                        CharacterTitle = insert(CharacterTitle, context);
                        return SaveResult.Inserted;
                    }

                    CharacterTitle = update(entity, CharacterTitle, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_CHARACTERTITLE_ERROR"),
                        CharacterTitle.CharacterTitleId, e.Message), e);
                return SaveResult.Error;
            }
        }

        private static CharacterTitleDTO insert(CharacterTitleDTO relation, NosGmContext context)
        {
            var entity = new CharacterTitle();
            Mapper.Mappers.CharacterTitleMapper.ToTitle(relation, entity);
            context.CharacterTitle.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.CharacterTitleMapper.ToTitleDTO(entity, relation)) return relation;

            return null;
        }

        private static CharacterTitleDTO update(CharacterTitle entity, CharacterTitleDTO relation,
            NosGmContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.CharacterTitleMapper.ToTitle(relation, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.CharacterTitleMapper.ToTitleDTO(entity, relation)) return relation;

            return null;
        }

        public async Task<CharacterTitleDTO> InsertOrUpdateAsync(CharacterTitleDTO characterTitle)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var characterId = characterTitle.CharacterTitleId;
                    CharacterTitle entity = context.CharacterTitle.FirstOrDefault(c => c.CharacterTitleId.Equals(characterId));
                    if (entity == null)
                    {
                        return await InsertAsync(characterTitle, context);
                    }
                    return await UpdateAsync(entity, characterTitle, context);
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("UPDATE_CHARACTERTITLE_ERROR"), characterTitle.CharacterTitleId, e.Message), e);
                return characterTitle;
            }
        }

        private async Task<CharacterTitleDTO> UpdateAsync(CharacterTitle entity, CharacterTitleDTO characterTitle, NosGmContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.CharacterTitleMapper.ToTitle(characterTitle, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (Mapper.Mappers.CharacterTitleMapper.ToTitleDTO(entity, characterTitle))
            {
                return characterTitle;
            }

            return null;
        }

        private async Task<CharacterTitleDTO> InsertAsync(CharacterTitleDTO characterTitle, NosGmContext context)
        {
            CharacterTitle entity = new CharacterTitle();
            Mapper.Mappers.CharacterTitleMapper.ToTitle(characterTitle, entity);
            context.CharacterTitle.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            if (Mapper.Mappers.CharacterTitleMapper.ToTitleDTO(entity, characterTitle))
            {
                return characterTitle;
            }

            return null;
        }

        #endregion
    }
}