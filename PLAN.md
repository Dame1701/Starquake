# Fixing what the review found

**Phases 0 to 5 have landed, and most of phase 6.** This document was written the way `REFACTORING.md` and
`DECOUPLING.md` were written before their stages landed, and each finished phase's section has
been replaced with what the work actually decided, as it landed, rather than left as the
intention. What is left is 6.1 and 6.2, both deferred by decision rather than by difficulty, and
6.6, which is open.

It covers a full review of the game carried out against `ef5a6a3`. Every finding is here; the
ordering is the point of the document. `ProjectMercury` was out of scope throughout.

The starting state was good and is worth naming, because it set the bar for everything below. The
solution built at 0 warnings and 0 errors, the 65 tests passed, there was no `null!` or
`#pragma warning disable` anywhere in our own code, only six methods exceeded 80 lines, and there
were nine lines of commented-out code in the whole game. What was left was class-level shape, a
short list of real defects, and a standard that was written down but never checked.

## How this is ordered

Three rules, applied in this priority. Where they conflict, the earlier rule wins.

**Things that are wrong for the player come first.** A crash or a bricked control scheme outranks
any amount of internal tidiness, and each of those is hours of work rather than days.

**The safety net goes in before the structural work, not after.** Phase 1 makes the build enforce
the standard `.editorconfig` already declares. Done afterwards, the phase 4 splits could quietly
reintroduce the 35 violations and nothing would say so.

**Subtract before you move.** Every line deleted in phase 2 is a line phase 4 does not have to
relocate, re-test or argue about.

Phase 4 is the expensive one and is where the god-class question is actually settled. Phases 5 and
6 are optional in the sense that nothing depends on them.

Verification for every item is the existing bar, and since phase 1 it is also CI:

```sh
dotnet build Starquake.sln                          # must stay at 0 warnings, 0 errors
dotnet test Starquake.Tests/Starquake.Tests.csproj  # must stay green
```

## Phase 0 — stop the bleeding *(landed)*

Player-visible wrongness and one latent crash, one commit and one play-test each.

**0.1 — the three world-edge bounds tests** (`105a07c`). `Blob.ChangeScreenOffRightEdge`,
`Blob.ChangeScreenOffBottomEdge` and `GameEngine`'s NumPad2 test key all guarded with
`ScreenNumber + n > WorldMap.ScreenCount`. `ScreenCount` is 512 and the screen array is exactly
that long, so the last valid index is 511 — which `WorldMap` already names `LastScreenNumber`. The
right-edge test permitted 512; the bottom-edge test, adding `ScreensAcross`, permitted up to 527.
Both dereference the array on the next line. The matching left and top tests already used `>= 0`
and `StepBetweenScreens`'s NumPad6 already used `LastScreenNumber`, so these three were the odd
ones out rather than a convention.

**0.2 — the joystick option is a joypad** (`ca765b6`). Selecting *Joystick 0* bound Up, Down,
Left, Right and Fire to `Keys.None` and set a `UseJoyPad` flag nothing read, so a player who
picked it could pause and quit and nothing else, with no message saying why. The decision was to
implement it rather than remove it: DesktopGL reaches a pad through SDL, so `CustomControl` reads
`GamePad.GetState(PlayerIndex.One)` — d-pad or left stick past a threshold for the four
directions, A for fire.

Two things fell out of doing it that way. The pad is read **alongside** the keyboard rather than
instead of it, so it works whichever preset is chosen and the `UseJoyPad` flag and the inference
that set it are both gone; the joystick preset is now simply the one that binds no keys. And the
preset is offered only when a pad is actually plugged in — pulling one out while its preset is
selected hands the selection back to the cursor keys, because confirming it otherwise leaves
nothing bound.

**0.3 — `MatchSomeCoreElementsToTheCore` is clamped** (`712bedb`) to the lengths of both lists it
draws from, rather than trusting `Next(2, 6)` to fit. Neither list is a fixed length: a hundred
pick-up screens is what made five safe, not construction. The type check the key placement needs
is still phase 6's — see *The key and the access card*.

**0.4 — the world generator is seeded** (`a0744c7`). `Reset` built a `Random` of its own from
`DateTime.Now.Ticks` while the class already held one from its constructor — and
`PlaceCheopsPyramid` used both, the local for the location and the field for the access code. The
local is gone and the parameter threading it through four methods with it. This is what makes 4.3
testable and what lets a bad world layout be reproduced from a bug report.

