using System;
using System.Collections.Generic;

namespace ChickenAPI.DAL
{
    public interface IGenericSyncRepository<TObject, in TObjectId> where TObject : class
    {
        /// <summary>
        ///     Returns every objects of type TObject from data storage
        /// </summary>
        /// <returns></returns>
        IEnumerable<TObject> GetAll();

        /// <summary>
        ///     Returns every objects of type TObject from data storage that matches with the given predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IEnumerable<TObject> GetAllMatching(Func<TObject, bool> predicate);


        /// <summary>
        ///     Returns the first
        ///     <typeparam name="TObject"></typeparam>
        ///     from the data storage that matches with the given predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        TObject GetFirstMatchingOrDefault(Func<TObject, bool> predicate);

        /// <summary>
        ///     Returns the object from data storage by its id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TObject GetById(TObjectId id);


        /// <summary>
        ///     Returns all the objects from data storage by their ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        IEnumerable<TObject> GetByIds(IEnumerable<TObjectId> ids);


        /// <summary>
        ///     Inserts or updates object given in parameter into data storage
        /// </summary>
        /// <param name="obj"></param>
        TObject Save(TObject obj);

        /// <summary>
        ///     Inserts or update all the objects given in parameter into data storage
        /// </summary>
        /// <param name="objs"></param>
        List<TObject> Save(IEnumerable<TObject> objs);

        /// <summary>
        ///     Delete the object from the data storage with the given id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        void DeleteById(TObjectId id);

        /// <summary>
        ///     Delete all the objects from the data storage with the given id
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        void DeleteByIds(IEnumerable<TObjectId> ids);
    }
}