// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Starquake.Core;

namespace Starquake.Engines.ScreenEffects;

internal abstract class ScreenEffectBase
{
    protected int EffectDuration;
    protected int EffectSpeed;

    protected RenderViewPort? EffectSurface;

    public ScreenEffectType ScreenEffectType { get; protected init; }
    public bool EffectFinished { get; protected set; } = true;

    /// <summary>
    ///     Whether Blob and the nasties should still be held still. An effect that says nothing holds
    ///     them for as long as it runs; one that can give control back before it has finished drawing
    ///     overrides this.
    /// </summary>
    public virtual bool HoldsGameplay => !EffectFinished;

    public virtual void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        EffectSpeed = effectSpeed;
        EffectDuration = effectDuration;
        EffectFinished = false;
        EffectSurface = effectSurface;
    }

    public abstract void Reset();

    public abstract void Update();

    /// <summary>Composes the playfield this frame.</summary>
    /// <returns>
    ///     True if the plain renderer should still draw the playfield as usual, which is what an
    ///     effect that only tints or shifts what is there wants. False if the effect drew the picture
    ///     itself.
    /// </returns>
    public virtual bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
        => true;
}