**0.5 — the three orphaned doc comments** (`63554d9`). Each declaration carried two stacked
`<summary>` blocks and the compiler kept the second. `BlobMaxItemsHeld`'s summary sat above
`TunnelEntrySpeed`; `GameEngine` carried a summary of the deleted `GetInstance()` singleton on
`GameScreens`; `UpdateFrameOfMind` had the same summary twice, differing only in line wrapping.
All three look like a member sort that moved declarations and left their comments behind, so the
`awk` below is worth re-running after any future sort:

```sh
for f in $(find Starquake.Shared SharedLibs -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*"); do
  awk -v F="$f" '/\/\/\/ <\/summary>/{p=NR} /\/\/\/ <summary>/{if(NR==p+1) print F":"NR}' "$f"
done
```

## Phase 1 — the safety net *(landed)*

`.editorconfig` opens by saying its severities exist "so Rider, `dotnet build` and
`jb inspectcode` all report the same thing and the cleanup we did does not drift back a file at a
time." **`dotnet build` did not report them.** Analysers with the `IDE` prefix only run during a
build when `EnforceCodeStyleInBuild` is set, and nothing set it. The five rules declared as
kept-at-zero had never been checked by a build, and the game had 35 violations of them.

**1.1 and 1.2 landed as one commit** (`b10a13c`), because a commit that switches the gate on
without clearing the findings does not build, and a bisect needs every commit to.
`Directory.Build.props` sets `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`, `Nullable`,
`ImplicitUsings` and `LangVersion` once for the solution. ProjectMercury is excluded through the
`.editorconfig` section it already had.

What the 35 turned out to be, which was not quite what the review predicted:

**Five were named, documented constants nobody read** — and in every case the value they described
was spelled out as a literal a few hundred lines further down. `Blob.FireFadeStep` against a bare
`16`, `BlobStartXPos`/`BlobStartYPos` against `416` and `414`, and
`GameScreenCollection.ItemTileXOffset`/`ItemTileYOffset`, which were a second copy of
`PlacementXInset` and `PlacementYLift` under a different name. So they are used now rather than
deleted, and the surviving pair says where its numbers come from.

**`SpriteCollection.Begin`'s unused `gameTime` was the expensive one.** Dropping it left about
forty private draw helpers whose only use of `gameTime` was passing it on, and the screens above
them after that — 45 declarations and their call sites in all. Also gone: `ScreenEffectEngine`'s
`screenWidth`/`screenHeight`, which every caller reads off `CompositionSize` anyway;
`GameEngine`'s `parentWindow`; `GameScreenCollection`'s `xOffset` and `yOffset`, both passed `0`;
`JumpPlatforms`' `ContentManager`; and `Main`'s `args`.

**`CustomControl`'s two analogue speed-band methods** went with them, and with them its `Blob`
field — dissolving the `Blob` ↔ `CustomControl` cycle the decoupling work left standing. Worth
recording, as evidence of what unreferenced code hides: `SetBlobVSpeed`'s band chain had a hole —
an input between `VerticalDeadZone` (0.1) and `SpeedBand1` (0.2) matched no branch and fell
through to the final `else`, giving full speed. Invisible only because nothing called it.

**1.3 — `Directory.Packages.props`** (`b9f071c`). MonoGame was pinned in five project files and
MessagePack in three; two of the five MonoGame references are different flavours of the same
framework, which have to move together or the heads stop agreeing with the content pipeline.

**1.4 — CI** (`d73c6fe`). `.github/workflows/build.yml` restores, builds and tests on Ubuntu,
macOS and Windows on every push to `main` and every pull request. Three runners rather than one:
every project builds on every platform, so one would prove the code compiles, but the packaging
does not travel — the `.app` bundle and its ad-hoc signature are a macOS-only target. 8.0 is
installed alongside 9.0 because MGCB is a `net8.0` application.

## Phase 2 — subtract *(landed)*

**2.1 landed inside 1.2**, because the analyser sweep required it — see above.

**2.2 — `IGameScreen.Success` is gone** (`036b437`). Nine classes implemented it, eight answered a
constant `true`, and nothing read it through the interface. `AccessBoothScreen`'s copy is private.

**2.3 and 2.4 — `SpriteCollection`** (`6a44399`). `Clone` copied two `int[]` by round-tripping
them through `MessagePackSerializer` and loaded the texture a second time; the constructor invoked
on the line above computes both arrays from the same sheet's dimensions and loads the same
texture, so all three assigned a value over an identical one. With the initialiser no longer
writing them, those three fields are `readonly`. `[Serializable]` and the four `[NonSerialized]`
attributes went too — `BinaryFormatter` was removed in .NET 9, so they marked nothing.

