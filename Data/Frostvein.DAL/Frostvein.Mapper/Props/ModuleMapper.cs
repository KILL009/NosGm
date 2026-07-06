using Frostvein.DAL.Interface.PropertiesMapping;
using System;

namespace Frostvein.Mapper.Props
{
    public abstract class ModuleMapper<TDTOEntity, TEntity> : IModuleMapper<TDTOEntity, TEntity>
    {
        public Type GetTypeDto() => typeof(TDTOEntity);
        public Type GetTypeEntity() => typeof(TEntity);

        public abstract bool ToDTO(TEntity input, TDTOEntity output);
        public abstract bool ToEntity(TDTOEntity input, TEntity output);
    }
}
