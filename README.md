# Zomp.MethodCloning

**Roslyn source generators that copy methods, so you write each one once.**

| I want to...                                                          | Use                                                            |
| --------------------------------------------------------------------- | -------------------------------------------------------------- |
| Stop maintaining T4 templates that repeat a method per format or type | [Zomp.MethodCloning.Variants](src/Zomp.MethodCloning.Variants) |
| Write my own generator that copies methods and changes them           | [Zomp.MethodCloning](src/Zomp.MethodCloning)                   |

[Zomp.SyncMethodGenerator](https://github.com/zompinc/sync-method-generator), which writes the sync version of async methods, is built on the same core.

## Learn more

- [Variants reference](docs/variants.md): every rule, the limitations, and migrating from T4.
- [AGENTS.md](AGENTS.md): how the repository is built and tested, and the rules the core's source files follow.

## Build

```bash
dotnet build
dotnet test
```
