# The god classes, second pass

`PLAN.md` phase 4 took `GameScreenCollection` from 1,938 lines to 550, `Nasty` from 1,646 to 819 and
`StatusPanel` from 547 to 255, and `REFACTORING.md` turned the long methods inside them into named
ones. Both are finished. What neither did is ask which classes still own too much *state*, because
both were measuring length, and length is not the thing that makes a class hard to change.

This document asks that question instead, and answers it with one measurement: **how many mutable
fields does a class own, and how many unrelated groups do they fall into.** A class with forty short,
well-named methods over thirty-six fields with no owner is harder to reason about than a class with
one long method, and `Blob` is exactly that class now.

Each stage below builds at 0 warnings and 0 errors and leaves the tests green, so each is
independently shippable and a bisect can tell two stages apart. No behaviour changes are smuggled
in; where a real defect is found it gets its own commit, separate from any move. That discipline is
`REFACTORING.md`'s and it is not restated per stage.

Line numbers are as of `07db88c` and will drift. Every finding is named by symbol as well, so
re-locate by symbol and treat the numbers as a hint.

## What has landed already

Nothing from the stage list. What landed was the obstacle in front of it, found by writing down the
rule in *The rule each extraction is placed by* and discovering the codebase broke it.

| Landed | What it did |
| --- | --- |
| `cf95609` | Five fades off the draw path: the blob's energy-drain tint, his teleport fade-in, the glow on a shot, and the fade and shrink of a dead nasty and a dead conveyer nasty. `ConveyerNasty` had no `Dead` branch in `Update` at all. |
| `32c0345` | `SpriteCollection` held one rotation angle and advanced it inside `End`. The angle moves to the nasty, the blob's shuttle flame and the intro's ship, one per logic frame. `GameSession` falls out of `Nasty`, `Nasties` and `PlanetCoreScreen` with the `paused` flag that existed to freeze the spin. |
| `07db88c` | The two booth screens asked for their verdict sound while drawing - hence the latch stopping it playing sixty times a second - and started a logic-frame wait from a draw. Plus three idempotent draw-time writes that left logic waiting on a frame having been drawn. |

**`GameHost` runs the logic at a fixed fifty hertz and draws once per vertical retrace**, so anything
counting frames while drawing was counting the display's frames. Ten sites did. They were twenty per
cent fast on a sixty hertz monitor, nearly three times fast at 144, and kept running while the game
was paused, because `Draw` does not stop when `Update` does. `39670f1` had already claimed the fades
for the fifty hertz clock; these are the ones that did not go with it.

That changes what this document's central argument is, so read *Why this reopens what `PLAN.md`
closed* rather than assuming it still says what it said.

## Baseline

```
237 tests passing, 0 warnings, 0 errors
```

| Class | Lines | Code | Held objects | Mutable fields | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Blob` | 2,062 | 1,492 | 21 | **36** | The real one |
| `GameEngine` | 995 | 623 | 6 | 24 | God *builder*, not god behaviour |
| `EditorSession` | 934 | 597 | 1 | 19 | The editor's single bind target |
| `ScreenCanvas` | 912 | 624 | 1 | 7 | Two classes in one file |
| `Nasty` | 863 | 579 | 11 | 13 | Mid-tier, and booby-trapped |
| `MainGameScreen` | 676 | 424 | 1 | 6 | Coupling, not size |

"Code" is total lines less blanks and comments, because several of these files are a fifth `///` and
the raw count flatters them. "Held objects" is `readonly` backing fields — collaborators and
animations — and "mutable fields" is everything else, constants excluded. `NastyBrain.cs` is 941
lines and 576 of code over six mutable fields, which is why it is on the leave-alone list rather
than in this table. `Blob` and `Nasty` each gained a mutable field and lost a held one to the three
commits above - an angle that now belongs to them, and a `GameSession` that never did.

## The order, in one table

