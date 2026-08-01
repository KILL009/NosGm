using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class ItemDAO : IItemDAO
    {
        private static readonly ICacheService<short, ItemDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, ItemDTO>();

        #region Methods

        public IEnumerable<ItemDTO> FindByName(string name)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ItemDTO>();
                foreach (var item in context.Item.Where(s =>
                    string.IsNullOrEmpty(name) ? s.Name.Equals("") : s.Name.Contains(name)))
                {
                    var dto = new ItemDTO();
                    ItemMapper.ToItemDTO(item, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public void Insert(IEnumerable<ItemDTO> items)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    foreach (var Item in items)
                    {
                        var entity = new Item();
                        ItemMapper.ToItem(Item, entity);
                        context.Item.Add(entity);
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public ItemDTO Insert(ItemDTO item)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Item();
                    ItemMapper.ToItem(item, entity);
                    context.Item.Add(entity);
                    context.SaveChanges();
                    if (ItemMapper.ToItemDTO(entity, item)) return item;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ItemDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ItemDTO>();
                foreach (var item in context.Item.AsNoTracking())
                {
                    var dto = new ItemDTO();
                    ItemMapper.ToItemDTO(item, dto);
                    _cache.Set(dto.VNum, dto, TimeSpan.FromHours(24));
                    result.Add(dto);
                }

                return result;
            }
        }

        public ItemDTO LoadById(short vNum)
        {
            try
            {
                if (_cache.TryGetValue(vNum, out var cachedDto))
                {
                    return cachedDto;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new ItemDTO();
                    if (ItemMapper.ToItemDTO(context.Item.AsNoTracking().FirstOrDefault(i => i.VNum.Equals(vNum)), dto))
                    {
                        _cache.Set(vNum, dto, TimeSpan.FromHours(24));
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

        #endregion
    }
}