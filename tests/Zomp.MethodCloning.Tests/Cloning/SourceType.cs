namespace Zomp.MethodCloning.Tests.Cloning;

/// <summary>
/// How the source of a cloning test is wrapped before it is compiled.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// The body of <c>[Clone] async Task MethodAsync(CancellationToken ct)</c>, inside the class
    /// <see cref="ClassBody"/> wraps.
    /// </summary>
    MethodBody,

    /// <summary>
    /// The body of <c>partial class Class</c> in <c>namespace Test</c>.
    /// </summary>
    ClassBody,

    /// <summary>
    /// The whole file, as it is.
    /// </summary>
    Full,
}
