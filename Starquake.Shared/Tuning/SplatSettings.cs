// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Tuning;

/// <summary>
///     What an organic nasty coming apart looks like, and the one sum behind it. Driven by
///     <c>Shaders/Splat.fx</c>; see <c>Nasty.DrawDeath</c> for where it is handed over.
/// </summary>
/// <remarks>
///     The corpse already shrank, span and faded while the splat particles played over it. The
///     fade is what this replaces: a body is eaten away from its edges inward along a wandering
///     boundary instead, with a wet rim in the colour that kind of nasty splatters. The shrinking
///     and the spin are untouched.
///     <para>
///         It runs over the death fade's own countdown, so it takes exactly as long as that does
///         and nothing here sets its speed. That is four frames now - the death has been halved
///         twice since this was written, from three seconds to under one - so the dissolve is a
///         flicker of a body being taken apart rather than something to watch. If it is ever
///         wanted back, <c>Nasty.CreatureFadeStep</c> is the one number - and it is a creature's
///         alone, where a machine's wreck lies in its explosion three times as long under
///         <see cref="MachineDeathSettings" />.
///     </para>
///     <para>
///         Spiky and conveyor nasties never reach this. They are machines, they explode, and
///         nothing about that wanted changing.
///     </para>
/// </remarks>
internal static class SplatSettings
{
    /// <summary>How soft the eaten edge is. Too hard and it reads as a cut-out.</summary>
    public const float EdgeSoftness = 0.09f;

    /// <summary>How wide the wet rim on the boundary is.</summary>
    public const float RimWidth = 0.14f;

    /// <summary>
    ///     How hard the rim burns. Over one on purpose - it is meant to bloom in the cathode ray
    ///     tube rather than merely be bright.
    /// </summary>
    public const float RimBrightness = 1.9f;

    /// <summary>
    ///     How much the boundary wanders. At nought the body would dissolve as a clean oval, which
    ///     is the thing that reads as mechanical rather than organic.
    /// </summary>
    public const float Lumpiness = 0.14f;

    /// <summary>
    ///     How far gone the corpse is, from nought the moment it is shot to one once there is
    ///     nothing left.
    /// </summary>
    /// <remarks>
    ///     Taken from the death fade the corpse already counted down, so the body comes apart over
    ///     exactly the time it used to fade over and nothing else about the death had to move.
    /// </remarks>
    public static float DecayFrom(int deathFadeLevel, int fullFade)
        => Math.Clamp(1f - deathFadeLevel / (float)fullFade, 0f, 1f);
}
