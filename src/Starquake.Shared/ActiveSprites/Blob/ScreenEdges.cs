// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;

namespace Starquake.ActiveSprites;

/// <summary>
///     Whether Blob has walked off an edge of the current screen, and which screen that leads to.
///     The planet is one flat array of screens indexed by position (<see cref="WorldMap" />), so each
///     edge is an arithmetic step on the screen number and a bound on the map beyond it.
/// </summary>
/// <remarks>
///     Four separate questions rather than one "which edge", because they are asked in turn and more
///     than one can answer. Walking off the left edge while a lift carries Blob through the top moves
///     a screen left <em>and</em> a screen up: the left crossing moves Blob, then the top crossing is
///     asked about the resulting position. Collapsing this into one decision would stop that working.
///     <para>
///         The sideways pair also stop a crossing being taken twice. Going left lands Blob's right
///         edge exactly on the screen's width, the one position <see cref="OffRight" /> reads as
///         still on the screen; going right lands at nought, which <see cref="OffLeft" /> reads the
///         same way. Both equalities hold by a single pixel, so <c>ScreenEdgesTests</c> covers them.
///     </para>
/// </remarks>
internal static class ScreenEdges
{
    /// <summary>
    ///     Off the left edge, arriving against the far side of the previous screen. Nothing happens
    ///     on the map's first screen, which has no column to its left.
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
    ///     comes back unchanged, because what Blob lands on depends on what is carrying him.
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
    ///     Blob's full height, shuttle included, since that is what decides whether his feet have
    ///     left the screen.
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
