// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.Tuning;

namespace Starquake.World;

/// <summary>
///     The key-locked doors on the screen being shown: where they are, whether each lets Blob
///     through, and the force field that says so. Found afresh on every arrival at a screen,
///     stepped once a logic frame, and gone again when Blob leaves.
/// </summary>
/// <remarks>
///     This was the planet's fourth job. <c>GameScreenCollection</c> is the map - the screens, the
///     landing site, the core, what is where - and it also held the one list of mutable door state
///     in the codebase, the five methods that stepped it, and the
///     <see cref="IDynamicCollisionCells" /> implementation that existed solely to hand those
///     doors' cells to the collision map. That last point is what settles it: the planet's whole
///     reason for implementing that interface was these doors, so taking them out takes the
///     interface off the planet with them.
///     <para>
///         It is the third thing to own cells this way, after <c>JumpPlatforms</c> and
///         <c>PlanetCoreScreen</c>, and it registers itself the same way they do.
///     </para>
/// </remarks>
internal sealed class KeyLockDoors : IDynamicCollisionCells
{
    /// <summary>
    ///     How loudly a key-locked door says it has given way. The booth's own success tone, which
    ///     is what the planet already uses for access granted - quietly, because a door may be
    ///     opened again every time Blob walks back onto its screen.
    /// </summary>
    private const float DoorGivingWayVolume = 0.25f;

    private readonly ScreenCollisionMap _collisions;
    private readonly GraphicsSet _graphicsSet;
    private readonly ScreenRenderer _renderer;
    private readonly ResourceManager _resources;
    private readonly GameScreenData[] _screens;

    /// <summary>
    ///     The doors on the screen being shown, or null when they have not been looked for yet.
    ///     Null rather than empty is the difference between "no doors here" and "not asked yet".
    /// </summary>
    private List<KeyLockDoor>? _doors;

    /// <summary>
    ///     Which screen <see cref="_doors" /> was found on, so <see cref="CellsOnScreen" /> can
    ///     answer nothing for any other.
    /// </summary>
    private int _screenNumber;

