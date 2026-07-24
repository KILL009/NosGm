using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class BazaarItemDAO : IBazaarItemDAO
    {
        #region Methods

        public DeleteResult Delete(long bazaarItemId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    BazaarItem bazaarItem = context.BazaarItem.FirstOrDefault(c => c.BazaarItemId == bazaarItemId);

                    if (bazaarItem != null)
                    {
                        context.BazaarItem.Remove(bazaarItem);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), bazaarItemId, e.Message), e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref BazaarItemDTO bazaarItem)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long bazaarItemId = bazaarItem.BazaarItemId;
                    BazaarItem entity = context.BazaarItem.FirstOrDefault(c => c.BazaarItemId == bazaarItemId);

                    if (entity == null)
                    {
                        bazaarItem = Insert(bazaarItem, context);
                        return SaveResult.Inserted;
                    }

                    bazaarItem = Update(entity, bazaarItem, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"BazaarItemId: {bazaarItem.BazaarItemId} Message: {e.Message}", e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<BazaarItemDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return context.BazaarItem.AsNoTracking()
                    .Select(entity => new BazaarItemDTO
                    {
                        AccountId = entity.AccountId,
                        RegistrationIP = entity.RegistrationIP,
                        CurrentIp = entity.CurrentIp,
                        Amount = entity.Amount,
                        BazaarItemId = entity.BazaarItemId,
                        DateStart = entity.DateStart,
                        Duration = entity.Duration,
                        IsPackage = entity.IsPackage,
                        ItemInstanceId = entity.ItemInstanceId,
                        MedalUsed = entity.MedalUsed,
                        Price = entity.Price,
                        SellerId = entity.SellerId
                    })
                    .ToList();
            }
        }

        public IEnumerable<BazaarItemLoadDTO> LoadAllHydrated()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                List<BazaarItem> entities = context.BazaarItem.AsNoTracking()
                    .Include(entity => entity.Character)
                    .Include(entity => entity.ItemInstance)
                    .ToList();

                var result = new List<BazaarItemLoadDTO>(entities.Count);
                foreach (BazaarItem entity in entities)
                {
                    var bazaarItem = new BazaarItemDTO();
                    BazaarItemMapper.ToBazaarItemDTO(entity, bazaarItem);

                    ItemInstanceDTO itemInstance = null;
                    if (entity.ItemInstance != null)
                    {
                        itemInstance = new ItemInstanceDTO();
                        ItemInstanceMapper.ToItemInstanceDTO(entity.ItemInstance, itemInstance);
                    }

                    result.Add(new BazaarItemLoadDTO
                    {
                        BazaarItem = bazaarItem,
                        ItemInstance = itemInstance,
                        OwnerName = entity.Character?.Name
                    });
                }

                return result;
            }
        }

        public BazaarItemDTO LoadById(long bazaarItemId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new BazaarItemDTO();
                    return BazaarItemMapper.ToBazaarItemDTO(
                        context.BazaarItem.AsNoTracking()
                            .FirstOrDefault(item => item.BazaarItemId == bazaarItemId), dto)
                        ? dto
                        : null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void RemoveOutDated()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    List<BazaarItem> expiredItems = context.BazaarItem.Where(entity =>
                            DbFunctions.AddDays(DbFunctions.AddHours(entity.DateStart, entity.Duration),
                                entity.MedalUsed ? 30 : 7) < DateTime.Now)
                        .ToList();

                    if (expiredItems.Count == 0)
                    {
                        return;
                    }

                    context.BazaarItem.RemoveRange(expiredItems);
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public IEnumerable<BazaarItemDTO> LoadByCharacterId(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.BazaarItem.AsNoTracking()
                        .Where(item => item.SellerId == characterId)
                        .Select(entity => new BazaarItemDTO
                        {
                            AccountId = entity.AccountId,
                            RegistrationIP = entity.RegistrationIP,
                            CurrentIp = entity.CurrentIp,
                            Amount = entity.Amount,
                            BazaarItemId = entity.BazaarItemId,
                            DateStart = entity.DateStart,
                            Duration = entity.Duration,
                            IsPackage = entity.IsPackage,
                            ItemInstanceId = entity.ItemInstanceId,
                            MedalUsed = entity.MedalUsed,
                            Price = entity.Price,
                            SellerId = entity.SellerId
                        })
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Log.Error("LoadByCharacterId", e);
                return Enumerable.Empty<BazaarItemDTO>();
            }
        }

        private static BazaarItemDTO Insert(BazaarItemDTO bazaarItem, NosGmContext context)
        {
            var entity = new BazaarItem();
            BazaarItemMapper.ToBazaarItem(bazaarItem, entity);
            context.BazaarItem.Add(entity);
            context.SaveChanges();
            return BazaarItemMapper.ToBazaarItemDTO(entity, bazaarItem) ? bazaarItem : null;
        }

        private static BazaarItemDTO Update(BazaarItem entity, BazaarItemDTO bazaarItem, NosGmContext context)
        {
            if (entity != null)
            {
                BazaarItemMapper.ToBazaarItem(bazaarItem, entity);
                context.SaveChanges();
            }

            return BazaarItemMapper.ToBazaarItemDTO(entity, bazaarItem) ? bazaarItem : null;
        }

        #endregion
    }
}
