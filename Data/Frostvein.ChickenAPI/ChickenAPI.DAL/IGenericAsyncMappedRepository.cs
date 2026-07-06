namespace ChickenAPI.DAL
{
    /// <summary>
    ///     IGenericAsyncMappedRepository permits to manage specialize an IGenericAsyncRepository for IMappedDto objects (which
    ///     are managed by int)
    /// </summary>
    /// <typeparam name="TDto"></typeparam>
    public interface IGenericAsyncMappedRepository<TDto> : IGenericAsyncRepository<TDto, int> where TDto : class, IMappedDto
    {
    }
}