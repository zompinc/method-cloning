namespace Zomp.MethodCloning.Tests.Cloning;

public class TargetTests
{
    // On an interface the attribute marks its methods, which have no body to clone.
    [Test]
    public Task TargetInterface() => """
namespace Test;

[Clone]
public partial interface ITargetInterface
{
    Task MethodAsync();
}
""".Verify(SourceType.Full);

    [Test]
    [Arguments("class")]
    [Arguments("struct")]
    [Arguments("record")]
    [Arguments("record struct")]
    public Task TargetType(string type) => $$"""
namespace Test;

[Clone]
public partial {{type}} Target
{
    Task MethodAsync()
    {
        return Task.CompletedTask;
    }
}
""".Verify(SourceType.Full, parameters: type.Replace(" ", string.Empty, StringComparison.Ordinal));
}