| Stage | Track | What comes out | Of | Tests with it |
| --- | --- | --- | --- | --- |
| 1 | `Blob` | `BlobSurroundings` | 4 fields, 1 method | Yes |
| 2 | `Blob` | `BlobWeapon` | 9 fields, 7 methods, ~200 lines | Yes |
| 3 | `Blob` | `BlobFall` | 5 fields, 3 methods | Yes, the best of them |
| 4 | `Blob` | `TunnelLiftRide` | 4 fields, 3 methods, ~155 lines | No |
| 5 | `Blob` | `OverloadSequence` | 7 fields, 5 methods | No |
| 6 | Editor | `ScreenPainter` | ~500 lines of `ScreenCanvas` | No |
| 7 | Editor | five clusters out of `EditorSession` | 19 fields | Some |
| 8 | Engine | `GameComposition` | ~330 lines of `GameEngine` | No |
| 9 | — | `NastyLifecycle`, `ScreenShake`, the fade | 7 + 3 + 2 fields | Yes for the last two |

Stages 1 to 5 are in order and each assumes the one before. Tracks B and C are independent of track A
and of each other. Stage 6 is the cheapest thing here and needs nothing to have happened first.

## Leave alone

- **`NastyBrain`** (941 lines). Two collaborators, six mutable fields outside its state struct, and
  one job done six ways. The `MoveType` dispatch in `MoveNasty` looks like it wants a strategy per
  type, and it does not want one yet: see *Why no strategy objects for the nasties* below.
- **`GameScreenCollection`** (597). Already cut from 1,938 by phase 4. Thirty members is a wide
  facade, and `DECOUPLING.md` has already decided that the planet is a genuinely wide collaborator
  rather than a role — nineteen files use twenty-seven distinct members of it. Splitting it would
  produce two classes every caller needs both of.
- **`ScreenRenderer`** (589), **`ScreenCollisionMap`** (589), **`WorldPopulator`** (590). All three
  are one subject each, all three have tests, and the biggest method in any of them is 67 lines.
  There is no second concern inside them to take out.
- **`ProjectMercury`** — vendored. `Emitter` at 597 and `Calculator` at 547 are someone else's.
- **`ToggleKeys.GetFriendlyName`** — still the only method in `Starquake.Shared` over 80 lines, and
  `REFACTORING.md` stage 4 named leaving it alone as an acceptable outcome. It still is.

## The rule each extraction is placed by

`IFireSurroundings` and `BlobFire` set the bar and it has not moved: **the test is not "what does
this class touch", it is "what does this class need in order to be run with no graphics device".**
An extraction that merely renames a slice of `Blob` has moved the problem.

Three more rules this document's subject matter adds:

**One owner per field.** Where a cluster of fields moves out, the fields move — they are not copied
and they are not shadowed. `_blobXPos`/`_blobYPos` are the exception that proves it: they are read
by nearly every method in `Blob`, so they stay where they are and every extracted piece is *handed*
the position per frame. Two copies of where the blob is would be worse than the god class.

**The draw path is read-only, and every extraction has to keep it that way.** It was not when this
document was written: ten places advanced frame-counted state while drawing, which is what the three
commits above fixed. Nothing in the stage list may put any of it back. The test is mechanical - the
script in the appendix finds every write reachable from a `Draw` - and a `Draw` that decides anything
is a defect rather than a style preference, because the two clocks run at different rates and only
one of them stops when the game is paused.

**A cluster still takes its `Draw` with it.** That used to be forced: a counter and the thing that
incremented it could not be separated. Now it is a choice, and still the right one - see the second
half of the next section for why a renderer split is possible and not wanted.

**No interface until a test wants one, and no machinery before checking the order.** Both are
`DECOUPLING.md`'s findings, earned five times over: every cycle it met turned out to be three values
or fewer, or a constructor in the wrong method. The same check applies here before anything below
reaches for a factory.

## Why this reopens what `PLAN.md` closed

`PLAN.md` already has a standing decision on this, under *`Blob` is the one god class still
standing*, and it goes the other way:

> The three clusters the phase-4 plan named and did not take — the weapon, the tunnel traversal and
> the death sequence — are each particle effects, sounds and drawing wrapped around two or three
> counters, so moving them would add no testable surface. Splitting `Blob` properly needs its update
> logic and its draw code prised apart first, which is a different task.

Those three are stages 2, 4 and 5 below, so this document has to say why that position changes.
Three reasons, and the first two matter more than the third.

