using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IGmSanctionDAO
    {
        bool IsAvailable();

        GmSanctionResultDTO Execute(GmSanctionRequestDTO request);

        IEnumerable<GmSanctionActionDTO> LoadByCase(long caseId, int take = 20);
    }
}
