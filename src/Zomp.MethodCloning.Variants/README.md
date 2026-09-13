# Zomp.MethodCloning.Variants

**Write a method once. Get one per format, type or mode.**

A source generator that replaces T4 templates. Write `SaveAsBmp` by hand, list the other formats, and `SaveAsGif`, `SaveAsPng` and the rest are generated on every build, in any IDE.

## You write

```csharp
[CloneVariants("Bmp", "Gif", "Png")]
public static partial class ImageExtensions
{
    /// <summary>Saves the image in the Bmp format.</summary>
    public static void SaveAsBmp(this Image image, Stream stream, BmpEncoder? encoder)
        => image.Save(stream, encoder ?? BmpFormat.Instance.DefaultEncoder);
}
```

## You get

```csharp
/// <summary>Saves the image in the Gif format.</summary>
public static void SaveAsGif(this Image image, Stream stream, GifEncoder? encoder)
    => image.Save(stream, encoder ?? GifFormat.Instance.DefaultEncoder);

/// <summary>Saves the image in the Png format.</summary>
public static void SaveAsPng(this Image image, Stream stream, PngEncoder? encoder)
    => image.Save(stream, encoder ?? PngFormat.Instance.DefaultEncoder);
```

Simplified for reading: the generated code fully qualifies every name.

## Install

```xml
<PackageReference Include="Zomp.MethodCloning.Variants" PrivateAssets="all" />
```

Works with the .NET 8 SDK or later. Nothing is added to your assembly.

## What gets swapped

| Where                          | `Bmp` becomes `Gif`                                  |
| ------------------------------ | ---------------------------------------------------- |
| The method's name              | `SaveAsBmp` to `SaveAsGif` (the name must contain the word) |
| Types it uses                  | `Formats.Bmp.BmpEncoder` to `Formats.Gif.GifEncoder` |
| Static calls by simple name    | `SaveAsBmp(image, stream)` to `SaveAsGif(image, stream)` |
| Its documentation              | "in the Bmp format" to "in the Gif format"           |
| The file's using directives    | `using Formats.Bmp;` to `using Formats.Gif;`          |

A type with no counterpart, such as a `BmpQuantizer` with no `GifQuantizer`, is kept, with warning ZMC003.

## Good to know

- **On a type**, the attribute covers every method in that declaration. **On a method**, it covers just that method, and wins over the type's.
- **Nullability follows your method.** Write it in `#nullable disable` to match what a T4 template produced.
- **The word is replaced everywhere.** With `Normal` as the word, `Normalize()` would become `Multiplyize()`. Pick a word that appears nowhere else.
- **Not swapped:** generic and nested types, calls written as `image.SaveAsBmp()`, string literals, and comments inside the method.

Full rules, limitations and a step-by-step migration from T4: [the reference](https://github.com/zompinc/method-cloning/blob/master/docs/variants.md).

## Diagnostics

| ID     | Severity | Means                                         | Fix                                                |
| ------ | -------- | --------------------------------------------- | -------------------------------------------------- |
| ZMC001 | Error    | Two variants would declare the same member    | Remove the duplicate from the list                 |
| ZMC002 | Error    | The method's name lacks the word being swapped | Rename the method, or take the attribute off it   |
| ZMC003 | Warning  | A type has no counterpart, so it is kept      | Add the counterpart, or suppress if intended       |
