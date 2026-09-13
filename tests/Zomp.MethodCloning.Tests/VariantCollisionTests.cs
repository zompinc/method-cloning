namespace Zomp.MethodCloning.Tests;

public class VariantCollisionTests
{
    // Methods written for Bmp and for Png both produce SaveAsGif(Image, Stream), so neither
    // Gif variant is written and both are reported.
    [Test]
    public Task CollidingVariants() => TestHelper.Verify("""
namespace Img
{
    public class Image { }

    public static partial class ImageExtensions
    {
        [Zomp.MethodCloning.CloneVariants("Bmp", "Gif")]
        public static void SaveAsBmp(this Image source, System.IO.Stream stream) { }

        [Zomp.MethodCloning.CloneVariants("Png", "Gif")]
        public static void SaveAsPng(this Image source, System.IO.Stream stream) { }
    }
}
""");
}
