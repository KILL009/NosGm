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
    public class MapDAO : IMapDAO
    {
        private static readonly ICacheService<short, MapDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, MapDTO>(dto => dto.Clone());
        private static int _isFullyLoaded;
        private static readonly object _loadLock = new object();

        #region Methods

        public void Insert(List<MapDTO> maps)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Item in maps)
                    {
                        var entity = new Map();
                        MapMapper.ToMap(Item, entity);
                        context.Map.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
                
                _cache.Clear();
                Volatile.Write(ref _isFullyLoaded, 0);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public MapDTO Insert(MapDTO map)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (context.Map.FirstOrDefault(c => c.MapId.Equals(map.MapId)) == null)
                    {
                        var entity = new Map();
                        MapMapper.ToMap(map, entity);
                        context.Map.Add(entity);
                        context.SaveChanges();
                        if (MapMapper.ToMapDTO(entity, map))
                        {
                            _cache.Set(map.MapId, map);
                            return map;
                        }

                        return null;
                    }

                    return new MapDTO();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<MapDTO> LoadAll()
        {
            if (Volatile.Read(ref _isFullyLoaded) == 1)
            {
                return _cache.GetAll();
            }

            lock (_loadLock)
            {
                if (Volatile.Read(ref _isFullyLoaded) == 1)
                {
                    return _cache.GetAll();
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = new List<MapDTO>();
                    var cacheItems = new List<KeyValuePair<short, MapDTO>>();
                    foreach (var Map in context.Map.AsNoTracking())
                    {
                        var dto = new MapDTO();
                        MapMapper.ToMapDTO(Map, dto);
                        cacheItems.Add(new KeyValuePair<short, MapDTO>(dto.MapId, dto));
                        result.Add(dto);
                    }

                    _cache.ReplaceAll(cacheItems);
                    Volatile.Write(ref _isFullyLoaded, 1);
                    return result;
                }
            }
        }

        public MapDTO LoadById(short mapId)
        {
            try
            {
                if (_cache.TryGetValue(mapId, out var cachedDto))
                {
                    return cachedDto;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new MapDTO();
                    if (MapMapper.ToMapDTO(context.Map.AsNoTracking().FirstOrDefault(c => c.MapId.Equals(mapId)), dto))
                    {
                        _cache.Set(mapId, dto);
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