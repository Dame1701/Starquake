// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Display;

/// <summary>
///     What the electricity burns like, and the flicker behind it. Driven by
///     <c>Shaders/Electric.fx</c>; see <c>ElectricityGatesAndNodesEngine</c> for where it is handed
///     over.
/// </summary>
/// <remarks>
///     The arcs and the node and gate lights were already drawn additively out of an animated
///     strip, and that has not changed. What the shader adds is a white-hot centre, a halo
///     spreading out of it, and a brightness that is never quite the same two frames running.
/// </remarks>
internal static class ElectricSettings
{
    /// <summary>How far the halo reaches, in the sprite's own pixels.</summary>
    public const float GlowRadius = 3.2f;

    /// <summary>How much of the halo is added back on top of the arc.</summary>
    public const float GlowAmount = 1.1f;

    /// <summary>
    ///     How sharply the white centre takes over from the coloured halo. Higher keeps the white
    ///     to the very middle of the spark.
    /// </summary>
    /// <summary>
    ///     Where a filament starts, as an intensity. Measured from the artwork rather than chosen:
    ///     the band sits almost flat at 0.89 and only its strands reach 0.97 and above, so a
    ///     threshold under that whitens the whole band evenly and the strands stop being visible
    ///     at all.
    /// </summary>
    public const float CoreThreshold = 0.91f;

    public const float CoreSharpness = 1.2f;

    /// <summary>
    ///     How much white is laid into the hot middle of the spark, on top of the artwork rather
    ///     than in place of it.
    /// </summary>
    public const float CoreBoost = 0.8f;

    /// <summary>What the middle of the spark burns.</summary>
    public static Vector3 CoreColour => new(1f, 1f, 1f);

    /// <summary>
    ///     What the halo glows. The cyan the lights are already drawn in, so the spread looks like
    ///     more of the same light rather than a second one.
    /// </summary>
    public static Vector3 HaloColour => new(0.35f, 0.8f, 1f);

    /// <summary>How far either side of its usual brightness the arc wanders.</summary>
    private const double FlickerSwing = 0.3;

    /// <summary>
    ///     How bright the arc burns on a given logic frame.
    /// </summary>
    /// <remarks>
    ///     Two sines at rates that do not divide one another, so it never repeats on any period
    ///     short enough to be noticed as a loop, and no random number generator has to be threaded
    ///     through the draw to get there. It is a function of the frame and nothing else, so it is
    ///     the same on every machine and can be checked without running the game.
    ///     <para>
    ///         Taken from the logic clock rather than the drawn one. <c>GameHost</c> draws once per
    ///         vertical retrace, so a flicker counted in drawn frames would crackle faster on a
    ///         better monitor - which is the defect class <c>GODCLASSES.md</c> lists ten of.
    ///     </para>
    /// </remarks>
    public static float FlickerAt(int logicFrame)
        => (float)(1.0 + FlickerSwing * 0.5 *
                   (Math.Sin(logicFrame * 0.9) + Math.Sin(logicFrame * 2.3 + 1.7)));
}
