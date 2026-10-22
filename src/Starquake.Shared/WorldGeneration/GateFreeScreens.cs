// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.WorldGeneration;

/// <summary>
///     The thing that can stand between Blob and a screen: a door the key opens, or an access
///     booth the card gets through.
/// </summary>
internal enum Gate
{

    /// <summary>
    ///     A key-locked door. Each <see cref="ActionTiles.AccessForceField" /> tile is one, solid to
    ///     Blob until he carries the key. See <c>KeyLockDoors</c>.
    /// </summary>
    KeyDoor,

    /// <summary>
    ///     An access booth. Getting through means satisfying its code, and the access card satisfies
    ///     any digit. See <c>AccessCodeLock</c>.
    /// </summary>
    AccessBooth
}

/// <summary>
///     Which screens Blob can reach from where his ship came down without going through a gate.
/// </summary>
/// <remarks>
///     This is what stops the key being put somewhere that needs the key, and the access card
///     somewhere that needs the card. "Every route to this screen passes a gate" is the same statement
///     as "this screen cannot be reached once the gates are shut", so one flood from the landing site
///     with the gates sealed answers it for all 512 screens at once.
///     <para>
///         <b>The model is deliberately an under-estimate.</b> It moves Blob by walking, falling and
///         climbing only, and knows nothing of tunnel mouths, tunnel lifts, shuttles or teleporters.
///         Each of those can only ever <i>add</i> reachability, so a screen this returns is one Blob
///         genuinely reaches without meeting a gate, and the ones it misses are candidates given up.
///         That asymmetry is the point: over-estimating puts the key behind a door and makes the game
///         unwinnable, where under-estimating costs only variety, and the shipped map still leaves
///         some 185 screens to choose from. It is also why the planet core need not appear in the
///         result.
///     </para>
///     <para>
///         Climbing is treated as free because it very nearly is: walking off the top of a screen
///         hands Blob a fresh jump platform to stand on (<c>Blob.ChangeScreenOffTopEdge</c>), so
///         height is not what limits it.
///     </para>
/// </remarks>
internal static class GateFreeScreens
{

    /// <summary>
    ///     The collision cells across and down one screen. Movement is worked out on this grid rather
    ///     than the tiles, because a door is one cell wide where a tile is four.
    /// </summary>
    private const int CellsAcross = TileMetrics.TilesAcross * TileMetrics.CollisionCellsAcrossTile;

    /// <inheritdoc cref="CellsAcross" />
    private const int CellsDown = TileMetrics.TilesDown * TileMetrics.CollisionCellsDownTile;

    /// <summary>
    ///     How many cells of the grid Blob fills, each way. Two each way, so a one-cell gap is not a
    ///     way through, which is exactly what a door is.
    /// </summary>
    private const int BlobCellsAcross = SpriteMetrics.SpriteWidth / TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="BlobCellsAcross" />
    private const int BlobCellsDown = SpriteMetrics.SpriteHeight / TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     How far into its tile a key-locked door's barrier stands, in cells. The door adds its
    ///     collision partway through the tile rather than across the whole of it, so sealing the tile
    ///     would wall off more than the door does.
    /// </summary>
    private const int DoorBarrierCell = 48 / TileMetrics.CollisionCellWidth;

    /// <summary>
    ///     How far the search looks for somewhere Blob fits, if the landing site itself is inside
    ///     scenery. A badly marked start should still seed the flood rather than report the planet
    ///     empty.
    /// </summary>
    private const int StartSearchRadius = 2;

    /// <summary>
    ///     Every screen reachable from the landing site without passing through a gate of the given
    ///     kind.
    /// </summary>
    public static HashSet<int> Find(
        GameScreenData[] screens,
        ScreenCollisionMap collisions,
        Gate gate)
    {
        LandingSite site = LandingSite.FindIn(screens);
        bool[] blocked = BuildBlockedCells(screens, collisions, gate);

        var reached = new HashSet<int>();

        if (FindFoothold(blocked, screens.Length, site) is not { } start)
        {
            return reached;
        }

        bool[] visited = new bool[blocked.Length];
        visited[start] = true;

        var pending = new Queue<int>();
        pending.Enqueue(start);

        while (pending.Count > 0)
        {
            int position = pending.Dequeue();
            reached.Add(position / (CellsAcross * CellsDown));

            foreach (int next in Steps(position, screens.Length))
            {
                if (!visited[next] && Fits(blocked, next))
                {
                    visited[next] = true;
                    pending.Enqueue(next);
                }
            }
        }

        return reached;
    }

    /// <summary>
    ///     Where Blob can stand, as one cell per position of his top left corner. Built for the whole
    ///     planet at once, because the flood crosses screens.
    /// </summary>
    private static bool[] BuildBlockedCells(
        GameScreenData[] screens,
        ScreenCollisionMap collisions,
        Gate gate)
    {
        bool[] blocked = new bool[screens.Length * CellsAcross * CellsDown];

        for (int screen = 0; screen < screens.Length; screen++)
        {
            ScreenTile[,] tiles = screens[screen].ScreenTiles;

            for (int tileY = 0; tileY < TileMetrics.TilesDown; tileY++)
            {
                for (int tileX = 0; tileX < TileMetrics.TilesAcross; tileX++)
                {
                    int image = tiles[tileX, tileY].TileImage;

                    // A marker is a note to the game rather than scenery, and nothing collides with it.
                    if (image < 0 || MarkerTiles.Contains(image))
                    {
                        continue;
                    }

                    BlockGate(blocked, screen, tileX, tileY, image, gate);
                    BlockScenery(blocked, collisions, screen, tileX, tileY, image);
                }
            }
        }

        return blocked;
    }

