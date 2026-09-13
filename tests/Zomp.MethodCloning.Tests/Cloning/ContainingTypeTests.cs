namespace Zomp.MethodCloning.Tests.Cloning;

public class ContainingTypeTests
{
    [Test]
    public Task Struct() => """
namespace Test;
public partial struct Struct
{
    [Clone]
    public readonly async Task MethodAsync() => await Task.Delay(1000);
}
""".Verify(SourceType.Full);

    [Test]
    public Task Record() => """
namespace Test;
public partial record Record
{
    [Clone]
    public async Task MethodAsync() => await Task.Delay(1000);
}
""".Verify(SourceType.Full);

    [Test]
    public Task RecordStruct() => """
namespace Test;
public partial record struct RecordStruct
{
    [Clone]
    public readonly async Task MethodAsync() => await Task.Delay(1000);
}
""".Verify(SourceType.Full);

    [Test]
    public Task RecordClass() => """
namespace Test;
public partial record class Record
{
    [Clone]
    public async Task MethodAsync() => await Task.Delay(1000);
}
""".Verify(SourceType.Full);

    // Every level keeps its modifiers, including the private protected one.
    [Test]
    public Task NestedTypes() => """
namespace NsOne
{
    public partial class C1
    {
        partial class C2
        {
            private protected partial class C3
            {
                private partial class C4
                {
                    [Clone]
                    async void EmptyAsync()
                    {
                    }
                }
            }
        }
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task GenericClassWithConstraints() => """
namespace Test;
partial class GenericClass<T1, T2> where T1 : struct where T2 : class
{
    [Clone]
    async void EmptyAsync()
    {
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task GenericClassWithGenericInnerClass() => """
namespace Test;

internal partial class Class<T>
{
    [Clone]
    public async Task FooAsync(Int<int> i) { }

    internal class Int<TU> { }
}
""".Verify(SourceType.Full);

    // A default interface method is cloned into the interface.
    [Test]
    public Task InterfaceWithBody() => """
namespace Test;

public partial interface IMyInterface
{
    [Clone]
    async Task MethodAsync()
    {
    }
}
""".Verify(SourceType.Full);
}
