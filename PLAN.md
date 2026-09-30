# Fixing what the review found

**Phases 0 to 3 have landed.** This document was written the way `REFACTORING.md` and
`DECOUPLING.md` were written before their stages landed, and each finished phase's section has
been replaced with what the work actually decided, as it landed, rather than left as the
intention. Phases 4 to 6 are still the plan.

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

## Where that leaves the four god classes

The numbers below phase 4 quotes were measured against `ef5a6a3`. Phases 0 to 3 moved them a
little, and not always down — the input snapshot has to be threaded to fifteen constructors, which
is what replacing global state with a dependency costs:

| Class | Then | Now |
| --- | --- | --- |
| `Blob` | 2,218 | 2,210 |
| `GameScreenCollection` | 1,938 | 1,915 |
| `Nasty` | 1,646 | 1,642 |
| `GameEngine` | 1,227 | 1,233 |

Phase 4 is untouched by all of this, which is the point of having done it first: the deletions are
out of the way, the build now enforces the standard the splits could otherwise erode, and the
world generator is seeded so 4.3 has something to test against.

## Phase 4 — break up the god classes

Four classes hold 7,000 of the game's 22,436 lines. These figures were measured against
`ef5a6a3`; the table above shows where phases 0 to 3 left them.

| Class | Lines | Fields | What it actually does |
| --- | --- | --- | --- |
| `Blob` | 2,218 | 110 | Player state machine, six detection systems, firing, inventory, tunnel travel, death sequence, and all its own drawing |
| `GameScreenCollection` | 1,938 | 18 | Seven jobs — storage, collision building, collision querying, rendering, world generation, key-door state, Olly's eyes |
| `Nasty` | 1,646 | 35 | Lifecycle state machine, fourteen movement behaviours, and drawing |
| `GameEngine` | 1,227 | 29 | Composition root, frame loop, event broker — plus a loading screen and a hotkey handler |

**Size is the symptom, not the diagnosis.** 5 of 129 types are under test, and
`Starquake.Tests.csproj` explains why in its own header comment: a test needing a
`GraphicsDevice` does not belong there. That is not a testing problem. It is what happens when
update logic and draw code share a class.

**So the measure for this whole phase is the one `DECOUPLING.md` set for itself: if nothing
becomes testable that was not testable before, the split earned nothing and should be reverted.**
Every item below names its test payoff. Ordered smallest-payoff-first, so that the cheap ones
teach you something before the expensive ones.

**4.1 — `PlayerStats` out of `StatusPanel`** (547 lines). The class is named for a HUD widget and
owns the player's entire game state: `PlayerScore`, `BlobLives`, `EnergyLevel`, `FirePower`,
`Platforms`, the inventory reference, all eight power-up rules and the platform cost. That is why
`Blob` reads its supplies off the panel — the cycle `DECOUPLING.md` had to break by passing the
blob into `Update` rather than holding it. *Payoff: every scoring and power-up rule becomes
testable with no graphics, and `StatusPanel` goes back to drawing.*

**4.2 — `ScreenCollisionMap` out of `GameScreenCollection`** (about 400 lines).
`UpdateCollisionMap` (called `BuildScreenCollisionMap` before 2.8 merged the two), the five
`Add*CollisionCells` methods that survived it, `CheckCollision`,
`CheckRectangles`, `CheckTileByObject`, `StepAllowedBy`, `ClampToScreenBounds`. *Payoff: the
thing most likely to break when level data changes becomes testable.*

**4.3 — `WorldPopulator` out of `GameScreenCollection`** (about 250 lines). `Reset` and the six
placement methods, which no longer take a `Random` parameter — 0.4 gave them the field.
Reproducible from a seed, which is the whole of what makes this testable. *Payoff: world generation becomes
testable and reproducible — and this is where the phase 6 key placement lands.*

