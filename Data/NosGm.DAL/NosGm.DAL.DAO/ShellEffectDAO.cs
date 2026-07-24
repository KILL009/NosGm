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
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class ShellEffectDAO : IShellEffectDAO
    {
        #region Methods

        public int CleanupDuplicateNonRuneEffects(long characterId, int maximumEffects = 15)
        {
            if (characterId <= 0 || maximumEffects < 1)
            {
                return 0;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                using (var transaction = context.Database.BeginTransaction())
                {
                    IQueryable<Guid> characterSerials = context.ItemInstance
                        .Where(item => item.CharacterId == characterId && item.EquipmentSerialId.HasValue)
                        .Select(item => item.EquipmentSerialId.Value)
                        .Distinct();

                    List<Guid> duplicateSerials = context.ShellEffect
                        .Where(effect => !effect.IsRune && characterSerials.Contains(effect.EquipmentSerialId))
                        .GroupBy(effect => effect.EquipmentSerialId)
                        .Where(group => group.Count() > maximumEffects)
                        .Select(group => group.Key)
                        .ToList();

                    if (duplicateSerials.Count == 0)
                    {
                        transaction.Commit();
                        return 0;
                    }

                    List<ItemInstance> affectedItems = context.ItemInstance
                        .Where(item => item.CharacterId == characterId &&
                                       item.EquipmentSerialId.HasValue &&
                                       duplicateSerials.Contains(item.EquipmentSerialId.Value))
                        .ToList();

                    foreach (ItemInstance item in affectedItems)
                    {
                        item.ShellRarity = null;
                    }

                    List<ShellEffect> duplicateEffects = context.ShellEffect
                        .Where(effect => !effect.IsRune && duplicateSerials.Contains(effect.EquipmentSerialId))
                        .ToList();

                    context.ShellEffect.RemoveRange(duplicateEffects);
                    context.SaveChanges();
                    transaction.Commit();

                    return duplicateSerials.Count;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to clean duplicate shell effects for CharacterId {characterId}.", e);
                return 0;
            }
        }

        public DeleteResult DeleteByEquipmentSerialId(Guid id, bool isRune)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    List<ShellEffect> deleteEntities = context.ShellEffect
                        .Where(effect => effect.EquipmentSerialId == id && effect.IsRune == isRune)
                        .ToList();

                    if (deleteEntities.Count != 0)
                    {
                        context.ShellEffect.RemoveRange(deleteEntities);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), id, e.Message), e);
                return DeleteResult.Error;
            }
        }

        public ShellEffectDTO InsertOrUpdate(ShellEffectDTO shellEffect)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long shellEffectId = shellEffect.ShellEffectId;
                    ShellEffect entity = context.ShellEffect.FirstOrDefault(c => c.ShellEffectId == shellEffectId);

                    return entity == null
                        ? Insert(shellEffect, context)
                        : Update(entity, shellEffect, context);
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), shellEffect, e.Message),
                    e);
                return shellEffect;
            }
        }

        public void InsertOrUpdateFromList(List<ShellEffectDTO> shellEffects, Guid equipmentSerialId)
        {
            try
            {
                if (!shellEffects.Any())
                {
                    return;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    void InsertEffect(ShellEffectDTO shellEffect)
                    {
                        var entity = new ShellEffect();
                        ShellEffectMapper.ToShellEffect(shellEffect, entity);
                        context.ShellEffect.Add(entity);
                        context.SaveChanges();
                        shellEffect.ShellEffectId = entity.ShellEffectId;
                    }

                    void UpdateEffect(ShellEffect entity, ShellEffectDTO shellEffect)
                    {
                        if (entity != null)
                        {
                            ShellEffectMapper.ToShellEffect(shellEffect, entity);
                        }
                    }

                    foreach (ShellEffectDTO item in shellEffects)
                    {
                        item.EquipmentSerialId = equipmentSerialId;
                        ShellEffect entity = context.ShellEffect
                            .FirstOrDefault(c => c.ShellEffectId == item.ShellEffectId);

                        if (entity == null)
                        {
                            InsertEffect(item);
                        }
                        else
                        {
                            UpdateEffect(entity, item);
                        }
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public IEnumerable<ShellEffectDTO> LoadByEquipmentSerialId(Guid id, bool isRune)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ShellEffectDTO>();
                foreach (ShellEffect entity in context.ShellEffect
                             .Where(c => c.EquipmentSerialId == id && c.IsRune == isRune))
                {
                    var dto = new ShellEffectDTO();
                    ShellEffectMapper.ToShellEffectDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public async Task InsertOrUpdateFromListAsync(List<ShellEffectDTO> shellEffects, Guid equipmentSerialId)
        {
            try
            {
                if (!shellEffects.Any())
                {
                    return;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    async Task InsertAsync(ShellEffectDTO shellEffect)
                    {
                        var entity = new ShellEffect();
                        ShellEffectMapper.ToShellEffect(shellEffect, entity);
                        context.ShellEffect.Add(entity);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        shellEffect.ShellEffectId = entity.ShellEffectId;
                    }

                    async Task UpdateAsync(ShellEffect entity, ShellEffectDTO shellEffect)
                    {
                        if (entity != null)
                        {
                            ShellEffectMapper.ToShellEffect(shellEffect, entity);
                            await context.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }

                    foreach (ShellEffectDTO item in shellEffects)
                    {
                        item.EquipmentSerialId = equipmentSerialId;
                        ShellEffect entity = context.ShellEffect
                            .FirstOrDefault(c => c.ShellEffectId == item.ShellEffectId);

                        if (entity == null)
                        {
                            await InsertAsync(item).ConfigureAwait(false);
                        }
                        else
                        {
                            await UpdateAsync(entity, item).ConfigureAwait(false);
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static ShellEffectDTO Insert(ShellEffectDTO shellEffect, NosGmContext context)
        {
            var entity = new ShellEffect();
            ShellEffectMapper.ToShellEffect(shellEffect, entity);
            context.ShellEffect.Add(entity);
            context.SaveChanges();
            return ShellEffectMapper.ToShellEffectDTO(entity, shellEffect) ? shellEffect : null;
        }

        private static ShellEffectDTO Update(ShellEffect entity, ShellEffectDTO shellEffect, NosGmContext context)
        {
            if (entity != null)
            {
                ShellEffectMapper.ToShellEffect(shellEffect, entity);
                context.SaveChanges();
            }

            return ShellEffectMapper.ToShellEffectDTO(entity, shellEffect) ? shellEffect : null;
        }

        #endregion
    }
}