**The prerequisite it names was real, and it has been paid.** "Prise the update logic and the draw
code apart first" is a fair description of what was in the way, and the first draft of this document
got it wrong in the other direction: it argued the prerequisite was *unmeetable*, on the grounds that
`Animation.GetNextFrame` is called from the draw path. That was simply false - all nineteen calls to
it are on update paths, and always were. What the draw path actually held was ten fade counters,
latches and sound triggers, and `cf95609`, `32c0345` and `07db88c` moved every one of them. So
`PLAN.md` was right about the obstacle and this document was wrong about its nature.

**With that paid, the question is what shape the split takes, and it is still not a renderer.** A
`BlobRenderer` is now possible - the draw path reads and does not write. It is still the wrong cut,
for an ordinary reason rather than a correctness one: it would have to read the position, the frame,
four fade levels, the scale, the shuttle flag and five animations' current frames, so either `Blob`
exposes most of its state publicly or the renderer takes a snapshot that is the god object with a
different name. Splitting by *subject* gives each subject a handful of fields and the few lines of
drawing that read them, which is what every stage below does.

**The survey measured the three clusters it had already named, and the two best candidates are not
among them.** `BlobSurroundings` (stage 1) and `BlobFall` (stage 3) were not on phase 4's list at
all. Falling is five counters of pure arithmetic — safe drop height, the running total, the
tunnel-mouth shuffle — with no particle effect, no sound in the deciding path and no draw code
whatever. It is the single most testable thing left in the class, and it was missed because the list
was of clusters, not of fields.

**On the weapon, phase 4 was half right.** The *effects* are what it says: sparks, a trail and a fade
with nothing to assert about them. The *decisions* are not — which way a shot leaves when the blob is
on the shuttle versus on foot, and `PointBlankReach`'s answer to a shot fired while standing on a
collectable, which is live arithmetic that has already drifted once. `BlobFire` and
`IFireSurroundings` got fourteen tests out of the half of the weapon that was extracted; this is the
other half of the same subject.

Stages 4 and 5 — the tunnel ride and the death sequence — are conceded the point. Neither adds much
testable surface, and both are on this list for the other reason: they are 130-odd lines and nine
fields that nothing else in the class has any business reaching. If the testable-surface rule is
read as the only rule, do stages 1, 2 and 3 and stop; the class is already much better at that
point. That is a legitimate place to stop and it is worth saying so before starting.

The one place this document agrees with `PLAN.md` without qualification is `MainGameScreen`, which
*Still deliberately out of scope* is right about — see stage 9.

## Verification, every stage

```sh
dotnet build Starquake.sln                          # must stay at 0 warnings, 0 errors
dotnet test Starquake.Tests/Starquake.Tests.csproj  # must stay green
```

Plus the draw-path check in the appendix, which must keep coming back empty. It is cheap and it is
the one piece of this that a reviewer cannot eyeball: a `Draw` that advances a counter looks like
ordinary code and goes wrong only on somebody else's monitor.

Stages 1 to 5 change the blob, which no test covers end to end, so each also wants the game launched
and the touched behaviour exercised by hand: walking and falling, firing on foot and on the shuttle,
a tunnel lift both ways, a death by spikes and a death by overload, and a screen change in each of
the four directions. The three commits above were signed off that way, on all eight of the things
they touched.

---

# Track A — `Blob`

Fifty-seven field declarations — 22 held objects and 35 mutable (`Blob.cs:252-327`). `PLAN.md` quotes
123 fields for this class; that figure includes the 53 `const`s, which `REFACTORING.md` stage 1 put
there on purpose and which are not state.

Method length is not the problem: `UpdateNormalState` is an eighteen-line pipeline of well-named
calls, which is phase 2's work and it worked. The problem is that those 57 fields fall into eight
groups that have nothing to do with each other, every one of 50-odd methods can reach all of them,
and the only thing keeping the groups apart is the naming convention.

The groups, from the field-to-method matrix. Held objects are counted with their group, because an
animation moves with the thing that advances it:

| Group | Fields | Methods |
| --- | --- | --- |
| Fire | 10 | 8 |
| Position and walking | 5 | 6 |
| Overload and death | 5 | 6 |
| Falling | 5 | 3 |
| Shuttle | 4 | 4 |
| Tunnel lift | 4 | 3 |
| Surroundings | 4 | 9 |
| Teleport arrival | 3 | 4 |