**4.4 — `NastyMovement` out of `Nasty`** (lines 847 to 1480). Fourteen behaviours, about 25 of the
class's 45 constants, `MoveType`, `Direction`, `MoveSpeed`, `_stepsToMove`,
`_lastCollisionDirection`. Needs the collision map and a `Random`, no graphics. Replace
`CopyFromSource` with a `record struct` that copies whole: it currently assigns `Direction` and
`_stepsMovedCounter` twice each, and copies 17 fields of a class that has about 35, so
`_birthTimer`, `_nastyStuckCounter`, `_spikeyPause`, `_firstMovement`, `_prevXpos`/`_prevYpos`,
`Smoking` and `_deathScale` are silently dropped across a screen swap. Whether each omission is
deliberate cannot be told from the code. *Payoff: "nasties get stuck" becomes reproducible —
`ReleaseAttractionIfStuck` and `_nastyStuckCounter` exist because it happens.*

**4.5 — loading screen and global hotkeys out of `GameEngine`.** Two clusters that are neither
composition root nor frame loop. The loading screen is `_bootScreen`, `_bootProgress`,
`_bootProgressBackground`, `_loadingScreenFade`, `_bootProgressBackgroundFade`, seven layout
constants, `DefineBootGraphics`, `FadeUpTheLoadingScreen`, `WaitToLeaveTheLoadingScreen`, the
content-status handler and about forty lines of `Draw` — it *is* a screen, and every other screen
in the game is its own class. The hotkeys are `TogglePauseIfAsked`, `ToggleFullScreenIfAsked`,
`ToggleBackgroundsIfAsked`, `SwitchGraphicsSetIfAsked`, `HandleTestModeKeys` and
`StepBetweenScreens`, roughly 150 lines wanting the session, the graphics set and the
presentation settings. Takes the engine under 1,000 lines. *Payoff: modest, mostly clarity — but
cheap.*

**4.6 — `ScreenRenderer` out of `GameScreenCollection`** (about 400 lines). All fourteen `Draw*`
methods, their nineteen sprite-index constants, the key-door force-field effects, and Olly's eyes
— a one-screen easter egg carrying six constants of its own. Leaves `GameScreenCollection` as the
screen array, `ScreenNumber`, `CurrentGameScreenData`, `GetGameScreen` and `SaveData`, which is
what its name has always claimed. Do it last of the three so 4.2 and 4.3 have already drawn the
seams. *Payoff: none directly — it is the residue. Combine with 5.1.*

**4.7 — `Blob`'s four clusters.** The method work here is already done; the problem is 110 fields,
and the clusters are legible straight off the field names:

| Cluster | Fields | Methods |
| --- | --- | --- |
| `BlobWeapon` | `_blobFire`, `_blobFireFade`, `_blobCanFire`, `_directionBlobFired`, `_blobFireSparks`, `_blobFireTrail` | `StartFiring`, `UpdateFire`, `PointBlankReach`, `StopFireEffects`, `DrawFire` |
| `BlobInventory` | `BlobInventory[]`, `_inventoryAccessed` | `AccessInventory`, `DropLastHeldItem`, `ShuffleInventoryAlong`, `PickUpItemUnderfoot` |
| `TunnelTraversal` | `_inTunnelLift`, `_distanceInTunnelLift`, `_blobTunnelLiftSoundId`, `_directionGoingThroughTunnel` | `RideTunnelLift`, `DetectLeftHandTunnel`, `DetectRightHandTunnel` |
| `BlobDeathSequence` | `_blobOverloadState`, `_blobScale`, `_blobAlarmSoundCounter`, `_teleportFadeLevel` | `UpdateOverloadingState`, `UpdateDyingState`, `DrawOverloadAlarmState`, `DrawOverloadFlare` |

Start with the unstick probe — `UnstickFromEdge`, `EdgeProbe`, `OppositeOf`, `PushAwayFrom`,
`IsInsideScenery`. It is already self-contained, four of its five methods are `static` or take
only a rectangle, and it is the one part of `Blob` testable today. `FireSurroundings`, the private
class already at the bottom of the file, is the shape to copy. *Most expensive, least urgent.*

