// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Constants;

/// <summary>
///     The shape of the planet. Screens live in one flat array and their index alone places them on
///     the map. Walking off the right edge adds one; walking off the bottom adds a whole row.
/// </summary>
public static class WorldMap
{
    /// <summary>
    ///     How many screens make up one row. Add or subtract this to move a screen down or up; a
    ///     screen number modulo this is its column.
    /// </summary>
    public const int ScreensAcross = 16;

    public const int ScreenCount = 512;

    public const int ScreensDown = ScreenCount / ScreensAcross;

    public const int LastScreenNumber = ScreenCount - 1;

    /// <summary>
    ///     The planet core room. The map treats it as an ordinary screen, but the game recognises it
    ///     by number: it is seeded differently, and it keeps running while Blob is elsewhere.
    /// </summary>
    public const int CoreScreenNumber = 199;

    /// <summary>The room next to the core, where Blob is put down on leaving it.</summary>
    public const int CoreApproachScreenNumber = 198;
}
