using Basic.Reference.Assemblies;
using Zomp.MethodCloning.Variants;

namespace Zomp.MethodCloning.Tests;

/// <summary>
/// Runs the variant generator over a source and records what it produced.
/// </summary>
internal static class TestHelper
{
    private const string AttributeFile = "CloneVariantsAttribute.g.cs";

    /// <summary>
    /// Generates the variants of a source, fails if they do not compile, and verifies the
    /// generated files and the diagnostics against the snapshot.
    /// </summary>
    /// <param name="source">The whole source file.</param>
    /// <returns>The verification.</returns>
    public static Task Verify(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: Net100.References.All,
            options: new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var run = CSharpGeneratorDriver.Create(new VariantGenerator())
            .WithUpdatedParseOptions(parseOptions)
            .RunGenerators(compilation)
            .GetRunResult()
            .Results
            .Single();

        if (run.Exception is { } exception)
        {
            throw new InvalidOperationException("The generator threw", exception);
        }

        var errors = compilation
            .AddSyntaxTrees(run.GeneratedSources.Select(s => s.SyntaxTree))
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                "The generated code does not compile:\n" + string.Join("\n", errors.Select(d => d.ToString())));
        }

        var text = new StringBuilder();

        foreach (var diagnostic in run.Diagnostics)
        {
            _ = text.AppendLine(CultureInfo.InvariantCulture, $"// {diagnostic.Id} {diagnostic.Severity}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }

        foreach (var generated in run.GeneratedSources.Where(s => s.HintName != AttributeFile).OrderBy(s => s.HintName, StringComparer.Ordinal))
        {
            _ = text.AppendLine(CultureInfo.InvariantCulture, $"//HintName: {generated.HintName}");
            _ = text.AppendLine(generated.SourceText.ToString());
        }

        return Verifier.Verify(text.ToString()).UseDirectory("Snapshots");
    }
}
