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
| **Stage 7** | `CanvasViewOptions`, `AuthoredPositions` and `TilePaletteState` out of `EditorSession`. 19 mutable fields to 5; 597 code lines to 404. Two of the plan's five clusters are recommended against rather than done - see below. |
| **Stage 6** | `ScreenPainter`, `ScreenLayout` and `Hover` out of `ScreenCanvas`. 912 lines to 400; the control drops out of the table entirely. No tests - it draws. |
| **Stage 5** | `BlobOverload` and `BlobEntryPoint`. Six fields and the death's arithmetic out, plus ten constants. Twelve tests. **`Blob` is no longer the worst class in the codebase by mutable field count** - `GameEngine` is. |
| **Stage 4** | `TunnelLift`. Three counters and the 83-line ride out, into 111 code lines. Nineteen tests - the plan said none were to be had here, and was wrong for the reason stage 3 found. The two tunnel-mouth detectors the plan grouped with it stay where they are. |
| **Stage 3** | `BlobFall`. Four counters and the tunnel-mouth shuffle out, into 99 code lines that need a collision map and nothing else. Eleven tests, and this time the plan's promise held - see below for why, and for the one case left uncovered. |
| **Stage 2** | `BlobWeapon`. Ten fields and seven methods out, 219 code lines, and the back-reference that made the blob hand out `this` mid-construction. Twelve tests - but only over the one rule in it that is pure, which is not what this plan promised. |
| **Stage 1** | `BlobSurroundings`. Four `TileDetected?` fields become one value, probed by a free function against a `ScreenCollisionMap` and handed to each of the eight readers as the tile it actually uses. Eight tests, where there could be none before. `Blob` 36 mutable fields to 33. |
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

Measured before stage 1, and the verdict column is now out of date: **after stages 1 to 5 `Blob` holds
16 mutable fields, fewer than `GameEngine`'s 24 or `EditorSession`'s 19.** It is still the largest file,
at 1,081 code lines against `GameEngine`'s 623, but it is no longer the god class this document was
written about. 299 tests.

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
| ~~1~~ | `Blob` | `BlobSurroundings` | 4 fields into 1, 1 method | **done** - 8 tests |
| ~~2~~ | `Blob` | `BlobWeapon` | 10 fields, 7 methods, 219 lines | **done** - 12 tests, see below |
| ~~3~~ | `Blob` | `BlobFall` | 4 fields, 2 methods | **done** - 11 tests |
| ~~4~~ | `Blob` | `TunnelLift` | 3 fields, 1 method, 83 lines | **done** - 19 tests |
| ~~5~~ | `Blob` | `BlobOverload`, `BlobEntryPoint` | 6 fields, 10 constants | **done** - 12 tests |
| ~~6~~ | Editor | `ScreenPainter` + 2 values | 437 lines of `ScreenCanvas` | **done** |
| ~~7~~ | Editor | three clusters out of `EditorSession` | 14 of 19 fields | **done** |
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

**Read the build's summary line rather than grepping it for an error prefix.** The editor's XAML errors
are `AVLN####`, so a filter written for `CS` and `IDE` drops them - and the XAML task is skipped as up
to date on a second build even when the first one failed, which makes a repeated build on a broken file
report success. Stage 7 met both. See it for the detail.

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

## Stage 1 — `BlobSurroundings` — **done**

The four `TileDetected?` fields become one `readonly record struct`, probed by
`BlobSurroundings.Around` and handed to each reader as the tile it uses -
`DetectTeleporter(TileDetected?
toTheRight)` rather than the whole value, so each signature says what it depends on.

**Four fields became one, not none, and that is the stage's finding.** The plan said a local passed
in, and 35 fields down to 31. It cannot be a local, because `HandleShuttlePad` runs *before*
`DetectSurroundingTiles` in `UpdateNormalState` and reads the previous frame's tile. That looked like
a bug worth its own commit - a method reading a stale probe because of where it had been dropped -
and it is not one. `DetectShuttlePad` sets `_blobOnShuttlePad` from the same reading at the end of the
frame, so the flag and the tile beneath it are a matched pair, and probing afresh at the top of the
next frame would hand `HandleShuttlePad` a tile that no longer matches the flag telling it to act. The
detection genuinely spans the frame boundary. One field holds it, and the boundary is written down at
the call site instead of being an accident of ordering.

