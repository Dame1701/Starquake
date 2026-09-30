# Making the code say what the comments say

**Stage 1 landed.** Stages 2 to 4 are still a plan. This document is meant to become a record
the way `NULLABLE.md` did — as each stage lands, its section is rewritten to say what it actually
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

25 methods in `Starquake.Shared` exceed 80 lines. The ten largest:

| Method | Lines |
| --- | --- |
| `Blob.Update` | 1,564 |
| `Nasty.Update` | 432 |
| `GameEngine.Update` | 306 |
| `Nasty.SpikeyMovement` | 241 |
| `Nasty.MoveNastyOneStep` | 235 |
| `ToggleKeys.GetFriendlyName` | 232 |
| `GameScreenCollection.BuildScreenCollisionMap` | 227 |
| `GameScreenCollection.DrawUpperLayer` | 222 |
| `Nasty.RandomMovementsAllDirections` | 222 |
| `GameEngine.DefineContent` | 219 |

`Blob.Update` is 62% of `Blob.cs` on its own. It is one `switch` over three `BlobState` values,
and the `Normal` case is almost all of it.

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

## Stage 2 — split `Blob.Update`

The largest single win, and pure code motion. `Blob.Update` is 1,564 lines; its own comments
already mark every seam. The `switch` over `BlobsState` becomes three calls, and the `Normal`
case becomes roughly twenty named methods.

```
Update
  ├─ UpdateNormalState
  │    ├─ HandleShuttlePad              // Do something about being on something etc.
  │    ├─ WalkLeft / WalkRight
  │    ├─ MoveOnShuttle
  │    ├─ LayJumpPlatformOrJump         // Create a standing platform
  │    ├─ AccessInventory               // Pick up an object or drop one perhaps.....
  │    ├─ ApplyGravity                  // Crappy fake gravity routine :p
  │    │    └─ EdgeShuffleTowardOpening // Check to see if Blob can potentially enter a teleport booth, tunnel etc.
  │    ├─ StartFiring                   // Fire not on a shuttle
  │    ├─ UpdateFire                    // Fire routine          ← stage 3 rewrites this one
  │    ├─ StopFireEffects               // Stop the particle effect since the fire has stopped
  │    ├─ ChangeScreenIfOffEdge         // Change screens...?
  │    ├─ DetectSurroundingTiles        // the four _tile*Blob assignments
  │    ├─ DetectShuttlePad              // Detect Blob on a shuttle pad
  │    ├─ DetectTeleporter              // Detect a teleporter
  │    ├─ DetectAccessBooth             // Detect an access booth (keep the three-line why as ///)
  │    ├─ DetectTunnel                  // Detect a lefthand / righthand tunnel
  │    ├─ BreakBridgeUnderfoot          // Breakable bridges
  │    ├─ RideTunnelLift                // Tunnel lifts
  │    ├─ CheckSpikes                   // Spiky tiles
  │    └─ CollectPowerUp                // Collect powerups
  ├─ UpdateOverloadingState             // Play the alarm sound for a bit
  └─ UpdateDyingState
```

Notes for the doing:

- `changedScreens` is read at the very end of `Update` to record the screen entry point. Several
  extracted methods set it. Return a `bool` from those and `|=` it at the call site rather than
  promoting it to a field — it is genuinely per-frame state and a field would outlive the frame.
- `// Detect an access booth` carries a real three-line explanation of why *both* tiles have to
  be present for either side to trigger. That is a why, so it becomes the `///` on
  `DetectAccessBooth`, not a deleted comment.
- `// Crappy fake gravity routine :p` — the name becomes `ApplyGravity`. Whether the joke
  survives as a `///` is a taste call; the information in it is "this is not real physics", which
  is worth a sentence.
- `// If -1 is returned from this routine, the collision check failed` sits above a test for
  `-1000`, and just above it is an empty `if (temp == -1) { }`. Something is off there. Do not
  fix it inside a move commit — leave both alone, and note it for a follow-up so it gets the same
  separate-commit treatment as 1c.
- `Blob` has ~60 fields, and after the split many will be used by exactly one method. Resist
  turning those into locals in the same commit; it is a second refactor and wants its own pass
  once the methods exist and the reads are visible.

Same treatment, same file, once the above is in: `Blob.Draw` (197 lines) and the `Blob`
constructor (147 lines).

---

## Stage 3 — collapse the fire duplication

The highest-value change in the codebase and the only one that rewrites live logic. Do it after
stage 2, so it operates on an extracted `UpdateFire` rather than on a slice of a 1,564-line
method.

`Blob.cs:1012-1595` is one behaviour written six times:

| Branch | Lines | Roughly |
| --- | --- | --- |
| on shuttle, `Direction.Up` | 1014-1134 | 121 |
| on shuttle, `Direction.Down` | 1135-1255 | 121 |
| on shuttle, `Direction.Left` | 1256-1383 | 128 |
| on shuttle, `Direction.Right` | 1384-1517 | 134 |
| on foot, `Direction.Left` | 1520-1557 | 38 |
| on foot, `Direction.Right` | 1558-1595 | 38 |

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