## Phase 5 — performance

Deliberately after 4.6, because the renderer split is where these live and doing both at once
makes the diff unreadable. None of it matters on a modern GPU at 60 Hz — it is the difference
between running on a Raspberry Pi and not.

**5.1 — batch the tile draws.** Each screen is an 8 × 6 grid. `DrawLowerTile`
(`GameScreenCollection.cs:1379`) runs 48 times a frame and per tile does a dictionary lookup, a
cast, then a full `SpriteBatch.Begin()` → one `Draw` → `End()`. `DrawUpperTilePatch` (line 1506)
does the same 48 more times with two lookups each. A `Begin`/`End` pair flushes a draw call, so
the tile layers alone issue up to 5,700 flushes a second to draw at most 96 sprites — batching is
the entire purpose of `SpriteBatch`. Both layers draw from one sheet each, so hoisting `Begin`
and `End` out of the loops is one batch per layer.

**5.2 — the two sort modes.** `SpriteSortMode.Immediate` (4 sites) flushes on every `Draw`.
`BackToFront` (48 sites) sorts by layer depth, on batches of one sprite with no depth set.

**5.3 — resolve sprite sheets once.** 277 calls to `GetSpriteCollection(string)`, each a string
hash, a dictionary probe and an unbox. The sheets only change identity across the F8 art swap, so
`GraphicsSet` could hand out the resolved `SpriteCollection` rather than its name. `DrawPowerUp`
alone does three lookups for one sheet in four lines.

## Phase 6 — features and finish

Nothing depends on these, which is why they are last rather than because they do not matter.

**6.1 — place the key and the access card**, then remove the two inventory lines. Depends on 4.3.
See the next section.

**6.2 — persist hi-scores.** `HiScores` hardcodes eight names in its constructor and
`AddScoreToTable` writes only to memory, so the table resets on every launch. There is no read or
write to disk anywhere in the solution. The name-entry screen is already fully built.

**6.3 — replace the `object? param` channel.**
`IGameScreen.PerformPostScreenChangeActions(GameScreenType, object? param)` carries four
unrelated meanings: "do not reset the screen fade" (`MainGameScreen.cs:118`), "the access code was
correct" but only when the trigger is `AccessBooth` (`MainGameScreen.cs:155`, an unchecked
`(bool)` cast), a `HiScore` (`HiScoreScreen.cs:79`), and a `true` from `GameEngine`'s test keys
meaning the first of those. One record per transition, or a small closed hierarchy.

**6.4 — language tidying.** `ToggleKeys.GetFriendlyName` as a `switch` expression — 232 lines to
about 45, and the last method in the game over 80. `REFACTORING.md` left it alone deliberately;
the modern form is what changes the calculation. `TileDetected`, `SecondLayerTile`, `ResetTile`,
`HiScore` and `KeyLockDoor` are data carriers with a constructor and get-only properties, so they
are `record`s. And the two Visual Studio designer-era method names,
`Nasty.nastyDies_ParticleEffectComplete` and `Blob.blobTeleporterBeam_ParticleEffectComplete` —
nothing else in the codebase looks like that.

**6.5 — `GameEngine.Dispose` to the standard pattern.** No idempotence flag, no
`GC.SuppressFinalize`. It works because it is called once from `GameHost.Dispose`, and phase 1's
analysers will flag it.

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

**The line and field counts** with `wc -l` and a `grep -cE` over the private declarations, and the
methods over 80 lines with the `awk` script in `REFACTORING.md` — including its known false
positives, where a method followed by a run of get-only properties is credited with their lines.

**The map figures** — three force-field screens, nineteen `PreferredCoreLocation` screens, the
screen numbers — by deserialising `Starquake.Shared/Data/SQScreenData.dat` directly with
MessagePack in a throwaway console project referencing `SharedLibs`, reading the length-prefixed
`GameScreenData[]` the way `ResourceManager.LoadDataResources` does. Not inferred from the code.

**The orphaned doc comments** with the `awk` one-liner in 0.5.
