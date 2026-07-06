using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IFishingPositionDao
    {
        SaveResult InsertOrUpdate(List<FishingPositionDto> positions);

        IEnumerable<FishingPositionDto> LoadAll();
    }
}
