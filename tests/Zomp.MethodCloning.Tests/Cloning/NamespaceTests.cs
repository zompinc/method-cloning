namespace Zomp.MethodCloning.Tests.Cloning;

public class NamespaceTests
{
    [Test]
    public Task MultipleNamespaces() => """
namespace NsOne
{
    namespace NsTwo.NsThree
    {
        namespace NsFour
        {
            public partial class MultipleNamespaces
            {
                [Clone]
                async void EmptyAsync()
                {
                }
            }
        }
    }
}
""".Verify(SourceType.Full);

    // A global using already reaches the file the clone lands in, so the clone must not declare it again.
    [Test]
    public Task GlobalUsingInSourceFile() => """
global using System.Globalization;
using System.Text;

namespace N
{
    public partial class C
    {
        /// <summary>Formats with <see cref="CultureInfo"/> into a <see cref="StringBuilder"/>.</summary>
        [Clone]
        public async Task MethodAsync() => _ = new StringBuilder().Append(CultureInfo.InvariantCulture, $"{1}");
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task StaticUsings() => """
using static N2.C2;

namespace N1
{
    public partial class C1
    {
        [Clone]
        public async Task MethodAsync()
        {
            _ = OtherConst;
        }
    }
}

namespace N2
{
    public class C2
    {
        public const int OtherConst = 1;
    }
}
""".Verify(SourceType.Full);

    // The type argument resolves through a using inside the namespace, which a global using
    // would hide, so the source is the whole file.
    [Test]
    public Task GenericTypeInDeclarationPattern() => """
namespace N
{
    public class A { }
}

namespace M
{
    using N;

    public partial class B
    {
        [Clone]
        public async Task DoSomethingAsync(object arg)
        {
            if (arg is List<A> lst)
            {
                await Task.Delay(1);
            }
        }
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task GenericTypeInTypeOfExpression() => """
namespace N
{
    public class A { }
}

namespace M
{
    using N;

    public partial class B
    {
        [Clone]
        public async Task DoSomethingAsync()
        {
            _ = typeof(List<A>);
            await Task.Delay(1);
        }
    }
}
""".Verify(SourceType.Full);

    // A cref resolves against the file the clone lands in, so it needs the usings which were in
    // scope where the documentation was written.
    [Test]
    public Task KeepUsingsSoDocumentationCrefsResolve() => """
using System.Text;

namespace Test
{
    using System.Net.Sockets;

    public partial class Class
    {
        /// <summary>
        /// Sends a <see cref="StringBuilder"/> over a <see cref="Socket"/>.
        /// </summary>
        /// <param name="input">Where to read from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Cloning.Clone]
        public async Task SendAsync(Stream input, CancellationToken cancellationToken = default)
            => await input.FlushAsync(cancellationToken);
    }
}
""".Verify(SourceType.Full);

    // Extension calls keep their form, and resolve through the using the clone carries along.
    [Test]
    public Task ExtensionCallsResolveThroughCopiedUsings() => """
using System.Threading;
using System.Threading.Tasks;

namespace Callers
{
    using Extensi.ons123;

    partial class ExtensionMethods
    {
        [Clone]
        public static async Task ZeroParamsAsync(object o, CancellationToken ct) => await o.SomeMethodAsync(ct);

        [Clone]
        public static async Task TwoParamsAsync(object o, string s, int i, CancellationToken ct) => await o.SomeMethodAsync(s, i, ct);
    }
}

namespace Extensi.ons123
{
    internal static class MyExtensionClass
    {
        public static async Task SomeMethodAsync(this object _, CancellationToken _1) => await Task.CompletedTask;

        public static async Task SomeMethodAsync(this object _, string _2, int _3, CancellationToken _1) => await Task.CompletedTask;
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task GenericExtensionCalls() => """
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Callers
{
    using Extensi.ons123;

    partial class Extensions
    {
        [Clone]
        public static async Task HasGenericExtensionAsync(object o, CancellationToken ct)
        {
            var z = o.TryGetValue<Point>(out var item);
        }

        [Clone]
        public static async Task HasGeneric2ExtensionAsync(object o, CancellationToken ct)
        {
            var z = o.TryGetValue<Point, PointF>(out var _, out var _1);
        }
    }
}

namespace Extensi.ons123
{
    internal static class MyExtensionClass
    {
        public static bool TryGetValue<T>(this object _, out T? item)
        {
            item = default;
            return false;
        }

        public static bool TryGetValue<T1, T2>(this object _, out T1? item1, out T2? item2)
        {
            item1 = default;
            item2 = default;
            return false;
        }
    }
}
""".Verify(SourceType.Full);
}