The surroundings row is the clearest signal in the matrix: `DetectSurroundingTiles` writes four
fields and **eight** unrelated methods silently depend on having been run first. That is why stage 1
is stage 1.

## Stage 1 — `BlobSurroundings`

The four `TileDetected?` fields become a record struct built once per frame and passed in.

- Moves: `DetectSurroundingTiles` becomes a function returning the value.
- Changes: `DetectShuttlePad`, `DetectTeleporter`, `DetectAccessBooth`, `DetectLeftHandTunnel`,
  `DetectRightHandTunnel`, `BreakBridgeUnderfoot`, `RideTunnelLift`, `HandleShuttlePad` take it as a
  parameter instead of reading fields.
- Fields: 35 mutable → 31.

Smallest stage here and deliberately first. It costs four fields and buys the thing every later
stage needs: the hidden ordering dependency inside `UpdateNormalState` becomes an argument, so when
a cluster moves out it is visible what it is being given. Testable on its own against a collision
map, which is one more class off the "needs a graphics device" list.

## Stage 2 — `BlobWeapon`

The biggest single win, and the one with a seam already cut for it.

| Takes | |
| --- | --- |
| Fields | `_blobCanFire`, `_blobFire`, `_blobFireFade`, `_blobFireRotation`, `_directionBlobFired`, `_blobAnimFire1`, `_blobAnimFire2`, `_blobFireTrail`, `_blobFireSparks`, `_blobOnShuttleCached` |
| Methods | `StartFiring`, `UpdateFire`, `AdvanceFireAppearance`, `StopFireEffects`, `PointBlankReach`, `DrawFire`, `DrawUpper`, `RegisterFireHit` |
| Members | `BlobFireX`, `BlobFireY`, `BlobHasFired` |
| Type | the nested `FireSurroundings` (`Blob.cs:2008`) goes with it |

Roughly 200 lines. `BlobFire` is already the projectile and already testable without a screen; this
is the launcher, which is the half that stayed behind.

**It also removes a back-reference.** `FireSurroundings.ThrowSparks` reaches into
`blob._blobFireSparks` (`Blob.cs:2028`) and that is the only reason it holds the blob at all. With
the sparks owned by the weapon, `FireSurroundings` needs the screen collection and a `Random` and
nothing else — so the nested private class can stop being nested.

What the weapon needs handed to it per frame: the blob's position, `DirectionBlobMoved`, whether he
is on the shuttle, and `PlayerStats.FirePower`. It raises `BlobAction.FiredLazer` the same way, via
a callback the blob provides rather than by holding the blob.

Tests: the fire-direction rules are currently reachable only through a live blob. On the far side of
this stage they are a constructor and three calls — which is how `BlobFireTests` came to have
fourteen of them.

## Stage 3 — `BlobFall`

`_blobFallHeight`, `_blobFallSpeed`, `_blobDistanceFallen`, `_blobTotalDistanceFallen`,
`_blobFallsSoundId` with `ApplyGravity`, `EdgeShuffleTowardOpening` and the landing arithmetic in
`ChangeScreenOffBottomEdge`.

Five fields, and the fall-damage sums are the part of the blob most worth having under test: how far
is a safe drop, when the total resets, and what the tunnel-mouth shuffle does at the boundary.
`EdgeShuffleTowardOpening` is 51 lines of pure geometry against the collision map and needs nothing
else. `WalkLeft`/`WalkRight` keep their one line that zeroes the fall height — they call the fall
object instead of writing the field.

## Stage 4 — `TunnelLiftRide`

`_inTunnelLift`, `_distanceInTunnelLift`, `_blobTunnelLiftSoundId`, `_directionGoingThroughTunnel`
with `RideTunnelLift` (83 lines, `Blob.cs:1362`), `DetectLeftHandTunnel` and `DetectRightHandTunnel`.

`_inTunnelLift` is read as a guard by `AccessInventory`, `ApplyGravity`, `ChangeScreenOffTopEdge` and
`UpdateDyingState`, so the bit stays queryable on the blob as a property over the ride object. The
behaviour is what moves. This is the one group that is already a mode in all but name.

