// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

namespace Starquake.Display;

/// <summary>
///     The planet's tremors. Every so often the ground shakes for a few seconds, and it shakes once on
///     purpose as the intro's ship finishes coming down.
/// </summary>
/// <remarks>
///     Decides whether the ground is moving and by how much, and hands back a <see cref="Shake" />
///     saying so. Acting on the rumble and the view port is the caller's job, which lets this be
///     tested with no sound engine and no screen effects.
/// </remarks>
internal sealed class ScreenShake
{
    /// <summary>
    ///     How the random tremor is rolled for: one number in <see cref="Odds" /> on each frame it is
    ///     not already shaking. At fifty frames a second that is a tremor every forty seconds or so.
    /// </summary>
    private const int Odds = 2000;

    /// <inheritdoc cref="Odds" />
    private const int Trigger = 1;

    /// <summary>How long a tremor lasts, in logic frames: between three and six seconds.</summary>
    private const int Shortest = 180;

    /// <inheritdoc cref="Shortest" />
    private const int Longest = 300;

    /// <summary>
    ///     How long the intro's own tremor lasts. Longer than any random one, because it is the ship
    ///     coming down rather than the planet settling.
    /// </summary>
    private const int IntroDuration = 500;

    /// <summary>
    ///     The largest offset the view port is pushed by, exclusive. Small, so the picture trembles
    ///     rather than lurches.
    /// </summary>
    private const int LargestOffset = 5;

    /// <summary>
    ///     How long a jolt lasts, in logic frames, and how hard it starts. Short and sharp where a
    ///     tremor is long and low, because something has gone off.
    /// </summary>
    private const int JoltFrames = 11;

    /// <inheritdoc cref="JoltFrames" />
    private const int LargestJoltOffset = 11;

    private int _framesShaken;

    private int _framesToShake;

    private int _joltFramesLeft;

    private bool _shaking;

    /// <summary>
    ///     Something has exploded. Knocks the picture about for a fraction of a second, harder than a
    ///     tremor and over much sooner.
    /// </summary>
    /// <remarks>
    ///     A jolt makes no rumble. A blast already carries its own noise, and a rumble started and
    ///     stopped inside a fifth of a second would just be a click.
    /// </remarks>
    public void Jolt() => _joltFramesLeft = JoltFrames;

    /// <summary>One logic frame of the ground moving.</summary>
    /// <param name="randomNumberGenerator">Rolls for a tremor and for how far the picture is pushed.</param>
    /// <param name="introRunning">
    ///     Whether the intro is still playing. No random tremor starts during it, because the ship
    ///     coming down is the only shake the opening should have.
    /// </param>
    /// <param name="introAtEndPause">
    ///     Whether the intro has reached the pause at its end, the frame the ship lands on and where
    ///     the deliberate tremor starts.
    /// </param>
    /// <param name="screenEffectRunning">
    ///     Whether a screen effect is composing the picture. While one is, the view port belongs to it
    ///     and the ground is left alone rather than fighting for the offset.
    /// </param>
    /// <returns>
    ///     What to do this frame, or <c>null</c> while a screen effect owns the picture, which means
    ///     leave both the view port and the rumble as they are rather than stopping either.
    /// </returns>
    public Shake? Step(
        Random randomNumberGenerator,
        bool introRunning,
        bool introAtEndPause,
        bool screenEffectRunning)
    {
        // The roll happens whether or not it is already shaking, as the original had it. It reads
        // from the generator the nasties also draw from, so rolling on a different set of frames
        // would move every random thing in the game along with it.
        if (!introRunning)
        {
            if (randomNumberGenerator.Next(0, Odds) == Trigger && !_shaking)
            {
                _framesToShake = randomNumberGenerator.Next(Shortest, Longest);
                _shaking = true;
            }
        }

        if (introAtEndPause)
        {
            _framesToShake = IntroDuration;
            _shaking = true;
        }

        if (screenEffectRunning)
        {
            return null;
        }

        // A jolt takes the frame from whatever the ground is doing rather than adding to it. Both
        // are rolled fresh each frame, so adding them would make a tremor during a jolt no different
        // from a jolt alone.
        if (_joltFramesLeft > 0)
        {
            _joltFramesLeft--;

            // Hardest on the first frame and all but gone by the last, so it hits and settles rather
            // than buzzing. Never nought, because a Shake with an offset of nought on either axis
            // does not move the picture at all.
            int hardness = 1 + LargestJoltOffset * _joltFramesLeft / JoltFrames;

            return new Shake(
                randomNumberGenerator.Next(1, hardness + 1),
                randomNumberGenerator.Next(1, hardness + 1),
                false);
        }

        if (!_shaking)
        {
            return Shake.Still;
        }

        int xOffset = randomNumberGenerator.Next(0, LargestOffset);
        int yOffset = randomNumberGenerator.Next(0, LargestOffset);

        _framesShaken++;

        if (_framesShaken > _framesToShake)
        {
            _framesShaken = 0;
            _shaking = false;
        }

        return new Shake(xOffset, yOffset, true);
    }
}
