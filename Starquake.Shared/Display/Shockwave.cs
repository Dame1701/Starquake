// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Helpers;
using Starquake.Sprites;
using Starquake.Tuning;

namespace Starquake.Display;

/// <summary>
///     The ring a blast throws out, from the moment it goes off until it has run out of room.
///     Blob coming apart has one, and so does each of the two nasties that are machines.
/// </summary>
/// <remarks>
///     It owns no artwork. A single square sprite is drawn at the size the ring will reach and
///     <c>Shaders/Shockwave.fx</c> fills it - see <see cref="ShockwaveSettings" /> for the numbers
///     it is driven by. What this holds is where the blast was, how many frames ago, and the
///     attitude that blast's wobble was dealt.
///     <para>
///         Counted in logic frames and never in the draw. The game is drawn once per vertical
///         retrace, so a ring stepped on the drawn frame would travel faster on a better monitor -
///         the same trap <c>BlobWeapon</c>'s beam is kept out of.
///     </para>
/// </remarks>
internal sealed class Shockwave
{
    private readonly int _diameter;
    private readonly int _frames;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    private Point _centre;
    private int _frame;
    private bool _running;

    /// <summary>
    ///     Where this blast started the waves that run round its rim, in radians. Dealt fresh on
    ///     every trigger, so two rings going off together are not rippling in step.
    /// </summary>
    private float _attitude;

    /// <param name="diameter">How far across the ring reaches, in pixels.</param>
    /// <param name="frames">How many logic frames it takes to get there.</param>
    public Shockwave(ResourceManager resources, Random randomNumberGenerator, int diameter, int frames)
    {
        _resources = resources;
        _randomNumberGenerator = randomNumberGenerator;
        _diameter = diameter;
        _frames = frames;
    }

    /// <summary>
    ///     Something has gone off at a point. Restarts the ring from nothing wherever it had got
    ///     to, because a second blast is a second blast and not a continuation of the first.
    /// </summary>
    public void Trigger(int centreX, int centreY)
    {
        _centre = new Point(centreX, centreY);
        _frame = 0;
        _running = true;

        _attitude = (float)(_randomNumberGenerator.NextDouble() * 2 * Math.PI);
    }

    /// <summary>
    ///     Takes the ring off the screen at once. For a screen change, where a blast on the room
    ///     Blob has just left would otherwise finish travelling over the one he has walked onto.
    /// </summary>
    public void Stop() => _running = false;

    /// <summary>One logic frame of travel.</summary>
    public void Update()
    {
        if (!_running)
        {
            return;
        }

        _frame++;

        if (_frame >= _frames)
        {
            _running = false;
        }
    }

    /// <param name="screenFade">
    ///     How lit the room is. The blend is additive, so this reaches the ring as the tint the
    ///     shader's output is multiplied by.
    /// </param>
    public void Draw(int screenFade)
    {
        if (!_running)
        {
            return;
        }

        SpriteCollection quad = _resources.GetSpriteCollection(Assets.Graphics.ParticleSprite);

        float radius = ShockwaveSettings.RadiusAt(_frame, _frames);

        Effect blast = _resources.GetEffect(Assets.Shaders.Shockwave);

        // The quad's square in its sheet, as the one multiply and add the shader can afford
        // rather than the rectangle and the divide the other shaders are handed.
        Vector4 quadRect = quad.SpriteRectIn01(0);

        // Doubled, so the quad runs minus one to one across and the shader has one multiply
        // fewer to make the distance out with.
        var quadScale = new Vector2(2f / quadRect.Z, 2f / quadRect.W);

        blast.Parameters["QuadScale"].SetValue(quadScale);
        blast.Parameters["QuadBias"].SetValue(
            new Vector2(quadRect.X * quadScale.X + 1f, quadRect.Y * quadScale.Y + 1f));
        blast.Parameters["Radius"].SetValue(radius);
        blast.Parameters["OverFrontSoftness"].SetValue(1f / ShockwaveSettings.FrontSoftness);
        blast.Parameters["OverFront"].SetValue(ShockwaveSettings.OverFrontAt(radius));
        blast.Parameters["OverTail"].SetValue(ShockwaveSettings.OverTailAt(radius));
        blast.Parameters["WaveRate"].SetValue(ShockwaveSettings.WaveRateAt(radius));
        blast.Parameters["WavePhase"].SetValue(ShockwaveSettings.WavePhaseAt(_frame));
        blast.Parameters["TrailWeight"].SetValue(ShockwaveSettings.TrailWeight);
        blast.Parameters["ShimmerTurn"].SetValue(ShockwaveSettings.ShimmerTurnAt(_frame, _attitude));
        blast.Parameters["FineShimmerTurn"].SetValue(
            ShockwaveSettings.FineShimmerTurnAt(_frame, _attitude));
        blast.Parameters["EdgeColour"].SetValue(ShockwaveSettings.EdgeColour);
        blast.Parameters["ColourLift"].SetValue(ShockwaveSettings.ColourLift);
        blast.Parameters["Intensity"].SetValue(ShockwaveSettings.IntensityAt(_frame, _frames));
        blast.Parameters["OverRimFade"].SetValue(1f / ShockwaveSettings.RimFade);

        // The scaled draw turns about the sprite's own middle, so the corner is stepped back by
        // half a sprite to put that middle on the blast.
        quad.Begin(BlendState.Additive, null, blast);
        quad.DrawSprite(
            0,
            _centre.X - quad.SpriteWidth / 2,
            _centre.Y - quad.SpriteHeight / 2,
            ColourHelper.WhiteFadedTo(screenFade),
            _diameter / (float)quad.SpriteWidth,
            0f);
        quad.End();
    }
}
