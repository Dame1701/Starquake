// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Tuning;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     Arriving somewhere by teleport. The room comes in unsettled, with standing waves running
///     through it, and settles as they die away.
/// </summary>
/// <remarks>
///     Displaces where the picture is read from rather than drawing over it, so the room itself moves
///     and the picture it settles to is exactly the one that was always there.
///     <c>Shaders/TeleportArrival.fx</c> does the work.
///     <para>
///         It does not hold the game still while it runs. The tunnel entries have to, because Blob is
///         being moved through a wall, but a player who has just arrived wants to walk and a second
///         of being unable to is a second of the game having stopped.
///     </para>
/// </remarks>
internal sealed class TeleportArrivalEffect : ScreenEffectBase
{
    private readonly PresentationSettings _presentation;

    private readonly ResourceManager _resources;

    private int _framesRun;

    public TeleportArrivalEffect(PresentationSettings presentation, ResourceManager resources)
    {
        _presentation = presentation;
        _resources = resources;

        ScreenEffectType = ScreenEffectType.TeleportArrival;
    }

    /// <inheritdoc />
    public override bool HoldsGameplay => false;

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

        if (_framesRun >= TeleportArrivalSettings.Frames)
        {
            EffectFinished = true;
        }
    }

    /// <inheritdoc />
    public override bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
    {
        Effect shimmer = _resources.GetEffect(Assets.Shaders.TeleportArrival);

        shimmer.Parameters["Strength"].SetValue(TeleportArrivalSettings.StrengthAt(_framesRun));
        shimmer.Parameters["Phase"].SetValue(TeleportArrivalSettings.PhaseAt(_framesRun));
        shimmer.Parameters["Amplitude"].SetValue(TeleportArrivalSettings.Amplitude);
        shimmer.Parameters["Frequency"].SetValue(TeleportArrivalSettings.Frequency);
        shimmer.Parameters["Glimmer"].SetValue(TeleportArrivalSettings.Glimmer);
        shimmer.Parameters["ArrivalColour"].SetValue(TeleportArrivalSettings.ArrivalColour);

        renderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            null, null, shimmer, _presentation.Transform);

        renderer.Draw(
            renderViewPortToUse.RenderTarget,
            new Rectangle(
                renderViewPortToUse.XPos,
                renderViewPortToUse.YPos,
                renderViewPortToUse.Width,
                renderViewPortToUse.Height),
            Color.White);

        renderer.End();

        // The shimmer drew the picture itself, so the plain renderer has nothing to add.
        return false;
    }

    public override void Reset() => EffectFinished = true;
}
