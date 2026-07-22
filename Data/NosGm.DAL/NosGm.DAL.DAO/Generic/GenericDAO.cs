using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.Interface.PropertiesMapping;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace NosGm.DAL.DAO.Generic
{
    public class GenericDAO<TDTOEntity, TEntity> where TDTOEntity : class where TEntity : class
    {
        public TDTOEntity InsertOrUpdate(NosGmContext context, TDTOEntity dtoEntity, DbSet<TEntity> contextList, IModuleMapper<TDTOEntity, TEntity> moduleMapper, Func<TEntity, bool> condition)
        {
            try
            {
                var entity = contextList.Where(condition).FirstOrDefault();

                if (entity == null) return Insert(dtoEntity, contextList, moduleMapper, context);
                return Update(entity, dtoEntity, moduleMapper, context);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), dtoEntity, e.Message),
                    e);
                return dtoEntity;
            }
        }

        [Obsolete("Have to find a way to build the condition")]
        public void InsertOrUpdateFromList(NosGmContext context, List<TDTOEntity> dtoEntityList, DbSet<TEntity> contextList, IModuleMapper<TDTOEntity, TEntity> moduleMapper, Func<TEntity, bool> condition)
        {
            try
            {
                foreach (var dtoEntity in dtoEntityList)
                {
                    var entity = contextList.FirstOrDefault(condition);

                    if (entity == null) Insert(dtoEntity, contextList, moduleMapper, context);
                    Update(entity, dtoEntity, moduleMapper, context);
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), dtoEntityList, e.Message),
                    e);
            }
        }

        private TDTOEntity Insert(TDTOEntity dtoEntity, DbSet<TEntity> contextList, IModuleMapper<TDTOEntity, TEntity> moduleMapper, NosGmContext context)
        {
            var entity = Activator.CreateInstance<TEntity>();
            moduleMapper.ToEntity(dtoEntity, entity);
            contextList.Add(entity);
            context.SaveChanges();
            if (moduleMapper.ToDTO(entity, dtoEntity)) return dtoEntity;

            return null;
        }

        private TDTOEntity Update(TEntity entity, TDTOEntity dtoEntity, IModuleMapper<TDTOEntity, TEntity> moduleMapper, NosGmContext context)
        {
            if (entity != null)
            {
                moduleMapper.ToEntity(dtoEntity, entity);
                context.SaveChanges();
            }

            if (moduleMapper.ToDTO(entity, dtoEntity)) return dtoEntity;

            return null;
        }
    }
}