    public KeyLockDoors(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        GameScreenData[] screens,
        ScreenCollisionMap collisions,
        ScreenRenderer renderer)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _screens = screens;
        _collisions = collisions;
        _renderer = renderer;
    }

    /// <summary>
    ///     The cells of the doors found on the screen being shown. A door only hunts for its own
    ///     cells when it has none - see <see cref="StepDoor" /> - so without this a rebuild left the
    ///     door holding cells that were no longer in the map, and its force field let Blob walk
    ///     through for the rest of the visit.
    /// </summary>
    public IEnumerable<CollisionTile> CellsOnScreen(int screenNumber)
    {
        if (screenNumber != _screenNumber || _doors == null)
        {
            yield break;
        }

        foreach (KeyLockDoor keyLockDoor in _doors)
        {
            foreach (CollisionTile collisionTile in keyLockDoor.CollisionTiles)
            {
                yield return collisionTile;
            }
        }
    }

    /// <summary>
    ///     Throws away the doors found for the screen just left, so the next frame looks for the new
    ///     screen's. Driven by the screen change rather than by noticing the number has moved: a
    ///     door is meant to be found on each arrival, which is what gives the moment of one opening
    ///     in front of Blob rather than having been green when the screen appeared.
    /// </summary>
    public void ForgetScreen() => _doors = null;

    /// <summary>
    ///     Steps every door on the screen, and keeps the renderer's force field in step with whether
    ///     there are any.
    /// </summary>
    public void Update(GameTime gameTime, int screenNumber, bool blobHasKey, int blobXPos, int blobYPos)
    {
        _screenNumber = screenNumber;
        _doors ??= DoorsOn(screenNumber);
        _renderer.KeyLockDoors = _doors;

        // Counted rather than tested for null. DoorsOn returns an empty list for a screen with no
        // doors on it, never null, so asking whether it is null said yes on all 512 screens - which
        // left the force field running and being drawn everywhere once any door had started it.
        if (_doors.Count == 0)
        {
            // Nothing on this screen is emitting, so neither should anything left over from the last
            // screen that was. Clearing rather than stopping, because stopping would leave the
            // particles already in the air hanging there unaged.
            _renderer.ClearForceField();

            return;
        }

        _renderer.StartForceField();

        foreach (KeyLockDoor keyLockDoor in _doors)
        {
            StepDoor(keyLockDoor, gameTime, blobHasKey, blobXPos, blobYPos);
        }
    }

    private List<KeyLockDoor> DoorsOn(int screenNumber)
    {
        var found = new List<KeyLockDoor>();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (_screens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.AccessForceField)
                {
                    continue;
                }

                found.Add(new KeyLockDoor
                {
                    Location = new Point(
                        x * _resources
                            .GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteWidth,
                        y * _resources
                            .GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteHeight)
                });
            }
        }

        return found;
    }

    /// <summary>
    ///     One door: whether it lets Blob through, and the force field that says so. Passable to him
    ///     if he is carrying the key, solid if not - and solid to nasties either way, which is why
    ///     the tiles flip between <see cref="CollisionTileType.NotBlob" /> and
    ///     <see cref="CollisionTileType.EveryoneHits" /> rather than being removed.
    /// </summary>
    private void StepDoor(
        KeyLockDoor keyLockDoor, GameTime gameTime, bool blobHasKey, int blobXPos, int blobYPos)
    {
        if (keyLockDoor.CollisionTiles.Count == 0)
        {
            AddCollisionTiles(keyLockDoor);
        }

        foreach (CollisionTile collisionTile in keyLockDoor.CollisionTiles)
        {
            collisionTile.CollisionTileType = blobHasKey
                ? CollisionTileType.NotBlob
                : CollisionTileType.EveryoneHits;
        }

        StepTheForceField(keyLockDoor, blobHasKey, blobXPos, blobYPos);

        _renderer.AnimateForceField(keyLockDoor, gameTime);
    }

    /// <summary>
    ///     Moves the door's own state on a frame: how far open it is drawn, what is left of the
    ///     burst at the moment it gave way, and the clock the field's bands and scanner run off.
    /// </summary>
    /// <remarks>
    ///     The opening is light and nothing else - the cells above have already flipped on the key
    ///     alone, so a door caught halfway through opening is passable. What it buys is the moment:
    ///     a door that is shut when Blob walks onto the screen and opens in front of him says he
    ///     has the key for it far better than one that was already green when the screen appeared.
    ///     <para>
    ///         A door is found afresh every time its screen is shown - see
    ///         <see cref="ForgetScreen" /> - so that moment is had on each arrival rather than once
    ///         in the game.
    ///     </para>
    /// </remarks>
    private void StepTheForceField(KeyLockDoor keyLockDoor, bool blobHasKey, int blobXPos, int blobYPos)
    {
        keyLockDoor.Frame++;
        keyLockDoor.Brace = ForceFieldSettings.BraceAgainst(keyLockDoor.Doorway, blobXPos, blobYPos);

        if (blobHasKey && !keyLockDoor.WasOpen)
        {
            keyLockDoor.Flash = ForceFieldSettings.NewFlash();

            _resources.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.AccessSuccess, DoorGivingWayVolume, false);
        }

        keyLockDoor.WasOpen = blobHasKey;
        keyLockDoor.Openness = ForceFieldSettings.OpennessAfter(keyLockDoor.Openness, blobHasKey);
        keyLockDoor.Flash = ForceFieldSettings.FlashAfter(keyLockDoor.Flash);
    }

    private void AddCollisionTiles(KeyLockDoor keyLockDoor)
    {
        Rectangle doorway = keyLockDoor.Doorway;

        for (int c = 0; c < TileMetrics.CollisionCellsDownTile; c++)
        {
            CollisionTile keyLockCollisionTile =
                CollisionTile.ForObject(
                    CollisionObjectType.KeyAccessDoor,
                    doorway.Left,
                    doorway.Top + c * TileMetrics.CollisionCellHeight,
                    CollisionTileType.None);

            // The door keeps its own tiles so it can take them out again, and the collision map is
            // what anything walking into the door actually tests against.
            keyLockDoor.CollisionTiles.Add(keyLockCollisionTile);
            _collisions.Add(keyLockCollisionTile);
        }
    }
}
