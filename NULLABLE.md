# Putting the game on the nullable standard

**Done.** `Starquake.Shared` has `<Nullable>enable</Nullable>` in its `.csproj`, the solution
builds at 0 warnings and 0 errors, and no file carries a `#nullable enable` directive any more.
Twenty three commits, `2568943` through `15eb567`.

Kept because the reasoning is worth having: the rule the conversion followed, what the warnings
turned out to be, the traps that cost time, the bugs it turned up, and the one question it
deliberately left open.

## The rule it followed

**No `null!`, and no `?` added just to silence a warning.** Both tell the compiler to stop
asking without answering the question. A `?` went on a member only where null genuinely means
something and callers check it. Where nothing checked, the member was made non-null instead.

The rule held. There is no `null!` anywhere in our own code, and the one place it was reached
for — casting the power up type in `StatusPanel` — became a check that throws instead.

## What the 246 warnings actually were

Almost none were a question about null. They were the same few shapes repeated:

**A member assigned by a second phase that ran immediately after the first.** The dominant case,
about ninety fields. `LoadContent` or `Initialise` was called on the line after the constructor,
so the half-built state the pattern exists to allow never lasted long enough for anyone to
observe. Folding each into the constructor made the field `readonly` and the warning went with
the method.

**A member nothing ever set.** `XnaFont` and `ImageFont` were whole types reachable only through
one such field. `ColourFaderAnimation.Name` had five `ColourAnimationEngine` overloads that
existed only to look animations up by it, so their loops could never match. Deleting beat
annotating, and it deleted the methods too.

**A lookup returning null for a name that was simply wrong.** `ResourceManager`'s three,
`ContentList.GetContent`, `ScreenEffectEngine.GetRenderViewPort`, `GameEngine.GetGameScreen`.
No caller checked — the 380 `GetSpriteCollection` calls dereference on the spot — so a wrong
name surfaced as a null reference inside a draw call rather than as the missing registration it
was. They throw now.

**A lookup returning null for an empty result.** `GameScreenCollection`'s four tile lookups and
`Blob.GetCollisionObjects` returned null rather than an empty list, so every caller asked twice:
once for null, once for the count. They return empty collections.

**A member that really was optional.** The minority, and they kept their `?`: the periodic sound
timer, a screen's teleport code, the tiles around Blob, every event with no obliged subscriber,
`StatusPanel`'s inventory before Blob has moved.

## The one place the pattern was structural

Six of `GameEngine`'s fields — `GameScreens`, `Player`, `ColourAnimationEngine`,
`BreakableBridgesEngine`, `ScreenEffectEngine` and the main game screen — cannot be
constructor-set by any reordering, because each needs resources that load on a background
worker. This is the case this plan expected to solve by moving the composition root and could
not.

Annotating them nullable was measured rather than argued about: **187 warnings across twenty
files**, because of several hundred readers only two ever check, and both of those are defensive
rather than meaningful. So each is a private nullable field behind one non-null property:

```csharp
private GameScreenCollection? _gameScreens;
public GameScreenCollection GameScreens => NotBeforeContentLoads(_gameScreens);
```

The `?` is on a field where null genuinely means "content is still loading" and it *is* checked,
once, by the property — which fails with the name of the thing asked for too early instead of a
null reference somewhere further on. Callers are unchanged and the public surface tells the
truth.

`DefineContent` and `Initialise` were a different problem wearing the same warning: both already
ran inside `GameHost.Initialize` immediately after the constructor, so the constructor calls them
itself and they declare what they populate with `[MemberNotNull]`, which the compiler checks.
`JumpPlatform.Reset` and `Nasty.CheckSpritesInUse` use the same attribute for the one field each
genuinely does re-assign per allocation.

## Verifying it

There are **no tests in this repository**. `TestScreen.cs` is a game screen, not a test project.

