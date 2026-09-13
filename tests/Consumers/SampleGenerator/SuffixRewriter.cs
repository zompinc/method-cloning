namespace SampleGenerator;

/// <summary>
/// Changes nothing but the name of the method.
/// </summary>
/// <param name="semanticModel">The semantic model.</param>
/// <param name="targetMethod">The method declaration to copy.</param>
internal sealed class SuffixRewriter(SemanticModel semanticModel, MethodDeclarationSyntax targetMethod)
    : CloningRewriter(semanticModel, targetMethod)
{
    /// <inheritdoc/>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        => base.VisitMethodDeclaration(node) is MethodDeclarationSyntax copy
            ? copy.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.ValueText + "Copy").WithTriviaFrom(copy.Identifier))
            : null;
}
