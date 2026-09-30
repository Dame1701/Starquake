# Making the code say what the comments say

**Stages 1 to 3 landed, and stage 4 all but its tail.** This document is a record of what the work
actually decided, the way `NULLABLE.md` is — each stage's section was rewritten as it landed rather
than left as the intention. What is left to do is listed at the end of stage 4, along with the
findings this work turned up and deliberately did not act on.

Twenty commits, `a7e3e8d` to `4935935`. The solution builds at 0 warnings and 0 errors throughout,
and the 40 tests added in stage 3 pass.

The goal is the clean-code one: fewer comments, because the code says it instead. In this
codebase that means one specific move, made many times — a long method whose `//` comments act
as its table of contents becomes several methods whose *names* are those comments.

Line numbers below were written as of `c8787e5` and have already drifted — stage 1 moved nothing,
but stage 2 onwards will. Every finding is also named by symbol, so re-locate by symbol and treat
the numbers as a hint.

## The rule it follows

**Only one kind of comment goes away.** The codebase has two, and they are not the same thing.

`///` doc comments explain *why*, and they stay. `TunnelTiles` explaining why the tile numbers
interleave, `GameScreenData` explaining why `ScreenTiles` is not nullable, `Blob`'s constants
explaining the overload sequence — a reader cannot recover any of that from the code, which is
exactly the test for whether a comment earns its place. Several were added deliberately and
recently (`8acad72`, `39fb903`). Deleting them would be losing the argument, not winning it.

Inline `//` section headers *inside* a method are the target: `// Fire routine`, `// Spiky
tiles`, `// Detect a teleporter`, `// Crappy fake gravity routine :p`. These explain nothing.
They are navigation for a method too long to navigate, and each one is a method name that was
never written down as one.

So: **if a comment names a section, it becomes a method name. If it explains a reason, it
stays.** A comment that does both gets split — the name goes on the method, the reason becomes a
`///` on it.

**No behaviour changes smuggled in.** Stages 2 and 4 are pure code motion and must leave
behaviour identical. Where a real defect is found, it gets its own commit, separate from any
move, so that a bisect can tell the two apart. Stage 1 and stage 3 are the only stages allowed
to change what the game does, and stage 3 only because it cannot avoid it.

## Leave alone

- **`BlobDeathAnimation.cs:18`** — `FragmentSpriteNumbers = [0, 1, 1, 2, 3, 5]`. The repeated `1`
  and the gap at `4` are intentional. It looks like a typo and is not.
- **`SharedLibs`** — already clean. No method in it exceeds 80 lines and its doc comments are the
  kind that stay. It was examined and needs nothing; don't go looking for work there.
- **The `WINDOWS` escape hatch and the content-build wiring** — `BUILD.md` explains why those are
  shaped the way they are. None of this plan touches the build.

## Where the size is

25 methods in `Starquake.Shared` exceeded 80 lines when this was written. The ten largest, with
what stage 2 left them at:

| Method | Lines | Now |
| --- | --- | --- |
| `Blob.Update` | 1,564 | 37 |
| `Blob.UpdateFire` (was part of it) | 608 | gone — see stage 3 |
| `Nasty.Update` | 432 | 47 |
| `GameEngine.Update` | 306 | 20 |
| `Nasty.SpikeyMovement` | 241 | 73 |
| `Nasty.MoveNastyOneStep` | 235 | 46 |
| `ToggleKeys.GetFriendlyName` | 232 | 232 — left alone, see stage 4 |
| `GameScreenCollection.BuildScreenCollisionMap` | 227 | 38 |
| `GameScreenCollection.DrawUpperLayer` | 222 | 16 |
| `Nasty.RandomMovementsAllDirections` | 222 | 18 |
| `GameEngine.DefineContent` | 219 | 26 |

`Blob.Update` was 62% of `Blob.cs` on its own. It was one `switch` over three `BlobState` values,
and the `Normal` case was almost all of it.

31 methods still exceed 80 lines, the longest of them 232. The list is at the end of stage 4.

Be careful reading that script's output. Its `^    (public|private|...).*\(` pattern needs a `(`
to start counting a new method, so a method followed by a run of get-only properties is credited
with their lines too. That is why it reports the `Blob` constructor as 147 lines when it is 92.

To re-measure after a stage lands:

