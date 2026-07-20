using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Packets.Packets.CommandPackets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public sealed class SanctionHandler : IPacketHandler
    {
        private const int ConfirmationLifetimeSeconds = 120;
        private const int DefaultTake = 10;
        private const int MaximumTake = 30;

        private static readonly ConcurrentDictionary<long, PendingSanction> PendingByActor =
            new ConcurrentDictionary<long, PendingSanction>();

        public SanctionHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void Sanction(SanctionPacket packet)
        {
            if (Session?.Account == null || Session.Character == null)
            {
                return;
            }

            string[] parts = (packet?.Contents ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string mode = parts.FirstOrDefault()?.ToLowerInvariant() ?? "help";

            switch (mode)
            {
                case "status":
                    ShowStatus();
                    return;
                case "preview":
                case "prepare":
                    Preview(parts);
                    return;
                case "confirm":
                    Confirm(parts);
                    return;
                case "cancel":
                    Cancel();
                    return;
                case "recent":
                case "history":
                    ShowRecent(parts);
                    return;
                default:
                    SendHelp();
                    return;
            }
        }

        private void ShowStatus()
        {
            Send($"Sanction schema: {(GmSanctionService.Instance.IsAvailable() ? "AVAILABLE" : "MISSING")}", 11);
            if (PendingByActor.TryGetValue(Session.Account.AccountId, out PendingSanction pending) &&
                pending.ExpiresAtUtc > DateTime.UtcNow)
            {
                Send($"Pending: case={pending.CaseId} action={pending.ActionType} target={pending.TargetCharacterName} " +
                     $"expires={pending.ExpiresAtUtc:HH:mm:ss}Z", 10);
            }
            else
            {
                PendingByActor.TryRemove(Session.Account.AccountId, out _);
                Send("Pending: none", 10);
            }
        }

        private void Preview(string[] parts)
        {
            if (parts.Length < 6 ||
                !long.TryParse(parts[1], out long caseId) ||
                !TryParseAction(parts[2], out GmSanctionActionType actionType) ||
                !int.TryParse(parts[3], out int duration))
            {
                SendHelp();
                return;
            }

            string targetCharacterName = parts[4];
            string reason = string.Join(" ", parts.Skip(5)).Trim();
            var pending = new PendingSanction
            {
                OperationId = Guid.NewGuid(),
                ConfirmationCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant(),
                CaseId = caseId,
                ActionType = actionType,
                DurationValue = duration,
                TargetCharacterName = targetCharacterName,
                Reason = reason,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(ConfirmationLifetimeSeconds)
            };

            if (!TryBuildRequest(pending, out GmSanctionRequestDTO request, out _, out _, out string error))
            {
                Send(error, 11);
                return;
            }

            PendingByActor[Session.Account.AccountId] = pending;
            Send("===== SANCTION PREVIEW =====", 12);
            Send($"Case: {request.CaseId} | Action: {request.ActionType} | Target: {request.SubjectName}", 10);
            Send($"Account: {request.SubjectAccountId} | Character: {FormatNullable(request.SubjectCharacterId)}", 10);
            Send($"Duration: {FormatDuration(request.ActionType, request.DurationValue)}", 10);
            Send($"End: {(request.PenaltyEnd.HasValue ? request.PenaltyEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : "n/a")}", 10);
            Send($"Reason: {request.Reason}", 10);
            if (request.ActionType == GmSanctionActionType.IpBan)
                Send("IP capture: available from the target's current online session.", 11);
            Send($"Confirm within {ConfirmationLifetimeSeconds} seconds: $Sanction confirm {pending.ConfirmationCode}", 13);
        }

        private void Confirm(string[] parts)
        {
            if (parts.Length < 2)
            {
                Send("A confirmation code is required.", 11);
                return;
            }

            if (!PendingByActor.TryGetValue(Session.Account.AccountId, out PendingSanction pending))
            {
                Send("No pending sanction exists.", 11);
                return;
            }

            if (pending.ExpiresAtUtc <= DateTime.UtcNow)
            {
                PendingByActor.TryRemove(Session.Account.AccountId, out _);
                Send("The pending sanction expired. Create a new preview.", 11);
                return;
            }

            if (!string.Equals(parts[1], pending.ConfirmationCode, StringComparison.OrdinalIgnoreCase))
            {
                Send("The confirmation code is incorrect.", 11);
                return;
            }

            if (!PendingByActor.TryRemove(Session.Account.AccountId, out pending))
            {
                Send("The pending sanction was already consumed or cancelled.", 11);
                return;
            }

            if (!TryBuildRequest(pending, out GmSanctionRequestDTO request,
                    out CharacterDTO targetCharacter, out AccountDTO targetAccount, out string error))
            {
                Send(error, 11);
                return;
            }

            GmSanctionResultDTO result = GmSanctionService.Instance.Execute(request);
            if (result == null || !result.Success)
            {
                Send(result?.Error ?? "The sanction could not be completed.", 11);
                return;
            }

            RefreshPenaltyCache(targetAccount.AccountId);
            ApplyRuntimeEffects(request, targetCharacter);

            string repeated = result.AlreadyCompleted ? " (idempotent replay)" : string.Empty;
            Send($"Sanction completed: {request.ActionType} | case={request.CaseId} | " +
                 $"action={result.Action?.ActionId}{repeated}", 10);
        }

        private void Cancel()
        {
            if (PendingByActor.TryRemove(Session.Account.AccountId, out _))
                Send("Pending sanction cancelled.", 10);
            else
                Send("No pending sanction exists.", 10);
        }

        private void ShowRecent(string[] parts)
        {
            if (parts.Length < 2 || !long.TryParse(parts[1], out long caseId) || caseId <= 0)
            {
                Send("Usage: $Sanction recent <CaseId> [take]", 10);
                return;
            }

            int take = DefaultTake;
            if (parts.Length >= 3 && int.TryParse(parts[2], out int parsed))
                take = Math.Max(1, Math.Min(MaximumTake, parsed));

            List<GmSanctionActionDTO> actions = GmSanctionService.Instance.GetByCase(caseId, take).ToList();
            Send($"===== Sanctions for case {caseId} =====", 12);
            if (actions.Count == 0)
            {
                Send("No sanction actions were found.", 10);
                return;
            }

            foreach (GmSanctionActionDTO action in actions)
            {
                Send($"#{action.ActionId} {action.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z {action.ActionType} " +
                     $"target={action.SubjectName ?? action.SubjectAccountId.ToString()} duration={action.DurationValue} " +
                     $"affected={action.AffectedPenaltyCount} by={action.ActorName}", 10);
            }
        }

        private bool TryBuildRequest(
            PendingSanction pending,
            out GmSanctionRequestDTO request,
            out CharacterDTO targetCharacter,
            out AccountDTO targetAccount,
            out string error)
        {
            request = null;
            targetCharacter = null;
            targetAccount = null;
            error = null;

            if (!GmSanctionService.Instance.IsAvailable())
            {
                error = "Sanction tables are missing. Apply the GM sanction migration.";
                return false;
            }

            targetCharacter = DAOFactory.CharacterDAO.LoadByName(pending.TargetCharacterName);
            if (targetCharacter == null)
            {
                error = "Target character was not found.";
                return false;
            }

            targetAccount = DAOFactory.AccountDAO.LoadById(targetCharacter.AccountId);
            if (targetAccount == null)
            {
                error = "Target account was not found.";
                return false;
            }

            if (targetAccount.AccountId == Session.Account.AccountId)
            {
                error = "You cannot sanction your own account.";
                return false;
            }

            if (targetAccount.Authority >= Session.Account.Authority)
            {
                error = $"Authority protection: target={targetAccount.Authority}, actor={Session.Account.Authority}.";
                return false;
            }

            GmCaseDTO caseFile = GmCaseService.Instance.Get(pending.CaseId);
            if (caseFile == null)
            {
                error = "The GM case does not exist.";
                return false;
            }

            if (caseFile.SubjectAccountId != targetAccount.AccountId ||
                (caseFile.SubjectCharacterId.HasValue && caseFile.SubjectCharacterId.Value != targetCharacter.CharacterId))
            {
                error = "The GM case belongs to a different account or character.";
                return false;
            }

            if (caseFile.Status == GmCaseStatus.Dismissed)
            {
                error = "A dismissed case cannot authorize a sanction.";
                return false;
            }

            if (!pending.ActionType.IsReversal() &&
                caseFile.Status != GmCaseStatus.Open &&
                caseFile.Status != GmCaseStatus.Investigating &&
                caseFile.Status != GmCaseStatus.Waiting)
            {
                error = "New sanctions require an open, investigating or waiting case.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(pending.Reason) || pending.Reason.Trim().Length < 4)
            {
                error = "A meaningful reason of at least four characters is required.";
                return false;
            }

            DateTime start = DateTime.Now;
            if (!TryResolveDuration(pending.ActionType, pending.DurationValue, Session.Account.Authority,
                    start, out DateTime? end, out error))
            {
                return false;
            }

            string ipAddress = null;
            if (pending.ActionType == GmSanctionActionType.IpBan)
            {
                if (Session.Account.Authority < AuthorityType.ADMIN)
                {
                    error = "IP bans require ADMIN authority.";
                    return false;
                }

                ClientSession targetSession = ServerManager.Instance.GetSessionByCharacterId(targetCharacter.CharacterId);
                ipAddress = targetSession?.CleanIpAddress;
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    error = "The target must be online for an IP ban so the current address can be captured.";
                    return false;
                }
            }

            request = new GmSanctionRequestDTO
            {
                OperationId = pending.OperationId,
                CaseId = pending.CaseId,
                ActionType = pending.ActionType,
                SubjectAccountId = targetAccount.AccountId,
                SubjectCharacterId = targetCharacter.CharacterId,
                SubjectName = targetCharacter.Name,
                OccurredAtUtc = DateTime.UtcNow,
                PenaltyStart = start,
                PenaltyEnd = end,
                DurationValue = pending.DurationValue,
                Reason = pending.Reason,
                IpAddress = ipAddress,
                ActorAccountId = Session.Account.AccountId,
                ActorCharacterId = Session.Character.CharacterId,
                ActorName = Session.Character.Name
            };
            return true;
        }

        private static bool TryResolveDuration(
            GmSanctionActionType actionType,
            int duration,
            AuthorityType authority,
            DateTime start,
            out DateTime? end,
            out string error)
        {
            end = null;
            error = null;

            switch (actionType)
            {
                case GmSanctionActionType.Warning:
                    if (duration != 0)
                    {
                        error = "Warnings use duration 0.";
                        return false;
                    }
                    end = start;
                    return true;

                case GmSanctionActionType.Mute:
                    int maximumMute = authority >= AuthorityType.ADMIN ? 525600 : 10080;
                    if (duration < 1 || duration > maximumMute)
                    {
                        error = $"Mute duration must be between 1 and {maximumMute} minutes.";
                        return false;
                    }
                    end = start.AddMinutes(duration);
                    return true;

                case GmSanctionActionType.Ban:
                case GmSanctionActionType.IpBan:
                    int maximumBan = authority >= AuthorityType.ADMIN ? 3650 : 30;
                    if (duration == 0)
                    {
                        if (authority < AuthorityType.ADMIN)
                        {
                            error = "Permanent bans require ADMIN authority.";
                            return false;
                        }
                        end = start.AddYears(15);
                        return true;
                    }
                    if (duration < 1 || duration > maximumBan)
                    {
                        error = $"Ban duration must be between 1 and {maximumBan} days, or 0 for permanent ADMIN bans.";
                        return false;
                    }
                    end = start.AddDays(duration);
                    return true;

                case GmSanctionActionType.Unmute:
                case GmSanctionActionType.Unban:
                    if (duration != 0)
                    {
                        error = "Reversal actions use duration 0.";
                        return false;
                    }
                    end = start;
                    return true;

                default:
                    error = "Unknown sanction action.";
                    return false;
            }
        }

        private void ApplyRuntimeEffects(GmSanctionRequestDTO request, CharacterDTO targetCharacter)
        {
            ClientSession targetSession = ServerManager.Instance.GetSessionByCharacterId(targetCharacter.CharacterId);

            switch (request.ActionType)
            {
                case GmSanctionActionType.Warning:
                    targetSession?.SendPacket(UserInterfaceHelper.GenerateInfo($"Warning: {request.Reason}"));
                    break;

                case GmSanctionActionType.Mute:
                    targetSession?.SendPacket(UserInterfaceHelper.GenerateInfo(
                        $"Muted for {request.DurationValue} minute(s): {request.Reason}"));
                    break;

                case GmSanctionActionType.Ban:
                case GmSanctionActionType.IpBan:
                    lock (ServerManager.Instance.BannedCharacters)
                    {
                        if (!ServerManager.Instance.BannedCharacters.Contains(targetCharacter.CharacterId))
                            ServerManager.Instance.BannedCharacters.Add(targetCharacter.CharacterId);
                    }
                    ServerManager.Instance.Kick(targetCharacter.Name);
                    break;

                case GmSanctionActionType.Unmute:
                    targetSession?.SendPacket(UserInterfaceHelper.GenerateInfo("Your mute has been removed."));
                    break;

                case GmSanctionActionType.Unban:
                    List<long> characterIds = DAOFactory.CharacterDAO.LoadAllByAccount(request.SubjectAccountId)
                        .Select(character => character.CharacterId)
                        .ToList();
                    lock (ServerManager.Instance.BannedCharacters)
                    {
                        ServerManager.Instance.BannedCharacters.RemoveAll(characterIds.Contains);
                    }
                    break;
            }
        }

        private static void RefreshPenaltyCache(long accountId)
        {
            List<PenaltyLogDTO> refreshed = DAOFactory.PenaltyLogDAO.LoadByAccount(accountId).ToList();
            if (ServerManager.Instance.PenaltyLogs == null)
                ServerManager.Instance.PenaltyLogs = new List<PenaltyLogDTO>();

            lock (ServerManager.Instance.PenaltyLogs)
            {
                ServerManager.Instance.PenaltyLogs.RemoveAll(log => log.AccountId == accountId);
                ServerManager.Instance.PenaltyLogs.AddRange(refreshed);
            }
        }

        private static bool TryParseAction(string value, out GmSanctionActionType action)
        {
            action = GmSanctionActionType.Warning;
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "warning":
                case "warn":
                    action = GmSanctionActionType.Warning;
                    return true;
                case "mute":
                    action = GmSanctionActionType.Mute;
                    return true;
                case "ban":
                    action = GmSanctionActionType.Ban;
                    return true;
                case "ipban":
                case "banip":
                    action = GmSanctionActionType.IpBan;
                    return true;
                case "unmute":
                    action = GmSanctionActionType.Unmute;
                    return true;
                case "unban":
                    action = GmSanctionActionType.Unban;
                    return true;
                default:
                    return false;
            }
        }

        private void SendHelp()
        {
            Send(SanctionPacket.ReturnHelp(), 10);
            Send("$Sanction status", 10);
            Send("$Sanction preview <CaseId> <warning|mute|ban|ipban|unmute|unban> <duration> <Character> <reason>", 10);
            Send("$Sanction confirm <code>", 10);
            Send("$Sanction cancel", 10);
            Send("$Sanction recent <CaseId> [take]", 10);
            Send("Duration: mute=minutes, ban/ipban=days, warning/unmute/unban=0.", 10);
        }

        private void Send(string message, int color) =>
            Session.SendPacket(Session.Character.GenerateSay(message, color));

        private static string FormatNullable<T>(T? value) where T : struct =>
            value.HasValue ? value.Value.ToString() : "-";

        private static string FormatDuration(GmSanctionActionType action, int duration)
        {
            if (action == GmSanctionActionType.Warning || action.IsReversal()) return "n/a";
            if (duration == 0) return "permanent";
            return action == GmSanctionActionType.Mute
                ? $"{duration} minute(s)"
                : $"{duration} day(s)";
        }

        private sealed class PendingSanction
        {
            public Guid OperationId { get; set; }
            public string ConfirmationCode { get; set; }
            public long CaseId { get; set; }
            public GmSanctionActionType ActionType { get; set; }
            public int DurationValue { get; set; }
            public string TargetCharacterName { get; set; }
            public string Reason { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
        }
    }
}