## Stage 5 — `OverloadSequence`, and what is left

`_blobOverloadState`, `_blobAlarmSoundCounter`, `_blobScale`, `_resetBlobToScreenEntryPoint` with
`KillBlob`, `UpdateOverloadingState`, `UpdateDyingState`, `DrawOverloadAlarmState` and
`DrawOverloadFlare`.

Two of the three `BlobState` cases leave with this, and `Blob.Update`'s switch drops to one real
case and two delegations. The entry-point snapshot — `_blobScreenEntryLocation`, `_blobFrameOnEntry`,
`_blobOnShuttleOnEntry` — is written by `Update` and read only by `UpdateDyingState`, so it goes too,
which is three more fields than the list above.

What remains in `Blob` after stage 5: position, walking, control reading, the four screen-edge
changes, the inventory, the shuttle, the teleport fade, the event surface and a `Draw` that
dispatches. Around 500 lines and a dozen fields.

**The shuttle and the teleport fade are deliberately left.** The shuttle is four fields and four
methods, but `_blobOnShuttle` is read by **fourteen** methods across every other group — it is a mode
the whole class is aware of, and extracting the behaviour while that bit stays public buys less than
the churn costs. Teleport arrival is three fields and four methods totalling 33 lines; it is small
enough to be fine where it is. Both are worth revisiting once stages 1 to 5 have landed and the
class can
be read in one sitting, and neither is worth doing before that.

**`PerformPostChangeScreenActions` is the check on all of this.** It is 68 lines, and it touches the
fire group, the position group, the tunnel group, the jump platforms and the bridge tile resets. On
the far side it should be a fan-out of one call per owner. If it does not collapse to that, the
groups are wrong.

---

# Track B — the editor

Independent of track A — different project, no shared code — so these can go in any order relative
to the blob, and stage 6 is the cheapest thing in this document.

## Stage 6 — `ScreenPainter` out of `ScreenCanvas`

Eleven `Draw*` methods plus `StripRange`, `CoreArea`, `ScreenPixel` and the `Layout` record struct
(`ScreenCanvas.cs:447-873`) are around 500 lines, and the split is already most of the way made:
**seven of those fourteen members are `static` as they stand, and five more read nothing from the
control at all** — they take `(DrawingContext, EditorSession, Layout)` and use only those. Twelve of
the fourteen move to a `ScreenPainter` unchanged.

Only two need anything. `DrawPlaceholder` reads `Bounds` and takes a `Rect` instead.
`DrawHover` is the real one: it reads `_pointerOnScreen`, `_stroking`, `_strokeTool`, `_placingCore`,
`_hoverX` and `_hoverY`, which are *input* state and stay with the control — so the painter is handed
a hover value rather than the six fields. That one parameter is the whole seam, and it is worth
noticing that it is only drawing that needed any of the control's state in the first place.

What is left is an Avalonia control of about 350 lines doing input and hit-testing: the four
`OnPointer*` overrides plus `OnPointerCaptureLost`, `OnKeyDown`, `ToolFor`, `TryHitTest`,
`EndStroke`, `ReportHover`, `PlaceCore` and the attach/detach pair. The painter becomes the only
thing in the editor that knows tile geometry, which is where `CollisionMaskEditor` and `WorldMapView`
should eventually be getting theirs from too.

Near-mechanical, no behaviour risk, and the largest line reduction per unit of effort here. It does
not need any of the rest of this document to have happened.

## Stage 7 — `EditorSession`'s five clusters

Nineteen mutable fields in five groups with no overlap at all between them:

| Extract | Fields | Notes |
| --- | --- | --- |
| `CanvasViewOptions` | `_showGrid`, `_showCollision`, `_showNeighbours`, `_showTileNumbers`, `_paletteTileWidth`, `_hoverStatus` | Pure preferences. No logic whatever — six properties and a `PropertyChanged` |
| `TilePaletteState` | `_selectedTile`, `_tileFilter`, `_usedTilesOnly`, `_visibleTiles`, `_tilesInUse`, `_recentTiles`, `_sheet` | With `RebuildVisibleTiles`, `Matches`, `TilesInUse`, `RememberTile`, `SelectedMarker`, `SelectedTileDescription` |
| `MarkerPlacements` | `_blobStart`, `_shipCrashSite`, `_coreExit`, `_core` | With `FindAuthoredPositions`, `TakeAwayMarkerElsewhere`, `PlaceCore`, `Square` and the two `*Note`/`*Display` pairs |
| `ScreenOperations` | `_clipboard` | With `CopyScreen`, `PasteScreen`, `ClearScreen`, `FloodScreen`, `MoveScreen`, `ReplaceScreen`, `CanPaste` |
| `TeleportCodeEditor` | — | `CurrentTeleportCode`, `CurrentScreenHasTeleportBooth`, `ShowTeleportPanel`, `TeleportCodeDisplay`, `SetTeleportCode`, `DescribeTeleportCodeProblem`, `RaiseTeleportPropertiesChanged` |

`_currentScreen` stays on the session. Like the blob's position it is what everything else is
relative to, and the sub-objects are handed it or handed the document.

**The cost is bindings, and it is small enough to pay.** `EditorSession` is a `ViewModelBase`, and
`MainWindow.axaml` binds seventeen distinct members through it across 25 lines — eight of those
occurrences being the four view toggles. Exposing each cluster as a property means those become
`Session.View.ShowGrid` and so on. The four controls reach it in code rather than XAML — `ScreenCanvas`
uses 22 members, `TilePalette` 8, `WorldMapView` 5, `CollisionMaskEditor` 6 — and those are
compiler-checked, so the churn is mechanical and visible.

Do this after stage 6, not before: `ScreenCanvas` is the heaviest consumer of the session and it is
easier to re-point 22 members once the control is half the size.

---

# Track C — the two that are not what they look like

## Stage 8 — `GameComposition` out of `GameEngine`

`GameEngine` is not a god class by behaviour any more; `DECOUPLING.md` spent twenty-odd commits
making sure of that. What it still is, is the thing that builds everything and the thing everything
reaches each other through, and those are the same fact.

Around 330 of its 998 lines are construction: `BuildComponentsNeedingLoadedContent` (118,
`GameEngine.cs:464`), `BuildGameScreens` (111, `:638`), `RegisterColourAnimations` (56, `:582`),
`RegisterScreenEffectViewPorts` and `DefineFonts`. Those move to a `GameComposition` that builds the
actor graph and hands back what was built. What stays is `Update`, `Draw`, `Dispose` and the seven
event-relay handlers — a frame loop, which is what the class is named after.

**The twelve `NotBeforeContentLoads` properties (`GameEngine.cs:213-249`) are the measure of whether
this worked.** They exist so that a class built before content can reach, later, something built
after it. Every one of them is a route from any file to any actor. Once something else owns the
graph and hands each actor its collaborators directly, they have no callers — and that is the
remaining half of `DECOUPLING.md`'s stage 6, which is why this stage is where it belongs rather than
in that document's second pass.

Check the order before building anything to manage it. That has been the right move five times
running in `DECOUPLING.md`, and three of its "blocked on construction order" findings turned out to
be a constructor sitting in `Initialise()` for no reason.

## Stage 9 — the small true extractions

Two classes where the god-class reading is wrong but there is real work anyway.

**`Nasty`** — the lifecycle group is `_birthColourLevel`, `_birthFadeLevel`, `_birthScale`,
`_birthTimer`, `_conceptionWaitCounter`, `_nastyDeadPauseCounter`, `_deathAnticlockwise`, `_rotation`
with `BeginConception`, `WaitToBeBorn`, `UpdateBirth`, `StartDying`, `UpdateDeath`,
`AdvanceRotation`. It separates cleanly
from the drawing, and **`CopyFromSource` is a trap sitting on top of it**
(`Nasty.cs:265`). A screen swap hands the spare set the live set's state, and what carries is: the
whole `NastyMovementState` record struct, plus `NastyType`/`XPos`/`YPos`/`KeepNastyTheSame`, plus
`_nastyType` and `_nastyDeadPauseCounter` by hand. The birth counters deliberately do **not** carry —
a nasty mid-birth becomes `Normal` on the swap. So anything the lifecycle object owns must either
sit inside a copied value or be named in `CopyFromSource`, and the birth fields must stay
uncarried. `_rotation` arrived with `32c0345` and is deliberately uncarried for the same reason - a
swap resets a birth or a death, so there is no spin to continue - which is written on the field.

