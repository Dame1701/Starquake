// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     One frame of a value moving steadily towards a limit and stopping there. A fade up out of
///     black, a corpse fading out and shrinking, a core element growing into place are all this.
/// </summary>
internal static class Ramp
{
    /// <summary>
    ///     Moves <paramref name="value" /> up by <paramref name="step" />, never past
    ///     <paramref name="limit" />.
    /// </summary>
    /// <param name="value">Where the value is now.</param>
    /// <param name="step">
    ///     How far to move it, as a magnitude. A step of nought leaves the value where it is, which
    ///     is why this can be called every frame without a flag for whether anything is running.
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