**2.5 — two dead field initialisers** (`2e220f1`). `Nasties._nastyCount = 1` was overwritten in
the constructor. `GameScreenCollection._pickUpCount = 100` was never assigned at all — a `const`
wearing a field's clothing, and a `const` now.

**2.6 — `packages/` deleted** (`64bfa25`). 21 tracked files, 5.7 MB, nothing referencing them.

**2.7 — `Temp/` is `Assets/`** (`0275677`), and the plan was wrong about why. The content pipeline
does *not* build from it: `Content.mgcb` compiles `Starquake.Shared/Content/`, which holds its own
copy of nearly all the same files — 175 byte-identical, 17 differing, and five that exist only in
`Assets` (three `Shaders/*.fx` nothing compiles, two font PNGs the `.spritefont` descriptions
replaced). So there was nothing in `Content.mgcb` to update. `BUILD.md` now says what the folder
is, that keeping the two in step is manual, and that they have drifted.

**2.8 — the carried-over follow-ups** (`d31561b`). Three of the four were still there.

`LayJumpPlatformOrJump`'s empty `if (temp == -1) { }`: `GetNewJumpPlatform` never returns `-1`, it
returns `-1000`, which is now `JumpPlatforms.NoRoomForPlatform` and says why it has to be well
below any real answer — the position it normally returns can itself be negative mid-screen-change.

`BuildScreenCollisionMap`'s `screenChanged` was confirmed to control only the key-door cache. The
`!screenChanged` path removed the game-object tiles and the map was replaced wholesale on the next
line, so that work was discarded — and the removal walked the list by index while removing from
it, so it would have skipped every second match had it mattered.

`PowerUpIsInTheWay` was **deleted rather than fixed**, which the review did not anticipate. It
could never fire: it compared a pixel location against a tile index, and with a 96-pixel tile and
the 64-pixel lift an item is drawn at, the Y test has no solution for any row on the screen.
Making it fire would not have helped either — during a `Reset` the power-up it reads is the one
from *before* this generation run, because `PlaceScreenObjects` calls `GetPotentialLocations`
before `PlacePowerUpAndPickUp` assigns the new one, so a live guard would have excluded a tile
chosen by the previous game. What actually keeps the two apart is `PlacePowerUpAndPickUp` removing
the power-up's location from the list before drawing one for the pick-up.

The fourth, `SoundEffectEngine`'s commented-out spin-wait, had already gone with the timer rework
in `9a490e5`. The review carried it forward stale.

## Phase 3 — time, randomness and threads *(landed)*

**3.1 — `CollisionDetection` statics to locals** (`eaaa1f0`). Eight `private static` fields held
eight locals of one pure method, which is why test parallelisation was off — and a latent bug in
the game the moment anything touches collision off the update thread. Parallelisation is back on
and the `AssemblyInfo.cs` whose only purpose was to disable it is gone. `_distanceX`/`_distanceY`
were each assigned and immediately reassigned by an `if/else` computing `Math.Abs`, and the method
ended `if (a && b) return true; return false;`. **The integer division stayed**: `Check` compares
against `obj1W / 2 + obj2W / 2`, so it is not `Rectangle.Intersects`.

**3.2 — the background rumble's timer** (`69b6d4a`). It drew from the same `System.Random` a dozen
collaborators use every frame; it uses `Random.Shared` now and the sound engine no longer takes
one, so `ResourceManager` does not either. It also called `PlaySound` and `GetSoundState` from the
callback, walking lists the game thread rewrites in `KillSounds` — surviving only because
"BackgroundSound1" is registered `Single`. The callback now sets a flag and re-arms, and a new
`SoundEffectEngine.Update`, called once a frame from `GameEngine`, does the playing.

**3.3 — game time is counted in logic frames** (`8b8ad66`). The frame rate was private to
`GameHost`, which is *why* the places that wanted to wait a second reached for the clock instead;
it is `LogicClock.FramesPerSecond` now. Converted: the energy drain, the two one-second booth and
Cheops read delays, the continual particle effects' duration, and play time — which now measures
time the game actually ran for rather than time that passed. Multi-instance sound age is
`Environment.TickCount64`: it only answers which of two instances started first.

`ContinuousParticleEffect.TriggerPeriod` went with them — **nothing ever set it**, so four of the
nineteen `DateTime.Now` calls were in branches that could not run. The one remaining call is the
world generator's seed, which is what a seed from the clock is for.

**3.4 and 3.5 — one input snapshot a frame** (`41173d8`). `ToggleKeys` was two mutable static
`Keys` fields, the last global state in the game. It is an instance owned by `GameHost`, which is
the thing that decides when a logic frame happens. While it is an instance it may as well hold the
frame's snapshot, which is the same problem's other half: `Keyboard.GetState()` was called three
times in `ToggleKeys`, five more in `CustomControl` and another fourteen through the engine, the
screens and the nasties, so two reads could disagree inside one frame. There are exactly two polls
left in the game and both are on the same line of `ToggleKeys.Update`.

