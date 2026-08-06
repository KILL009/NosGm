# ServiceManager package ownership repair

`NosGm.ServiceManager` is a legacy .NET Framework 4.8.1 project outside `NosGm.sln`. Its package paths previously escaped one directory above the repository, its root `packages.config` declared only a fraction of the assemblies referenced by the project, and a second nested package manifest declared conflicting dependency versions.

This repair keeps the existing `packages.config` model while making one root manifest authoritative. Every `HintPath`, analyzer and package import must resolve through `..\..\packages`, match an exact package identifier and version in the root manifest, and exist after restore.

The project reference is also aligned from the obsolete `NosTale.Configuration` path to `NosGm.Configuration`.

The project remains outside `NosGm.sln`; the focused workflow restores and builds it directly before any later decision about reintroducing it into the main solution.
