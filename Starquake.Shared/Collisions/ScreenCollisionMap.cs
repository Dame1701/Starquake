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
///     What Blob and the nasties can walk into on one screen, and how far they are allowed to
///     move before they do. Built from the screen's tiles and the collision data that ships beside
///     them, then asked, many times a frame.
/// </summary>
/// <remarks>
///     Collision is tested against a finer grid than the tiles are drawn on - see
///     <see cref="TileMetrics.CollisionCellWidth" /> - so a tile can be solid in part: the top of a
///     platform, one edge of a wall.
///     <para>
///         The map is in two halves. Everything read off the screen's tiles lands on a 32-pixel
///         grid by construction, so it is baked into <see cref="_staticLayer" /> - two planes of
///         cells, one that stops everything and one that stops only the nasties - and a question
///         about it is an array index rather than a search. Everything else - the items lying on
///         the screen, a platform Blob has laid, a door's force field, the core's elements - sits
///         at whatever pixel it was put at and cannot be gridded, so it stays a list. There are
///         only ever a handful.
///     </para>
///     <para>
///         Two planes rather than one kind per cell, because a cell can be both at once: a
///         teleport booth lays nasty-only cells over the whole of its tile while the tile's own
///         mask may already have made part of it solid. Collapsing those two into one would quietly
///         change what <see cref="HasNastyOnlyCellAt" /> answers, and with it where a nasty may be
///         born.
///     </para>
///     <para>
///         The bake is kept between rebuilds and reused when the screen's tiles have not changed,
///         which is nearly every rebuild - a pick-up taken or a bridge dissolving does not move a
///         tile. Whether they changed is decided by comparing the tiles themselves rather than by
///         trusting a caller to say so, because they do change under the game's feet: a breakable
///         bridge sets its tile to -1 and puts it back, and a shuttle pad swaps between two tile
///         numbers. A cache that depended on being told would be one missed call away from Blob
///         walking through a wall.
///     </para>
/// </remarks>
internal class ScreenCollisionMap
{
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
    ///     How many collision cells a whole screen is, which is what the baked planes are sized to.
    /// </summary>
    private const int CellsAcrossScreen = TileMetrics.PlayAreaWidth / TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="CellsAcrossScreen" />
    private const int CellsDownScreen = TileMetrics.PlayAreaHeight / TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     What a plane holds where it has no cell.
    /// </summary>
    private const int NoCell = -1;

    /// <summary>
    ///     The cells that are not read off the screen's tiles: the items lying on it, and whatever
    ///     the dynamic sources and <see cref="Add" /> have put there. Rebuilt in full every time,
    ///     which costs nothing at this length.
    /// </summary>
    private readonly List<CollisionTile> _dynamicCells = [];

    /// <summary>
    ///     The sources of collision cells that are not read off a screen's tiles, asked to hand their
    ///     cells over again at the end of every <see cref="Rebuild" />. See
    ///     <see cref="IDynamicCollisionCells" /> for why they cannot simply be put into the map once.
    /// </summary>
    private readonly List<IDynamicCollisionCells> _dynamicCellSources = [];

    private readonly GameScreenData[] _screens;

    private readonly StaticLayer _staticLayer = new();

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
    ///     Every cell on the map as it stands, the baked ones rebuilt into objects as they are
    ///     handed over. For tests and for reading the map in a debugger - the game asks the
    ///     questions below instead, none of which walks this.
    /// </summary>
    public IEnumerable<CollisionTile> Cells
    {
        get
        {
            for (int cellY = 0; cellY < CellsDownScreen; cellY++)
            {
                for (int cellX = 0; cellX < CellsAcrossScreen; cellX++)
                {
                    if (_staticLayer.StopsEveryone[cellX, cellY] != NoCell)
                    {
                        yield return new CollisionTile(
                            _staticLayer.StopsEveryone[cellX, cellY],
                            cellX * TileMetrics.CollisionCellWidth,
                            cellY * TileMetrics.CollisionCellHeight,
                            CollisionTileType.EveryoneHits);
                    }

                    if (_staticLayer.StopsNastiesOnly[cellX, cellY] != NoCell)
                    {
                        yield return CollisionTile.ForObject(
                            CollisionObjectType.GameObject,
                            cellX * TileMetrics.CollisionCellWidth,
                            cellY * TileMetrics.CollisionCellHeight,
                            CollisionTileType.NotBlob);
                    }
                }
            }

            foreach (CollisionTile cell in _dynamicCells)
            {
                yield return cell;
            }
        }
    }