```sh
for f in $(find Starquake.Shared SharedLibs -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*"); do
  awk -v F="$f" '
  /^    (public|private|protected|internal).*\(/ {
      if (name != "" && NR-start > 80) printf "%5d  %s:%d  %s\n", NR-start, F, start, name
      name = $0; start = NR; next }
  END { if (name != "" && NR-start > 80) printf "%5d  %s:%d  %s\n", NR-start, F, start, name }
  ' "$f"
done | sort -rn
```

## Tests

There were none when this was written, and that decided the order: the stages that are pure code
motion are safe without tests and came first, and the one stage that rewrites live collision maths
came last, behind the offer of a test project. Stage 3 took that offer up, so there is now a
`Starquake.Tests` project — see that stage for what is in it and why it is shaped the way it is.

Verification for every stage is at minimum:

```sh
dotnet build Starquake.sln                          # must stay at 0 warnings, 0 errors
dotnet test Starquake.Tests/Starquake.Tests.csproj  # must stay green
```

Zero warnings is the current state and the bar — the nullable work got it there (`NULLABLE.md`)
and a refactor must not spend it. Beyond that, stages 2 and 4 want the game launched and the
touched behaviour exercised by hand.

---

## Stage 1 — the free wins — **done**

Two commits, as planned: `a7e3e8d` for the code that was already correct but not saying so, and
`ab470df` alone for the one line that changed what the game does.

### 1a. The constants are wired up

Nine, not five. The plan counted `OverloadShrinking`, `OverloadScaleStep`, `OverloadMaxScale`,
`OverloadMinScale` and `OverloadFlareCopies`, and folding in `OverloadSwelling` and
`OverloadExploded` as it suggested exposed four more in the same two blocks of code:
`OverloadAlarmFrames` (the bare `60` in the alarm counter test) and `OverloadFlareFirstScale`,
`OverloadFlareScaleStep`, `OverloadFlareAlpha` — the `0.4f`, `0.2f` and `150` sitting in the three
lines of `Blob.Draw`'s flare loop next to the `5` the plan did name. They were left unwired by the
same unfinished extraction, so they went in the same commit rather than waiting for a reason to
come back.

Two literals in that region stayed literal on purpose. `_blobScale = 1.0f` in the `Dying` case is
the reset to normal size, not an overload value, and `temp >= 0f` in the flare loop is the test
for "not inside out yet". Neither has a constant and neither wants one.

### 1b. The two dead parameters are gone

`Nasty.DiagonalBouncing` and `Nasty.DiagonalBouncingInSteps` no longer take the blob's position.
Four call sites in `Nasty.Update` updated.

One of them collapsed. The `DiagonalBouncing` call site was an `if (GameEngine.Player != null)`
with the real position in one arm and `(0, 0)` in the other; with no parameters left both arms
were the same call, so the branch went too. The `DiagonalBouncingInSteps` call site was
dereferencing `GameEngine.Player` with no such guard, which is one fewer way to throw.

### 1c. The `ScreenWidth` bug is fixed

`Blob.cs:1222` now tests `BlobFireX + FireLength` against `ScreenWidth` and zeroes
`amountToMoveX`, matching the `Direction.Up` branch. Its own commit, with the behaviour change
named in the message.

The wider point stands and is now measured: the fix was a two-line edit to one of six copies of
the same logic, and nothing but reading all six side by side would have found it. That is stage 3.

---

## Stage 2 — split `Blob.Update` — **done**

Two commits, `f63e916` for `Update` and `76a192c` for `Draw`. `Blob.Update` was 1,564 lines and
is now 37; `Blob.Draw` was 197 and is now 23. `Blob.cs` gained 38 private methods and no statements.

The tree came out as planned, with three differences.

`DetectTunnel` became two methods, `DetectLeftHandTunnel` and `DetectRightHandTunnel`. One method
holding both would have had to keep `// Detect a lefthand tunnel` and `// Detect a righthand
tunnel` inside it as section headers, which is the thing this plan exists to remove.

`AccessInventory` split one level further than the tree showed. Its three inner section comments
were doing the same job as the outer ones, so they became `DropLastHeldItem`,
`ShuffleInventoryAlong` and `PickUpItemUnderfoot`. `interactiveItemDropped` stopped being a
variable declared `null` ahead of an `if` and became that first method's return value.

