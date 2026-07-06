using System.Collections.Generic;

namespace ChickenAPI.DAL
{
    public interface IKeyValueSyncRepository<TObject, in TKey>
    {

        /// <summary>
        /// Gets all the objects stored within the cache
        /// </summary>
        /// <returns></returns>
        IEnumerable<TObject> GetAll();


        /// <summary>
        /// Gets all the objects that are contained in the given id enumerable
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        IEnumerable<TObject> GetByIds(IEnumerable<TKey> ids);

        /// <summary>
        /// Gets the object with the given key from the cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TObject GetById(TKey id);

        /// <summary>
        /// Registers the object that are contained
        /// </summary>
        /// <param name="id"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        void Register(TKey id, TObject obj);

        /// <summary>
        /// Registers the object assuming that the object contains a key
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        void Register(TObject obj);

        /// <summary>
        /// Asynchronously registers all the objects given as parameter assuming that these objects contains a key
        /// </summary>
        /// <param name="objs"></param>
        void Register(IEnumerable<TObject> objs);

        /// <summary>
        /// Asynchronously removes the object and returns it
        /// </summary>
        /// <param name="id"></param>
        TObject Remove(TKey id);


        /// <summary>
        /// Asynchronously removes the objects with the given keys
        /// </summary>
        /// <param name="ids"></param>
        IEnumerable<TObject> Remove(IEnumerable<TKey> ids);
    }
}