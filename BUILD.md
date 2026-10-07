# Building Starquake

## Layout

The game's code and content live once, in the `Starquake.Shared` project. Each platform gets
a thin "head" project that imports `Starquake.Shared/Starquake.Shared.props`, chooses its
target framework and MonoGame flavour, and adds whatever packaging that platform needs.

| Project | Platform | Target framework | MonoGame flavour |
| --- | --- | --- | --- |
| `Starquake.Shared` | all (the game itself, plus `Content/` and `Data/`) | `net10.0`, `net10.0-windows` | both, by target framework |
| `Starquake.Linux` | Linux | `net10.0` | DesktopGL |
| `Starquake.Mac` | macOS | `net10.0` | DesktopGL |
| `Starquake.Windows` | Windows | `net10.0-windows` | WindowsDX |
| `ProjectMercury` | all (particle engine) | `net8.0`, `net8.0-windows` | both, by target framework |
| `ProjectMercury.ContentPipeline` | build-time only (MGCB extension) | `net8.0` | DesktopGL |
| `SharedLibs` | all (screen data structures and world constants) | `net10.0` | — |
| `ScreenEditor` | all (Avalonia tool, not part of the game) | `net10.0` | — |

`Starquake.Shared` and `ProjectMercury` both multi-target a plain and a `-windows` framework.
That is what lets one set of sources serve every platform: MonoGame ships a different flavour
of `MonoGame.Framework` per platform and they all share an assembly identity, so two flavours
cannot meet in one output folder. The plain target builds against DesktopGL for the Linux and
macOS heads, the `-windows` one against WindowsDX for the Windows head.

Each head builds to an assembly named `Starquake`, so `Starquake.Shared.dll` plus a small
`Starquake.dll` entry point is the normal shape of the output.

`SharedLibs` has no MonoGame dependency, so there is nothing to multi-target for and a single
`net10.0` build serves the game and the editor alike. As well as the screen data structures it
holds the constants that describe the shape of the world — `TileMetrics`, `WorldMap`,
`TunnelTiles`, `ActionTiles`, `MarkerTiles`, `LandingSite` and `TeleportCodes`. The editor has
to agree with the game about every one of those, so they live in one place rather than being
written down twice; the ones that moved there kept their `Starquake.Constants` and
`Starquake.Structure` namespaces on the way, so the game's own sources did not change.

`ScreenEditor` is an Avalonia app and nothing in the game references it. It is on Avalonia
11.3 rather than the 11.2 it was generated against, because `Material.Icons.Avalonia` — the
toolbar's icons — needs 11.3.

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

The macOS build also produces `Starquake.Mac/bin/<Config>/net10.0/Starquake.app`, ad-hoc
signed and ready to launch from Finder. The plain executable is written alongside it.

Everything builds and runs natively on Apple Silicon — the game, and MGCB too. That is the
main reason this is on MonoGame 3.8.4: 3.8.2's content builder shipped x86_64-only macOS
natives (FreeImage, freetype) and could only be built by routing MGCB through an x64 .NET
host under Rosetta.

## Publishing ahead of time

A NativeAOT publish compiles the game to machine code at build time and ships no .NET runtime
at all: one native executable, no JIT, no `Starquake.dll`, nothing to install on the machine it
runs on. There is one profile per platform, under each head's `Properties/PublishProfiles/`:

```sh
dotnet publish Starquake.Mac/Starquake.Mac.csproj -p:PublishProfile=Aot-osx-arm64   # or Aot-osx-x64
dotnet publish Starquake.Linux/Starquake.Linux.csproj -p:PublishProfile=Aot-linux-x64
dotnet publish Starquake.Windows\Starquake.Windows.csproj -p:PublishProfile=Aot-win-x64
```

Each writes to `<head>/bin/Publish/<rid>/`. The macOS ones also build `Starquake.app` around the
published output — `CreateAotAppBundle`, a second copy of the bundling target, because MSBuild
runs a target once per project instance and `dotnet publish` runs Build and Publish in the same
one, so one target cannot see both output folders.

