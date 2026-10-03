// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Display;
using Starquake.Helpers;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     Blob squeezing through a tunnel into the next screen. The screen he is leaving is squashed
///     horizontally towards the edge he leaves by until nothing is left, while the screen he is
///     arriving at opens from the opposite edge - so for a moment both are on screen as slivers
///     with the dark of the tunnel between them, and it reads as being pulled through a gap rather
///     than as a wipe across one.
///     <para>
///         Which edge the picture collapses towards is the only difference between going left and
///         going right, so both directions share this and say nothing but which way they go. They
///         used to be two near-copies, which is how one of them ended up missing a line the other
///         had.
///     </para>
///     <para>
///         The numbers below are the feel of it, and they are the ones to turn. What matters is
///         that the motion is not linear: the squeeze accelerates, so the picture is snatched away
///         rather than slid away; there is a beat where little but the tunnel is showing, so it has
///         a length; and coming out is slower than going in, so the new screen settles rather than
///         snapping.
///     </para>
/// </summary>
/// <remarks>
///     The outgoing screen is drawn from a <see cref="ScreenSnapshot" /> rather than from the view
///     port, because by the time this is drawing, the game has already moved on: <c>Blob</c> steps
///     the screen number, repositions himself at the far tunnel mouth and only then starts the
///     effect. Before the snapshot existed the squeeze was showing the destination being snatched
///     away, so the same screen arrived twice - about a third of a second of the wrong picture,
///     and the opposite of what the first paragraph above has always claimed.
/// </remarks>
internal abstract class TunnelEntryEffect(ScreenSnapshot outgoingScreen, PresentationSettings presentation)
    : ScreenEffectBase
{
    /// <summary>
    ///     How long the arrival is held at <see cref="SliverOfLight" /> before it opens out - the
    ///     time spent inside the tunnel. Without it the picture turns around the instant it
    ///     vanishes, and the two halves read as one bounce instead of a passage.
    /// </summary>
    private const int FramesInTheDark = 7;

    /// <summary>
    ///     How much longer emerging takes than being squeezed. Equal times feel mechanical; arriving
    ///     more slowly than you left reads as settling into the new screen.
    /// </summary>
    private const double EmergeSlowdown = 1.4;

    /// <summary>
    ///     How much of the squeeze the arriving screen is already showing for, as a fraction of it.
    ///     This is the overlap that makes the transition a merge rather than a swap: for these last
    ///     few frames the screen being left and the screen being entered are both on display, each
    ///     a sliver against its own edge, with the tunnel dark between them.
    ///     <para>
    ///         It wants to stay small. The squeeze is heavily back-loaded - the picture is still at
    ///         full width three fifths of the way through it - so anything much past a fifth starts
    ///         the arrival while the screen being left is still large, and the two read as two
    ///         pictures rather than as one passage.
    ///     </para>
    /// </summary>
    private const double OverlapOfTheSqueeze = 0.35;

    /// <summary>
    ///     How wide the arriving screen sits while the tunnel is being crossed, as a fraction of the
    ///     play area - the light at the end of it. A true black here would be a cut; a sliver of the
    ///     place being arrived at is what gives the beat somewhere to go.
    /// </summary>
    private const double SliverOfLight = 0.12;

    /// <summary>
    ///     How far each picture's inner edge fades out over, as a fraction of the play area, and so
    ///     also how far it is drawn past where it ends. It does both jobs because they are the same
    ///     job: the band that fades is the band that reaches across, so where the two are close
    ///     enough their fades sit on top of each other and one dissolves into the other, and where
    ///     they are not they melt into the dark of the tunnel instead of stopping at a hard line.
    ///     <para>
    ///         A hard edge is what made the two read as two pictures. Raising this makes them bleed
    ///         into each other for longer; lowering it back towards nothing puts the cut back.
    ///     </para>
    /// </summary>
    private const double BleedOfTheSeam = 0.16;

    /// <summary>
    ///     How many strips the fading band is drawn in. <c>SpriteBatch</c> tints a whole quad at
    ///     once, so a gradient has to be several quads; eight is enough that the steps are not
    ///     visible at this width and few enough to be free.
    /// </summary>
    private const int BleedStrips = 8;

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

    /// <summary>
    ///     How far into the arrival Blob gets control back, as a fraction of it. He is let go
    ///     before the transition has finished, so the last of the overshoot settles while he is
    ///     already walking - but not while the picture is still narrow, because the play area is
    ///     drawn into a rectangle of the current width and his movement would be squashed along with
    ///     it. At two fifths the picture is back to within a per cent of full width, which is the
    ///     earliest it reads honestly.
    /// </summary>
    private const double HeldThroughArrival = 0.4;

    private int _emergeFrames;
    private int _emergeStart;
    private int _frame;
    private int _incomingWidth;
    private int _outgoingWidth;
    private int _releaseFrame;
    private int _sliverStart;
    private int _squeezeFrames;

    /// <inheritdoc />
    public override bool HoldsGameplay => !EffectFinished && _frame < _releaseFrame;

    /// <summary>
    ///     Whether the picture collapses towards the left edge as Blob leaves, and so expands
    ///     from the right edge as it arrives. The other direction is the mirror of it.
    /// </summary>
    protected abstract bool SqueezesTowardsTheLeftEdge { get; }

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, effectSpeed, -1);

        // Before anything has drawn this frame, so what the view port is holding is the screen
        // being left rather than the one already stepped to.
        outgoingScreen.Capture(effectSurface.RenderTarget);

        // Kept in terms of the speed the callers ask for, so the pace is still theirs to set: the
        // squeeze takes as long as it would have at that many pixels a frame, and the rest follows.
        _squeezeFrames = Math.Max(1, effectSurface.MasterWidth / Math.Max(1, EffectSpeed));
        _emergeFrames = Math.Max(1, (int)(_squeezeFrames * EmergeSlowdown));
        _sliverStart = _squeezeFrames - (int)(_squeezeFrames * OverlapOfTheSqueeze);
        _emergeStart = _squeezeFrames + FramesInTheDark;
        _releaseFrame = _emergeStart + (int)(_emergeFrames * HeldThroughArrival);
        _frame = 0;

        _outgoingWidth = effectSurface.MasterWidth;
        _incomingWidth = 0;
    }

    public override void Update()
    {
        if (EffectSurface == null || EffectFinished)
        {
            return;
        }

        _frame++;

        int master = EffectSurface.MasterWidth;

        _outgoingWidth = WidthOfTheScreenBeingLeft(master);
        _incomingWidth = WidthOfTheScreenBeingEntered(master);

        if (_frame > _emergeStart + _emergeFrames)
        {
            // Landed. Set both ends exactly rather than clamping one and leaving the other,
            // which is what left the play area sixteen pixels adrift before.
            _incomingWidth = master;
            EffectSurface.Width = master;
            EffectSurface.XPos = EffectSurface.MasterXPos;
            EffectFinished = true;

            return;
        }

        // The view port still describes the live picture, which is the one arriving, so anything
        // reading its width while the transition runs sees what is actually being drawn into.
        EffectSurface.Width = _incomingWidth;
        EffectSurface.XPos = EdgeOf(EffectSurface, _incomingWidth, !SqueezesTowardsTheLeftEdge);
    }

    /// <summary>
    ///     Draws both screens: the one being left, from the snapshot, against the edge it is
    ///     collapsing towards, and the one being entered, live, against the other. They never
    ///     overlap - the gap between them is the tunnel - so the back buffer's clear colour shows
    ///     through between them and no blending is needed.
    /// </summary>
    public override bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
    {
        var bleed = (int)(renderViewPortToUse.MasterWidth * BleedOfTheSeam);

        renderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null,
            presentation.Transform);

        if (outgoingScreen.Picture is { } leaving && _outgoingWidth > 0)
        {
            DrawBleedingInwards(renderer, leaving,
                PlacedAgainst(renderViewPortToUse, _outgoingWidth, SqueezesTowardsTheLeftEdge),
                bleed, SqueezesTowardsTheLeftEdge);
        }

        if (_incomingWidth > 0)
        {
            DrawBleedingInwards(renderer, renderViewPortToUse.RenderTarget,
                PlacedAgainst(renderViewPortToUse, _incomingWidth, !SqueezesTowardsTheLeftEdge),
                bleed, !SqueezesTowardsTheLeftEdge);
        }

        renderer.End();

        // Both halves are drawn, so there is nothing for the default renderer to do.
        return false;
    }

    public override void Reset()
    {
        EffectFinished = true;
        _frame = 0;
        _outgoingWidth = 0;
        _incomingWidth = 0;
    }

    /// <summary>
    ///     Stretched, then snatched away. The trailing edge lags behind at first, which is what
    ///     makes it look pulled rather than shrunk, and the last few frames go quickest.
    /// </summary>
    private int WidthOfTheScreenBeingLeft(int master)
    {
        if (_frame > _squeezeFrames)
        {
            return 0;
        }

        return (int)(master * (1 - StretchedIn(_frame / (double)_squeezeFrames)));
    }

    /// <summary>
    ///     A sliver from before the screen being left has finished going, held across the tunnel,
    ///     then opened out fast, past full width, and settled back into it.
    /// </summary>
    private int WidthOfTheScreenBeingEntered(int master)
    {
        if (_frame < _sliverStart)
        {
            return 0;
        }

        var sliver = (int)(master * SliverOfLight);

        if (_frame <= _emergeStart)
        {
            return sliver;
        }

        var travelled = (_frame - _emergeStart) / (double)_emergeFrames;

        return Math.Max(sliver, (int)(master * StretchedOut(travelled)));
    }

    /// <summary>
    ///     Draws <paramref name="picture" /> across <paramref name="area" />, reaching
    ///     <paramref name="bleed" /> pixels further towards the middle of the screen than the
    ///     picture is wide and fading away across that reach.
    /// </summary>
    /// <remarks>
    ///     The reach is more of the picture rather than its edge column smeared outwards, so the
    ///     band that fades is real content and the two screens dissolve into one another where
    ///     their bands meet.
    /// </remarks>
    /// <param name="pinnedToTheLeftEdge">
    ///     Which edge the picture is held against, and so which of its edges is the one facing in.
    /// </param>
    private static void DrawBleedingInwards(SpriteBatch renderer, Texture2D picture, Rectangle area, int bleed,
        bool pinnedToTheLeftEdge)
    {
        var reached = pinnedToTheLeftEdge
            ? area with { Width = area.Width + bleed }
            : area with { X = area.X - bleed, Width = area.Width + bleed };

        var solid = reached.Width - bleed;

        DrawSlice(renderer, picture, reached, pinnedToTheLeftEdge ? reached.Left : reached.Left + bleed, solid,
            ColourLevels.Full);

        var strip = Math.Max(1, bleed / BleedStrips);

        for (int step = 0; step < BleedStrips; step++)
        {
            var into = step * strip;

            var x = pinnedToTheLeftEdge
                ? reached.Right - bleed + into
                : reached.Left + bleed - into - strip;

            // Falls off across the band, so the far end of it is all but gone.
            var fade = (int)(ColourLevels.Full * (1 - ((into + (strip / 2.0)) / bleed)));

            DrawSlice(renderer, picture, reached, x, strip, fade);
        }
    }

    /// <summary>
    ///     One vertical slice of a picture, taking the part of the picture that belongs where the
    ///     slice lands so the slices line up into the whole.
    /// </summary>
    private static void DrawSlice(SpriteBatch renderer, Texture2D picture, Rectangle across, int x, int width,
        int fade)
    {
        if (width <= 0)
        {
            return;
        }

        var sourceX = (int)((x - across.X) / (double)across.Width * picture.Width);
        var sourceWidth = (int)(width / (double)across.Width * picture.Width);

        var source = new Rectangle(
            Math.Clamp(sourceX, 0, picture.Width - 1),
            0,
            Math.Clamp(sourceWidth, 1, picture.Width - Math.Clamp(sourceX, 0, picture.Width - 1)),
            picture.Height);

        renderer.Draw(picture, new Rectangle(x, across.Y, width, across.Height), source,
            ColourHelper.WhiteFadedTo(fade));
    }

    private static Rectangle PlacedAgainst(RenderViewPort viewPort, int width, bool theLeftEdge)
        => new(EdgeOf(viewPort, width, theLeftEdge), viewPort.YPos, width, viewPort.Height);

    private static int EdgeOf(RenderViewPort viewPort, int width, bool theLeftEdge)
        => theLeftEdge
            ? viewPort.MasterXPos
            : viewPort.MasterXPos + viewPort.MasterWidth - width;

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