    /// <summary>
    ///     The width and height of a whole screen in pixels - what a nasty is clamped inside.
    /// </summary>
    private static int ScreenWidth => TileMetrics.TileWidth * TileMetrics.TilesAcross;

    /// <inheritdoc cref="ScreenWidth" />
    private static int ScreenHeight => TileMetrics.TileHeight * TileMetrics.TilesDown;

    /// <summary>
    ///     Points the map at a screen and builds everything that is not baked. Called when the screen
    ///     changes and whenever something on it has moved, appeared or been picked up.
    /// </summary>
    public void Rebuild(int screenNumber)
    {
        ScreenNumber = screenNumber;

        _staticLayer.BakeIfStale(screenNumber, _screens[screenNumber], _tileCollisionData);

        _dynamicCells.Clear();
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
    ///     Adds a cell to the map that is not read off the screen's tiles - a jump platform Blob has
    ///     just laid, or the force field across a key-locked door. This is so the cell counts from this
    ///     frame on; keeping it across a rebuild is the job of the
    ///     <see cref="IDynamicCollisionCells" /> source that owns it.
    /// </summary>
    public void Add(CollisionTile cell)
        => _dynamicCells.Add(cell);

    /// <summary>
    ///     Takes a cell added by <see cref="Add" /> back out, for a platform that has dissolved. Does
    ///     nothing if a rebuild has already been through, which is why the source stops reporting a
    ///     cell rather than relying on this.
    /// </summary>
    public void Remove(CollisionTile cell)
        => _dynamicCells.Remove(cell);

    /// <summary>
    ///     The image of the first tile a box overlaps, or -1 if it overlaps none. A
    ///     <see cref="CollisionTileType.NotBlob" /> cell is there for the nasties to walk on, so Blob
    ///     passes through it and the search carries on looking for something he does hit.
    /// </summary>
    /// <remarks>
    ///     Which image comes back when the box covers several cells at once is not promised, and was
    ///     not before either - it was whichever the build happened to have added first. Every caller
    ///     asks only whether anything was hit.
    /// </remarks>
    public int CheckRectangles(
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        bool isBlob)
    {
        var box = new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight);

        foreach ((int cellX, int cellY) in CellsNear(playerXPos, playerYPos, playerWidth, playerHeight))
        {
            if (_staticLayer.StopsEveryone[cellX, cellY] != NoCell && OverlapsCell(box, cellX, cellY))
            {
                return _staticLayer.StopsEveryone[cellX, cellY];
            }

            if (!isBlob &&
                _staticLayer.StopsNastiesOnly[cellX, cellY] != NoCell &&
                OverlapsCell(box, cellX, cellY))
            {
                return (int)CollisionObjectType.GameObject;
            }
        }

        foreach (CollisionTile cell in _dynamicCells)
        {
            if (isBlob && cell.CollisionTileType == CollisionTileType.NotBlob)
            {
                continue;
            }

            if (CollisionDetection.Check(
                    box,
                    new Rectangle(cell.XPos, cell.YPos,
                        TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight)))
            {
                return cell.TileImage;
            }
        }

        return -1;
    }

    /// <summary>
    ///     How far the mover may travel in a direction before something stops it - its full velocity if
    ///     nothing does. Nasties are additionally held inside the screen; Blob is not, because
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

