using System.Collections.Generic;

namespace Frostvein.Mapper
{
    public interface IMapper<TDto, TEntity> where TDto : class, new() where TEntity : class, new()
    {
        TEntity Map(TDto input);

        TDto Map(TEntity input);

        IEnumerable<TDto> Map(IEnumerable<TEntity> input);

        IEnumerable<TEntity> Map(IEnumerable<TDto> input);
    }
}