So the count is 36 mutable fields to 33. The gain is not the three fields; it is that eight methods
which silently required a ninth to have run first now take what they need as an argument.

**Eight tests, where there could be none before.** `GameScreenCollection.CheckTileByObject` is a pure
delegate to `ScreenCollisionMap.CheckTileByObject`, so `Around` takes the map and nothing else - no
content pipeline, no graphics device. The tests pin the four directions against each other (a
transposition in a four-line constructor is exactly the mistake this refactor could have made) and
the separate downward reach, which is the one piece of real behaviour the probe encodes: riding the
shuttle makes the blob taller, the extra height is shuttle rather than blob, so the ground has to be
looked for from his own feet or he reads the floor through the shuttle.

One thing the tests pinned down on the way: the probe reports which tile *image* is in the way and
never consults the collision mask. That is what its callers need - a shuttle pad and a tunnel mouth
are walked into, not blocked by - and it is why the tests place tiles and set no cells.

## Stage 2 — `BlobWeapon` — **done**

Ten fields, seven methods and the nested `FireSurroundings` out of `Blob`, into a 265-line class.
`Blob` drops 219 code lines, five held objects and five mutable fields, and three members off its
public surface.

| Moved | |
| --- | --- |
| Fields | `_blobCanFire`, `_blobFire`, `_blobFireFade`, `_blobFireRotation`, `_directionBlobFired`, `_blobAnimFire1`, `_blobAnimFire2`, `_blobFireTrail`, `_blobFireSparks`, `_blobOnShuttleCached` |
| Methods | `StartFiring`, `UpdateFire`, `AdvanceFireAppearance`, `StopFireEffects`, `PointBlankReach`, `DrawFire`, `CheckForBlobCollisionWithObject`, `GetCollisionObjects` |
| Members | `BlobFireX`, `BlobFireY`, `BlobHasFired`, `RegisterFireHit` become `Weapon.X`, `.Y`, `.HasFired`, `.RegisterHit` |
| Type | `FireSurroundings` |

**The back-reference is gone, which was the point as much as the size.** `FireSurroundings` held the
blob for one call - `ThrowSparks` - and that was the only reason `Blob`'s constructor handed out
`this` before it had finished running. The weapon owns the sparks, so `FireSurroundings` takes the
effect instead and the blob gives out nothing.

`PointBlankReach` took `CheckForBlobCollisionWithObject` and `GetCollisionObjects` with it: each had
exactly one caller, all the way up the chain, so the whole question of what the blob is standing on
belonged to the shot and nothing else used it.

**The plan was wrong about the tests, and this is the honest version.** It said the fire rules would
be "a constructor and three calls" on the far side. They are not: `BlobWeapon`'s constructor pulls
two particle effects out of the `ResourceManager`, and `DrawFire` a sprite sheet, so the class cannot
be built without the content pipeline. Extracting it did not make it testable.

What *is* testable is the one rule in it that is pure, and it is the rule worth having: which way a
shot leaves. On the shuttle he fires whichever way he is moving, including up and down; on foot a
shot only ever goes left or right, so a blob standing still fires the way he last walked. That is now
`static BlobWeapon.DirectionOfShot(directionBlobMoved, onShuttle, lastFired)` and has twelve tests,
including the two cases that follow from carrying the last direction - getting off the shuttle still
aimed upwards, and firing upwards until he walks again.

The lesson for stages 3 to 5: **ask what in the cluster is arithmetic before promising a test for the
cluster.** Stage 3 is five counters and genuinely pure; stages 4 and 5 were already conceded to be
effects and sound.

**Kept verbatim, deliberately.** `StartFiring`'s three nested conditions and its two-armed direction
test moved unchanged, and `StopFireEffects` kept its `if (!HasFired)` rather than becoming an early
return. `REFACTORING.md` is explicit that a move commit is the wrong place to spend the risk of
inverting a guard, and flattening those is one of its open follow-ups. The one thing tidied on the way
was the local called `temp`, which that document also lists.

Verified as motion rather than rewriting: every moved body is textually identical to its original
once the field renames and the one intended parameter change are substituted back.

Two things that are not identical, both stated rather than smuggled:

