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

internal class MultiTriggerTimeBasedParticleEffect(
    GraphicsSet graphicsSet,
    ParticleEffect mercuryParticleEffect,
    SpriteBatchRenderer renderer) : IPositionalTriggerParticleEffect
{
    private readonly MercuryEffect _mercury = new(graphicsSet, mercuryParticleEffect, renderer);

    public event RaiseEffectComplete? ParticleEffectComplete;

    public ParticleEffect Effect => _mercury.Effect;

    /// <summary>
    ///     This one has no active flag of its own: it is emitting for as long as there is anything
    ///     in flight, which is what lets the same effect be triggered again at a second place
    ///     before the first lot has finished.
    /// </summary>
    public bool EffectActive => _mercury.ActiveParticles > 0;

    public void Start(int xPos, int yPos) => _mercury.Trigger(xPos, yPos);

    public void Update(GameTime gameTime)
    {
        _mercury.Advance(gameTime);

        if (_mercury.ActiveParticles == 0)
        {
            ParticleEffectComplete?.Invoke();
        }
    }

    public void Stop() => _mercury.Terminate();

    public void Reset() => _mercury.Terminate();

    /// <remarks>
    ///     Unlike the other two this skips the draw entirely when nothing is in flight, rather than
    ///     handing Mercury an empty effect to render.
    /// </remarks>
    public void Draw()
    {
        if (_mercury.ActiveParticles > 0)
        {
            _mercury.Render();
        }
    }

    /// <inheritdoc cref="Draw()" />
    public void Draw(int screenFade)
    {
        if (_mercury.ActiveParticles > 0)
        {
            _mercury.Render(screenFade);
        }
    }
}
