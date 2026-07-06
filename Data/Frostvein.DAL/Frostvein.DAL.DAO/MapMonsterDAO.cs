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
    public class MapMonsterDAO : IMapMonsterDAO
    {
        #region Methods

        public IEnumerable<MapMonsterDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapMonsterDTO>();
                foreach (var MapMonster in context.MapMonster)
                {
                    var dto = new MapMonsterDTO();
                    MapMonsterMapper.ToMapMonsterDTO(MapMonster, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public DeleteResult DeleteById(int mapMonsterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var monster = context.MapMonster.First(i => i.MapMonsterId.Equals(mapMonsterId));

                    if (monster != null)
                    {
                        context.MapMonster.Remove(monster);
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

        public bool DoesMonsterExist(int mapMonsterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return context.MapMonster.Any(i => i.MapMonsterId.Equals(mapMonsterId));
            }
        }

        public void Insert(IEnumerable<MapMonsterDTO> mapMonsters)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var monster in mapMonsters)
                    {
                        var entity = new MapMonster();
                        MapMonsterMapper.ToMapMonster(monster, entity);
                        context.MapMonster.Add(entity);
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

        public MapMonsterDTO Insert(MapMonsterDTO mapMonster)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new MapMonster();
                    MapMonsterMapper.ToMapMonster(mapMonster, entity);
                    context.MapMonster.Add(entity);
                    context.SaveChanges();
                    if (MapMonsterMapper.ToMapMonsterDTO(entity, mapMonster)) return mapMonster;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public MapMonsterDTO LoadById(int mapMonsterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new MapMonsterDTO();
                    if (MapMonsterMapper.ToMapMonsterDTO(
                        context.MapMonster.FirstOrDefault(i => i.MapMonsterId.Equals(mapMonsterId)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<MapMonsterDTO> LoadFromMap(short mapId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapMonsterDTO>();
                foreach (var MapMonsterobject in context.MapMonster.Where(c => c.MapId.Equals(mapId)))
                {
                    var dto = new MapMonsterDTO();
                    MapMonsterMapper.ToMapMonsterDTO(MapMonsterobject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public SaveResult Update(ref MapMonsterDTO mapMonster)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mapMonsterId = mapMonster.MapMonsterId;
                    var entity = context.MapMonster.FirstOrDefault(c => c.MapMonsterId.Equals(mapMonsterId));

                    mapMonster = update(entity, mapMonster, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_MAPMONSTER_ERROR"),
                        mapMonster.MapMonsterId, e.Message), e);
                return SaveResult.Error;
            }
        }

        private static MapMonsterDTO update(MapMonster entity, MapMonsterDTO mapMonster, FrostveinContext context)
        {
            if (entity != null)
            {
                MapMonsterMapper.ToMapMonster(mapMonster, entity);
                context.Entry(entity).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (MapMonsterMapper.ToMapMonsterDTO(entity, mapMonster)) return mapMonster;

            return null;
        }

        #endregion
    }
}