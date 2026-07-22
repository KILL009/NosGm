// SPDX-License-Identifier: GPL-3.0-only

namespace NosGM.PacketCatalog;

internal sealed class CatalogValidator
{
    private readonly DiagnosticSink _diagnostics;

    public CatalogValidator(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public void Validate(
        IReadOnlyCollection<PacketDescriptor> packets,
        IReadOnlyCollection<HandlerDescriptor> rawHandlers)
    {
        ValidatePackets(packets);
        ValidateHeaders(packets, rawHandlers);
    }

    private void ValidatePackets(IEnumerable<PacketDescriptor> packets)
    {
        foreach (var packet in packets)
        {
            if (packet.Amount < 1)
            {
                _diagnostics.Add("PKT003", CatalogSeverity.Error, "Packet Amount must be at least 1.",
                    packet.Source.Path, packet.Source.Line, packet.Name);
            }

            foreach (var header in packet.Headers.Where(string.IsNullOrWhiteSpace))
            {
                _diagnostics.Add("PKT004", CatalogSeverity.Error, "Packet header is empty.",
                    packet.Source.Path, packet.Source.Line, packet.Name, header);
            }

            foreach (var duplicate in packet.Properties.GroupBy(property => property.Index).Where(group => group.Count() > 1))
            {
                _diagnostics.Add("PKT005", CatalogSeverity.Error,
                    $"PacketIndex {duplicate.Key} is assigned to multiple properties: {string.Join(", ", duplicate.Select(property => property.Name))}.",
                    packet.Source.Path, packet.Source.Line, packet.Name);
            }

            foreach (var property in packet.Properties.Where(property => property.Index < 0))
            {
                _diagnostics.Add("PKT006", CatalogSeverity.Error, $"Property {property.Name} uses a negative PacketIndex.",
                    property.Source.Path, property.Source.Line, packet.Name);
            }

            var finalProperties = packet.Properties.Where(property => property.SerializeToEnd).ToArray();
            if (finalProperties.Length > 1)
            {
                _diagnostics.Add("PKT007", CatalogSeverity.Error,
                    $"Multiple properties use SerializeToEnd: {string.Join(", ", finalProperties.Select(property => property.Name))}.",
                    packet.Source.Path, packet.Source.Line, packet.Name);
            }

            if (finalProperties.Length == 1 && packet.Properties.Count > 0 &&
                finalProperties[0].Index != packet.Properties.Max(property => property.Index))
            {
                _diagnostics.Add("PKT008", CatalogSeverity.Error,
                    $"SerializeToEnd property {finalProperties[0].Name} is not the highest PacketIndex; later properties are unreachable during serialization.",
                    finalProperties[0].Source.Path, finalProperties[0].Source.Line, packet.Name);
            }

            foreach (var property in packet.Properties.Where(property => property.RemoveSeparator && !LooksLikeCollection(property.Type)))
            {
                _diagnostics.Add("PKT009", CatalogSeverity.Warning,
                    $"Property {property.Name} requests RemoveSeparator but its type does not look like a collection.",
                    property.Source.Path, property.Source.Line, packet.Name);
            }

            var indexes = packet.Properties.Select(property => property.Index).Distinct().OrderBy(index => index).ToArray();
            if (indexes.Length > 0 && indexes[0] > 0)
            {
                _diagnostics.Add("PKT010", CatalogSeverity.Info,
                    $"Packet indexes start at {indexes[0]}; PacketFactory will synthesize leading zero fields.",
                    packet.Source.Path, packet.Source.Line, packet.Name);
            }

            if (indexes.Length > 1)
            {
                var missing = Enumerable.Range(indexes[0], indexes[^1] - indexes[0] + 1).Except(indexes).ToArray();
                if (missing.Length > 0)
                {
                    _diagnostics.Add("PKT011", CatalogSeverity.Info,
                        $"Packet index gaps will be serialized as zero fields: {string.Join(", ", missing)}.",
                        packet.Source.Path, packet.Source.Line, packet.Name);
                }
            }

            if (!packet.IsSubPacket && packet.Headers.Count == 0)
            {
                _diagnostics.Add("PKT012", CatalogSeverity.Error,
                    "PacketHeader attribute did not yield a usable header.",
                    packet.Source.Path, packet.Source.Line, packet.Name);
            }

            if (packet.Handlers.Count > 0 && packet.Headers.Count == 0)
            {
                _diagnostics.Add("HDL003", CatalogSeverity.Error,
                    "Typed handler consumes a packet without a usable PacketHeader.",
                    packet.Source.Path, packet.Source.Line, packet.Name);
            }
        }
    }

    private void ValidateHeaders(
        IReadOnlyCollection<PacketDescriptor> packets,
        IReadOnlyCollection<HandlerDescriptor> rawHandlers)
    {
        var typedHeaderUses = packets
            .SelectMany(packet => packet.Handlers.SelectMany(handler =>
                handler.Headers.Select(header => new HeaderUse(header, handler, packet))))
            .ToArray();
        var rawHeaderUses = rawHandlers
            .SelectMany(handler => handler.Headers.Select(header => new HeaderUse(header, handler, null)))
            .ToArray();

        foreach (var group in typedHeaderUses.Concat(rawHeaderUses)
                     .Where(use => !string.IsNullOrWhiteSpace(use.Header))
                     .GroupBy(use => use.Header, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            var locations = string.Join(", ", group
                .Select(use => $"{use.Handler.ContainingType}.{use.Handler.Method}")
                .Distinct(StringComparer.Ordinal));
            var first = group.First();
            _diagnostics.Add("HDL004", CatalogSeverity.Error,
                $"Header '{group.Key}' is registered by multiple handlers: {locations}. Runtime dictionary registration can fail.",
                first.Handler.Source.Path, first.Handler.Source.Line, first.Packet?.Name, group.Key);
        }

        foreach (var group in packets
                     .SelectMany(packet => packet.Headers.Select(header => new HeaderPacket(header, packet)))
                     .Where(item => !string.IsNullOrWhiteSpace(item.Header))
                     .GroupBy(item => item.Header, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Select(item => item.Packet.FullName)
                         .Distinct(StringComparer.Ordinal).Count() > 1))
        {
            var first = group.First();
            _diagnostics.Add("PKT013", CatalogSeverity.Warning,
                $"Header '{group.Key}' is declared by multiple packet classes: {string.Join(", ", group.Select(item => item.Packet.FullName).Distinct(StringComparer.Ordinal))}.",
                first.Packet.Source.Path, first.Packet.Source.Line, first.Packet.Name, group.Key);
        }

        foreach (var handler in rawHandlers.Where(handler => handler.Amount < 1))
        {
            _diagnostics.Add("HDL005", CatalogSeverity.Error,
                "Raw packet handler Amount must be at least 1.",
                handler.Source.Path, handler.Source.Line);
        }
    }

    private static bool LooksLikeCollection(string type) =>
        type.Contains("List<", StringComparison.Ordinal) ||
        type.Contains("IList<", StringComparison.Ordinal) ||
        type.Contains("IEnumerable<", StringComparison.Ordinal) ||
        type.EndsWith("[]", StringComparison.Ordinal);

    private sealed record HeaderUse(string Header, HandlerDescriptor Handler, PacketDescriptor? Packet);
    private sealed record HeaderPacket(string Header, PacketDescriptor Packet);
}
