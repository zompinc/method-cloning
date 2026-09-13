namespace Zomp.MethodCloning.Tests.Cloning;

public class FileNameTests
{
    // Class, Class<T> and Class<T, T2> are three types, so their files must differ.
    [Test]
    public Task DoNotCollideClassNames() => """
namespace Test;

public partial class Class
{
    [Clone]
    public async Task MethodAsync()
    {
    }
}

public partial class Class<T>
{
    [Clone]
    public async Task MethodAsync()
    {
    }
}

public partial class Class<T, T2>
{
    [Clone]
    public async Task MethodAsync()
    {
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task ShortenLongFileName() => """
namespace A.Long.Enough.Namespace.To.Make.The.Generated.File.Name.Exceed.What.Is.Reasonable;

public partial class OuterClass
{
    public partial class InnerClass
    {
        [Clone]
        public async Task MethodAsync()
        {
        }
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task ShortenLongFileNameWhenTheMethodNameFillsIt() => """
namespace A.Long.Enough.Namespace.To.Make.The.Generated.File.Name.Exceed.What.Is.Reasonable;

public partial class OuterClass
{
    [Clone]
    public async Task ThisMethodHasAnUnreasonablyLongNameWhichOnItsOwnLeavesNoRoomForTheNamespaceOrTheContainingTypeAsync()
    {
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task ShortenLongFileNameKeepsOverloadsApart() => """
namespace A.Long.Enough.Namespace.To.Make.The.Generated.File.Name.Exceed.What.Is.Reasonable;

public partial class OuterClass
{
    public partial class InnerClass
    {
        [Clone]
        public async Task MethodAsync()
        {
        }

        [Clone]
        public async Task MethodAsync(int i)
        {
        }
    }
}
""".Verify(SourceType.Full);
}
