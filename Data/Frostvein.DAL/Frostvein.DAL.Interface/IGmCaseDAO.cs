using Frostvein.Data;
using Frostvein.Domain;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IGmCaseDAO
    {
        bool IsAvailable();

        GmCaseDTO Create(GmCaseDTO caseFile, GmCaseNoteDTO initialNote);

        GmCaseDTO LoadById(long caseId);

        IEnumerable<GmCaseDTO> LoadRecent(int take = 20);

        IEnumerable<GmCaseDTO> LoadByAssignedAccount(long accountId, int take = 20);

        IEnumerable<GmCaseDTO> LoadBySubject(long accountId, long? characterId, int take = 20);

        IEnumerable<GmCaseNoteDTO> LoadNotes(long caseId, int take = 30);

        GmCaseNoteDTO AddNote(GmCaseNoteDTO note);

        GmCaseDTO Assign(
            long caseId,
            long? assignedAccountId,
            long? assignedCharacterId,
            string assignedName,
            GmCaseNoteDTO auditNote);

        GmCaseDTO UpdateStatus(long caseId, GmCaseStatus status, GmCaseNoteDTO auditNote);

        GmCaseDTO UpdatePriority(long caseId, GmCasePriority priority, GmCaseNoteDTO auditNote);
    }
}