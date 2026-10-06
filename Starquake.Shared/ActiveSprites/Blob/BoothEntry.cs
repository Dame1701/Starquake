// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     Whether Blob has walked far enough into a booth's doorway for the booth to take over - a
///     teleport booth or either hand of an access booth. Three doorways, two reaches, and one
///     standing.
/// </summary>
/// <remarks>
///     The same shape as <see cref="TunnelMouths" /> and for the same reason: which tile it is was
///     already named, and whether Blob was standing in the right place was four bare numbers inside
///     <c>Blob</c>. Two of those were <see cref="BoothMetrics.BlobYOffset" /> written as 32, on the
///     two lines below the one where the teleporter already asked for it by name.
/// </remarks>
internal static class BoothEntry
{
    /// <summary>
    ///     How far into the booth's tile Blob has to have walked for it to count as having stepped
    ///     in, approaching from the left. A little past <see cref="BoothMetrics.BlobXOffset" />, so
    ///     Blob settles back onto the doorway as the booth takes over rather than having to be walked
    ///     into exactly.
    /// </summary>
    /// <remarks>
    ///     The teleporter and the left-hand access booth share this, which was not visible before:
    ///     the teleporter had it as a named constant of <c>Blob</c> and the booth had the same 24
    ///     written inline eighteen lines away.
    /// </remarks>
    private const int EntryReach = 24;

    /// <summary>
    ///     And the reach approaching from the right, which is its own number and a different one.
    ///     Blob meets a right-hand booth from the far side of its tile, so this is measured towards
    ///     the tile's left edge rather than away from it - the same asymmetry the two tunnel mouths
    ///     have, for the same reason.
    /// </summary>
    private const int RightHandBoothReach = 46;

    /// <summary>
    ///     A teleport booth, walked into rightwards. Whether Blob is <em>trying</em> to walk right is
    ///     the control's business and stays with <c>Blob</c>; this is the standing.
    /// </summary>
    public static bool IntoATeleporter(TileDetected? toTheRight)
        => SteppedInFromTheLeft(toTheRight, ActionTiles.TeleportBooth);

    /// <summary>
    ///     The left-hand side of an access booth, which is the side Blob walks rightwards into.
    /// </summary>
    public static bool IntoALeftHandBooth(TileDetected? toTheRight)
        => SteppedInFromTheLeft(toTheRight, ActionTiles.AccessBoothLeft);

    /// <summary>
    ///     The right-hand side, walked into leftwards and so measured the other way across its tile.
    /// </summary>
    public static bool IntoARightHandBooth(TileDetected? toTheLeft)
        => toTheLeft is not null &&
           toTheLeft.TileNumber == (int)ActionTiles.AccessBoothRight &&
           toTheLeft.XPosition <= RightHandBoothReach &&
           toTheLeft.YPosition == BoothMetrics.BlobYOffset;

    private static bool SteppedInFromTheLeft(TileDetected? tile, ActionTiles booth)
        => tile is not null &&
           tile.TileNumber == (int)booth &&
           tile.XPosition >= EntryReach &&
           tile.YPosition == BoothMetrics.BlobYOffset;
}
