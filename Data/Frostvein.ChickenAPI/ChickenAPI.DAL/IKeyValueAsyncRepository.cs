using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChickenAPI.DAL
{
    public interface IKeyValueAsyncRepository<TObject, TKey>
    {
        /// <summary>
        /// Gets all the objects stored within the cache
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<TObject>> GetAllAsync();


        /// <summary>
        /// Gets all the objects that are contained in the given id enumerable
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<IEnumerable<TObject>> GetByIdsAsync(IEnumerable<TKey> ids);

        /// <summary>
        /// Gets the object with the given key from the cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TObject> GetByIdAsync(TKey id);

        /// <summary>
        /// Registers the object that are contained
        /// </summary>
        /// <param name="id"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task RegisterAsync(TKey id, TObject obj);

        /// <summary>
        /// Asynchronously registers all the objects given as parameter assuming that these objects contains a key
        /// </summary>
        /// <param name="objs"></param>
        Task RegisterAsync(IEnumerable<(TKey, TObject)> objs);

        /// <summary>
        /// Asynchronously removes the object and returns it
        /// </summary>
        /// <param name="id"></param>
        Task<TObject> RemoveAsync(TKey id);


        /// <summary>
        /// Asynchronously removes the objects with the given keys
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<IEnumerable<TObject>> RemoveAsync(IEnumerable<TKey> ids);
    }
}