# Putting the game on the nullable standard

A work plan, written to be picked up cold. Everything needed to start is in this file.

## The goal, and the one rule

`Starquake.Shared` is the last project with `<Nullable>` off. Turning it on reports 246
warnings, because the code was written before nullable reference types existed and so never
says what may be null. The job is to make it say so.

**The rule: no `null!`, and no `?` added just to silence a warning.** Both of those tell the
compiler to stop asking without answering the question, which is the whole point of the
exercise. A `?` goes on a member only when null genuinely means something — "this screen has
no power-up" — and every such member should have callers that check it. If nothing checks it,
it is not nullable, and the fix is to make it non-null rather than to annotate it.

This is not a rewrite. It is about 90 field declarations and 40 construction sites across 35
files, plus roughly ten members that simply get deleted. Field *use* sites do not change: a
field keeps its name whether the constructor sets it or `LoadContent` does.

## Work one file at a time, not one project at a time

Do **not** set `<Nullable>enable</Nullable>` in `Starquake.Shared.csproj` until the very end.
Instead put `#nullable enable` at the top of a single file, fix that file, and commit. The
build stays at zero warnings the whole way through, every commit is reviewable, and anything
that breaks the game bisects to one class.

When the last file has the directive, move the setting into the `.csproj` and delete the
directives in one final commit.

## The three techniques

**1. Delete it.** Some members are never assigned anywhere and never read. `XnaFont` is the
clearest case: the entire type is reachable only through `GameEngine.TextWriter`, a field that
is never assigned and never read. Deleting beats annotating.

**2. `required` with an object initialiser.** For a member the *owner* fills in straight after
construction — `BouncingParticle.Fireball`, set by `BlobDeathAnimation.LoadContent`, or
`ContinuousParticleEffect.MercuryParticleEffect`, set by `ParticleEngine`:

```csharp
public required IStandardParticleEffect Fireball { get; init; }
```

and the owner constructs with `new BouncingParticle(engine, 0) { Fireball = …, SparkEffect = … }`.
Honest, non-null, no bang. The cost is moving the construction to where the value is known,
which usually means out of a constructor and into `LoadContent`.

**3. Pass content to the constructor.** The main group. A component is currently constructed
empty and filled later by `LoadContent` or `Initialise`. Give the constructor what it needs and
make the field `readonly`. The object cannot exist in a half-built state, so the field cannot be
null, and the two-phase pattern disappears with the warning.

Where a member really is optional, leave it `?` and make sure callers check. `GameScreenData`
is the worked example, in commit `67eb015`: `ScreenTiles` and `CollisionMap` became non-nullable
because nothing ever checked them, while `PickUps` and `PowerUp` stayed nullable because seven
call sites do.

## Order of work

Leaf classes first, so that by the time `GameEngine` is converted everything it composes already
has honest signatures. `GameEngine` is last and is the only genuinely hard one: `DefineContent()`
at line 411 constructs everything and a separate private `LoadContent()` at line 782 runs later,
so making its fields constructor-set means reordering the composition root.

| Stage | Files | Members | Notes |
| --- | --- | --- | --- |
| ~~1. Delete the dead~~ | `Fonts/XnaFont.cs`, `Fonts/XnaFontUv.cs`, `GameEngine.TextWriter` | ~10 | **Done**, `2568943` |
| ~~2. Owner-assigned~~ | the four `Particles/*.cs`, `BouncingParticle.cs` | ~10 | **Done**, `abd14e0` |
| ~~3. Leaf classes~~ | 30 files | ~50 | **Done**, `e536e79`…`d6ca5c6` |
| 4. Mid-size | `JumpPlatform` (4), `Nasty` (6), `ElectricityGatesAndNodesEngine` (8), `PlanetCoreScreen` (11), `TeleportScreen` (11), `GameScreenCollection` (36) | 76 | |
| 5. `Blob.cs` | 1 | 21 | Large but self-contained |
| 6. `GameEngine.cs` and what only it can settle | `GameEngine.cs` (40), `IntroSequence.cs` (1), `MainGameScreen.cs` (2), `TestScreen.cs` (2) | 45 | The composition root; do last |
| 7. Flip the project | `Starquake.Shared.csproj` | — | Remove all `#nullable enable` directives |

`IntroSequence` was in stage 3 and is not any more. Its `_mainScreen` is set by
`Reset(MainGameScreen)` and then dereferenced unchecked at twenty sites, so the honest fix is
the constructor — but `IntroSequence` is built in `GameEngine.Initialise` and `MainGameScreen`
in the `GameEngineStatus.Started` handler, so that means reordering the composition root, which
is stage 6's job. `MainGameScreen` and `TestScreen` are there for a smaller reason: their
`Reset` and `PerformPostScreenChangeActions` need to match `IGameScreen`'s optional data
parameter, which is a one-line change each once their file is converted.

To regenerate the member list at any point, add `<Nullable>enable</Nullable>` to
`Starquake.Shared.csproj`, run the build below, then **revert the csproj** — it is a
measurement, not a change:

