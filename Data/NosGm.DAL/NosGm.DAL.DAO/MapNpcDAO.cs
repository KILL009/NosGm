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
    public class MapNpcDAO : IMapNpcDAO
    {
        #region Methods

        public DeleteResult DeleteById(int mapNpcId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var npc = context.MapNpc.First(i => i.MapNpcId.Equals(mapNpcId));

                    if (npc != null)
                    {
                        context.MapNpc.Remove(npc);
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

        public bool DoesNpcExist(int mapNpcId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                return context.MapNpc.Any(i => i.MapNpcId.Equals(mapNpcId));
            }
        }

        public void Insert(List<MapNpcDTO> npcs)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Item in npcs)
                    {
                        var entity = new MapNpc();
                        MapNpcMapper.ToMapNpc(Item, entity);
                        context.MapNpc.Add(entity);
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

        public MapNpcDTO Insert(MapNpcDTO npc)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new MapNpc();
                    MapNpcMapper.ToMapNpc(npc, entity);
                    context.MapNpc.Add(entity);
                    context.SaveChanges();
                    if (MapNpcMapper.ToMapNpcDTO(entity, npc)) return npc;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<MapNpcDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapNpcDTO>();
                foreach (var entity in context.MapNpc)
                {
                    var dto = new MapNpcDTO();
                    MapNpcMapper.ToMapNpcDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public MapNpcDTO LoadById(int mapNpcId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new MapNpcDTO();
                    if (MapNpcMapper.ToMapNpcDTO(context.MapNpc.FirstOrDefault(i => i.MapNpcId.Equals(mapNpcId)), dto))
                        return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<MapNpcDTO> LoadFromMap(short mapId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MapNpcDTO>();
                foreach (var npcobject in context.MapNpc.Where(c => c.MapId.Equals(mapId)))
                {
                    var dto = new MapNpcDTO();
                    MapNpcMapper.ToMapNpcDTO(npcobject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public SaveResult Update(ref MapNpcDTO mapNpc)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mapNpcId = mapNpc.MapNpcId;
                    var entity = context.MapNpc.FirstOrDefault(c => c.MapNpcId.Equals(mapNpcId));

                    mapNpc = update(entity, mapNpc, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_MAPNPC_ERROR"), mapNpc.MapNpcId,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        private static MapNpcDTO update(MapNpc entity, MapNpcDTO mapNpc, NosGmContext context)
        {
            if (entity != null)
            {
                MapNpcMapper.ToMapNpc(mapNpc, entity);
                context.Entry(entity).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (MapNpcMapper.ToMapNpcDTO(entity, mapNpc)) return mapNpc;

            return null;
        }

        #endregion
    }
}