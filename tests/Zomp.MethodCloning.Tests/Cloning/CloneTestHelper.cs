using Basic.Reference.Assemblies;

namespace Zomp.MethodCloning.Tests.Cloning;

/// <summary>
/// Clones the methods of a source through <see cref="IdentityCloneGenerator"/>, fails if the
/// clones do not compile beside the originals, and verifies them against the snapshot.
/// </summary>
internal static class CloneTestHelper
{
    private const string AttributeSource = """
namespace Cloning
{
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Interface)]
    internal sealed class CloneAttribute : System.Attribute
    {
    }
}
""";

    private const string GlobalUsingsSource = """
global using global::System;
global using global::System.Buffers;
global using global::System.Collections.Generic;
global using global::System.Data;
global using global::System.Data.Common;
global using global::System.Diagnostics;
global using global::System.Drawing;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Numerics;
global using global::System.Reflection;
global using global::System.Runtime.CompilerServices;
global using global::System.Text;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::System.Xml;
global using global::Cloning;
""";

    // References the documentation of a clone makes, which must resolve where the clone lands.
    private static readonly string[] CrefDiagnostics = ["CS1574", "CS1580", "CS1581", "CS1584"];

    /// <summary>
    /// Clones the marked methods of a source and verifies the clones.
    /// </summary>
    /// <param name="source">The source, wrapped as <paramref name="sourceType"/> says.</param>
    /// <param name="sourceType">How the source is wrapped.</param>
    /// <param name="languageVersion">The language version to compile with.</param>
    /// <param name="documentationMode">How documentation comments are parsed.</param>
    /// <param name="parameters">Parameters of the test, which tell its snapshots apart.</param>
    /// <returns>The verification.</returns>
    public static Task Verify(
        this string source,
        SourceType sourceType = SourceType.ClassBody,
        LanguageVersion languageVersion = LanguageVersion.Preview,
        DocumentationMode documentationMode = DocumentationMode.Diagnose,
        params object?[] parameters)
    {
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(languageVersion)
            .WithDocumentationMode(documentationMode)
            .WithPreprocessorSymbols("NET8_0_OR_GREATER");

        if (sourceType == SourceType.MethodBody)
        {
            source = $$"""
[Clone]
async Task MethodAsync(CancellationToken ct)
{
{{Indent(source)}}
}
""";
        }

        if (sourceType != SourceType.Full)
        {
            source = $$"""
namespace Test;
partial class Class
{
{{Indent(source)}}
}
""";
        }

        List<SyntaxTree> trees = [CSharpSyntaxTree.ParseText(source, parseOptions), CSharpSyntaxTree.ParseText(AttributeSource, parseOptions)];

        if (languageVersion >= LanguageVersion.CSharp10)
        {
            trees.Add(CSharpSyntaxTree.ParseText(GlobalUsingsSource, parseOptions));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: trees,
            references: Net100.References.All,
            options: new(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: languageVersion >= LanguageVersion.CSharp8 ? NullableContextOptions.Enable : NullableContextOptions.Disable));

        var run = CSharpGeneratorDriver.Create(new IdentityCloneGenerator())
            .WithUpdatedParseOptions(parseOptions)
            .RunGenerators(compilation)
            .GetRunResult()
            .Results
            .Single();

        if (run.Exception is { } exception)
        {
            throw new InvalidOperationException("The generator threw", exception);
        }

        if (run.GeneratedSources.IsEmpty && run.Diagnostics.IsEmpty)
        {
            throw new InvalidOperationException("Nothing was cloned");
        }

        var generated = run.GeneratedSources.Select(s => s.SyntaxTree).ToHashSet();

        var problems = compilation
            .AddSyntaxTrees(generated)
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error
                || (d.Location.SourceTree is { } tree && generated.Contains(tree) && CrefDiagnostics.Contains(d.Id)))
            .ToArray();

        if (problems.Length > 0)
        {
            throw new InvalidOperationException(
                "The clones do not compile:\n" + string.Join("\n", problems.Select(d => d.ToString())));
        }

        var text = new StringBuilder();

        foreach (var diagnostic in run.Diagnostics)
        {
            _ = text.AppendLine(CultureInfo.InvariantCulture, $"// {diagnostic.Id} {diagnostic.Severity}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }

        foreach (var clone in run.GeneratedSources.OrderBy(s => s.HintName, StringComparer.Ordinal))
        {
            _ = text.AppendLine(CultureInfo.InvariantCulture, $"//HintName: {clone.HintName}");
            _ = text.AppendLine(clone.SourceText.ToString());
        }

        var verify = Verifier.Verify(text.ToString()).UseDirectory("Snapshots");

        return parameters.Length > 0 ? verify.UseParameters(parameters) : verify;
    }

    private static string Indent(string source)
        => string.Join("\n", source.Split('\n').Select(line => line.TrimEnd('\r')).Select(line => line.Length == 0 || line[0] == '#' ? line : "    " + line));
}
