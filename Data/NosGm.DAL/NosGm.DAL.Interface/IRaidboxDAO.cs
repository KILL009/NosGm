using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IRaidboxDAO
    {
        #region Methods

        RaidboxDTO Insert(RaidboxDTO item);

        IEnumerable<RaidboxDTO> LoadAll();

        RaidboxDTO LoadById(short id);

        IEnumerable<RaidboxDTO> LoadByItemVNum(short vnum);

        IEnumerable<RaidboxDTO> LoadByItemVNumAndDesign(short vnum, short design);

        #endregion
    }
}