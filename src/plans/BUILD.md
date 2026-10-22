# Building Starquake

## Layout

The game's code and content live once, in the `Starquake.Shared` project. Each platform gets
a thin "head" project that imports `Starquake.Shared/Starquake.Shared.props`, chooses its
target framework and MonoGame runtime, and adds whatever packaging that platform needs.

| Project | Platform | Target framework | MonoGame packages |
| --- | --- | --- | --- |
| `Starquake.Shared` | all (the game itself, plus `Content/` and `Data/`) | `net10.0` | Native |
| `Starquake.Linux` | Linux | `net10.0` | Native + `Runtime.Linux.Vulkan` |
| `Starquake.Mac` | macOS | `net10.0` | Native + `Runtime.Mac.Vulkan` |
| `Starquake.Windows` | Windows | `net10.0` | Native + `Runtime.Windows.Vulkan` |
| `ProjectMercury` | all (particle engine) | `net8.0` | Native |
| `ProjectMercury.ContentPipeline` | build-time only (MGCB extension) | `net8.0` | Native |
| `SharedLibs` | all (screen data structures and world constants) | `net10.0` | — |
| `ScreenEditor` | all (Avalonia tool, not part of the game) | `net10.0` | — |

**One MonoGame platform, one target framework, every platform.** Every head renders through
Vulkan on MonoGame's native backend. `MonoGame.Framework.Native` is the managed framework, the
same assembly everywhere, and each head adds the one runtime package for its operating system.
That package holds a single native library with SDL built in, and MoltenVK as well on macOS.
The content platform is `DesktopVK`, so one set of `.xnb` serves all three heads. See "Every
head is Vulkan" below for why.

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

Every project builds on any of the three platforms, and builds its content there too, with
nothing installed beyond the .NET SDKs.

The macOS build also produces `Starquake.Mac/bin/<Config>/net10.0/Starquake.app`, ad-hoc
signed and ready to launch from Finder. The plain executable is written alongside it.

Everything builds and runs natively on Apple Silicon — the game, and MGCB too. That is the
reason this is on MonoGame 3.8.4 or later: 3.8.2's content builder shipped x86_64-only macOS
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

**It does not cross-compile between operating systems, and the reason is the linker, not ILC.**
This page used to say a cross-OS attempt stops before it starts with `Cross-OS native compilation
is not supported`. It does not. Told to target `linux-x64` from a Mac, ILC compiles the whole
program and writes a perfectly good 48MB `ELF 64-bit LSB relocatable, x86-64` object file; what
fails is the step after it, because macOS clang has no linker that targets Linux:

```
clang : error : invalid linker name in argument '-fuse-ld=bfd'
```

So each profile wants the platform's own toolchain: macOS the Xcode command line tools, Linux
`clang` plus the zlib headers plus an `objcopy` or `llvm-objcopy` for the symbol stripping,
Windows the Visual Studio C++ build tools. Miss the stripping tool and the publish dies at the
very last step, after every bit of the compiling is done.

Being a link failure rather than a refusal makes it the same *kind* of problem as
cross-architecture, and solvable the same way — a cross-linker and a sysroot — rather than a
wall. Not worth doing here: a runner on the right OS is free (see "CI"), and Docker runs Linux
containers on a Mac.

**Another architecture on the same OS varies by OS.** On Linux the link fails for the same reason
in miniature, the system linker knowing only its own machine: `unrecognised emulation mode:
aarch64linux` from an x86_64 box. **On macOS it simply works** — Xcode's clang carries both
targets, so `Aot-osx-x64` builds on Apple Silicon and comes out `Mach-O 64-bit executable
x86_64`, 10.4MB. Measured, not assumed. One Mac, and one macOS runner, covers both Mac targets.

### What ships

**Not a single file.** The executable holds all the managed code, and that is all it holds:

| | macOS | Linux | Windows |
| --- | --- | --- | --- |
| The game | `Starquake`, 8.9MB | `Starquake` | `Starquake.exe` |
| Native library | `libmgruntime.dylib`, 23MB (arm64 and x86_64 in one) | `libmgruntime.so`, 4.5MB | `mgruntime.dll`, 2.7MB |
| `Content/` | 107MB of `.xnb` | same | same |
| `Data/SQScreenData.dat` | 892KB, the map | same | same |