That is the exact mistake `NastyMovementState` was created to end, and it will not announce itself:
it shows up as a nasty that occasionally keeps growing after a screen change.

**`MainGameScreen`** does not want splitting, and `PLAN.md`'s *Still deliberately out of scope* has
already settled that: it uses every actor because it *is* the main game, and the alternatives are an
eighteen-parameter constructor or a bag object existing solely for it. Nothing here disturbs that. Its
problem is 18 distinct `GameEngine` members, which is `DECOUPLING.md`'s job rather than this
document's — it is the widest of the six broad consumers that document has always said it comes down
to. What it does have is two extractions that are not splitting-on-size: `ScreenShake`
(`_shakingScreen`, `_shakingScreenCount`, `_shakingScreenCounter` with `ShakeScreen`, 63 lines at
`MainGameScreen.cs:438`) and the fade pair (`ScreenFade`, `ScreenFadeSpeed`, `FadeScreenBackUp`,
`ResetScreenFade`). Both are arithmetic over a counter, both are currently untested, and both are
testable the moment they are their own class.

---

## Why no strategy objects for the nasties

`NastyBrain.MoveNasty` dispatches on `MoveType` to six movement methods, and the instinct is one
class per `MoveType` behind an interface. It is the wrong move here, for a reason specific to this
codebase rather than a general objection.

The movement state has to **copy whole**. `NastyMovementState` exists because `CopyFromSource` used
to assign seventeen fields by hand out of thirty-five, and anything added afterwards was silently
dropped at every screen change. Six strategy objects holding their own state would reintroduce
exactly that: the state that carries would once again be decided by what someone remembered to
write down, and nothing would say so.

Stateless strategies taking `ref NastyMovementState` would preserve the invariant, and then the
interface buys nothing that the `MoveType` switch does not already give — the dispatch is one if/else
chain with six arms and each arm is a single call, and the shared step-and-reflect helpers
(`MoveNastyOneStep`, `DistanceClear`, `Reflected`, the component and sign functions) are used by most
of the arms, so they would all need passing a context object that is the brain with a different name.

`NastyBrain` has twelve test methods and one subject. Leave it.

## What this document does not fix

Coupling. Nothing below stage 8 reduces anyone's dependency count, and two of the extractions
nominally increase it — a `Blob` that holds a `BlobWeapon` and a `BlobFall` has two more
collaborators than it did. That is the intended trade: fewer fields reachable from fewer places, in
exchange for named objects that each fit in a reader's head and most of which can be put under test
without a graphics device. `DECOUPLING.md` is the document about dependency counts and it is not
finished; these two pieces of work are orthogonal and can proceed in either order.

## Appendix — how the table was measured

Both scripts below were run to produce the figures in this document, and every number in the baseline
table comes out of the first one. Run from the solution root.

Code lines, held objects and mutable fields per class:

```sh
for f in $(find Starquake.Shared ScreenEditor SharedLibs -name "*.cs" \
             -not -path "*/obj/*" -not -path "*/bin/*"); do
  awk -v F="$f" '
    /^    (private|public|protected)( readonly)? [A-Za-z][][A-Za-z0-9<>?,. ]* _?[A-Za-z0-9]+ *(=|;)/ \
      && !/\(/ && !/ const / && !/ event / && !/=>/ { if (/ readonly /) held++; else mutable++ }
    /^[[:space:]]*\/\// || /^[[:space:]]*$/ { next }
    { code++ }
    END { if (code > 350) printf "%5d code  %3d held  %3d mutable   %s\n", code, held, mutable, F }
  ' "$f"
done | sort -rn
```

`const` is excluded because a constant is not state — that is the whole difference between this
count and `PLAN.md`'s 123 for `Blob`. Events are excluded because they are a surface, not a field to
be owned.

Everything the draw path writes. This is the check *Verification* calls for, and the three commits
under *What has landed already* are what it took to get its output down to the two lines below:

