// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Display;

/// <summary>
///     What a nasty arriving looks like, and the one sum behind it. Driven by
///     <c>Shaders/Materialise.fx</c>; see <c>Nasty.DrawBirth</c> for where it is handed over.
/// </summary>
/// <remarks>
///     The birth already span the nasty down to square, grew it from nothing and brought it from
///     blue into its own colours over sixty logic frames. This adds the two things a sprite batch
///     alone cannot do: sampling the three colour channels apart from one another, and revealing
///     the sprite by part of itself rather than by its alpha as a whole - two lines opening out
///     from its middle, held still against the screen while the nasty spins under them.
/// </remarks>
internal static class MaterialiseSettings
{
    /// <summary>
    ///     How far apart the red and blue channels are pulled at the moment a nasty starts to
    ///     arrive, as a fraction of the sprite. They converge to nothing as it settles.
    /// </summary>
    public const float ChromaticSpread = 0.22f;

    /// <summary>
    ///     How soft the edge of the arriving lines is, as a fraction of the sprite's height. Too
    ///     hard and it reads as a wipe; too soft and it reads as a fade with no line in it.
    /// </summary>
    public const float ScanSoftness = 0.1f;

    /// <summary>How wide the hot part of each line is, as a fraction of the height.</summary>
    public const float BandWidth = 0.22f;

    /// <summary>
    ///     How hard the lines burn. Over one on purpose - it is meant to bloom in the cathode ray
    ///     tube rather than merely be bright, and the nasty is spinning past at two revolutions a
    ///     second, so anything subtle on it is not seen at all.
    /// </summary>
    public const float BandBrightness = 2.4f;

    /// <summary>
    ///     What the arriving lines glow. The same blue the birth already brings a nasty in on, so
    ///     they look like the thing the colour is arriving from rather than a second idea.
    /// </summary>
    public static Vector3 BandColour => new(0.55f, 0.8f, 1f);

    /// <summary>
    ///     How far through arriving a nasty is, from nought to one.
    /// </summary>
    /// <remarks>
    ///     Clamped at both ends because the birth runs past its own period: it keeps spinning for
    ///     up to <c>BirthSpinOutLimit</c> frames waiting to come to rest square, and a progress
    ///     past one would start pulling the channels apart again in the other direction.
    /// </remarks>
    public static float ProgressThrough(int frame, int period)
        => Math.Clamp(frame / (float)period, 0f, 1f);
}