So about 120MB to 150MB in a folder, of which the executable is under a tenth. The native library
is MonoGame's backend, with SDL and audio inside it, and is loaded by name at runtime rather than
linked in, so it travels as a file. The content is read off disk by `ContentManager` and a
`FileStream`, so it travels too. The win is startup and having no runtime to install, not a
smaller folder.

**The graphics driver is the one thing the player supplies.** The game needs Vulkan. Every
current Windows driver from AMD, Intel and NVIDIA has it, and `vulkan-1.dll` comes with the
driver. On Linux that is Mesa or the vendor's driver: the runtime opens `libvulkan.so.1` and
X11 or Wayland itself, and links against nothing but libc. macOS needs nothing, because MoltenVK
is inside `libmgruntime.dylib`. A machine with no Vulkan driver at all - Intel graphics older
than about 2015, Windows' basic display driver, some virtual machines - cannot start the game.

**Self-contained is not single-file**, on any of the three. `PublishSingleFile` is one of only three
things the SDK refuses outright alongside `PublishAot`, and even if it did not, `Content/` is read
off disk and the native library is opened by name at runtime - neither can live inside the executable.
What travels is a folder, or on macOS the `.app` that wraps one. One file per platform means an
archive: see "Distributing a build".

### Every head is Vulkan

The heads used to be DesktopGL, which is SDL and OpenGL, and on Windows that was the problem. On
a real Windows PC the game ran noticeably worse than the same build on macOS or Linux. OpenGL on
Windows is only as good as the vendor's OpenGL driver, and vendors spend their effort on
Direct3D and Vulkan. Vulkan goes straight to a driver that is maintained, and keeps one renderer
for all three platforms. On macOS it runs through MoltenVK onto Metal, in place of Apple's
OpenGL, which has been frozen since 2018.

MonoGame 3.8.5 introduced the native backend: one C/C++ library per platform in place of the
third-party C# wrappers DesktopGL used. Two things fell out of the move:

- **`MonoGame.Framework.Native` does not set `MonoGamePlatform`**, and the content build stops
  with "The MonoGamePlatform property was not defined" without it, so `Starquake.Shared` sets
  `DesktopVK` itself.
- **The shaders had to move to Shader Model 6.** Vulkan shaders are compiled by DXC, which
  refuses anything below `ps_6_0` and does not accept the DX9 forms. Every effect now declares a
  `Texture2D` and a `SamplerState`, calls `Sample` where it called `tex2D`, and returns
  `SV_Target`. The maths did not change. See "Shaders" below.

The Windows head stays `net10.0` rather than `net10.0-windows`, and keeps its `app.manifest` and
`ApplicationIcon`. Every head embeds an `Icon.bmp`: MonoGame's `NativeGameWindow` looks for that
resource in the entry assembly and falls back to its own logo without it. `ApplicationIcon` is
what Explorer draws on `Starquake.exe`, which is a different thing from the icon on the window.

WindowsDX12 was the other choice on Windows. It would have meant a second content build and a
second shader target for one head, and it is aimed at Microsoft's GDK. Vulkan is the one backend
that covers all three.

### What had to be told to the compiler

A JIT works out at runtime what ILC has to know at build time, and two libraries here ask for
types by name. Four things answer it, and all four are in the repo already:

- **`Starquake.Shared/ContentManagement/NativeAot.cs`** names every content reader that is
  generic over a type ILC never meets in source. That is the particle effects and only those:
  Mercury's `.xnb` files are written by `IntermediateSerializer`, so each asks for a
  `ReflectiveReader<SomeMercuryType>`, and a generic instantiation nothing references cannot be
  in the binary. `GameHost`'s constructor calls it, on every build — on a JIT build it registers
  the same readers reflection would have found and changes nothing. Its `Reflective<T>` carries
  the same `DynamicallyAccessedMembers` annotation that `ReflectiveReader<T>` declares on its own
  `T`; without it ILC reports `IL2091` and the publish fails.
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

## Versioning and releasing

**`<Version>` in `Directory.Build.props` is the only place the number is written down, and
bumping it is what publishes a release.** There is no tag to remember to push and no second
number to keep in step.

Before it existed there was no version at all. Three hardcoded `1.0.0`s - two keys in
`Info.plist`, one in `app.manifest` - plus the SDK's implicit `1.0.0` behind the assemblies,
none of them connected to any of the others, and no git tags. Now everything derives from the
one property:

| What | How it gets there |
| --- | --- |
| `AssemblyVersion`, `FileVersion`, `InformationalVersion` | The SDK, from `<Version>` |
| `CFBundleShortVersionString`, `CFBundleVersion` | Both bundle targets `sed` it into `Info.plist` |
| The installer's version | CI passes `/DAppVersion` to Inno Setup |
| Every asset filename | `Starquake-<version>-<rid>.<ext>` |

`app.manifest` is the one thing left out. Its `assemblyIdentity` version is a side-by-side
identity rather than a displayed version - what Explorer shows on the `.exe` is `FileVersion`,
which does come from `<Version>` - so there is nothing to gain by templating it.

### What a bump does

Push a commit to `main` that changes `<Version>` and CI releases it: the `version` job reads the
number, finds no `v<version>` tag, and the `release` job builds every platform, creates the tag
at that commit, and publishes a GitHub Release with the five archives attached.

Push anything else and nothing is released. The `version` job asks git whether the tag already
exists, which is what makes the whole thing idempotent - re-run the workflow, push a typo fix,
re-run it a year later, and as long as the number has not moved there is no second release.

### Trying it without publishing

A lower version number is **not** a dry run. `0.9.0` on `main` with no `v0.9.0` tag publishes a
real, public, "Latest" release exactly as `1.0.0` would; GitHub attaches no meaning to `0.x`.
There are two real answers instead.

**A dry run** builds all five platforms, packs every archive and publishes nothing. Actions tab →
Build → *Run workflow*, and leave **"Build everything and publish nothing"** ticked - it defaults
to on, so a manual run is safe unless it is deliberately made otherwise. It is the only way to
exercise the whole pipeline against `main` without releasing, and the artifacts are downloadable
from the run's summary page afterwards. A pull request does the same thing, because the version
job only arms on `main`.

**A pre-release** is for when it should be downloadable but not be the version everyone gets.
Any version with a hyphen in it - `1.0.0-rc1`, semver's own convention - is published as a real
Release with real assets, flagged as a pre-release and left out of "Latest". Dropping the suffix
later and bumping to `1.0.0` then releases the final one beside it.

| What you want | What to do | Result |
| --- | --- | --- |
| Prove the pipeline works | Run workflow, dry run ticked | Five builds, no Release |
| Something testers can download | `<Version>1.0.0-rc1</Version>` | Release, flagged pre-release |
| The actual thing | `<Version>1.0.0</Version>` | Release, marked Latest |

### Changing a version that is already published

**Nothing is overwritten, and nothing silently replaces a release.** The tag is the record:

- **Bump to a number that has never shipped** - the normal path. A new tag, a new Release, and
  every earlier one still sitting there with its own assets.
- **Push again without changing the number** - no release. The tag already exists, the `version`
  job says so in the log, and the build still runs and still uploads plain artifacts.
- **Move `<Version>` back to a number already released** - also no release, for the same reason.
  It does not rebuild or replace the old one.

### Deleting a release and making it again

Yes, and there is one catch: **deleting the Release does not delete the tag**, and the tag is what
the version job checks. Delete the Release alone and nothing will regenerate - the job keeps
reporting "already released" against a tag with nothing on the end of it. Delete both:

```sh
gh release delete v1.0.0 --cleanup-tag --yes
```

With the tag gone the version is new again, and either of these publishes it afresh:

- push anything to `main`, or
- Actions → Build → *Run workflow* on `main` with the dry run **unticked**.

It is a genuine rebuild, not a restore: every platform is compiled again and the new tag is
created at whatever `main` points to now, which may not be the commit the deleted release was
built from. That is usually what is wanted, and it is worth being clear that it is what happens.

`--cleanup-tag` is the whole trick. Without it the leftover tag is the most confusing state this
system has, because everything looks correct and nothing happens.

Git does not overwrite a published release under any of these. A tag can be force-moved by hand
(`git tag -f`, `git push --force`), and the Release that already exists keeps pointing at the
commit it was built from while the tag moves out from under it - which is why nothing in CI ever
does that.

### Which number to bump

Semantic versioning, and the game has no API, so read it as: patch for fixes, minor for new
content or features, major for something that changes how the game is played or breaks saved
high scores. The `.dmg`, `.AppImage` and `Setup.exe` all carry the number in their filename, so
whatever is chosen is what a player sees in their downloads folder.

## Distributing a build

An ahead-of-time publish is the thing to hand to someone else: no runtime to install and no
version to match. What `dotnet publish` leaves behind is not a single file, though — everything in
"What ships" above has to travel together — so CI packs each one before it becomes a release asset.

