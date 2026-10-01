// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.WorldGeneration;

/// <summary>
///     The thing that can stand between the blob and a screen: a door the key opens, or an access
///     booth the card gets through.
/// </summary>
internal enum Gate
{

    /// <summary>
    ///     A key-locked door. Each <see cref="ActionTiles.AccessForceField" /> tile is one, and it is
    ///     solid to the blob until it is carrying the key - see <c>GameScreenCollection.UpdateKeyLockDoor</c>.
    /// </summary>
    KeyDoor,

    /// <summary>
    ///     An access booth. Getting through one means satisfying its code, and the access card
    ///     satisfies any digit of one - see <c>AccessBoothScreen.CheckInventoryAgainstCode</c>.
    /// </summary>
    AccessBooth
}

/// <summary>
///     Which screens the blob can reach from where its ship came down without going through a gate.
/// </summary>
/// <remarks>
///     <para>
///         This is what stops the key being put somewhere that needs the key, and the access card
///         somewhere that needs the card. "Every route to this screen passes a gate" is the same
///         statement as "this screen cannot be reached once the gates are shut", so one flood from
///         the landing site with the gates sealed answers it for all 512 screens at once rather than
///         tracing a route per candidate.
///     </para>
///     <para>
///         <b>The model is deliberately an under-estimate.</b> It moves the blob by walking, falling
///         and climbing only, and knows nothing of tunnel mouths, tunnel lifts, shuttles or
///         teleporters. Every one of those can only ever <i>add</i> reachability, so a screen this
///         returns is one the blob genuinely reaches without meeting a gate, and the ones it misses
///         are merely candidates given up. That asymmetry is the whole point: over-estimating would
///         put the key behind a door and make the game unwinnable, while under-estimating costs
///         nothing but variety, and there are still some 185 screens to choose from on the shipped
///         map. It is also why the planet core does not appear in the result and does not need to.
///     </para>
///     <para>
///         Climbing is treated as free because it very nearly is: walking off the top of a screen
///         hands the blob a fresh jump platform to stand on (<c>Blob.ChangeScreenOffTopEdge</c>), so
///         height is not what limits it.
///     </para>
/// </remarks>
internal static class GateFreeScreens
{

    /// <summary>
    ///     The collision cells across and down one screen. Movement is worked out on this grid rather
    ///     than on the tiles, because a door is a single cell wide and a tile is four.
    /// </summary>
    private const int CellsAcross = TileMetrics.TilesAcross * TileMetrics.CollisionCellsAcrossTile;

    /// <inheritdoc cref="CellsAcross" />
    private const int CellsDown = TileMetrics.TilesDown * TileMetrics.CollisionCellsDownTile;

    /// <summary>
    ///     How many cells of the grid the blob fills, each way. It is as wide as two cells and as tall
    ///     as two, so a one cell gap is not a way through - which is exactly what a door is.
    /// </summary>
    private const int BlobCellsAcross = SpriteMetrics.SpriteWidth / TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="BlobCellsAcross" />
    private const int BlobCellsDown = SpriteMetrics.SpriteHeight / TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     How far into its tile a key-locked door's barrier stands, in cells. The door adds its
    ///     collision at <c>Location.X + 48</c>, which is partway through the tile rather than across
    ///     the whole of it, so sealing the tile would wall off more than the door does.
    /// </summary>
    private const int DoorBarrierCell = 48 / TileMetrics.CollisionCellWidth;

    /// <summary>
    ///     How far the search will look for somewhere the blob fits, if the landing site itself is
    ///     inside scenery. A badly marked start should still seed the flood rather than report that
    ///     the planet is empty.
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
    ///     Where the blob can stand, as one cell per position of its top left corner. Built for the
    ///     whole planet at once because the flood crosses screens.
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

                    // A marker is a note to the game, not scenery, and nothing collides with one.
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
    ///     the height of its tile; an access booth is sealed whole, because the blob cannot get past
    ///     one without the card even though it can stand in the mouth of it.
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
    ///     Whether the blob fits with its top left corner at a position, which means every cell of its
    ///     footprint being clear.
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
    ///     Where the blob starts the flood from: the square the landing site marks, or the nearest
    ///     place to it that it actually fits.
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
    ///     The four places the blob could move to from a position, following its own rules for leaving
    ///     a screen.
    /// </summary>
    /// <remarks>
    ///     Horizontally the planet is a ribbon rather than a grid: walking off the right hand edge adds
    ///     one to the screen number whatever column it was in, so the end of a row leads to the start
    ///     of the next one rather than to nothing. Vertically it is a grid, a row of screens at a time.
    ///     Both of those are <c>Blob.ChangeScreenOff*Edge</c> and neither is geometric adjacency, which
    ///     is why this cannot be one flat bitmap of the planet.
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