**Nothing static and mutable is left in the game.**

## Phase 4 — break up the god classes *(landed)*

Four classes held 7,000 of the game's 22,436 lines when this was written. The measure set for the
phase was `DECOUPLING.md`'s: **if nothing becomes testable that was not testable before, the split
earned nothing and should be reverted.** By that measure it earned 82 tests, 65 to 147.

| Class | Before | After | What came out |
| --- | --- | --- | --- |
| `Blob` | 2,218 | 2,055 | `UnstickProbe`, `Inventory` |
| `GameScreenCollection` | 1,938 | **550** | `ScreenCollisionMap`, `WorldPopulator`, `ScreenRenderer` |
| `Nasty` | 1,646 | **819** | `NastyBrain`, `NastyMovementState`, `MoveType` |
| `GameEngine` | 1,227 | **961** | `LoadingScreen`, `GlobalHotkeys`, `TestModeKeys` |
| `StatusPanel` | 547 | **255** | `PlayerStats`, `PowerUp` |

**4.1 — `PlayerStats` out of `StatusPanel`** (`057bdc1`). The split settled four dependencies
rather than one: `Blob`, `CoreElements`, `GameOverScreen` and `HiScoreScreen` each held a
`StatusPanel` and wanted nothing from it but the stats, so all four now name what they depend on.
`AccessBoothScreen` and `CheopsScreen` still hold the panel, because `ScanSlot` really is a HUD
animation.

What made it testable was not an interface. `PlayerStats` takes the real sound engine, and a sound
it has never been given is a no-op returning `Guid.Empty`, so a test hands it a `SoundEffectEngine`
over an empty `ContentManager`. `DrawEnergy` and `TakeALife` return what they decided rather than
acting on a blob, so the class never learns what a blob is. *21 tests.*

**4.2 — `ScreenCollisionMap`** (`312fd6d`). It needs no content: the screen records and the tile
collision data are plain arrays, and **every tile sheet in the game is registered at
`TileMetrics.TileWidth` by `TileHeight`**, so asking a sprite sheet for its frame size was the same
answer the long way round. Same for the `CollAreaWidth`/`CollAreaHeight` constructor parameters,
which `GameEngine` was passing in as the `CollisionCell` constants.

Three things the move turned up. `_detectedCollisionMap` was filled on every `CheckCollision` call
— several times a frame per mover — and read by nothing. The six places that add a nasty-only cell
each carried the same *"Random image that's meaningless"* comment against the same call; that is
one `AddNastyOnlyCell`. And the spike and booth cell lists were written out longhand where they are
two nested loops over the same cell ranges. *22 tests, on screens built by hand so a test says what
it needs — one solid tile, one spike, one tunnel mouth — rather than hunting the shipped map.*

**4.3 — `WorldPopulator`** (`6d4fbd4`). The one thing that would have dragged the graphics in
behind it is `CoreElements`; `Populate` takes the element numbers as a list instead.
`GetPotentialLocations`, `GetPreferredLocation`, `CheckAccessCodeValidity` and the private
`GetCoordsOfTiles` overload moved with it, world generation being their only caller — and that
overload was building a list of `Point` only so `AssignAccessBoothCode` could ask whether it was
empty, which is now a `HasTile` that stops at the first one.

Everything is behaviour for behaviour **including the order the random numbers are drawn in**,
which is what makes a seed mean the same world it did before. *16 tests: same seed same world, the
ceilings hold, codes have no repeated digit, nothing lands on a tile that already means something —
and the draw is pinned as stopping one short of the key and the access card, which is the defect
6.1 fixes.*

**4.4 — `NastyBrain`** (`5448a86`). `CopyFromSource` is why this was worth doing: seventeen fields
assigned by hand, two of them twice, out of a class with about thirty-five, so anything added
afterwards was silently dropped at every screen swap and nothing said so. The movement half is one
assignment of a `record struct` now, and what is inside that struct is the answer to which state
carries. **The state that deliberately did not carry stays outside it and is documented as starting
fresh**, which preserves the old behaviour exactly — the difference is that it is a decision written
down rather than a list someone might not have finished.

Two things the tests caught while being written:

*Moving before reconsidering is load-bearing.* `Nasty.Update` called `ReleaseAttractionIfStuck`,
`MoveNasty`, `UpdateFrameOfMind` in that order. Reordering it made a core guardian come out facing
the wrong way, because the movement methods read what the step just walked into.

