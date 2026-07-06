using System;

namespace ChickenAPI.DAL
{
    public interface IGenericSyncUuidRepository<T> : IGenericSyncRepository<T, Guid> where T : class, IUuidDto
    {
    }
}