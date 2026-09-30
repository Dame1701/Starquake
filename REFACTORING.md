# Making the code say what the comments say

**Stages 1 and 2 landed.** Stages 3 and 4 are still a plan. This document is meant to become a
record the way `NULLABLE.md` did — as each stage lands, its section is rewritten to say what it
actually decided rather than what it intended.

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

## There are no tests

No test project in the solution. That decides the order of what follows: the stages that are
pure code motion are safe without tests and come first, and the one stage that rewrites live
collision maths comes last, behind the offer of a test project.

Verification for every stage is at minimum:

```sh
dotnet build Starquake.sln    # must stay at 0 warnings, 0 errors
```

Zero warnings is the current state and the bar — the nullable work got it there (`NULLABLE.md`)
and a refactor must not spend it. Beyond the build, stages 2 and 4 want the game launched and
the touched behaviour exercised by hand.

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

## Stage 3 — collapse the fire duplication

The highest-value change in the codebase and the only one that rewrites live logic. Stage 2 has
landed, so this now operates on `Blob.UpdateFire` — 608 lines, the whole of what is left of the
old `Update` — rather than on a slice of a 1,564-line method.

`Blob.UpdateFire` (`Blob.cs:1127`) is one behaviour written six times. Line numbers below are as
of `76a192c`:

| Branch | Lines | Roughly |
| --- | --- | --- |
| on shuttle, `Direction.Up` | 1140-1260 | 121 |
| on shuttle, `Direction.Down` | 1261-1381 | 121 |
| on shuttle, `Direction.Left` | 1382-1509 | 128 |
| on shuttle, `Direction.Right` | 1510-1643 | 134 |
| on foot, `Direction.Left` | 1646-1683 | 38 |
| on foot, `Direction.Right` | 1684-1721 | 38 |

Every one of them does the same three things: step the shot along its axis, deflect it to a
random diagonal if it hits scenery, and kill it at the screen edge. The four shuttle branches
differ only in which axis is primary and which sign each delta carries. About 580 lines should
become something on the order of 80.

The shape to aim for is a direction expressed as a delta — a `Point`, or an axis plus sign —
with the boundary test derived from it rather than written out per branch. Then the `Up`/`Down`
asymmetry that produced the 1c bug is not expressible.

**This is where a test project earns its keep.** `SharedLibs` has no MonoGame dependency, so the
stepping and boundary maths can be exercised headlessly. Recommend adding one before this stage
and ask before starting it — a silent regression here is a shot that passes through walls, which
is not something a build catches and not something a casual play-test notices either.

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
