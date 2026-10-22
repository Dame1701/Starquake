// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     Where Blob's ship came down: the screen a new game opens on, the square Blob is put
///     down on, and the square the wreck rests in.
/// </summary>
/// <remarks>
///     Marked on the map with <see cref="MarkerTile.BlobStartPosition" /> and
///     <see cref="MarkerTile.ShipCrashSite" />. This reads them back.
///     <para>
///         Positions are in tiles, not pixels. Turning a square into a place to stand needs the size
///         of what stands there, and sprite sizes belong to the game rather than the map. See
///         <c>GameScreenCollection.BlobStartPosition</c>.
///     </para>
/// </remarks>
public sealed record LandingSite(
    int ScreenNumber,
    int BlobStartTileX,
    int BlobStartTileY,
    int CrashTileX,
    int CrashTileY)
{

    /// <summary>
    ///     The fallback for a map with neither marker placed. Without it such a map would start Blob
    ///     nowhere.
    /// </summary>
    public static LandingSite Original { get; } = new(8, 3, 4, 5, 3);

    /// <summary>
    ///     Reads the site off a map. Both markers must be found, because a start position with no
    ///     wreck to climb out of is a half-authored screen, and <see cref="Original" /> is a better
    ///     answer than half a new site.
    /// </summary>
    public static LandingSite FindIn(GameScreenData[] screens)
    {
        MarkerPlacement? start = MarkerTiles.Locate(screens, MarkerTile.BlobStartPosition);
        MarkerPlacement? crash = MarkerTiles.Locate(screens, MarkerTile.ShipCrashSite);

        if (start is not { } startSquare || crash is not { } crashSquare)
        {
            return Original;
        }

        // The game opens on one screen, so a crash site on a different screen cannot be honoured.
        return crashSquare.ScreenNumber != startSquare.ScreenNumber
            ? Original
            : new LandingSite(
                startSquare.ScreenNumber,
                startSquare.TileX,
                startSquare.TileY,
                crashSquare.TileX,
                crashSquare.TileY);
    }
}
