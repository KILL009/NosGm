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
    public class ShopItemDAO : IShopItemDAO
    {
        #region Methods

        public DeleteResult DeleteById(int itemId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var Item = context.ShopItem.FirstOrDefault(i => i.ShopItemId.Equals(itemId));

                    if (Item != null)
                    {
                        context.ShopItem.Remove(Item);
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

        public ShopItemDTO Insert(ShopItemDTO item)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new ShopItem();
                    ShopItemMapper.ToShopItem(item, entity);
                    context.ShopItem.Add(entity);
                    context.SaveChanges();
                    if (ShopItemMapper.ToShopItemDTO(entity, item)) return item;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void Insert(List<ShopItemDTO> items)
        {
            foreach (var item in items) Insert(item);
        }

        public IEnumerable<ShopItemDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ShopItemDTO>();
                foreach (var entity in context.ShopItem)
                {
                    var dto = new ShopItemDTO();
                    ShopItemMapper.ToShopItemDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public ShopItemDTO LoadById(int itemId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new ShopItemDTO();
                    if (ShopItemMapper.ToShopItemDTO(context.ShopItem.FirstOrDefault(i => i.ShopItemId.Equals(itemId)),
                        dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ShopItemDTO> LoadByShopId(int shopId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ShopItemDTO>();
                foreach (var ShopItem in context.ShopItem.Where(i => i.ShopId.Equals(shopId)))
                {
                    var dto = new ShopItemDTO();
                    ShopItemMapper.ToShopItemDTO(ShopItem, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}