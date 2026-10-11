// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The core room's own loop, given some room to sound in. See <c>PlanetCoreScreen</c>.
/// </summary>
/// <remarks>
///     The recording is a single mono voice, which on its own sits dead centre and flat. It is
///     played twice, the two copies a little out of tune with one another and circling the
///     listener on opposite sides, so it spreads across the speakers and moves the way the funnel
///     does. The tuning apart also lets the two slide out of time with each other, which is what
///     stops them adding back up to one voice in the middle.
///     <para>
///         Over that, the pitch follows the elements flashing: up as they show the passed colour,
///         down as they show the failed one. The sound's answer to the picture, the way
///         <see cref="MalfunctionSettings.MusicPitchAt" /> is the report's.
///     </para>
/// </remarks>
internal static class CoreSoundSettings
{
    /// <summary>How many copies of the loop are played. See the remarks on the class.</summary>
    public const int Voices = 2;

    /// <summary>How loud each copy is at the front of its circle.</summary>
    private const float Volume = 0.4f;

    /// <summary>
    ///     How much quieter a copy is at the back of its circle than the front, as a share of
    ///     <see cref="Volume" />. The one cue a pair of speakers has for something passing behind.
    /// </summary>
    private const float QuieterBehind = 0.35f;

    /// <summary>How far towards a speaker a copy goes at the side of its circle.</summary>
    private const float Spread = 0.75f;

    /// <summary>How long one trip round the listener takes.</summary>
    private static readonly int OrbitFrames = LogicClock.Seconds(9);

    /// <summary>
    ///     How far either side of true each copy plays, where one would be an octave. About a
    ///     seventh of a semitone, which is out of tune enough to chorus and not enough to sour.
    /// </summary>
    private const float Detune = 0.012f;

    /// <summary>
    ///     How far the pitch bends at the height of the flashing, either way. Comfortably more than
    ///     <see cref="Detune" />, so it reads as the core changing rather than the copies drifting.
    /// </summary>
    private const float FlashBend = 0.07f;

    /// <summary>
    ///     How strongly the elements, taken together, are showing passed rather than failed: one
    ///     all passed, minus one all failed.
    /// </summary>
    /// <param name="flashes">
    ///     Each missing element's own lean, as <c>CoreElement.Flash</c>.
    /// </param>
    /// <remarks>
    ///     Summed over the square root of the count rather than averaged. The elements flash out of
    ///     step with each other, so a straight average of nine of them flattens to nearly nothing and
    ///     the bend would only be heard once the core was all but finished. This keeps the swing
    ///     about the same however many are left.
    /// </remarks>
    public static float FlashSwing(IReadOnlyCollection<float> flashes)
    {
        if (flashes.Count == 0)
        {
            return 0f;
        }

        return Math.Clamp(flashes.Sum() / MathF.Sqrt(flashes.Count), -1f, 1f);
    }

    /// <summary>
    ///     How far off true a copy plays, for a sound effect instance's pitch: its own detuning plus
    ///     the bend from the flashing.
    /// </summary>
    public static float PitchOf(int voice, float flashSwing)
        => Math.Clamp(FlashBend * flashSwing + (voice % 2 == 0 ? -Detune : Detune), -1f, 1f);

    /// <summary>Which speaker a copy is in on a given frame, minus one left to one right.</summary>
    public static float PanOf(int voice, int logicFrame)
        => Spread * MathF.Sin(AngleOf(voice, logicFrame));

    /// <summary>How loud a copy is on a given frame, quietest as it passes behind.</summary>
    public static float VolumeOf(int voice, int logicFrame)
        => Volume * (1f - QuieterBehind * (1f - MathF.Cos(AngleOf(voice, logicFrame))) / 2f);

    /// <summary>
    ///     Where a copy is on its circle, nought being straight ahead. The copies are spaced evenly
    ///     round it, so two are always on opposite sides.
    /// </summary>
    private static float AngleOf(int voice, int logicFrame)
        => 2f * MathF.PI * (logicFrame / (float)OrbitFrames + voice / (float)Voices);
}
