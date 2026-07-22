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
    public class RespawnMapTypeDAO : IRespawnMapTypeDAO
    {
        #region Methods

        public void Insert(List<RespawnMapTypeDTO> respawnMapTypes)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var RespawnMapType in respawnMapTypes)
                    {
                        var entity = new RespawnMapType();
                        RespawnMapTypeMapper.ToRespawnMapType(RespawnMapType, entity);
                        context.RespawnMapType.Add(entity);
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

        public SaveResult InsertOrUpdate(ref RespawnMapTypeDTO respawnMapType)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mapId = respawnMapType.DefaultMapId;
                    var entity = context.RespawnMapType.FirstOrDefault(c => c.DefaultMapId.Equals(mapId));

                    if (entity == null)
                    {
                        respawnMapType = insert(respawnMapType, context);
                        return SaveResult.Inserted;
                    }

                    respawnMapType.RespawnMapTypeId = entity.RespawnMapTypeId;
                    respawnMapType = update(entity, respawnMapType, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public RespawnMapTypeDTO LoadById(long respawnMapTypeId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RespawnMapTypeDTO();
                    if (RespawnMapTypeMapper.ToRespawnMapTypeDTO(
                        context.RespawnMapType.FirstOrDefault(s => s.RespawnMapTypeId.Equals(respawnMapTypeId)), dto))
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

        public IEnumerable<RespawnMapTypeDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RespawnMapTypeDTO>();
                foreach (var Map in context.RespawnMapType)
                {
                    var dto = new RespawnMapTypeDTO();
                    RespawnMapTypeMapper.ToRespawnMapTypeDTO(Map, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public RespawnMapTypeDTO LoadByMapId(short mapId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RespawnMapTypeDTO();
                    if (RespawnMapTypeMapper.ToRespawnMapTypeDTO(
                        context.RespawnMapType.FirstOrDefault(s => s.DefaultMapId.Equals(mapId)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static RespawnMapTypeDTO insert(RespawnMapTypeDTO respawnMapType, NosGmContext context)
        {
            try
            {
                var entity = new RespawnMapType();
                RespawnMapTypeMapper.ToRespawnMapType(respawnMapType, entity);
                context.RespawnMapType.Add(entity);
                context.SaveChanges();
                if (RespawnMapTypeMapper.ToRespawnMapTypeDTO(entity, respawnMapType)) return respawnMapType;

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static RespawnMapTypeDTO update(RespawnMapType entity, RespawnMapTypeDTO respawnMapType,
            NosGmContext context)
        {
            if (entity != null)
            {
                RespawnMapTypeMapper.ToRespawnMapType(respawnMapType, entity);
                context.SaveChanges();
            }

            if (RespawnMapTypeMapper.ToRespawnMapTypeDTO(entity, respawnMapType)) return respawnMapType;

            return null;
        }

        #endregion
    }
}