- `Blob.Update` stepped the sparks, then the teleport beam, then the trail. The weapon steps the
  sparks and the trail together, so the beam now goes first. The three are independent particle
  effects.
- `StartFiring`, `UpdateFire` and `StopFireEffects` were three consecutive calls in
  `UpdateNormalState` and are now one `Weapon.Step`, which calls the same three in the same order.

`Animation.Looping` came out of `Blob`'s private `LoopingAnimation` so both classes can build a cycle
without repeating seven property assignments. `Nasty` still assigns them by hand and was left alone.

## Stage 3 — `BlobFall` — **done**

The four counters - `_blobFallHeight`, `_blobFallSpeed`, `_blobDistanceFallen`,
`_blobTotalDistanceFallen` - with the gravity arithmetic and `EdgeShuffleTowardOpening`, into 99 code
lines. `Blob` drops 70 code lines and four mutable fields, and four constants go with them.

**The plan's promise held this time, and the reason is worth writing down.** `BlobFall` needs a
`ScreenCollisionMap` and nothing else, so it constructs in a test. Two things made that true, and
neither was luck:

- `GameScreenCollection.CheckCollision` is a pure delegate to `ScreenCollisionMap.CheckCollision`,
  exactly as `CheckTileByObject` was for stage 1. The planet is not needed, only the map.
- **The falling sound stayed behind.** It is the one part of gravity that needs the resource manager,
  and taking it would have made the class as untestable as `BlobWeapon` is. So `Step` returns whether
  the drop has become audible and `Blob` plays the sound. That is the whole difference between a stage
  that earned tests and one that did not.

Eleven tests: the starting speed, standing on the ground, the step up in speed on the fourth frame of
a drop, the terminal speed being reached and never exceeded, a short drop being silent, a long one
becoming audible, landing clearing the fall height, and the shuffle not firing on a blob nobody is
steering.

**`StartFallingAgain` clears the speed and not the total, deliberately.** A drop through the bottom of
one screen onto the next is one fall: clearing the total too would cut the falling sound off at every
screen boundary, and a test pins it.

**One case is left uncovered rather than approximated.** The shuffle *catching* the lip of an opening
needs a fall fast enough to overshoot it and a ledge inside that overshoot, and the probe is a
65-pixel box one blob width to the side - so the geometry takes several tiles of hand-built collision
cells to construct. The first attempt at it put the blob inside its own ledge, which the test caught.
The negative case is covered and the positive one is named in the test's remarks.

Two things that are not identical, stated rather than smuggled:

- `ApplyGravity` played and stopped the falling sound *before* the shuffle ran. The decision is taken
  at the same point, but `Blob` now acts on it after `Step` returns, which is after the shuffle. The
  shuffle does not touch the sound.
- The repeated `CheckCollision(Direction.Down, ...)` - the same call either side of the speed step -
  is one private `DistanceToTheGround`, and the two `x = x + y` counters are `+=`.

`EdgeShuffleTowardOpening` moved verbatim: its body is textually identical once the field reads become
the parameters it is handed.

## Stage 4 — `TunnelLift` — **done**

`_inTunnelLift`, `_distanceInTunnelLift` and `_blobTunnelLiftSoundId`'s decisions with the 83-line
`RideTunnelLift`, into 111 code lines. `Blob` drops 67 code lines, two mutable fields and two
constants.

**The plan grouped two unrelated things under this stage, and only one of them moved.** It listed
`DetectLeftHandTunnel` and `DetectRightHandTunnel` alongside the lift because all three say "tunnel".
They share nothing else: the lift is a vertical mode with three counters and one collaborator, while
the detectors are a *screen change* - they touch `_gameScreens`, `_screenEffects`, `_resources` and the
`OnScreenChanged` event to set one field, `_directionGoingThroughTunnel`, which only
`PerformPostChangeScreenActions` reads. Four collaborators and an event for one field is not a cluster
worth extracting; it is the blob changing screens, which is what the blob is for. They stay.

So the measured group was three fields and one method, not four and three.

**Nineteen tests, where the plan said there would be none.** It had this stage down as "effects and
sound wrapped around counters", and stage 3 found what actually decides that: whether the resource
manager can be left behind. Here it can, by the same trade - the lift takes its sound as two
callbacks, so `Blob` keeps the sound engine and the lift keeps the decision about when to start and
stop. The tests count those callbacks, because *when* the sound starts is part of the behaviour.

