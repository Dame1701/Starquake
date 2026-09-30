// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.ContentManagement;
using Starquake.Structure;

namespace Starquake.Particles;

public class TriggeredParticleEffect : IStandardParticleEffect
{
    private readonly GraphicsSet _graphicsSet;

    internal readonly ParticleEffect MercuryParticleEffect;
    internal readonly SpriteBatchRenderer Renderer;

    private bool _triggered;

    public TriggeredParticleEffect(GraphicsSet graphicsSet, ParticleEffect mercuryParticleEffect,
        SpriteBatchRenderer renderer)
    {
        _graphicsSet = graphicsSet;
        MercuryParticleEffect = mercuryParticleEffect;
        Renderer = renderer;
        EffectActive = false;
        _triggered = false;
    }
    public bool EffectActive { get; private set; }

    public ParticleEffect Effect => MercuryParticleEffect;

    public event RaiseEffectComplete? ParticleEffectComplete;

    // Starts an effect that is not triggered
    public void Start()
    {
        if (EffectActive)
        {
            // Kill the previous effect
            MercuryParticleEffect.Terminate();
            EffectActive = false;
            _triggered = false;
        }

        _triggered = false;
        EffectActive = true;
    }

    // Stops an effect playing
    public void Stop()
    {
        MercuryParticleEffect.Terminate();
        EffectActive = false;
        _triggered = false;
    }

    public void Reset()
    {
        EffectActive = false;
        MercuryParticleEffect.Terminate();
    }

    public void Update(int xPos, int yPos, GameTime gameTime)
    {
        if (EffectActive)
        {
            if (!_triggered)
            {
                var position = new Vector2(xPos, yPos);
                MercuryParticleEffect.Trigger(ref position);

                // Workaround for a bizzare bug that seems to stop the particle from working
                // unless we trigger until we get an active partical count.....
                if (MercuryParticleEffect.ActiveParticlesCount > 0)
                {
                    _triggered = true;
                }
            }
            else
            {
                if (MercuryParticleEffect.ActiveParticlesCount == 0)
                {
                    _triggered = false;
                    EffectActive = false;
                    MercuryParticleEffect.Terminate();

                    if (ParticleEffectComplete != null)
                    {
                        ParticleEffectComplete();
                    }
                }
            }

        }

        // Mercury must always be updated
        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MercuryParticleEffect.Update(deltaSeconds);
    }

    public void Draw()
    {
        if (_graphicsSet.IsRedrawn)
        {
            // We must always call this!
            Renderer.RenderEffect(MercuryParticleEffect);
        }
    }

    public void Draw(int screenFade)
    {
        if (_graphicsSet.IsRedrawn)
        {
            Renderer.RenderEffect(MercuryParticleEffect, screenFade);
        }
    }
}
