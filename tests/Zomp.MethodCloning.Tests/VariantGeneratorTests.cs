namespace Zomp.MethodCloning.Tests;

public class VariantGeneratorTests
{
    private const string FormatTypes = """
namespace Img.Formats.Bmp
{
    public sealed class BmpEncoder : Img.IImageEncoder { }

    public sealed class BmpFormat : Img.IImageFormat
    {
        public static BmpFormat Instance { get; } = new();
    }
}

namespace Img.Formats.Gif
{
    public sealed class GifEncoder : Img.IImageEncoder { }

    public sealed class GifFormat : Img.IImageFormat
    {
        public static GifFormat Instance { get; } = new();
    }
}

namespace Img.Formats.Png
{
    public sealed class PngEncoder : Img.IImageEncoder { }

    public sealed class PngFormat : Img.IImageFormat
    {
        public static PngFormat Instance { get; } = new();
    }
}

namespace Img
{
    public interface IImageEncoder { }

    public interface IImageFormat { }

    public sealed class FormatsManager
    {
        public IImageEncoder GetEncoder(IImageFormat format) => throw new System.NotSupportedException();
    }

    public sealed class Configuration
    {
        public FormatsManager ImageFormatsManager { get; } = new();
    }

    public class Image
    {
        public Configuration Configuration { get; } = new();

        public void Save(System.IO.Stream stream, IImageEncoder encoder) { }

        public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream, IImageEncoder encoder, System.Threading.CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
}
""";

    // The types, the calls between overloads and the documentation all name the format.
    [Test]
    public Task FormatOverloads() => TestHelper.Verify($$"""
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Img.Formats.Bmp;

{{FormatTypes}}

namespace Img
{
    [Zomp.MethodCloning.CloneVariants("Bmp", "Gif", "Png")]
    public static partial class ImageExtensions
    {
        /// <summary>
        /// Saves the image to the given stream in the Bmp format.
        /// </summary>
        /// <param name="source">The image this method extends.</param>
        /// <param name="stream">The stream to save the image to.</param>
        public static void SaveAsBmp(this Image source, Stream stream) => SaveAsBmp(source, stream, default);

        /// <summary>
        /// Saves the image to the given stream in the Bmp format.
        /// </summary>
        /// <param name="source">The image this method extends.</param>
        /// <param name="stream">The stream to save the image to.</param>
        /// <param name="encoder">The encoder to save the image with.</param>
        public static void SaveAsBmp(this Image source, Stream stream, BmpEncoder? encoder)
            => source.Save(stream, encoder ?? source.Configuration.ImageFormatsManager.GetEncoder(BmpFormat.Instance));

        /// <summary>
        /// Saves the image to the given stream in the Bmp format.
        /// </summary>
        /// <param name="source">The image this method extends.</param>
        /// <param name="stream">The stream to save the image to.</param>
        /// <param name="encoder">The encoder to save the image with.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task SaveAsBmpAsync(this Image source, Stream stream, BmpEncoder? encoder, CancellationToken cancellationToken = default)
            => source.SaveAsync(stream, encoder ?? source.Configuration.ImageFormatsManager.GetEncoder(BmpFormat.Instance), cancellationToken);
    }
}
""");

    // Only the blend function each equation calls differs between the blend modes.
    [Test]
    public Task BlendFunctions() => TestHelper.Verify("""
using System.Numerics;

namespace Blend
{
    [Zomp.MethodCloning.CloneVariants("Normal", "Multiply", "Screen")]
    internal static partial class PorterDuff
    {
        /// <summary>
        /// Returns the result of the "NormalSrcAtop" compositing equation.
        /// </summary>
        public static Vector4 NormalSrcAtop(Vector4 backdrop, Vector4 source, float opacity)
        {
            source = WithW(source, source * opacity);

            return Atop(backdrop, source, Normal(backdrop, source));
        }
    }

    internal static partial class PorterDuff
    {
        public static Vector4 Normal(Vector4 backdrop, Vector4 source) => source;

        public static Vector4 Multiply(Vector4 backdrop, Vector4 source) => backdrop * source;

        public static Vector4 Screen(Vector4 backdrop, Vector4 source) => Vector4.One - ((Vector4.One - backdrop) * (Vector4.One - source));

        public static Vector4 Atop(Vector4 backdrop, Vector4 source, Vector4 blend) => blend;

        public static Vector4 WithW(Vector4 value, Vector4 w) => value;
    }
}
""");

