// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Core;

namespace Starquake.Particles;

internal class TriggeredParticleEffect(GraphicsSet graphicsSet,
    ParticleEffect mercuryParticleEffect,
    SpriteBatchRenderer renderer) : IStandardParticleEffect
{
    private readonly MercuryEffect _mercury = new(graphicsSet, mercuryParticleEffect, renderer);

    private bool _triggered;

    public bool EffectActive { get; private set; }

    public ParticleEffect Effect => _mercury.Effect;

    public event RaiseEffectComplete? ParticleEffectComplete;

    /// <summary>
    ///     Starts the effect. One already playing is killed first, so a retrigger replaces it rather
    ///     than running two at once.
    /// </summary>
    public void Start()
    {
        if (EffectActive)
        {
            _mercury.Terminate();
            EffectActive = false;
            _triggered = false;
        }

        _triggered = false;
        EffectActive = true;
    }

    /// <summary>Stops the effect at once, particles in the air included.</summary>
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

                // Mercury sometimes reports no active particles after a trigger, so it is triggered
                // again until the count comes back non-zero. The cause is not understood; removing
                // this loop stops the effect appearing at all.
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

                    ParticleEffectComplete?.Invoke();
                }
            }
        }

        _mercury.Advance(gameTime);
    }

    public void Draw() => _mercury.Render();

    public void Draw(int screenFade) => _mercury.Render(screenFade);
}
