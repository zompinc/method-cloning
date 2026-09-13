using Basic.Reference.Assemblies;

namespace Zomp.MethodCloning.Tests.Cloning;

// A clone is only rewritten when the method it comes from changes.
public class IncrementalTests
{
    private const string Attribute = """
namespace Cloning
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal sealed class CloneAttribute : System.Attribute
    {
    }
}
""";

    [Test]
    public async Task UnrelatedEditIsCached()
    {
        var (output, clone, source) = RunTwice(
            """
            using System.Threading.Tasks;

            class Test
            {
                public Task OtherAsync() => Task.CompletedTask;

                [Cloning.Clone]
                public async Task MethodAsync()
                {
                    await OtherAsync();
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class Test
            {
                public Task OtherAsync() => Task.Delay(1);

                [Cloning.Clone]
                public async Task MethodAsync()
                {
                    await OtherAsync();
                }
            }
            """);

        _ = await Assert.That(output).IsEqualTo(IncrementalStepRunReason.Cached);
        _ = await Assert.That(clone).IsEqualTo(IncrementalStepRunReason.Unchanged);
        _ = await Assert.That(source).IsEqualTo(IncrementalStepRunReason.Cached);
    }

    [Test]
    public async Task EditToTheMethodIsRegenerated()
    {
        var (output, clone, source) = RunTwice(
            """
            using System.Threading.Tasks;

            class Test
            {
                public Task OtherAsync() => Task.CompletedTask;

                [Cloning.Clone]
                public async Task MethodAsync()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class Test
            {
                public Task OtherAsync() => Task.CompletedTask;

                [Cloning.Clone]
                public async Task MethodAsync()
                {
                    await OtherAsync();
                }
            }
            """);

        _ = await Assert.That(output).IsEqualTo(IncrementalStepRunReason.Modified);
        _ = await Assert.That(clone).IsEqualTo(IncrementalStepRunReason.Modified);
        _ = await Assert.That(source).IsEqualTo(IncrementalStepRunReason.Modified);
    }

    private static (IncrementalStepRunReason Output, IncrementalStepRunReason Clone, IncrementalStepRunReason Source) RunTwice(string source, string updated)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        Compilation compilation = CSharpCompilation.Create(
            "Incremental",
            [tree, CSharpSyntaxTree.ParseText(Attribute)],
            Net100.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new IdentityCloneGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(updated)));

        var result = driver.GetRunResult().Results.Single();
        var (_, outputReason) = result.TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs).Single();

        return (
            outputReason,
            result.TrackedSteps["Clone"].Single().Outputs[0].Reason,
            result.TrackedSteps["GenerateSource"].Single().Outputs[0].Reason);
    }
}