`ReleaseHeldControls` was not in the tree. It is the four small flag resets that sat between
`AccessInventory` and `ApplyGravity` — un-latching up and down, and putting the walk cycles back
to looping after the shuttle stops them.

### Guards moved whole, and were not rewritten

Every extracted method keeps its guard condition verbatim, so most of them are a single `if`
wrapping the whole body rather than an early return. That was deliberate. Negating
`MovedDown() && !MovedLeft() && !MovedRight()` correctly is exactly the kind of edit that looks
free and silently inverts a case, and a move commit is the worst place to spend that risk. The
nesting is now one level deeper than it needs to be inside each method, which is a follow-up
worth doing once each method can be read on its own — the same reasoning the plan already applied
to the ~60 fields.

The check that this really was code motion: normalise every statement in the old `Update` and the
new methods to whitespace-stripped text and compare the multisets. The only differences were the
new call statements, the added `return`s, and the `interactiveItemDropped` rename. Nothing was
dropped and nothing was duplicated. Same check on `Draw`: nine new calls, nothing else.

### Comments that stayed

Three became `///` on the method that replaced them, as the plan said: `DetectAccessBooth`'s
explanation of why both tiles must be present, `ApplyGravity`'s "this is not real physics" (the
joke did not survive, the information did), and the tunnel-mouth `EdgeShuffleTowardOpening`.
Four more `///` were written for things the extraction itself made worth saying — what the
`bool` returns mean, and that `FadeEnergyDrainTint` fades both ways.

The rule that settled the rest: **a section comment heading a block that became a method is
deleted, because the name now carries it. A section comment heading a block that stayed inline is
kept only if it says something the code does not.** So `// Make blob flicker` stays, because it
explains why a frame counter is tested for even. `// Draw other bits` and `// Draw the fire trail
particles` are gone, because the very next line said it better. `CheckSpikes` keeps its three
inner comments for the same reason: `Spike1`, `Spike2` and `Spike3` do not tell you which is
wall-mounted and which is broken glass.

Two comments were deleted as debris rather than under the rule: `// Collect powerups`, which sat
above an unrelated declaration and duplicated the name of a later section, and
`//gameEngine.GameScreens.C`, a truncated commented-out fragment.

### `Blob.Draw`

Split as listed above. The `Normal` case became `FadeEnergyDrainTint`, `DrawBlobSprite`,
`DrawShuttle`, `DrawFire`, `DrawTeleportBeam` and `FadeInAfterTeleport`; the two `Overloading`
cases became `DrawOverloadAlarmState` and `DrawOverloadFlare`.

### The `Blob` constructor — left alone

It is not 147 lines. The measurement script's `^    (public|private|...).*\(` pattern needs a
`(` to start counting a new method, so a run of get-only properties after the constructor is
counted as part of it. The constructor is 92 lines, and the same artefact inflates any method
followed by properties.

More to the point, it does not want splitting. It is a flat list of initialisations, and nine of
the fields it sets are `readonly`, so extracting them needs factory methods that return a value
rather than methods that do a job — five `CreateWalkLeftAnimation`-shaped methods whose names
repeat the field names they assign. The two section comments in it are `// Clear inventory items`,
naming a four-line loop that is not dead (`Collectables.None` is 255, not 0, so the loop is
required), and `// Temporary test code to give Blob Access card and Key`, which is a why and
stays. This is the `ToggleKeys.GetFriendlyName` case the plan already concedes: long because it
is data, and already as clear as it is going to get.

### Still over 80 lines in `Blob.cs`

- `UpdateFire` — 608. Stage 3.
- `UnstickBlobFromTiles` — 97. Pre-existing and untouched; it belongs to stage 4's tail.
- `ChangeScreenIfOffEdge` — 88. Four sibling blocks, one per screen edge, with no comments left in
  it. Splitting it would produce four methods named after the four edges and read no better.

### Noted, not fixed

- `LayJumpPlatformOrJump` still contains the empty `if (temp == -1) { }` and the comment about
  `-1` sitting above a test for `-1000`. Left exactly as found, per the plan.
- `DrawOverloadFlare` and `LayJumpPlatformOrJump` both have a local called `temp`. Worth renaming,
  not in a move commit.

---

## Stage 3 — collapse the fire duplication — **done**

Four commits. `66e7b0e` added the test project the plan said to offer first, `2082656` and
`482b2ba` dealt with what reading all six branches side by side turned up, and `5febc34` did the
collapse.

