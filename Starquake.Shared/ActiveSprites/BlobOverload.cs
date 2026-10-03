// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

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
    ///     How fast he swells and shrinks, and the two ends of it. Drawing at a negative scale is what
    ///     turns him inside out, which is why the minimum is below zero rather than at it.
    /// </summary>
    private const float ScaleStep = 0.30f;

    /// <inheritdoc cref="ScaleStep" />
    private const float MaxScale = 1.6f;

    /// <inheritdoc cref="ScaleStep" />
    private const float MinScale = -1f;

    /// <summary>
    ///     How long the alarm sounds before he goes, in logic frames - a second at fifty hertz.
    /// </summary>
    private const int AlarmFrames = 60;

    /// <summary>
    ///     The flare drawn as he explodes: how many copies of him, how much smaller than he is the first
    ///     one starts, and how much bigger each one after it is.
    /// </summary>
    private const int FlareCopies = 5;

    /// <inheritdoc cref="FlareCopies" />
    private const float FlareFirstScale = 0.4f;

    /// <inheritdoc cref="FlareCopies" />
    private const float FlareScaleStep = 0.2f;

    private int _alarmFramesSounded;

    private OverloadStage _stage;

    /// <summary>
    ///     How big he is drawn, which runs from one up to <see cref="MaxScale" /> and then down through
    ///     zero to <see cref="MinScale" />.
    /// </summary>
    public float Scale { get; private set; } = 1f;

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
        _stage = soundTheAlarmFirst ? OverloadStage.Swelling : OverloadStage.Exploded;
        _alarmFramesSounded = 0;
    }

    /// <summary>One more frame of the alarm.</summary>
    public void CountAnAlarmFrame() => _alarmFramesSounded++;

    /// <summary>
    ///     One frame of coming apart, and what stage that leaves him at. If he began already exploded
    ///     he stays there, which is what makes the explosion immediate.
    /// </summary>
    public OverloadStage Swell()
    {
        if (_stage == OverloadStage.Swelling)
        {
            Scale += ScaleStep;

            if (Scale > MaxScale)
            {
                _stage = OverloadStage.Shrinking;
                Scale = MaxScale;
            }
        }
        else if (_stage == OverloadStage.Shrinking)
        {
            Scale -= ScaleStep;

            if (Scale < MinScale)
            {
                Scale = MinScale;
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
        _stage = OverloadStage.Swelling;
        Scale = 1f;
    }

    /// <summary>
    ///     The scales the flare is drawn at, smallest first - a stack of copies of him growing outward from
    ///     whatever size he currently is. The ones that would come out inside out are left out rather than
    ///     drawn, which is what the original's test against zero did.
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