*`Direction`'s default is `Up`, not `None`* — `None` is last in the enum, because
`RandomStraightDirection` and `RandomDiagonal` index the first and second four. A brain that has
not been `Reset` is facing up rather than nowhere, which is why every nasty is `Initialise`d before
it runs.

*12 tests, and the one this split was for is that a chasing nasty walled in gives up chasing after
a quarter of a second. "Nasties get stuck" is the thing `ReleaseAttractionIfStuck` exists for, and
it is now reproducible in a test rather than only in a game.*

**4.5 — `LoadingScreen`, `GlobalHotkeys` and `TestModeKeys`** (`56fe4ba`). The loading screen keeps
its two decisions as return values rather than events: `FadeUp` says when the content load may
begin, `WaitForAKey` says when the game may start, and the engine decides what those mean because
that is the engine's business. The artwork swap tells whoever is drawing through a callback the
engine supplies rather than by holding the engine — the reference `DECOUPLING.md` removed, and not
worth reintroducing for one line. Both the call to the debug keys and their construction are
`Conditional`, so a release build still has no path to any of it.

One tidy worth naming: the five in-play screens were listed three times in three slightly different
shapes — twice as a chain of ORs and once as a pattern — and are one array tested once.
*No tests, and none expected; the plan called this "modest, mostly clarity" in advance.*

**4.6 — `ScreenRenderer`** (`28d795d`). The last of `GameScreenCollection`'s seven jobs. The
key-door frame was the one genuinely mixed method — it drove the force field's emitters and set the
collision tile types in the same sequence — and the two no longer interleave, the tile loop having
sat between the dim pass and the bright one without touching either. *No tests, and none expected:
drawing needs a graphics device. What it buys is the place 5.1 goes.*

**4.7 — `UnstickProbe` and `Inventory` out of `Blob`** (`90e7910`). The two parts of `Blob` that are
arithmetic rather than drawing, which by this phase's measure are the two worth taking. The probe
is a question now rather than a command — given where the blob is, where should he be — and the
blob does the moving.

**The other three clusters are deliberately not taken.** `BlobWeapon`, `TunnelTraversal` and
`BlobDeathSequence` are each mostly particle effects, sounds and drawing wrapped around two or
three counters, so extracting them would move lines without making anything testable, and this
phase says in its own words that such a split has earned nothing. `Blob` is still 2,055 lines and
its 110 fields are still the real problem. *11 tests, including the two rules that make the
inventory a puzzle rather than a bag.*

### What phase 4 did not settle

`Blob` is the one god class still standing, and the reason is worth writing down rather than
leaving for the next reader to rediscover: its remaining bulk is not separable state, it is one
state machine that draws itself. Splitting it needs the update logic and the draw code prised
apart first, and that is a different task from moving four field clusters into four files.

## Phase 5 — performance *(landed)*

Done after 4.6, as planned: the renderer split is where most of this lived, and doing both at
once would have made the diff unreadable.

**5.1 — the tile layers are batched** (`4260cb1`). Both layers opened and closed a `SpriteBatch`
per tile, so the two of them issued up to 144 draw-call flushes a frame to draw at most 96
sprites. They issue three.

Batching reorders work, so three orderings had to be checked rather than assumed. Lower tiles
never overlap each other, being a grid of distinct cells. The animated patches are a second pass
now, and every animated sprite is registered at most one tile big, so none can be covered by a
base tile drawn later. The upper layer is two passes because the patches and the tunnel entrances
come off different sheets — and **every row offset in `UpperLayerPatchFor` is 0 or 1, never
negative**, so a tunnel entrance sharing a cell with a patch was always drawn after it anyway.

The new batches are `Deferred`, which issues the draws in call order. That is the point:
`Immediate` would have flushed per sprite and undone the batching.

**5.2 — nothing sorts by a constant any more** (`393b360`). **Nothing in the game sets a layer
depth** — every `SpriteBatch.Draw` in `SpriteCollection` passes nought — so `BackToFront` at 46
sites was a sort by a constant key. That costs a sort and leaves the order among equal keys
unspecified, because MonoGame sorts with `Array.Sort`, which is not stable.

Not hypothetical: `Nasty.DrawLiveNasty` draws a smoking nasty **three times at the same position
in one batch**, in grey, red and green, with no depth between them. Which colour ended up on top
was unspecified. It is now the last one drawn, which is what the code reads as meaning.

Of `SpriteCollection.Begin`'s 39 callers that named a mode, 37 asked for `BackToFront` and 2 for
`Immediate`. Neither was wanted, so **the overload that takes a mode is gone** and neither can be
asked for again.

