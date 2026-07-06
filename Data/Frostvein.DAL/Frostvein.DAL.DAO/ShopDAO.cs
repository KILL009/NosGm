using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class ShopDAO : IShopDAO
    {
        #region Methods

        public DeleteResult DeleteByNpcId(int mapNpcId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var shop = context.Shop.First(i => i.MapNpcId.Equals(mapNpcId));
                    IEnumerable<ShopItem> shopItem = context.ShopItem.Where(s => s.ShopId.Equals(shop.ShopId));
                    IEnumerable<ShopSkill> shopSkill = context.ShopSkill.Where(s => s.ShopId.Equals(shop.ShopId));

                    if (shop != null)
                    {
                        foreach (var item in shopItem)
                        {
                            context.ShopItem.Remove(item);
                        }

                        foreach (var skill in shopSkill)
                        {
                            context.ShopSkill.Remove(skill);
                        }

                        context.Shop.Remove(shop);
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

        public void Insert(List<ShopDTO> shops)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Item in shops)
                    {
                        var entity = new Shop();
                        ShopMapper.ToShop(Item, entity);
                        context.Shop.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public ShopDTO Insert(ShopDTO shop)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (context.Shop.FirstOrDefault(c => c.MapNpcId.Equals(shop.MapNpcId)) == null)
                    {
                        var entity = new Shop();
                        ShopMapper.ToShop(shop, entity);
                        context.Shop.Add(entity);
                        context.SaveChanges();
                        if (ShopMapper.ToShopDTO(entity, shop))
                        {
                            return shop;
                        }

                        return null;
                    }

                    return new ShopDTO();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ShopDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ShopDTO>();
                foreach (var entity in context.Shop)
                {
                    var dto = new ShopDTO();
                    ShopMapper.ToShopDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public ShopDTO LoadById(int shopId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new ShopDTO();
                    if (ShopMapper.ToShopDTO(context.Shop.FirstOrDefault(s => s.ShopId.Equals(shopId)), dto))
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

        public ShopDTO LoadByNpc(int mapNpcId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new ShopDTO();
                    if (ShopMapper.ToShopDTO(context.Shop.FirstOrDefault(s => s.MapNpcId.Equals(mapNpcId)), dto))
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

        public SaveResult Update(ref ShopDTO shop)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var shopId = shop.ShopId;
                    var entity = context.Shop.FirstOrDefault(c => c.ShopId.Equals(shopId));

                    shop = update(entity, shop, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_SHOP_ERROR"), shop.ShopId, e.Message), e);
                return SaveResult.Error;
            }
        }

        private static ShopDTO insert(ShopDTO account, FrostveinContext context)
        {
            var entity = new Shop();
            ShopMapper.ToShop(account, entity);
            context.Shop.Add(entity);
            context.SaveChanges();
            ShopMapper.ToShopDTO(entity, account);
            return account;
        }

        private static ShopDTO update(Shop entity, ShopDTO shop, FrostveinContext context)
        {
            if (entity != null)
            {
                ShopMapper.ToShop(shop, entity);
                context.Entry(entity).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (ShopMapper.ToShopDTO(entity, shop))
            {
                return shop;
            }

            return null;
        }

        #endregion
    }
}