**It is `publish`, not `build`.** `PublishAot` does nothing on `dotnet build`, which still
produces ordinary IL assemblies — there is no AOT build configuration to select in an IDE, and
nothing about a normal build changes. It is also not ReadyToRun: `PublishReadyToRun` is false
here and stays false. These are different things, and NativeAOT is the one with no runtime.

**It does not cross-compile.** ILC emits code for the machine it is told to target and then links
it with the platform's own linker, so each profile has to be run on the platform it names: macOS
needs the Xcode command line tools, Linux needs `clang` and the zlib headers, Windows needs the
Visual Studio C++ build tools. Build for another OS and it stops before it starts, with
`Cross-OS native compilation is not supported`. Windows is doubly so — that head's content is
built by MGCB's WindowsDX platform, which already needs a Windows machine for the shader compiler.

Another *architecture* on the same OS is a softer no: ILC will happily emit the code, and then the
link fails, because the system linker only knows its own machine — `unrecognised emulation mode:
aarch64linux` from an x86_64 box. That one is solvable by installing a cross-toolchain, where the
cross-OS one is not. The simple reading is the right one: build each on its own machine.

### What ships

**Not a single file.** The executable holds all the managed code, and that is all it holds:

| | macOS | Linux |
| --- | --- | --- |
| The game | `Starquake`, 9.9MB | `Starquake`, 10.3MB |
| Native libraries | `libSDL2-2.0.0.dylib`, `libopenal.dylib` | `libSDL2-2.0.so.0`, `libopenal.so` |
| `Content/` | 107MB of `.xnb` | same |
| `Data/SQScreenData.dat` | 892KB, the map | same |

So about 120MB in a folder, of which the executable is under a tenth. SDL and OpenAL are loaded by
name at runtime rather than linked in, so they travel as files; the content is read off disk by
`ContentManager` and a `FileStream`, so it travels too. The win is startup and having no runtime
to install, not a smaller folder. The Windows head carries no native libraries at all - WindowsDX
talks to DirectX, which is already on the machine - but see the caveat below.

**MonoGame ships no SDL for `linux-arm64`.** `MonoGame.Library.SDL` carries natives for
`linux-x64`, `osx` and `win-x64` only, so an arm64 Linux build links fine and comes out with
`libopenal.so` and no `libSDL2`. `Aot-linux-arm64.pubxml` is there and it builds, but that
machine has to supply SDL2 itself. This is not an AOT problem — a normal arm64 Linux build is
short the same file.

### Windows is wired up and unproven

The `Aot-win-x64` profile is correct configuration and it has never been run, because NativeAOT
cannot cross-compile and this was set up on a Mac. Expect to find out on the first attempt, and
expect it to be the one platform that needs more than the profile.

The reason to doubt it is `MonoGame.Framework.WindowsDX`, which carries a framework reference to
`Microsoft.WindowsDesktop.App.WindowsForms` and puts its game window up through
`WinFormsGameWindow`. **Windows Forms is not supported under NativeAOT** - it is not trimmable,
and the SDK does not even offer the AOT option to a project that sets `UseWindowsForms`. Nothing
blocks this head, because the reference arrives transitively rather than through that property,
so ILC will try; whether what comes out runs is the open question. WindowsDX also brings SharpDX,
which is .NET Framework-era interop and has had no trimming work done to it.

If it does not come out working, the way round is a Windows head on **DesktopGL** rather than
WindowsDX - SDL and OpenGL instead of WinForms and DirectX, the same flavour the Linux and macOS
heads already use, and `MonoGame.Library.SDL` ships a `win-x64` native for exactly that. It is a
new head (see "Adding a head"), not a change to this one, because the MonoGame flavour is chosen
by the head's package reference and the two flavours cannot share an output folder.

### What had to be told to the compiler

A JIT works out at runtime what ILC has to know at build time, and two libraries here ask for
types by name. Four things answer it, and all four are in the repo already:

