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
    public class MapTypeMapDAO : IMapTypeMapDAO
    {
        #region Methods

        public void Insert(List<MapTypeMapDTO> mapTypeMaps)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var mapTypeMap in mapTypeMaps)
                    {
                        var entity = new MapTypeMap();
                        MapTypeMapMapper.ToMapTypeMap(mapTypeMap, entity);
                        context.MapTypeMap.Add(entity);
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

        public IEnumerable<MapTypeMapDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapTypeMapDTO>();
                foreach (var MapTypeMap in context.MapTypeMap)
                {
                    var dto = new MapTypeMapDTO();
                    MapTypeMapMapper.ToMapTypeMapDTO(MapTypeMap, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public MapTypeMapDTO LoadByMapAndMapType(short mapId, short maptypeId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new MapTypeMapDTO();
                    if (MapTypeMapMapper.ToMapTypeMapDTO(
                        context.MapTypeMap.FirstOrDefault(i => i.MapId.Equals(mapId) && i.MapTypeId.Equals(maptypeId)),
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

        public IEnumerable<MapTypeMapDTO> LoadByMapId(short mapId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapTypeMapDTO>();
                foreach (var MapTypeMap in context.MapTypeMap.Where(c => c.MapId.Equals(mapId)))
                {
                    var dto = new MapTypeMapDTO();
                    MapTypeMapMapper.ToMapTypeMapDTO(MapTypeMap, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<MapTypeMapDTO> LoadByMapTypeId(short maptypeId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapTypeMapDTO>();
                foreach (var MapTypeMap in context.MapTypeMap.Where(c => c.MapTypeId.Equals(maptypeId)))
                {
                    var dto = new MapTypeMapDTO();
                    MapTypeMapMapper.ToMapTypeMapDTO(MapTypeMap, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}