# What is left

This began as a full review of the game against `ef5a6a3`, laid out as seven phases. **Phases 0 to 6
have landed bar the hi-scores**, and their sections have been removed now that they are history
rather than plan - the commits carry the reasoning, and the decisions that outlived the work are
written into the code and `BUILD.md`. What is below is what remains.

The key and the access card were phase 6.1 and are done; the section on them is kept rather than
removed, because the rule they are placed by is not obvious from the code and one recommendation in
it was overturned for a reason worth not relearning.

## What was done, in one table

| Phase | What it was | Where it ended up |
| --- | --- | --- |
| 0 | Defects: three off-by-one bounds tests, an unclamped core-element draw, a clock-seeded world generator, three orphaned doc comments | `105a07c` `712bedb` `a0744c7` `63554d9` |
| 0.2 | The joystick option that bricked the controls | A real joypad, read alongside the keyboard - `ca765b6` |
| 1 | The build never checked the standard `.editorconfig` declared. 35 violations | `b10a13c` `b9f071c` `d73c6fe`, and CI on three platforms |
| 2 | Subtraction: `IGameScreen.Success`, `SpriteCollection.Clone`, dead initialisers, `packages/`, `Temp/` → `Assets/`, four carried-over follow-ups | `036b437` … `d31561b` |
| 3 | Time, randomness and threads: `CollisionDetection`'s statics, the sound timer, 19 `DateTime.Now` calls, the last global state | `eaaa1f0` `69b6d4a` `8b8ad66` `41173d8` |
| 4 | The god classes. `GameScreenCollection` 1,938 → 550, `Nasty` 1,646 → 819, `GameEngine` 1,227 → 961 (985 now, phase 6 added back), `StatusPanel` 547 → 255 | `057bdc1` … `90e7910` |
| 5 | Performance: tile batching, 46 batches sorting by a constant, repeated sheet lookups | `4260cb1` `393b360` `fe10a3a` |
| 6 | The `object? param` channel, the lookup table, three records, six handler names, `Dispose`, the build fix | `314a9d4` `ea37940` `07433fc` |
| 6.1 | The key and the access card, placed by computed reachability rather than by hand - see below | *uncommitted as this was written* |
| — | The landing site and the markers out of the tile sheet: `MarkerTile` at 500+, `LandingSite`, and the shipped map migrated onto both | *uncommitted as this was written* |
| — | Three drawing defects older than the repository: the background's alpha, the force field drawn on every screen, and the emitter names a deep copy threw away | *uncommitted as this was written* |

**65 tests to 211.** That was phase 4's own measure of whether the splits earned anything; 159 of
them were its own, and the rest came with the work below.

## Still open

### 6.2 — persist hi-scores

*Deferred by decision, not difficulty.* The original game never remembered scores either, so this
is a new feature rather than a defect. `HiScores` hardcodes eight names in its constructor,
`AddScoreToTable` writes only to memory, and there is no read or write to disk anywhere in the
solution. The name-entry screen is already fully built, so the work is a file format and two
calls.

### `Blob` is the one god class still standing

2,055 lines and 123 fields. Phase 4 took the two parts of it that are arithmetic rather than
drawing - the unstick probe and the inventory - and stopped, because the rest is one state machine
that draws itself. The three clusters the phase-4 plan named and did not take - the weapon, the
tunnel traversal and the death sequence - are each particle effects, sounds and drawing wrapped
around two or three counters, so moving them would add no testable surface. Splitting `Blob`
properly needs its update logic and its draw code prised apart first, which is a different task.

### Phase 5's changes have mostly still not been seen running

All three performance items change *drawing*, the build is clean and the tests pass. Script-driven
input reaches the options screen and no further - `System Events` can hold a named key like space
long enough for the 60 Hz poll to catch it, but not a digit, and the options screen needs `0`. So
this is still eyeballing, and still mostly undone.

It is worth saying what eyeballing has already been worth. Playing the game for the key and the
card turned up three drawing defects in one sitting, none of which any test would have caught and
all of which predate the repository: the planet background at an alpha of zero, the key-door force
field drawn on all 512 screens, and a particle lookup that threw the moment a force field was on
screen. None of them are phase 5's doing - they were simply never reached before.