        foreach ((int cellX, int cellY) in SweptCells(
                     playerDirection, playerXPos, playerYPos, playerWidth, playerHeight, velocity))
        {
            int allowed = StaticStepAllowedBy(
                cellX, cellY, playerDirection,
                playerXPos, playerYPos, playerWidth, playerHeight, velocity, isBlob);

            if (allowed < checkMovementAmount)
            {
                checkMovementAmount = allowed;
            }

            // Nothing below can lower a nought, and most of a screen is nowhere near the mover.
            if (checkMovementAmount == 0)
            {
                break;
            }
        }

        if (checkMovementAmount > 0)
        {
            foreach (CollisionTile colTile in _dynamicCells)
            {
                int allowed = StepAllowedBy(
                    colTile, playerDirection,
                    playerXPos, playerYPos, playerWidth, playerHeight, velocity, isBlob);

                if (allowed < checkMovementAmount)
                {
                    checkMovementAmount = allowed;
                }

                if (checkMovementAmount == 0)
                {
                    break;
                }
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
    ///     than the cells of it that are solid - it is how Blob finds a tunnel mouth, a lift shaft
    ///     or a booth to step into.
    /// </summary>
    /// <remarks>
    ///     A probe can cover more than one tile, and which of them is reported is the whole
    ///     question. It used to be whichever came first in a scan along the top row and down, so a
    ///     tile up and to the left of the one Blob was walking into answered in its place. Going
    ///     left that is the tile he has just left, and it hid the tunnel mouth he was standing in.
    ///     <para>
    ///         The rule now is: among the tiles the probe reaches, prefer one the mover is actually
    ///         standing in, and otherwise keep the old scan order exactly. That is deliberately the
    ///         smallest rule that fixes it, and it is additive - measured over every position on
    ///         every shipped screen, no tunnel, booth or teleporter that used to be reachable stops
    ///         being reachable, and what is gained is an eight-pixel band at eighteen places where
    ///         entry was silently impossible. <c>CheckTileByObjectTests</c> holds that measurement.
    ///     </para>
    ///     <para>
    ///         Markers are skipped as well as empty tiles. A marker is a position the author noted
    ///         rather than scenery - there is no artwork for one and <see cref="Rebuild" /> has
    ///         always passed over them - but this did not, so a marker dropped beside a tunnel
    ///         mouth would have answered in its place and quietly stopped the tunnel working.
    ///     </para>
    /// </remarks>
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

        var mover = new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight);

        TileDetected? best = null;
        int bestRank = -1;

        foreach ((int x, int y) in TilesOverlapping(probe))
        {
            int tileImage = TileImageAt(x, y);

            if (tileImage <= -1 || MarkerTiles.Contains(tileImage))
            {
                continue;
            }

            int rank = OverlapWith(mover, x, y) > 0 ? 1 : 0;

            if (rank <= bestRank)
            {
                continue;
            }

            bestRank = rank;

            int xpos = playerXPos - x * TileMetrics.TileWidth;
            int ypos = (measureFromFoot ? playerYPos + playerHeight : playerYPos) - y * TileMetrics.TileHeight;

            best = new TileDetected(tileImage, xpos, ypos, x, y);
        }

        return best;
    }