- **`Starquake.Shared/ContentManagement/NativeAot.cs`** names every content reader that is
  generic over a type ILC never meets in source. That is the particle effects and only those:
  Mercury's `.xnb` files are written by `IntermediateSerializer`, so each asks for a
  `ReflectiveReader<SomeMercuryType>`, and a generic instantiation nothing references cannot be
  in the binary. `GameHost`'s constructor calls it, on every build — on a JIT build it registers
  the same readers reflection would have found and changes nothing.
- **`SharedLibs/ScreenDataSerializer.cs`** does the same job for the screen data, naming the
  three formatters MessagePack would otherwise work out from the type at runtime. The game, the
  editor and the tests all pass its `Options`, so all three agree on the format by construction
  rather than by each trusting a process-wide default.
- **`Starquake.Shared.props` roots `MonoGame.Framework` and `ProjectMercury`** against the
  trimmer, which would otherwise delete the readers MonoGame looks up by string and the emitter
  properties `ReflectiveReader` reads reflectively. Rooting MonoGame costs about 4MB.
- **`WarningsNotAsErrors` lets `IL2104` and `IL3053` through.** Those are the roll-ups saying a
  dependency produced trim or AOT warnings; MonoGame and MessagePack both do, it is their own
  code, and `TreatWarningsAsErrors` would otherwise fail every publish. Warnings against files in
  this solution are untouched and still fail the build.

Neither the rooting nor the warning list is conditioned on `PublishAot`, and that is deliberate:
a publish profile is not read until the SDK's targets run, long after this file is imported, so
the condition would be false on exactly the publish that needs it. Both are inert otherwise.

**The symptom when a content reader is missing** is a `NotSupportedException` at the first
`Content.Load`, naming the type it could not build — ``'...ReflectiveReader`1[ProjectMercury.X]'
is missing native code or metadata``. Add that type to `NativeAot.cs`. Nothing warns at build
time, because nothing at build time knows what the `.xnb` files are going to ask for.

**A missing formatter says so in both builds**, which is the reason the screen data's resolver is
spelled out rather than left to MessagePack's default. `GetFormatterWithVerify` throws naming the
type it could not resolve, and it throws the same way under the JIT — so a type added to
`GameScreenData` that needs a formatter nobody named fails in front of whoever added it, instead
of working all through development and breaking on the first AOT publish.

### Those four warnings cannot be cleared

Not by suppressing the right thing, not by updating the libraries, and not by passing explicit
serialiser options — all three were tried. Expanded, the four roll-ups are 103 individual
warnings: 90 in MessagePack's Reflection.Emit formatter machinery, 13 in MonoGame. Every one is
either a path already answered above, a path the game cannot reach (`ReloadGraphicsAssets`, which
`GameHost.LoadContent` deliberately throws on; `MultiArrayReader`, which no asset uses), or
design-time only (`Design.VectorConversion`, the `TypeConverter`s a WinForms designer wants).

What does not work, with the measurements:

- **Newer libraries.** MessagePack 3.1.11 still ships `DynamicObjectTypeBuilder`, `DynamicAssembly`
  and `System.Reflection.Emit`; MonoGame 3.8.5.1 still has `ContentTypeReaderManager` and
  `ReflectiveReader`. Resolving a reader from a string in the `.xnb` *is* the XNA content format.
  These are structural, not stale.
- **Explicit serialiser options.** `MessagePackSerializer.Deserialize` has `options ??=
  DefaultOptions` in its own body, and `DefaultOptions` reaches `StandardResolver` and every
  dynamic resolver under it. Passing options changes which branch runs, not what is reachable, and
  the trimmer reasons about reachability: 103 warnings before, 103 after, binary 16 bytes larger.
  `ScreenDataSerializer` is kept for the other two reasons, not this one.

Leave them visible rather than adding them to `NoWarn`. They are the signal that a dependency's
reflection surface has changed, which is exactly when this page needs rereading. A normal
`dotnet build` is still 0 warnings; these appear only on an AOT publish.

## Where the art lives

`Starquake.Shared/Content/` holds it, and `Content.mgcb` compiles it. There is one copy and
one place to edit.

