// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Tuning;

/// <summary>
///     What an organic nasty coming apart looks like, and the one sum behind it. Driven by
///     <c>Shaders/Splat.fx</c>; see <c>Nasty.DrawDeath</c> for where it is handed over.
/// </summary>
/// <remarks>
///     The corpse is eaten away from its edges inward along a wandering boundary, with a wet rim in
///     the colour that kind of nasty splatters. The shrinking and the spin happen alongside it.
///     <para>
///         It runs over the death fade's own countdown, so it takes exactly as long as that and
///         nothing here sets its speed. At four frames the dissolve is a flicker of a body being
///         taken apart rather than something to watch; <c>Nasty.CreatureFadeStep</c> is the one
///         number that changes it. That is a creature's alone: a machine's wreck lies in its
///         explosion three times as long under <see cref="MachineDeathSettings" />.
///     </para>
///     <para>Spiky and conveyor nasties never reach this. They are machines and they explode.</para>
/// </remarks>
internal static class SplatSettings
{
    /// <summary>How soft the eaten edge is. Too hard and it reads as a cut-out.</summary>
    public const float EdgeSoftness = 0.09f;

    /// <summary>How wide the wet rim on the boundary is.</summary>
    public const float RimWidth = 0.14f;

    /// <summary>
    ///     How hard the rim burns. Over one on purpose, so it blooms in the cathode ray tube rather
    ///     than merely being bright.
    /// </summary>
    public const float RimBrightness = 1.9f;

    /// <summary>
    ///     How much the boundary wanders. At nought the body dissolves as a clean oval, which reads
    ///     as mechanical rather than organic.
    /// </summary>
    public const float Lumpiness = 0.14f;

    /// <summary>
    ///     How far gone the corpse is, from nought the moment it is shot to one once nothing is left.
    /// </summary>
    /// <remarks>
    ///     Taken from the death fade the corpse already counts down, so the body comes apart over
    ///     exactly that time and nothing else about the death has to change.
    /// </remarks>
    public static float DecayFrom(int deathFadeLevel, int fullFade)
        => Math.Clamp(1f - deathFadeLevel / (float)fullFade, 0f, 1f);
}