What they pin: getting on only under the middle of a shaft (walking past the edge of one must not grab
him), all four shaft tiles carrying him, the three recognised tops ending the ride, the minimum ride
before he may step off sideways, steering into a wall not counting as a way out, and the minimum ride
counting afresh from each shaft - so a blob who has ridden once cannot step straight out of the next
one he enters.

### A fourth lift top that the ride has never recognised

`ActionTiles.TunnelLiftTopAlternate` (396) is documented as a lift top, and both `ScreenRenderer` and
`TunnelLiftAnimationEngine` treat it as one. `RideTunnelLift` never has - its switch lists
`TunnelLiftTop1`, `2` and `3` only.

Measured against the shipped map rather than guessed at: **tile 396 is placed fifteen times across
nine screens and is never directly above a shaft tile**, so it never caps one and the asymmetry cannot
bite today. Were a screen ever authored with it as the top of a shaft, the ride would carry on
straight past it.

Left exactly as found, and pinned by a test that says so in its name. A move is the wrong commit to
change behaviour in, and this is worth a decision rather than a reflex: the screen editor could make
it reachable tomorrow, so either the ride should recognise 396 or the editor should refuse to place it
as a cap. That is a defect question, and it gets its own commit if it gets one at all.

**Two things that are not identical, stated rather than smuggled:**

- The distance test was inside each direction's arm; it is now checked once, before either. Logically
  identical - both arms did the same thing - and the only observable difference is that
  `CheckCollision` is not called at all when the ride is too short, which is a pure query.
- The original's `else if` meant that holding both directions with a wall to the left still stepped
  him off to the right. That is preserved as an `||` rather than an early return, and the comment at
  the site says why.

## Stage 5 — `BlobOverload` and `BlobEntryPoint` — **done**

Two extractions, because the group turned out to be two things. `Blob` drops 41 code lines, six mutable
fields and ten constants, and lands at **16 mutable fields - fewer than `GameEngine` or
`EditorSession`.** This is where it stops being the subject of this document.

**`BlobOverload`** owns the shape of a death: the alarm counter, the stage, and the scale, with the
swell-shrink-explode progression and the flare's geometry. 64 code lines, needs nothing at all to
construct, and has twelve tests - the most thorough of the five pieces taken out of the blob.

**`BlobEntryPoint`** collapses `_blobScreenEntryLocation`, `_blobFrameOnEntry` and
`_blobOnShuttleOnEntry` into one named object. Worth checking before believing: arriving is written
from two places, and both are genuine arrivals - `Update` records walking or falling onto a screen, and
the position setters record being *placed*, which is what a teleport booth, an access booth and the
planet core's exit all do. Those three call sites are why the setters write it at all.

### The condition that looks vacuous and is not

`UpdateOverloadingState` tested `_blobOverloadState < OverloadExploded && _blobAlarmSoundCounter <
OverloadAlarmFrames`. The first half reads like dead weight: the swell only runs once the counter has
reached sixty, so the stage cannot have advanced while the counter is still below it.

It is the mechanism by which a death skips the alarm. `KillBlob(_, overloadFirst: false)` sets the stage
to `Exploded` with the counter at nought, so the first half is false from the start and the whole alarm
phase is stepped over - that is how spikes, electricity, a nasty and the debug key kill instantly, while
running out of energy (the only `overloadFirst: true` caller, in `StatusPanel`) gets its second of
warning. `BlobOverload.AlarmSounding` keeps both halves and the doc comment on `Begin` says why.

Worth the paragraph because `PLAN.md` has a section on exactly this shape - conditions from the original
port that are always true - and this one is the counter-example.

`OverloadSwelling`, `OverloadShrinking` and `OverloadExploded` were `int` constants numbered 0, 1 and
**3**. Nothing ever held the gap at 2, and the only comparison made of them was against `Exploded`, so
they became an `OverloadStage` enum.

### What did not move, and the plan's last wrong prediction

