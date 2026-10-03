// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     Blob squeezing through a tunnel into the next screen. The screen he is leaving is squashed
///     towards the edge he leaves by while the screen he is arriving at swells from the opposite
///     edge, the two meeting at a seam that is never a line - each bleeds into the other across it,
///     so one picture becomes the other without ever cutting.
///     <para>
///         Which edge the picture collapses towards is the only difference between going left and
///         going right, so both directions share this and say nothing but which way they go. They
///         used to be two near-copies, which is how one of them ended up missing a line the other
///         had.
///     </para>
///     <para>
///         The numbers below are the feel of it, and they are the ones to turn. What matters is
///         that the motion is one unbroken move: the seam is driven by a single eased clock, so
///         there is no frame where anything is held still. It starts by stretching the screen being
///         left past full width, whips across, overshoots and settles - and leaving is quicker than
///         arriving, so the new screen beds in rather than snapping into place.
///     </para>
/// </summary>
/// <remarks>
///     The outgoing screen is drawn from a <see cref="ScreenSnapshot" /> rather than from the view
///     port, because by the time this is drawing, the game has already moved on: <c>Blob</c> steps
///     the screen number, repositions himself at the far tunnel mouth and only then starts the
///     effect. Before the snapshot existed the squeeze was showing the destination being snatched
///     away, so the same screen arrived twice.
/// </remarks>
internal abstract class TunnelEntryEffect(
    ScreenSnapshot outgoingScreen,
    PresentationSettings presentation,
    ResourceManager resources)
    : ScreenEffectBase
{
    /// <summary>
    ///     How far the colour drains out as Blob crosses, where one is all of it and nought is
    ///     none. A tunnel is a dark place to be inside and there is nothing to light it, so the
    ///     picture loses its colour on the way through and gets it back on the other side.
    /// </summary>
    /// <remarks>
    ///     This is what the dark gap between the two screens was trying to be and was not. A gap
    ///     reads as a hole punched between two pictures; draining them reads as the pictures
    ///     themselves being somewhere darker, which is the thing Blob is actually doing.
    /// </remarks>
    private const double ColourDrain = 1.0;

    /// <summary>
    ///     How far the light goes with the colour, where one would be pitch black and nought
    ///     leaves the grey as bright as the picture was. Draining the colour alone gives a flat
    ///     mid grey that reads as a photograph rather than as a dark place; taking most of the
    ///     light with it is what makes it read as a tunnel.
    /// </summary>
    private const double LightDrain = 0.78;

    /// <summary>
    ///     How much the drain lingers at its deepest, as the power its curve is raised to. One
    ///     leaves the curve as it is; below one flattens the top so the picture stays down across
    ///     the middle of the crossing rather than touching bottom and turning straight round.
    /// </summary>
    private const double DrainSpread = 0.75;

    /// <summary>
    ///     How much longer arriving takes than leaving. Equal halves feel mechanical; coming in
    ///     more slowly than you went out reads as settling into the new screen.
    ///     <para>
    ///         This is where the seam reaches the middle rather than two separate timings - the
    ///         whole transition is one clock now, and this is the point along it at which the two
    ///         screens are the same width.
    ///     </para>
    /// </summary>
    private const double ArriveSlowdown = 1.4;

    /// <summary>
    ///     How far each picture's inner edge fades out over, as a fraction of the play area. A hard
    ///     edge is what made the two screens read as two pictures; fading the edge each turns
    ///     towards the other softens the join into a band rather than a line.
    ///     <para>
    ///         The band is inside the picture rather than beyond it. Reaching past the edge meant
    ///         drawing the picture into a rectangle wider than itself, which stretched it sideways
    ///         by a sixth for the whole transition and then snapped it back the moment the effect
    ///         handed over - see <see cref="BleedNow" />.
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
    ///     overshooting on the way back, reads as being accelerated - the screen being left lags
    ///     before it goes, and the one arriving overshoots and settles.
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
    ///     drawn into a rectangle of the current width and his movement would be squashed along
    ///     with it. At two fifths of the arrival the picture is back to full width, which is the
    ///     earliest it reads honestly.
    /// </summary>
    private const double HeldThroughArrival = 0.4;

    private int _frame;
    private int _incomingWidth;
    private int _outgoingWidth;
    private int _releaseFrame;

    /// <summary>
    ///     Where along the transition the two screens are the same width - the moment the seam is
    ///     in the middle. Before it the screen being left is the larger.
    /// </summary>
    private double _seamReachesTheMiddle;

    private int _transitionFrames;

    /// <inheritdoc />
    public override bool HoldsGameplay => !EffectFinished && _frame < _releaseFrame;

    /// <summary>
    ///     Whether the picture collapses towards the left edge as Blob leaves, and so the next one
    ///     swells from the right edge as it arrives. The other direction is the mirror of it.
    /// </summary>
    protected abstract bool SqueezesTowardsTheLeftEdge { get; }

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, effectSpeed, -1);

        // Before anything has drawn this frame, so what the view port is holding is the screen
        // being left rather than the one already stepped to.
        outgoingScreen.Capture(effectSurface.RenderTarget);

        // Kept in terms of the speed the callers ask for, so the pace is still theirs to set: the
        // leaving half takes as long as it would have at that many pixels a frame, and the arriving
        // half is the slower one.
        var leavingFrames = Math.Max(1, effectSurface.MasterWidth / Math.Max(1, EffectSpeed));
        var arrivingFrames = Math.Max(1, (int)(leavingFrames * ArriveSlowdown));

        _transitionFrames = leavingFrames + arrivingFrames;
        _seamReachesTheMiddle = leavingFrames / (double)_transitionFrames;
        _releaseFrame = (int)(_transitionFrames *
                              (_seamReachesTheMiddle + ((1 - _seamReachesTheMiddle) * HeldThroughArrival)));
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

        var master = EffectSurface.MasterWidth;

        if (_frame >= _transitionFrames)
        {
            // Landed. Set both ends exactly rather than clamping one and leaving the other,
            // which is what left the play area sixteen pixels adrift before.
            _outgoingWidth = 0;
            _incomingWidth = master;
            EffectSurface.Width = master;
            EffectSurface.XPos = EffectSurface.MasterXPos;
            EffectFinished = true;

            return;
        }

        // One clock, two complementary widths: whatever the screen being left gives up, the one
        // arriving takes. There is no frame in which neither is moving, which is what makes the
        // transition read as a single move rather than as two halves with a wait between them.
        var crossedOver = SeamCrossing(_frame / (double)_transitionFrames);

        _outgoingWidth = Math.Max(0, (int)(master * (1 - crossedOver)));
        _incomingWidth = Math.Max(0, (int)(master * crossedOver));

        // The view port still describes the live picture, which is the one arriving, so anything
        // reading its width while the transition runs sees what is actually being drawn into.
        EffectSurface.Width = _incomingWidth;
        EffectSurface.XPos = EdgeOf(EffectSurface, _incomingWidth, !SqueezesTowardsTheLeftEdge);
    }

    /// <summary>
    ///     Draws both screens: the one being left, from the snapshot, against the edge it is
    ///     collapsing towards, and the one being entered, live, against the other. Each reaches
    ///     past the other's edge and fades across the reach, so the join between them is a band
    ///     that mixes rather than a line.
    /// </summary>
    public override bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
    {
        var bleed = BleedNow(renderViewPortToUse.MasterWidth);

        // One shader over the whole batch, so both screens drain together and the seam between
        // them does not have a colour or a brightness boundary running down it.
        var drained = DrainedBy();

        Effect desaturate = resources.GetEffect(Assets.Shaders.Desaturate);
        desaturate.Parameters["Saturation"].SetValue((float)(1 - (ColourDrain * drained)));
        desaturate.Parameters["Brightness"].SetValue((float)(1 - (LightDrain * drained)));

        renderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, desaturate,
            presentation.Transform);

        if (outgoingScreen.Picture is { } leaving && _outgoingWidth > 0)
        {
            DrawFadingInwards(renderer, leaving,
                PlacedAgainst(renderViewPortToUse, _outgoingWidth, SqueezesTowardsTheLeftEdge),
                bleed, SqueezesTowardsTheLeftEdge);
        }

        if (_incomingWidth > 0)
        {
            DrawFadingInwards(renderer, renderViewPortToUse.RenderTarget,
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
    ///     How much of the way the seam has crossed the screen, from nought at the start to one at
    ///     the end. Nought means the screen being left fills the play area and the one arriving has
    ///     no width at all; a half means they share it.
    /// </summary>
    /// <remarks>
    ///     Two back eases laid end to end at <see cref="_seamReachesTheMiddle" />, which is what
    ///     makes the move continuous and still asymmetric. It leaves the range deliberately at both
    ///     ends - below nought early, so the screen being left is stretched past full width before
    ///     it goes, and above one late, so the one arriving overshoots and comes back. Both
    ///     spill into the margin either side of the play area rather than off the picture.
    /// </remarks>
    private double SeamCrossing(double throughTheTransition)
        => throughTheTransition < _seamReachesTheMiddle
            ? 0.5 * StretchedIn(throughTheTransition / _seamReachesTheMiddle)
            : 0.5 + (0.5 * StretchedOut((throughTheTransition - _seamReachesTheMiddle) /
                                        (1 - _seamReachesTheMiddle)));

    /// <summary>
    ///     How far into the tunnel the picture is, from nought at both ends of the transition to
    ///     one in the middle of it. Both the colour and the light are taken down by this.
    /// </summary>
    /// <remarks>
    ///     Driven off plain frame count rather than off <see cref="SeamCrossing" />. The seam's
    ///     ease is heavily back-loaded by design - it sits near the edge for the first third and
    ///     then whips across - so a drain following it was full colour for ten frames and then
    ///     grey four frames later, which read as a flicker rather than as going somewhere. Against
    ///     the frame count it drains and recovers over the whole crossing instead.
    /// </remarks>
    private double DrainedBy()
    {
        if (EffectSurface == null || EffectFinished)
        {
            return 0;
        }

        var through = Math.Clamp(_frame / (double)_transitionFrames, 0, 1);

        // A raised cosine rather than a parabola. Both peak in the middle, but a parabola leaves
        // nought at a slope - at 38 frames its first frame is already a third drained, which is a
        // jump rather than a fade - while this one leaves and arrives flat, so the drain starts
        // and finishes without a step.
        var raisedCosine = (1 - Math.Cos(2 * Math.PI * through)) / 2;

        return Math.Pow(raisedCosine, DrainSpread);
    }

    /// <summary>
    ///     How wide the softening band is this frame. Widest in the middle of the crossing, where
    ///     there is a seam to soften, and nothing at either end of it.
    /// </summary>
    /// <remarks>
    ///     Tapering it is what makes the hand-over invisible. On its last frame this effect draws
    ///     the arriving screen and on the next frame the engine's own renderer draws the same
    ///     screen, flat and full width; anything this is still doing to the picture at that moment
    ///     is a step. At nought bleed the two draw the same thing, so there is nothing to see.
    /// </remarks>
    private int BleedNow(int master)
        => (int)(master * BleedOfTheSeam * DrainedBy());

    /// <summary>
    ///     Draws <paramref name="picture" /> across <paramref name="area" /> with the edge facing
    ///     the middle of the screen fading away over the last <paramref name="bleed" /> pixels of
    ///     it.
    /// </summary>
    /// <remarks>
    ///     The picture maps onto <paramref name="area" /> exactly, so it is never stretched - the
    ///     fade is taken out of the picture's own width rather than added beyond it.
    /// </remarks>
    /// <param name="pinnedToTheLeftEdge">
    ///     Which edge the picture is held against, and so which of its edges is the one facing in.
    /// </param>
    private static void DrawFadingInwards(SpriteBatch renderer, Texture2D picture, Rectangle area, int bleed,
        bool pinnedToTheLeftEdge)
    {
        bleed = Math.Min(bleed, area.Width);

        var solid = area.Width - bleed;

        DrawSlice(renderer, picture, area, pinnedToTheLeftEdge ? area.Left : area.Left + bleed, solid,
            ColourLevels.Full);

        if (bleed <= 0)
        {
            return;
        }

        var strip = Math.Max(1, bleed / BleedStrips);

        for (int step = 0; step * strip < bleed; step++)
        {
            var into = step * strip;

            var x = pinnedToTheLeftEdge
                ? area.Right - bleed + into
                : area.Left + bleed - into - strip;

            // Falls off across the band, so the edge itself is all but gone.
            var fade = (int)(ColourLevels.Full * (1 - ((into + (strip / 2.0)) / bleed)));

            DrawSlice(renderer, picture, area, x, strip, fade);
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

        var sourceX = Math.Clamp((int)((x - across.X) / (double)across.Width * picture.Width), 0,
            picture.Width - 1);
        var sourceWidth = (int)(width / (double)across.Width * picture.Width);

        var source = new Rectangle(sourceX, 0, Math.Clamp(sourceWidth, 1, picture.Width - sourceX),
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
    ///     accelerates to exactly 1.
    /// </summary>
    private static double StretchedIn(double travelled)
        => ((Stretch + 1) * travelled * travelled * travelled) - (Stretch * travelled * travelled);

    /// <summary>
    ///     Arriving. The mirror of <see cref="StretchedIn" />: exactly 0 at the start, rises past 1
    ///     to overshoot, and comes back to exactly 1 at the end.
    /// </summary>
    private static double StretchedOut(double travelled)
    {
        var overshot = travelled - 1;

        return 1 + ((Stretch + 1) * overshot * overshot * overshot) + (Stretch * overshot * overshot);
    }
}
