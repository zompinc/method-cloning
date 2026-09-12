# Zomp.MethodCloning

Building blocks for Roslyn source generators which copy a method into a file of
its own and change it on the way: the plumbing that
[Zomp.SyncMethodGenerator](https://github.com/zompinc/sync-method-generator)
uses to turn async methods into sync ones, without the async rules.

It ships as source. The files compile into the generator which references the
package, as internal types, so there is no DLL to bundle into `analyzers/` and
no version for two generators to disagree on.

## What it does

- `CloneTarget` finds the methods an attribute marks, on the method itself or on
  its containing type.
- `MethodLocation` collects what a copy needs to compile elsewhere: namespaces,
  using directives and the containing types to declare again as partial.
- `CloningRewriter` fully qualifies every type and static member the method
  names. A transformation derives from it and overrides what it changes, with
  the `MapSymbol`, `MapTypeName` and `MapMemberName` hooks for substitutions.
- `ClonedMethod` and `ClonedMethodOutput` name the files, detect copies which
  would declare the same member, and register the output.

## Requirements

- C# 12 or later in the generator project.
- On `netstandard2.0`, the polyfills the files rely on, such as `IsExternalInit`
  and `NotNullWhenAttribute`. [PolySharp](https://github.com/Sergio0694/PolySharp)
  provides them.
- Optionally the `ROSLYN_4_12_OR_GREATER` and `ROSLYN_5_0_OR_GREATER` constants,
  when the generator compiles against those Roslyn versions. Without them the
  files use APIs available since Roslyn 4.8.

## Usage

```xml
<PackageReference Include="Zomp.MethodCloning" PrivateAssets="all" />
```
