using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IFishingInformationsDao
    {
        SaveResult InsertorUpdate(List<FishingInformationsDto> fishes);

        IEnumerable<FishingInformationsDto> LoadAll();
    }
}
