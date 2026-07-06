using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IMapTypeDAO
    {
        #region Methods

        MapTypeDTO Insert(ref MapTypeDTO mapType);

        void Insert(List<MapTypeDTO> mapType);

        IEnumerable<MapTypeDTO> LoadAll();

        MapTypeDTO LoadById(short maptypeId);

        #endregion
    }
}