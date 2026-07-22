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
    public class ScriptedInstanceDAO : IScriptedInstanceDAO
    {
        #region Methods

        public void Insert(List<ScriptedInstanceDTO> scriptedInstances)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var scriptedInstance in scriptedInstances)
                    {
                        var entity = new ScriptedInstance();
                        ScriptedInstanceMapper.ToScriptedInstance(scriptedInstance, entity);
                        context.ScriptedInstance.Add(entity);
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

        public ScriptedInstanceDTO Insert(ScriptedInstanceDTO scriptedInstance)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new ScriptedInstance();
                    ScriptedInstanceMapper.ToScriptedInstance(scriptedInstance, entity);
                    context.ScriptedInstance.Add(entity);
                    context.SaveChanges();
                    if (ScriptedInstanceMapper.ToScriptedInstanceDTO(entity, scriptedInstance)) return scriptedInstance;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<ScriptedInstanceDTO> LoadByMap(short mapId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<ScriptedInstanceDTO>();
                foreach (var timespaceObject in context.ScriptedInstance.Where(c => c.MapId.Equals(mapId)))
                {
                    var dto = new ScriptedInstanceDTO();
                    ScriptedInstanceMapper.ToScriptedInstanceDTO(timespaceObject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}