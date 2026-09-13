namespace Consumer;

/// <summary>
/// Marks a method for the sample generator to copy.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class CloneAttribute : Attribute;

/// <summary>
/// A method, and a call to the copy the sample generator writes of it.
/// </summary>
internal static partial class Sample
{
    /// <summary>
    /// Doubles a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Twice the value.</returns>
    [Clone]
    public static int Twice(int value) => value * 2;

    /// <summary>
    /// Calls the copy, which does not compile unless the generator wrote it.
    /// </summary>
    /// <returns>Four.</returns>
    public static int CallCopy() => TwiceCopy(2);
}
