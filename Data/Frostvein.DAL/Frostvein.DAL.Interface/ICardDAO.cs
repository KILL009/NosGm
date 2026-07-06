using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface ICardDAO
    {
        #region Methods

        CardDTO Insert(ref CardDTO card);

        void Insert(List<CardDTO> cards);

        SaveResult InsertOrUpdate(CardDTO card);

        IEnumerable<CardDTO> LoadAll();

        CardDTO LoadById(short cardId);

        #endregion
    }
}