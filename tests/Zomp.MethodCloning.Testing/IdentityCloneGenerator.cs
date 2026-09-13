using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading;

namespace Zomp.MethodCloning.Testing;

/// <summary>
/// Clones each method <c>[Cloning.Clone]</c> marks, directly or through its type, changing
/// nothing but its name, so that the output shows what the cloning layer does on its own.
/// </summary>
public sealed class IdentityCloneGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Metadata name of the attribute which marks the methods to clone.
    /// </summary>
    public const string AttributeName = "Cloning.CloneAttribute";

    /// <summary>
    /// Reported when two clones would declare the same member.
    /// </summary>
    private static readonly DiagnosticDescriptor Collision = new(
        id: "CLONE001",
        title: "Clones collide",
        messageFormat: "Two clones produce '{0}'",
        category: "Testing",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var clones = CloneTarget.ForAttribute(context.SyntaxProvider, AttributeName)
            .Select(static (target, ct) => Clone(target, ct)!)
            .WithTrackingName("Clone")
            .Where(static clone => clone is not null);

        ClonedMethodOutput.Register(context, clones, Collision);
    }

    private static ClonedMethod? Clone(CloneTarget target, CancellationToken ct)
    {
        var context = target.Context;
        var isTargetType = context.TargetSymbol is ITypeSymbol;

        var symbol = isTargetType
            ? context.SemanticModel.GetDeclaredSymbol(target.Syntax, ct)
            : context.TargetSymbol as IMethodSymbol;

        // A method with an attribute of its own is cloned for that attribute, not its type's.
        if (symbol is null
            || (isTargetType && symbol.GetAttributes().Any(static a => a.AttributeClass?.ToDisplayString() == AttributeName))
            || !MethodLocation.TryCreate(target.Syntax, symbol, out var location, out var root))
        {
            return null;
        }

        var disableNullable = !context.SemanticModel.GetNullableContext(target.Syntax.SpanStart).AnnotationsEnabled();
        var clone = new IdentityRewriter(context.SemanticModel, target.Syntax).Visit(root);

        return ClonedMethod.Create(location, target.Syntax, clone, disableNullable, []);
    }
}