```bash
dotnet build Starquake.sln --no-incremental -v q --nologo   # must end: 0 Warning(s), 0 Error(s)
dotnet build Starquake.Mac/Starquake.Mac.csproj -v q --nologo        # refills Content, see below
timeout 12 dotnet run --project Starquake.Mac/Starquake.Mac.csproj --no-build
# exit code 124 means it ran the full 12s without throwing; any other code, read the output
```

**Use `--no-incremental` for any warning check.** An incremental build of an edited tree
reported `0 Warning(s)` while a clean build of the same tree reported three. Two `dotnet build`
invocations in one command line is enough to do it: the second is a no-op and prints a count
that belongs to nothing.

**The twelve second run only proves content loads.** It stops on the boot screen waiting for a
keypress, and everything the engine builds after that — every screen, Blob, the colour animation
and screen effect engines — is past it. To reach them, temporarily replace the keypress test at
the "Wait for a keypress before leaving the loading screen" comment with `if (true)`, run for
twenty seconds and check the log has no exceptions. Put it back before committing. Stages 4, 5
and 6 were all verified this way and it is the only reason their reordering can be trusted.

Past that the game sits on the options screen, and `GameScreens.Reset()` — which places every
pickup, powerup and access code — needs a keypress to start a game. Nothing automated drives
that, so **no gameplay is covered by any of this**. A question about the screen data is better
answered by a temporary probe in a constructor that runs at load time, which is how the access
booth count below was settled.

## Traps found the hard way

**A solution build will not refill the content folder.** An `--no-incremental` build deletes
`Starquake.Mac/bin/Debug/net9.0/Content/Graphics` and MGCB's cache then believes it is up to
date, so the next `--no-build` run dies on a missing `BootScreen2.xnb` and looks exactly like a
broken constructor. A full solution build succeeds, includes that project, and leaves the folder
empty anyway. Only building the head project on its own puts it back:

```bash
dotnet build Starquake.Mac/Starquake.Mac.csproj -v q --nologo
ls Starquake.Mac/bin/Debug/net9.0/Content/Graphics | wc -l   # 0 means this is what happened
```

This cost four confusing crashes across the conversion, three of them after it was written down.

**Do not run `jb cleanupcode`.** It produced 34 build errors: it strips `using MessagePack;`
from the `SharedLibs` serialisation types while leaving `[MessagePackObject]` and `[Key]` behind.
It also cannot do the conversions anyone would want it for — zero `for`→`foreach`, zero primary
constructors — it only reformats. `jb inspectcode` is fine and is what found this work.

**File encodings differ by project.** Our own sources are UTF-8 **with BOM** and LF endings; read
and write them as `utf-8-sig`. `ProjectMercury` sources are **Latin-1** and will raise
`UnicodeDecodeError` if opened as UTF-8. This is why `grep` treats some Mercury files as binary.

**`ProjectMercury` is out of scope.** A 2009 fork kept deliberately unmodernised, with
`<Nullable>disable</Nullable>` and an explanatory comment in both its `.csproj` files.

**`ScreenEditor/Program.cs` `BuildAvaloniaApp` must stay public, and so must `Program`.** Nothing
in the solution calls it except `Main`, so every inspection suggests making it private. The
Avalonia visual designer finds it by name at runtime, which is also why `Program` is the one
public type left in an otherwise internal editor. The same caution applies to anything reachable
only from `.axaml`.

**A name that is not registered now throws.** If a new sprite, sound, data resource or particle
effect is added, register it in `GameEngine.DefineContent` or the game fails loudly at startup,
which is the intent. That is what caught the two misspelt names below.

## Bugs it turned up

Five, all pre-existing, all fixed where they were found. Every one is the same shape: a member
nothing sets is usually a member nothing reads, but not always.

- **`CustomControl` dropped the `PlayerKeySet` its constructor was handed**, assigning only the
  engine reference. So the key set was null until the player redefined their keys on the options
  screen, while `MovedUp`, `MovedDown`, `MovedLeft`, `MovedRight` and `HasFired` all read
  straight off it with no guard.
