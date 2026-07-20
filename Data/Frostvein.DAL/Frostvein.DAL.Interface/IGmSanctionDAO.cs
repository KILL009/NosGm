using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IGmSanctionDAO
    {
        bool IsAvailable();

        GmSanctionResultDTO Execute(GmSanctionRequestDTO request);

        IEnumerable<GmSanctionActionDTO> LoadByCase(long caseId, int take = 20);
    }
}
