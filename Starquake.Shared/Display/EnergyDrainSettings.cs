// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Display;

/// <summary>
///     What a nasty draining Blob's energy looks like. Driven by <c>Shaders/Overload.fx</c>'s
///     <c>Draining</c> pass; see <c>Blob.DrawBlobSprite</c> for where it is handed over.
/// </summary>
/// <remarks>
///     It used to be a flat tint - red held at full while green and blue came down together, which
///     is a healthy light of a different colour. Energy going out of something is not steady, so
///     this is not either: the wash moves with a flicker that bites harder the more of him has
///     gone, and a corona gathers on his outline where it is leaving him.
/// </remarks>
internal static class EnergyDrainSettings
{
    /// <summary>
    ///     What the loss burns. Not pure red: a little green in it keeps him looking lit rather
    ///     than looking like a silhouette cut out of the background.
    /// </summary>
    public static Vector3 DrainColour => new(1f, 0.18f, 0.12f);

    /// <summary>How hard the corona gathers on his outline.</summary>
    public const float CoronaAmount = 1.5f;

    /// <summary>How far out the corona is looked for, in the sprite's own pixels.</summary>
    public const float CoronaRadius = 1.4f;

    /// <summary>
    ///     How far gone his energy is, from the counter the drain already runs. The counter is the
    ///     green and blue he has left, so it reads full when he is unhurt and nought when a nasty
    ///     has all of it - which is the other way round from what the shader wants.
    /// </summary>
    public static float DrainFrom(int energyDrainCounter)
        => Math.Clamp(1f - energyDrainCounter / (float)ColourLevels.Full, 0f, 1f);

    /// <summary>
    ///     Whether there is any drain to draw at all. Nearly all the time there is not - he is only
    ///     touched now and then - so the shader is skipped rather than run over every pixel of him
    ///     to do nothing.
    /// </summary>
    public static bool IsDraining(int energyDrainCounter) => energyDrainCounter < ColourLevels.Full;
}
