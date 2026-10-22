// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;
using Starquake.Tuning;

namespace Starquake.ActiveSprites;

/// <summary>
///     How Blob comes apart. Running out of energy sounds an alarm for a second first. Everything
///     else that kills Blob, such as spikes, electricity or a nasty, goes straight to the explosion.
/// </summary>
/// <remarks>
///     The shape of the death only, which is the part that is arithmetic. The sound, the fragments and
///     putting Blob back belong to <see cref="Blob" />, because each needs something the overload has
///     no business holding.
/// </remarks>
internal sealed class BlobOverload
{
    /// <summary>How far the blast throws its shells each frame once Blob has come apart.</summary>
    private const float DischargeStep = 0.55f;

    /// <summary>How long the alarm sounds first, in logic frames. A second at fifty hertz.</summary>
    private const int AlarmFrames = 60;

    /// <summary>
    ///     How long coming apart takes, in logic frames. A quarter of a second, because this is a
    ///     charge letting go and anything slower reads as posing rather than failing.
    /// </summary>
    public const int CrackFrames = 12;

    /// <summary>
    ///     How big Blob is by the time he has come apart. Growing to this is what widens the cracks,
    ///     so they open because the halves are separating rather than alongside it.
    /// </summary>
    private const float CrackScale = 1.75f;

    /// <summary>
    ///     How long the blast lasts, in logic frames. A tenth of a second, because it is the flash at
    ///     the end rather than a stage in its own right.
    /// </summary>
    private const int DischargeFrames = 5;

    /// <summary>
    ///     The flare drawn during the explosion: how many copies of Blob, how much smaller the first
    ///     one starts, and how much bigger each one after it is.
    /// </summary>
    public const int FlareCopies = 5;

    /// <inheritdoc cref="FlareCopies" />
    private const float FlareFirstScale = 0.4f;

    /// <inheritdoc cref="FlareCopies" />
    private const float FlareScaleStep = 0.2f;

    private int _alarmFramesSounded;

    private int _crackFramesDone;

    private int _dischargeFramesDone;

    /// <summary>
    ///     How big Blob is drawn. Only ever grows: out to <see cref="CrackScale" /> while coming
    ///     apart, and further out through the blast.
    /// </summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>
    ///     Which part of coming apart Blob is in, so the draw can tell cracking from bursting.
    /// </summary>
    public OverloadStage Stage { get; private set; }

    /// <summary>
    ///     How far open the cracks are, from nought to one. Nought once past that stage, so nothing
    ///     has to ask whether it is still running.
    /// </summary>
    public float Split => Stage == OverloadStage.Cracking
        ? Math.Clamp(_crackFramesDone / (float)CrackFrames, 0f, 1f)
        : 0f;

    /// <summary>
    ///     Whether the alarm is still running. False from the start for a death that skips it, as
    ///     <see cref="Begin" /> is told.
    /// </summary>
    public bool AlarmSounding => Stage != OverloadStage.Exploded && _alarmFramesSounded < AlarmFrames;

    /// <summary>
    ///     Whether the alarm began this frame, which is when the sound is asked for. That is the
    ///     frame after the death, because the count advances at the end of a frame.
    /// </summary>
    public bool AlarmJustStarted => _alarmFramesSounded == 1;

    /// <summary>
    ///     Whether Blob is drawn this frame. Blob flickers on every other frame of the alarm.
    /// </summary>
    public bool Visible => _alarmFramesSounded % 2 == 0;

    /// <summary>
    ///     Whether the flare is drawn instead of the flickering Blob. The flare takes over once the
    ///     alarm has run its course, whether or not there was one.
    /// </summary>
    public bool ShowingTheAlarm => _alarmFramesSounded < AlarmFrames;

    /// <summary>
    ///     Logic frames the overload has run for, which drives both the flicker on Blob's skin and
    ///     the shake of the picture. Counted on update and never in draw: <c>GameHost</c> draws once
    ///     per vertical retrace, so anything counted in drawn frames crackles faster on a better
    ///     monitor.
    /// </summary>
    /// <remarks>
    ///     Deliberately not reset by <see cref="Begin" />. Carrying across lives starts each death at
    ///     a different point of <c>OverloadSettings.ShakeAt</c>'s sines, so two deaths do not shake
    ///     identically. Nothing reading it cares where it started: the shake's size comes from
    ///     <see cref="Split" /> and the flicker is a clock.
    /// </remarks>
    public int Frame { get; private set; }

    /// <summary>
    ///     Which frame of the electricity artwork the beams are drawn from, so they crawl as well as
    ///     turn. Derived: <see cref="Frame" /> wrapped to the length of the run.
    /// </summary>
    public int ArcFrame => Frame % OverloadSettings.BeamArcFrames;

    /// <summary>Starts a death.</summary>
    /// <param name="soundTheAlarmFirst">
    ///     True for running out of energy, which gives a second of warning. False for everything else,
    ///     which begins already exploded. That is how the alarm is skipped, and the only thing
    ///     <see cref="AlarmSounding" /> tests the stage for.
    /// </param>
    public void Begin(bool soundTheAlarmFirst)
    {
        Stage = soundTheAlarmFirst ? OverloadStage.Cracking : OverloadStage.Exploded;
        _alarmFramesSounded = 0;
        _crackFramesDone = 0;
        _dischargeFramesDone = 0;
    }

    /// <summary>One more frame of the alarm.</summary>
    public void CountAnAlarmFrame() => _alarmFramesSounded++;

    /// <inheritdoc cref="Frame" />
    public void CountAFrame() => Frame++;

    /// <summary>
    ///     One frame of coming apart, and the stage it leaves Blob at. A death that began already
    ///     exploded stays there, which is what makes it immediate.
    /// </summary>
    public OverloadStage Swell()
    {
        if (Stage == OverloadStage.Cracking)
        {
            _crackFramesDone++;

            // Growth and crack width share one count, so they cannot drift apart: at the widest,
            // Blob is also at his most broken.
            Scale = 1f + (CrackScale - 1f) * Split;

            if (_crackFramesDone >= CrackFrames)
            {
                Stage = OverloadStage.Discharging;
                Scale = CrackScale;
            }

            return Stage;
        }

        if (Stage == OverloadStage.Discharging)
        {
            _dischargeFramesDone++;
            Scale += DischargeStep;

            if (_dischargeFramesDone >= DischargeFrames)
            {
                Stage = OverloadStage.Exploded;
            }
        }

        return Stage;
    }

    /// <summary>Back to normal size, ready for the next death.</summary>
    public void Reset()
    {
        Stage = OverloadStage.Cracking;
        _crackFramesDone = 0;
        _dischargeFramesDone = 0;
        Scale = 1f;
    }

    /// <summary>
    ///     The scales the flare is drawn at, smallest first: a stack of copies growing outward from
    ///     Blob's current size. The guard against a negative scale stays even though nothing reaches
    ///     one, because a copy at a negative scale is a drawing mistake rather than a smaller one.
    /// </summary>
    public IEnumerable<float> FlareScales()
    {
        float scale = Scale - FlareFirstScale;

        for (int copy = 0; copy < FlareCopies; copy++)
        {
            if (scale >= 0f)
            {
                yield return scale;
            }

            scale += FlareScaleStep;
        }
    }
}
