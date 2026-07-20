using Frostvein.Core;
using Frostvein.Core.Handling;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Packets.Packets.CommandPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Frostvein.Handler.PacketHandler.Command
{
    /// <summary>
    /// Read-only GM investigation command for the append-only item ledger and
    /// administration of restrictive staff capability profiles.
    /// </summary>
    public sealed class ItemTraceHandler : IPacketHandler
    {
        private const int DefaultTake = 10;
        private const int MaximumTake = 30;

        public ItemTraceHandler(ClientSession session)
        {
            Session = session;
            StaffPermissionBootstrap.EnsureConfigured();
        }

        public ClientSession Session { get; }

        public void ItemTrace(ItemTracePacket packet)
        {
            if (packet == null || Session?.Character == null || string.IsNullOrWhiteSpace(packet.Contents))
            {
                SendHelp();
                return;
            }

            var contents = packet.Contents.Trim();
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[ItemTrace] {contents}");

            var parts = contents.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var mode = parts[0].ToLowerInvariant();

            switch (mode)
            {
                case "item":
                case "instance":
                    if (!TryReadGuid(parts, 1, out var itemInstanceId))
                    {
                        SendHelp();
                        return;
                    }
                    ShowItemHistory(itemInstanceId, ReadTake(parts, 2));
                    return;

                case "serial":
                    if (!TryReadGuid(parts, 1, out var equipmentSerialId))
                    {
                        SendHelp();
                        return;
                    }
                    ShowSerial(equipmentSerialId, ReadTake(parts, 2));
                    return;

                case "operation":
                case "op":
                    if (!TryReadGuid(parts, 1, out var operationId))
                    {
                        SendHelp();
                        return;
                    }
                    ShowOperation(operationId, ReadTake(parts, 2));
                    return;

                case "duplicates":
                case "dupes":
                    ShowDuplicates(ReadTake(parts, 1, 15));
                    return;

                case "suspicious":
                case "alerts":
                    ShowSuspicious(ReadTake(parts, 1));
                    return;

                default:
                    SendHelp();
                    return;
            }
        }

        public void ManageStaffPermission(StaffPermissionPacket packet)
        {
            string[] parts = (packet?.Contents ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string mode = parts.FirstOrDefault()?.ToLowerInvariant() ?? "status";

            switch (mode)
            {
                case "status":
                    Send($"Staff permission schema: {(StaffPermissionService.Instance.IsAvailable() ? "AVAILABLE" : "MISSING")}", 11);
                    Send("Missing or disabled profiles use the legacy AuthorityType rules.", 10);
                    Send("Enabled profiles only restrict commands; they never elevate authority.", 10);
                    return;

                case "categories":
                case "category":
                    Send("===== Staff permission categories =====", 11);
                    foreach (StaffPermission category in StaffPermissionCatalog.Categories)
                    {
                        Send($"{category} = {(long)category}", 10);
                    }
                    return;

                case "show":
                case "list":
                    if (!TryResolveAccount(parts, 1, out AccountDTO showAccount))
                    {
                        SendStaffHelp();
                        return;
                    }
                    ShowStaffProfile(showAccount);
                    return;

                case "enable":
                case "disable":
                    if (!TryResolveAccount(parts, 1, out AccountDTO toggleAccount))
                    {
                        SendStaffHelp();
                        return;
                    }
                    if (mode == "enable" && toggleAccount.Authority < AuthorityType.GS)
                    {
                        Send("Only accounts with GS authority or higher can use a staff profile.", 11);
                        return;
                    }
                    SaveAndShow(toggleAccount, StaffPermissionService.Instance.SetEnabled(
                        toggleAccount.AccountId,
                        mode == "enable",
                        Session.Account?.AccountId,
                        Session.Character?.CharacterId,
                        JoinReason(parts, 2)));
                    return;

                case "grant":
                case "revoke":
                    if (!TryResolveAccount(parts, 1, out AccountDTO targetAccount) ||
                        parts.Length < 3 ||
                        !StaffPermissionCatalog.TryParse(parts[2], out StaffPermission permission) ||
                        permission == StaffPermission.None)
                    {
                        SendStaffHelp();
                        return;
                    }
                    if (targetAccount.Authority < AuthorityType.GS)
                    {
                        Send("Only accounts with GS authority or higher can receive staff permissions.", 11);
                        return;
                    }

                    StaffPermissionProfileDTO saved = mode == "grant"
                        ? StaffPermissionService.Instance.Grant(
                            targetAccount.AccountId,
                            permission,
                            Session.Account?.AccountId,
                            Session.Character?.CharacterId,
                            JoinReason(parts, 3))
                        : StaffPermissionService.Instance.Revoke(
                            targetAccount.AccountId,
                            permission,
                            Session.Account?.AccountId,
                            Session.Character?.CharacterId,
                            JoinReason(parts, 3));
                    SaveAndShow(targetAccount, saved);
                    return;

                default:
                    SendStaffHelp();
                    return;
            }
        }

        private void ShowItemHistory(Guid itemInstanceId, int take)
        {
            var traces = ItemTraceService.Instance.GetHistory(itemInstanceId, take).ToList();
            Send($"===== Item trace {itemInstanceId} =====", 12);
            WriteTraceRows(traces);
        }

        private void ShowSerial(Guid equipmentSerialId, int take)
        {
            Send($"===== Equipment serial {equipmentSerialId} =====", 12);
            var liveItems = ItemTraceService.Instance
                .GetCurrentItemsBySerial(equipmentSerialId, Math.Min(take, MaximumTake))
                .ToList();

            if (liveItems.Count == 0)
            {
                Send("No live ItemInstance currently uses this serial.", 11);
            }
            else
            {
                Send($"Live instances: {liveItems.Count} (reported group size: {liveItems.Max(item => item.InstanceCount)})", 10);
                foreach (var item in liveItems) WriteLiveItem(item);
            }

            Send("----- Ledger history -----", 10);
            WriteTraceRows(ItemTraceService.Instance.GetSerialHistory(equipmentSerialId, take).ToList());
        }

        private void ShowOperation(Guid operationId)
        {
            ShowOperation(operationId, DefaultTake);
        }

        private void ShowOperation(Guid operationId, int take)
        {
            var traces = ItemTraceService.Instance.GetOperation(operationId).Take(take).ToList();
            Send($"===== Item operation {operationId} =====", 12);
            WriteTraceRows(traces);
        }

        private void ShowDuplicates(int takeGroups)
        {
            var rows = ItemTraceService.Instance.GetDuplicateEquipmentSerialItems(takeGroups).ToList();
            var groups = rows.GroupBy(row => row.EquipmentSerialId)
                .OrderByDescending(group => group.Max(row => row.InstanceCount))
                .ThenBy(group => group.Key)
                .ToList();

            Send("===== Duplicate equipment serial detector =====", 12);
            if (groups.Count == 0)
            {
                Send("No duplicate non-empty EquipmentSerialId values were found.", 10);
                return;
            }

            Send($"Collision groups: {groups.Count}. Use '$ItemTrace serial <guid>' for details.", 11);
            foreach (var group in groups)
            {
                var entries = group.ToList();
                var owners = string.Join(",", entries.Select(item => item.CharacterId).Distinct().Take(4));
                var vnums = string.Join(",", entries.Select(item => item.ItemVNum).Distinct().Take(4));
                Send($"serial={group.Key} copies={entries.Max(item => item.InstanceCount)} owners={owners} vnums={vnums}", 13);
            }
        }

        private void ShowSuspicious(int take)
        {
            Send("===== Suspicious item ledger events =====", 12);
            WriteTraceRows(ItemTraceService.Instance.GetSuspicious(take).ToList());
        }

        private void ShowStaffProfile(AccountDTO account)
        {
            StaffPermissionProfileDTO profile = StaffPermissionService.Instance.GetProfile(account.AccountId, true);
            Send($"===== Staff profile {account.Name} ({account.AccountId}) =====", 12);
            Send($"Authority ceiling: {account.Authority}", 10);
            if (profile == null)
            {
                Send("Mode: LEGACY (no profile row)", 10);
                return;
            }

            Send($"Mode: {(profile.IsEnabled ? "GRANULAR" : "LEGACY/DISABLED")}", profile.IsEnabled ? 11 : 10);
            Send($"Permissions: {StaffPermissionCatalog.Format(profile.Permissions)} | mask={profile.PermissionMask}", 10);
            Send($"Updated: {profile.UpdatedAtUtc:yyyy-MM-dd HH:mm:ss}Z by account={FormatNullable(profile.UpdatedByAccountId)} character={FormatNullable(profile.UpdatedByCharacterId)}", 10);
            if (!string.IsNullOrWhiteSpace(profile.Reason)) Send($"Reason: {profile.Reason}", 10);
        }

        private void SaveAndShow(AccountDTO account, StaffPermissionProfileDTO saved)
        {
            if (saved == null)
            {
                Send("Staff permission update failed. Apply the migration and inspect the server log.", 11);
                return;
            }
            Send("Staff permission profile updated.", 10);
            ShowStaffProfile(account);
        }

        private bool TryResolveAccount(string[] parts, int index, out AccountDTO account)
        {
            account = null;
            if (parts.Length <= index) return false;
            string value = parts[index];
            account = long.TryParse(value, out long accountId)
                ? DAOFactory.AccountDAO.LoadById(accountId)
                : DAOFactory.AccountDAO.LoadByName(value);
            if (account == null) Send($"Account '{value}' was not found.", 11);
            return account != null;
        }

        private void WriteTraceRows(IReadOnlyCollection<ItemTraceDTO> traces)
        {
            if (traces == null || traces.Count == 0)
            {
                Send("No matching item trace events were found.", 11);
                return;
            }

            foreach (var trace in traces)
            {
                var marker = trace.IsSuspicious ? " ALERT" : string.Empty;
                var beforeInventory = FormatInventory(trace.InventoryTypeBefore, trace.SlotBefore);
                var afterInventory = FormatInventory(trace.InventoryTypeAfter, trace.SlotAfter);
                Send(
                    $"{trace.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z {trace.Action}/{trace.Source}{marker} " +
                    $"item={ShortGuid(trace.ItemInstanceId)} v={trace.ItemVNum} " +
                    $"amount={FormatNullable(trace.AmountBefore)}->{FormatNullable(trace.AmountAfter)} " +
                    $"owner={FormatNullable(trace.OwnerCharacterIdBefore)}->{FormatNullable(trace.OwnerCharacterIdAfter)} " +
                    $"inv={beforeInventory}->{afterInventory} op={ShortGuid(trace.OperationId)}#{trace.Sequence}",
                    trace.IsSuspicious ? 11 : 10);
            }
        }

        private void WriteLiveItem(DuplicateEquipmentSerialItemDTO item)
        {
            var inventory = Enum.IsDefined(typeof(InventoryType), item.InventoryTypeValue)
                ? ((InventoryType)item.InventoryTypeValue).ToString()
                : item.InventoryTypeValue.ToString();

            Send($"item={item.ItemInstanceId} v={item.ItemVNum} x{item.Amount} owner={item.CharacterId} " +
                 $"inv={inventory}:{item.Slot} rare={item.Rare} upgrade={item.Upgrade}", 13);
        }

        private void SendHelp()
        {
            Send(ItemTracePacket.ReturnHelp(), 10);
            Send("$ItemTrace item <ItemInstanceId> [take]", 10);
            Send("$ItemTrace serial <EquipmentSerialId> [take]", 10);
            Send("$ItemTrace operation <OperationId> [take]", 10);
            Send("$ItemTrace duplicates [groups]", 10);
            Send("$ItemTrace suspicious [take]", 10);
        }

        private void SendStaffHelp()
        {
            Send(StaffPermissionPacket.ReturnHelp(), 10);
            Send("$StaffPerm status", 10);
            Send("$StaffPerm categories", 10);
            Send("$StaffPerm show <AccountId|AccountName>", 10);
            Send("$StaffPerm grant <account> <category|all> [reason]", 10);
            Send("$StaffPerm revoke <account> <category|all> [reason]", 10);
            Send("$StaffPerm enable <account> [reason]", 10);
            Send("$StaffPerm disable <account> [reason]", 10);
        }

        private void Send(string message, int color) =>
            Session.SendPacket(Session.Character.GenerateSay(message, color));

        private static bool TryReadGuid(string[] parts, int index, out Guid value)
        {
            value = Guid.Empty;
            return parts.Length > index && Guid.TryParse(parts[index], out value) && value != Guid.Empty;
        }

        private static int ReadTake(string[] parts, int index, int defaultValue = DefaultTake)
        {
            if (parts.Length <= index || !int.TryParse(parts[index], out var take)) return defaultValue;
            return Math.Max(1, Math.Min(MaximumTake, take));
        }

        private static string JoinReason(string[] parts, int index) =>
            parts.Length <= index ? null : string.Join(" ", parts.Skip(index));

        private static string ShortGuid(Guid value) => value.ToString("N").Substring(0, 8);

        private static string FormatNullable<T>(T? value) where T : struct =>
            value.HasValue ? value.Value.ToString() : "-";

        private static string FormatInventory(InventoryType? type, short? slot) =>
            type.HasValue ? $"{type.Value}:{(slot.HasValue ? slot.Value.ToString() : "-")}" : "-";
    }

    internal static class StaffPermissionBootstrap
    {
        private static int _configured;

        public static void EnsureConfigured()
        {
            if (Interlocked.Exchange(ref _configured, 1) == 0)
            {
                StaffCommandPolicyBridge.Configure(Evaluate);
            }
        }

        private static StaffCommandPolicyDecision Evaluate(StaffCommandPolicyRequest request)
        {
            ClientSession session = ResolveSession(request?.ParentHandler);
            if (session?.Account == null) return StaffCommandPolicyDecision.Legacy;

            StaffAuthorizationResult result = StaffPermissionService.Instance.Authorize(
                session.Account.AccountId,
                session.Account.Authority,
                request.Header,
                request.RequiredAuthority);

            if (!result.Allowed && session.HasSelectedCharacter)
            {
                session.SendPacket(session.Character.GenerateSay(
                    $"Permission denied: {result.RequiredPermission}. Granted: {StaffPermissionCatalog.Format(result.GrantedPermissions)}", 11));
            }

            return new StaffCommandPolicyDecision
            {
                Allowed = result.Allowed,
                RequiredPermission = result.RequiredPermission,
                Reason = result.Reason
            };
        }

        private static ClientSession ResolveSession(object parentHandler)
        {
            if (parentHandler == null) return null;
            PropertyInfo property = parentHandler.GetType().GetProperty("Session");
            return property?.GetValue(parentHandler) as ClientSession;
        }
    }
}
