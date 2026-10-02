// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Display;

/// <summary>
///     A screen coming back up out of black. Every page and the game itself arrive dark and brighten over
///     a few frames, which is what stops a screen change being a jolt.
/// </summary>
/// <remarks>
///     Four lines, and they were written twice: once in <c>GameScreenBase.Update</c> for the five
///     full-screen pages, and once in <c>MainGameScreen.FadeScreenBackUp</c> for the game. The two copies
///     were character for character the same but for a <c>+=</c>, and the game cannot inherit the base -
///     see that class for why - so sharing them means a function rather than a base method.
/// </remarks>
internal static class ScreenFadeUp
{
    /// <summary>
    ///     One frame of brightening: the level moves up by <paramref name="speed" /> until it reaches full,
    ///     and never past it.
    /// </summary>
    /// <param name="level">How bright the screen is now, from nought to <see cref="ColourLevels.Full" />.</param>
    /// <param name="speed">
    ///     How much brighter to make it. Nought is what a screen that is not fading holds, which is why a
    ///     screen at full brightness and a screen with no speed both come back unchanged.
    /// </param>
    public static int Advanced(int level, int speed)
    {
        if (level < ColourLevels.Full)
        {
            level += speed;
        }

        if (level > ColourLevels.Full)
        {
            level = ColourLevels.Full;
        }

        return level;
    }
}