`Blob.UpdateFire` was 608 lines and is now 18, calling into `BlobFire` — 242 lines including its
documentation. `Blob.cs` went from 2,723 lines to 2,133, and no method in it exceeds 80 lines any
more.

### The shape

A shot is an axis plus a sign. Fired up is the vertical axis with a sign of -1; fired right is the
horizontal axis with a sign of +1. Deflecting reverses the sign on the axis it was fired along and
opens up the other one, so a shot fired up that hits the ceiling comes back down and away to one
side. Every boundary test is then derived from the sign — a negative sign stops at nought, a
positive one stops at the screen's edge less the shot's own length — which is the point: the
`Up`/`Down` asymmetry that produced the stage 1c bug is no longer something you can write.

`BlobFire` knows nothing but its own position. Everything else it asks an `IFireSurroundings` for:
the screen's size, how far it may travel in a direction, which way to deflect, and where to throw
sparks. `Blob`'s private nested `FireSurroundings` is the only implementation in the game; the
tests supply the other one. That is what lets the maths run without a screen, a content pipeline
or a graphics device, and it is the only interface introduced by any of this.

### Two more drifts, found by reading all six together

Exactly what the plan predicted, and both got their own commit before the collapse so that a
bisect can tell them from the move.

`2082656` — the `Left` and `Right` shuttle branches still wrote a bare `24` where
`FireSparksYOffset` is 24. The same unfinished extraction as stage 1a, three commits later.

`482b2ba` — of the eight screen-edge checks across the four shuttle branches, seven set
`hitScreenBoundary` and one did not. So a shot fired right off a shuttle pad, deflected, and then
run off the **left** edge threw its impact sparks at the edge, where the other seven go quietly.
Cosmetic, and invisible unless you line all eight up.

That makes three defects in six copies of one behaviour, none of which a build or a play-test
would have surfaced. The case for this stage was not hypothetical.

### Two asymmetries that were real, and stayed

Not every difference between the six branches was drift, and the collapse had to keep the two that
were not.

**The point-blank push-through** — which stops a shot dying on the spot when the blob is standing
over a collectable — exists only for shots fired along the horizontal axis. Vertical shots from a
shuttle never had it. That looks accidental rather than intended, but adding it to vertical shots
changes what the game does, so `BlobFire` keeps the axis test and `Advance`'s documentation says
why. A follow-up, if anyone wants it.

**The distance a shot has travelled** counts horizontal movement only. Four of the six branches
add `amountToMoveX` to it and the two vertical ones add nothing — but their `amountToMoveX` is
always nought, so the two are the same rule written twice, and the unified `DistanceTravelled +=
stepX` reproduces all six. The only thing that reads it is the push-through check, so counting one
axis is all it needs.

### How the collapse was verified

Two ways, because a build proves nothing here.

25 tests in `Starquake.Tests/BlobFireTests.cs` pin the axes, the four straight screen edges, the
eight deflected ones, the difference between deflecting off a shuttle and dying on foot, the
push-through applying horizontally and not vertically, and what happens when the blob fires in a
direction nothing travels along. Two of them are the cases that would have caught the two drifts.

Beyond that, the six old branches were ported mechanically into a throwaway harness — only the
dependency calls rewritten — and run against `BlobFire` over every firing direction, every
deflection state, both screen sizes and a spread of on- and off-screen positions, with a fake that
answers both implementations identically for the same question. **2,359,054 frames compared,
no differences** in position, distance travelled, deflection state, whether the shot was still
alive, or the exact sequence of spark positions. The harness was not committed: it exists to
retire one risk once, and keeping a 600-line copy of deleted code in the repo to guard code that
now has its own tests would be the worse trade.

### The test project

`Starquake.Tests`, xunit, single-targeted at `net9.0` and referencing `Starquake.Shared`'s `net9.0`
build. That pulls MonoGame DesktopGL into the test process, which is harmless headlessly because
SDL is not loaded until something constructs a `Game` or a `GraphicsDevice`. A test that needs a
`GraphicsDevice` does not belong in it. `RollForward=Major`, for the same reason the four heads set
it — there is no 9.0 runtime installed on this machine.

