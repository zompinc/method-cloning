namespace Zomp.MethodCloning.Tests.Cloning;

// Directives in front of the method belong to the file it was written in and are dropped.
// Directives inside it are part of it and must survive balanced.
public class PreprocessorTests
{
    [Test]
    public Task DirectivesBeforeTheMethod() => """
#if !THIS_IS_FALSE
private static readonly bool b = false;
#else
private static readonly bool b = false;
#endif

[Clone]
async Task MethodAsync(StreamWriter writer, CancellationToken ct = default)
{
}
""".Verify();

    [Test]
    public Task DirectivesAroundTheMethod() => """
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public partial class Class
{
#if false
These comments shouldn't show
#endif
#if true
#else
#endif
#region R1
#if true
    /// <summary>
    /// A summary
    /// </summary>
    [Clone]
    public async Task WrappedAsync() => await Task.CompletedTask;
#endif
#endregion
}
""".Verify(SourceType.Full);

    [Test]
    public Task DirectivesAroundBraces() => """
#if !BLA
{
#endif

#if !BLA
}
#endif
""".Verify(SourceType.MethodBody);

    [Test]
    public Task ArgumentInsideDirective() => """
[Clone]
async Task MethodAsync(StreamReader reader, CancellationToken ct = default)
{
    await reader.ReadLineAsync(
#if NET8_0_OR_GREATER
        ct
#endif
    );
}
""".Verify();

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task CallChainInsideDirective(bool withConfigureAwait) => $$"""
[Clone]
async Task MethodAsync(XmlReader reader, CancellationToken ct = default)
{
    if (await reader.ReadAsync()
#if NET8_0_OR_GREATER
                    .WaitAsync(ct)
#endif
                    {{(withConfigureAwait ? ".ConfigureAwait(false)" : string.Empty)}})
    {
    }
}
""".Verify(parameters: withConfigureAwait);

    [Test]
    public Task StatementInsideDirective() => """
#if !MY_SPECIAL_SYMBOL
if (true)
{
}
#endif
""".Verify(SourceType.MethodBody);

    [Test]
    public Task IfSplitByDirective() => """
#if MY_SPECIAL_SYMBOL
if (true)
#else
if (true)
#endif
{
}
""".Verify(SourceType.MethodBody);

    [Test]
    public Task UsingSplitByDirective() => """
#if MY_SPECIAL_SYMBOL
using (var stream = new global::System.IO.MemoryStream())
#else
using (var stream2 = new global::System.IO.MemoryStream())
#endif
{
}
""".Verify(SourceType.MethodBody);

    [Test]
    public Task ExpressionSplitByDirective() => """
var bar =
#if MY_SPECIAL_SYMBOL
    true;
#else
    false;
#endif
""".Verify(SourceType.MethodBody);
}
