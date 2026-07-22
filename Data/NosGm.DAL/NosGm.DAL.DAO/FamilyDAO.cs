using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class FamilyDAO : IFamilyDAO
    {
        #region Methods

        public DeleteResult Delete(long familyId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var Fam = context.Family.FirstOrDefault(c => c.FamilyId == familyId);

                    if (Fam != null)
                    {
                        context.Family.Remove(Fam);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), familyId, e.Message),
                    e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref FamilyDTO family)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var AccountId = family.FamilyId;
                    var entity = context.Family.FirstOrDefault(c => c.FamilyId.Equals(AccountId));

                    if (entity == null)
                    {
                        family = insert(family, context);
                        return SaveResult.Inserted;
                    }

                    family = update(entity, family, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_FAMILY_ERROR"), family.FamilyId,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<FamilyDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<FamilyDTO>();
                foreach (var entity in context.Family)
                {
                    var dto = new FamilyDTO();
                    FamilyMapper.ToFamilyDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public FamilyDTO LoadByCharacterId(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var familyCharacter =
                        context.FamilyCharacter.FirstOrDefault(fc => fc.Character.CharacterId.Equals(characterId));
                    if (familyCharacter != null)
                    {
                        var family = context.Family.FirstOrDefault(a => a.FamilyId.Equals(familyCharacter.FamilyId));
                        if (family != null)
                        {
                            var dto = new FamilyDTO();
                            if (FamilyMapper.ToFamilyDTO(family, dto)) return dto;

                            return null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public FamilyDTO LoadById(long familyId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var family = context.Family.FirstOrDefault(a => a.FamilyId.Equals(familyId));
                    if (family != null)
                    {
                        var dto = new FamilyDTO();
                        if (FamilyMapper.ToFamilyDTO(family, dto)) return dto;

                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public FamilyDTO LoadByName(string name)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var family = context.Family.FirstOrDefault(a => a.Name.Equals(name));
                    if (family != null)
                    {
                        var dto = new FamilyDTO();
                        if (FamilyMapper.ToFamilyDTO(family, dto)) return dto;

                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        private static FamilyDTO insert(FamilyDTO family, NosGmContext context)
        {
            var entity = new Family();
            FamilyMapper.ToFamily(family, entity);
            context.Family.Add(entity);
            context.SaveChanges();
            if (FamilyMapper.ToFamilyDTO(entity, family)) return family;

            return null;
        }

        private static FamilyDTO update(Family entity, FamilyDTO family, NosGmContext context)
        {
            if (entity != null)
            {
                FamilyMapper.ToFamily(family, entity);
                context.SaveChanges();
            }

            if (FamilyMapper.ToFamilyDTO(entity, family)) return family;

            return null;
        }

        #endregion
    }
}