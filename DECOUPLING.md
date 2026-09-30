# Breaking up the god object

`GameEngine` is 1371 lines and ~50 public members, and 50 files across `Starquake.Shared` reach into
it. This plan takes it apart.

The destination is the obvious one: each class is handed the specific things it uses, not the
object those things happen to hang off. The order matters more than the destination, and most of
this document is about the order.

This follows `REFACTORING.md`'s discipline. Every stage builds at 0 warnings and 0 errors and
leaves the 40 tests passing, so every stage is independently shippable and a bisect can tell two
stages apart. No behaviour changes are smuggled in; where a real defect is found it gets its own
commit, separate from any move.

## Why not just rewrite the constructors

The instinct — inventory what `GameEngine` provides, find every consumer, feed each one its
specifics — is right about the endpoint and wrong about the route. Done as one pass it touches 50
files simultaneously, with 40 tests covering two classes to catch the mistakes, and produces a
diff nobody can review or bisect. Three things also have to happen *before* the constructor
rewrite is even possible:

- **Some of what it "provides" is not a dependency at all.** Ten of the members are constants
  wearing a field's clothing. Injecting them would be enshrining a mistake.
- **The graph is cyclic, so some of it cannot be injected yet.** `GameEngine`'s constructor does
  `new ResourceManager(this)` (`GameEngine.cs:227`) and hands out `this` before it is finished
  constructing. `Blob` reads `GameEngine.Player` — itself — at `Blob.cs:1592`. You cannot pass a
  thing to a constructor that does not exist yet; the cycles have to be cut, not injected.
- **The clusters are invisible under the noise.** What `GameEngine` actually is turns out to be
  five or six coherent objects, but that is only legible once the fake constants are gone.

So: subtract first, cut the cycles second, then inject — leaves before trunk.

## What the consumers actually want

Every class was measured against the members it touches. The shape of the answer is that the
dependency is almost always narrow — the median consumer uses **four** of the ~50 members, and
twenty classes use three or fewer. Six classes account for nearly all the breadth.

| Cluster | Members | Files |
| --- | --- | --- |
| Resources | `ContentResourceManager`, `ContentMgr`, the ten `*Name` fields, `Font1`, `Font2`, `UsingOldGraphics` | 34 |
| Chance | `RandomNumberGenerator` | 20 |
| World | `GameScreens`, `GetGameScreen`, `CurrentGameScreen` | 25 |
| Presentation | `GraphicsDeviceMgr`, `PresentationTransform`, `ScreenClearColour`, `ScreenXOffset`, `ScreenYOffset`, `ScanlinesEnabled`, `BackgroundColour`, `DrawBackgrounds`, `Scanlines` | 14 |
| Session | `BlobStatusPanel`, `Paused`, `PlayerKeys`, `InTestMode`, `IntroSequence` | 12 |
| Actors | `Player`, `Nasties`, `Elements`, `BlobKiller`, `ConveyerNastiesInstance`, `ElectricityNodeGateEngine`, `BreakableBridgesEngine`, `TunnelAnimations`, `ColourAnimationEngine`, `ScreenEffectEngine` | 20 |

The six broad consumers, which stage 5 is entirely about:

| Class | Members used | Lines |
| --- | --- | --- |
| `MainGameScreen` | 23 | 610 |
| `Blob` | 14 | 2122 |
| `TeleportScreen` | 13 | — |
| `CheopsScreen` | 12 | — |
| `AccessBoothScreen` | 11 | — |
| `GameScreenCollection` | 10 | 1887 |

## The pattern to copy

`IFireSurroundings` and `BlobFire` already did this once, and did it correctly. The interface is
five members — the questions a shot in flight has to ask of the world — and extracting it is what
let the stepping rules be exercised with no screen, no content pipeline and no graphics device.
It also immediately caught two branches that had silently drifted.

That is the test for every interface below: **not "what does this class touch", but "what does
this class need in order to be run without a graphics device".** An interface that merely renames
a slice of `GameEngine` has moved the coupling, not removed it.