There used to be two. `Assets/` sat alongside it as a second copy of nearly the same files -
199 of them, called `Temp/` until a folder of that name proved to be one confident `rm -rf`
away from gone. Nothing read it: every one of `Content.mgcb`'s entries resolves inside
`Content/`, and a clone with `Assets/` deleted outright still builds. Keeping the two in step
was manual, so they drifted, and because the stale copy was the one nothing built, a stale
copy looked exactly like a current one. The options screen's credit line is what it cost -
the painted artwork was cleared in `Content/` and left alone in `Assets/`, and only the live
copy was right.

Of its 199 files, 170 were byte-identical to their `Content/` twin - Git stores one blob per
pair, so those were free and deleting them cost nothing either. The other 29 were set aside
for review before the folder went. None is loaded by the game: the 10 with no twin are all
superseded, most of them documented as such in `Assets.cs` - the bitmap font atlases the
`.spritefont` descriptions replaced, the `BootScreen.png` that `BootScreen2.png` supersedes,
the `Scanlines` pair that `Shaders/Crt.fx` now computes, the old `StarFont.png`, an `Icon.png`
no head consumes, and three `.fx` files the pipeline has never compiled.

The layered originals were never in `Assets/` anyway. They are in
`Starquake.Shared/Creation/Graphics/`, as `.psd` and `.rdw`.

### Shaders need Wine on macOS and Linux

MonoGame has no shader compiler of its own. On Windows `EffectProcessor` uses the DirectX HLSL
compiler that is already on the machine; on macOS and Linux it runs that same Windows compiler
under Wine, and wants `MGFXC_WINE_PATH` pointing at a prefix MonoGame builds for the purpose.
Without one the content build fails outright - not just that asset - with

```
System.PlatformNotSupportedException: Wine is not installed on this system.
   at MonoGame.Effect.Compiler.WineHelper..cctor()
```

**Homebrew is not the way in.** All three `wine` casks were disabled on 2026-09-01 for failing
Homebrew's Gatekeeper audit, so `brew install --cask wine-stable` refuses. MonoGame's own macOS
setup page sidesteps Homebrew entirely and fetches the same Gcenx build directly, which is what
works:

```sh
curl -L -o wine.tar.xz \
  "https://github.com/Gcenx/macOS_Wine_builds/releases/download/11.0_1/wine-stable-11.0_1-osx64.tar.xz"
tar -xJf wine.tar.xz
mv "Wine Stable.app" ~/Applications/          # /Applications and sudo are not needed
export PATH="$HOME/Applications/Wine Stable.app/Contents/Resources/wine/bin:$PATH"
curl -sL https://monogame.net/downloads/net9_mgfxc_wine_setup.sh | bash
```

Four notes on that, each of which cost something to find:

- **Use the `net9_` script, not the one on MonoGame's `develop` branch.** The older one tests for
  a `wine64` binary, and Wine 11 ships only `wine`; it exits at line 6. The `net9_` script falls
  back to `wine` and symlinks a wrapper.
- **The quarantine flag never appears.** MonoGame's docs run `xattr -dr com.apple.quarantine`,
  and it is harmless, but a file fetched with `curl` is marked `com.apple.provenance` instead and
  was never blocked.
- **MonoGame's step 7 is written with `>` rather than `>>`**, which truncates `~/.zprofile`
  instead of adding to it. Append.
- It wants `7z` (`brew install p7zip`) and pulls down about 450MB: Wine, a Windows .NET SDK for
  the prefix, and a Firefox 62 installer that `d3dcompiler_47.dll` is extracted out of.

### Shaders are built for DesktopGL only on a Mac

Wine covers the DesktopGL content build and does **not** cover the WindowsDX one, which fails
with an unexplained `Unexpected error compiling`. So `Content.mgcb` is referenced conditionally:
the `net10.0-windows` target framework builds content only on Windows.

That target framework still compiles everywhere - `EnableWindowsTargeting` sees to that - it
just ships no content off a Mac, which costs nothing because the Windows head cannot run there.
**Build the Windows head's content on Windows.** There it needs no Wine at all.

