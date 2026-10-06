// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.Rules;

/// <summary>
///     Whether Blob has walked far enough into a tunnel mouth for it to take him through to the next
///     screen. Which tiles are mouths is <see cref="TunnelTiles" />'s; this is the standing and the
///     reach, which is the part that decides whether walking at one works.
/// </summary>
/// <remarks>
///     Three conditions, and the tile number is only the first of them. Blob also has to be standing
///     on the tunnel's floor rather than above it, and far enough across the tile - so a mouth in the
///     wall of a shaft he is falling past does not swallow him, and one he is standing on the lip of
///     does not either.
/// </remarks>
internal static class TunnelMouths
{
    /// <summary>
    ///     How far across its own tile Blob has to have got for a mouth to take him, measured from
    ///     the tile's left edge for a left-hand mouth and towards it for a right-hand one.
    /// </summary>
    /// <remarks>
    ///     Its own number, left as one. Three constants in this codebase are also 32 - a collision
    ///     cell either way, half a sprite, and <see cref="StandingInTheMouth" /> - and deriving this
    ///     from any of them would quietly tie the reach of a tunnel to something it has no reason to
    ///     follow. It was a bare 32 written inline in two conditions, which is the only reason it
    ///     needs saying at all. <c>ScreenCollisionMap.GameObjectProbeSize</c> is the same story.
    /// </remarks>
    private const int MouthReach = 32;

    /// <summary>
    ///     Where in the tile Blob's top edge sits when he is standing on the tunnel floor, which is
    ///     the only height a mouth takes him at. Also its own number: it happens to equal a tile's
    ///     height less a sprite's, which is what standing on the floor of a tile means, and it is
    ///     left written down rather than derived for the same reason as <see cref="MouthReach" /> -
    ///     the two were one bare number each and the point here is to name them, not to re-decide
    ///     them.
    /// </summary>
    private const int StandingInTheMouth = 32;

    /// <summary>
    ///     A left-hand mouth, which steps the screen number down. Blob walks at it leftwards, so he
    ///     meets it from the far side of its tile and has to get within <see cref="MouthReach" /> of
    ///     that tile's left edge.
    /// </summary>
    public static bool IntoALeftMouth(TileDetected? toTheLeft)
        => toTheLeft is not null &&
           TunnelTiles.IsLeftMouth(toTheLeft.TileNumber) &&
           toTheLeft.XPosition <= MouthReach &&
           toTheLeft.YPosition == StandingInTheMouth;

    /// <summary>
    ///     A right-hand mouth, which steps the screen number up. The reach is the other way round:
    ///     walking rightwards Blob meets the tile from its left, so he has to have got
    ///     <see cref="MouthReach" /> into it.
    /// </summary>
    public static bool IntoARightMouth(TileDetected? toTheRight)
        => toTheRight is not null &&
           TunnelTiles.IsRightMouth(toTheRight.TileNumber) &&
           toTheRight.XPosition >= MouthReach &&
           toTheRight.YPosition == StandingInTheMouth;
}
