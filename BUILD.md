# Building Starquake

## Layout

The game's code and content live once, in the `Starquake.Shared` project. Each platform gets
a thin "head" project that imports `Starquake.Shared/Starquake.Shared.props`, chooses its
target framework and MonoGame flavour, and adds whatever packaging that platform needs.

| Project | Platform | Target framework | MonoGame flavour |
| --- | --- | --- | --- |
| `Starquake.Shared` | all (the game itself, plus `Content/` and `Data/`) | `net9.0`, `net9.0-windows` | both, by target framework |
| `Starquake.Linux` | Linux | `net9.0` | DesktopGL |
| `Starquake.Mac` | macOS | `net9.0` | DesktopGL |
| `Starquake.Windows` | Windows | `net9.0-windows` | WindowsDX |
| `ProjectMercury` | all (particle engine) | `net8.0`, `net8.0-windows` | both, by target framework |
| `ProjectMercury.ContentPipeline` | build-time only (MGCB extension) | `net8.0` | DesktopGL |
| `SharedLibs` | all (screen data structures) | `net9.0` | — |
| `ScreenEditor` | all (Avalonia tool, not part of the game) | `net9.0` | — |

`Starquake.Shared` and `ProjectMercury` both multi-target a plain and a `-windows` framework.
That is what lets one set of sources serve every platform: MonoGame ships a different flavour
of `MonoGame.Framework` per platform and they all share an assembly identity, so two flavours
cannot meet in one output folder. The plain target builds against DesktopGL for the Linux and
macOS heads, the `-windows` one against WindowsDX for the Windows head.

Each head builds to an assembly named `Starquake`, so `Starquake.Shared.dll` plus a small
`Starquake.dll` entry point is the normal shape of the output.

`SharedLibs` has no MonoGame dependency, so there is nothing to multi-target for and a single
`net9.0` build serves the game and the editor alike.

## Building

```sh
dotnet build Starquake.sln                              # everything
dotnet build Starquake.Mac/Starquake.Mac.csproj         # macOS
dotnet build Starquake.Linux/Starquake.Linux.csproj     # Linux
dotnet build Starquake.Windows/Starquake.Windows.csproj # Windows
```

Every project builds on any of the three platforms — the Windows head sets
`EnableWindowsTargeting`, so it compiles on Linux and macOS even though it only runs on
Windows.

The macOS build also produces `Starquake.Mac/bin/<Config>/net9.0/Starquake.app`, ad-hoc
signed and ready to launch from Finder. The plain executable is written alongside it.

Everything builds and runs natively on Apple Silicon — the game, and MGCB too. That is the
main reason this is on MonoGame 3.8.4: 3.8.2's content builder shipped x86_64-only macOS
natives (FreeImage, freetype) and could only be built by routing MGCB through an x64 .NET
host under Rosetta.

## The content build and Project Mercury

`Content.mgcb` is built by `Starquake.Shared` itself, not by the heads. That is deliberate:
MGCB keys its output folder off the `.mgcb` file's own location and the MonoGame platform, so
two heads on the same platform (Linux and macOS are both DesktopGL) would race each other over
the same `.xnb` files if they each built the content. The built content reaches each head's
output through the project reference. Adding a `MonoGameContentReference` to a head brings
that race back.

The `ParticleEffects/*.xml` assets are compiled by an MGCB extension,
`ProjectMercury.ContentPipeline`, which drags `ProjectMercury` in with it so MGCB can resolve
`ProjectMercury.ParticleEffect`. **MGCB is a `net8.0` application**, and a `net9.0` assembly
cannot be loaded into it — that is why those two projects target `net8.0` while the game
targets `net9.0`. A `net9.0` game referencing a `net8.0` library is fine; the reverse is not.
Retargeting either of them to `net9.0` produces a build that still "succeeds" while every
particle effect silently fails to compile.

`ProjectMercury.ContentPipeline` writes to a fixed `bin/mgcb/` folder rather than the usual
`bin/<Config>/<TFM>/`, because the `/reference:` lines in `Content.mgcb` have to name a
literal path — MGCB response files cannot interpolate `$(Configuration)`. Hardcoding one
configuration there is how those references rotted last time, leaving the effects unbuilt.

`Starquake.Shared` builds that extension itself, in the `BuildContentPipelineExtension`
target, because MGCB runs before project references are resolved. The extension is also
listed as a `ProjectReference` with `ReferenceOutputAssembly="false"` so that
`dotnet build` on a head restores it; none of its assets reach the game's output.

## Writing platform-specific code

Don't, if you can avoid it. When you can't, `Starquake.Shared` defines `WINDOWS` on its
`net9.0-windows` target only, and that is the single supported escape hatch:

```csharp
#if WINDOWS
    // WinForms and anything else Windows-only goes here.
#endif
```

Nothing uses it today. The one candidate is the commented-out F11 handler in
`Starquake.Shared/Engines/GameEngine.cs`, which resized the window back to its default
through `System.Windows.Forms.Form.FromHandle`. Reviving it also means adding
`<UseWindowsForms>true</UseWindowsForms>` to the `net9.0-windows` target of
`Starquake.Shared.csproj` and to `Starquake.Windows.csproj`.

## Adding a head

Create `Starquake.<Platform>/Starquake.<Platform>.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\Starquake.Shared\Starquake.Shared.props" />
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RollForward>Major</RollForward>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.4.1" />
  </ItemGroup>
</Project>
```

The props file supplies the reference to `Starquake.Shared` and the shared `Program.cs`. A
head only picks its framework and flavour and adds its own packaging.

If a head ever needs a target framework other than the two `Starquake.Shared` builds, add it
to `<TargetFrameworks>` in `Starquake.Shared.csproj` first.

## Gotchas

**Working directory.** The screen data in `Data/*.dat` is read with a plain `FileStream` on a
relative path, and MonoGame resolves content relative to the process location. `Program.cs`
pins the working directory to `AppContext.BaseDirectory` for exactly this reason — launched
from Finder, a `.app` bundle otherwise starts at `/`.

**The .app bundle layout is not arbitrary.** Inside a bundle MonoGame resolves content
against `Contents/Resources`, so the built content is copied there, while the binaries and
`Data/` go in `Contents/MacOS` next to the executable. Putting content beside the binaries
builds a bundle that fails on the first texture load. The bundle is ad-hoc signed because
unsigned bundles are killed on launch on Apple Silicon.

**Content asset names use backslashes** (`"Graphics\\BootScreen2"`). MonoGame's
`TitleContainer` normalises those to the platform separator, so they work on Linux and macOS
as-is; there is no need to convert them.
