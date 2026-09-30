// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Particles;

public class MultiTriggerTimeBasedParticleEffect : IPositionalTriggerParticleEffect
{
    private readonly GameEngine _gameEngineReference;
    internal readonly ParticleEffect MercuryParticleEffect;
    internal readonly SpriteBatchRenderer Renderer;

    public MultiTriggerTimeBasedParticleEffect(GameEngine gameEngineReference,
        ParticleEffect mercuryParticleEffect, SpriteBatchRenderer renderer)
    {
        _gameEngineReference = gameEngineReference;
        MercuryParticleEffect = mercuryParticleEffect;
        Renderer = renderer;
    }
    public event RaiseEffectComplete? ParticleEffectComplete;

    public ParticleEffect Effect => MercuryParticleEffect;

    public void Start(int xPos, int yPos)
    {
        var position = new Vector2(_gameEngineReference.ScreenEffectEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffectEngine.GetParticleY(yPos));
        MercuryParticleEffect.Trigger(ref position);
    }

    public void Update(GameTime gameTime)
    {
        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MercuryParticleEffect.Update(deltaSeconds);

        if (MercuryParticleEffect.ActiveParticlesCount == 0)
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
            if (MercuryParticleEffect.ActiveParticlesCount > 0)
            {
                return true;
            }
            return false;
        }
    }


    public void Stop()
    {
        MercuryParticleEffect.Terminate();
    }

    public void Reset()
    {
        MercuryParticleEffect.Terminate();
    }

    public void Draw()
    {
        if (!_gameEngineReference.UsingOldGraphics)
        {
            // We must always call this!
            if (MercuryParticleEffect.ActiveParticlesCount > 0)
            {
                Renderer.RenderEffect(MercuryParticleEffect);
            }
        }
    }

    public void Draw(int screenFade)
    {
        if (!_gameEngineReference.UsingOldGraphics)
        {
            // We must always call this!
            if (MercuryParticleEffect.ActiveParticlesCount > 0)
            {
                Renderer.RenderEffect(MercuryParticleEffect, screenFade);
            }
        }
    }
}
