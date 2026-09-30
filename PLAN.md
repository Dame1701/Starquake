# Fixing what the review found

**Nothing has landed yet.** This is a plan, written the way `REFACTORING.md` and `DECOUPLING.md`
were written before their stages landed, and it should be rewritten the same way — each phase's
section replaced with what the work actually decided, as it lands, rather than left as the
intention.

It covers a full review of the game carried out against `ef5a6a3`. Every finding is here; the
ordering is the point of the document. `ProjectMercury` was out of scope throughout.

The starting state is good and worth naming, because it sets the bar for everything below. The
solution builds at 0 warnings and 0 errors, the 65 tests pass, there is no `null!` or
`#pragma warning disable` anywhere in our own code, only six methods exceed 80 lines, and there
are nine lines of commented-out code in the whole game. What is left is class-level shape, a
short list of real defects, and a standard that is written down but never checked.

## How this is ordered

Three rules, applied in this priority. Where they conflict, the earlier rule wins.

**Things that are wrong for the player come first.** A crash or a bricked control scheme outranks
any amount of internal tidiness, and each of those is hours of work rather than days.

**The safety net goes in before the structural work, not after.** Phase 1 makes the build enforce
the standard `.editorconfig` already declares. Done afterwards, the phase 4 splits can quietly
reintroduce the 35 violations and nothing would say so.

**Subtract before you move.** Every line deleted in phase 2 is a line phase 4 does not have to
relocate, re-test or argue about.

Phases 0 to 3 are small and independent — about two working weeks in total, and any of them can
ship alone. Phase 4 is the expensive one and is where the god-class question is actually settled.
Phases 5 and 6 are optional in the sense that nothing depends on them.

Verification for every item is the existing bar:

```sh
dotnet build Starquake.sln                          # must stay at 0 warnings, 0 errors
dotnet test Starquake.Tests/Starquake.Tests.csproj  # must stay green
```

## Phase 0 — stop the bleeding

Player-visible wrongness and one latent crash. All of it is small, none of it depends on anything
else, and each item wants its own commit and its own play-test.

**0.1 — the three world-edge bounds tests.** `Blob.ChangeScreenOffRightEdge` (`Blob.cs:1208`),
`Blob.ChangeScreenOffBottomEdge` (`Blob.cs:1259`) and `GameEngine`'s NumPad2 test key all guard
with `ScreenNumber + n > WorldMap.ScreenCount`. `ScreenCount` is 512 and the screen array is
exactly that long, so the last valid index is 511 — which `WorldMap` already names
`LastScreenNumber`. The right-edge test therefore permits 512; the bottom-edge test adds
`ScreensAcross`, so it permits up to 527. Both dereference the array on the next line. The
matching left and top tests use `>= 0` and are correct, and `StepBetweenScreens`'s NumPad6
already uses `< WorldMap.LastScreenNumber`, so these three are the odd ones out rather than a
convention. Whether it is reachable depends on the level data at the map's right and bottom
edges, which is exactly why a one-character change here wants its own commit. Carried over from
`REFACTORING.md`'s follow-ups, re-ranked because it is a crash rather than a wart.

**0.2 — decide the joystick option.** `OptionsScreen.cs:66` offers *Joystick 0* as control option
1. Selecting it binds Up, Down, Left, Right and Fire to `Keys.None` and sets
`CustomControl.UseJoyPad = true` (`GameEngine.cs:283`). `UseJoyPad` is read by nothing.
`CustomControl`'s five input methods read the keyboard and nothing else — the DirectInput code
that would have backed them is commented out at `CustomControl.cs:83`. A player who picks option
1 can pause and quit and do nothing else, with no message saying why. Either remove the option,
or implement it: `GamePad.GetState(PlayerIndex.One)` works on DesktopGL through SDL and does in
about fifteen lines what the commented block was reaching for. The decision determines what 2.1
deletes, so take it first.

**0.3 — guard `MatchSomeCoreElementsToTheCore`** (`GameScreenCollection.cs:1919`). It draws
`matchNumber` from `Next(2, 6)` and then indexes `screensContainingCoreElements[a]` for `a` up to
4 with no `Count` check, and picks from `tempElements` without checking it is non-empty. Safe
today only because a hundred pick-up screens are generated, not safe by construction. It also
needs a type check before phase 6 — see *The key and the access card*.