    /// <summary>
    ///     Shuts a gate, if this tile is one of the kind being sealed. A door is a single cell column
    ///     the height of its tile. An access booth is sealed whole, because Blob cannot get past one
    ///     without the card even though he can stand in its mouth.
    /// </summary>
    private static void BlockGate(
        bool[] blocked,
        int screen,
        int tileX,
        int tileY,
        int image,
        Gate gate)
    {
        if (gate == Gate.KeyDoor && image == (int)ActionTiles.AccessForceField)
        {
            for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
            {
                Block(blocked, screen,
                    tileX * TileMetrics.CollisionCellsAcrossTile + DoorBarrierCell,
                    tileY * TileMetrics.CollisionCellsDownTile + cellDown);
            }

            return;
        }

        if (gate == Gate.AccessBooth &&
            image is (int)ActionTiles.AccessBoothLeft or (int)ActionTiles.AccessBoothRight)
        {
            BlockWholeTile(blocked, screen, tileX, tileY);
        }
    }

    private static void BlockScenery(
        bool[] blocked,
        ScreenCollisionMap collisions,
        int screen,
        int tileX,
        int tileY,
        int image)
    {
        for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
        {
            for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
            {
                if (collisions.IsSolid(image, cellAcross, cellDown))
                {
                    Block(blocked, screen,
                        tileX * TileMetrics.CollisionCellsAcrossTile + cellAcross,
                        tileY * TileMetrics.CollisionCellsDownTile + cellDown);
                }
            }
        }
    }

    private static void BlockWholeTile(bool[] blocked, int screen, int tileX, int tileY)
    {
        for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
        {
            for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
            {
                Block(blocked, screen,
                    tileX * TileMetrics.CollisionCellsAcrossTile + cellAcross,
                    tileY * TileMetrics.CollisionCellsDownTile + cellDown);
            }
        }
    }

    private static void Block(bool[] blocked, int screen, int cellX, int cellY)
        => blocked[Index(screen, cellX, cellY)] = true;

    private static int Index(int screen, int cellX, int cellY)
        => (screen * CellsAcross + cellX) * CellsDown + cellY;

    /// <summary>
    ///     Whether Blob fits with his top left corner at a position, meaning every cell of his
    ///     footprint is clear.
    /// </summary>
    private static bool Fits(bool[] blocked, int position)
    {
        int screen = position / (CellsAcross * CellsDown);
        int within = position % (CellsAcross * CellsDown);
        int cellX = within / CellsDown;
        int cellY = within % CellsDown;

        if (cellX + BlobCellsAcross > CellsAcross || cellY + BlobCellsDown > CellsDown)
        {
            return false;
        }

        for (int across = 0; across < BlobCellsAcross; across++)
        {
            for (int down = 0; down < BlobCellsDown; down++)
            {
                if (blocked[Index(screen, cellX + across, cellY + down)])
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Where Blob starts the flood from: the square the landing site marks, or the nearest place
    ///     he actually fits.
    /// </summary>
    private static int? FindFoothold(bool[] blocked, int screenCount, LandingSite site)
    {
        if (site.ScreenNumber < 0 || site.ScreenNumber >= screenCount)
        {
            return null;
        }

        int cellX = site.BlobStartTileX * TileMetrics.CollisionCellsAcrossTile;
        int cellY = site.BlobStartTileY * TileMetrics.CollisionCellsDownTile;

        for (int radius = 0; radius <= StartSearchRadius; radius++)
        {
            for (int down = -radius; down <= radius; down++)
            {
                for (int across = -radius; across <= radius; across++)
                {
                    int x = cellX + across;
                    int y = cellY + down;

                    if (x < 0 || y < 0 || x >= CellsAcross || y >= CellsDown)
                    {
                        continue;
                    }

                    int position = Index(site.ScreenNumber, x, y);

                    if (Fits(blocked, position))
                    {
                        return position;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     The four places Blob could move to from a position, following his own rules for leaving a
    ///     screen.
    /// </summary>
    /// <remarks>
    ///     Horizontally the planet is a ribbon rather than a grid: walking off the right-hand edge adds
    ///     one to the screen number whatever column it was in, so the end of a row leads to the start
    ///     of the next. Vertically it is a grid, a row of screens at a time. Both follow
    ///     <c>Blob.ChangeScreenOff*Edge</c> and neither is geometric adjacency, which is why this
    ///     cannot be one flat bitmap of the planet.
    /// </remarks>
    private static IEnumerable<int> Steps(int position, int screenCount)
    {
        int screen = position / (CellsAcross * CellsDown);
        int within = position % (CellsAcross * CellsDown);
        int cellX = within / CellsDown;
        int cellY = within % CellsDown;

        // Left and right, stepping to the neighbouring screen number at the far edge.
        if (cellX > 0)
        {
            yield return Index(screen, cellX - 1, cellY);
        }
        else if (screen - 1 >= 0)
        {
            yield return Index(screen - 1, CellsAcross - BlobCellsAcross, cellY);
        }

        if (cellX + BlobCellsAcross < CellsAcross)
        {
            yield return Index(screen, cellX + 1, cellY);
        }
        else if (screen + 1 < screenCount)
        {
            yield return Index(screen + 1, 0, cellY);
        }

        // Up and down, a whole row of screens at a time.
        if (cellY > 0)
        {
            yield return Index(screen, cellX, cellY - 1);
        }
        else if (screen - WorldMap.ScreensAcross >= 0)
        {
            yield return Index(screen - WorldMap.ScreensAcross, cellX, CellsDown - BlobCellsDown);
        }

        if (cellY + BlobCellsDown < CellsDown)
        {
            yield return Index(screen, cellX, cellY + 1);
        }
        else if (screen + WorldMap.ScreensAcross < screenCount)
        {
            yield return Index(screen + WorldMap.ScreensAcross, cellX, 0);
        }
    }
}
