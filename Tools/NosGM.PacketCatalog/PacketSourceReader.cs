// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NosGM.PacketCatalog;

internal sealed class PacketSourceReader
{
    private readonly DiagnosticSink _diagnostics;

    public PacketSourceReader(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public IEnumerable<PacketDescriptor> Read(SourceSyntaxFile syntaxFile)
    {
        foreach (var declaration in syntaxFile.Root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var derivesFromPacketDefinition = declaration.BaseList?.Types.Any(type =>
                SyntaxHelpers.SimpleTypeName(type.Type).TrimEnd('?') == "PacketDefinition") == true;
            if (!derivesFromPacketDefinition)
            {
                continue;
            }

            var headerAttributes = SyntaxHelpers.Attributes(declaration.AttributeLists, "PacketHeader").ToArray();
            var headers = new List<string>();
            var amount = 1;
            var isCharScreen = false;
            var passNonParseable = false;
            var authority = "User";
            var authorities = new List<string>();

            foreach (var attribute in headerAttributes)
            {
                foreach (var argument in attribute.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<AttributeArgumentSyntax>))
                {
                    var name = ArgumentName(argument);
                    if (name is null)
                    {
                        var value = SyntaxHelpers.StringValue(argument.Expression);
                        if (value is null)
                        {
                            _diagnostics.Add("PKT001", CatalogSeverity.Warning,
                                $"PacketHeader argument could not be evaluated: {argument.Expression}",
                                syntaxFile.Path, SyntaxHelpers.Line(argument), declaration.Identifier.ValueText);
                        }
                        else
                        {
                            headers.Add(value);
                        }
                        continue;
                    }

                    switch (name)
                    {
                        case "Amount":
                            amount = SyntaxHelpers.IntValue(argument.Expression) ?? amount;
                            break;
                        case "IsCharScreen":
                            isCharScreen = SyntaxHelpers.BoolValue(argument.Expression) ?? isCharScreen;
                            break;
                        case "PassNonParseablePacket":
                            passNonParseable = SyntaxHelpers.BoolValue(argument.Expression) ?? passNonParseable;
                            break;
                        case "Authority":
                            authority = SyntaxHelpers.ExpressionText(argument.Expression).Split('.').Last();
                            break;
                        case "Authorities":
                            authorities.AddRange(SyntaxHelpers.ArrayItems(argument.Expression)
                                .Select(SyntaxHelpers.ExpressionText)
                                .Select(value => value.Split('.').Last()));
                            break;
                    }
                }
            }

            var properties = declaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .SelectMany(property => ReadIndexedProperties(syntaxFile.Path, declaration.Identifier.ValueText, property))
                .OrderBy(property => property.Index)
                .ThenBy(property => property.Name, StringComparer.Ordinal)
                .ToArray();

            var packetNamespace = SyntaxHelpers.Namespace(declaration);
            var packetName = declaration.Identifier.ValueText;
            yield return new PacketDescriptor
            {
                Name = packetName,
                Namespace = packetNamespace,
                FullName = string.IsNullOrWhiteSpace(packetNamespace) ? packetName : $"{packetNamespace}.{packetName}",
                Headers = headers.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                Properties = properties,
                Handlers = Array.Empty<HandlerDescriptor>(),
                Source = new SourceReference(syntaxFile.Path, SyntaxHelpers.Line(declaration)),
                Summary = SyntaxHelpers.XmlSummary(declaration),
                IsSubPacket = headerAttributes.Length == 0,
                IsCharScreen = isCharScreen,
                PassNonParseablePacket = passNonParseable,
                Amount = amount,
                Authority = authority,
                Authorities = authorities.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray()
            };
        }
    }

    private IEnumerable<PacketPropertyDescriptor> ReadIndexedProperties(
        string path,
        string packetName,
        PropertyDeclarationSyntax property)
    {
        foreach (var attribute in SyntaxHelpers.Attributes(property.AttributeLists, "PacketIndex"))
        {
            var arguments = attribute.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<AttributeArgumentSyntax>);
            if (arguments.Count == 0 || SyntaxHelpers.IntValue(arguments[0].Expression) is not { } index)
            {
                _diagnostics.Add("PKT002", CatalogSeverity.Error, "PacketIndex requires a constant integer index.",
                    path, SyntaxHelpers.Line(attribute), packetName);
                continue;
            }

            var isReturnPacket = PositionalBool(arguments, 1) ?? NamedBool(arguments, "IsReturnPacket") ?? false;
            var serializeToEnd = PositionalBool(arguments, 2) ?? NamedBool(arguments, "SerializeToEnd") ?? false;
            var removeSeparator = PositionalBool(arguments, 3) ?? NamedBool(arguments, "RemoveSeparator") ?? false;

            yield return new PacketPropertyDescriptor(
                property.Identifier.ValueText,
                property.Type.ToString().Trim(),
                index,
                isReturnPacket,
                serializeToEnd,
                removeSeparator,
                SyntaxHelpers.XmlSummary(property),
                new SourceReference(path, SyntaxHelpers.Line(property)));
        }
    }

    private static string? ArgumentName(AttributeArgumentSyntax argument) =>
        argument.NameEquals?.Name.Identifier.ValueText ?? argument.NameColon?.Name.Identifier.ValueText;

    private static bool? NamedBool(SeparatedSyntaxList<AttributeArgumentSyntax> arguments, string name) =>
        arguments.Where(argument => string.Equals(ArgumentName(argument), name, StringComparison.OrdinalIgnoreCase))
            .Select(argument => SyntaxHelpers.BoolValue(argument.Expression))
            .FirstOrDefault(value => value.HasValue);

    private static bool? PositionalBool(SeparatedSyntaxList<AttributeArgumentSyntax> arguments, int position)
    {
        var positional = arguments.Where(argument => ArgumentName(argument) is null).ToArray();
        return positional.Length > position ? SyntaxHelpers.BoolValue(positional[position].Expression) : null;
    }
}