**0.4 — seed the world generator.** `GameScreenCollection.Reset` does
`var r = new Random((int)DateTime.Now.Ticks)` at line 1694 while the class already holds
`_randomNumberGenerator` from its constructor (line 212). Use the injected instance. This looks
cosmetic and is not: it is what makes 4.3 testable and what lets a bad world layout be reproduced
from a bug report, and the phase 6 test for key placement cannot be written without it.

**0.5 — three orphaned doc comments.** Each declaration below carries two stacked `<summary>`
blocks; the compiler keeps the second and the first is silently swallowed.

| Where | What happened |
| --- | --- |
| `Blob.cs:30` | `BlobMaxItemsHeld`'s summary sits above `TunnelEntrySpeed`, so the constant it describes has no documentation at all |
| `GameEngine.cs:213` | A summary describing the deleted `GetInstance()` singleton is now attached to `GameScreens`, documenting something that no longer exists |
| `Nasty.cs:910` | The same summary twice on `UpdateFrameOfMind`, differing only in line wrapping |

All three look like a member sort that moved declarations and left their comments behind. Three
lines of `awk` find them, and it is worth re-running after any future sort:

```sh
for f in $(find Starquake.Shared SharedLibs -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*"); do
  awk -v F="$f" '/\/\/\/ <\/summary>/{p=NR} /\/\/\/ <summary>/{if(NR==p+1) print F":"NR}' "$f"
done
```

## Phase 1 — the safety net

`.editorconfig` opens by saying its severities exist "so Rider, `dotnet build` and
`jb inspectcode` all report the same thing and the cleanup we did does not drift back a file at a
time."

**`dotnet build` does not report them.** Analysers with the `IDE` prefix only run during a build
when `EnforceCodeStyleInBuild` is set, and nothing sets it. There is no `Directory.Build.props`.
The five rules the file declares as kept-at-zero have never been checked by the build, and the
game code has 35 violations of them.

**1.1 — add `Directory.Build.props`** setting `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`,
`Nullable`, `ImplicitUsings` and `LangVersion` once for the solution. `ImplicitUsings` is
currently on in `SharedLibs`, `Starquake.Tests` and both ProjectMercury projects and off in
`Starquake.Shared` and `ScreenEditor`, which is why the game files open with fifteen-line `using`
blocks and the others do not. Exclude ProjectMercury — it has eight findings of its own and is
out of scope, and it already has its own `.editorconfig` section to hang that off.

**1.2 — clear the 35.** Reproduce them with:

```sh
dotnet build Starquake.Shared/Starquake.Shared.csproj -f net9.0 -t:Rebuild \
  -p:EnforceCodeStyleInBuild=true -p:NoWarn=1591
```

| Rule | Count | What they are |
| --- | --- | --- |
| IDE0060 | 22 | Unused parameters — `ScreenEffectEngine`'s `screenWidth`/`screenHeight`, `GameEngine`'s `parentWindow`, `ColourAnimationEngine`'s `screenFade` three times, `SpriteCollection.Begin`'s `gameTime` |
| IDE0051/0052 | 10 | Unused private members — `Blob.FireFadeStep`, `Blob.BlobStartXPos`/`BlobStartYPos`, `GameScreenCollection.ItemTileXOffset`/`ItemTileYOffset`, and `CustomControl`'s four dead joypad members |
| IDE0044 | 3 | `SpriteCollection`'s three writable-but-never-written fields |

Five of those are named, documented constants nobody reads. Expect a handful to be real questions
rather than deletions rather than a mechanical sweep — `ScreenEffectEngine`'s unused dimensions in
particular. `ScreenEditor` has two and is otherwise clean, which is itself evidence that the
standard holds when it is checked.

**1.3 — add `Directory.Packages.props`.** MonoGame 3.8.4.1 is pinned in five project files and
MessagePack 3.1.10 in three. Central package management makes the next MonoGame bump one line.

**1.4 — add CI.** One GitHub Actions job running `dotnet build` and `dotnet test` across the three
heads. There is no CI of any kind today, so "0 warnings, 65 tests green" is held by discipline
alone, and discipline is exactly what a `git bisect` six months from now cannot verify.

