// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     A position the level author marks on a screen, rather than scenery to draw there.
/// </summary>
/// <remarks>
///     A screen is stored as a grid of tile numbers, so saying "a conveyor nasty walks here" means
///     painting a number that means it. None of these are drawn: no tile sheet has artwork for them,
///     and the screen editor draws its own labelled box wherever one is placed.
///     <para>
///         The numbers start clear of the tile sheets on purpose. A sheet is 440 tiles, so nothing
///         from here up can be mistaken for scenery or drawn by accident. <see cref="ActionTiles" />
///         covers the opposite case: tiles with artwork that must also be recognised by number.
///     </para>
/// </remarks>
public enum MarkerTile
{

    /// <summary>
    ///     A gap that should stay a gap. Nothing is drawn or placed here, reserving the square for
    ///     Blob to move through.
    /// </summary>
    Spacer = 500,

    /// <summary>Where a conveyor nasty walks. One nasty is created per marker on the screen.</summary>
    ConveyorNasty = 501,

    /// <summary>
    ///     Where to seed a core element, for a screen that would otherwise scatter one somewhere
    ///     awkward to reach.
    /// </summary>
    PreferredCoreLocation = 502,

    /// <summary>
    ///     Where Blob is put down at the start of a game. Its screen is the screen the game opens on,
    ///     so there is one of these on the whole planet.
    /// </summary>
    BlobStartPosition = 503,

    /// <summary>
    ///     Where Blob's ship comes to rest. The intro flies the ship into this square and the wreck
    ///     smokes here for the rest of the game, so it must be on the same screen as
    ///     <see cref="BlobStartPosition" />.
    /// </summary>
    ShipCrashSite = 504,

    /// <summary>
    ///     Where Blob is put down on leaving the planet core. Its screen is also the one the core
    ///     keeps running for and the one nasties are disabled on. See <see cref="CoreExit" />.
    /// </summary>
    CoreExitPosition = 505
}

/// <summary>
///     Where a marker has been placed: which screen, and which square of it.
/// </summary>
public readonly record struct MarkerPlacement(int ScreenNumber, int TileX, int TileY);

/// <summary>
///     The markers as a set, for questions about all of them rather than any one.
/// </summary>
public static class MarkerTiles
{

    private static readonly HashSet<int> Numbers = BuildNumbers();

    public static IReadOnlyList<MarkerTile> All { get; } = Enum.GetValues<MarkerTile>();

    /// <summary>
    ///     Whether exactly one of this marker is meant to exist on the whole planet. True for the
    ///     landing site and the core exit. The screen editor holds the map to it.
    /// </summary>
    public static bool IsOnePerMap(MarkerTile marker)
        => marker is MarkerTile.BlobStartPosition or MarkerTile.ShipCrashSite or MarkerTile.CoreExitPosition;

    /// <summary>
    ///     Whether a tile number is a marker. A set lookup rather than a walk of the enum, because
    ///     this is asked once per tile per frame while a screen is drawn, and again while its
    ///     collision map is built.
    /// </summary>
    public static bool Contains(int tileImage) => Numbers.Contains(tileImage);

    /// <summary>
    ///     Where a marker has been placed, or null if the map has none. The first one found wins,
    ///     which only matters for the one-per-map markers.
    /// </summary>
    public static MarkerPlacement? Locate(GameScreenData[] screens, MarkerTile marker)
    {
        for (int screen = 0; screen < screens.Length; screen++)
        {
            MarkerPlacement? placement = LocateOn(screens, screen, marker);

            if (placement is not null)
            {
                return placement;
            }
        }

        return null;
    }

    /// <summary>
    ///     Where a marker has been placed on one screen, or null if it is not on that screen.
    /// </summary>
    public static MarkerPlacement? LocateOn(GameScreenData[] screens, int screen, MarkerTile marker)
    {
        ScreenTile[,] tiles = screens[screen].ScreenTiles;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (tiles[x, y].TileImage == (int)marker)
                {
                    return new MarkerPlacement(screen, x, y);
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Reads the enum directly, not <see cref="All" />. Going through <see cref="All" /> would
    ///     make this depend on static field initialisation order, and any static field elsewhere that
    ///     reads the markers while building its own would come up empty.
    /// </summary>
    private static HashSet<int> BuildNumbers()
    {
        var numbers = new HashSet<int>();

        foreach (MarkerTile marker in Enum.GetValues<MarkerTile>())
        {
            numbers.Add((int)marker);
        }

        return numbers;
    }
}