## Stage 1 — subtract the things that were never dependencies

**Done.** Not quite as written: the survey that produced this plan was wrong about these fields.

The ten `public string *Name` fields (`GameEngine.cs:109-206`) are *not* assigned only at their
declaration. `SwitchGraphicsSetIfAsked` reassigned all ten on F8, swapping every swappable sheet
between the redrawn artwork and the original 1985 graphics. They are mutable state, not `const`
with extra steps, and moving them to a static class would have deleted the F8 toggle.

What is true is the thing underneath: the ten names carry no independent state. They are a pure
function of `UsingOldGraphics` — one bit, read ten ways. So instead of ten constants there is now
one immutable value object with exactly two instances, `ResourceNames.Redrawn` and
`ResourceNames.Original`, in `Starquake.Shared/Constants/ResourceNames.cs`. `GameEngine` holds one
field pointing at whichever is in play, and F8 swaps the pointer instead of rewriting ten strings.
Both name sets are spelled out in full rather than derived by appending `Original`, so searching
for a sheet name still finds the place it is asked for as well as the place it is loaded.

Ten of the ~50 public members become one, and the consumers thin as predicted where they wanted a
name and something else — `KeyScanner` 5 to 3, `CoreElement` 6 to 3, `GameScreenCollection` 10 to
7. The ones that wanted *only* a name do not thin, because they still reach through the engine for
the art set: `BlobAccessBoothAnim` stays at 2 rather than dropping to 1. Those go when the
Resources cluster is collapsed in stage 3, which is where `ResourceNames` belongs in the end -
it is the same art-set selection `UsingOldGraphics` is, and the two should not stay separate
members.

One loose end found on the way: `AccessCodesGrey` is written by the toggle and read nowhere in the
solution. It is kept, since it is part of the art set and the sheet exists, but it is dead as of
now.

## Stage 2 — delete the static

Five call sites, three files, and all five want one of two things:

- `BreakableBridge.cs:32,34` and `JumpPlatform.cs:33,44` — `GraphicsDeviceMgr.GraphicsDevice`
- `SpritePixelMatrix.cs:61` — `RandomNumberGenerator`

Both classes already receive a `GameEngine`, so this is passing a narrower thing they could have
had all along. Then `_sInstance` and `GetInstance()` are deleted.

Small, but it buys an invariant every later stage leans on: after this, **the only route to the
engine is a constructor parameter**, so the dependency map is the whole truth.

## Stage 3 — cut the resource cycle

The biggest single win, and the one that makes the rest tractable.

`ResourceManager` is depended on by 34 files — and it uses exactly one member of the engine,
`ContentMgr`. Its two children are nearly as thin: `ParticleEngine` needs `ContentMgr` and
`GraphicsDeviceMgr`; `SoundEffectEngine` needs `ContentMgr` and `RandomNumberGenerator`.

So the entire resource subtree — the thing a third of the codebase depends on — needs three
primitives and no engine at all:

```
ResourceManager(ContentManager)
  ParticleEngine(ContentManager, GraphicsDeviceManager)
  SoundEffectEngine(ContentManager, Random)
```

Change those three constructors and the `engine → ResourceManager → engine` cycle is gone.
`GameEngine` keeps constructing them; it is the composition root and should be. `Font1`/`Font2`
and the art-set selection — `UsingOldGraphics` (15 files) together with the `ResourceNames` stage
1 left behind, which are one decision held as two members — belong here too, and folding them in
collapses the whole Resources cluster into one narrow dependency.

## Stage 4 — inject the leaf primitives

Now mechanical, and roughly twenty classes are one-line changes. `Random` to its 20 consumers,
`ContentManager` to its 7, `GraphicsDeviceManager` to its 7, the stage 3 `ResourceManager` to its
34. These are all objects that never needed the engine to be reached through.