| Platform | Single file | Portable | Made by |
| --- | --- | --- | --- |
| macOS | `.dmg`, about 30MB | — | `create-dmg`, UDZO, `Starquake.Mac/dmg/build-dmg.sh` |
| Windows | `Setup.exe` | `.zip` | Inno Setup 6, `Starquake.Windows/Starquake.iss` |
| Linux | `.AppImage` | `.tar.gz` | `appimagetool`, `Starquake.Linux/AppRun` + `.desktop` |

The compression is worth noticing: 124MB of bundle becomes a 30MB `.dmg`, because `Content/` is
almost all of it and `.xnb` compresses hard.

### The macOS window

A `.dmg` is a mounted volume, and what a player sees when they open one is a Finder window. Left
to itself that is a single icon on grey, with nothing to suggest the app is meant to be copied to
Applications rather than run from the mount — so `Starquake.Mac/dmg/build-dmg.sh` sets the window
instead: 1000×480, the app on the left, the Applications folder on the right with an arrow drawn
between them, a first-launch note beside them, and the game's own title screen behind it all.

Window size, icon positions and the background picture all live in a `.DS_Store`, which only
Finder knows how to write. There is no flag on `hdiutil` for any of it. `create-dmg` is the
standard way round that: it mounts a writable image, drives a Finder session over AppleScript to
arrange the window, and converts the result to a compressed read-only image. So the packaging step
needs `brew install create-dmg` and a runner with a login session — which the GitHub-hosted macOS
runners have.

Two files have to agree: `build-dmg.sh` holds the icon coordinates, and `background.py` draws the
picture around those same coordinates. The background is generated rather than drawn by hand —
`python3 background.py [comic|title]` picks between the website's headline panel and the game's title screen
headline panel as the backdrop, and needs `brew install librsvg`. The PNG it produces is committed,
so a build does not need any of that.

**Packaging is possible at all because the game writes nothing beside itself.** `HiScoreStore`
writes to `ApplicationData` and `Screenshots` to the pictures folder, both by
`Environment.GetFolderPath`. That is what lets an AppImage work from a read-only mount and lets
the installer use the user's own Programs folder with `PrivilegesRequired=lowest`, so there is no
UAC prompt. Anything that ever wants to write next to the executable breaks all three at once.

**AppImages want `libfuse2`**, which Ubuntu has not shipped by default since 22.04, so the tarball
stays as the fallback. `--appimage-extract-and-run` is the other way round it, and is how CI runs
`appimagetool` itself.

### macOS: it is signed, and that is not the same as being allowed to run

`Starquake.app` is **ad-hoc signed** — `codesign --sign -`, which is what `CreateAotAppBundle`
does. There are two separate gates here and the bundle passes only the first:

- **Apple Silicon requires every arm64 binary to carry a signature.** Without one the kernel
  refuses to start it. The ad-hoc signature satisfies this, which is the reason that `codesign`
  line exists at all.
- **Gatekeeper wants a Developer ID signature and a notarisation ticket.** An ad-hoc signature is
  neither: `codesign -dvv` reports `Signature=adhoc` and `TeamIdentifier=not set`, and
  `spctl -a -t exec Starquake.app` answers `rejected`.

Gatekeeper's verdict is only enforced on files carrying the quarantine attribute, which is why
this never shows up locally and bites the first person you send it to:

| How it travels | Quarantined | Result |
| --- | --- | --- |
| USB stick, `scp`/`rsync`, a tarball over ssh, built on the machine | no | runs |
| Browser download, AirDrop, Mail, Messages, Slack | yes | blocked |

Tested both ways: a copy with the attribute set as Safari sets it does not launch — no window, no
process. The same bundle without it runs from anywhere, including a folder with nothing else in
it.

**The fix for a recipient** is to strip the attribute:

```sh
xattr -dr com.apple.quarantine /path/to/Starquake.app
```

Or System Settings → Privacy & Security → "Open Anyway". Note that Control-clicking the app and
choosing Open — the advice most of the internet still gives — **stopped working in macOS 15**;
Apple removed that bypass, so it is one of those two or nothing.

Doing it properly means an Apple Developer Program membership, a Developer ID Application
certificate, the hardened runtime, `notarytool` and `stapler`. That costs money every year and is
deliberately not done here. Worth knowing for whenever it is: because this build has no JIT, it
needs none of the `allow-jit` or unsigned-executable-memory entitlements a normal .NET app has to
ask the notary service for.

