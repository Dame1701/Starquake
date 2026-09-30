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

## Where this has got to

| Landed | What it did |
| --- | --- |
| Stage 1 | The ten swappable sheet names become one `ResourceNames` value object with two instances. Ten public members become one. |
| Stage 2 | `GetInstance()` and `_sInstance` deleted. `SpritePixelMatrix` takes a `Random` and is the first class off the map entirely. |
| — | Both survey errors written up under *What the survey missed*, and the metric script corrected. |
| Stage 4 (part) | `SpriteCollection` takes a `GraphicsDeviceManager` and keeps the `ContentManager` it was already handed; `Clone` needs no argument. `SoundEffectEngine` takes a `ContentManager` and a `Random`. |
| — | `GameComponentBase` no longer holds an engine for its 22 subclasses. Each holds its own, so each can now be narrowed alone. |
| — | `UsingOldGraphics` and `ResourceNames` fold into one `GraphicsSet`. Two members become one, and the art set becomes something a class can be handed and read live. |
| — | The three particle effects stop depending on `ScreenEffectEngine` — its two hooks were identity functions and always had been — and take the `GraphicsSet` alone. `ParticleEngine` follows. |
| — | `AnimTile` takes the `GraphicsSet`. `AnimTileEngine`'s engine parameter was unused and is deleted. |
| Bug | `RandomNumberGenerator` was seeded after the resource manager was built, so handing it to the sound engine captured a null. Reordered. Mine, from two commits earlier. |
| Stage 3 | `ResourceManager(ContentManager, GraphicsDeviceManager, Random, GraphicsSet)`. The cycle is cut and the whole resource subtree is free of the engine. |
| — | `ScreenEffectBase` stops holding an engine it never called a member on. `LeftTunnelEntryEffect` and `RightTunnelEntryEffect` took one, passed it to the base, and never read it; both constructors now take nothing. `AdditiveFade`, the only one that wanted it, holds its own. |
| — | The metric script's identifier list was missing plain `gameEngine`, which under-reported four files as using no members at all. Fixed, and the counts restated against it. |
| Stage 5 (Presentation) | Seven loose fields become one `PresentationSettings`, and the transform calculation moves with them — nine tests now pin the uniform scale and the even border, which needed a graphics device before. `AdditiveFade` drops the engine entirely. |
| Stage 5 (World) | Two clusters, not one: eleven files want the planet, five the director, eight both. `ScreenDirectory` takes `CurrentGameScreen` and `GetGameScreen`; eight tests pin the throwing and lenient lookups apart. `GameScreens` — the planet — is still to do. |
| Bug | The joypad check listed five conditions and the fifth repeated the third. Dead as written; removed behaviour-preserving, with the likely intent (`Fire`) raised rather than assumed. |
| — | `InTestMode` was the last constant wearing a field's clothing. Now `TestMode.Enabled`. |

**50 → 46 coupled files**, and `GameEngine`'s public surface is down to 31 members from about 50.

### What is left of stage 5

`GameScreens` — the planet, wanted by nineteen files — and the Session cluster. Session is more
entangled than the plan expected, and not by accident:

- `Paused` is not plain state. Its setter starts and stops the periodic background sounds, and it
  is assigned during construction *before* `ContentResourceManager` exists, which is what the null
  check in the setter is really for.
- `PlayerKeys` is not plain state either. Its setter pushes the key set into `Player.BlobsControl`
  and decides keyboard against joypad — so it depends on the blob.

`BlobStatusPanel` is the only one of the four that is simply a reference, and moving it into a
`GameSession` would re-home it rather than narrow anything. So Session wants the sound engine and
the blob, which means it is stage 6 work wearing a stage 5 label. Better done once the actor graph
is decided than forced into a settings object now.

The map is down to **50 coupled files from 60**, measured with the corrected script at both ends.
The 58 and 42 quoted earlier were both computed with the incomplete identifier list and the earlier
of the two also dropped the holders branch; the honest figure is ten files freed, not sixteen.
Outside the six broad consumers the widest row is seven members. Stage 3's real signature was four primitives, not one: the graphics device manager
is for the sprite collections it loads, the `Random` for the sound engine, the `GraphicsSet` for the
animated tiles — all things it passed on rather than called.

What made stage 3 reachable was not the three constructors the plan named. It was finding that the
particle effects' dependency on `ScreenEffectEngine` was two functions that multiply by one, and
that `AnimTileEngine`'s engine parameter had never been used at all. Two of the three blockers were
not dependencies.

Stages 5 and 6 are what is left, and they are now the only things left: every row on the map above
seven members is one of the six broad consumers the plan always said this would come down to.

The 21 private `GameEngine` fields the base-class change created are PascalCase, against the
convention for private fields, and deliberately not renamed: the bodies already spell the field
that way in every one of those classes, `ScreenEffectBase` already holds its engine under the same
name, and stages 4 to 6 delete most of these fields anyway. Renaming them would be a large diff in
service of a name that is going away.

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

**Done.** Five call sites, three files, and all five wanted one of two things:

- `BreakableBridge.cs:32,34` and `JumpPlatform.cs:33,44` — `GraphicsDeviceMgr.GraphicsDevice`
- `SpritePixelMatrix.cs:61` — `RandomNumberGenerator`

The two sprite classes already received a `GameEngine`, so those four were passing a narrower thing
they could have had all along — and in `BreakableBridge` the very next method was already reaching
`GraphicsDevice` through the reference it held, so the static was the odd one out in its own file.

`SpritePixelMatrix` was the exception the plan had wrong: it takes no `GameEngine` and never did,
only an `int size`. So rather than narrowing an existing reference it gains a `Random` parameter,
and the two places that construct one hand it the engine's. That also finishes stage 4's job for
this class early — it is the first to drop off the dependency map entirely.

