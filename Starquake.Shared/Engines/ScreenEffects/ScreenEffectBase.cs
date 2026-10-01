// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

internal abstract class ScreenEffectBase
{
    protected int EffectDuration;
    protected int EffectSpeed;

    protected RenderViewPort? EffectSurface;

    public ScreenEffectType ScreenEffectType { get; protected init; }
    public bool EffectFinished { get; protected set; } = true;

    /// <summary>
    ///     Whether the blob and the nasties should still be held still. An effect that says nothing
    ///     holds them for as long as it runs, which is what they all used to do; one that can afford
    ///     to give control back before it has finished drawing says so by overriding this.
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

    // If this returns true the we want to use the default renderer
    public virtual bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
        => true;
}
