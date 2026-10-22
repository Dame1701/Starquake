// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     What a nasty draining Blob's energy looks like. Driven by <c>Shaders/Overload.fx</c>'s
///     <c>Draining</c> pass; see <c>Blob.DrawBlobSprite</c> for where it is handed over.
/// </summary>
/// <remarks>
///     Energy going out of something is not steady, so neither is this: the wash moves with a
///     flicker that bites harder the more of Blob has gone, and a corona gathers on his outline where
///     it is leaving him. A flat tint reads as a healthy light of a different colour instead.
/// </remarks>
internal static class EnergyDrainSettings
{

    /// <summary>How hard the corona gathers on Blob's outline.</summary>
    public const float CoronaAmount = 1.5f;

    /// <summary>How far out the corona is looked for, in the sprite's own pixels.</summary>
    public const float CoronaRadius = 1.4f;
    /// <summary>
    ///     What the loss burns. Not pure red: a little green keeps Blob looking lit rather than like
    ///     a silhouette cut out of the background.
    /// </summary>
    public static Vector3 DrainColour => new(1f, 0.18f, 0.12f);

    /// <summary>
    ///     How far gone Blob's energy is, taken from the counter the drain already runs. That counter
    ///     is the green and blue remaining, so it reads full when unhurt and nought when a nasty has
    ///     all of it, which is the opposite of what the shader wants.
    /// </summary>
    public static float DrainFrom(int energyDrainCounter)
        => Math.Clamp(1f - energyDrainCounter / (float)ColourLevels.Full, 0f, 1f);

    /// <summary>
    ///     Whether there is any drain to draw. Nearly always there is not, so the shader is skipped
    ///     rather than run over every pixel of Blob to do nothing.
    /// </summary>
    public static bool IsDraining(int energyDrainCounter) => energyDrainCounter < ColourLevels.Full;
}
