namespace Zomp.MethodCloning.Tests.Cloning;

// What a method's body refers to: other methods, fields, locals and delegates.
public class MethodBodyTests
{
    [Test]
    public Task GenericMethodInferringTypeArgument() => """
[Clone]
public async Task<bool> FooAsync()
{
    return await Bar(() => Task.FromResult(true));
}

public T Bar<T>(Func<T> innerLogic) => innerLogic();
""".Verify();

    [Test]
    public Task GenericMethodWithExplicitTypeArgument() => """
[Clone]
public async Task<bool> FooAsync()
{
    return await Bar<Task<bool>>(() => Task.FromResult(true));
}

public T Bar<T>(Func<T> innerLogic) => innerLogic();
""".Verify();

    [Test]
    public Task GenericMethodInGenericClass() => """
namespace Test;
partial class GenericClass<T>
{
    [Clone]
    public async Task<T> FooAsync<T>(CancellationToken ct = default)
        => await this.InnerFooAsync<T>(ct);

    private async Task<T> InnerFooAsync<T>(CancellationToken ct = default) => default;
}
""".Verify(SourceType.Full);

    [Test]
    public Task RecursiveGenericCall() => """
[Clone]
public async Task MyFuncAsync<T>()
{
    await MyFuncAsync<T>();
}
""".Verify();

    [Test]
    public Task ConstInGenericClass() => """
namespace Test;
partial class GenericClass<T>
{
    private const string Bar = "Bar";

    [Clone]
    public async Task MethodAsync()
    {
        _ = Bar;
    }
}
""".Verify(SourceType.Full);

    [Test]
    public Task StaticFieldAndNestedClass() => """
[Clone]
public static async Task MethodAsync(CancellationToken ct)
{
    int a = await PrivateClass.FromResult(2, 6), b = await Task.FromResult(2), c = await pc.IntProperty(ct);
}

static PrivateClass pc = new PrivateClass();

private class PrivateClass
{
    internal Func<CancellationToken, Task<int>> IntProperty => (ct) => Task.FromResult(2);

    public static Task<TResult> FromResult<TResult>(TResult delay, int unrelated) => Task.FromResult(delay);
}
""".Verify();

    [Test]
    public Task GenericReturnTypes() => """
[Clone]
public static async Task<IList<Point>> GetPointsAsync()
{
    return await Task.FromResult(new[] { new Point(1, 2) });
}

[Clone]
public static async Task<LinkedListNode<Point>[]> GetNodesAsync<T>() where T : new()
{
    return await Task.FromResult(new LinkedListNode<Point>[] { });
}

[Clone]
public static async Task<IList<T>> GetArrayOfTAsync<T>() where T : new()
{
    return await Task.FromResult(new T[] { new T() });
}
""".Verify();

    [Test]
    public Task NullableDelegateParameter() => """
[Clone]
public static async Task WithActionAsync(Action<Point, IEnumerable<Point>>? action) { }
""".Verify();

    [Test]
    public Task DelegateParameter() => """
[Clone]
public async Task<int> MethodAsync(Func<Task<int>> task)
{
    var r = await task();
    return r;
}
""".Verify();

    [Test]
    public Task LocalFunction() => """
[Clone]
public static async Task<int> InternalExampleAsync(Stream stream, CancellationToken ct)
{
    static async Task<int> Internal(Stream stream, CancellationToken ct)
    {
        var buf = new byte[1];
        return await stream.ReadAsync(buf, 0, 1, ct);
    }
    return await Internal(stream, ct);
}
""".Verify();

    [Test]
    public Task LocalFunctionWithParams() => """
static byte[] HelperMethod(params int[] myParams) => null!;
_ = HelperMethod(1, 2);
""".Verify(SourceType.MethodBody);

    [Test]
    public Task DefaultParameterValue() => """
[Clone]
public async Task GetColorAsync(FileAccess access = FileAccess.Read) { }
""".Verify();

    [Test]
    public Task Event() => """
public event EventHandler? MyEvent;

[Clone]
private async Task MethodAsync()
{
    MyEvent?.Invoke(this, EventArgs.Empty);
}
""".Verify();
}