**Do not start phase 2 until CI is green on `main`.** From here on every later phase is checked
by the build rather than by hand.

## Phase 2 — subtract

Pure deletion, no behaviour change. Doing it now leaves phase 4 with roughly 400 fewer lines to
relocate and one dependency cycle fewer to reason about.

**2.1 — gut `CustomControl`.** About 120 of its 182 lines are the commented-out DirectInput block
and two dead analogue speed-band methods. Deleting them also removes its `Blob` field, dissolving
a `Blob` ↔ `CustomControl` cycle the decoupling work left standing. Worth noting before it goes,
as evidence of what unreferenced code hides: `SetBlobVSpeed`'s band chain has a hole — an input
between `VerticalDeadZone` (0.1) and `SpeedBand1` (0.2) matches no branch and falls through to the
final `else`, giving full speed. That is invisible only because nothing calls it. Depends on 0.2.

**2.2 — delete `IGameScreen.Success`.** Nine classes implement it and eight are `=> true`. Nothing
reads it through the interface. `AccessBoothScreen` is the only class with a real value, uses it
internally, and passes it on explicitly as `param` rather than relying on the property; its copy
becomes private.

**2.3 — fix `SpriteCollection.Clone`** (`SpriteCollection.cs:487`). It copies two `int[]` by
round-tripping them through `MessagePackSerializer` — a deep-clone-via-`BinaryFormatter` idiom
ported forward. `(int[])x.Clone()` would do it without the round trip, but neither is needed: the
constructor invoked on the line above already computed both arrays from the same texture's
dimensions, so the two lines serialise, deserialise and then overwrite identical values. The same
is true of the `_contentManager.Load<Texture2D>(_imageFilePath)` two lines further down. Delete
all three.

**2.4 — remove `[Serializable]` and the four `[NonSerialized]` attributes** from
`SpriteCollection`. `BinaryFormatter` was removed in .NET 9, so they are inert; the real
serialisation is MessagePack. The stray `using MessagePack;` goes with 2.3.

**2.5 — two dead field initialisers.** `Nasties._nastyCount = 1` (`Nasties.cs:53`) is overwritten
in the constructor at line 80. `GameScreenCollection._pickUpCount = 100`
(`GameScreenCollection.cs:211`) is never assigned at all — a `const` wearing a field's clothing,
which is the exact pattern stage 1 of `DECOUPLING.md` existed to remove.

**2.6 — delete `packages/`.** 21 tracked files, 5.7 MB of `MonoGame.Framework 3.2.99.1-Beta` and
`3.4.0` binaries from the `packages.config` era. Nothing references them.

**2.7 — rename `Temp/` to `Assets/`.** 197 tracked files, 20 MB. It is the master art and sound
source the content pipeline builds from, not temporary anything. A folder called `Temp` is one
confident `rm -rf` away from gone. Update `Content.mgcb` and `BUILD.md` with it.

**2.8 — take the four carried-over follow-ups** from `REFACTORING.md`, each of which already
carries a `///` saying so: `Blob.LayJumpPlatformOrJump`'s empty `if (temp == -1) { }` with a
comment about `-1` above a test for `-1000`; `BuildScreenCollisionMap`'s `screenChanged`
parameter, whose work is discarded on the next line; `GameScreenCollection.PowerUpIsInTheWay`
comparing a pixel position against a tile index and reading the wrong screen; and
`SoundEffectEngine`'s commented-out spin-wait labelled "Hack to make the sounds actually stop!".

## Phase 3 — time, randomness and threads

One theme: the game takes its time and its randomness from the wrong places. These are grouped
because they touch the same handful of files and because two of them block testing.

**3.1 — `CollisionDetection` statics to locals** (`CollisionDetection.cs:12`). Eight
`private static` fields hold what are plainly eight locals of one pure method. This is why test
parallelisation is switched off in `Starquake.Tests.csproj`, and it is the one place in the game
logic where two threads would silently read each other's arithmetic. The method is pure given its
arguments, so the conversion is mechanical. Two smaller things ride along: `_distanceX` and
`_distanceY` are each assigned and then immediately reassigned by the `if/else` below (they are
`Math.Abs`), and the method ends `if (a && b) return true; return false;`.