Safe to sweep because a scan for a `Begin` opened while another was still open found no genuinely
interleaved batches — only `if`/`else` branches sharing one `End`, and two overload declarations.
Had any existed, `Immediate` → `Deferred` would have reversed the order two batches reached the
device in.

**5.3 — one lookup per sheet per method** (`fe10a3a`). 261 `GetSpriteCollection` calls down to
143; `CoreElement.Draw` alone made 15 of them for four sheets. `ColourAnimationEngine`'s two
passes were the only hot ones, resolving the sheet four times per animated tile inside three
nested loops — and reading the x and y step off the sheet's own `SpriteWidth`, the upper pass
reading it off the *lower* sheet, which is the same number only because every tile sheet is
registered at `TileMetrics.TileWidth` by `TileHeight`. Those are the constants now.

**The plan oversold this item and the record should say so.** A short-string hash and a dictionary
probe, 261 times a frame, is a few microseconds — it is not what would stop the game running on a
Raspberry Pi, and 5.1's draw calls were. What it is actually worth is that
`_resources.GetSpriteCollection(_graphicsSet.Names.Pickups)` written three times in four lines is
noise.

### What phase 5 was not able to verify

None of this has been seen running. The build is clean, the 147 tests pass and the game boots and
loads, but the three changes are all to *drawing*, and the one thing that would settle them is
looking at the screen. Driving the game from a script got as far as the options screen —
`System.Events` can hold a named key like space long enough for the 60 Hz poll to catch it, but
not a digit, and the options screen needs `0` — so **the main game screen has not been looked at
since the renderer changed.** Someone should play a screen or two and check the tiles, the
upper-layer patches over the lift shafts and booths, the tunnel mouths, Olly, and a smoking nasty.

## Phase 6 — features and finish *(6.3, 6.4 and 6.5 landed; 6.1 and 6.2 deferred by decision; 6.6 open)*

Nothing depends on these, which is why they are last rather than because they do not matter.

**6.1 — place the key and the access card.** *Not done, deliberately.* 4.3 has landed, so the
seeded, tested `WorldPopulator` this goes in now exists and nothing blocks it technically. What is
open is the placement algorithm itself, which is being thought about separately — see *The key and
the access card* below for what the map actually constrains. **The two temporary lines in
`Blob`'s constructor stay until it is, because deleting them without replacing them makes the game
unwinnable.**

**6.2 — persist hi-scores.** *Not done, deliberately.* The original game never remembered them
either, so this is a new feature rather than a defect, and it may or may not be wanted. Left as
written: `HiScores` hardcodes eight names in its constructor, `AddScoreToTable` writes only to
memory, and there is no read or write to disk anywhere in the solution. The name-entry screen is
already fully built, so the work is a file format and two calls.

**6.3 — the `object? param` channel is four named changes** (`314a9d4`). It carried four unrelated
meanings, told apart by a null check and an unchecked `(bool)` cast.

The overloading was worse than four meanings in one parameter. **Two call sites were passing
`true` and one `false` purely to be non-null**, which is exactly why the booth's genuine
true-or-false could not be distinguished from them — the booth's `bool` suppressed the screen fade
as a side effect of being a `bool` at all. `ScreenChange` is a closed hierarchy of the four with a
private constructor: `FadeIn` at ten call sites, `KeepPicture` at six, `AccessCode` and
`FinalScore` at one each, which is the whole channel.

**6.4 — the lookup table, three records, six handler names** (`ea37940`).

`GetFriendlyName` is a `switch` expression and `ToggleKeys` is 276 lines to 141. **Two oddities in
that table are kept and now have tests saying so**, because both are what the define-keys screen
shows today: `Keys.Up` has no entry at all, so it reads "Up" beside DN, LF and RT; and four labels
are used by two keys each — RT for Right and Enter, BS for both browser buttons, CL for CapsLock
and OemClear, a full stop for Decimal and OemPeriod — so those pairs are genuinely
indistinguishable in the two characters the screen has.

`TileDetected`, `SecondLayerTile` and `ResetTile` are records. **`HiScore` and `KeyLockDoor` are
not, though this plan listed them**: both are mutated after construction — `HiScore` has its name
typed into it and is built by object initialiser, `KeyLockDoor` accumulates collision tiles — so
neither has the shape the item describes, and giving `HiScore` value equality would change how a
list of eight scores behaves for no benefit.

The designer-era method names: **this plan named two and there are six.** The house style is
`PascalCase_Event` and nine handlers already follow it, so the six `camelCase` ones were renamed
to match rather than to anything new.

