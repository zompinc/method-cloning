# Zomp.MethodCloning

Building blocks for Roslyn source generators which copy a method into a file of its own and change it on the way, and a ready generator built on them.

| Package                                                       | What it is                                                                  |
| ------------------------------------------------------------- | --------------------------------------------------------------------------- |
| [Zomp.MethodCloning](src/Zomp.MethodCloning)                   | The core, shipped as source and compiled into the generator which uses it   |
| [Zomp.MethodCloning.Variants](src/Zomp.MethodCloning.Variants) | Writes the variants of a method from the one written by hand, replacing T4 |

The core is what [Zomp.SyncMethodGenerator](https://github.com/zompinc/sync-method-generator) uses to write the sync version of an async method, without the async rules.