The plan said two of the three `BlobState` cases would leave with this stage, and `Blob.Update`'s switch
would drop to "one real case and two delegations". It has not, and should not. What moved is the
arithmetic *inside* `UpdateOverloadingState` and `UpdateDyingState`; the methods themselves stay,
because between them they touch the death animation, the weapon, the jump platforms, the planet, the
`OnBlobDiedAndRespawned` event, the blob's position and his height. `UpdateDyingState` is almost
entirely the business of putting a blob back together, which is the blob's own business.

That is the third stage in a row where the plan's prediction about *shape* was wrong while its
prediction about *which fields cluster* was right. The fields were measured; the shapes were guessed.

### Flare geometry, and a note on the drawing rule

`FlareScales()` yields the sizes the explosion's copies are drawn at, so the stack geometry - five
copies, growing outward, skipping any that would come out inside out - is testable and `Blob` keeps only
the `foreach` and the alpha. That is a partial exception to *a cluster takes its `Draw` with it*: the
drawing itself needs the blob's sprite sheet, position and frame, so moving it would mean handing over
most of the blob, exactly as a `BlobRenderer` would. The geometry moved and the drawing stayed.

---

# After the blob

Five stages in, `Blob` has gone from 1,492 code lines over 36 mutable fields to **1,081 over 16**, and
from 237 tests in the suite to 299. Five of the eight field groups are gone.

What is left in it is one subject: a blob that walks, falls, carries things, rides a shuttle, changes
screens and dies - holding a weapon, a fall, a lift, an overload and an entry point, each of which can
be reasoned about and most of which can be tested. The three groups left inside it, named in stage 5's
original plan as deliberately-left, are still the right call: the shuttle because `_blobOnShuttle` is a
mode fourteen methods are aware of, the teleport fade because it is three fields and thirty lines, and
the position because everything is relative to it.

**The remaining four stages are not about the blob**, and nothing in them depends on anything above.

---

# Track B — the editor

Independent of track A — different project, no shared code — so these can go in any order relative
to the blob, and stage 6 is the cheapest thing in this document.

## Stage 6 — `ScreenPainter` out of `ScreenCanvas` — **done**

Thirteen drawing members, the `Layout` struct and seven drawing resources out of the control. 437 lines
moved; `ScreenCanvas` goes from 912 lines to **400**, and at 265 code lines it is no longer in this
document's table at all.

Three files, not one, because the control still needs two of the things the drawing needs:

- **`ScreenPainter`** (347 code lines, no state at all) — every `Draw*`, plus `Paint`, which took over
  `Render`'s body. Which overlays go down and in what order is drawing policy and belongs here.
- **`ScreenLayout`** — the `Layout` record struct, promoted out of the control because `TryHitTest` and
  `ScreenPixel` need the same geometry to turn a pointer position back into a tile. `NeighbourTiles`
  moved onto it, since `For` is what reads it.
- **`Hover`** — the one thing the painter needed that the session does not carry. `DrawHover` reached
  into six control fields: the hovered tile, the pointer position, whether a stroke is in progress, and
  which tool it started with. Those are *input* state and stay with the control, which now hands over a
  value. That one parameter is the whole seam.

`ScreenCanvas.Render` is two lines. What is left of the control is the dependency property, the six
`OnPointer*`/`OnKeyDown` overrides, `ToolFor`, `TryHitTest`, `EndStroke`, `PlaceCore`, `ReportHover`,
`ScreenPixel` and the attach/detach pair — input and hit-testing, which is what a control is for.

**Pure motion, mechanically checked.** All thirteen moved bodies are textually identical to their
originals once `Layout` → `ScreenLayout`, the six hover fields → the `Hover` value, and `Bounds` →
the `bounds` parameter are substituted back. The layout struct differs in exactly two ways, both
intended: `private` became `internal`, and it gained the `NeighbourTiles` const.

Three usings fell out of the canvas with the drawing (`System.Globalization`,
`ScreenEditor.Styles`, `SharedLibs`), which the analyser does not flag here, so they were checked by
hand against the types each provides.

**No tests, and none to be had.** Everything moved writes to a `DrawingContext`; there is no arithmetic
in it that a test could assert on without a rendering surface. `ScreenLayout.For` is the one exception -
it is pure geometry and could be pinned - but it is twelve lines that the whole editor exercises
visibly on every resize, so a test would be describing what the window already demonstrates. The
stage's value is the 437 lines and the control dropping out of the table, not coverage.

## Stage 7 — `EditorSession`'s clusters — **done, three of five**

