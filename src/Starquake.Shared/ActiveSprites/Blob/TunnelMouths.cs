// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     Whether Blob has walked far enough into a tunnel mouth to be taken through to the next screen.
///     Which tiles are mouths belongs to <see cref="TunnelTiles" />; this is the standing height and
///     the reach.
/// </summary>
/// <remarks>
///     Three conditions, of which the tile number is only the first. Blob must also be standing on
///     the tunnel's floor rather than above it, and far enough across the tile. That stops a mouth in
///     the wall of a shaft swallowing a falling Blob, and one being entered from its lip.
/// </remarks>
internal static class TunnelMouths
{
    /// <summary>
    ///     How far across its tile Blob must get for a mouth to take him, measured from the tile's
    ///     left edge for a left-hand mouth and towards it for a right-hand one.
    /// </summary>
    /// <remarks>
    ///     Deliberately its own number. Several other constants happen to be 32 as well, including a
    ///     collision cell, half a sprite, and <see cref="StandingInTheMouth" />, and deriving this
    ///     from any of them would tie a tunnel's reach to something it has no reason to follow.
    /// </remarks>
    private const int MouthReach = 32;

    /// <summary>
    ///     Where Blob's top edge sits when standing on the tunnel floor, which is the only height a
    ///     mouth takes him at. Its own number, for the same reason as <see cref="MouthReach" />,
    ///     though it happens to equal a tile's height less a sprite's.
    /// </summary>
    private const int StandingInTheMouth = 32;

    /// <summary>
    ///     A left-hand mouth, which steps the screen number down. Blob walks at it leftwards, meeting
    ///     it from the far side of its tile, so must get within <see cref="MouthReach" /> of that
    ///     tile's left edge.
    /// </summary>
    public static bool IntoALeftMouth(TileDetected? toTheLeft)
        => toTheLeft is not null &&
           TunnelTiles.IsLeftMouth(toTheLeft.TileNumber) &&
           toTheLeft.XPosition <= MouthReach &&
           toTheLeft.YPosition == StandingInTheMouth;

    /// <summary>
    ///     A right-hand mouth, which steps the screen number up. The reach is measured the other way:
    ///     walking rightwards, Blob meets the tile from its left and must get
    ///     <see cref="MouthReach" /> into it.
    /// </summary>
    public static bool IntoARightMouth(TileDetected? toTheRight)
        => toTheRight is not null &&
           TunnelTiles.IsRightMouth(toTheRight.TileNumber) &&
           toTheRight.XPosition >= MouthReach &&
           toTheRight.YPosition == StandingInTheMouth;
}
