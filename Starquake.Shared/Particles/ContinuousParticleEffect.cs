// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Particles;

public class ContinuousParticleEffect : IStandardParticleEffect
{
    private readonly GameEngine _gameEngineReference;

    private DateTime _effectLastTriggered;

    private DateTime _effectStartTime;

    internal readonly ParticleEffect MercuryParticleEffect;

    internal readonly SpriteBatchRenderer Renderer;

    public ContinuousParticleEffect(GameEngine gameEngineReference, ParticleEffect mercuryParticleEffect,
        SpriteBatchRenderer renderer)
    {
        _gameEngineReference = gameEngineReference;
        MercuryParticleEffect = mercuryParticleEffect;
        Renderer = renderer;
        EffectDuration = new TimeSpan(0);
        EffectActive = false;
    }

    public TimeSpan? EffectDuration { get; set; }

    public TimeSpan? TriggerPeriod { get; set; }

    public ParticleEffect Effect => MercuryParticleEffect;
    public bool EffectActive { get; private set; }

    public event RaiseEffectComplete? ParticleEffectComplete;

    // Starts an effect that is not triggered
    public void Start()
    {
        if (!EffectActive)
        {
            EffectActive = true;
            if (EffectDuration != null)
            {
                _effectStartTime = DateTime.Now;
            }

            if (TriggerPeriod != null)
            {
                _effectLastTriggered = DateTime.Now;
            }
        }
    }

    // Stops an effect that is not trigger nor has a duration
    public void Stop()
    {
        EffectActive = false;
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
            if (EffectDuration != null)
            {
                if (_effectStartTime + EffectDuration <
                    DateTime.Now)
                {
                    // Continual trigger effects need to allow the particle engine to finish drawing
                    if (MercuryParticleEffect.ActiveParticlesCount == 0)
                    {
                        if (ParticleEffectComplete != null)
                        {
                            ParticleEffectComplete();
                        }
                        EffectActive = false;
                    }
                }
                else
                {
                    if (TriggerPeriod != null)
                    {
                        if (DateTime.Now > _effectLastTriggered + TriggerPeriod)
                        {
                            MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffectEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffectEngine.GetParticleY(yPos)));
                            _effectLastTriggered = DateTime.Now;
                        }
                    }
                    else
                    {
                        MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffectEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffectEngine.GetParticleY(yPos)));
                    }
                }
            }
            else
            {
                // Trigger always
                if (TriggerPeriod != null)
                {
                    if (DateTime.Now > _effectLastTriggered + TriggerPeriod)
                    {
                        MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffectEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffectEngine.GetParticleY(yPos)));
                        _effectLastTriggered = DateTime.Now;
                    }
                }
                else
                {
                    MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffectEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffectEngine.GetParticleY(yPos)));
                }
            }
        }

        // Mercury must always be updated
        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MercuryParticleEffect.Update(deltaSeconds);
    }

    public void Draw()
    {
        // We must always call this!
        if (_gameEngineReference.GraphicsSet.IsRedrawn)
        {
            Renderer.RenderEffect(MercuryParticleEffect);
        }
    }

    public void Draw(int screenFade)
    {
        if (_gameEngineReference.GraphicsSet.IsRedrawn)
        {
            Renderer.RenderEffect(MercuryParticleEffect, screenFade);
        }
    }
}