It also covers `CollisionDetection`, the primitive every moving thing in the game is stepped by and
the one the fire maths sits directly on. Those tests characterise what it currently decides rather
than what a rectangle overlap test ideally would: it compares centre distances with truncating
integer division, so two boxes of odd size can overlap by a pixel and not count as touching. Pinned,
not corrected.

Test parallelisation is off, and this is the reason: `CollisionDetection` keeps its intermediate
values in **static fields**, so two tests calling it at once can read each other's working. That
wants fixing — it is a latent bug in the game too the moment anything touches collision off the
update thread — but in its own commit.

Verification for the whole solution is now:

```sh
dotnet build Starquake.sln                        # 0 warnings, 0 errors
dotnet test Starquake.Tests/Starquake.Tests.csproj  # 40 tests
```

---


---

## Stage 4 — the same treatment elsewhere — **mostly done**

All four named targets are addressed. The unnamed tail at the end of this stage is partly done and
is listed below.

### `Nasty` — done, four commits

`2,061` lines to `1,551`. `Update` was 432 and is 47: one method per `NastyState`, plus
`ShouldAnimate`, `ReleaseAttractionIfStuck`, `MoveNasty` and `UpdateFrameOfMind`.

The reflection table the plan asked for turned out to be **four** tables, and all four are now
data:

- `Reflected` — the 12 `(travelling, hit) -> new direction` mappings that `DiagonalBouncing` and
  `DiagonalBouncingInSteps` each wrote out in full. The one real difference between those two
  methods survives as `TurnedAwayVertically`: the stepped one turns away even when it hit nothing.
- `WaysAwayFrom` — the eight escape triples from `RandomMovementsAllDirections`, which was 222
  lines of twelve near-identical blocks and is now two lookups and one pick.
- `WaysTowardsBlob` — the four chase triples from the same method.
- `BounceStraightBack` and `HeadTowardsBlob` — shared by `RandomMovement` and
  `RandomUpDownLeftRight`, which were near-identical for 75 lines.

`SpikeyMovement` (241 to 73) was the same 55 lines once per quadrant the blob can be in, with the
directions and subtractions swapped. `MoveNastyOneStep` (235 to 46) was eight blocks saying how to
move and eight more saying what it hit, when every direction is a vertical component, a horizontal
one, or both. Its two `ref` parameters were the same dead pair as stage 1b's and are gone.

Two things were preserved rather than tidied, each with a `///` saying why. `RandomMovement` clears
the direction before chasing and `RandomUpDownLeftRight` does not, so a nasty level with the blob
behaves differently in the two — that is load-bearing. And `SpikeyMovement`'s quadrant tests were
an if/else chain with inclusive bounds on both sides, so a nasty exactly level with the blob fell
into whichever arm matched first; that is an artefact of the order they were written in, and
straightening it would change the game.

**A third drift, found the same way as the other two.** `RandomMovementsAllDirections` picks one of
three escape directions for each of twelve cases. Eleven roll `Next(0, 3)`; the case for hitting
something above rolled `Next(0, 2)`, so its third option could never come up and a nasty bouncing
off a ceiling never went down-right. Fixed in its own commit.

### `GameEngine` — done, one commit

`Update` 306 lines to 20, `DefineContent` 219 to 26, and
`ContentResourceManager_OnGameComponentStatusChanged` 147 to 17. The file grew slightly, to 1,371
lines: the split is signatures and documentation, not logic.

`Update` splits as the plan predicted — the debug keys are one method each rather than four
anonymous blocks in the middle of a frame — and `DefineContent` becomes fourteen `Define*` methods,
one per comment that already grouped the registrations. Its `[MemberNotNull]` contract from
`NULLABLE.md` is kept whole by having `DefineBootGraphics` and `DefineFonts` declare what they fill
in; without that the split costs six warnings, which is exactly the thing this plan must not spend.

The handler was doing four jobs, not two: `BuildComponentsNeedingLoadedContent`,
`RegisterColourAnimations`, `BuildGameScreens` and `RegisterScreenEffectViewPorts`.

As the plan said, the `catch { }` around `ToggleFullScreen` is left alone.

### `GameScreenCollection` — done, one commit

`BuildScreenCollisionMap` 227 lines to 38, `DrawUpperLayer` 222 to 16, `Reset` 206 to 19.

