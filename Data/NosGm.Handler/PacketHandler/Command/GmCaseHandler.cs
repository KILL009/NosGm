using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Packets.Packets.CommandPackets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    /// <summary>
    /// Read-only account/character inspector plus persistent staff case workflow.
    /// The inspector deliberately omits passwords, verification tokens and email data.
    /// </summary>
    public sealed class GmCaseHandler : IPacketHandler
    {
        private const int DefaultTake = 10;
        private const int MaximumTake = 30;

        public GmCaseHandler(ClientSession session) => Session = session;

        public ClientSession Session { get; }

        public void Inspect(PlayerInspectPacket packet)
        {
            string[] parts = Split(packet?.Contents);
            if (parts.Length < 2 ||
                !TryResolveSubject(parts[0], parts[1], out AccountDTO account,
                    out CharacterDTO character, out GmCaseSubjectType subjectType))
            {
                Send(PlayerInspectPacket.ReturnHelp(), 10);
                return;
            }

            ShowInspection(account, character, subjectType);
        }

        public void GmCase(GmCasePacket packet)
        {
            string[] parts = Split(packet?.Contents);
            string mode = parts.FirstOrDefault()?.ToLowerInvariant() ?? "help";

            switch (mode)
            {
                case "status":
                    Send($"GM case schema: {(GmCaseService.Instance.IsAvailable() ? "AVAILABLE" : "MISSING")}", 11);
                    Send("Cases are persistent; notes, evidence, assignments and state changes are historical entries.", 10);
                    return;

                case "open":
                    OpenCase(parts);
                    return;

                case "show":
                    if (!TryReadCaseId(parts, 1, out long showCaseId))
                    {
                        SendCaseHelp();
                        return;
                    }
                    ShowCase(showCaseId, ReadTake(parts, 2, 20));
                    return;

                case "recent":
                    WriteCases(GmCaseService.Instance.GetRecent(ReadTake(parts, 1)).ToList(), "Recent GM cases");
                    return;

                case "mine":
                    WriteCases(GmCaseService.Instance.GetMine(
                        Session.Account.AccountId, ReadTake(parts, 1)).ToList(), "My active GM cases");
                    return;

                case "subject":
                    ShowSubjectCases(parts);
                    return;

                case "note":
                    AddNote(parts);
                    return;

                case "evidence":
                case "proof":
                    AddEvidence(parts);
                    return;

                case "assign":
                    AssignCase(parts);
                    return;

                case "state":
                case "setstate":
                    ChangeState(parts);
                    return;

                case "priority":
                    ChangePriority(parts);
                    return;

                default:
                    SendCaseHelp();
                    return;
            }
        }

        private void OpenCase(string[] parts)
        {
            if (parts.Length < 5 ||
                !TryResolveSubject(parts[1], parts[2], out AccountDTO account,
                    out CharacterDTO character, out GmCaseSubjectType subjectType) ||
                !TryParsePriority(parts[3], out GmCasePriority priority))
            {
                SendCaseHelp();
                return;
            }

            string reason = Join(parts, 4);
            if (string.IsNullOrWhiteSpace(reason))
            {
                Send("A case requires a reason.", 11);
                return;
            }

            GmCaseDTO created;
            try
            {
                created = GmCaseService.Instance.Create(
                    subjectType,
                    account.AccountId,
                    character?.CharacterId,
                    character?.Name ?? account.Name,
                    priority,
                    reason,
                    Session.Account.AccountId,
                    Session.Character?.CharacterId,
                    Session.Character?.Name ?? Session.Account.Name);
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to validate a new GM case.", exception);
                Send("The case could not be created. Check the reason and server log.", 11);
                return;
            }

            if (created == null)
            {
                Send("The case could not be persisted. Apply the GM case migration and inspect the server log.", 11);
                return;
            }

            Send($"GM case #{created.CaseId} created and assigned to you.", 10);
            WriteCaseRow(created);
        }

        private void ShowSubjectCases(string[] parts)
        {
            if (parts.Length < 3 ||
                !TryResolveSubject(parts[1], parts[2], out AccountDTO account,
                    out CharacterDTO character, out GmCaseSubjectType subjectType))
            {
                SendCaseHelp();
                return;
            }

            WriteCases(
                GmCaseService.Instance.GetBySubject(account.AccountId, character?.CharacterId,
                    ReadTake(parts, 3)).ToList(),
                $"Cases for {character?.Name ?? account.Name} ({subjectType})");
        }

        private void AddNote(string[] parts)
        {
            if (!TryReadCaseId(parts, 1, out long caseId) || parts.Length < 3)
            {
                SendCaseHelp();
                return;
            }

            GmCaseNoteDTO note = GmCaseService.Instance.AddNote(
                caseId,
                Join(parts, 2),
                Session.Account.AccountId,
                Session.Character?.CharacterId,
                Session.Character?.Name ?? Session.Account.Name);
            Send(note == null ? "The note could not be added." : $"Note #{note.NoteId} added to case #{caseId}.",
                note == null ? 11 : 10);
        }

        private void AddEvidence(string[] parts)
        {
            if (!TryReadCaseId(parts, 1, out long caseId) || parts.Length < 3)
            {
                SendCaseHelp();
                return;
            }

            GmCaseNoteDTO note = GmCaseService.Instance.AddEvidence(
                caseId,
                parts[2],
                Join(parts, 3),
                Session.Account.AccountId,
                Session.Character?.CharacterId,
                Session.Character?.Name ?? Session.Account.Name);
            Send(note == null ? "The evidence could not be added." : $"Evidence #{note.NoteId} added to case #{caseId}.",
                note == null ? 11 : 10);
        }

        private void AssignCase(string[] parts)
        {
            if (!TryReadCaseId(parts, 1, out long caseId) || parts.Length < 3)
            {
                SendCaseHelp();
                return;
            }

            long? assignedAccountId;
            long? assignedCharacterId = null;
            string assignedName;
            string target = parts[2];

            if (string.Equals(target, "me", StringComparison.OrdinalIgnoreCase))
            {
                assignedAccountId = Session.Account.AccountId;
                assignedCharacterId = Session.Character?.CharacterId;
                assignedName = Session.Character?.Name ?? Session.Account.Name;
            }
            else if (string.Equals(target, "none", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(target, "clear", StringComparison.OrdinalIgnoreCase))
            {
                assignedAccountId = null;
                assignedName = null;
            }
            else
            {
                AccountDTO account = ResolveAccount(target);
                if (account == null)
                {
                    Send($"Account '{target}' was not found.", 11);
                    return;
                }
                if (account.Authority < AuthorityType.GS)
                {
                    Send("Cases can only be assigned to staff accounts.", 11);
                    return;
                }
                assignedAccountId = account.AccountId;
                assignedName = account.Name;
            }

            GmCaseDTO updated = GmCaseService.Instance.Assign(
                caseId,
                assignedAccountId,
                assignedCharacterId,
                assignedName,
                Session.Account.AccountId,
                Session.Character?.CharacterId,
                Session.Character?.Name ?? Session.Account.Name);
            Send(updated == null ? "The case assignment could not be changed." : $"Case #{caseId} assignment updated.",
                updated == null ? 11 : 10);
            if (updated != null) WriteCaseRow(updated);
        }

        private void ChangeState(string[] parts)
        {
            if (!TryReadCaseId(parts, 1, out long caseId) || parts.Length < 3 ||
                !TryParseStatus(parts[2], out GmCaseStatus status))
            {
                SendCaseHelp();
                return;
            }

            GmCaseDTO updated = GmCaseService.Instance.UpdateStatus(
                caseId,
                status,
                Join(parts, 3),
                Session.Account.AccountId,
                Session.Character?.CharacterId,
                Session.Character?.Name ?? Session.Account.Name);
            Send(updated == null ? "The case state could not be changed." : $"Case #{caseId} state changed to {status}.",
                updated == null ? 11 : 10);
            if (updated != null) WriteCaseRow(updated);
        }

        private void ChangePriority(string[] parts)
        {
            if (!TryReadCaseId(parts, 1, out long caseId) || parts.Length < 3 ||
                !TryParsePriority(parts[2], out GmCasePriority priority))
            {
                SendCaseHelp();
                return;
            }

            GmCaseDTO updated = GmCaseService.Instance.UpdatePriority(
                caseId,
                priority,
                Join(parts, 3),
                Session.Account.AccountId,
                Session.Character?.CharacterId,
                Session.Character?.Name ?? Session.Account.Name);
            Send(updated == null ? "The case priority could not be changed." : $"Case #{caseId} priority changed to {priority}.",
                updated == null ? 11 : 10);
            if (updated != null) WriteCaseRow(updated);
        }

        private void ShowCase(long caseId, int takeNotes)
        {
            GmCaseDTO caseFile = GmCaseService.Instance.Get(caseId);
            if (caseFile == null)
            {
                Send($"Case #{caseId} was not found.", 11);
                return;
            }

            Send($"===== GM case #{caseFile.CaseId} =====", 12);
            WriteCaseRow(caseFile);
            Send($"Summary: {LimitDisplay(caseFile.Summary, 220)}", 10);
            Send("----- History -----", 10);

            List<GmCaseNoteDTO> notes = GmCaseService.Instance.GetNotes(caseId, takeNotes).ToList();
            if (notes.Count == 0)
            {
                Send("No case notes were found.", 11);
                return;
            }

            foreach (GmCaseNoteDTO note in notes.OrderBy(note => note.OccurredAtUtc).ThenBy(note => note.NoteId))
            {
                string reference = string.IsNullOrWhiteSpace(note.Reference)
                    ? string.Empty
                    : $" ref={LimitDisplay(note.Reference, 70)}";
                Send($"{note.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z [{note.NoteType}] " +
                     $"{note.AuthorName ?? note.AuthorAccountId.ToString()}: {LimitDisplay(note.Text, 180)}{reference}",
                    note.NoteType == GmCaseNoteType.Evidence ? 13 : 10);
            }
        }

        private void ShowInspection(AccountDTO account, CharacterDTO character, GmCaseSubjectType subjectType)
        {
            bool revealIp = Session.Account.Authority >= AuthorityType.ADMIN;
            IEnumerable<CharacterDTO> accountCharacters = DAOFactory.CharacterDAO.LoadAllByAccount(account.AccountId) ??
                                                           Enumerable.Empty<CharacterDTO>();
            List<CharacterDTO> characters = accountCharacters.OrderBy(entry => entry.Slot).ToList();
            List<PenaltyLogDTO> penalties = (DAOFactory.PenaltyLogDAO.LoadByAccount(account.AccountId) ??
                                             Enumerable.Empty<PenaltyLogDTO>())
                .OrderByDescending(entry => entry.DateStart)
                .ToList();
            List<PenaltyLogDTO> activePenalties = penalties.Where(entry => entry.DateEnd > DateTime.Now).ToList();
            List<GmCaseDTO> cases = GmCaseService.Instance
                .GetBySubject(account.AccountId, character?.CharacterId, 10)
                .ToList();

            Send($"===== Player inspector: {character?.Name ?? account.Name} =====", 12);
            Send($"Account id={account.AccountId} name={account.Name} authority={account.Authority} language={account.Language ?? "-"}", 10);
            Send($"Registration IP={DisplayIp(account.RegistrationIP, revealIp)} | characters={characters.Count} | penalties={penalties.Count} active={activePenalties.Count}", 10);

            if (character != null)
            {
                ClientSession liveSession = ServerManager.Instance.GetSessionByCharacterId(character.CharacterId);
                string online = liveSession?.IsConnected == true ? "ONLINE" : "OFFLINE";
                Send($"Character id={character.CharacterId} slot={character.Slot} state={character.State} {online}", 10);
                Send($"Class={character.Class} gender={character.Gender} level={character.Level} job={character.JobLevel} hero={character.HeroLevel}", 10);
                Send($"Gold={character.Gold:N0} bank={character.GoldBank:N0} reputation={character.Reputation:N0} dignity={character.Dignity:N0}", 10);
                Send($"Map={character.MapId} ({character.MapX},{character.MapY}) faction={character.Faction} currentIP={DisplayIp(character.CurrentIp, revealIp)}", 10);
            }
            else
            {
                foreach (CharacterDTO entry in characters.Take(8))
                {
                    Send($"char={entry.Name} id={entry.CharacterId} slot={entry.Slot} state={entry.State} class={entry.Class} lvl={entry.Level}+{entry.HeroLevel}", 10);
                }
            }

            if (activePenalties.Count > 0)
            {
                Send("----- Active penalties -----", 11);
                foreach (PenaltyLogDTO penalty in activePenalties.Take(5))
                {
                    Send($"#{penalty.PenaltyLogId} {penalty.Penalty} until={penalty.DateEnd:yyyy-MM-dd HH:mm} by={penalty.AdminName ?? "-"} reason={LimitDisplay(penalty.Reason, 100)}", 11);
                }
            }

            WriteCases(cases, $"Related cases ({subjectType})", false);
            Send("Inspector intentionally hides password, email and verification-token fields.", 10);
        }

        private void WriteCases(IReadOnlyCollection<GmCaseDTO> cases, string title, bool showEmpty = true)
        {
            Send($"===== {title} =====", 12);
            if (cases == null || cases.Count == 0)
            {
                if (showEmpty) Send("No matching cases were found.", 10);
                return;
            }

            foreach (GmCaseDTO caseFile in cases) WriteCaseRow(caseFile);
        }

        private void WriteCaseRow(GmCaseDTO caseFile)
        {
            string assigned = caseFile.AssignedAccountId.HasValue
                ? caseFile.AssignedName ?? caseFile.AssignedAccountId.Value.ToString()
                : "unassigned";
            Send($"#{caseFile.CaseId} [{caseFile.Priority}/{caseFile.Status}] " +
                 $"subject={caseFile.SubjectName ?? caseFile.SubjectAccountId.ToString()} " +
                 $"assigned={assigned} notes={caseFile.NoteCount} updated={caseFile.UpdatedAtUtc:yyyy-MM-dd HH:mm}Z " +
                 $"title={LimitDisplay(caseFile.Title, 90)}",
                caseFile.Priority >= GmCasePriority.High ? 11 : 10);
        }

        private bool TryResolveSubject(
            string subjectValue,
            string targetValue,
            out AccountDTO account,
            out CharacterDTO character,
            out GmCaseSubjectType subjectType)
        {
            account = null;
            character = null;
            subjectType = GmCaseSubjectType.Account;
            string normalized = (subjectValue ?? string.Empty).Trim().ToLowerInvariant();

            if (normalized == "character" || normalized == "char" || normalized == "player")
            {
                subjectType = GmCaseSubjectType.Character;
                character = long.TryParse(targetValue, out long characterId)
                    ? DAOFactory.CharacterDAO.LoadById(characterId)
                    : DAOFactory.CharacterDAO.LoadByName(targetValue);
                if (character == null)
                {
                    Send($"Character '{targetValue}' was not found.", 11);
                    return false;
                }
                account = DAOFactory.AccountDAO.LoadById(character.AccountId);
            }
            else if (normalized == "account" || normalized == "acc")
            {
                account = ResolveAccount(targetValue);
            }
            else
            {
                return false;
            }

            if (account == null)
            {
                Send($"Account for '{targetValue}' was not found.", 11);
                return false;
            }
            return true;
        }

        private static AccountDTO ResolveAccount(string value) => long.TryParse(value, out long accountId)
            ? DAOFactory.AccountDAO.LoadById(accountId)
            : DAOFactory.AccountDAO.LoadByName(value);

        private void SendCaseHelp()
        {
            Send(GmCasePacket.ReturnHelp(), 10);
            Send("$GmCase status", 10);
            Send("$GmCase open <character|account> <id|name> <low|normal|high|critical> <reason>", 10);
            Send("$GmCase show <caseId> [notes]", 10);
            Send("$GmCase recent [take] | $GmCase mine [take]", 10);
            Send("$GmCase subject <character|account> <id|name> [take]", 10);
            Send("$GmCase note <caseId> <text>", 10);
            Send("$GmCase evidence <caseId> <reference> [description]", 10);
            Send("$GmCase assign <caseId> <me|none|AccountId|AccountName>", 10);
            Send("$GmCase state <caseId> <open|investigating|waiting|resolved|dismissed> [reason]", 10);
            Send("$GmCase priority <caseId> <low|normal|high|critical> [reason]", 10);
        }

        private void Send(string message, int color) =>
            Session.SendPacket(Session.Character.GenerateSay(message, color));

        private static string[] Split(string value) => (value ?? string.Empty)
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private static string Join(string[] parts, int index) =>
            parts.Length <= index ? null : string.Join(" ", parts.Skip(index));

        private static bool TryReadCaseId(string[] parts, int index, out long caseId)
        {
            caseId = 0;
            return parts.Length > index && long.TryParse(parts[index], out caseId) && caseId > 0;
        }

        private static int ReadTake(string[] parts, int index, int defaultValue = DefaultTake)
        {
            if (parts.Length <= index || !int.TryParse(parts[index], out int take)) return defaultValue;
            return Math.Max(1, Math.Min(MaximumTake, take));
        }

        private static bool TryParsePriority(string value, out GmCasePriority priority)
        {
            priority = GmCasePriority.Normal;
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "low": priority = GmCasePriority.Low; return true;
                case "normal": case "medium": priority = GmCasePriority.Normal; return true;
                case "high": priority = GmCasePriority.High; return true;
                case "critical": case "urgent": priority = GmCasePriority.Critical; return true;
                default: return false;
            }
        }

        private static bool TryParseStatus(string value, out GmCaseStatus status)
        {
            status = GmCaseStatus.Open;
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "open": status = GmCaseStatus.Open; return true;
                case "investigating": case "investigation": case "active": status = GmCaseStatus.Investigating; return true;
                case "waiting": case "pending": status = GmCaseStatus.Waiting; return true;
                case "resolved": case "closed": status = GmCaseStatus.Resolved; return true;
                case "dismissed": case "rejected": status = GmCaseStatus.Dismissed; return true;
                default: return false;
            }
        }

        private static string DisplayIp(string value, bool reveal)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            string normalized = value.Replace("tcp://", string.Empty).Trim();
            if (reveal) return LimitDisplay(normalized, 64);

            int separator = normalized.LastIndexOf(':');
            if (separator > normalized.LastIndexOf('.')) normalized = normalized.Substring(0, separator);
            string[] parts = normalized.Split('.');
            return parts.Length == 4 ? $"{parts[0]}.{parts[1]}.*.*" : "masked";
        }

        private static string LimitDisplay(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            string normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength) + "...";
        }
    }
}