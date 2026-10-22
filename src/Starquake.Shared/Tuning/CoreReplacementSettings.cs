// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The two beats either side of a part going into the planet's core: the core finding what it
///     wants and shaking it loose, and the room being wrung round the slot once the part is in.
/// </summary>
/// <remarks>
///     Driven by <c>Shaders/CoreSurge.fx</c> and <c>CoreElement</c>'s own drawing. Deliberately a
///     different shape from <c>TeleportArrivalSettings</c>' standing waves: arriving somewhere
///     happens to the room, where this is the room answering something put into it.
/// </remarks>
internal static class CoreReplacementSettings
{

    /// <summary>How far the identified part shakes at its worst, in pixels.</summary>
    private const float ShakeDistance = 2.5f;

    /// <summary>How much the identified part brightens at the top of its pulse.</summary>
    public const float GlowAmount = 0.8f;

    /// <summary>How fast the glow pulses, in radians per logic frame.</summary>
    private const double PulseSpeed = 0.42;

    /// <summary>How far round the room is wrung at the slot, in radians.</summary>
    public const float Twist = 1.15f;

    /// <summary>How many times the ripple crosses the distance the wring reaches over.</summary>
    public const float Ripples = 7f;

    /// <summary>How far the wring reaches, as a fraction of the room.</summary>
    public const float Reach = 0.62f;

    /// <summary>How fast the ripple travels outward, in radians per logic frame.</summary>
    private const double RippleSpeed = 0.5;

    /// <summary>How hard that light burns at the slot.</summary>
    public const float Surge = 0.75f;
    /// <summary>
    ///     How long the core spends picking a part out before it goes in, in logic frames. Without
    ///     this beat a part simply appears and shrinks.
    /// </summary>
    public static readonly int IdentifyFrames = LogicClock.Seconds(1.1);

    /// <summary>How long the room takes to recover from a part landing, in logic frames.</summary>
    public static readonly int SurgeFrames = LogicClock.Seconds(0.9);

    /// <summary>The light the core throws back through the room as it takes a part.</summary>
    public static Vector3 SurgeColour => new(1f, 0.74f, 0.32f);

    /// <summary>
    ///     How hard the identified part is glowing and shaking on a given frame. A pulse rather than
    ///     a steady light, so it reads as the core working at the part rather than as a label.
    /// </summary>
    public static float PulseAt(int frame)
        => (float)((Math.Sin(frame * PulseSpeed) + 1) * 0.5);

    /// <summary>
    ///     Where the identified part sits relative to where it is drawn, as it is shaken loose.
    /// </summary>
    /// <remarks>
    ///     Two rates that do not divide one another, one per axis, so it rattles rather than tracing
    ///     a line. The same reasoning as <c>OverloadSettings.ShakeAt</c>.
    /// </remarks>
    public static Vector2 ShakeAt(int frame)
        => new(
            (float)Math.Sin(frame * 2.9) * ShakeDistance,
            (float)Math.Sin(frame * 4.1 + 1.3) * ShakeDistance);

    /// <summary>
    ///     How unsettled the room still is after a part lands, one at the landing and nought once
    ///     recovered. Squared, so it springs back and then eases, like
    ///     <c>TeleportArrivalSettings.StrengthAt</c>.
    /// </summary>
    public static float SurgeStrengthAt(int frame)
    {
        float left = Math.Clamp(1f - frame / (float)SurgeFrames, 0f, 1f);

        return left * left;
    }

    /// <summary>Where the ripple has travelled to by a given frame.</summary>
    public static float RippleAt(int frame) => (float)(frame * RippleSpeed);
}