The classes that finish this stage with no `GameEngine` reference at all: `ElementFader`,
`AnimTile`, `SpritePixelMatrix`, `BreakableBridges`, `RenderViewPort`, `AdditiveFade`,
`CustomControl`, `ParticleEngine`, `SoundEffectEngine`, `ResourceManager`, `BlobDeathAnimation`,
`BouncingAnimation`, `BlobAccessBoothAnim`, `ElectricityRandomiser`, `JumpPlatforms`,
`BouncingParticle`, `SpriteCollection`, `KeyDefiner`, and the three particle effects.

That is twenty-odd of the 50, done without inventing a single interface.

## Stage 5 — extract the role interfaces

Only now, with the noise gone, are the remaining clusters worth naming — and each one gets an
interface only when a test wants it, never preemptively.

- **Presentation** is not a service, it is settings: `PresentationTransform`, `ScreenClearColour`,
  the two screen offsets, `ScanlinesEnabled`, `BackgroundColour`, `DrawBackgrounds`. A
  `PresentationSettings` object holding state, passed where it is read.
- **World** — `GameScreens`, `GetGameScreen`, `CurrentGameScreen` — is the one genuine service, and
  `IScreenDirectory` is its `IFireSurroundings`: "which screen am I on, and give me another one".
  25 files want it.
- **Session** — `BlobStatusPanel`, `Paused`, `PlayerKeys`, `InTestMode` — is score, lives, pause and
  controls. A `GameSession` object, and the thing most likely to yield new tests.

The six broad consumers are the whole of the work here. They are the game's centre and some of
them will legitimately keep a wide dependency — but on three or four named things rather than one
50-member class, and each extraction is a class that can be tested.

## Stage 6 — the actors, and the last cycle

Left until last because it is the real one. `GameEngine.Player` is read by 10 files, one of which
is `Blob` reaching back through the engine to find itself (`Blob.cs:1592`). The actors
(`Player`, `Nasties`, `Elements`, `BlobKiller`, and the five engines) are mutually referential by
nature — nasties need the blob, the blob needs the bridges — and no amount of constructor
threading makes that graph acyclic on its own.

This is where the honest answer may be "a registry the actors resolve each other through, set up
once after construction", rather than a pretence that the graph is a tree. Decide it with stages
1–5 landed and the cycle isolated to this one cluster, not now.

## What this is not

**No DI container, at any stage.** The wiring is already centralised — `GameEngine`'s
`BuildComponentsNeedingLoadedContent` and `BuildGameScreens` are a composition root in embryo,
they just pass `this` every time. Once the graph is narrow, that is about twenty objects wired
once at boot, and hand-wiring it in a composition root is clearer than a container, with no
trimming or startup story to manage. A container would also have thrown on the cycles in stages
3 and 6, which is a reason to fix them, not a reason to adopt one.

**No new tests as a goal, but tests as the measure.** If a stage extracts an interface and nothing
becomes testable that was not testable before, the interface earned nothing and should be
reverted. That is the only check that distinguishes this from moving the coupling around.

## Appendix — regenerating the measurements

Every number in this document came from the script below. It is the progress metric as well as the
survey: re-run it after each stage and the rows should get shorter and fewer. The job is done when
it prints only the six broad consumers, and stage 4 is done when the twenty classes named above
have dropped off it entirely.

```sh
for f in $(find Starquake.Shared -name "*.cs" -not -path "*/obj/*" -not -name "GameEngine.cs"); do
  m=$(grep -hoE "\b(GameEngine|_gameEngine|_gameEngineReference|_gameEngineInstance|gameEngineReference|gameEngineInstance|gameEngineRef|_gameEngineRef)\.[A-Za-z_][A-Za-z0-9_]*" "$f" \
      | sed -E 's/^[^.]+\.//' | sort -u | tr '\n' ' ')
  if [ -n "$m" ]; then printf "%-62s %s\n" "${f#Starquake.Shared/}" "$m"; fi
done
```

The identifier list in that regex is every name the engine reference goes by across the codebase.
It was checked to be exhaustive as of this document; if a stage introduces another spelling, add it
or the map will silently under-report.

Verify a stage with:

```sh
dotnet build Starquake.sln -v q --nologo     # must stay 0 warnings, 0 errors
dotnet test Starquake.Tests/Starquake.Tests.csproj --nologo    # must stay 40 passing
```