Sixteen shaders are built, all of them `ps_2_0` because the profile is `Reach` and allows no higher:

| Shader | What it does |
| --- | --- |
| `Desaturate.fx` | Drains the colour and light for the tunnel transition |
| `Crt.fx` | The cathode ray tube F9 steps through - scanlines, a phosphor grille, a glow, a curve |
| `Materialise.fx` | A nasty arriving: two lines opening out of it, and the colour channels converging |
| `Splat.fx` | An organic nasty coming apart along a wandering boundary, with a wet rim |
| `Electric.fx` | The node and gate arcs: a white core on the filaments, a halo, a flicker |
| `Overload.fx` | Blob's energy failing: drained by a nasty, the charge building, the cracking, the blast |
| `CoreFunnel.fx` | The funnel of energy turning about the planet's core |
| `TeleportArrival.fx` | The room settling after Blob arrives somewhere by teleport |
| `CoreSurge.fx` | The room wrung round a slot as the planet's core takes a part |
| `Laser.fx` | Blob's shot: a beam with a ball of plasma at each end, drawn out of no artwork |
| `Platform.fx` | A jump platform dissolving, with every hole the dissolve opens burning at its edge |
| `Water.fx` | Olly's screen flooded: everything below the waterline read through moving water |
| `Halo.fx` | The glow under a shuttle tag's lights, drawn from no texture at all |
| `ForceField.fx` | The sheet of light across a key-locked door, drawn back onto its jambs as it opens |
| `Malfunction.fx` | The flight computer's display failing - bands thrown sideways, the colour channels split, lines losing their signal, the vertical hold going |
| `Shockwave.fx` | The ring a blast throws out - a hot front, the ripples behind it, and two waves running round its rim |

**`ps_2_0` has no dynamic branching and 64 arithmetic slots**, and that is a real ceiling rather
than a theoretical one. It is why `Crt.fx` is three techniques rather than one shader with
branches - each mode compiles as only the work it needs - and the whole-tube pass is the one with
nothing to spare: eight glow taps instead of four compile to 71 slots and will not build. Hoisting
the shared terms does not help, because the compiler has already found them.

`Overload.fx`'s cracking pass hit the same wall at 85 slots, with two crossing sines and a rim on
it, and came back under by dropping to one sine across a slope and no rim. `CoreFunnel.fx` hit it
at 76, and `Shockwave.fx` at 71 the moment the waves round its rim went in - and again at 65 when
those waves were made finer, which needed one more doubling. It sits at the ceiling now, so
anything added to it has to buy its slots from somewhere. Each technique gets the
budget to itself, which is the other reason to split a shader rather than branch inside one. The
remaining shaders are small and nowhere near it.

**Three things buy slots back cheaply**, the first two learnt on `CoreFunnel.fx` and the third on
`Shockwave.fx`:

- **`pow` is not a multiply.** It compiles to a logarithm, a multiply and an exponent. `x * x * x`
  is nearly free where `pow(x, 3.0)` is not, and three of them were most of that shader's overrun.
- **Divide on the processor, not per pixel.** A span that never changes between frames can have
  its reciprocal worked out once and passed in as a uniform, turning a per-pixel divide into a
  per-pixel multiply. An amplitude a uniform is multiplied by can be folded into that uniform for
  the same reason, and so can a rectangle a coordinate is shifted and scaled by - one multiply and
  add in the shader instead of a subtract and a divide.
- **A cosine costs about ten slots.** Where the wave only has to look periodic rather than be a
  true sinusoid, `frac` and `abs` make a triangle in four, and squaring it once or twice sharpens
  the crest further for a slot apiece. That one swap is most of what brought `Shockwave.fx` back
  under; `rsqrt` serving as both a length and a direction is cheap for the same
  reason - one reciprocal square root instead of a `sqrt` and a divide.

`disappear.fx`, `drawaswhite.fx` and `refraction.fx` were never under `Content/Shaders/` and
nothing referenced them; they went with `Assets/`.

