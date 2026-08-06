# Legacy solution module boundary

`NosGm.sln` contains two experimental ASP.NET Core module projects:

- `Data/NosGm.Program/NosTale.Modules/NosTale.Modules.csproj`
- `Data/NosGm.Program/NosTale.Module.Bazaar/NosTale.Module.Bazaar.csproj`

Both target `net8.0`, but their project graph still reaches libraries that expose only .NET Framework 4.8.1 or targets that are not consumable from .NET 8. The Windows server build sets `NosGmLegacyBuild=true`, so restoring or compiling those modules as part of the same solution graph is structurally invalid.

The source solution remains unchanged and continues to describe all repository projects. `scripts/prepare-legacy-solution.ps1` creates `artifacts/NosGm.Legacy.generated.sln`, a temporary copy inside the already ignored diagnostics directory. Because the generated solution lives one directory below the repository root, the script rebases every C# project path before MSBuild consumes it.

The generated copy removes exactly the two incompatible module projects and all of their solution configuration metadata while preserving Login, Master, Parser, World and every other legacy project.

The script refuses to generate the filtered solution when:

- either module disappears or stops targeting `net8.0`;
- a module no longer has an incompatible project reference;
- a referenced project is missing;
- a required server executable project would be removed;
- more or fewer than the expected two projects leave the graph;
- a source solution project uses an absolute path;
- the rebased paths no longer contain the required server projects.

This boundary is temporary, not a deletion. The modules can return to a modern build after their complete dependency chain exposes a target consumable by the chosen runtime, preferably the repository's .NET 10 foundation. Reintegration work is tracked in issue #208.

Local generation from the repository root:

```powershell
.\scripts\prepare-legacy-solution.ps1
```

The generated solution is an ephemeral build input under `artifacts/`; it must not replace `NosGm.sln`.
