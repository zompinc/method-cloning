namespace Zomp.MethodCloning.Tests.Cloning;

public class NullabilityTests
{
    // C# 7.3 has no nullable reference types, so the clone must not enable them.
    [Test]
    public Task NoNullableSupport() => """
namespace Test {
    partial class WithNullableDisabled
    {
        [Cloning.Clone]
        public async System.Threading.Tasks.Task MethodAsync(string l) => await System.Threading.Tasks.Task.CompletedTask;
    }
}
""".Verify(SourceType.Full, LanguageVersion.CSharp7_3);

    // A method written with nullable disabled is cloned with it disabled.
    [Test]
    public Task NullableDisabledInFile() => """
#nullable disable

namespace Test;

partial class MyClass
{
    [Clone]
    public async Task MethodAsync()
    {
        string f = null;
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task NullableValueTypeParameter() => """
[Clone]
async Task<string> MethodAsync(System.Net.HttpStatusCode? bar)
{
    return string.Empty;
}
""".Verify();

    [Test]
    public Task NullableReturnType() => """
[Clone]
public Task<string?> MethodAsync() => Task.FromResult<string?>(null);
""".Verify();

    [Test]
    public Task NullableReturnTypeGenerics() => """
[Clone]
public async Task<Tuple<Tuple<string, StringBuilder?>?, object?>?> MethodAsync()
=> new(new(null, null), null);
""".Verify();

    [Test]
    public Task NullableReturnTypeTuple() => """
[Clone]
public Task<(bool, string?)> MethodAsync() => Task.FromResult<(bool, string?)>((true, null));
""".Verify();
}
