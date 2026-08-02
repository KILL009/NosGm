using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NosGm.DAL.DAO
{
    public class ItemDAO : IItemDAO
    {
        private static readonly ICacheService<short, ItemDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, ItemDTO>(dto => dto.Clone());
        private static int _isFullyLoaded;
        private static readonly object _loadLock = new object();

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

                lock (_loadLock)
                {
                    Volatile.Write(ref _isFullyLoaded, 0);
                    _cache.Clear();
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
                    if (ItemMapper.ToItemDTO(entity, item))
                    {
                        lock (_loadLock)
                        {
                            _cache.Set(item.VNum, item);
                        }
                        return item;
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

        public IEnumerable<ItemDTO> LoadAll()
        {
            lock (_loadLock)
            {
                if (Volatile.Read(ref _isFullyLoaded) == 1)
                {
                    return _cache.GetAll().ToList();
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = new List<ItemDTO>();
                    var cacheItems = new List<KeyValuePair<short, ItemDTO>>();
                    foreach (var item in context.Item.AsNoTracking())
                    {
                        var dto = new ItemDTO();
                        ItemMapper.ToItemDTO(item, dto);
                        cacheItems.Add(new KeyValuePair<short, ItemDTO>(dto.VNum, dto));
                        result.Add(dto);
                    }

                    _cache.ReplaceAll(cacheItems);
                    Volatile.Write(ref _isFullyLoaded, 1);
                    return result;
                }
            }
        }

        public CacheStatisticsSnapshot GetCacheStatistics() => _cache.GetStatistics();

        public ItemDTO LoadById(short vNum)
        {
            try
            {
                if (_cache.TryGetValue(vNum, out var cachedDto))
                {
                    return cachedDto;
                }

                lock (_loadLock)
                {
                    if (_cache.TryGetValue(vNum, out cachedDto))
                    {
                        return cachedDto;
                    }

                    using (var context = DataAccessHelper.CreateContext())
                    {
                        var dto = new ItemDTO();
                        if (ItemMapper.ToItemDTO(context.Item.AsNoTracking().FirstOrDefault(i => i.VNum.Equals(vNum)), dto))
                        {
                            _cache.Set(vNum, dto);
                            return dto;
                        }

                        return null;
                    }
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