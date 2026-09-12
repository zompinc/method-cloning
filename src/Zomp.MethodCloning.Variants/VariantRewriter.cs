using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Zomp.MethodCloning.Variants;

/// <summary>
/// Writes a method as one of its variants, swapping the word which names the variant wherever it
/// appears: in the method's name, in the types and static members it refers to, and in its
/// documentation. Writing <c>SaveAsBmp</c> as its <c>Gif</c> variant produces <c>SaveAsGif</c>,
/// which takes a <c>GifEncoder</c> where the original took a <c>BmpEncoder</c>.
/// </summary>
/// <param name="semanticModel">The semantic model.</param>
/// <param name="targetMethod">The method declaration to write a variant of.</param>
/// <param name="original">The word naming the variant the method was written for.</param>
/// <param name="variant">The word naming the variant to write.</param>
internal sealed class VariantRewriter(SemanticModel semanticModel, MethodDeclarationSyntax targetMethod, string original, string variant)
    : CloningRewriter(semanticModel, targetMethod)
{
    private readonly ImmutableArray<ReportedDiagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<ReportedDiagnostic>();

    // Types kept for want of a counterpart, each reported once however often the method names it.
    private readonly List<ISymbol> kept = [];

    private Location? methodLocation;

    /// <summary>
    /// Gets the diagnostics reported while writing the variant.
    /// </summary>
    public ImmutableArray<ReportedDiagnostic> Diagnostics => diagnostics.ToImmutable();

    /// <inheritdoc/>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        methodLocation = node.Identifier.GetLocation();

        if (base.VisitMethodDeclaration(node) is not MethodDeclarationSyntax clone)
        {
            return null;
        }

        // The documentation names the variant in prose, which no symbol reaches. It is taken from
        // the original, since dropping the attribute which marked the method can take it along.
        var documentation = SyntaxFactory.ParseLeadingTrivia(Rename(node.GetLeadingTrivia().ToFullString(), original, variant));

        var name = SyntaxFactory.Identifier(Rename(node.Identifier.ValueText, original, variant)).WithTriviaFrom(clone.Identifier);

        return clone
            .WithIdentifier(name)
            .WithLeadingTrivia(RemovePreprocessorDirectives(documentation));
    }

    /// <summary>
    /// Drops <c>[CloneVariants]</c>, which marks the method written by hand, not its variants.
    /// </summary>
    /// <param name="node">The attribute list.</param>
    /// <returns>The list without the attribute, or null when nothing else is left in it.</returns>
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        var visited = (AttributeListSyntax)base.VisitAttributeList(node)!;

        var attributes = node.Attributes
            .Zip(visited.Attributes, static (original, rewritten) => (original, rewritten))
            .Where(pair => !IsCloneVariants(pair.original))
            .Select(static pair => pair.rewritten);

        var remaining = SyntaxFactory.SeparatedList(attributes);

        return remaining.Count == 0 ? null : visited.WithAttributes(remaining);
    }

    /// <summary>
    /// Maps a type written with its namespace in front of a member, as in
    /// <c>Formats.Bmp.BmpFormat.Instance</c>. In an expression that is a chain of member accesses
    /// rather than a type name, so the base, which qualifies type names, leaves it as it is.
    /// </summary>
    /// <param name="node">The member access.</param>
    /// <returns>The member access, on the counterpart of the type when it has one.</returns>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var visited = base.VisitMemberAccessExpression(node);

        return visited is MemberAccessExpressionSyntax access
            && node.Expression is MemberAccessExpressionSyntax
            && GetSymbol(node.Expression) is INamedTypeSymbol type
            && MapSymbol(type) is { } counterpart
            ? access.WithExpression(counterpart.WithTriviaFrom(access.Expression))
            : visited;
    }

    /// <summary>
    /// Swaps the word naming one variant for the word naming another.
    /// </summary>
    /// <param name="text">Text to swap the word in.</param>
    /// <param name="from">The word naming the variant the text was written for.</param>
    /// <param name="to">The word naming the variant to write.</param>
    /// <returns>The text for the other variant.</returns>
    internal static string Rename(string text, string from, string to) => text.Replace(from, to);

    /// <summary>
    /// Substitutes the counterpart of a type written for the original variant, found by swapping
    /// the word in its namespace and name: <c>Formats.Bmp.BmpEncoder</c> becomes
    /// <c>Formats.Gif.GifEncoder</c>. A type without a counterpart is kept, and reported.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The counterpart, or null to keep the symbol.</returns>
    protected override SimpleNameSyntax? MapSymbol(ISymbol symbol)
    {
        if (symbol is not INamedTypeSymbol { ContainingNamespace: { } containingNamespace } type
            || type.Name.IndexOf(original, StringComparison.Ordinal) < 0)
        {
            return null;
        }

        var metadataName = containingNamespace.IsGlobalNamespace
            ? type.MetadataName
            : $"{containingNamespace.ToDisplayString()}.{type.MetadataName}";

        var counterpartName = Rename(metadataName, original, variant);

        if (SemanticModel.Compilation.GetTypeByMetadataName(counterpartName) is { } counterpart
            && !SymbolEqualityComparer.Default.Equals(counterpart, type))
        {
            return ProcessSymbol(counterpart);
        }

        if (methodLocation is not null && !kept.Contains(type, SymbolEqualityComparer.Default))
        {
            kept.Add(type);
            diagnostics.Add(ReportedDiagnostic.Create(
                VariantDiagnostics.NoCounterpart,
                methodLocation,
                $"'{type.ToDisplayString()}' has no counterpart named '{counterpartName}'"));
        }

        return null;
    }

    /// <summary>
    /// Refers to the counterpart of a static member written for the original variant, which may
    /// be another variant: <c>SaveAsBmp(source, path, default)</c> becomes <c>SaveAsGif(...)</c>.
    /// </summary>
    /// <param name="member">The static field or method.</param>
    /// <returns>The name of its counterpart.</returns>
    protected override string MapMemberName(ISymbol member) => Rename(member.Name, original, variant);

    private bool IsCloneVariants(AttributeSyntax attribute)
        => GetSymbol(attribute) is IMethodSymbol { ContainingType: { } type }
        && type.ToDisplayString() == VariantGenerator.AttributeName;
}
