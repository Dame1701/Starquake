// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     The square Blob is put down on when leaving the planet core.
/// </summary>
/// <remarks>
///     Three things key off the screen this is on: where Blob emerges, which screen the core is kept
///     running on so it is still working when Blob returns, and which screen nasties are disabled on.
///     <para>
///         It occupies one square, so it is a <see cref="MarkerTile" />. The core itself is not,
///         because it needs finer resolution than the tile grid.
///     </para>
/// </remarks>
public sealed record CoreExit(int ScreenNumber, int TileX, int TileY)
{

    /// <summary>
    ///     The fallback for a map with no marker placed: the one square in the right-hand column of
    ///     the screen beside the core that has a floor under it. Keeps an older planet putting Blob
    ///     somewhere standable.
    /// </summary>
    public static CoreExit Original { get; } = new(WorldMap.CoreApproachScreenNumber, 7, 4);

    /// <summary>
    ///     Reads the exit square off a map, falling back to <see cref="Original" /> when no marker
    ///     has been placed.
    /// </summary>
    public static CoreExit FindIn(GameScreenData[] screens)
        => MarkerTiles.Locate(screens, MarkerTile.CoreExitPosition) is { } placed
            ? new CoreExit(placed.ScreenNumber, placed.TileX, placed.TileY)
            : Original;
}
