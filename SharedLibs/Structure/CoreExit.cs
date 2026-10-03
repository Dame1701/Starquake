// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     The core's doorstep: the square Blob is put down on when he leaves the planet core.
/// </summary>
/// <remarks>
///     <para>
///         More than a position, because three things key off the screen it is on. It is where Blob
///         emerges (<c>PlanetCoreScreen</c>), it is the screen the core is kept ticking over for
///         so that it is still working when Blob walks back in (<c>GameEngine</c>), and it is the
///         screen nasties are disabled on (<c>Nasty.Update</c>). All three used to read
///         <see cref="WorldMap.CoreApproachScreenNumber" />, which is why moving the core meant
///         editing the game.
///     </para>
///     <para>
///         Unlike the core itself this is one square, so it is a <see cref="MarkerTile" /> like the
///         landing site rather than a position needing finer resolution than the tile grid.
///     </para>
/// </remarks>
public sealed record CoreExit(int ScreenNumber, int TileX, int TileY)
{

    /// <summary>
    ///     The doorstep the game had written into it before it was on the map: the right-hand column of
    ///     the screen beside the core, on the one square there with a floor under it. Used for a map
    ///     saved before the marker existed, so that an older planet still puts Blob somewhere he can
    ///     stand rather than nowhere.
    /// </summary>
    public static CoreExit Original { get; } = new(WorldMap.CoreApproachScreenNumber, 7, 4);

    /// <summary>
    ///     Reads the doorstep off a map, falling back to the one the game shipped with when no marker
    ///     has been placed.
    /// </summary>
    public static CoreExit FindIn(GameScreenData[] screens)
        => MarkerTiles.Locate(screens, MarkerTile.CoreExitPosition) is { } placed
            ? new CoreExit(placed.ScreenNumber, placed.TileX, placed.TileY)
            : Original;
}
