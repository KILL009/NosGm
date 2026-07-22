namespace NosGm.DAL.Interface.PropertiesMapping
{
    public interface IModuleMapper<TDTOEntity, TEntity>
    {
        bool ToDTO(TEntity input, TDTOEntity output);
        bool ToEntity(TDTOEntity input, TEntity output);
    }
}
