// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NosGM.PacketCatalog;

internal sealed class HandlerSourceReader
{
    private readonly DiagnosticSink _diagnostics;

    public HandlerSourceReader(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public void Read(
        SourceSyntaxFile syntaxFile,
        ICollection<HandlerDescriptor> rawHandlers,
        ICollection<TypedHandlerCandidate> typedCandidates)
    {
        foreach (var method in syntaxFile.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var containingType = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            var containingName = containingType is null
                ? "<global>"
                : string.IsNullOrWhiteSpace(SyntaxHelpers.Namespace(containingType))
                    ? containingType.Identifier.ValueText
                    : $"{SyntaxHelpers.Namespace(containingType)}.{containingType.Identifier.ValueText}";
            var source = new SourceReference(syntaxFile.Path, SyntaxHelpers.Line(method));

            foreach (var attribute in SyntaxHelpers.Attributes(method.AttributeLists, "Packet"))
            {
                var args = attribute.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<AttributeArgumentSyntax>);
                var amount = 1;
                var start = 0;
                if (args.Count > 0 && SyntaxHelpers.IntValue(args[0].Expression) is { } explicitAmount)
                {
                    amount = explicitAmount;
                    start = 1;
                }

                var headers = args.Skip(start)
                    .Select(argument => SyntaxHelpers.StringValue(argument.Expression))
                    .Where(value => value is not null)
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (headers.Length == 0)
                {
                    _diagnostics.Add("HDL001", CatalogSeverity.Error,
                        "Packet handler attribute has no constant string header.",
                        syntaxFile.Path, SyntaxHelpers.Line(attribute));
                }

                rawHandlers.Add(new HandlerDescriptor(
                    "RawHeader",
                    containingName,
                    method.Identifier.ValueText,
                    headers,
                    amount,
                    null,
                    source));
            }

            var firstParameter = method.ParameterList.Parameters.FirstOrDefault();
            if (firstParameter?.Type is not null)
            {
                typedCandidates.Add(new TypedHandlerCandidate(
                    SyntaxHelpers.SimpleTypeName(firstParameter.Type).TrimEnd('?'),
                    containingName,
                    method.Identifier.ValueText,
                    source));
            }
        }
    }

    public void LinkTypedHandlers(
        IList<PacketDescriptor> packets,
        IReadOnlyCollection<TypedHandlerCandidate> typedCandidates)
    {
        var packetsByName = packets
            .GroupBy(packet => packet.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var candidate in typedCandidates)
        {
            if (!packetsByName.TryGetValue(candidate.PacketType, out var matches))
            {
                continue;
            }

            if (matches.Length > 1)
            {
                _diagnostics.Add("HDL002", CatalogSeverity.Warning,
                    $"Typed handler parameter {candidate.PacketType} is ambiguous because multiple packet classes share that simple name.",
                    candidate.Source.Path, candidate.Source.Line, candidate.PacketType);
                continue;
            }

            var packet = matches[0];
            var handler = new HandlerDescriptor(
                "TypedPacket",
                candidate.ContainingType,
                candidate.Method,
                packet.Headers,
                packet.Amount,
                packet.FullName,
                candidate.Source);
            packet.Handlers = packet.Handlers.Append(handler)
                .OrderBy(item => item.ContainingType, StringComparer.Ordinal)
                .ThenBy(item => item.Method, StringComparer.Ordinal)
                .ToArray();
        }

        foreach (var packet in packets)
        {
            if (packet.Handlers.Count > 0)
            {
                packet.Direction = "ClientToServer";
                packet.DirectionEvidence = "A typed IPacketHandler method consumes this PacketDefinition.";
            }
            else if (packet.Source.Path.Contains("/ClientPackets/", StringComparison.OrdinalIgnoreCase))
            {
                packet.Direction = "ClientToServerUnbound";
                packet.DirectionEvidence = "The source path identifies a client packet, but no typed handler was found.";
            }
            else if (packet.Source.Path.Contains("/ServerPackets/", StringComparison.OrdinalIgnoreCase) ||
                     packet.Source.Path.Contains("/CommandPackets/", StringComparison.OrdinalIgnoreCase))
            {
                packet.Direction = "ServerToClientOrCommand";
                packet.DirectionEvidence = "No typed handler was found; the source path suggests an outbound or command packet.";
            }
        }
    }
}

internal sealed record TypedHandlerCandidate(
    string PacketType,
    string ContainingType,
    string Method,
    SourceReference Source);