**What to eyeball:** the tiles, the upper-layer patches over lift shafts and booths, the tunnel
mouths, Olly on screen 477, and a nasty you have shot - the smoking one, whose three same-position
draws had an unspecified colour on top until `393b360` made it deterministic. The nasty matters
twice over now: its splat colours are set by emitter name, and that name changed.

### 6.6 — the build fix is good, not perfect

`07433fc` took a rebuild-then-build cycle from leaving the game unable to start **11 times out of
12 to once out of twelve**, and that once now repairs itself on the next build rather than
persisting. The residual is a Rebuild-time hiccup rather than the edit-build loop, which measured
clean ten times out of ten.

Two theories were wrong and are recorded so nobody retries them. `DisableIncrementalClean` makes
it worse. And MonoGame's `ExtraContent` glob is **not** coming back empty - a `-v diag` log shows
it populated with all 102 files for both platforms, which is what sent the investigation to the
copy step and found the real cause.

### Carried over from `REFACTORING.md`

Four of its eleven follow-ups are still open and are listed there: the point-blank push-through
applying only to horizontal shots, `Blob`'s field count, the guards that sit one level deeper than
they need to, and `Scorpian` and `Helicopter` sharing splatter colours. Three methods also still
have a local called `temp`.

## The key and the access card — how they ended up placed

**Done.** One key and one access card are placed on every world, each on a random screen the blob
can reach without passing through the gate that item itself opens. The two lines in `Blob`'s
constructor that handed the player both at startup are gone.

### Why the items never spawned

Pick-up types are drawn with `Next(PickUpTypeCount)` in both branches of
`WorldPopulator.PlacePowerUpAndPickUp`, and that count is 28, so it yields 0 to 27.
`Collectables.AccessCard` is **28** and `Collectables.Key` is **29**, so the range stopped exactly
one short of both. That draw is unchanged - widening it would have left the count of each item to
chance - and the two are placed deliberately instead, which is what guarantees exactly one of each.

The scaffolding was already there, including a guard in
`WorldPopulator.CollectScreensWithCheopsAndCoreElements` excluding pick-ups of either type from the
core-element list, written in anticipation of placement that was then never written. So was the
artwork: `Pickups.png` is 30 frames, and 28 and 29 are the last two.

### What the rule is

A location is invalid if **every** route to it from the landing site passes a gate the item opens.
Stated the other way about - and this is the form worth implementing - the screen is one that
cannot be reached once the gates are shut. So a single flood from the landing site with the gates
sealed answers it for all 512 screens at once, rather than tracing a route per candidate. A
key-locked door is an `AccessForceField` tile (419); an access booth is `AccessBoothLeft` or
`AccessBoothRight` (399, 400). Each gate is shut only for the item that opens it, so a door does
not constrain the card.

### Why it is computed, and not marked by hand

**This section used to recommend the opposite** - add a `KeySafeLocation` marker tile and mark the
safe screens in ScreenEditor, on the grounds that three gated screens is a small constraint and a
hand-authored answer is verifiable by eye in a way a solver's output is not. That was overturned,
for a reason the recommendation had not weighed: **the map is edited.** A marked set of safe screens
is correct only for the map it was marked against, and goes stale the first time somebody moves a
door in the editor. A computed one adjusts to whatever world has been made.

The old section was also right about something, and it is worth keeping: a naive model is worse than
none. A flood over the screen *grid* treating gated screens as walls strands nothing - removing 3
screens, or even 11, cannot disconnect a 16 by 32 grid - so it reports every screen as safe while
appearing to enforce the rule. Two things make the honest version tractable, and neither is
geometric adjacency:

- **The planet is a ribbon horizontally.** `Blob.ChangeScreenOffRightEdge` adds one to the screen
  number whatever column it was in, so the end of a row leads to the start of the next. Vertically
  it is a grid, a row at a time. Neither is what a 2D bitmap of the planet would do.