19 mutable fields to **5**, and 597 code lines to **404**. The five that stay are the five that
should: `_currentScreen`, because everything else is relative to it; `_sheet`, because the loaded
artwork is read by four separate controls and is not the palette's to own; and `_selectedTile` with
`_tool`, for the reason below.

| Extracted | Fields | What it is |
| --- | --- | --- |
| `CanvasViewOptions` | 6 | The four overlay toggles, the palette's tile width and the hover status line. Preferences about looking, with nothing in them about the map |
| `AuthoredPositions` | 4 | The blob's start, the crash site, the core exit and the core, with the landing-site verdict built on the first two |
| `TilePaletteState` | 5 | The filter, the used-tiles-only switch, the visible list, the in-use cache and the recent row |

### Where the plan was wrong, and where this session's own work changed it

**`_selectedTile` does not belong with the palette.** The plan put it in `TilePaletteState` along with
the filtering. Earlier in this same session, the editor gained an invariant - *what is in hand
follows the tool* - which ties `SelectedTile` and `Tool` together through one `WhatGoesInHandFor`
switch: erase takes nothing, the brush takes the last ordinary tile back, fill never takes a marker.
They are now one concept, "what the next click will do", and splitting them across two objects would
have put that switch on one side of a boundary and half its state on the other. They stay together
on the session.

So `TilePaletteState` is about what the palette *offers*, not about what is *chosen* - which is a
better cut than the plan's, and one only visible after the tool work.

**Two of the five clusters are recommended against.**

- *`ScreenOperations`* would move **one** field, `_clipboard`, and would need `Document`, `Undo`,
  `_currentScreen` and `_selectedTile` to do it. Four collaborators to re-home one field is a worse
class
  than the one it came out of.
- *`TeleportCodeEditor`* has **no fields at all**. Its seven members read and write the document
directly.
  By this document's own measure - how many mutable fields a class owns - extracting it buys
nothing, and
  it would add an indirection to every call.

Both were listed from the field map, which cannot see that one of them has nothing to move and the
other would need half the session to move it.

### The pattern that made this cheap

`CanvasViewOptions` is handed the session's `Invalidate`, so changing a toggle still raises the one
`Invalidated` event the canvas already listens to rather than a second event for the same purpose.

`TilePaletteState` needed the sheet and the map, both of which change over a session's life. Rather than
hold a reference back to the session or take two `Func<>`s, it is *handed* them: `Rebind(sheet,
document)` is called from the two places either one changes, and `MapChanged()` from the one place
the map's contents change. The object has no view of the session at all.

### The cost, which was the predicted one

33 call sites re-pointed for the view options, and 13 XAML bindings across all three objects -
`Session.ShowGrid` becomes `Session.View.ShowGrid`. The compiler found every one of both kinds.

The editor has compiled bindings on - `AvaloniaUseCompiledBindingsByDefault` in the csproj and
`x:DataType` on the window - so a stale binding path is a build error rather than a silent failure at
run time:

```
MainWindow.axaml(466,34): Avalonia error AVLN2000: Unable to resolve property or method
of name 'TileFilter' on type 'Starquake.ScreenEditor.Editing.EditorSession'.
```

All 81 bindings in `MainWindow.axaml` are covered, both its `DataTemplate`s declare an `x:DataType`,
and the other two `.axaml` files hold no bindings at all. Nothing falls back to reflection, so
explicit `{CompiledBinding}` would buy nothing over the plain `{Binding}` the file already uses.

### Two ways to not notice an AVLN error, both met while doing this

Worth writing down, because both of them look like a clean build.

**Filtering the build output by error prefix.** `dotnet build | grep "error C"` catches `CS` and `IDE`
and silently drops `AVLN2000`. Read the summary line, or grep for `error` alone.

**Building twice.** The XAML compilation task is skipped as up to date on the second run, *even when
the first run failed*, so a repeated build on an unchanged broken file reports success:

```
build 1   (broken binding)   1 Error(s)   AVLN2000
build 2   (same file)        0 Error(s)   <- the task never re-ran
touch the .axaml             1 Error(s)   <- back again
```

So an `AVLN` result is only trustworthy on the first build after a change. `touch` the `.axaml`, or
rebuild, before believing a clean one.

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