    /// <summary>
    ///     Whether a cell sitting exactly here is one the nasties collide with and Blob walks
    ///     through. Asked of every cell of a tile before a nasty is born on it.
    /// </summary>
    public bool HasNastyOnlyCellAt(int cellX, int cellY)
    {
        if (cellX % TileMetrics.CollisionCellWidth == 0 &&
            cellY % TileMetrics.CollisionCellHeight == 0 &&
            IsOnTheGrid(cellX / TileMetrics.CollisionCellWidth, cellY / TileMetrics.CollisionCellHeight) &&
            _staticLayer.StopsNastiesOnly[
                cellX / TileMetrics.CollisionCellWidth, cellY / TileMetrics.CollisionCellHeight] != NoCell)
        {
            return true;
        }

        foreach (CollisionTile cell in _dynamicCells)
        {
            if (cell.XPos == cellX && cell.YPos == cellY &&
                cell.CollisionTileType == CollisionTileType.NotBlob)
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    ///     How much of a tile the mover itself is standing in, which is what decides between tiles
    ///     when the probe reaches more than one.
    /// </summary>
    /// <remarks>
    ///     Two wrong answers were tried first, and both are worth writing down because each looks
    ///     right until it is measured against the shipped map.
    ///     <list type="bullet">
    ///         <item>
    ///             The nearest tile along the direction of travel. A probe is stretched the way it
    ///             is going, so a tile it has reached by one pixel is the nearest one, and Blob
    ///             stepped into a tunnel sixty-three pixels before he touched its mouth.
    ///         </item>
    ///         <item>
    ///             The tile the probe covers most of. The probe is a different shape in each
    ///             direction - stretched rightwards, shifted leftwards, a thin strip downwards - so
    ///             ranking by it moved the answer the wrong way going right, and lost the
    ///             teleporter, the right-hand tunnels and one side of the access booths.
    ///         </item>
    ///     </list>
    ///     Ranking by the mover's own box is what both were reaching for: among the tiles the probe
    ///     can see, the one Blob is most inside. The probe still decides which tiles are candidates,
    ///     so each direction keeps its own reach; it no longer decides which of them wins.
    ///     <para>
    ///         Plain edge arithmetic rather than the centre-distance test, because this ranks
    ///         candidates rather than deciding whether they are candidates at all;
    ///         <see cref="TilesOverlapping" /> has already settled that with the game's own test. A
    ///         probe that touches nothing the mover is in - the downward strip below his feet -
    ///         leaves every candidate on nought, and the first in scan order wins as it always did.
    ///     </para>
    /// </remarks>
    private static int OverlapWith(Rectangle mover, int tileX, int tileY)
    {
        int left = tileX * TileMetrics.TileWidth;
        int top = tileY * TileMetrics.TileHeight;

        int across = Math.Min(mover.X + mover.Width, left + TileMetrics.TileWidth) - Math.Max(mover.X, left);
        int down = Math.Min(mover.Y + mover.Height, top + TileMetrics.TileHeight) - Math.Max(mover.Y, top);

        return Math.Max(0, across) * Math.Max(0, down);
    }

    private static bool IsOnTheGrid(int cellX, int cellY)
        => cellX >= 0 && cellX < CellsAcrossScreen && cellY >= 0 && cellY < CellsDownScreen;

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
    ///     Every tile of the screen a box overlaps. Indexed rather than scanned: the tiles are a
    ///     regular grid, so which ones a box can touch follows from its edges.
    /// </summary>
    /// <remarks>
    ///     The bounds are drawn a tile wider than the box on every side because overlap here is the
    ///     game's centre-distance test rather than an edge test, and that reaches further than the
    ///     box does for a box narrower than a tile. Candidates are still tested exactly, so the
    ///     margin only costs a few comparisons.
    /// </remarks>
    private static IEnumerable<(int X, int Y)> TilesOverlapping(Rectangle probe)
    {
        int firstX = Math.Max(0, probe.X / TileMetrics.TileWidth - 1);
        int lastX = Math.Min(TileMetrics.TilesAcross - 1,
            (probe.X + probe.Width) / TileMetrics.TileWidth + 1);
        int firstY = Math.Max(0, probe.Y / TileMetrics.TileHeight - 1);
        int lastY = Math.Min(TileMetrics.TilesDown - 1,
            (probe.Y + probe.Height) / TileMetrics.TileHeight + 1);

        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                var tileRect = new Rectangle(
                    x * TileMetrics.TileWidth,
                    y * TileMetrics.TileHeight,
                    TileMetrics.TileWidth,
                    TileMetrics.TileHeight);

                if (CollisionDetection.Check(probe, tileRect))
                {
                    yield return (x, y);
                }
            }
        }
    }

    private static bool OverlapsCell(Rectangle box, int cellX, int cellY)
        => CollisionDetection.Check(
            box,
            new Rectangle(
                cellX * TileMetrics.CollisionCellWidth,
                cellY * TileMetrics.CollisionCellHeight,
                TileMetrics.CollisionCellWidth,
                TileMetrics.CollisionCellHeight));

