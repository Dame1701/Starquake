// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Engines;

namespace Starquake.Particles;

public class ParticleEngine
{
    private readonly GameEngine _gameEngineInstance;
    private readonly Dictionary<string, ParticleEffect> _particleEffects;

    private readonly SpriteBatchRenderer _particleRenderer;

    public ParticleEngine(GameEngine gameEngineInstance)
    {
        _gameEngineInstance = gameEngineInstance;
        _particleRenderer = new SpriteBatchRenderer();
        _particleRenderer.GraphicsDeviceService = _gameEngineInstance.GraphicsDeviceMgr;

        _particleEffects = new Dictionary<string, ParticleEffect>();
    }

    public void AddEffect(string effectName, string effectPath)
    {
        var temp = _gameEngineInstance.ContentMgr.Load<ParticleEffect>(effectPath);
        _particleEffects.Add(effectName, temp);
    }

    public void LoadContent()
    {
        foreach (var pe in _particleEffects.Values)
        {
            pe.LoadContent(_gameEngineInstance.ContentMgr);
        }

        _particleRenderer.LoadContent(_gameEngineInstance.ContentMgr);

        foreach (var pe in _particleEffects.Values)
        {
            pe.Initialise();
        }
    }

    /// <summary>
    ///     Returns a Particle effect based on the effect name given if it's single trigger it will only trigger once
    /// </summary>
    /// <param name="effectName"></param>
    /// <param name="SingleTrigger"></param>
    /// <param name="duration">
    ///     Single trigger how long until we signal the effect is complete, for non single trigger, how long
    ///     we want to play the routine
    /// </param>
    public ContinuousParticleEffect CreateContinualEffectInstance(string effectName, TimeSpan? duration)
    {
        if (_particleEffects.ContainsKey(effectName))
        {
            // Each instance needs its own copy of the effect, otherwise callers sharing
            // an effect name emit into, and draw from, the same particle pool
            var cpe = new ContinuousParticleEffect(_gameEngineInstance);
            cpe.EffectDuration = duration;
            cpe.MercuryParticleEffect = _particleEffects[effectName].DeepCopy();
            cpe.MercuryParticleEffect.LoadContent(_gameEngineInstance.ContentMgr);
            cpe.MercuryParticleEffect.Initialise();
            cpe.Renderer = _particleRenderer;

            return cpe;
        }

        throw new InvalidOperationException($"Particle effect '{effectName}' has not been registered.");
    }

    public MultiTriggerTimeBasedParticleEffect CreateMultiTriggeredTimeBasedEffectInstance(string effectName)
    {
        // Deep copy the effect
        // Return a class like the above but with the single trigger stuff in it
        // take the single trigger stuff out of the class above to avoid confusiojn
        // DONE :)
        if (_particleEffects.ContainsKey(effectName))
        {
            var mtpe = new MultiTriggerTimeBasedParticleEffect(_gameEngineInstance);
            mtpe.MercuryParticleEffect = _particleEffects[effectName].DeepCopy();
            mtpe.MercuryParticleEffect.LoadContent(_gameEngineInstance.ContentMgr);
            mtpe.MercuryParticleEffect.Initialise();
            mtpe.Renderer = _particleRenderer;

            return mtpe;
        }

        throw new InvalidOperationException($"Particle effect '{effectName}' has not been registered.");
    }

    public TriggeredParticleEffect CreateSingleTriggerEffectInstance(string effectName)
    {
        // Deep copy the effect
        // Return a class like the above but with the single trigger stuff in it
        // take the single trigger stuff out of the class above to avoid confusiojn
        // DONE :)
        if (_particleEffects.ContainsKey(effectName))
        {
            var tpe = new TriggeredParticleEffect(_gameEngineInstance);
            tpe.MercuryParticleEffect = _particleEffects[effectName].DeepCopy();
            tpe.MercuryParticleEffect.LoadContent(_gameEngineInstance.ContentMgr);
            tpe.MercuryParticleEffect.Initialise();
            tpe.Renderer = _particleRenderer;

            return tpe;
        }

        throw new InvalidOperationException($"Particle effect '{effectName}' has not been registered.");
    }
}