// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     The blob squeezing through a tunnel into the next screen. The picture is squashed
///     horizontally until it is a slit and nothing is left, and then squashed back out from the
///     edge the blob arrives at on the other side - so it reads as being pulled through a gap
///     rather than as a wipe across it.
///     <para>
///         Which edge the picture collapses towards is the only difference between going left and
///         going right, so both directions share this and say nothing but which way they go. They
///         used to be two near-copies, which is how one of them ended up missing a line the other
///         had.
///     </para>
///     <para>
///         The three numbers below are the feel of it, and they are the ones to turn. What matters
///         is that the motion is not linear: the squeeze accelerates, so the picture is snatched
///         away rather than slid away; there is a beat in the dark, so the tunnel has a length; and
///         coming out is slower than going in, so the new screen settles rather than snapping.
///     </para>
/// </summary>
public abstract class TunnelEntryEffect : ScreenEffectBase
{
    /// <summary>
    ///     How long the screen stays black at the pinch - the time spent inside the tunnel. Without
    ///     this the picture turns around the instant it vanishes, and the two halves read as one
    ///     bounce instead of a passage.
    /// </summary>
    private const int FramesInTheDark = 7;

    /// <summary>
    ///     How much longer emerging takes than being squeezed. Equal times feel mechanical; arriving
    ///     more slowly than you left reads as settling into the new screen.
    /// </summary>
    private const double EmergeSlowdown = 1.4;

    /// <summary>
    ///     How hard the picture stretches before it is pulled through and after it lands, as a
    ///     fraction of the play area. Squashing alone reads as being crushed; stretching first, and
    ///     overshooting on the way back, reads as being accelerated - the trailing edge lags going
    ///     in, and the leading edge overshoots and settles coming out.
    ///     <para>
    ///         There is room for this. The play area is 1024 wide inside a 1280 composition, so
    ///         there are 128 pixels of margin either side of it; a tenth of the play area is 102,
    ///         which fills the margin without spilling into the letterbox borders. Raising it much
    ///         past a tenth will start drawing the game outside the picture.
    ///     </para>
    /// </summary>
    private const double Stretch = 1.70158;

    private int _emergeFrames;
    private int _frame;
    private int _squeezeFrames;

    /// <summary>
    ///     Whether the picture collapses towards the left edge as the blob leaves, and so expands
    ///     from the right edge as it arrives. The other direction is the mirror of it.
    /// </summary>
    protected abstract bool SqueezesTowardsTheLeftEdge { get; }

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, effectSpeed, -1);

        // Kept in terms of the speed the callers ask for, so the pace is still theirs to set: the
        // squeeze takes as long as it would have at that many pixels a frame, and the rest follows.
        _squeezeFrames = Math.Max(1, effectSurface.MasterWidth / Math.Max(1, effectSpeed));
        _emergeFrames = Math.Max(1, (int)(_squeezeFrames * EmergeSlowdown));
        _frame = 0;
    }

    public override void Update()
    {
        if (EffectSurface == null || EffectFinished)
        {
            return;
        }

        _frame++;

        var master = EffectSurface.MasterWidth;
        int width;
        bool pinnedToTheLeftEdge;

        if (_frame <= _squeezeFrames)
        {
            // Stretched, then snatched away. The trailing edge lags behind at first, which is what
            // makes it look pulled rather than shrunk, and the last few frames go quickest.
            var travelled = _frame / (double)_squeezeFrames;
            width = (int)(master * (1 - StretchedIn(travelled)));
            pinnedToTheLeftEdge = SqueezesTowardsTheLeftEdge;
        }
        else if (_frame <= _squeezeFrames + FramesInTheDark)
        {
            // Inside the tunnel. A zero width draws nothing, so the play area is simply black.
            width = 0;
            pinnedToTheLeftEdge = SqueezesTowardsTheLeftEdge;
        }
        else
        {
            var travelled = (_frame - _squeezeFrames - FramesInTheDark) / (double)_emergeFrames;

            if (travelled >= 1)
            {
                // Landed. Set both ends exactly rather than clamping one and leaving the other,
                // which is what left the play area sixteen pixels adrift before.
                EffectSurface.Width = master;
                EffectSurface.XPos = EffectSurface.MasterXPos;
                EffectFinished = true;

                return;
            }

            // Arrives fast, overshoots a little past full width, and settles back into it.
            width = (int)(master * StretchedOut(travelled));
            pinnedToTheLeftEdge = !SqueezesTowardsTheLeftEdge;
        }

        EffectSurface.Width = width;
        EffectSurface.XPos = pinnedToTheLeftEdge
            ? EffectSurface.MasterXPos
            : EffectSurface.MasterXPos + master - width;
    }

    public override void Reset()
    {
        EffectFinished = true;
        _frame = 0;
    }

    /// <summary>
    ///     Leaving. Goes negative early - which is what stretches the picture past full width - then
    ///     accelerates to exactly 1, so the picture is exactly gone on the last frame of the squeeze.
    /// </summary>
    private static double StretchedIn(double travelled)
        => (Stretch + 1) * travelled * travelled * travelled - Stretch * travelled * travelled;

    /// <summary>
    ///     Arriving. The mirror of <see cref="StretchedIn" />: exactly 0 on the first frame, rises
    ///     past 1 to overshoot, and comes back to exactly 1 on the last.
    /// </summary>
    private static double StretchedOut(double travelled)
    {
        var overshot = travelled - 1;

        return 1 + (Stretch + 1) * overshot * overshot * overshot + Stretch * overshot * overshot;
    }
}