- **A door is not a sealed screen.** `AddKeyLockDoorCollisionTiles` puts three collision cells at
  `Location.X + 48` - one cell wide, the height of its tile. It blocks a path *through* a screen,
  and it only blocks it at all because the blob is two cells wide.

### What the model does, and what it deliberately does not

`GateFreeScreens` floods the blob's 2x2 collision-cell footprint out from the landing site, using
the blob's own rules for leaving a screen, with the gate of one kind sealed.

**It is deliberately an under-estimate.** It moves the blob by walking, falling and climbing only,
and knows nothing of tunnel mouths, tunnel lifts, shuttles or teleporters. Every one of those can
only ever *add* reachability, so a screen it returns is one the blob genuinely reaches without
meeting a gate, and the ones it misses are candidates given up. That asymmetry is the whole design:
over-estimating puts the key behind a door and makes the game unwinnable; under-estimating costs
nothing but variety. On the shipped map it leaves 188 screens, 185 of them with somewhere to stand -
a generous pool. It is also why the planet core is not in the result and does not need to be.
Climbing is treated as free because it nearly is: walking off the top of a screen hands the blob a
fresh jump platform.

### Two things that had to be got right

- **Placement runs last.** The scatter empties `PickUps` on every screen it thins out, and
  `MatchSomeCoreElementsToTheCore` writes over the first pick-up on a screen. A key placed any
  earlier is thinned away or overwritten. This was the `PickUps[0]` hazard the old plan named; it is
  avoided by ordering rather than by a guard.
- **Four existing tests had to change.** They counted every pick-up as a scattered one, which the
  two deliberate items break. They now filter through `TestData.IsScattered`.

### What still is not guaranteed

The items make the game *reliably* winnable, not provably so. Winning needs all nine core elements
replaced (`PlanetCoreScreen.CoreElementsToReplace`), while only two to five of the types the core
wants are placed deliberately; the rest comes from trading at a Cheops pyramid, which offers an
element the core actually wants. Nothing checks that a required element is reachable at all, as
opposed to merely not gated - that is the same per-screen model again, pointed at a different
question.

And there is a failure mode now live that was harmless before. `CheopsScreen.ChooseItemToSwap`: a
blob carrying nothing but the key and the card has to give up the key, or the card if it has no key.
A door is passable only while the key is *in the inventory*, so trading it away re-locks every door,
and the player can no longer start a game holding one. Whether a pyramid should refuse both items is
a design question, not a defect, and nothing has been changed about it.

## Still deliberately out of scope

**Split `MainGameScreen`.** It uses every actor because it *is* the main game — it drives the
blob, the nasties, the bridges, the lifts, the animations and the effects every frame. Its
alternatives are an eighteen-parameter constructor or a bag object existing solely for it, and
both are worse than holding the engine. If it should be smaller, that is splitting a 642-line
class on size, which is a different task and should not be smuggled in here.

**Introduce a DI container**, for the reasons `DECOUPLING.md` already gives.

**Touch ProjectMercury**, beyond the `.editorconfig` section that excludes it from the analysers
the rest of the solution is held to. It has eight findings of its own and is upstream code.

*One exception has been taken.* `Emitter.CopyBaseFields` renamed every emitter to "Copy of"
whatever it had been called, and since each effect instance is a deep copy, nothing could be found
by the name its content file gives it - which crashed the game on the first screen with a key-locked
door. The alternative was a second workaround in the game's own layer, on top of the one in
`NastyHelpers` that had been hiding the bug. It is not NuGet code - it is a project in this
solution and already carries local changes, `Renderer.RenderEmitter`'s `screenFade` among them.

**Touch `BlobDeathAnimation.FragmentSpriteNumbers`, `SharedLibs`, or the `WINDOWS` and
content-build wiring.** All three are settled decisions with the reasoning written down —
`REFACTORING.md` for the first two, `BUILD.md` for the third.

**Add tests as a goal.** Tests were the measure for phase 4, not the deliverable. A split that
adds no testable surface has earned nothing - which is why three of `Blob`'s four named clusters
were left alone.

