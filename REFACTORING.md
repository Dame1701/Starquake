# Making the code say what the comments say

**Stages 1 to 3 landed.** Stage 4 is still a plan. This document is meant to become a record the
way `NULLABLE.md` did — as each stage lands, its section is rewritten to say what it actually
decided rather than what it intended.

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
| `Blob.Update` | 1,564 | 36 |
| `Blob.UpdateFire` (was part of it) | 608 | gone — see stage 3 |
| `Nasty.Update` | 432 | 425 |
| `GameEngine.Update` | 306 | 306 |
| `Nasty.SpikeyMovement` | 241 | 241 |
| `Nasty.MoveNastyOneStep` | 235 | 235 |
| `ToggleKeys.GetFriendlyName` | 232 | 232 |
| `GameScreenCollection.BuildScreenCollisionMap` | 227 | 227 |
| `GameScreenCollection.DrawUpperLayer` | 222 | 222 |
| `Nasty.RandomMovementsAllDirections` | 222 | 222 |
| `GameEngine.DefineContent` | 219 | 219 |

`Blob.Update` was 62% of `Blob.cs` on its own. It was one `switch` over three `BlobState` values,
and the `Normal` case was almost all of it. What is left of it is `UpdateFire` at 608 lines, which
is stage 3.

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
is now 36; `Blob.Draw` was 197 and is now 23. `Blob.cs` gained 38 private methods and no statements.

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

`Blob.UpdateFire` was 608 lines and is now 20, calling into `BlobFire` — 242 lines including its
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

## Stage 4 — the same treatment elsewhere

In descending value. Each is independent; each is its own commit or small series.

**`Nasty`** (2,061 lines). `Update` is 432 lines. Beyond splitting it, the six movement methods
share one idea: given a direction and what it just hit, pick the reflected direction.
`DiagonalBouncing` and `DiagonalBouncingInSteps` are near-identical — the second adds an `else`
fallback per case and sets `_stepsToMove` at the end — and both express a reflection table as
four nested `if`/`else if` ladders. That table is data. Make it data, and the six methods get
much smaller. `SpikeyMovement` (241) and `MoveNastyOneStep` (235) then want their own splits.

**`GameEngine`** (1,243 lines). `Update` (306) splits along its state tests into
`HandleDebugKeys` — the F8/F9/F10/F12 block, which is most of it — plus `UpdateBootSequence` and
`UpdateActiveScreen`. `DefineContent` (219) is a flat list of registrations already grouped by
comment: `DefineBootGraphics`, `DefineFonts`, `DefinePresentationScreens`, `DefineGameGraphics`,
`DefineSounds`. `ContentResourceManager_OnGameComponentStatusChanged` (147) is doing two
unrelated jobs — constructing the components that need loaded content, and registering the
colour animations; the second is a long literal list and wants to be its own method at least.
The `catch { }` around `ToggleFullScreen` is a separate question, not a refactor — leave it.

**`GameScreenCollection`** (1,769 lines) is already the best-decomposed of the four, at 31
methods. Three are long: `BuildScreenCollisionMap` (227), `DrawUpperLayer` (222), `Reset` (206).
Split those and it is done; the rest of the file is fine.

**`ToggleKeys.GetFriendlyName`** (232) is a single `switch` mapping keys to display strings. It is
long but it is a lookup table, and it is already as clear as it is going to get. Converting it to
a dictionary is defensible and is not an improvement in readability. Lowest priority, and
"leave it" is an acceptable outcome.

Then the remaining `Update`/`Draw`/`Reset` methods over 80 lines across
`GameComponents/GameScreens/*` and `Animations/*` — same move, much smaller each. Re-run the
measurement script to pick them up.
