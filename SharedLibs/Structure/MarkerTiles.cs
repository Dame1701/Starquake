// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     A position the level author marks on a screen, rather than scenery to draw there.
/// </summary>
/// <remarks>
///     <para>
///         A screen is stored as a grid of tile numbers, so the only way to say "a floor nasty walks
///         here" or "this is where the blob's ship comes down" is to paint a number that means it.
///         These are those numbers, and the game draws none of them: there is no artwork for any of
///         them on either tile sheet, and the screen editor draws a labelled box of its own wherever
///         one has been placed.
///     </para>
///     <para>
///         They are numbered clear of the tile sheets on purpose. A sheet is 440 tiles, so a number
///         from here up can never be mistaken for scenery and nothing can draw one by accident by
///         indexing a sheet with it. <see cref="ActionTiles" /> is the other half of the story -
///         tiles that do have artwork, but that the game has to recognise by number as well as draw.
///     </para>
/// </remarks>
public enum MarkerTile
{

    /// <summary>
    ///     A gap that should stay a gap. Nothing is drawn and nothing is placed here, which is what
    ///     reserves the square for the blob to move through.
    /// </summary>
    Spacer = 500,

    /// <summary>
    ///     Where a conveyer nasty walks. One is created per marker on the screen.
    /// </summary>
    ConveyerNasty = 501,

    /// <summary>
    ///     Where a core element would best be seeded, for a screen that would otherwise scatter one
    ///     somewhere awkward to reach.
    /// </summary>
    PreferredCoreLocation = 502,

    /// <summary>
    ///     Where the blob is put down at the start of a game. The screen this is on is the screen the
    ///     game opens on, so there is one of these on the whole planet.
    /// </summary>
    BlobStartPosition = 503,

    /// <summary>
    ///     Where the blob's ship comes to rest. The intro sequence flies the ship into this square and
    ///     the wreck smokes here for the rest of the game, so it belongs on the same screen as
    ///     <see cref="BlobStartPosition" /> - the blob climbs out of the wreck it arrived in.
    /// </summary>
    ShipCrashSite = 504
}

/// <summary>
///     Where a marker has been placed: which screen, and which square of it.
/// </summary>
public readonly record struct MarkerPlacement(int ScreenNumber, int TileX, int TileY);

/// <summary>
///     The markers as a set, for the questions that are about all of them rather than any one.
/// </summary>
public static class MarkerTiles
{

    public static IReadOnlyList<MarkerTile> All { get; } = Enum.GetValues<MarkerTile>();

    private static readonly HashSet<int> Numbers = BuildNumbers();

    /// <summary>
    ///     Whether there is meant to be exactly one of a marker on the whole planet. The landing site
    ///     is the only thing that works that way - a game opens on one screen, so there is one start
    ///     position and one wreck - and the screen editor is what holds the map to it.
    /// </summary>
    public static bool IsOnePerMap(MarkerTile marker)
        => marker is MarkerTile.BlobStartPosition or MarkerTile.ShipCrashSite;

    /// <summary>
    ///     Whether a tile number is a marker. Asked once per tile per frame while a screen is drawn and
    ///     once more while its collision map is built, which is why it is a set lookup rather than a
    ///     walk of the enum.
    /// </summary>
    public static bool Contains(int tileImage) => Numbers.Contains(tileImage);

    /// <summary>
    ///     Where a marker has been placed, or null if the map has none of it. The first one found
    ///     wins, which matters only for the markers there is meant to be one of.
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
    ///     Reads the enum rather than <see cref="All" />, so that this cannot depend on which of the two
    ///     is declared first. It did, and every static field in the game that reached for the markers
    ///     while building its own came up empty.
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