**6.5 — `GameEngine.Dispose` is idempotent** (`ea37940`). A guard, so a second call does nothing.
No finaliser and so no `GC.SuppressFinalize`: it holds no unmanaged handle of its own — the sound
timer is a managed object with its own — so a finaliser would be one more thing to get wrong for
no benefit.

**6.6 — stop the incremental build deleting the built content.** *Still open, and now better
understood.* Found while play-testing phase 4, and the only item here that stops the game running
at all.

*The symptom.* An incremental `dotnet build Starquake.sln` sometimes empties
`Starquake.Shared/bin/<Config>/net9.0/Content/`, and the heads' copies with it. The next launch
dies on the first asset it touches:

```
ContentLoadException: The content file was not found.
  Could not find file '.../Content/Graphics/BootScreen2.xnb'
```

*What was measured.* Twelve runs of rebuild-then-build: the default parallel build kept the
content three times and deleted it three; `-m:1` kept it six out of six. **`dotnet build
Starquake.sln -m:1` is a reliable workaround.**

*The mechanism.* MSBuild's `IncrementalClean` does the deleting — a `-v detailed` log of a failing
run names it — because it removes everything in the previous build's `FileListAbsolute.txt` that
the current build did not re-declare as a file write.

**But `IncrementalClean` is the symptom, not the cause, and an afternoon's attempt to fix it there
made things worse.** Setting `DisableIncrementalClean` on `Starquake.Shared` turned an
intermittent failure into one on every single build: with nothing deleting, the content still
never appeared, which proves the copy is being *skipped* rather than undone. The MGCB output in
`Content/bin/DesktopGL/` is intact and correct throughout — only the copy into `bin/` is missing.

*Where that points.* `MonoGame.Content.Builder.Task.targets` collects the built files into its
`ExtraContent` item with a **wildcard evaluated inside the `RunContentBuilder` target**
(`Include="%(ContentReference.ContentOutputDir)\**\*.*"`), and `IncludeContent` turns that into
the `Content` items the SDK copies. If that glob comes back empty, there are no items, so nothing
is copied *and* nothing is declared — and `IncrementalClean` then removes last time's copies as no
longer produced. Both observed failures follow from one empty glob. `Starquake.Shared`
multi-targets, so its two frameworks build concurrently and share MSBuild's process-wide
file-enumeration cache, which would explain why it is intermittent and why serial builds never
show it.

*That last paragraph is a hypothesis, not a finding.* The next person should take a binary log
(`dotnet build Starquake.sln -bl`) of a failing run and look at what `ExtraContent` actually
contains, rather than trust it.

*Candidates, now that the obvious one is ruled out.* Copy the content with a target of our own
that has explicit `Inputs`/`Outputs` and registers its results in `FileWrites`, rather than
relying on the upstream glob — bearing in mind the heads take their copies from
`Starquake.Shared` through the project reference, so it has to feed that too. Or build the content
under one target framework and have the other take it from there. `DisableIncrementalClean` is
**not** a candidate: it has been tried and it is worse.

## The key and the access card

The two lines at `Blob.cs:423` under the comment "Temporary test code to give Blob Access card and
Key" are the only `TODO`-class comment in 22,000 lines, and the obvious reading — a debugging
leftover to delete — is wrong. **They are a workaround for placement that was never finished.
Deleting them without replacing them makes the game unwinnable.**

### Why the items never spawn

Pick-up types are drawn with `r.Next(28)` in both branches of
`GameScreenCollection.PlacePowerUpAndPickUp`. That yields 0 to 27. `Collectables.AccessCard` is
**28** and `Collectables.Key` is **29**, so the range stops exactly one short of both.

The rest of the scaffolding is already there. `CollectScreensWithCheopsAndCoreElements`
(`GameScreenCollection.cs:1885`) *explicitly excludes* pick-ups of type `AccessCard` or `Key` from
the core-element list — a guard written in anticipation of placement that was then never written.

### What the constraint actually costs

The requirement is that the key must not be placed anywhere that can only be reached by passing
through a door the key itself opens. Measured against the shipped map data:

| | |
| --- | --- |
| Gated screens | Exactly **3** of the 512 screens contain an `AccessForceField` tile (419): **190** (col 14, row 11), **452** (col 4, row 28), **486** (col 6, row 30) |
| Start and core | The blob starts on screen 8 (`ShipWreckScreenNumber`); the core is 199 |
| Existing marker | `PreferredCoreLocation` (tile 392) is carried by **19** screens, read by `GetPreferredLocation`, and already authorable in ScreenEditor — `TileCatalogue.cs:47` lists it as `"CORE"` |

