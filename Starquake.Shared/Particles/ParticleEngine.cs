// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
#nullable enable
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
    /// <param name="duration">
    ///     Single trigger how long until we signal the effect is complete, for non single trigger, how long
    ///     we want to play the routine
    /// </param>
    public ContinuousParticleEffect CreateContinualEffectInstance(string effectName, TimeSpan? duration)
        => new(_gameEngineInstance, CopyEffect(effectName), _particleRenderer)
        {
            EffectDuration = duration
        };

    public MultiTriggerTimeBasedParticleEffect CreateMultiTriggeredTimeBasedEffectInstance(string effectName)
        => new(_gameEngineInstance, CopyEffect(effectName), _particleRenderer);

    public TriggeredParticleEffect CreateSingleTriggerEffectInstance(string effectName)
        => new(_gameEngineInstance, CopyEffect(effectName), _particleRenderer);

    /// <summary>
    ///     Each instance needs its own copy of the effect, otherwise callers sharing an effect name emit into,
    ///     and draw from, the same particle pool.
    /// </summary>
    private ParticleEffect CopyEffect(string effectName)
    {
        if (!_particleEffects.TryGetValue(effectName, out var registered))
        {
            throw new InvalidOperationException($"Particle effect '{effectName}' has not been registered.");
        }

        var copy = registered.DeepCopy();
        copy.LoadContent(_gameEngineInstance.ContentMgr);
        copy.Initialise();

        return copy;
    }
}
