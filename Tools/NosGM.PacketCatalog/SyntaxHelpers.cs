// SPDX-License-Identifier: GPL-3.0-only

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NosGM.PacketCatalog;

internal static partial class SyntaxHelpers
{
    public static string AttributeName(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString().Split('.').Last();
        return name.EndsWith("Attribute", StringComparison.Ordinal)
            ? name[..^"Attribute".Length]
            : name;
    }

    public static IEnumerable<AttributeSyntax> Attributes(SyntaxList<AttributeListSyntax> lists, string name) =>
        lists.SelectMany(list => list.Attributes)
            .Where(attribute => AttributeName(attribute).Equals(name, StringComparison.Ordinal));

    public static string Namespace(MemberDeclarationSyntax declaration)
    {
        var namespaces = declaration.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(node => node.Name.ToString())
            .Reverse();
        return string.Join(".", namespaces);
    }

    public static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    public static string? StringValue(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
        InterpolatedStringExpressionSyntax interpolated when !interpolated.Contents.OfType<InterpolationSyntax>().Any() =>
            string.Concat(interpolated.Contents.OfType<InterpolatedStringTextSyntax>().Select(text => text.TextToken.ValueText)),
        _ => null
    };

    public static int? IntValue(ExpressionSyntax expression)
    {
        if (expression is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.UnaryMinusExpression) &&
            prefix.Operand is LiteralExpressionSyntax negativeLiteral &&
            int.TryParse(negativeLiteral.Token.ValueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var negative))
        {
            return -negative;
        }

        return expression is LiteralExpressionSyntax literal &&
               int.TryParse(literal.Token.ValueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public static bool? BoolValue(ExpressionSyntax expression) => expression.Kind() switch
    {
        SyntaxKind.TrueLiteralExpression => true,
        SyntaxKind.FalseLiteralExpression => false,
        _ => null
    };

    public static string ExpressionText(ExpressionSyntax expression) => expression.ToString().Trim();

    public static string SimpleTypeName(TypeSyntax type)
    {
        var text = type.ToString().Trim();
        var nullable = text.EndsWith("?", StringComparison.Ordinal);
        if (nullable)
        {
            text = text[..^1];
        }

        var genericIndex = text.IndexOf('<');
        if (genericIndex >= 0)
        {
            text = text[..genericIndex];
        }

        var simple = text.Split('.').Last();
        return nullable ? simple + "?" : simple;
    }

    public static string? XmlSummary(SyntaxNode node)
    {
        var raw = string.Join("\n", node.GetLeadingTrivia()
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                             trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .Select(trivia => trivia.ToFullString()));
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var match = SummaryRegex().Match(raw);
        if (!match.Success)
        {
            return null;
        }

        var text = match.Groups[1].Value;
        text = DocumentationPrefixRegex().Replace(text, " ");
        text = XmlTagRegex().Replace(text, " ");
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text.Length == 0 ? null : text;
    }

    public static IReadOnlyList<ExpressionSyntax> ArrayItems(ExpressionSyntax expression)
    {
        if (expression is ArrayCreationExpressionSyntax arrayCreation && arrayCreation.Initializer is { } arrayInitializer)
        {
            return arrayInitializer.Expressions.ToArray();
        }

        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray && implicitArray.Initializer is { } implicitInitializer)
        {
            return implicitInitializer.Expressions.ToArray();
        }

        if (expression is InitializerExpressionSyntax initializer)
        {
            return initializer.Expressions.ToArray();
        }

        return Array.Empty<ExpressionSyntax>();
    }

    [GeneratedRegex(@"<summary>(.*?)</summary>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex SummaryRegex();

    [GeneratedRegex(@"(^|\n)\s*///?\s?", RegexOptions.Multiline)]
    private static partial Regex DocumentationPrefixRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex XmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
