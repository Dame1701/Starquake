// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Structure;

/// <summary>
///     Whether Blob has walked off an edge of the screen he is on, and which screen that puts him
///     on. The planet is one flat array of screens stitched together by index - see
///     <see cref="WorldMap" /> - so each edge is an arithmetic step on the screen number and a
///     bound on the map beyond it.
/// </summary>
/// <remarks>
///     Four separate questions rather than one "which edge", because the four are asked in turn and
///     more than one of them can answer. Walking off the left edge while a lift is carrying Blob up
///     through the top puts him a screen left <em>and</em> a screen up, and the diagonal is reachable:
///     the left crossing moves him and the top crossing is then asked about the position it left him
///     in. Collapsing this into one decision would quietly stop that working.
///     <para>
///         The sideways pair are also what stop a crossing being taken twice. Going left lands Blob
///         with his right edge exactly on the screen's width, which is the one position
///         <see cref="OffRight" /> reads as still on the screen; going right lands him at nought,
///         which is the one position <see cref="OffLeft" /> reads the same way. That is an equality
///         holding by a single pixel in both directions, which is worth a test rather than a
///         comment - see <c>ScreenEdgesTests</c>.
///     </para>
/// </remarks>
internal static class ScreenEdges
{
    /// <summary>
    ///     Off the left edge, which puts him against the far side of the screen before this one.
    ///     Nothing happens on the map's first screen; there is no column to the left of it.
    /// </summary>
    public static ScreenCrossing? OffLeft(int xPos, int yPos, int width, int screenWidth, int screenNumber)
    {
        if (xPos >= 0 || screenNumber - 1 < 0)
        {
            return null;
        }

        return new ScreenCrossing(screenNumber - 1, screenWidth - width, yPos);
    }

    /// <inheritdoc cref="OffLeft" />
    public static ScreenCrossing? OffRight(int xPos, int yPos, int width, int screenWidth, int screenNumber)
    {
        if (xPos + width <= screenWidth || screenNumber + 1 > WorldMap.LastScreenNumber)
        {
            return null;
        }

        return new ScreenCrossing(screenNumber + 1, 0, yPos);
    }

    /// <summary>
    ///     Off the top edge, which is a whole row of screens back. <see cref="ScreenCrossing.YPos" />
    ///     comes back unchanged: Blob has to be put down on something and what that is depends on
    ///     what is carrying him.
    /// </summary>
    public static ScreenCrossing? OffTop(int xPos, int yPos, int screenNumber)
    {
        if (yPos >= 0 || screenNumber - WorldMap.ScreensAcross < 0)
        {
            return null;
        }

        return new ScreenCrossing(screenNumber - WorldMap.ScreensAcross, xPos, yPos);
    }

    /// <summary>
    ///     Off the bottom edge, a row forward, arriving at the top of the new screen still falling.
    /// </summary>
    /// <param name="height">
    ///     Blob's full height, shuttle included - he is taller riding one, and that is what decides
    ///     whether his feet have left the screen.
    /// </param>
    public static ScreenCrossing? OffBottom(int xPos, int yPos, int height, int screenHeight, int screenNumber)
    {
        if (yPos + height <= screenHeight || screenNumber + WorldMap.ScreensAcross > WorldMap.LastScreenNumber)
        {
            return null;
        }

        return new ScreenCrossing(screenNumber + WorldMap.ScreensAcross, xPos, 0);
    }
}
