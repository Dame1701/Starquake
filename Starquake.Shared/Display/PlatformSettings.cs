// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Display;

/// <summary>
///     What a jump platform looks like as it comes apart. Driven by <c>Shaders/Platform.fx</c>; the
///     dissolve itself is <c>SpritePixelMatrix</c>'s and is not changed.
/// </summary>
/// <remarks>
///     The platform artwork is a dark slab - a mean of 45, 33, 34 across its lit texels, with a
///     median intensity under a fifth - so rubbing dark pixels out of it was nearly invisible.
///     Lighting the edge of every hole is what makes the dissolve something you can watch.
/// </remarks>
internal static class PlatformSettings
{
    /// <summary>
    ///     What a cut edge burns. Close to the arcing blue the rest of the game's energy is drawn
    ///     in, because a platform Blob stands on out of thin air is of a piece with it.
    /// </summary>
    public static Vector3 EdgeColour => new(0.4f, 0.75f, 1f);

    /// <summary>How hard a cut edge burns.</summary>
    public const float EdgeBoost = 1.6f;

    /// <summary>
    ///     How much the whole slab lifts toward that colour as it goes, so it reads as charging up
    ///     to failure rather than only being eaten away at the edges.
    /// </summary>
    public const float Charge = 0.35f;

    /// <summary>
    ///     How far through coming apart a platform is, from the countdown it already runs.
    /// </summary>
    /// <remarks>
    ///     Nought for the whole first half of its life, which it spends standing whole: the
    ///     dissolve does not start until then, and a platform that glowed before it had begun to
    ///     go would be promising something it was not yet doing.
    /// </remarks>
    public static float DecayFrom(int dissolveTime, int dissolveStartsAt)
        => Math.Clamp((dissolveStartsAt - dissolveTime) / (float)dissolveStartsAt, 0f, 1f);
}
