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
    private readonly MercuryEffect _mercury;

    private bool _triggered;

    public TriggeredParticleEffect(GraphicsSet graphicsSet, ParticleEffect mercuryParticleEffect,
        SpriteBatchRenderer renderer)
    {
        _mercury = new MercuryEffect(graphicsSet, mercuryParticleEffect, renderer);
        EffectActive = false;
        _triggered = false;
    }

    public bool EffectActive { get; private set; }

    public ParticleEffect Effect => _mercury.Effect;

    public event RaiseEffectComplete? ParticleEffectComplete;

    // Starts an effect that is not triggered
    public void Start()
    {
        if (EffectActive)
        {
            // Kill the previous effect
            _mercury.Terminate();
            EffectActive = false;
            _triggered = false;
        }

        _triggered = false;
        EffectActive = true;
    }

    // Stops an effect playing
    public void Stop()
    {
        _mercury.Terminate();
        EffectActive = false;
        _triggered = false;
    }

    public void Reset()
    {
        EffectActive = false;
        _mercury.Terminate();
    }

    public void Update(int xPos, int yPos, GameTime gameTime)
    {
        if (EffectActive)
        {
            if (!_triggered)
            {
                _mercury.Trigger(xPos, yPos);

                // Workaround for a bizarre bug that seems to stop the particle from working
                // unless we trigger until we get an active particle count.....
                if (_mercury.ActiveParticles > 0)
                {
                    _triggered = true;
                }
            }
            else
            {
                if (_mercury.ActiveParticles == 0)
                {
                    _triggered = false;
                    EffectActive = false;
                    _mercury.Terminate();

                    if (ParticleEffectComplete != null)
                    {
                        ParticleEffectComplete();
                    }
                }
            }
        }

        _mercury.Advance(gameTime);
    }

    public void Draw()
        => _mercury.Render();

    public void Draw(int screenFade)
        => _mercury.Render(screenFade);
}