### Windows

`Starquake.exe` with the content beside it, plus `mgruntime.dll`. Nothing has to be installed
beyond a graphics driver with Vulkan, which every current one has.

Expect SmartScreen to object to an unsigned executable much as Gatekeeper objects to an unsigned
bundle, and for the same reason. An Authenticode certificate is the answer and is deliberately
not bought, same as the Apple one.

The AOT publish itself is the one of the three that has never been run on a developer machine
here, because NativeAOT does not cross-compile and this was all set up on a Mac. CI runs it on
`windows-latest` on every push.

### Linux

`linux-x64` and `linux-arm64` are both complete: `libmgruntime.so` ships beside the executable
for each, and needs nothing from the machine but libc and a Vulkan driver. On Debian and Ubuntu
the driver is Mesa's, in `mesa-vulkan-drivers`, which a desktop install already has.

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

### Music is `.m4a` in, Ogg Vorbis out

The five pieces of music in `Content/Music/` are `.m4a`, and nothing ships them. `SongProcessor`
runs the FFmpeg that MonoGame's pipeline packages bring with it and transcodes each to Ogg
Vorbis, so what lands in `Content/bin/` is a `.ogg` next to a 130-byte `.xnb` naming it. Every
platform reads the same `.ogg`, which MonoGame decodes itself, so there is nothing per-platform
about any of it and no reason to convert the sources by hand. The transcode is also the only one:
pre-converting the sources to `.ogg` would just add a lossy generation, because MGCB would
re-encode them anyway.

Three things about that are worth knowing before they look like mistakes:

- **The importer is called `Mp3Importer` and the file is not an MP3.** None of MonoGame's four
  audio importers claims `.m4a` - they are `.mp3`, `.ogg`, `.wav` and `.wma` - but all four are
  the same thin wrapper that hands the file to FFmpeg, so the name is a misnomer rather than a
  constraint and `Mp3Importer` reads these perfectly well. It is named explicitly in
  `Content.mgcb`, which is the only reason it works: the extension is what the MGCB Editor
  matches on when it picks an importer for a file being added, so adding these through the
  editor rather than by hand is what would go wrong.
- **`.m4a` says nothing about the codec inside.** Four of the five are AAC and
  `gameover-song.m4a` is Opus, both in an MP4 container. It makes no difference - the importer
  hands the file to FFmpeg either way and what comes out is Vorbis - but a codec check on these
  files will not find one answer, and there is no need for it to.
- **The built content is no longer all `.xnb`.** Anything copying built content has to take the
  `.ogg` too or the music is missing at runtime with nothing said about it until a screen with
  music comes up. MGCB's own build task already copies whatever it finds, and so does the macOS
  app bundling; `PrebuiltContentDir` in `Starquake.Shared.csproj` - the seam CI hands content in
  through - was `**\*.xnb` and is now `**\*.*` for this reason.

### Shaders

MGCB compiles the effects with DXC, Microsoft's Shader Model 6 compiler, to SPIR-V for Vulkan.
MGCB ships DXC for every host it runs on, so the content builds on Windows, macOS and Linux with
nothing else installed. There is no Wine, and no `MGFXC_WINE_PATH`.

Sixteen shaders are built, every pass `ps_6_0`:

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

**Several of them are shaped by a limit that is gone.** They were written for `ps_2_0`, which has
no dynamic branching and 64 arithmetic slots. That is why `Crt.fx` is three techniques rather than
one shader with branches, why the whole-tube pass takes four glow taps rather than eight, why
`Overload.fx`'s cracking pass has one sine and no rim, why `Shockwave.fx` uses a triangle wave in
place of a cosine, and why so much is `step` products rather than `if`s. `ps_6_0` has neither
limit. The comments in those shaders still give the slot counts as the reason, and whether to
simplify them is open.

`disappear.fx`, `drawaswhite.fx` and `refraction.fx` were never under `Content/Shaders/` and
nothing referenced them; they went with `Assets/`.

## CI

`.github/workflows/build.yml`, five jobs. The content is built once and handed to every publish,
because every head loads the same `DesktopVK` output and building it five times would cost five
passes through the art for the same files.

| Job | Runner | What it does |
| --- | --- | --- |
| `namespaces` | ubuntu | Counts namespace cycles in `Starquake.Shared`. No .NET, answers in a second. |
| `build` | all three | Compiles the solution and runs the tests. |
| `content` | windows | Builds the content once, uploads it as an artifact. |
| `publish` | one per target | Five NativeAOT publishes, each on its own hardware. |
| `release` | ubuntu | When `<Version>` is new on `main`: the packages become a GitHub Release. |

