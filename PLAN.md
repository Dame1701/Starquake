# What is left

This began as a full review of the game against `ef5a6a3`, laid out as seven phases. **Phases 0 to
5 and most of phase 6 have landed**, and their sections have been removed now that they are
history rather than plan - the commits carry the reasoning, and the decisions that outlived the
work are written into the code and `BUILD.md`. What is below is what remains.

## What was done, in one table

| Phase | What it was | Where it ended up |
| --- | --- | --- |
| 0 | Defects: three off-by-one bounds tests, an unclamped core-element draw, a clock-seeded world generator, three orphaned doc comments | `105a07c` `712bedb` `a0744c7` `63554d9` |
| 0.2 | The joystick option that bricked the controls | A real joypad, read alongside the keyboard - `ca765b6` |
| 1 | The build never checked the standard `.editorconfig` declared. 35 violations | `b10a13c` `b9f071c` `d73c6fe`, and CI on three platforms |
| 2 | Subtraction: `IGameScreen.Success`, `SpriteCollection.Clone`, dead initialisers, `packages/`, `Temp/` → `Assets/`, four carried-over follow-ups | `036b437` … `d31561b` |
| 3 | Time, randomness and threads: `CollisionDetection`'s statics, the sound timer, 19 `DateTime.Now` calls, the last global state | `eaaa1f0` `69b6d4a` `8b8ad66` `41173d8` |
| 4 | The god classes. `GameScreenCollection` 1,938 → 550, `Nasty` 1,646 → 819, `GameEngine` 1,227 → 961, `StatusPanel` 547 → 255 | `057bdc1` … `90e7910` |
| 5 | Performance: tile batching, 46 batches sorting by a constant, repeated sheet lookups | `4260cb1` `393b360` `fe10a3a` |
| 6 | The `object? param` channel, the lookup table, three records, six handler names, `Dispose`, the build fix | `314a9d4` `ea37940` `07433fc` |

**65 tests to 159.** That was phase 4's own measure of whether the splits earned anything.

## Still open

### 6.1 — place the key and the access card

**The game is only winnable because of two temporary lines**, `Blob.cs:407-409`, which hand the
player both items at startup. The world generator draws pick-up types with `Next(28)`, one short
of `AccessCard` (28) and `Key` (29), so neither has ever appeared on the map. Deleting those two
lines without replacing them makes the game unwinnable.

Nothing blocks this technically: 4.3 extracted a seeded, tested `WorldPopulator` for it to go in,
and the `WorldPopulatorTests` pin the current behaviour, including a test asserting that neither
item is ever scattered. **What is open is the placement algorithm**, which is being worked out
separately. The section below is what the map data actually constrains.

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

### Phase 5's changes have not been seen running

All three performance items change *drawing*, the build is clean and the tests pass, but nobody
has looked at the screen since the renderer changed. Script-driven input reaches the options
screen and no further - `System Events` can hold a named key like space long enough for the 60 Hz
poll to catch it, but not a digit, and the options screen needs `0`.

**What to eyeball:** the tiles, the upper-layer patches over lift shafts and booths, the tunnel
mouths, Olly on screen 477, and a nasty you have shot - the smoking one, whose three same-position
draws had an unspecified colour on top until `393b360` made it deterministic.

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

## The key and the access card

The two lines in `Blob`'s constructor under the comment "Temporary test code to give Blob Access
card and Key" are the only `TODO`-class comment in the game, and the obvious reading — a
debugging leftover to delete — is wrong. **They are a workaround for placement that was never
finished. Deleting them without replacing them makes the game unwinnable.**

### Why the items never spawn

Pick-up types are drawn with `Next(PickUpTypeCount)` in both branches of
`WorldPopulator.PlacePowerUpAndPickUp`, and that count is 28, so it yields 0 to 27.
`Collectables.AccessCard` is **28** and `Collectables.Key` is **29**, so the range stops exactly
one short of both.

The rest of the scaffolding is already there.
`WorldPopulator.CollectScreensWithCheopsAndCoreElements` *explicitly excludes* pick-ups of type
`AccessCard` or `Key` from the core-element list — a guard written in anticipation of placement
that was then never written.

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
   index 0 and be silently overwritten by a core element. The bounds clamp landed in `712bedb`;
   the type check has not.
4. **Place both items explicitly** in `WorldPopulator`: pick one marked screen for each, rather
   than widening the `PickUpTypeCount` draw and rolling for them. Guaranteeing exactly one of
   each is better than leaving it to chance anyway.
5. **Then delete the two lines in `Blob`'s constructor**, and add a test: for any seed, the world
   contains exactly one key and one access card, and both are on marked screens. That test is
   possible because the generator is seeded, and `WorldPopulatorTests` already has the fixtures
   for it - including the test that currently asserts neither item is ever placed, which is the
   one this work inverts.

**If you would rather compute it.** The honest version needs a per-screen edge model — for each
screen, which of its four edges the blob can leave by, derived from the collision map rather than
the tile images. With that, reachability is an ordinary graph search, and the same model would
also answer whether the core is reachable at all in a given generated world, which nothing checks
today. That is a week or two rather than a day. `ScreenCollisionMap` now exists and is testable
without content, which is the piece such a model would be built on.

## Still deliberately out of scope

**Split `MainGameScreen`.** It uses every actor because it *is* the main game — it drives the
blob, the nasties, the bridges, the lifts, the animations and the effects every frame. Its
alternatives are an eighteen-parameter constructor or a bag object existing solely for it, and
both are worse than holding the engine. If it should be smaller, that is splitting a 642-line
class on size, which is a different task and should not be smuggled in here.

**Introduce a DI container**, for the reasons `DECOUPLING.md` already gives.

**Touch ProjectMercury**, beyond the `.editorconfig` section that excludes it from the analysers
the rest of the solution is held to. It has eight findings of its own and is upstream code.

**Touch `BlobDeathAnimation.FragmentSpriteNumbers`, `SharedLibs`, or the `WINDOWS` and
content-build wiring.** All three are settled decisions with the reasoning written down —
`REFACTORING.md` for the first two, `BUILD.md` for the third.

**Add tests as a goal.** Tests were the measure for phase 4, not the deliverable. A split that
adds no testable surface has earned nothing - which is why three of `Blob`'s four named clusters
were left alone.