## Appendix — the map as it stood at the start

The baseline, before stage 1. Sorted by how much each file wants.

| File | Members used |
| --- | --- |
| `GameComponents/GameScreens/MainGameScreen.cs` | BlobKiller BlobStatusPanel BreakableBridgesEngine ColourAnimationEngine ContentMgr ContentResourceManager ConveyerNastiesInstance CurrentGameScreen DrawBackgrounds ElectricityNodeGateEngine Elements GameScreens GetGameScreen InTestMode IntroSequence Nasties Paused Player PlayerKeys RandomNumberGenerator ScreenEffectEngine TunnelAnimations |
| `ActiveSprites/Blob.cs` | BlobKiller BlobSpritesName BlobStatusPanel BreakableBridgesEngine ContentResourceManager CurrentGameScreen ElectricityNodeGateEngine GameScreens GetGameScreen Paused Player PlayerKeys ScreenEffectEngine UsingOldGraphics |
| `GameComponents/GameScreens/TeleportScreen.cs` | ContentMgr ContentResourceManager CurrentGameScreen Font2 GameScreens GetGameScreen LowerScreenTilesName Player RandomNumberGenerator ScreenXOffset ScreenYOffset ShapeDrawer UpperScreenTilesName |
| `GameComponents/GameScreens/CheopsScreen.cs` | BlobStatusPanel ContentResourceManager CurrentGameScreen Elements Font2 GameScreens GetGameScreen PickupsName Player RandomNumberGenerator ShapeDrawer UsingOldGraphics |
| `GameComponents/GameScreens/AccessBoothScreen.cs` | BlobStatusPanel ContentResourceManager CurrentGameScreen Font2 GameScreens GetGameScreen LowerScreenTilesName Player RandomNumberGenerator ShapeDrawer UpperScreenTilesName |
| `Display/GameScreenCollection.cs` | ColourAnimationEngine ContentResourceManager Elements LowerScreenTilesName PickupsName Player PowerUpsName RandomNumberGenerator UpperScreenTilesName UsingOldGraphics |
| `Animations/IntroSequence.cs` | BlobKiller BlobsShipName ContentResourceManager Font2 GameScreens GetGameScreen RandomNumberGenerator ScreenClearColour ScreenEffectEngine UsingOldGraphics |
| `GameComponents/PresentationScreens/GameOverScreen.cs` | BlobStatusPanel ContentResourceManager CurrentGameScreen Elements Font2 GameScreens GetGameScreen GraphicsDeviceMgr ScreenClearColour ScreenEffectEngine |
| `GameComponents/PresentationScreens/ComputerReportScreen.cs` | ContentResourceManager CurrentGameScreen Font2 GetGameScreen GraphicsDeviceMgr RandomNumberGenerator ScreenClearColour ScreenEffectEngine |
| `GameComponents/PresentationScreens/OptionsScreen.cs` | ContentResourceManager CurrentGameScreen Font2 GetGameScreen GraphicsDeviceMgr PlayerKeys ScreenClearColour ScreenEffectEngine |
| `GameComponents/PresentationScreens/HiScoreScreen.cs` | BlobStatusPanel ContentResourceManager CurrentGameScreen Font2 GetGameScreen GraphicsDeviceMgr ScreenClearColour ScreenEffectEngine |
| `GameComponents/PresentationScreens/GameCompleteScreen.cs` | ContentResourceManager CurrentGameScreen Elements Font2 GetGameScreen GraphicsDeviceMgr ScreenClearColour ScreenEffectEngine |
| `GameComponents/GameScreens/PlanetCoreScreen.cs` | BackgroundColour ContentResourceManager CurrentGameScreen Elements GameScreens GetGameScreen Player |
| `ActiveSprites/CoreElement.cs` | AccessCodesFailedName AccessCodesPassedName BackgroundColour ContentResourceManager PickupsIlluminatedName PickupsName |
| `ActiveSprites/Nasties.cs` | ContentResourceManager ConveyerNastiesInstance ElectricityNodeGateEngine GameScreens Player RandomNumberGenerator |
| `ActiveSprites/Nasty.cs` | ContentResourceManager GameScreens Paused Player RandomNumberGenerator UsingOldGraphics |
| `Display/StatusPanel.cs` | BlobStatusPanel ContentResourceManager CurrentGameScreen IntroSequence PickupsName Player |
| `Animations/KeyScanner.cs` | AccessCodesFailedName AccessCodesPassedName ContentResourceManager PickupsName RandomNumberGenerator |
| `ActiveSprites/BreakableBridge.cs` | ContentResourceManager GetInstance GraphicsDeviceMgr ScreenEffectEngine UsingOldGraphics |
| `ActiveSprites/JumpPlatform.cs` | ContentResourceManager GetInstance GraphicsDeviceMgr ScreenEffectEngine UsingOldGraphics |
| `ActiveSprites/ConveyerNasties.cs` | ContentResourceManager GameScreens Player RandomNumberGenerator |
| `ActiveSprites/ConveyerNasty.cs` | ContentMgr ContentResourceManager GameScreens RandomNumberGenerator |
| `ActiveSprites/ElectricityGatesAndNodesEngine.cs` | ContentResourceManager GameScreens Player UsingOldGraphics |
| `Animations/ColourAnimationEngine.cs` | ContentResourceManager GameScreens LowerScreenTilesName UpperScreenTilesName |
| `Animations/TunnelLiftAnimationEngine.cs` | ContentResourceManager GameScreens UpperScreenTilesName UsingOldGraphics |
| `GameComponents/PresentationScreens/TestScreen.cs` | ContentResourceManager Font1 GameScreens ScreenEffectEngine |
| `GameHost.cs` | Draw PresentationTransform Scanlines ScanlinesEnabled |
| `Sprites/SpriteCollection.cs` | ContentMgr GraphicsDeviceMgr PresentationTransform |
| `Engines/ScreenEffects/ScreenEffectEngine.cs` | GraphicsDeviceMgr PresentationTransform ScreenClearColour |
| `ActiveSprites/CoreElements.cs` | BlobStatusPanel RandomNumberGenerator |
| `ActiveSprites/ElectricityRandomiser.cs` | ContentResourceManager RandomNumberGenerator |
| `ActiveSprites/BouncingParticle.cs` | GameScreens RandomNumberGenerator |
| `ActiveSprites/JumpPlatforms.cs` | ContentMgr GameScreens |
| `Particles/ParticleEngine.cs` | ContentMgr GraphicsDeviceMgr |
| `Particles/MultiTriggerTimeBasedParticleEffect.cs` | ScreenEffectEngine UsingOldGraphics |
| `Particles/ContinuousParticleEffect.cs` | ScreenEffectEngine UsingOldGraphics |
| `Particles/TriggeredParticleEffect.cs` | ScreenEffectEngine UsingOldGraphics |
| `Sounds/SoundEffectEngine.cs` | ContentMgr RandomNumberGenerator |
| `Animations/BlobDeathAnimation.cs` | ContentResourceManager UsingOldGraphics |
| `Animations/BouncingAnimation.cs` | ContentResourceManager RandomNumberGenerator |
| `Animations/BlobAccessBoothAnim.cs` | BlobSpritesName ContentResourceManager |
| `GameComponents/PresentationScreens/KeyDefiner.cs` | ContentResourceManager Font2 |
| `ActiveSprites/ElementFader.cs` | RandomNumberGenerator |
| `ActiveSprites/BreakableBridges.cs` | GameScreens |
| `Input/CustomControl.cs` | Player |
| `ContentManagement/ResourceManager.cs` | ContentMgr |
| `Engines/RenderViewPort.cs` | GraphicsDeviceMgr |
| `Engines/ScreenEffects/AdditiveFade.cs` | PresentationTransform |
| `Animations/AnimTile.cs` | UsingOldGraphics |
| `Sprites/SpritePixelMatrix.cs` | GetInstance |
