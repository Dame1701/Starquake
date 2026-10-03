// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.Collisions;

/// <summary>
///     What the blob and the nasties can walk into on one screen, and how far they are allowed to
///     move before they do. Built from the screen's tiles and the collision data that ships beside
///     them, then asked, many times a frame.
/// </summary>
/// <remarks>
///     Collision is tested against a finer grid than the tiles are drawn on - see
///     <see cref="TileMetrics.CollisionCellWidth" /> - so a tile can be solid in part: the top of a
///     platform, one edge of a wall. Building the map is turning one screen's eight-by-six grid of
///     tiles into the list of cells that are solid, plus the cells belonging to whatever items are
///     lying on it.
///     <para>
///         This was 400 lines of <c>GameScreenCollection</c>, which is the thing most likely to
///         break when level data changes and was the hardest part of the game to reach from a test,
///         because reaching it meant constructing a screen collection, which loads content. It needs
///         no content: the screen records and the tile collision data are both plain arrays, and the
///         tile size is a constant rather than something to ask a sprite sheet for - every tile sheet
///         in the game is registered at <see cref="TileMetrics.TileWidth" /> by
///         <see cref="TileMetrics.TileHeight" />, so asking was the same answer the long way round.
///     </para>
/// </remarks>
internal class ScreenCollisionMap
{
    /// <summary>
    ///     Collision tiles carrying an image number beyond this are game objects rather than scenery -
    ///     a nasty, a pickup, a core element - and are tested against a sprite-sized box.
    /// </summary>
    private const int FirstGameObjectImage = 999;

    /// <summary>
    ///     The box a game object is tested against when checking whether a nasty has ended up embedded
    ///     in one. Half a sprite, not a whole one, and not a collision cell either - it is its own
    ///     number and was written inline as a bare 32.
    /// </summary>
    private const int GameObjectProbeSize = 32;

    /// <summary>
    ///     How many cells across and down an item lying on a screen occupies, and how big each is.
    ///     An item is drawn at a sprite's size, which is two collision cells each way.
    /// </summary>
    private const int ItemCellsAcross = 2;

    /// <inheritdoc cref="ItemCellsAcross" />
    private const int ItemCellSize = 32;

    /// <summary>
    ///     How far down a tunnel mouth's cells run, and how far in from the tile's left edge a
    ///     right-hand mouth's do. A mouth is three cells tall and sits against one side of its tile.
    /// </summary>
    private const int TunnelMouthCellsDown = 3;

    /// <inheritdoc cref="TunnelMouthCellsDown" />
    private const int RightTunnelMouthInset = 96;

    /// <summary>
    ///     The sources of collision cells that are not read off a screen's tiles, asked to hand their
    ///     cells over again at the end of every <see cref="Rebuild" />. See
    ///     <see cref="IDynamicCollisionCells" /> for why they cannot simply be put into the map once.
    /// </summary>
    private readonly List<IDynamicCollisionCells> _dynamicCellSources = [];

    private readonly GameScreenData[] _screens;

    /// <summary>
    ///     Which cells of each tile image are solid, indexed by tile image, then cell across, then cell
    ///     down. Ships alongside the screen data and is authored in ScreenEditor.
    /// </summary>
    private readonly int[,,] _tileCollisionData;

    public ScreenCollisionMap(GameScreenData[] screens, int[,,] tileCollisionData)
    {
        _screens = screens;
        _tileCollisionData = tileCollisionData;
    }

    /// <summary>
    ///     The screen this map currently describes. Set by <see cref="Rebuild" />, which is the only
    ///     way the map changes which screen it is the map of.
    /// </summary>
    public int ScreenNumber { get; private set; }

    /// <summary>
    ///     The width and height of a whole screen in pixels - what a nasty is clamped inside.
    /// </summary>
    private static int ScreenWidth => TileMetrics.TileWidth * TileMetrics.TilesAcross;

    /// <inheritdoc cref="ScreenWidth" />
    private static int ScreenHeight => TileMetrics.TileHeight * TileMetrics.TilesDown;

    private List<CollisionTile> Cells => _screens[ScreenNumber].CollisionMap;