If the toolchain ever becomes a liability - Rosetta is on notice, and this Wine is an x86_64
binary running a x86_64 Windows compiler - MGCB's `/copy:` directive takes a prebuilt `.xnb`
verbatim. Compiling the shader once and committing the result would drop the dependency
entirely, at the cost of one `.xnb` per platform.

## CI

`.github/workflows/build.yml` restores, builds and tests the solution on Ubuntu, macOS and
Windows on every push to `main` and every pull request. `Directory.Build.props` sets
`TreatWarningsAsErrors`, so a warning fails it.

Every project builds on every platform, so one runner would prove the code compiles. There
are three because the packaging does not travel: the `.app` bundle and its ad-hoc signature
are a macOS-only target, and only a run on macOS builds them.

## The content build and Project Mercury

`Content.mgcb` is built by `Starquake.Shared` itself, not by the heads. That is deliberate:
MGCB keys its output folder off the `.mgcb` file's own location and the MonoGame platform, so
two heads on the same platform (Linux and macOS are both DesktopGL) would race each other over
the same `.xnb` files if they each built the content. The built content reaches each head's
output through the project reference. Adding a `MonoGameContentReference` to a head brings
that race back.

The `ParticleEffects/*.xml` assets are compiled by an MGCB extension,
`ProjectMercury.ContentPipeline`, which drags `ProjectMercury` in with it so MGCB can resolve
`ProjectMercury.ParticleEffect`. **MGCB is a `net8.0` application**, and a `net10.0` assembly
cannot be loaded into it — that is why those two projects target `net8.0` while the game
targets `net10.0`. A `net10.0` game referencing a `net8.0` library is fine; the reverse is not.
Retargeting either of them to `net10.0` produces a build that still "succeeds" while every
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
`net10.0-windows` target only, and that is the single supported escape hatch:

```csharp
#if WINDOWS
    // WinForms and anything else Windows-only goes here.
#endif
```

Nothing uses it today. The one candidate is the commented-out F11 handler in
`Starquake.Shared/Engines/GameEngine.cs`, which resized the window back to its default
through `System.Windows.Forms.Form.FromHandle`. Reviving it also means adding
`<UseWindowsForms>true</UseWindowsForms>` to the `net10.0-windows` target of
`Starquake.Shared.csproj` and to `Starquake.Windows.csproj`.

## Adding a head

Create `Starquake.<Platform>/Starquake.<Platform>.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\Starquake.Shared\Starquake.Shared.props" />
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
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

**An incremental build sometimes deletes the built content.** If the game starts failing with
`ContentLoadException ... BootScreen2.xnb`, the `Content` folder under a head's output is empty
and nothing you changed caused it: MSBuild's `IncrementalClean` removed it during a parallel
build - the copy into `bin/` is skipped and last time's copies are then swept away. Build again,
or build with `-m:1`, which does not do it in six runs out of six. See PLAN.md 6.6 for the
measurements and the candidate fixes, and note that `DisableIncrementalClean` has been tried and
makes it worse.

**`NETSDK1005 ... doesn't have a target for 'net8.0'`** means something restored with
`TargetFramework` as a *global* property. It is a two-step failure and neither step names the
other. `ProjectMercury` multi-targets `net8.0` and `net8.0-windows`; a global `TargetFramework`
overrides that, so restore writes it an assets file holding only the one framework, and then
`BuildContentPipelineExtension` asks for `net8.0` - which it is right to do - and finds nothing.
The publish is blamed for a file it never touched.

`dotnet restore` on its own repairs it, because without the property the project evaluates its own
`TargetFrameworks` again. To stop it happening, do not put `<TargetFramework>` in a publish
profile: an IDE publishes a profile by passing its properties on the command line, which makes
every one of them global. None of the `Aot-*.pubxml` files names a target framework, and the
comment at the top of each says why. Passing `-p:TargetFramework=` by hand does the same thing.

**Content asset names use backslashes** (`"Graphics\\BootScreen2"`). MonoGame's
`TitleContainer` normalises those to the platform separator, so they work on Linux and macOS
as-is; there is no need to convert them.
