// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

using Starquake.Core;
using Starquake.Tuning;
namespace Starquake.ActiveSprites;

/// <summary>
///     How Blob comes apart. Running out of energy sounds an alarm for a second first; everything else
///     that kills him - spikes, electricity, a nasty, the debug key - skips straight to the explosion.
///     Then he swells, shrinks through zero to inside out, and goes.
/// </summary>
/// <remarks>
///     The shape of the death only, which is the part of it that is arithmetic. The explosion's sound, the
///     fragments and putting Blob back where he came from are all <see cref="Blob" />'s, because each
///     needs something the overload has no business holding.
/// </remarks>
internal sealed class BlobOverload
{
    /// <summary>
    ///     How far the blast throws its shells each frame once he has come apart.
    /// </summary>
    private const float DischargeStep = 0.55f;

    /// <summary>
    ///     How long the alarm sounds before he goes, in logic frames - a second at fifty hertz.
    /// </summary>
    private const int AlarmFrames = 60;

    /// <summary>
    ///     How long he takes to come apart, in logic frames. A quarter of a second: this is a
    ///     charge letting go, and anything slower reads as him posing rather than failing.
    /// </summary>
    public const int CrackFrames = 12;

    /// <summary>
    ///     How big he is by the time he has come apart. He pulls himself open as he splits, so the
    ///     cracks widen because the two halves are separating rather than beside it.
    /// </summary>
    private const float CrackScale = 1.75f;

    /// <summary>
    ///     How long the blast lasts, in logic frames. A tenth of a second - it is the flash at the
    ///     end, not a stage in its own right.
    /// </summary>
    private const int DischargeFrames = 5;

    /// <summary>
    ///     The flare drawn as he explodes: how many copies of him, how much smaller than he is the first
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

    private OverloadStage _stage;

    /// <summary>
    ///     How big he is drawn. It only ever grows now: out to <see cref="CrackScale" /> as he
    ///     comes apart, and on outward through the blast. It used to come back down through zero
    ///     as well, which drew him inside out.
    /// </summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>
    ///     Which part of coming apart he is in, so the draw can tell the cracking from the burst.
    /// </summary>
    public OverloadStage Stage => _stage;

    /// <summary>
    ///     How far open the cracks are, from nought to one. Nought once he is past that stage, so
    ///     nothing has to ask whether it is still running.
    /// </summary>
    public float Split => _stage == OverloadStage.Cracking
        ? Math.Clamp(_crackFramesDone / (float)CrackFrames, 0f, 1f)
        : 0f;

    /// <summary>
    ///     Whether the alarm is still running. False from the start for a death that skips it, which is
    ///     what <see cref="Begin" /> is told.
    /// </summary>
    public bool AlarmSounding => _stage != OverloadStage.Exploded && _alarmFramesSounded < AlarmFrames;

    /// <summary>
    ///     Whether the alarm has just this frame begun, which is when the sound is asked for. It is the
    ///     frame after the one he died on, because the count is advanced at the end of a frame.
    /// </summary>
    public bool AlarmJustStarted => _alarmFramesSounded == 1;

    /// <summary>
    ///     Whether Blob is drawn at all this frame. He flickers through the alarm, on every other
    ///     frame of it.
    /// </summary>
    public bool Visible => _alarmFramesSounded % 2 == 0;

    /// <summary>
    ///     Whether what is drawn is the flickering Blob or the flare. The flare takes over when the alarm
    ///     has run its course, whether or not there was one.
    /// </summary>
    public bool ShowingTheAlarm => _alarmFramesSounded < AlarmFrames;

    /// <summary>
    ///     Starts a death.
    /// </summary>
    /// <param name="soundTheAlarmFirst">
    ///     True for running out of energy, which gives him a second of warning. False for everything else,
    ///     which begins already exploded - that is how the alarm is skipped, and the only thing
    ///     <see cref="AlarmSounding" /> tests the stage for.
    /// </param>
    public void Begin(bool soundTheAlarmFirst)
    {
        _stage = soundTheAlarmFirst ? OverloadStage.Cracking : OverloadStage.Exploded;
        _alarmFramesSounded = 0;
        _crackFramesDone = 0;
        _dischargeFramesDone = 0;
    }

    /// <summary>One more frame of the alarm.</summary>
    public void CountAnAlarmFrame() => _alarmFramesSounded++;

    /// <summary>
    ///     Logic frames the overload has run for, which is what the flicker on his skin and the
    ///     shake of the picture are both read off. Counted on the update and never in the draw:
    ///     <c>GameHost</c> draws once per vertical retrace, so anything counted in drawn frames
    ///     crackles faster on a better monitor.
    /// </summary>
    /// <remarks>
    ///     Deliberately not reset by <see cref="Begin" />. It carries across lives, so two deaths
    ///     start at different points of <c>OverloadSettings.ShakeAt</c>'s sines and do not shake
    ///     identically - and nothing reading it cares where it started, because the shake's size
    ///     comes from <see cref="Split" /> and the flicker is a clock.
    /// </remarks>
    public int Frame { get; private set; }

    /// <summary>
    ///     Which frame of the electricity artwork the beams are drawn from, so they crawl as well
    ///     as turn. Derived rather than held: it is <see cref="Frame" /> wrapped to the length of
    ///     the run.
    /// </summary>
    public int ArcFrame => Frame % OverloadSettings.BeamArcFrames;

    /// <inheritdoc cref="Frame" />
    public void CountAFrame() => Frame++;

    /// <summary>
    ///     One frame of coming apart, and what stage that leaves him at. If he began already exploded
    ///     he stays there, which is what makes the explosion immediate.
    /// </summary>
    public OverloadStage Swell()
    {
        if (_stage == OverloadStage.Cracking)
        {
            _crackFramesDone++;

            // Growing as the cracks open, and the two are the same count so they cannot drift
            // apart: by the time he is at his widest he is also at his most broken.
            Scale = 1f + (CrackScale - 1f) * Split;

            if (_crackFramesDone >= CrackFrames)
            {
                _stage = OverloadStage.Discharging;
                Scale = CrackScale;
            }

            return _stage;
        }

        if (_stage == OverloadStage.Discharging)
        {
            _dischargeFramesDone++;
            Scale += DischargeStep;

            if (_dischargeFramesDone >= DischargeFrames)
            {
                _stage = OverloadStage.Exploded;
            }
        }

        return _stage;
    }

    /// <summary>
    ///     Back to his own size, ready for the next death.
    /// </summary>
    public void Reset()
    {
        _stage = OverloadStage.Cracking;
        _crackFramesDone = 0;
        _dischargeFramesDone = 0;
        Scale = 1f;
    }

    /// <summary>
    ///     The scales the flare is drawn at, smallest first - a stack of copies of him growing outward from
    ///     whatever size he currently is. The guard against a negative scale is kept though nothing can
    ///     now reach one: he used to shrink back through zero to be drawn inside out, and a copy at a
    ///     negative scale is a drawing mistake rather than a smaller one.
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
