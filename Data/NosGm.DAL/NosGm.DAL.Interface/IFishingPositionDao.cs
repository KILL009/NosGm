using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IFishingPositionDao
    {
        SaveResult InsertOrUpdate(List<FishingPositionDto> positions);

        IEnumerable<FishingPositionDto> LoadAll();
    }
}