**What could not be measured, and why.** A flood fill over the screen grid from screen 8, treating
the three gated screens as walls, reports nothing stranded behind them. That result is not
trustworthy: the model assumes every grid-adjacent screen is walkable, and screen connectivity
here is determined by the tile layout at each edge, not by adjacency. A real answer needs
tile-level traversal — whether the blob can actually walk, fall, jump or ride between two
neighbouring screens — and the game has no such model.

### The recommendation: mark the screens, do not compute them

Three gated screens is a small constraint, and the game already has the mechanism for exactly this
kind of hand-authored hint. So rather than build a traversal solver:

1. **Add a marker tile.** Call it `KeySafeLocation`, as a sibling to `PreferredCoreLocation` in
   `ActionTiles`. Add it to `TileCatalogue.cs` so it appears in the editor palette, and to
   `TilesNothingMayBePlacedOn` so it never renders as scenery.
2. **Mark the safe screens by hand in ScreenEditor.** The map is known, the tool exists, and the
   result is verifiable by eye in a way a solver's output never is. A dozen or two marked screens
   is plenty for the key to feel scattered.
3. **Fix the `PickUps[0]` hazard first.** `MatchSomeCoreElementsToTheCore` writes
   `screensContainingCoreElements[a].PickUps[0].Type`. Once the key can be a pick-up it can sit at
   index 0 and be silently overwritten by a core element. 0.3 guards the bounds; this needs the
   type check too.
4. **Place both items explicitly** in `WorldPopulator`: pick one marked screen for each, rather
   than widening the `r.Next(28)` draw and rolling for them. Guaranteeing exactly one of each is
   better than leaving it to chance anyway.
5. **Then delete `Blob.cs:423-424`**, and add a test: for any seed, the world contains exactly one
   key and one access card, and both are on marked screens. That test is only possible because of
   0.4, which is why a one-line change sits in phase 0.

**If you would rather compute it.** The honest version needs a per-screen edge model — for each
screen, which of its four edges the blob can leave by, derived from the collision map rather than
the tile images. With that, reachability is an ordinary graph search, and the same model would
also answer whether the core is reachable at all in a given generated world, which nothing checks
today. That is a week or two rather than a day, and it belongs after 4.2 has extracted the
collision map.

## What this plan deliberately does not do

**Split `MainGameScreen`.** It uses every actor because it *is* the main game — it drives the
blob, the nasties, the bridges, the lifts, the animations and the effects every frame. Its
alternatives are an eighteen-parameter constructor or a bag object existing solely for it, and
both are worse than holding the engine. If it should be smaller, that is splitting a 642-line
class on size, which is a different task and should not be smuggled in here.

**Introduce a DI container**, for the reasons `DECOUPLING.md` already gives.

**Touch ProjectMercury**, beyond excluding it from the analyser settings in 1.1.

**Touch `BlobDeathAnimation.FragmentSpriteNumbers`, `SharedLibs`, or the `WINDOWS` and
content-build wiring.** All three are settled decisions with the reasoning written down —
`REFACTORING.md` for the first two, `BUILD.md` for the third.

**Add tests as a goal.** Tests are the measure for phase 4, not the deliverable. A split that adds
no testable surface has earned nothing.

## How the findings were measured

So they can be re-derived rather than taken on trust.

**The build state and the test count** by running `dotnet build Starquake.sln` and
`dotnet test Starquake.Tests/Starquake.Tests.csproj`.

**The 35 style violations** by building `Starquake.Shared` with
`-p:EnforceCodeStyleInBuild=true -p:NoWarn=1591 -t:Rebuild` and de-duplicating across the two
target frameworks. `NoWarn=1591` is needed because `GenerateDocumentationFile` floods the output
with missing-doc warnings that are not the point. Since 1.1 a plain `dotnet build Starquake.sln`
reports them, as errors.

**The test count** by running the tests: 65 before phase 4, 147 after it - which is the measure
that phase set for itself - and 159 after phase 6.

**The line and field counts** with `wc -l` and a `grep -cE` over the private declarations, and the
methods over 80 lines with the `awk` script in `REFACTORING.md` — including its known false
positives, where a method followed by a run of get-only properties is credited with their lines.

**The map figures** — three force-field screens, nineteen `PreferredCoreLocation` screens, the
screen numbers — by deserialising `Starquake.Shared/Data/SQScreenData.dat` directly with
MessagePack in a throwaway console project referencing `SharedLibs`, reading the length-prefixed
`GameScreenData[]` the way `ResourceManager.LoadDataResources` does. Not inferred from the code.

**The orphaned doc comments** with the `awk` one-liner in 0.5.