    /// <summary>
    ///     An item sitting on a screen occupies two cells each way, and nothing but Blob collides
    ///     with one.
    /// </summary>
    private static void AddObjectCollisionTiles(List<CollisionTile> collisionMap, InteractiveItem item)
    {
        for (int y = 0; y < ItemCellsAcross; y++)
        {
            for (int x = 0; x < ItemCellsAcross; x++)
            {
                collisionMap.Add(
                    CollisionTile.ForObject(
                        CollisionObjectType.GameObject,
                        item.XLocation + x * ItemCellSize,
                        item.YLocation + y * ItemCellSize,
                        CollisionTileType.NotBlob));
            }
        }
    }

    /// <summary>
    ///     How far one cell of the map allows the mover to travel, for a cell belonging to a game
    ///     object rather than to scenery. A nasty that has ended up embedded in one is released
    ///     rather than trapped.
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

        if (!isBlob &&
            colTile.ObjectType is CollisionObjectType.JumpPlatform or
                CollisionObjectType.CoreElement or
                CollisionObjectType.GameObject &&
            IsEmbeddedIn(colTile.XPos, colTile.YPos, playerXPos, playerYPos, playerWidth, playerHeight))
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
    ///     Whether a mover is already inside a game object's cell, which is the case a nasty is let
    ///     out of rather than held in.
    /// </summary>
    private static bool IsEmbeddedIn(
        int cellXPos, int cellYPos, int playerXPos, int playerYPos, int playerWidth, int playerHeight)
        => CollisionDetection.Check(
            new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
            new Rectangle(cellXPos, cellYPos, GameObjectProbeSize, GameObjectProbeSize));

    /// <summary>
    ///     Stops a mover at the edge of the screen. Only nasties are clamped: Blob walking off an edge
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

    /// <summary>
    ///     The baked cells a box could possibly touch where it stands. Bounded by the box's edges
    ///     with a cell of margin either side, for the same reason <see cref="TilesOverlapping" />
    ///     carries one.
    /// </summary>
    private static IEnumerable<(int X, int Y)> CellsNear(int xPos, int yPos, int width, int height)
        => CellsWithin(xPos, yPos, xPos + width, yPos + height);

    /// <summary>
    ///     The baked cells the mover could touch at any point of a move - the box it stands in,
    ///     stretched the way it is going by as far as it is going.
    /// </summary>
    private static IEnumerable<(int X, int Y)> SweptCells(
        Direction direction, int xPos, int yPos, int width, int height, int velocity)
    {
        int reach = Math.Max(0, velocity);

        int left = xPos - (direction == Direction.Left ? reach : 0);
        int top = yPos - (direction == Direction.Up ? reach : 0);
        int right = xPos + width + (direction == Direction.Right ? reach : 0);
        int bottom = yPos + height + (direction == Direction.Down ? reach : 0);

        return CellsWithin(left, top, right, bottom);
    }