`_sInstance`, the assignment in the constructor, and `GetInstance()` are gone. The only `static`
left in `GameEngine` is a private generic guard, so the invariant every later stage leans on now
holds: **the only route to the engine is a constructor parameter**, and the dependency map is the
whole truth.

## Stage 3 — cut the resource cycle

**Blocked on a decision, and the plan was wrong about its size.** See *What the survey missed*
below: `ResourceManager` does not use one member of the engine, it uses one member and is a conduit
for five constructors. Read that section before starting this stage.

The biggest single win, and the one that makes the rest tractable.

`ResourceManager` is depended on by 34 files — and it uses exactly one member of the engine,
`ContentMgr`. Its two children are nearly as thin: `ParticleEngine` needs `ContentMgr` and
`GraphicsDeviceMgr`; `SoundEffectEngine` needs `ContentMgr` and `RandomNumberGenerator`.

*(As landed: both of those were right, and `ResourceManager` itself needed four primitives rather
than one. See* Where this has got to.*)*

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

## What the survey missed

Two things, both found by doing stages 1 and 2 rather than by reading. The first was local and is
recorded under stage 1. The second is structural and changes stages 3 to 6.

**The map counts member accesses, so it cannot see the engine being passed on.** The script greps
for `<engine reference>.<Member>`. A class that takes a `GameEngine` and only hands it to something
else never matches, and so never appears. Ten such files existed, and the true number of files
coupled to the engine was **60, not 50** — 58 as first counted here, before plain `gameEngine` was
found to be missing from the identifier list too:

`AnimTileEngine`, `Overlays`, `PanelNumbers`, `LeftTunnelEntryEffect`, `RightTunnelEntryEffect`,
`ScreenEffectBase`, `ProportionalNumbers`, `GameScreenBase`, `HiScores`, `GameComponentBase`.

`ResourceManager` is the case that matters, because stage 3 turns on it. The map has it using one
member, `ContentMgr`, which is true — and it also keeps `_gameEngineInstance` in a field to pass
into five constructors: `ParticleEngine`, `AnimTileEngine`, `SoundEffectEngine`, and every
`AnimTile` and `SpriteCollection` it loads. So `ResourceManager(ContentManager)` is not a
three-constructor change. `SpriteCollection` wants `ContentMgr`, `GraphicsDeviceMgr` and
`PresentationTransform`; `AnimTile` wants the art-set selection. Those have to be narrowed first or
`ResourceManager` has nothing to pass them. Stage 3 sits on top of part of stage 4, not before it.

**The base classes are the actual backbone, and the plan never names them.** `GameComponentBase`
holds `protected GameEngine GameEngine` (`GameComponentBase.cs:12`) and does nothing else with it —
it never calls a member itself. It is a pure conduit, and it is why `GameEngine.Xxx` is a legal
expression in so many files: for 31 of the coupled classes that spelling is the *inherited field*,
not the type name. `ScreenEffectBase` does the same for three more.

That is the real mechanism by which the god object is reachable from everywhere, and it changes
what the later stages cost:

- **Stage 4's "roughly twenty classes are one-line changes" is optimistic.** `AnimTile`,
  `BlobDeathAnimation`, `ColourAnimationEngine`, `StatusPanel` and others on that list inherit the
  field. Dropping their `GameEngine` reference means not calling `base(gameEngine)`, which means
  the base constructor's parameter has to go, which touches every sibling at once — the
  one-file-at-a-time property the whole plan depends on does not hold through the base class.
- **The order has to account for it.** Either `GameComponentBase` loses its engine parameter early,
  in one deliberate commit that touches all 22 subclasses and gives each what it actually uses; or
  it keeps the parameter and stages 4 to 6 narrow only the classes that take their own, leaving the
  inherited field as a second route to the engine and the invariant stage 2 bought only half true.

The first is a large single commit and against this plan's grain. The second leaves the job
unfinished. Worth deciding explicitly before stage 3, because both readings change what stage 3 is.

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
  # "gameEngine" was missing from this list until stage 3, and four files spell it that way.
  m=$(grep -hoE "\b(GameEngine|gameEngine|_gameEngine|_gameEngineReference|_gameEngineInstance|gameEngineReference|gameEngineInstance|gameEngineRef|_gameEngineRef)\.[A-Za-z_][A-Za-z0-9_]*" "$f" \
      | sed -E 's/^[^.]+\.//' | sort -u | tr '\n' ' ')
  # A class that only passes the engine on accesses no member, so count the holders too or the
  # map reports 50 coupled files where there are 58.
  if [ -z "$m" ] && grep -qE "GameEngine [a-zA-Z_]|readonly GameEngine|protected GameEngine" "$f"; then
    m="(holds one, passes it on)"
  fi
  if [ -n "$m" ]; then printf "%-62s %s\n" "${f#Starquake.Shared/}" "$m"; fi
done
```

Two ways this under-reports, both of which bit once already:

- **The identifier list** was *not* exhaustive, though this document claimed it had been checked.
  Plain `gameEngine` was missing, and `HiScores`, `PanelNumbers`, `Overlays` and
  `ProportionalNumbers` all spell it that way — so all four reported no members at all while
  actually using `ContentResourceManager`, `Font2` or `ShapeDrawer`. Added. Every number in this
  document that predates stage 3 was computed without it.
- **A member access is not the only kind of coupling.** The `(holds one, passes it on)` branch above
  catches the ten files that take a `GameEngine` and only hand it to something else. It still does
  not distinguish a class that uses the engine from one that inherits the field from
  `GameComponentBase` and uses it — for those 31, `GameEngine.Xxx` is the inherited field, so they
  are counted, but the map makes them look like they hold their own reference when the fix is in
  the base class. Read the rows against *What the survey missed*.

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
