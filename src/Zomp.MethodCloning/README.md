# Zomp.MethodCloning

**The plumbing for source generators that copy a method and change it.**

Copying a method into a generated file sounds easy until the copy has to compile there. This package handles that part, so your generator only contains the change you actually want to make.

It powers [Zomp.SyncMethodGenerator](https://github.com/zompinc/sync-method-generator), which turns async methods into sync ones, and [Zomp.MethodCloning.Variants](https://github.com/zompinc/method-cloning/tree/master/src/Zomp.MethodCloning.Variants), which replaces T4 templates.

## What you get

| Piece                | Does                                                                 |
| -------------------- | -------------------------------------------------------------------- |
| `CloneTarget`        | Finds the methods your attribute marks, on the method or its type    |
| `MethodLocation`     | Collects the namespaces, usings and partial types the copy needs     |
| `CloningRewriter`    | Fully qualifies every name, so the copy compiles anywhere            |
| `ClonedMethodOutput` | Names the files, catches colliding copies and adds the output        |

You derive from `CloningRewriter`, override what you change, and use its `MapSymbol`, `MapTypeName` and `MapMemberName` hooks to substitute types and names.

## A whole generator, sketched

```csharp
[Generator]
public sealed class CopyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var copies = CloneTarget.ForAttribute(context.SyntaxProvider, "My.CopyAttribute")
            .Select(static (target, ct) => Copy(target)!)
            .Where(static copy => copy is not null);

        ClonedMethodOutput.Register(context, copies, MyDiagnostics.Collision);
    }

    private static ClonedMethod? Copy(CloneTarget target)
    {
        var symbol = (IMethodSymbol)target.Context.TargetSymbol;
        if (!MethodLocation.TryCreate(target.Syntax, symbol, out var location, out var root))
        {
            return null;
        }

        var copy = new RenameRewriter(target.Context.SemanticModel, target.Syntax).Visit(root);
        return ClonedMethod.Create(location, target.Syntax, copy, disableNullable: false, ImmutableArray<ReportedDiagnostic>.Empty);
    }
}

internal sealed class RenameRewriter(SemanticModel model, MethodDeclarationSyntax method)
    : CloningRewriter(model, method)
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        => base.VisitMethodDeclaration(node) is MethodDeclarationSyntax copy
            ? copy.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.ValueText + "Copy"))
            : null;
}
```

## Install

```xml
<PackageReference Include="Zomp.MethodCloning" PrivateAssets="all" />
```

It ships as **source**: the files compile into your generator as internal types. There is no DLL to bundle into `analyzers/`, and two generators can never disagree about its version.

## Requirements

- C# 12 or later in the generator project.
- On `netstandard2.0`, [PolySharp](https://github.com/Sergio0694/PolySharp) for polyfills such as `IsExternalInit`.
- Roslyn 4.8 or later. Define `ROSLYN_4_12_OR_GREATER` or `ROSLYN_5_0_OR_GREATER` when you compile against those, to get the newer code paths.