    private static IEnumerable<(int X, int Y)> CellsWithin(int left, int top, int right, int bottom)
    {
        int firstX = Math.Max(0, left / TileMetrics.CollisionCellWidth - 1);
        int lastX = Math.Min(CellsAcrossScreen - 1, right / TileMetrics.CollisionCellWidth + 1);
        int firstY = Math.Max(0, top / TileMetrics.CollisionCellHeight - 1);
        int lastY = Math.Min(CellsDownScreen - 1, bottom / TileMetrics.CollisionCellHeight + 1);

        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                yield return (x, y);
            }
        }
    }

    private int TileImageAt(int x, int y)
        => _screens[ScreenNumber].ScreenTiles[x, y].TileImage;

    /// <summary>
    ///     How far one baked cell allows the mover to travel. The nasty-only plane holds the cells
    ///     the six interior, spike and tunnel-mouth rules lay down, which are game-object cells and
    ///     so release a nasty that has ended up inside one.
    /// </summary>
    private int StaticStepAllowedBy(
        int cellX,
        int cellY,
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity,
        bool isBlob)
    {
        int cellXPos = cellX * TileMetrics.CollisionCellWidth;
        int cellYPos = cellY * TileMetrics.CollisionCellHeight;

        int allowed = velocity;

        if (_staticLayer.StopsEveryone[cellX, cellY] != NoCell)
        {
            allowed = CollisionDetection.CheckDirection(
                playerDirection, velocity,
                playerXPos, playerYPos, playerWidth, playerHeight,
                cellXPos, cellYPos,
                TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight);
        }

        if (allowed == 0 ||
            isBlob ||
            _staticLayer.StopsNastiesOnly[cellX, cellY] == NoCell ||
            IsEmbeddedIn(cellXPos, cellYPos, playerXPos, playerYPos, playerWidth, playerHeight))
        {
            return allowed;
        }

        return Math.Min(
            allowed,
            CollisionDetection.CheckDirection(
                playerDirection, velocity,
                playerXPos, playerYPos, playerWidth, playerHeight,
                cellXPos, cellYPos,
                TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight));
    }

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
                _dynamicCells.Add(cell);
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
            AddObjectCollisionTiles(_dynamicCells, powerUp);
        }

        foreach (InteractiveItem pickUp in _screens[ScreenNumber].PickUps)
        {
            AddObjectCollisionTiles(_dynamicCells, pickUp);
        }

        InteractiveItem? cheopsPyramid = _screens[ScreenNumber].CheopsPyramid;
        if (cheopsPyramid != null)
        {
            AddObjectCollisionTiles(_dynamicCells, cheopsPyramid);
        }
    }

    /// <summary>
    ///     The cells read off a screen's tiles, as two planes of a 32-pixel grid, kept between
    ///     rebuilds and rebuilt when the tiles beneath them change.
    /// </summary>
    private sealed class StaticLayer
    {
        /// <summary>
        ///     The tile image behind each cell that stops everything, or <see cref="NoCell" />.
        /// </summary>
        public int[,] StopsEveryone { get; } = new int[CellsAcrossScreen, CellsDownScreen];

        /// <summary>
        ///     The cells that stop the nasties and let Blob through - a spike, a booth's inside, a
        ///     tunnel mouth. Held apart from <see cref="StopsEveryone" /> because one cell can be
        ///     both.
        /// </summary>
        public int[,] StopsNastiesOnly { get; } = new int[CellsAcrossScreen, CellsDownScreen];

        /// <summary>
        ///     The screen the bake is of, and the tile images it was baked from. The images are kept
        ///     so that staleness is decided by looking rather than by being told - see the class
        ///     remarks on <see cref="ScreenCollisionMap" />.
        /// </summary>
        private int _bakedScreen = -1;

        /// <inheritdoc cref="_bakedScreen" />
        private readonly int[,] _bakedFrom = new int[TileMetrics.TilesAcross, TileMetrics.TilesDown];

        public void BakeIfStale(int screenNumber, GameScreenData screen, int[,,] tileCollisionData)
        {
            if (screenNumber == _bakedScreen && NothingHasMoved(screen))
            {
                return;
            }

            _bakedScreen = screenNumber;

            for (int y = 0; y < CellsDownScreen; y++)
            {
                for (int x = 0; x < CellsAcrossScreen; x++)
                {
                    StopsEveryone[x, y] = NoCell;
                    StopsNastiesOnly[x, y] = NoCell;
                }
            }

            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    int tileImage = screen.ScreenTiles[x, y].TileImage;
                    _bakedFrom[x, y] = tileImage;

                    // A marker is a position the author noted, not a tile: it has no artwork and no
                    // collision mask, and its number is past the end of the mask data entirely.
                    if (tileImage <= -1 || MarkerTiles.Contains(tileImage))
                    {
                        continue;
                    }

                    BakeTileCells(x, y, tileImage, tileCollisionData);
                    BakeInteriorCells(x, y, tileImage);
                    BakeCentralSpikeCells(x, y, tileImage);
                    BakeWallSpikeCells(x, y, tileImage);
                    BakeSecretPassageCells(x, y, tileImage);
                }
            }
        }

        private bool NothingHasMoved(GameScreenData screen)
        {
            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    if (_bakedFrom[x, y] != screen.ScreenTiles[x, y].TileImage)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     The cells of one tile that everything collides with, read from the collision data that
        ///     ships alongside the screens.
        /// </summary>
        private void BakeTileCells(int x, int y, int tileImage, int[,,] tileCollisionData)
        {
            for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
            {
                for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
                {
                    if (tileCollisionData[tileImage, cellAcross, cellDown] != 1)
                    {
                        continue;
                    }

                    (int cellX, int cellY) = CellAt(x, y, cellAcross, cellDown);
                    StopsEveryone[cellX, cellY] = tileImage;
                }
            }
        }

        /// <summary>
        ///     Teleporters, access booths and tunnel lifts. Blob walks into these on purpose, so their
        ///     cells stop everything except him.
        /// </summary>
        private void BakeInteriorCells(int x, int y, int tileImage)
        {
            if ((ActionTiles)tileImage is not (ActionTiles.TeleportBooth or
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

            // Every cell of the tile, not just the part the artwork fills: nothing may be born on a
            // lift shaft at all, so there is no need to be exact about its shape.
            for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
            {
                for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
                {
                    MarkNastyOnly(x, y, cellAcross, cellDown);
                }
            }
        }

        /// <summary>
        ///     Central spikes and broken bottles. Only the four middle cells, and only against nasties -
        ///     Blob is meant to be able to walk onto them and die.
        /// </summary>
        private void BakeCentralSpikeCells(int x, int y, int tileImage)
        {
            if ((ActionTiles)tileImage is not (ActionTiles.Spike2 or ActionTiles.Spike3))
            {
                return;
            }

            for (int cellDown = 1; cellDown <= 2; cellDown++)
            {
                for (int cellAcross = 1; cellAcross <= 2; cellAcross++)
                {
                    MarkNastyOnly(x, y, cellAcross, cellDown);
                }
            }
        }

        /// <summary>
        ///     Spikes mounted on a right-hand wall, so their cells run down the tile rather than across
        ///     its middle.
        /// </summary>
        private void BakeWallSpikeCells(int x, int y, int tileImage)
        {
            if ((ActionTiles)tileImage != ActionTiles.Spike1)
            {
                return;
            }

            for (int cellDown = 0; cellDown <= 1; cellDown++)
            {
                for (int cellAcross = 2; cellAcross <= 3; cellAcross++)
                {
                    MarkNastyOnly(x, y, cellAcross, cellDown);
                }
            }
        }

        /// <summary>
        ///     A tunnel mouth, which is a column of cells against one side of its tile - the left side
        ///     for an even-numbered mouth, the right for an odd one.
        /// </summary>
        private void BakeSecretPassageCells(int x, int y, int tileImage)
        {
            if (tileImage < TunnelTiles.FirstMouth ||
                tileImage >= TunnelTiles.FirstMouth + TunnelTiles.MouthPairs)
            {
                return;
            }

            int cellAcross = tileImage % 2 == 0
                ? 0
                : RightTunnelMouthInset / TileMetrics.CollisionCellWidth;

            for (int cellDown = 0; cellDown < TunnelMouthCellsDown; cellDown++)
            {
                MarkNastyOnly(x, y, cellAcross, cellDown);
            }
        }

        private void MarkNastyOnly(int tileX, int tileY, int cellAcross, int cellDown)
        {
            (int cellX, int cellY) = CellAt(tileX, tileY, cellAcross, cellDown);
            StopsNastiesOnly[cellX, cellY] = (int)CollisionObjectType.GameObject;
        }

        private static (int X, int Y) CellAt(int tileX, int tileY, int cellAcross, int cellDown)
            => ((tileX * TileMetrics.TileWidth) / TileMetrics.CollisionCellWidth + cellAcross,
                (tileY * TileMetrics.TileHeight) / TileMetrics.CollisionCellHeight + cellDown);
    }
}
