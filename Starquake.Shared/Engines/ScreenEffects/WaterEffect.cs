// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Structure;
using Starquake.Tuning;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     Olly's screen, flooded to just below the top of his head.
/// </summary>
/// <remarks>
///     Unlike every other effect here this one does not end on its own: it runs for as long as
///     Blob is on that screen and is stopped when he leaves. <see cref="HoldsGameplay" /> is false
///     for the same reason the teleport arrival's is - the water is somewhere to walk about in,
///     not something that happens to you.
/// </remarks>
internal sealed class WaterEffect : ScreenEffectBase
{
    private readonly PresentationSettings _presentation;

    private readonly ResourceManager _resources;

    private int _frame;

    public WaterEffect(PresentationSettings presentation, ResourceManager resources)
    {
        _presentation = presentation;
        _resources = resources;

        ScreenEffectType = ScreenEffectType.Water;
    }

    /// <inheritdoc />
    public override bool HoldsGameplay => false;

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, effectSpeed, effectDuration);

        _frame = 0;
    }

    /// <summary>
    ///     The water does not run down - it is simply there - so nothing here ends it. Leaving the
    ///     screen does, through <see cref="Reset" />.
    /// </summary>
    public override void Update() => _frame++;

    /// <inheritdoc />
    public override bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
    {
        Effect water = _resources.GetEffect(Assets.Shaders.Water);

        water.Parameters["Waterline"].SetValue(WaterSettings.WaterlineIn01);
        water.Parameters["OverFullDepth"].SetValue(WaterSettings.OverFullDepthIn01);
        water.Parameters["Phase"].SetValue(WaterSettings.PhaseAt(_frame));
        water.Parameters["Amplitude"].SetValue(WaterSettings.Amplitude);
        water.Parameters["Frequency"].SetValue(WaterSettings.Frequency);
        water.Parameters["WaterColour"].SetValue(WaterSettings.WaterColour);
        water.Parameters["Tint"].SetValue(WaterSettings.Tint);
        water.Parameters["AtTheSurface"].SetValue(WaterSettings.AtTheSurface);
        water.Parameters["BlueCast"].SetValue(WaterSettings.BlueCast);
        water.Parameters["SurfaceColour"].SetValue(WaterSettings.SurfaceColour);
        water.Parameters["SurfaceThickness"].SetValue(WaterSettings.SurfaceThickness);

        renderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            null, null, water, _presentation.Transform);

        renderer.Draw(
            renderViewPortToUse.RenderTarget,
            new Rectangle(
                renderViewPortToUse.XPos,
                renderViewPortToUse.YPos,
                renderViewPortToUse.Width,
                renderViewPortToUse.Height),
            Color.White);

        renderer.End();

        // The water drew the picture itself, so the plain renderer has nothing to add.
        return false;
    }

    public override void Reset() => EffectFinished = true;
}
