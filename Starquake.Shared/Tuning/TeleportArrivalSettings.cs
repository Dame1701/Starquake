// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The shape of the shimmer a teleport arrival leaves on the room. Driven by
///     <c>Shaders/TeleportArrival.fx</c>; see <c>TeleportArrivalEffect</c>.
/// </summary>
internal static class TeleportArrivalSettings
{
    /// <summary>
    ///     How long the room takes to settle, in logic frames. Just over a second: long enough to be
    ///     seen arriving, short enough to be over before the player wonders what is wrong with the
    ///     screen.
    /// </summary>
    public static readonly int Frames = LogicClock.Seconds(1.2);

    /// <summary>
    ///     How far the picture is pushed about at worst, as a fraction of it. Small: a fiftieth of
    ///     the screen is already a pronounced ripple, and much more reads as the room coming apart
    ///     rather than settling.
    /// </summary>
    public static Vector2 Amplitude => new(0.018f, 0.012f);

    /// <summary>
    ///     How closely the waves are spaced along each axis. The two share no common factor, so the
    ///     pattern never stands still long enough to read as stripes.
    /// </summary>
    public static Vector2 Frequency => new(29f, 17f);

    /// <summary>How much the brightness swims with the waves.</summary>
    public const float Glimmer = 0.35f;

    /// <summary>
    ///     The cold light the beam leaves on the room as it goes. The same blue the teleport beam
    ///     uses, so the arrival looks like the end of that rather than a new idea.
    /// </summary>
    public static Vector3 ArrivalColour => new(0.1f, 0.22f, 0.4f);

    /// <summary>How fast the waves travel, in radians per logic frame.</summary>
    private const double WaveSpeed = 0.55;

    /// <summary>
    ///     How unsettled the room still is, one as Blob arrives and nought once settled.
    /// </summary>
    /// <remarks>
    ///     Squared, so it falls away quickly and then lingers. A linear settle reads as the effect
    ///     being switched off at a moment rather than something dying down.
    /// </remarks>
    public static float StrengthAt(int frame)
    {
        float left = Math.Clamp(1f - frame / (float)Frames, 0f, 1f);

        return left * left;
    }

    /// <summary>
    ///     Where the waves have travelled to by a given frame. Logic frames, so they run at the same
    ///     rate whatever the display is doing.
    /// </summary>
    public static float PhaseAt(int frame) => (float)(frame * WaveSpeed);
}
