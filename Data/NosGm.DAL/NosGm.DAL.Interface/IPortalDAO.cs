using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IPortalDAO
    {
        #region Methods

        PortalDTO Insert(PortalDTO portal);

        void Insert(List<PortalDTO> portals);

        IEnumerable<PortalDTO> LoadByMap(short mapId);

        IEnumerable<PortalDTO> LoadAll();

        #endregion
    }
}