**Keep the integer division.** `Check` compares against `obj1W / 2 + obj2W / 2` with integer
division, so it is not `Rectangle.Intersects`, and a rewrite that reaches for the framework
method will change how odd-width sprites collide.

**3.2 — give the sound timer its own `Random`** (`SoundEffectEngine.cs:157`).
`PlayPeriodicSound` runs on a thread-pool timer callback and calls `randomNumberGenerator.Next`
on the same instance `GameEngine` constructs at line 201 and hands to about a dozen collaborators
that use it every frame. `System.Random` is not thread-safe; concurrent use can corrupt its state
so that it returns zero indefinitely. The same callback also calls `PlaySound`, which walks
`_singleInstanceSoundEffectInstances` and drives `SoundEffectInstance` objects the game thread
stops in `KillSounds` on every screen change.

That second half survives today *only* because "BackgroundSound1" is registered
`SoundEffectInstanceType.Single` in `ContentManifest.cs:271`. Change that one line to `Multi` and
the callback starts calling `List.Add` on a list the game thread enumerates. So: a dedicated
`Random` (or `Random.Shared`), and a flag the update loop acts on rather than the callback
touching sound instances directly.

**3.3 — replace the 19 `DateTime.Now` calls.** `GameHost` exists specifically to turn a variable
render loop into exactly sixty logic frames a second, and these ignore it: energy drain
(`StatusPanel.cs:410`), the six particle trigger periods in `ContinuousParticleEffect`, the
one-second booth and Cheops read delays (`AccessBoothScreen.cs:120`, `CheopsScreen.cs:293`),
multi-instance sound age (`SoundEffectEngine.cs:399`), and play time (`MainGameScreen.cs:172`).

Three consequences. The timings drift against the game's own frame rate. `DateTime.Now` is local
time, so a DST step or an NTP correction can turn `_screenReadWait + 1s < DateTime.Now` into an
hour-long wait. And it pays a timezone conversion on every call inside the update path. Frame
counts are the right unit for the game logic; `Environment.TickCount64` will do for the sound
ages.

**3.4 — make `ToggleKeys` an instance** (`ToggleKeys.cs:16`). Two mutable `static Keys` fields,
the last global state in the game — it survived the deletion of `GameEngine.GetInstance()` in
stage 2 of `DECOUPLING.md`. It requires `Update()` to be called exactly once a frame from exactly
one place, and nothing in the type system says so.

**3.5 — one `Keyboard.GetState()` per frame.** It is called five times in `CustomControl` and
three in `ToggleKeys`, so two of those reads can disagree within a single frame. Natural to do
alongside 3.4.

## Phase 4 — break up the god classes

Four classes hold 7,029 of the game's 21,998 lines:

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

**4.2 — `ScreenCollisionMap` out of `GameScreenCollection`** (about 430 lines).
`BuildScreenCollisionMap`, the six `Add*CollisionCells` methods, `CheckCollision`,
`CheckRectangles`, `CheckTileByObject`, `StepAllowedBy`, `ClampToScreenBounds`. *Payoff: the
thing most likely to break when level data changes becomes testable.*

**4.3 — `WorldPopulator` out of `GameScreenCollection`** (about 250 lines). `Reset` and the six
placement methods. Pure, given the seeded `Random` from 0.4. *Payoff: world generation becomes
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
with missing-doc warnings that are not the point.

**The line and field counts** with `wc -l` and a `grep -cE` over the private declarations, and the
methods over 80 lines with the `awk` script in `REFACTORING.md` — including its known false
positives, where a method followed by a run of get-only properties is credited with their lines.

**The map figures** — three force-field screens, nineteen `PreferredCoreLocation` screens, the
screen numbers — by deserialising `Starquake.Shared/Data/SQScreenData.dat` directly with
MessagePack in a throwaway console project referencing `SharedLibs`, reading the length-prefixed
`GameScreenData[]` the way `ResourceManager.LoadDataResources` does. Not inferred from the code.

**The orphaned doc comments** with the `awk` one-liner in 0.5.
