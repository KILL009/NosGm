using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IFishingInformationsDao
    {
        SaveResult InsertorUpdate(List<FishingInformationsDto> fishes);

        IEnumerable<FishingInformationsDto> LoadAll();
    }
}
