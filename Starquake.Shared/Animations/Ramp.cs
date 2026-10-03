// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Animations;

/// <summary>
///     One frame of a value moving steadily toward a limit and stopping there. A fade up out of
///     black, a corpse fading out and shrinking, a core element growing into place - all of them are
///     this, and all of them had written it out by hand.
/// </summary>
/// <remarks>
///     Eight copies, in two directions and two types: <c>ScreenFadeUp.Advanced</c> for the pages and
///     the game, the fade and the shrink in both <c>Nasty</c> and <c>ConveyerNasty</c>,
///     <c>TeleportScreen</c>'s fade out, <c>PlanetCoreScreen</c>'s blanking tiles, and the whole
///     bodies of <c>FloatAdder</c>, <c>FloatSubtractor</c> and <c>IntegerSubtractor</c>.
///     <para>
///         <c>ScreenFadeUp</c> had already found half of this - it says so itself, that four lines
///         were written twice - and named only the upward integer half. The measurement that found
///         the other six is in <c>GODCLASSES.md</c>.
///     </para>
/// </remarks>
internal static class Ramp
{
    /// <summary>
    ///     Moves <paramref name="value" /> up by <paramref name="step" />, never past
    ///     <paramref name="limit" />.
    /// </summary>
    /// <param name="value">Where the value is now.</param>
    /// <param name="step">
    ///     How far to move it, as a magnitude. Nought is what a value that is not moving holds, which
    ///     is why this can be called every frame without a flag saying whether anything is running.
    /// </param>
    /// <param name="limit">The value to stop at, and to clamp back to from beyond it.</param>
    public static int Up(int value, int step, int limit)
    {
        if (value < limit)
        {
            value += step;
        }

        if (value > limit)
        {
            value = limit;
        }

        return value;
    }

    /// <summary>
    ///     Moves <paramref name="value" /> down by <paramref name="step" />, never past
    ///     <paramref name="limit" />.
    /// </summary>
    /// <inheritdoc cref="Up(int, int, int)" />
    public static int Down(int value, int step, int limit)
    {
        if (value > limit)
        {
            value -= step;
        }

        if (value < limit)
        {
            value = limit;
        }

        return value;
    }

    /// <inheritdoc cref="Up(int, int, int)" />
    public static float Up(float value, float step, float limit)
    {
        if (value < limit)
        {
            value += step;
        }

        if (value > limit)
        {
            value = limit;
        }

        return value;
    }

    /// <inheritdoc cref="Down(int, int, int)" />
    public static float Down(float value, float step, float limit)
    {
        if (value > limit)
        {
            value -= step;
        }

        if (value < limit)
        {
            value = limit;
        }

        return value;
    }
}