```bash
dotnet build Starquake.Shared/Starquake.Shared.csproj -p:TargetFramework=net9.0 \
  --no-incremental -v n --nologo 2>&1 \
  | grep -oE "/Users/[^(]+\([0-9]+,[0-9]+\): warning CS[0-9]+: [^[]*" | sort -u
```

## Verifying each step

There are **no tests in this repository**. `TestScreen.cs` is a game screen, not a test project.
The only safety nets are a clean build and launching the game, so run both every time:

```bash
dotnet build Starquake.sln -v q --nologo              # must end: 0 Warning(s), 0 Error(s)
timeout 12 dotnet run --project Starquake.Mac/Starquake.Mac.csproj --no-build
# exit code 124 means it ran the full 12s without throwing; any other code, read the output
```

The smoke run matters more than it looks: content loading happens at startup, so a broken
constructor or a missing resource shows up immediately rather than at the first draw.

## Traps found the hard way

**Do not run `jb cleanupcode`.** It was tried on this solution and produced 34 build errors: it
strips `using MessagePack;` from the `SharedLibs` serialisation types while leaving
`[MessagePackObject]` and `[Key]` behind. It also cannot do the conversions anyone would want it
for — zero `for`→`foreach`, zero primary constructors — it only reformats. `jb inspectcode` is
fine and is what found this work.

**The measurement build empties the content output.** The `--no-incremental` build above
deletes `Starquake.Mac/bin/Debug/net9.0/Content/Graphics` and MGCB's cache then believes it is
still up to date, so the next `--no-build` smoke run dies with a missing `BootScreen2.xnb`. It
looks exactly like a broken constructor and is not. Rebuild the head project without
`--no-build` once to refill it:

```bash
dotnet build Starquake.Mac/Starquake.Mac.csproj -v q --nologo
```

**File encodings differ by project.** Our own sources are UTF-8 **with BOM** and LF endings; read
and write them as `utf-8-sig`. `ProjectMercury` sources are **Latin-1** and will raise
`UnicodeDecodeError` if opened as UTF-8. This is why `grep` treats some Mercury files as binary.

**`ProjectMercury` is out of scope.** It is a 2009 fork kept deliberately unmodernised, with
`<Nullable>disable</Nullable>` and an explanatory comment in both its `.csproj` files. Leave it
alone.

**`ScreenEditor/Program.cs` `BuildAvaloniaApp` must stay public.** Nothing in the solution calls
it except `Main`, so every inspection suggests making it private. The Avalonia visual designer
finds it by name at runtime. The same caution applies to anything reachable only from `.axaml` —
check the XAML before tightening a member in `ScreenEditor`.

**A resource name that is not registered now throws.** `ParticleEngine` raises
`InvalidOperationException` rather than returning null. If a new effect is added, register it in
`GameEngine.DefineContent` or the game will fail loudly at startup — which is the intent.

## Conventions

Commit messages are imperative and sentence case with no prefix, and the body explains *why*,
not *what* — see `git log` for this session's six commits. End every commit message with:

```
Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```

Severities for the inspections that are already at zero are pinned in `.editorconfig`; if a
cleanup here re-introduces one, that file is the place it is recorded.

## State

Stages 1 to 3 are done, in eleven commits from `2568943` to `d6ca5c6`. Warnings with the
project switch on are **246 → 142**, and 42 files carry `#nullable enable`. Solution build is
at 0 warnings, 0 errors and the smoke run reaches its twelve seconds.

Everything still outstanding is in stages 4 to 7 and lives in eleven files, listed in the table
above. Nothing is left part-converted: a file either has the directive and is clean, or has not
been touched.

Four things worth knowing before picking up stage 4:

- **`IGameScreen.Reset` and `PerformPostScreenChangeActions` take `object?` now**, as does
  `GameComponentBase.Initialise`. Implementations in files without the directive are still
  compatible, but every file converted from here on has to match those signatures, which shows
  up as CS8767 rather than as anything to think about.
- **`ResourceManager`'s lookups throw.** `GetSpriteCollection`, `GetDataResource` and
  `GetParticleEffectInstance` no longer return null for a name they do not know, so sprite and
  data fields taken from them are honestly non-null. This is what found the two misspelt
  resource names in `DefineContent`.
- **Two bugs turned up on the way** and are fixed: `CustomControl` dropped the `PlayerKeySet`
  its constructor was given, and `PickupsIlluminatedName` was spelt without its final d.
  Expect more of these in stages 4 to 6; a field that is never set is usually never read
  either, but not always.
- **`TestScreen` never calls `LoadContent` on its own `BlobDeathAnimation`**, which is why its
  fragment list is now empty rather than six particles with a null `Fireball`. Left alone
  deliberately; it is a debug screen and not part of this work.

Still outstanding and deliberately not part of this plan: unused non-private members (down from
101, several were removed here where a nullable question could not be answered without them),
75 primary-constructor suggestions (declined — they would undo the `readonly` work in `e7af430`
and break the `_camelCase` field convention), and 32 invalid XML doc comments.