    /// <summary>
    ///     Throws away the map for a screen and builds it again, from its tiles and then from the items
    ///     lying on it. Called when the screen changes and whenever something on it has moved, appeared
    ///     or been picked up.
    /// </summary>
    public void Rebuild(int screenNumber)
    {
        ScreenNumber = screenNumber;
        _screens[screenNumber].CollisionMap = new List<CollisionTile>();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                // A marker is a position the author noted, not a tile: it has no artwork and no
                // collision mask, and its number is past the end of the mask data entirely.
                if (TileImageAt(x, y) <= -1 || MarkerTiles.Contains(TileImageAt(x, y)))
                {
                    continue;
                }

                AddTileCollisionCells(x, y);
                AddInteriorCollisionCells(x, y);
                AddCentralSpikeCollisionCells(x, y);
                AddWallSpikeCollisionCells(x, y);
                AddSecretPassageCollisionCells(x, y);
            }
        }

        AddScreenObjectCollisionTiles();
        AddDynamicCollisionCells();
    }

    /// <summary>
    ///     Registers something that owns cells of its own - see <see cref="IDynamicCollisionCells" />.
    ///     A source stays registered for the life of the game and is asked about every screen.
    /// </summary>
    public void RegisterDynamicCells(IDynamicCollisionCells source)
        => _dynamicCellSources.Add(source);

    /// <summary>
    ///     Adds a cell to the map that is not read off the screen's tiles - a jump platform the blob has
    ///     just laid, or the force field across a key-locked door. This is so the cell counts from this
    ///     frame on; keeping it across a rebuild is the job of the
    ///     <see cref="IDynamicCollisionCells" /> source that owns it.
    /// </summary>
    public void Add(CollisionTile cell)
        => Cells.Add(cell);

    /// <summary>
    ///     Takes a cell added by <see cref="Add" /> back out, for a platform that has dissolved. Does
    ///     nothing if a rebuild has already been through, which is why the source stops reporting a
    ///     cell rather than relying on this.
    /// </summary>
    public void Remove(CollisionTile cell)
        => Cells.Remove(cell);

    /// <summary>
    ///     The image of the first tile a box overlaps, or -1 if it overlaps none. A
    ///     <see cref="CollisionTileType.NotBlob" /> cell is there for the nasties to walk on, so the
    ///     blob passes through it and the search carries on looking for something it does hit.
    /// </summary>
    public int CheckRectangles(
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        bool isBlob)
    {
        foreach (CollisionTile collisionTile in Cells)
        {
            bool collide =
                CollisionDetection.Check(
                    new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                    new Rectangle(collisionTile.XPos,
                        collisionTile.YPos, TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight));

            if (collide && !(isBlob && collisionTile.CollisionTileType == CollisionTileType.NotBlob))
            {
                return collisionTile.TileImage;
            }
        }

        return -1;
    }

    /// <summary>
    ///     How far the mover may travel in a direction before something stops it - its full velocity if
    ///     nothing does. Nasties are additionally held inside the screen; the blob is not, because
    ///     walking off an edge is how he changes screens.
    /// </summary>
    public int CheckCollision(
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity,
        bool isBlob)
    {
        int checkMovementAmount = velocity;

        foreach (CollisionTile colTile in Cells)
        {
            int checkMovementAmountTemp = StepAllowedBy(
                colTile,
                playerDirection,
                playerXPos,
                playerYPos,
                playerWidth,
                playerHeight,
                velocity,
                isBlob);

            if (checkMovementAmountTemp <= checkMovementAmount)
            {
                checkMovementAmount = checkMovementAmountTemp;
            }
        }

        if (!isBlob)
        {
            checkMovementAmount = ClampToScreenBounds(
                playerDirection,
                playerXPos,
                playerYPos,
                playerWidth,
                playerHeight,
                checkMovementAmount);
        }

        return checkMovementAmount;
    }

    /// <summary>
    ///     The tile an object is about to move into, and where within that tile the object meets it.
    ///     Unlike <see cref="CheckCollision" /> this is about the tile the level author placed rather
    ///     than the cells of it that are solid - it is how the blob finds a tunnel mouth, a lift shaft
    ///     or a booth to step into.
    /// </summary>
    public TileDetected? CheckTileByObject(
        Direction directionToCheck,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity)
    {
        if (ProbeRectangleFor(directionToCheck, playerXPos, playerYPos, playerWidth, playerHeight, velocity)
            is not { } probe)
        {
            return null;
        }

        // A check along the vertical axis reports where the object's foot sits inside the tile; every
        // other check reports where its top does.
        bool measureFromFoot = directionToCheck is Direction.Up or Direction.Down;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (TileImageAt(x, y) <= -1)
                {
                    continue;
                }

                var tileRect = new Rectangle(
                    x * TileMetrics.TileWidth,
                    y * TileMetrics.TileHeight,
                    TileMetrics.TileWidth,
                    TileMetrics.TileHeight);

                if (!CollisionDetection.Check(probe, tileRect))
                {
                    continue;
                }

                int xpos = playerXPos - x * TileMetrics.TileWidth;
                int ypos = (measureFromFoot ? playerYPos + playerHeight : playerYPos) - y * TileMetrics.TileHeight;

                return new TileDetected(TileImageAt(x, y), xpos, ypos, x, y);
            }
        }

        return null;
    }

    /// <summary>
    ///     The box to look for a tile in, given which way the object is about to move. Null for a
    ///     direction that is not checked, which is every diagonal.
    /// </summary>
    /// <remarks>
    ///     The four straight directions each probe a different shape, which is the only real difference
    ///     between the five arms this replaced: up and left shift the whole box, down probes a thin
    ///     strip below the object's feet, and right stretches the box rather than shifting it.
    /// </remarks>
    private static Rectangle? ProbeRectangleFor(
        Direction directionToCheck,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity)
        => directionToCheck switch
        {
            Direction.Up => new Rectangle(playerXPos, playerYPos - velocity, playerWidth, playerHeight),
            Direction.Down => new Rectangle(playerXPos, playerYPos + playerHeight, playerWidth, velocity),
            Direction.Left => new Rectangle(playerXPos - velocity, playerYPos, playerWidth, playerHeight),
            Direction.Right => new Rectangle(playerXPos, playerYPos, playerWidth + velocity, playerHeight),
            Direction.None => new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
            _ => null
        };

    /// <summary>
    ///     An item sitting on a screen occupies two cells each way, and nothing but blob collides
    ///     with one.
    /// </summary>
    private static void AddObjectCollisionTiles(List<CollisionTile> collisionMap, InteractiveItem item)
    {
        for (int y = 0; y < ItemCellsAcross; y++)
        {
            for (int x = 0; x < ItemCellsAcross; x++)
            {
                collisionMap.Add(
                    new CollisionTile(
                        (int)CollisionObjectType.GameObject, // Random image that's meaningless
                        item.XLocation + x * ItemCellSize,
                        item.YLocation + y * ItemCellSize,
                        CollisionTileType.NotBlob));
            }
        }
    }

    /// <summary>
    ///     Whether a tile's top surface is solid enough to stand something on - the two middle cells
    ///     of its top row. The world generator asks before it puts a pick-up somewhere.
    /// </summary>
    public bool CanStandOnTopOf(int tileImage)
        => _tileCollisionData[tileImage, 1, 0] == 1 && _tileCollisionData[tileImage, 2, 0] == 1;

    /// <summary>
    ///     Whether one cell of one tile image blocks movement. A tile number the collision data does
    ///     not cover is passable: a marker is numbered past the end of it, and so is anything painted
    ///     from a sheet larger than the data was built for.
    /// </summary>
    public bool IsSolid(int tileImage, int cellAcross, int cellDown)
        => tileImage >= 0 &&
           tileImage < _tileCollisionData.GetLength(0) &&
           _tileCollisionData[tileImage, cellAcross, cellDown] == 1;

    private int TileImageAt(int x, int y)
        => _screens[ScreenNumber].ScreenTiles[x, y].TileImage;

    /// <summary>
    ///     The cells of one tile that everything collides with, read from the collision data that ships
    ///     alongside the screens.
    /// </summary>
    private void AddTileCollisionCells(int x, int y)
    {
        for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
        {
            for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
            {
                if (_tileCollisionData[TileImageAt(x, y), cellAcross, cellDown] != 1)
                {
                    continue;
                }

                Cells.Add(
                    new CollisionTile(
                        TileImageAt(x, y),
                        x * TileMetrics.TileWidth + cellAcross * TileMetrics.CollisionCellWidth,
                        y * TileMetrics.TileHeight + cellDown * TileMetrics.CollisionCellHeight,
                        CollisionTileType.EveryoneHits));
            }
        }
    }

    /// <summary>
    ///     Teleporters, access booths and tunnel lifts. Blob walks into these on purpose, so their cells
    ///     stop everything except him.
    /// </summary>
    private void AddInteriorCollisionCells(int x, int y)
    {
        if ((ActionTiles)TileImageAt(x, y) is not (ActionTiles.TeleportBooth or
            ActionTiles.AccessBoothLeft or
            ActionTiles.AccessBoothRight or
            ActionTiles.TunnelLiftTop1 or
            ActionTiles.TunnelLiftTop2 or
            ActionTiles.TunnelLiftTop3 or
            ActionTiles.TunnelLiftShaft1 or
            ActionTiles.TunnelLiftShaft2 or
            ActionTiles.TunnelLiftShaft3))
        {
            return;
        }

        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        // Cover the whole tile since it doesn't matter - nasties can't touch any of it anyway
        for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
        {
            for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
            {
                AddNastyOnlyCell(
                    cellAcross * TileMetrics.CollisionCellWidth + tileX,
                    cellDown * TileMetrics.CollisionCellHeight + tileY);
            }
        }
    }

    /// <summary>
    ///     Central spikes and broken bottles. Only the four middle cells, and only against nasties - blob
    ///     is meant to be able to walk onto them and die.
    /// </summary>
    private void AddCentralSpikeCollisionCells(int x, int y)
    {
        if ((ActionTiles)TileImageAt(x, y) is not (ActionTiles.Spike2 or ActionTiles.Spike3))
        {
            return;
        }

        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        for (int cellDown = 1; cellDown <= 2; cellDown++)
        {
            for (int cellAcross = 1; cellAcross <= 2; cellAcross++)
            {
                AddNastyOnlyCell(
                    cellAcross * TileMetrics.CollisionCellWidth + tileX,
                    cellDown * TileMetrics.CollisionCellHeight + tileY);
            }
        }
    }

    /// <summary>
    ///     Spikes mounted on a right-hand wall, so their cells run down the tile rather than across its
    ///     middle.
    /// </summary>
    private void AddWallSpikeCollisionCells(int x, int y)
    {
        if ((ActionTiles)TileImageAt(x, y) != ActionTiles.Spike1)
        {
            return;
        }

        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        for (int cellDown = 0; cellDown <= 1; cellDown++)
        {
            for (int cellAcross = 2; cellAcross <= 3; cellAcross++)
            {
                AddNastyOnlyCell(
                    cellAcross * TileMetrics.CollisionCellWidth + tileX,
                    cellDown * TileMetrics.CollisionCellHeight + tileY);
            }
        }
    }

    /// <summary>
    ///     A tunnel mouth, which is a column of cells against one side of its tile - the left side for
    ///     an even-numbered mouth, the right for an odd one.
    /// </summary>
    private void AddSecretPassageCollisionCells(int x, int y)
    {
        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        for (int mouth = 0; mouth < TunnelTiles.MouthPairs; mouth++)
        {
            int tunnelTile = mouth + TunnelTiles.FirstMouth;

            if (TileImageAt(x, y) != tunnelTile)
            {
                continue;
            }

            int columnX = tunnelTile % 2 == 0 ? tileX : tileX + RightTunnelMouthInset;

            for (int cellDown = 0; cellDown < TunnelMouthCellsDown; cellDown++)
            {
                AddNastyOnlyCell(columnX, cellDown * TileMetrics.CollisionCellHeight + tileY);
            }
        }
    }

    /// <summary>
    ///     A cell the nasties collide with and the blob walks through. Its image number is meaningless -
    ///     nothing reads it back - which is what the six callers' repeated comment said.
    /// </summary>
    private void AddNastyOnlyCell(int xPos, int yPos)
        => Cells.Add(
            new CollisionTile(
                (int)CollisionObjectType.GameObject,
                xPos,
                yPos,
                CollisionTileType.NotBlob));

    /// <summary>
    ///     The cells of everything that keeps its own - the jump platforms, the key-locked doors, the
    ///     core's elements. Last, so that a source can see nothing of the rebuild it is part of.
    /// </summary>
    private void AddDynamicCollisionCells()
    {
        foreach (IDynamicCollisionCells source in _dynamicCellSources)
        {
            foreach (CollisionTile cell in source.CellsOnScreen(ScreenNumber))
            {
                Cells.Add(cell);
            }
        }
    }

    /// <summary>
    ///     The power-up, the pick-ups and the pyramid lying on this screen.
    /// </summary>
    private void AddScreenObjectCollisionTiles()
    {
        InteractiveItem? powerUp = _screens[ScreenNumber].PowerUp;
        if (powerUp != null)
        {
            AddObjectCollisionTiles(Cells, powerUp);
        }

        foreach (InteractiveItem pickUp in _screens[ScreenNumber].PickUps)
        {
            AddObjectCollisionTiles(Cells, pickUp);
        }

        InteractiveItem? cheopsPyramid = _screens[ScreenNumber].CheopsPyramid;
        if (cheopsPyramid != null)
        {
            AddObjectCollisionTiles(Cells, cheopsPyramid);
        }
    }

    /// <summary>
    ///     How far one collision tile allows the mover to travel. Two kinds of tile let it pass
    ///     regardless: those marked <see cref="CollisionTileType.NotBlob" /> when the mover is blob,
    ///     and a game object a nasty has ended up embedded inside, which is released rather than
    ///     trapped.
    /// </summary>
    private static int StepAllowedBy(
        CollisionTile colTile,
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity,
        bool isBlob)
    {
        if (colTile.CollisionTileType == CollisionTileType.NotBlob && isBlob)
        {
            return velocity;
        }

        if (colTile.TileImage > FirstGameObjectImage &&
            !isBlob &&
            (CollisionObjectType)colTile.TileImage is CollisionObjectType.JumpPlatform or
            CollisionObjectType.CoreElement or
            CollisionObjectType.GameObject &&
            CollisionDetection.Check(
                new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                new Rectangle(colTile.XPos, colTile.YPos, GameObjectProbeSize, GameObjectProbeSize)))
        {
            return velocity;
        }

        return CollisionDetection.CheckDirection(
            playerDirection,
            velocity,
            playerXPos,
            playerYPos,
            playerWidth,
            playerHeight,
            colTile.XPos,
            colTile.YPos,
            TileMetrics.CollisionCellWidth,
            TileMetrics.CollisionCellHeight);
    }

    /// <summary>
    ///     Stops a mover at the edge of the screen. Only nasties are clamped: blob walking off an edge
    ///     is how he changes screens.
    /// </summary>
    /// <remarks>
    ///     This was four arms, one per direction, which were two shapes written twice. Up and left run
    ///     into nought; down and right run into the screen's own width or height less the mover's size.
    /// </remarks>
    private static int ClampToScreenBounds(
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int checkMovementAmount)
    {
        (int position, int size, int limit) = playerDirection switch
        {
            Direction.Up or Direction.Down => (playerYPos, playerHeight, ScreenHeight),
            Direction.Left or Direction.Right => (playerXPos, playerWidth, ScreenWidth),
            _ => (0, 0, 0)
        };

        if (playerDirection is Direction.Up or Direction.Left)
        {
            int overshoot = position - checkMovementAmount;

            return overshoot < 0 ? checkMovementAmount + overshoot : checkMovementAmount;
        }

        if (playerDirection is Direction.Down or Direction.Right)
        {
            int overshoot = position + size + checkMovementAmount - limit;

            return overshoot > 0 ? checkMovementAmount - overshoot : checkMovementAmount;
        }

        return checkMovementAmount;
    }
}
