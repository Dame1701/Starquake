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

public class MultiTriggerTimeBasedParticleEffect : IPositionalTriggerParticleEffect
{
    private readonly GraphicsSet _graphicsSet;
    private readonly ParticleEffect _mercuryParticleEffect;
    private readonly SpriteBatchRenderer _renderer;

    public MultiTriggerTimeBasedParticleEffect(GraphicsSet graphicsSet,
        ParticleEffect mercuryParticleEffect, SpriteBatchRenderer renderer)
    {
        _graphicsSet = graphicsSet;
        _mercuryParticleEffect = mercuryParticleEffect;
        _renderer = renderer;
    }
    public event RaiseEffectComplete? ParticleEffectComplete;

    public ParticleEffect Effect => _mercuryParticleEffect;

    public void Start(int xPos, int yPos)
    {
        var position = new Vector2(xPos, yPos);
        _mercuryParticleEffect.Trigger(ref position);
    }

    public void Update(GameTime gameTime)
    {
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mercuryParticleEffect.Update(deltaSeconds);

        if (_mercuryParticleEffect.ActiveParticlesCount == 0)
        {
            if (ParticleEffectComplete != null)
            {
                ParticleEffectComplete();
            }
        }
    }

    public bool EffectActive
    {
        get
        {
            if (_mercuryParticleEffect.ActiveParticlesCount > 0)
            {
                return true;
            }
            return false;
        }
    }


    public void Stop()
    {
        _mercuryParticleEffect.Terminate();
    }

    public void Reset()
    {
        _mercuryParticleEffect.Terminate();
    }

    public void Draw()
    {
        if (_graphicsSet.IsRedrawn)
        {
            // We must always call this!
            if (_mercuryParticleEffect.ActiveParticlesCount > 0)
            {
                _renderer.RenderEffect(_mercuryParticleEffect);
            }
        }
    }

    public void Draw(int screenFade)
    {
        if (_graphicsSet.IsRedrawn)
        {
            // We must always call this!
            if (_mercuryParticleEffect.ActiveParticlesCount > 0)
            {
                _renderer.RenderEffect(_mercuryParticleEffect, screenFade);
            }
        }
    }
}
