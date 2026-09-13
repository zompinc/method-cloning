namespace Zomp.MethodCloning.Tests.Cloning;

// A member of a C# 14 extension block is cloned into the extension block, which is declared
// again around it; the other members of the block are left out.
public class ExtensionBlockTests
{
    [Test]
    public Task ExtensionBlockAndClassicExtension() => """
namespace Tests;

static partial class Class
{
    [Clone]
    public static async IAsyncEnumerable<T> WhereGreaterThan<T>(this IAsyncEnumerable<T> source, T threshold, int dummy)
        where T : INumber<T>
    {
        await foreach (var item in source)
        {
            if (item > threshold)
            {
                yield return item;
            }
        }
    }

    extension<T>(IAsyncEnumerable<T> source)
        where T : INumber<T>
    {
        [Clone]
        public async IAsyncEnumerable<T> WhereGreaterThan(T threshold)
        {
            await foreach (var item in source)
            {
                if (item > threshold)
                {
                    yield return item;
                }
            }
        }

        [Clone]
        public async IAsyncEnumerable<T> WhereLessThan(T threshold)
        {
            await foreach (var item in source)
            {
                if (item < threshold)
                {
                    yield return item;
                }
            }
        }
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task ExtensionBlockCallingAnotherExtension() => """
namespace Helpers
{
    internal static partial class StreamExtensions
    {
        extension(Stream stream)
        {
            internal async Task DrainAsync(CancellationToken cancellationToken = default)
                => await stream.FlushAsync(cancellationToken);
        }
    }
}

namespace Callers
{
    using Helpers;

    public static partial class StreamCallers
    {
        extension(Stream stream)
        {
            [Clone]
            public async Task CopyAndDrainAsync(Stream destination, CancellationToken cancellationToken = default)
            {
                await stream.CopyToAsync(destination, cancellationToken);
                await stream.DrainAsync(cancellationToken);
            }
        }
    }
}
""".Verify(SourceType.Full);
}