- **`PickupsIlluminatedName` was spelt `"PickupsIlluminate"`**, missing its final d, and only the
  F8 graphics toggle ever corrected it — so `CoreElement`'s illuminated element draw could not
  work until F8 had been pressed twice.
- **`AccessCodesGreyName` asked for `Grey`** where the resource is registered as `Gray`, at its
  declaration and in both branches of that toggle. Nothing reads it yet, which is the only reason
  it went unnoticed.
- **The access booth `else if` in `GameScreenCollection.Reset` repeated the `if` above it**, so
  its `AccessBoothRight` lookup could never run and a right-only booth screen would never get a
  code. Counting tiles across all 512 screens says no screen is right-only — seven have both, one
  has only a left — so the branch was unreachable and fixing it changed no outcome.
- **`BlobDeathAnimation`'s `_drawBlobParts` guard could never be met**, so the death animation
  bought a sprite batch and drew nothing. Fixed in `725abeb`, just before this work started.

## The one question left open

`Blob.cs`, access booth detection, around line 1755. Each of the two branches detects the booth
on one side of Blob and then hands the booth screen the **map position of the tile on his other
side**:

```csharp
if (_tileLeftBlob is { TileNumber: (int)ActionTiles.AccessBoothRight })   // detects on the left
    …Reset(new object[] { Direction.Left, _tileRightBlob.MapXPosition });  // reports the right
```

The teleport booth immediately above it uses `_tileRightBlob` throughout, which is what makes the
crossing look like a slip. The position is not discarded: `AccessBoothScreen.Reset` uses it to
place Blob when he leaves the booth, as `(int)temp[1] * 128` give or take a tile, so reading the
wrong tile puts him in the wrong place.

Left exactly as it was, because which tile the booth is meant to be told is a question about the
game and walking Blob into a booth is the only way to see which looks right. What did change is
that both tiles must now be present for either branch to fire, since the branch reading the
opposite tile never established it was there — previously that was a crash waiting for an empty
tile beside Blob. If the crossing is a bug, the fix is to read the detected tile's own
`MapXPosition` in both branches.

## Also worth knowing

- **`TestScreen` never calls `LoadContent` on its own `BlobDeathAnimation`**, so its fragment
  list is empty where it used to be six particles with a null `Fireball` that would have thrown
  on the first trigger. Left alone: it is a debug screen and was not part of this work.
- **`Nasties.LoadContent` takes a `ContentList` it no longer reads**, since `Nasty` gets its own
  sprites now. Kept only because `MainGameScreen` passes one. That belongs with the unused
  members below.

## Still outstanding, deliberately not part of this

- Unused non-private members, down from 101 — several were removed here where a nullable question
  could not be answered without deciding whether anything read them.
- 75 primary-constructor suggestions. Declined: they would undo the `readonly` work in `e7af430`
  and break the `_camelCase` field convention.
- 32 invalid XML doc comments.
- Severities for the inspections that are already at zero are pinned in `.editorconfig`; if a
  cleanup re-introduces one, that file is where it is recorded.

## Commit index

| Stage | Commits |
| --- | --- |
| 1. Delete the dead | `2568943` |
| 2. Owner-assigned particles | `abd14e0` |
| 3. Leaf classes, 30 files | `e536e79`, `b9a95ed`, `0c7d494`, `842b626`, `270e7ab`, `dea3db8`, `3aa0916`, `0e8d08c`, `30e553e`, `d6ca5c6` |
| 4. Mid-size, 6 files | `7087eae`, `dc44a51`, `f25f308`, `1703992` |
| 5. `Blob.cs` | `2086031` |
| 6. `GameEngine.cs` and the last three | `8acad72` |
| 7. Flip the project | `15eb567` |
| Plan upkeep | `e7ffe4b`, `e36455c`, `2cee64d`, `46d0378`, `39fb903` |
