using Zomp.MethodCloning;

namespace Consumer;

/// <summary>
/// The Bmp format.
/// </summary>
internal static class BmpFormat
{
}

/// <summary>
/// The Png format.
/// </summary>
internal static class PngFormat
{
}

/// <summary>
/// A method written for Bmp, of which the generator writes the Png variant.
/// </summary>
internal static partial class Formats
{
    /// <summary>
    /// Gets the name of the Bmp format.
    /// </summary>
    /// <returns>The name of <see cref="BmpFormat"/>.</returns>
    [CloneVariants("Bmp", "Png")]
    public static string NameOfBmp() => nameof(BmpFormat);

    /// <summary>
    /// Calls the Png variant, which does not compile unless the generator wrote it.
    /// </summary>
    /// <returns>The name of <see cref="PngFormat"/>.</returns>
    public static string CallVariant() => NameOfPng();
}