    // The attribute marks the method, is dropped from its variants, and leaves the other method alone.
    [Test]
    public Task MethodLevelAttribute() => TestHelper.Verify($$"""
{{FormatTypes}}

namespace Img
{
    public static partial class ImageExtensions
    {
        /// <summary>
        /// Gets the Bmp encoder.
        /// </summary>
        [Zomp.MethodCloning.CloneVariants("Bmp", "Png")]
        [System.Obsolete("Use the configuration")]
        public static IImageEncoder GetBmpEncoder(this Image source)
            => source.Configuration.ImageFormatsManager.GetEncoder(Img.Formats.Bmp.BmpFormat.Instance);

        public static IImageEncoder GetDefaultEncoder(this Image source) => GetBmpEncoder(source);
    }
}
""");

    // A method written without annotations gets variants without them.
    [Test]
    public Task NullableObliviousMethod() => TestHelper.Verify($$"""
{{FormatTypes}}

namespace Img
{
    public static partial class ImageExtensions
    {
#nullable disable
        [Zomp.MethodCloning.CloneVariants("Bmp", "Gif")]
        public static void SaveAsBmp(this Image source, System.IO.Stream stream, Img.Formats.Bmp.BmpEncoder encoder)
            => source.Save(stream, encoder ?? source.Configuration.ImageFormatsManager.GetEncoder(Img.Formats.Bmp.BmpFormat.Instance));
#nullable restore
    }
}
""");

    [Test]
    public Task NameWithoutOriginal() => TestHelper.Verify($$"""
{{FormatTypes}}

namespace Img
{
    public static partial class ImageExtensions
    {
        [Zomp.MethodCloning.CloneVariants("Bmp", "Gif")]
        public static void Save(this Image source, System.IO.Stream stream)
            => source.Save(stream, source.Configuration.ImageFormatsManager.GetEncoder(Img.Formats.Bmp.BmpFormat.Instance));
    }
}
""");

    // Gif has an encoder but no quantizer, so the Gif variant keeps the Bmp one and says so.
    [Test]
    public Task NoCounterpart() => TestHelper.Verify($$"""
{{FormatTypes}}

namespace Img.Formats.Bmp
{
    public sealed class BmpQuantizer { }
}

namespace Img
{
    public static partial class ImageExtensions
    {
        [Zomp.MethodCloning.CloneVariants("Bmp", "Gif")]
        public static object CreateBmpQuantizer(this Image source, Img.Formats.Bmp.BmpEncoder encoder)
            => new Img.Formats.Bmp.BmpQuantizer();
    }
}
""");

    // Methods written for different variants both produce a Png one, which must not share a file.
    [Test]
    public Task OverlappingVariants() => TestHelper.Verify($$"""
{{FormatTypes}}

namespace Img
{
    public sealed class ImageMetadata { }

    public sealed class FrameMetadata { }

    public static partial class MetadataExtensions
    {
        [Zomp.MethodCloning.CloneVariants("Bmp", "Png")]
        public static string GetBmpMetadata(this ImageMetadata source) => nameof(Img.Formats.Bmp.BmpFormat);

        [Zomp.MethodCloning.CloneVariants("Gif", "Png")]
        public static string GetGifMetadata(this FrameMetadata source) => nameof(Img.Formats.Gif.GifFormat);
    }
}
""");

    // The return type names the format, and so does the using directive the documentation relies on.
    [Test]
    public Task ReturnTypeAndUsings() => TestHelper.Verify($$"""
{{FormatTypes}}

namespace Img.Formats.Bmp
{
    public sealed class BmpMetadata { }
}

namespace Img.Formats.Gif
{
    public sealed class GifMetadata { }
}

namespace Img
{
    using Img.Formats.Bmp;

    public static partial class MetadataExtensions
    {
        /// <summary>
        /// Gets the <see cref="BmpMetadata"/> of the image.
        /// </summary>
        /// <returns>The <see cref="BmpMetadata"/>.</returns>
        [Zomp.MethodCloning.CloneVariants("Bmp", "Gif")]
        public static BmpMetadata GetBmpMetadata(this Image source) => new BmpMetadata();
    }
}
""");
}
