// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The shape of the planet. Screens are held in one flat array and stitched into a map by their
///     index alone - walking off the right edge of a screen adds one, walking off the bottom adds a
///     whole row - so these two numbers are the only thing that says where a screen sits relative to
///     its neighbours.
/// </summary>
public static class WorldMap
{
    /// <summary>
    ///     How many screens make up one row of the map. Adding or subtracting this is what moves the
    ///     blob a screen down or up, and a screen number modulo this is its column.
    /// </summary>
    public const int ScreensAcross = 16;

    /// <summary>
    ///     How many screens there are in total.
    /// </summary>
    public const int ScreenCount = 512;

    /// <summary>
    ///     How many rows of screens the map is deep.
    /// </summary>
    public const int ScreensDown = ScreenCount / ScreensAcross;

    /// <summary>
    ///     The number of the last screen in the map.
    /// </summary>
    public const int LastScreenNumber = ScreenCount - 1;

    /// <summary>
    ///     The planet core room. It is a screen like any other as far as the map is concerned, but
    ///     the game has to recognise it by number: it is seeded differently from the rest, and it
    ///     keeps running while the blob is elsewhere.
    /// </summary>
    public const int CoreScreenNumber = 199;

    /// <summary>
    ///     The room next to the core, which is where the blob is put down on leaving it.
    /// </summary>
    public const int CoreApproachScreenNumber = 198;

    /// <summary>
    ///     Where the blob's ship came down, which is both where a new game starts and the only screen
    ///     that draws the wreck and its smoke.
    /// </summary>
    public const int ShipWreckScreenNumber = 8;
}
