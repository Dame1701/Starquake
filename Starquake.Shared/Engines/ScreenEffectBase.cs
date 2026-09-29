// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Starquake.Structure;

namespace Starquake.Engines;

public abstract class ScreenEffectBase
{
    protected int EffectDuration;
    protected int EffectSpeed;

    protected RenderViewPort EffectSurface;
    protected GameEngine GameEngine;

    public ScreenEffectBase(GameEngine gameEngineReference)
    {
        GameEngine = gameEngineReference;
    }
    public ScreenEffectType ScreenEffectType { get; protected set; }
    public bool EffectFinished { get; protected set; } = true;

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