`DrawUpperLayer` held a fifth table: a fourteen-arm `if`/`else if` ladder in which every arm made
the same draw call with a different sprite number. It is now `UpperLayerPatchFor`, returning the
sprite and how many rows down to draw it. The row offset exists for one case — a shadow belongs to
the tile above it — and saying so is the whole point, because previously that was a `(y + 1)` in
the middle of 150 otherwise identical lines. `Reset`'s two trimming loops differed only in what
they cleared and are now one `TrimTo`.

### `ToggleKeys.GetFriendlyName` — left alone, as the plan allowed

232 lines, and still the longest method in the solution. It is a `switch` from key to display
string: long because it is data, and a dictionary would not read better. The plan offered "leave
it" as an acceptable outcome and that is the outcome.

### The tail — four done, 31 methods still over 80 lines

Done: `MainGameScreen.Update` (218 to 35), `Nasties.Update` (174 to 55), `CheopsScreen.Draw` (164
to 17), `IntroSequence.Update` (155 to 32).

`MainGameScreen.Update` had a sixth piece of duplication worth naming: four of its blocks opened
with the same two-line test for whether the intro sequence still owns the screen, now the
`GameIsInPlay` property.

**Still to do**, from the measurement script. Nothing here is over 150 lines and none of it is
duplicated logic — it is the plain version of the same move:

| Method | Lines |
| --- | --- |
| `BouncingParticle.Update` | 137 |
| `TeleportScreen.Draw` | 135 |
| `CheopsScreen.Reset` | 132 |
| `SoundEffectEngine.PlaySound` | 129 |
| `TeleportScreen.Update` | 128 |
| `OptionsScreen.Update` | 125 |
| `Nasty.UpdateFrameOfMind` | 122 |
| `GameScreenCollection.Update` / `Draw` / `CheckTileByObject` / `CheckCollision` | 107-115 |
| `Nasty.Draw` | 110 |
| `ElectricityGatesAndNodesEngine.DrawUpperLayer` | 110 |
| `MainGameScreen.Draw` | 108 |
| `ResourceManager.backgroundWorker_DoWork` | 103 |
| `CollisionDetection.CheckDirection` | 103 |
| `GameScreenCollection.PlaceScreenObjects` | 101 |
| ...and 12 more between 84 and 97 |

Three entries in that list want a note rather than a plain split:

- **`CollisionDetection.CheckDirection`** (103) is four copies of one loop differing only in which
  way the probe steps — the `BlobFire` treatment applies directly, and it is the one method in the
  solution that already has tests.
- **`GameScreenCollection.PlaceScreenObjects`** (101) is new, from this stage's own `Reset` split.
  It wants a second pass.
- **The `Blob` constructor** (148 by the script, 92 in fact) and
  **`GameScreenCollection`'s constants block** (111) are both measurement artefacts — see the note
  under "Where the size is".

---

## Follow-ups this work found and deliberately did not take

Each of these is a real finding, noted rather than acted on so that it gets its own commit the way
`ab470df`, `482b2ba` and the `Next(0, 3)` fix did.

- **`Blob.LayJumpPlatformOrJump`** still has the empty `if (temp == -1) { }` and the comment about
  `-1` sitting above a test for `-1000`. Carried over from stage 2.
- **`BuildScreenCollisionMap`'s `screenChanged` parameter does almost nothing.** The
  `!screenChanged` path removes game-object tiles from the collision map, and then the map is
  replaced wholesale on the next line, so that work is discarded. Both paths end up appending the
  screen's objects to a freshly built map in the same order. All the parameter actually controls is
  whether `_keyLockDoors` is cleared. Worth confirming and then simplifying.
- **`CollisionDetection` keeps its intermediate values in static fields**, which is why test
  parallelisation is off. It is a latent bug in the game too, the moment anything touches collision
  off the update thread.
- **The point-blank push-through applies only to horizontally fired shots** (stage 3). It looks
  accidental; fixing it changes the game.
- **`DrawOverloadFlare` and `LayJumpPlatformOrJump` both have a local called `temp`**, as do
  several methods in `Nasty`. Carried over from stage 2.
- **`Blob` has ~60 fields**, many now used by exactly one method. The plan called this out in stage
  2 and it is still true, now with the reads visible.
- **The guards are one level deeper than they need to be.** Stage 2 kept every guard verbatim
  rather than inverting it, on purpose. Later stages did invert the simple ones where it flattened
  a whole method. The stage 2 methods could have the same pass now that each can be read alone.

