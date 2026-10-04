// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     A part landing in the planet's core. The room is wrung round the slot it went into and
///     springs back, with the core's own light thrown out through it.
/// </summary>
/// <remarks>
///     Like <see cref="TeleportArrivalEffect" /> it does not hold the game still - the core room
///     carries on around it and Blob is free to walk - and like it the picture is drawn by the
///     shader rather than tinted afterwards. <c>Shaders/CoreSurge.fx</c> does the work.
/// </remarks>
internal sealed class CoreSurgeEffect : ScreenEffectBase
{
    private readonly PresentationSettings _presentation;

    private readonly ResourceManager _resources;

    private int _framesRun;

    /// <summary>
    ///     Where in the room the part went in, as a fraction of the playfield. Defaults to the
    ///     middle, which is where the core is, so an effect triggered without one being set still
    ///     wrings about something sensible.
    /// </summary>
    private Vector2 _slot = new(0.5f, 0.5f);

    public CoreSurgeEffect(PresentationSettings presentation, ResourceManager resources)
    {
        _presentation = presentation;
        _resources = resources;

        ScreenEffectType = ScreenEffectType.CoreSurge;
    }

    /// <inheritdoc />
    public override bool HoldsGameplay => false;

    /// <summary>
    ///     Says where the next surge should be centred, before it is triggered.
    /// </summary>
    public void WringAbout(Vector2 slotInPlayfield) => _slot = slotInPlayfield;

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, effectSpeed, effectDuration);

        _framesRun = 0;
    }

    public override void Update()
    {
        if (EffectFinished)
        {
            return;
        }

        _framesRun++;

        if (_framesRun >= CoreReplacementSettings.SurgeFrames)
        {
            EffectFinished = true;
        }
    }

    /// <inheritdoc />
    public override bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
    {
        Effect surge = _resources.GetEffect(Assets.Shaders.CoreSurge);

        surge.Parameters["Strength"].SetValue(
            CoreReplacementSettings.SurgeStrengthAt(_framesRun));
        surge.Parameters["Phase"].SetValue(CoreReplacementSettings.RippleAt(_framesRun));
        surge.Parameters["Centre"].SetValue(_slot);
        surge.Parameters["OverReach"].SetValue(1f / CoreReplacementSettings.Reach);
        surge.Parameters["Twist"].SetValue(CoreReplacementSettings.Twist);
        surge.Parameters["Ripples"].SetValue(CoreReplacementSettings.Ripples);
        surge.Parameters["SurgeColour"].SetValue(CoreReplacementSettings.SurgeColour);
        surge.Parameters["Surge"].SetValue(CoreReplacementSettings.Surge);

        renderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            null, null, surge, _presentation.Transform);

        renderer.Draw(
            renderViewPortToUse.RenderTarget,
            new Rectangle(
                renderViewPortToUse.XPos,
                renderViewPortToUse.YPos,
                renderViewPortToUse.Width,
                renderViewPortToUse.Height),
            Color.White);

        renderer.End();

        // The surge drew the picture itself, so the plain renderer has nothing to add.
        return false;
    }

    public override void Reset() => EffectFinished = true;
}