```sh
python3 - <<'PY'
import os, re

MUTATION = re.compile(r'^\s+(?:this\.)?(?:_\w+|[A-Z]\w*)\s*(?:=[^=]|\+=|-=|\*=|/=|\+\+|--)')
SIDE_EFFECT = re.compile(r'(?<![.\w])(?:\w+\.)?(?:GetNextFrame|PlaySound|Trigger|Launch)\s*\(')
SIGNATURE = re.compile(r'^\s{4,8}(?:\[[^\]]*\]\s*)?(?:public|private|protected|internal|static)'
                       r'[^;=]*?\b([A-Za-z_]\w*)\s*\(')
CALL = re.compile(r'(?<![.\w])([A-Za-z_]\w*)\s*\(')
KEYWORDS = {'if', 'for', 'while', 'switch', 'return', 'new', 'foreach', 'catch', 'lock',
            'using', 'nameof', 'typeof', 'sizeof', 'do', 'else', 'Equals'}

for root, _, names in os.walk('Starquake.Shared'):
    if '/obj' in root or '/bin' in root:
        continue
    for name in (n for n in names if n.endswith('.cs')):
        path = os.path.join(root, name)
        src = open(path).read().split('\n')

        bodies = {}
        marks = [(i, SIGNATURE.match(l).group(1)) for i, l in enumerate(src)
                 if SIGNATURE.match(l) and ' class ' not in l and ' record ' not in l]
        for (i, method), (j, _) in zip(marks, marks[1:] + [(len(src), '')]):
            bodies.setdefault(method, []).append((i, j))

        # everything the draw path can reach, resolved within the class
        reached, pending = set(), [m for m in bodies if m.startswith('Draw') or m == 'End']
        while pending:
            method = pending.pop()
            if method in reached or method not in bodies:
                continue
            reached.add(method)
            for i, j in bodies[method]:
                pending += [c for c in set(CALL.findall('\n'.join(src[i:j]))) - KEYWORDS
                            if c in bodies and c not in reached]

        constructor = name[:-3]
        for method in sorted(reached - {constructor}):
            for i, j in bodies[method]:
                for k in range(i, j):
                    line = src[k].strip()
                    # a statement ends in a semicolon; an object initialiser's entries do not
                    if line.startswith('//') or not line.endswith(';'):
                        continue
                    if MUTATION.match(src[k]) or SIDE_EFFECT.search(src[k]):
                        print(f'{path}:{k + 1}  {method}  {line}')
PY
```

It walks out from every `Draw*` method and `End`, resolving calls within the class, and reports any
assignment or known side effect it can reach. Two hits are expected and allowed:

```
Starquake.Shared/ActiveSprites/Nasty.cs:334  CheckSpritesInUse  _nastySprite = _nastySpriteOriginal;
Starquake.Shared/ActiveSprites/Nasty.cs:338  CheckSpritesInUse  _nastySprite = _nastySpriteNew;
```

Those pick which of the two cloned sheets to draw from, by art set. They write the same value on
every frame, so the refresh rate cannot change what they do - which is the test for whether a
draw-path write matters. **Anything else in that output is a defect.** Resolution is within the class
only, so a draw helper that mutates through a collaborator will not be caught; that is the known gap,
and reading the collaborator's own `Draw*` closes it.

The field-to-method matrix behind track A — which methods touch which fields, which is what the
groups were cut along:

```sh
python3 - <<'PY'
import re
src = open('Starquake.Shared/ActiveSprites/Blob.cs').read().split('\n')
fields = sorted({m.group(1) for l in src
                 if (m := re.match(r'^    private (?:readonly )?[\w<>?\[\],. ]+? (_\w+)', l))})
starts = [i for i, l in enumerate(src)
          if re.match(r'^    (?:public|private|protected|internal|static)', l) and '(' in l]
for a, b in zip(starts, starts[1:] + [len(src)]):
    name = re.sub(r'.*?(\w+)\(.*', r'\1', src[a].strip())
    used = [f for f in fields if re.search(r'\b' + f + r'\b', '\n'.join(src[a:b]))]
    if used:
        print(f'{name} ({b - a}) : {" ".join(used)}')
PY
```

Point it at any file by changing the path. The method pattern needs a `(` to start counting, so a
method followed by a run of get-only properties is credited with their lines — `REFACTORING.md`
records the same caveat about its own script. Use them for ranking, not for reporting.
