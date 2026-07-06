using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class MapDAO : IMapDAO
    {
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
                        if (MapMapper.ToMapDTO(entity, map)) return map;

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
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapDTO>();
                foreach (var Map in context.Map)
                {
                    var dto = new MapDTO();
                    MapMapper.ToMapDTO(Map, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public MapDTO LoadById(short mapId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new MapDTO();
                    if (MapMapper.ToMapDTO(context.Map.FirstOrDefault(c => c.MapId.Equals(mapId)), dto)) return dto;

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