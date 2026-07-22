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
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class RespawnDAO : IRespawnDAO
    {
        #region Methods

        public SaveResult InsertOrUpdate(ref RespawnDTO respawn)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var CharacterId = respawn.CharacterId;
                    var RespawnMapTypeId = respawn.RespawnMapTypeId;
                    var entity = context.Respawn.FirstOrDefault(c => c.RespawnMapTypeId.Equals(RespawnMapTypeId) && c.CharacterId.Equals(CharacterId));

                    if (entity == null)
                    {
                        respawn = insert(respawn, context);
                        return SaveResult.Inserted;
                    }

                    respawn.RespawnId = entity.RespawnId;
                    respawn = update(entity, respawn, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<RespawnDTO> LoadByCharacter(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RespawnDTO>();
                foreach (var Respawnobject in context.Respawn.Where(i => i.CharacterId.Equals(characterId)))
                {
                    var dto = new RespawnDTO();
                    RespawnMapper.ToRespawnDTO(Respawnobject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public RespawnDTO LoadById(long respawnId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RespawnDTO();
                    if (RespawnMapper.ToRespawnDTO(context.Respawn.FirstOrDefault(s => s.RespawnId.Equals(respawnId)),
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

        private static RespawnDTO insert(RespawnDTO respawn, NosGmContext context)
        {
            try
            {
                var entity = new Respawn();
                RespawnMapper.ToRespawn(respawn, entity);
                context.Respawn.Add(entity);
                context.SaveChanges();
                if (RespawnMapper.ToRespawnDTO(entity, respawn)) return respawn;

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static RespawnDTO update(Respawn entity, RespawnDTO respawn, NosGmContext context)
        {
            if (entity != null)
            {
                RespawnMapper.ToRespawn(respawn, entity);
                context.SaveChanges();
            }

            if (RespawnMapper.ToRespawnDTO(entity, respawn)) return respawn;

            return null;
        }

        public async Task<SaveResult>  InsertOrUpdateAsync(RespawnDTO respawn)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var CharacterId = respawn.CharacterId;
                    var RespawnMapTypeId = respawn.RespawnMapTypeId;
                    var entity = context.Respawn.FirstOrDefault(c => c.RespawnMapTypeId.Equals(RespawnMapTypeId) && c.CharacterId.Equals(CharacterId));

                    if (entity == null)
                    {
                        respawn = await InsertAsync(respawn, context).ConfigureAwait(false);
                        return SaveResult.Inserted;
                    }

                    respawn.RespawnId = entity.RespawnId;
                    respawn = await UpdateAsync(entity, respawn, context).ConfigureAwait(false);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        private async Task<RespawnDTO> InsertAsync(RespawnDTO respawn, NosGmContext context)
        {
            try
            {
                var entity = new Respawn();
                RespawnMapper.ToRespawn(respawn, entity);
                context.Respawn.Add(entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
                if (RespawnMapper.ToRespawnDTO(entity, respawn))
                {
                    return respawn;
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private async Task<RespawnDTO> UpdateAsync(Respawn entity, RespawnDTO respawn, NosGmContext context)
        {
            if (entity != null)
            {
                RespawnMapper.ToRespawn(respawn, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (RespawnMapper.ToRespawnDTO(entity, respawn))
            {
                return respawn;
            }

            return null;
        }

        #endregion
    }
}