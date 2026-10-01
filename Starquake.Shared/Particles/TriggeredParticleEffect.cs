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

internal class TriggeredParticleEffect : IStandardParticleEffect
{
    private readonly GraphicsSet _graphicsSet;

    private readonly ParticleEffect _mercuryParticleEffect;
    private readonly SpriteBatchRenderer _renderer;

    private bool _triggered;

    public TriggeredParticleEffect(GraphicsSet graphicsSet, ParticleEffect mercuryParticleEffect,
        SpriteBatchRenderer renderer)
    {
        _graphicsSet = graphicsSet;
        _mercuryParticleEffect = mercuryParticleEffect;
        _renderer = renderer;
        EffectActive = false;
        _triggered = false;
    }
    public bool EffectActive { get; private set; }

    public ParticleEffect Effect => _mercuryParticleEffect;

    public event RaiseEffectComplete? ParticleEffectComplete;

    // Starts an effect that is not triggered
    public void Start()
    {
        if (EffectActive)
        {
            // Kill the previous effect
            _mercuryParticleEffect.Terminate();
            EffectActive = false;
            _triggered = false;
        }

        _triggered = false;
        EffectActive = true;
    }

    // Stops an effect playing
    public void Stop()
    {
        _mercuryParticleEffect.Terminate();
        EffectActive = false;
        _triggered = false;
    }

    public void Reset()
    {
        EffectActive = false;
        _mercuryParticleEffect.Terminate();
    }

    public void Update(int xPos, int yPos, GameTime gameTime)
    {
        if (EffectActive)
        {
            if (!_triggered)
            {
                var position = new Vector2(xPos, yPos);
                _mercuryParticleEffect.Trigger(ref position);

                // Workaround for a bizarre bug that seems to stop the particle from working
                // unless we trigger until we get an active particle count.....
                if (_mercuryParticleEffect.ActiveParticlesCount > 0)
                {
                    _triggered = true;
                }
            }
            else
            {
                if (_mercuryParticleEffect.ActiveParticlesCount == 0)
                {
                    _triggered = false;
                    EffectActive = false;
                    _mercuryParticleEffect.Terminate();

                    if (ParticleEffectComplete != null)
                    {
                        ParticleEffectComplete();
                    }
                }
            }

        }

        // Mercury must always be updated
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mercuryParticleEffect.Update(deltaSeconds);
    }

    public void Draw()
    {
        if (_graphicsSet.IsRedrawn)
        {
            // We must always call this!
            _renderer.RenderEffect(_mercuryParticleEffect);
        }
    }

    public void Draw(int screenFade)
    {
        if (_graphicsSet.IsRedrawn)
        {
            _renderer.RenderEffect(_mercuryParticleEffect, screenFade);
        }
    }
}
