using System;

namespace ChickenAPI.DAL
{
    public interface IGenericAsyncUuidRepository<T> : IGenericAsyncRepository<T, Guid> where T : class, IUuidDto
    {
    }
}