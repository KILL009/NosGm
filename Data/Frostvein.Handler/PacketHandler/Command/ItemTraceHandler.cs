using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Packets.Packets.CommandPackets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    /// <summary>
    /// Read-only GM investigation command for the append-only item ledger and
    /// live equipment-serial collisions. It deliberately performs no quarantine
    /// or deletion so a human can review the evidence first.
    /// </summary>
    public sealed class ItemTraceHandler : IPacketHandler
    {
        private const int DefaultTake = 10;
        private const int MaximumTake = 30;

        public ItemTraceHandler(ClientSession session) => Session = session;

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
                foreach (var item in liveItems)
                {
                    WriteLiveItem(item);
                }
            }

            Send("----- Ledger history -----", 10);
            WriteTraceRows(ItemTraceService.Instance.GetSerialHistory(equipmentSerialId, take).ToList());
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

            Send(
                $"item={item.ItemInstanceId} v={item.ItemVNum} x{item.Amount} owner={item.CharacterId} " +
                $"inv={inventory}:{item.Slot} rare={item.Rare} upgrade={item.Upgrade}",
                13);
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

        private void Send(string message, int color) =>
            Session.SendPacket(Session.Character.GenerateSay(message, color));

        private static bool TryReadGuid(string[] parts, int index, out Guid value)
        {
            value = Guid.Empty;
            return parts.Length > index && Guid.TryParse(parts[index], out value) && value != Guid.Empty;
        }

        private static int ReadTake(string[] parts, int index, int defaultValue = DefaultTake)
        {
            if (parts.Length <= index || !int.TryParse(parts[index], out var take))
            {
                return defaultValue;
            }

            return Math.Max(1, Math.Min(MaximumTake, take));
        }

        private static string ShortGuid(Guid value) => value.ToString("N").Substring(0, 8);

        private static string FormatNullable<T>(T? value) where T : struct =>
            value.HasValue ? value.Value.ToString() : "-";

        private static string FormatInventory(InventoryType? type, short? slot) =>
            type.HasValue ? $"{type.Value}:{(slot.HasValue ? slot.Value.ToString() : "-")}" : "-";
    }
}
