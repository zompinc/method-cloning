# Zomp.MethodCloning.Variants

Writes the variants of a method from the one written by hand. It replaces T4
templates which stamp out the same method once per format, pixel type or blend
mode, with code which compiles, refactors and debugs like any other.

```csharp
[CloneVariants("Bmp", "Gif", "Png")]
public static partial class ImageExtensions
{
    /// <summary>
    /// Saves the image to the given stream in the Bmp format.
    /// </summary>
    public static void SaveAsBmp(this Image source, Stream stream, BmpEncoder? encoder)
        => source.Save(stream, encoder ?? source.Configuration.ImageFormatsManager.GetEncoder(BmpFormat.Instance));
}
```

generates `SaveAsGif`, taking a `GifEncoder` and using `GifFormat.Instance`, and
`SaveAsPng` in the same way. The word naming the variant is swapped:

- in the name of the method, which therefore has to contain it;
- in every type the method refers to whose name contains it, found by swapping
  the word in its namespace and name (`Formats.Bmp.BmpEncoder` becomes
  `Formats.Gif.GifEncoder`);
- in every static member it calls, such as another overload (`SaveAsBmp` becomes
  `SaveAsGif`);
- in its documentation.

Applied to a partial type, the attribute marks every method in that declaration
of the type. Each variant keeps the nullable context of the method it is written
from.

## Diagnostics

| ID     | Severity | Meaning                                                     |
| ------ | -------- | ----------------------------------------------------------- |
| ZMC001 | Error    | Two variants would declare the same member                  |
| ZMC002 | Error    | The method's name does not contain the word being swapped   |
| ZMC003 | Warning  | A type named after the original has no counterpart, so kept |

## Usage

```xml
<PackageReference Include="Zomp.MethodCloning.Variants" PrivateAssets="all" />
```
