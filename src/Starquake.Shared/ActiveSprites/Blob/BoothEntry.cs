// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.Core;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     Whether Blob has walked far enough into a booth's doorway for the booth to take over. Covers
///     a teleport booth and both hands of an access booth: three doorways, two reaches, one standing
///     height.
/// </summary>
internal static class BoothEntry
{
    /// <summary>
    ///     How far into the tile Blob must walk to count as stepped in, approaching from the left.
    ///     A little past <see cref="BoothMetrics.BlobXOffset" />, so Blob settles back onto the
    ///     doorway as the booth takes over rather than having to be walked into exactly. Shared by
    ///     the teleporter and the left-hand access booth.
    /// </summary>
    private const int EntryReach = 24;

    /// <summary>
    ///     The reach approaching from the right, a different number. Blob meets a right-hand booth
    ///     from the far side of its tile, so this is measured towards the tile's left edge rather
    ///     than away from it. The two tunnel mouths are asymmetric the same way.
    /// </summary>
    private const int RightHandBoothReach = 46;

    /// <summary>
    ///     A teleport booth, walked into rightwards. Only the standing position: whether Blob is
    ///     <em>trying</em> to walk right belongs to the controls, in <c>Blob</c>.
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
