namespace ChickenAPI.DAL
{
    public interface IGenericUuidRepository<T> : IGenericAsyncUuidRepository<T>, IGenericSyncUuidRepository<T> where T : class, IUuidDto
    {
    }
}