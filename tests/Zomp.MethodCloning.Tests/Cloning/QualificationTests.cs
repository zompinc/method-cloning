namespace Zomp.MethodCloning.Tests.Cloning;

// Every type and static member a clone names has to resolve to the same symbol in the file the
// clone lands in, whatever form it was written in.
public class QualificationTests
{
    [Test]
    public Task CatchType() => """
try
{
    await Task.CompletedTask;
}
catch (OperationCanceledException)
{
}
""".Verify(SourceType.MethodBody);

    [Test]
    public Task NotPattern() => "_ = new object() is not DBNull;".Verify(SourceType.MethodBody);

    [Test]
    public Task PatternMatchingWithConstant() => "_ = 1 is 2;".Verify(SourceType.MethodBody);

    [Test]
    public Task PatternIsNotLiteral() => "_ = StringComparison.CurrentCulture is not StringComparison.CurrentCulture;".Verify(SourceType.MethodBody);

    [Test]
    public Task PatternIsEnumMember() => "_ = StringComparison.CurrentCulture is StringComparison.CurrentCulture;".Verify(SourceType.MethodBody);

    [Test]
    public Task BinaryPattern() => "_ = new object() is DBNull or Stream;".Verify(SourceType.MethodBody);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task EnumPatternThroughStaticUsing(bool isQualified) => $$"""
using static System.Data.ConnectionState;
namespace Test;

partial class Class
{
    [Clone]
    public Task MethodAsync()
    {
        _ = {{(isQualified ? "System.Data.ConnectionState." : string.Empty)}}Closed is System.Data.ConnectionState.Closed;
        return Task.CompletedTask;
    }
}
""".Verify(SourceType.Full, parameters: isQualified);

    [Test]
    public Task EnumNamedLikeItsMember() => """
enum Test { Test }

partial class Class
{
    [Clone]
    public Task<bool> ReturnTrueAsync()
    {
        return Task.FromResult(Test.Test is Test.Test);
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task VariableDeclarationRedundant() => "MemoryStream ms = new MemoryStream();".Verify(SourceType.MethodBody);

    [Test]
    public Task VariableDeclaration() => "MemoryStream ms = new();".Verify(SourceType.MethodBody);

    [Test]
    public Task DeclarationExpression() => "new Dictionary<int, Stream>().TryGetValue(0, out Stream a);".Verify(SourceType.MethodBody);

    [Test]
    public Task NullableDeclarationExpression() => "new Dictionary<int, Stream>().TryGetValue(0, out Stream? a);".Verify(SourceType.MethodBody);

    [Test]
    public Task ForeachType() => """
foreach (Int32 i in new Int32[] { 1 })
{
}

await Task.CompletedTask;
""".Verify(SourceType.MethodBody);

    [Test]
    public Task NullableForeach() => """
foreach (Stream? i in Array.Empty<Stream?>())
{
}
""".Verify(SourceType.MethodBody);

    [Test]
    public Task NestedGenerics() => "var dict = new Dictionary<DateTime, List<int>>();".Verify(SourceType.MethodBody);

    [Test]
    public Task LocalTypes() => """
String myStr;
string myStrPredefined;
Exception ex;
Int16 myShort;
Int16[] myShorts;
long myLong;

await Task.CompletedTask;
""".Verify(SourceType.MethodBody);

    [Test]
    public Task ArrayParameter() => """
[Clone]
public async Task MethodAsync(Int32[] o)
{
}
""".Verify();

    [Test]
    public Task TwoDArrayParameter() => """
[Clone]
public async Task MethodAsync(Func<object[,], Task> o)
{
}
""".Verify();

    [Test]
    public Task CastToNestedType() => """
class CustomClass { }

[Clone]
public async Task<object> GetCustomObjectAsync(object o)
{
    return (CustomClass)o;
}
""".Verify();

    [Test]
    public Task CastTwice() => """
class CustomClass { }

[Clone]
public async Task<object> GetCustomObjectAsync(object o)
{
    return (CustomClass)(object)(CustomClass)o;
}
""".Verify();

    [Test]
    public Task FullyQualifiedArray() => "System.Text.RegularExpressions.Regex[] variable = null!;".Verify(SourceType.MethodBody);

    [Test]
    public Task IsExpression() => """
[Clone]
public async Task HasIsExpressionAsync(Stream stream) => _ = stream is FileStream;
""".Verify();

    [Test]
    public Task AsCast() => "_ = new object() as Stream;".Verify(SourceType.MethodBody);

    [Test]
    public Task Discard() => "_ = int.TryParse(\"2\", out _);".Verify(SourceType.MethodBody);

    [Test]
    public Task TypeOf() => "_ = typeof(Stream);".Verify(SourceType.MethodBody);

    [Test]
    public Task NameOf() => "_ = nameof(Stream);".Verify(SourceType.MethodBody);

    [Test]
    public Task NameOfGenericTuple() => "_ = nameof(IEnumerable<(Stream? S, int I)>);".Verify(SourceType.MethodBody);

    [Test]
    public Task TupleParameter() => "[Clone]public async Task MethodAsync((Stream S, int I) z) { }".Verify();

    [Test]
    public Task NullableTupleParameter() => "[Clone]public async Task MethodAsync((Stream? S, int I) z) { }".Verify();

    [Test]
    public Task NullableTupleOutVariable() => "new Dictionary<int, (int I, Stream? S)?>().TryGetValue(0, out (int I, Stream? S)? a);".Verify(SourceType.MethodBody);

    [Test]
    public Task QualifiedGenericName() => "System.Collections.Generic.HashSet<byte> z = null!;".Verify(SourceType.MethodBody);

    // The same type written with no namespace, part of one, and all of it.
    [Test]
    [Arguments("none")]
    [Arguments("System")]
    [Arguments("global")]
    public Task QualifiedNonGenericName(string qualification) => $$"""
namespace System;

public partial class Class
{
    [Clone]
    public async Task MethodAsync()
    {
        {{qualification switch { "System" => "System.", "global" => "global::System.", _ => string.Empty }}}Security.Cryptography.CryptographicException z = null!;
    }
}
""".Verify(SourceType.Full, parameters: qualification);

    [Test]
    public Task SwitchExpressionType() => """
[Clone]
public async Task SwitchAsync(Stream stream)
{
    var s = stream switch
    {
        FileStream fs => fs,
        _ => throw new InvalidOperationException("No"),
    };
}
""".Verify();

    [Test]
    public Task DelegateCreation() => "_ = new DataReceivedEventHandler((s, e) => { });".Verify(SourceType.MethodBody);

    // A qualified name inside an interpolation must not be mistaken for a format string.
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task InterpolationIsParenthesized(bool parenthesized) => $$"""
var z = $"123{{{Call(parenthesized)}}}456";
""".Verify(SourceType.MethodBody, parameters: parenthesized);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task InterpolationWithFormatString(bool parenthesized) => $$"""
var z = $"123{{{Call(parenthesized)}}:hh}456";
""".Verify(SourceType.MethodBody, parameters: parenthesized);

    private static string Call(bool parenthesized)
        => parenthesized ? "(await File.ReadAllTextAsync(\"123\"))" : "await File.ReadAllTextAsync(\"123\")";
}