**`content` runs on Windows, and any runner would do.** The output is `/platform:DesktopVK` and
so is platform-independent - the same `.xnb` every head loads - so which runner produced it does
not matter to anything downstream.

**`publish` is a matrix of runners rather than five runs on one**, because NativeAOT does not
cross-compile between operating systems. `osx-arm64` and `osx-x64` do share the one macOS runner,
since Xcode's clang carries both targets. The Linux legs install `clang`, `zlib1g-dev` and
`binutils` first; without `objcopy` the publish dies at the symbol stripping, after all the
compiling is done.

**`build` deliberately does not wait for `content`.** It passes `PrebuiltContentDir` pointed at an
empty folder, which skips the content build outright, so a compile error reports in a couple of
minutes rather than after MGCB has ground through 107MB of art. Nothing it does needs the content:
the tests hand the sound engine a `ContentManager` with nothing in it on purpose, and this job
never proved anything about the art, only about the code.

`PrebuiltContentDir` is the seam all of this turns on - set it and `Starquake.Shared` copies
`.xnb` from there instead of running MGCB. Nothing is committed and a local build with the
property unset compiles the content from `Content.mgcb` exactly as before.

**Bumping `<Version>` is the release process.** See "Versioning and releasing" above and
RELEASING.md. The packages become the assets on a Release, made by `gh` rather than a
third-party action. Every other push leaves plain artifacts, which expire.

## The content build and Project Mercury

`Content.mgcb` is built by `Starquake.Shared` itself, not by the heads. That is deliberate:
MGCB keys its output folder off the `.mgcb` file's own location and the MonoGame platform, so
heads on the same platform (all three are DesktopVK) would race each other over
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

There is no escape hatch, and nothing needs one. Every head is DesktopVK on `net10.0`, so there
is no `-windows` target framework to define a symbol on, and the `WINDOWS` symbol
`Starquake.Shared` used to define on that target went with it.

Nothing ever used it. The one candidate was the commented-out F11 handler in
`Starquake.Shared/Engines/GameEngine.cs`, which resized the window back to its default through
`System.Windows.Forms.Form.FromHandle` - a WinForms call that only ever made sense under
WindowsDX, and which `GameWindow` answers on all three platforms now.

`ProjectMercury` still has 25 `#if WINDOWS` blocks through its sources, and they are inert and
always were: that project has never defined the symbol. They are upstream XNA-era guards around
`[Serializable]` and one `Guard` call, left as found - it is a fork of 2009 code held to the
compiler but not to our style.

If a platform difference ever does turn up, prefer a runtime check -
`OperatingSystem.IsWindows()` - over bringing a second target framework back.

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
    <PackageReference Include="MonoGame.Framework.Native" />
    <PackageReference Include="MonoGame.Runtime.<Platform>.Vulkan" />
  </ItemGroup>
</Project>
```

The props file supplies the reference to `Starquake.Shared` and the shared `Program.cs`. A
head only picks its framework and runtime package and adds its own packaging. Versions come
from `Directory.Packages.props`.

A head that wants a framework other than the `net10.0` `Starquake.Shared` builds means changing
that project to `<TargetFrameworks>` and multi-targeting it.

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

**Do not put `<TargetFramework>` in a publish profile.** An IDE publishes a profile by passing
its properties on the command line, which makes every one of them a *global* property reaching
every project in the graph - and a global `TargetFramework` overrides what each project chose for
itself. None of the `Aot-*.pubxml` files names one, and each says so at the top.

This used to produce a failure that named nothing relevant - `NETSDK1005 ... doesn't have a target
for 'net8.0'`, because `ProjectMercury` multi-targeted `net8.0` and `net8.0-windows` and the global
property collapsed its assets file to one framework, after which `BuildContentPipelineExtension`
asked for `net8.0` and found nothing. Nothing multi-targets any more, so that particular wreck
cannot happen; `dotnet restore` on its own repaired it when it could. The rule stands regardless,
because a global `TargetFramework` is wrong for other reasons.

**Content asset names use backslashes** (`"Graphics\\BootScreen2"`). MonoGame's
`TitleContainer` normalises those to the platform separator, so they work on Linux and macOS
as-is; there is no need to convert them.
