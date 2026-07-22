namespace ChickenAPI.DAL
{
    public interface IGenericMappedRepository<T> :
        IGenericAsyncMappedRepository<T>,
        IGenericSyncMappedRepository<T> where T : class, IMappedDto
    {
    }
}