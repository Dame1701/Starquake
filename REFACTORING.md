# Making the code say what the comments say

**All four stages have landed.** This document is a record of what the work actually decided, the
way `NULLABLE.md` is — each stage's section was rewritten as it landed rather than left as the
intention. The findings it turned up and deliberately did not act on are listed at the end.

`ToggleKeys.GetFriendlyName` is the only method in `Starquake.Shared` still over 80 lines, and the
plan named that as an acceptable outcome before the work started.

Twenty-six commits, `a7e3e8d` to `820287c`. The solution builds at 0 warnings and 0 errors
throughout, and the 40 tests added in stage 3 pass.

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

One method still exceeds 80 lines: `ToggleKeys.GetFriendlyName`, at 232, left alone on purpose.

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
repeat the field names they assign. The one section comment left in it is `// Clear inventory items`,
naming a four-line loop that is not dead (`Collectables.None` is 255, not 0, so the loop is
required). The second, `// Temporary test code to give Blob Access card and Key`, was judged here to
be "a why and stays" - it has since gone, along with the two lines under it, now that
`WorldPopulator` places both items on the map. See `PLAN.md`. This is the
`ToggleKeys.GetFriendlyName` case the plan already concedes: long because it is data, and already as
clear as it is going to get.

### Still over 80 lines in `Blob.cs`

- `UpdateFire` — 608. Stage 3.
- `UnstickBlobFromTiles` — 97. Pre-existing and untouched; it belongs to stage 4's tail.
  *Since moved whole into `UnstickProbe` by `PLAN.md` 4.7, where it is a question - given where
  the blob is, where should he be - and has tests.*
- `ChangeScreenIfOffEdge` — 88. Four sibling blocks, one per screen edge, with no comments left in
  it. Splitting it would produce four methods named after the four edges and read no better.

### Noted, not fixed

- `LayJumpPlatformOrJump` still contains the empty `if (temp == -1) { }` and the comment about
  `-1` sitting above a test for `-1000`. Left exactly as found, per the plan. *Fixed in `d31561b`.*
- `DrawOverloadFlare` and `LayJumpPlatformOrJump` both have a local called `temp`. Worth renaming,
  not in a move commit. *Half done: `LayJumpPlatformOrJump`'s is `newYPos`; the other is still
  `temp`.*

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

## Stage 4 — the same treatment elsewhere — **done**

All four named targets are addressed, and so is the unnamed tail.

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

*Taken later, in `ea37940`: a `switch` expression says the same table in a third of the lines,
and it has tests now. The reasoning above still holds for why stage 4 did not do it.*

### The tail — done

All 31 are done. `ToggleKeys.GetFriendlyName` is the only method in `Starquake.Shared` still over
80 lines, and that is a decision rather than an omission.

The first four were the plain move: `MainGameScreen.Update` (218 to 35), `Nasties.Update` (174 to
55), `CheopsScreen.Draw` (164 to 17), `IntroSequence.Update` (155 to 32). `MainGameScreen.Update`
had a sixth piece of duplication worth naming — four of its blocks opened with the same two-line
test for whether the intro sequence still owns the screen, now the `GameIsInPlay` property.

The rest were not plain moves. **Eleven more tables came out of the tail**, which is the finding of
this stage: the pattern this plan was written to chase — N near-identical arms differing in one or
two values — was not confined to the four methods it named.

| Where | Was | Became |
| --- | --- | --- |
| `CollisionDetection.CheckDirection` | four copies of one stepped loop | one loop plus `StepFor` |
| `CheckCollision`'s bounds clamp | four arms, two shapes twice | `ClampToScreenBounds` |
| `CheckTileByObject` | five arms | `ProbeRectangleFor` + a `measureFromFoot` flag |
| `GetPotentialLocations` | a fifteen-term inequality chain | `TilesNothingMayBePlacedOn` |
| `UnstickBlobFromTiles` | four copies of one edge test | an edge plus a push |
| `BouncingParticle.Update` | four copies, axis and sign swapped | one `StepAlongAxis` |
| `Nasty.ChooseMoveSpeed` | two blocks of four arms, last repeating first | two tests |
| `TeleportScreen`'s key highlights | sixteen identical draw calls | `KeyHighlightPositions` |
| `OptionsScreen`'s key presets | four blocks of seven assignments | four object initialisers |
| `NastyHelpers.ApplyNastyColours` | fourteen arms of three colours | a fourteen-entry table |
| `StatusPanel`'s power ups | eight two-line arms | four one-line helpers |

Three more repeated conditions became named properties, joining stage 4's `GameIsInPlay`:
`MainGameScreen.WorldIsVisible`, `GameScreenCollection`'s `objectsArePlaced`, and
`Nasties`'s `goingBackTheWayWeCame`.

### How the tail was verified

A statement multiset comparison proves a move preserved everything. It proves nothing about a
deduplication — collapsing five arms into one is exactly the change it flags. So each of the eleven
was checked on its own terms, and the collision maths was checked by brute force:

| Check | Cases |
| --- | --- |
| `CheckDirection`, old against new | 32,131,125 |
| `ClampToScreenBounds`, old against new | 2,286,144 |
| `UnstickBlobFromTiles`, old against new | 576,000 |
| `BouncingParticle`'s axis step, old against new | 243,000 |

The rest were verified by extracting the table from both the old and the new source and comparing
it entry by entry: the fourteen nasty colour sets, the sixteen key positions, the four key presets
field by field, the thirty-nine tile-to-sprite mappings, the twelve nasty speed combinations, the
four starting directions, the five probe rectangles, the exclusion sets, the eight power-up
effects, and the key-door emitter sequence replayed for both key states.

**One of those checks caught a real mistake before it landed.** Naming the bare `32` in
`CheckCollision`'s game-object probe as `SpriteMetrics.SpriteWidth` looked obviously right and would
have doubled it to 64. `SpriteWidth` is 64; the probe is half that. It is `GameObjectProbeSize`
now. The lesson is the narrow one: do not name a literal by the first constant that seems to fit,
check its value.

### Debris removed along the way

Deliberately, and each said in its commit: the empty `if (amountThatCanBeMoved == 0) { }` in
`CheckDirection`; `GetPotentialLocations`'s dead inner re-test and the unreachable `else` beside
it; two locals in `TeleportScreen.Draw` assigned and never read; a redundant re-assignment inside
`Nasties`'s loop; `// 419 is the key access door tile`, which documented a number the code had
replaced with `ActionTiles.AccessForceField`; and five identical commented-out joystick blocks in
`CustomControl`, sixty lines of it, now one sentence on the class.

### One earlier decision reversed

Stage 4's record said `Blob.ChangeScreenIfOffEdge` was better left whole, on the grounds that four
methods named after four edges would read no better. Doing it showed otherwise: the two vertical
edges have to decide where blob lands, and that wants its own name and its own `///`. It is four
methods now.

The `Blob` constructor was revisited too, but only partly. Its five `Animation` initialisers were
seven assignments each and are now five calls to one factory; the rest stands, for the reason stage
2 gave.

---


## Follow-ups this work found and deliberately did not take

Eleven were listed here. Seven have since been taken, by the work `PLAN.md` describes, and are
struck from this list rather than left to be re-found; the commit each landed in is named in case
the reasoning is wanted. **Four are still open.**

### Still open

- **The point-blank push-through applies only to horizontally fired shots** (stage 3). It looks
  accidental; fixing it changes the game, which is why it is still here.
- **`Blob` is the one god class left standing.** It had ~60 fields when this was written and has
  123 now that the reads are visible. `PLAN.md` phase 4 took the two parts of it that were
  arithmetic rather than drawing - the unstick probe and the inventory - and stopped there, because
  the rest is one state machine that draws itself. Splitting it needs the update logic and the draw
  code prised apart first, which is a different task from moving field clusters into files.
- **The guards are one level deeper than they need to be.** Stage 2 kept every guard verbatim
  rather than inverting it, on purpose. Later stages did invert the simple ones where it flattened
  a whole method. The stage 2 methods could have the same pass now that each can be read alone.
- **`Scorpian` and `Helicopter` splatter identical colours.** Still true, and
  `NastyHelpers.ApplyNastyColours` has a `///` saying so. Could be deliberate, could be a paste;
  nobody has asked the question of whoever drew them.

A fifth is half open. **Three methods still have a local called `temp`** - `Blob.DrawOverloadFlare`,
`Blob.AccessInventory` and `Nasty.GetRandomNastyType` - though
`LayJumpPlatformOrJump`'s is now `newYPos`.

### Taken

- **`Blob.LayJumpPlatformOrJump`'s empty `if (temp == -1) { }`** - `d31561b`. `GetNewJumpPlatform`
  never returned -1; it returns -1000, which is now `JumpPlatforms.NoRoomForPlatform` and says why
  it has to be well below any real answer.
- **`BuildScreenCollisionMap`'s `screenChanged` parameter** - `d31561b`. Confirmed to control only
  the key-door cache, and simplified. The removal it also did walked the list by index while
  removing from it, so it would have skipped every second match had it mattered.
- **`CollisionDetection`'s static intermediate values** - `eaaa1f0`. They are locals, and test
  parallelisation is back on.
- **The three bounds tests that looked one out** - `105a07c`. They were.
- **`GameScreenCollection.PowerUpIsInTheWay`** - `d31561b`. Deleted rather than fixed: it could
  never fire, and making it fire would have made world generation worse rather than better.
- **`SoundEffectEngine`'s commented-out spin-wait** - already gone in `9a490e5`, before `PLAN.md`
  was written. That plan carried it forward stale.
- **`ToggleKeys.GetFriendlyName`**, left alone by stage 4 as the plan allowed - `ea37940`. It is a
  switch expression, and the two oddities in its table now have tests pinning them.
