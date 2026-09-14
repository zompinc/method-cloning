namespace SampleGenerator;

/// <summary>
/// Copies each method <c>[Consumer.Clone]</c> marks, appending <c>Copy</c> to its name.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SuffixGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor Collision = new(
        id: "SAMPLE001",
        title: "Copies collide",
        messageFormat: "Two copies produce '{0}'",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var copies = CloneTarget.ForAttribute(context.SyntaxProvider, "Consumer.CloneAttribute")
            .Select(static (target, _) => Copy(target))
            .Where(static copy => copy is not null)
            .Select(static (copy, _) => copy!);

        ClonedMethodOutput.Register(context, copies, Collision);
    }

    private static ClonedMethod? Copy(CloneTarget target)
    {
        if (target.Context.TargetSymbol is not IMethodSymbol symbol
            || !MethodLocation.TryCreate(target.Syntax, symbol, out var location, out var root))
        {
            return null;
        }

        var copy = new SuffixRewriter(target.Context.SemanticModel, target.Syntax).Visit(root);

        return ClonedMethod.Create(location, target.Syntax, copy, nullableDisabled: false, ImmutableArray<ReportedDiagnostic>.Empty);
    }
}
