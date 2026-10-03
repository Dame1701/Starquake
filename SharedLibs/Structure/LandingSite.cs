// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     Where Blob's ship came down: the screen a new game opens on, the square Blob is put
///     down on, and the square the wreck rests in.
/// </summary>
/// <remarks>
///     <para>
///         All three used to be numbers written into the game - a screen number in the world
///         constants, a start position in Blob, a crash position in the intro sequence - which
///         meant the one screen the game always begins on could not be moved without editing three
///         files. They are part of the map now, marked with <see cref="MarkerTile.BlobStartPosition" />
///         and <see cref="MarkerTile.ShipCrashSite" />, and this is what reads them back.
///     </para>
///     <para>
///         Positions are in tiles rather than pixels. Turning a square into a place to stand needs
///         the size of the thing standing there, and the sprite sizes belong to the game rather than
///         to the map - see <c>GameScreenCollection.BlobStartPosition</c>.
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
    ///     The site the game had written into it before it was part of the map, used for a map saved
    ///     before the two markers existed. Without it such a map would start Blob nowhere, so
    ///     this is the same courtesy <see cref="TeleportCodes.Original" /> does for the codes.
    /// </summary>
    public static LandingSite Original { get; } = new(8, 3, 4, 5, 3);

    /// <summary>
    ///     Reads the site off a map. Both markers have to be found for the map to be believed, because
    ///     a start position with no wreck to climb out of is a half-authored screen rather than a
    ///     deliberate one, and the original site is a better answer than half of a new one.
    /// </summary>
    public static LandingSite FindIn(GameScreenData[] screens)
    {
        MarkerPlacement? start = MarkerTiles.Locate(screens, MarkerTile.BlobStartPosition);
        MarkerPlacement? crash = MarkerTiles.Locate(screens, MarkerTile.ShipCrashSite);

        if (start is not { } startSquare || crash is not { } crashSquare)
        {
            return Original;
        }

        // The game can only open on one screen, so a crash site on another one cannot be honoured.
        // The start position decides, because that is where Blob has to be put down.
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
