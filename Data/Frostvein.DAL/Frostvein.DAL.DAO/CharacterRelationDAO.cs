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

namespace Frostvein.DAL.DAO
{
    public class CharacterRelationDAO : ICharacterRelationDAO
    {
        #region Methods

        public DeleteResult Delete(long characterRelationId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var relation =
                        context.CharacterRelation.SingleOrDefault(
                            c => c.CharacterRelationId.Equals(characterRelationId));

                    if (relation != null)
                    {
                        context.CharacterRelation.Remove(relation);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("DELETE_CHARACTER_ERROR"), characterRelationId,
                        e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref CharacterRelationDTO characterRelation)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var characterId = characterRelation.CharacterId;
                    var relatedCharacterId = characterRelation.RelatedCharacterId;
                    var entity = context.CharacterRelation.FirstOrDefault(c =>
                        c.CharacterId.Equals(characterId) && c.RelatedCharacterId.Equals(relatedCharacterId));

                    if (entity == null)
                    {
                        characterRelation = insert(characterRelation, context);
                        return SaveResult.Inserted;
                    }

                    characterRelation.CharacterRelationId = entity.CharacterRelationId;
                    characterRelation = update(entity, characterRelation, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_CHARACTERRELATION_ERROR"),
                        characterRelation.CharacterRelationId, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<CharacterRelationDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterRelationDTO>();
                foreach (var entity in context.CharacterRelation)
                {
                    var dto = new CharacterRelationDTO();
                    CharacterRelationMapper.ToCharacterRelationDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public CharacterRelationDTO LoadById(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new CharacterRelationDTO();
                    if (CharacterRelationMapper.ToCharacterRelationDTO(
                        context.CharacterRelation.FirstOrDefault(s => s.CharacterRelationId.Equals(characterId)), dto))
                        return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static CharacterRelationDTO insert(CharacterRelationDTO relation, FrostveinContext context)
        {
            var entity = new CharacterRelation();
            CharacterRelationMapper.ToCharacterRelation(relation, entity);
            context.CharacterRelation.Add(entity);
            context.SaveChanges();
            if (CharacterRelationMapper.ToCharacterRelationDTO(entity, relation)) return relation;

            return null;
        }

        private static CharacterRelationDTO update(CharacterRelation entity, CharacterRelationDTO relation,
            FrostveinContext context)
        {
            if (entity != null)
            {
                CharacterRelationMapper.ToCharacterRelation(relation, entity);
                context.SaveChanges();
            }

            if (CharacterRelationMapper.ToCharacterRelationDTO(entity, relation)) return relation;

            return null;
        }

        #endregion
    }
}