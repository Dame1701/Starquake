// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.ContentManagement;

namespace Starquake.Particles;

public class ParticleEngine
{
    private readonly ContentManager _contentManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly Dictionary<string, ParticleEffect> _particleEffects;

    private readonly SpriteBatchRenderer _particleRenderer;

    public ParticleEngine(ContentManager content, GraphicsDeviceManager graphics, GraphicsSet graphicsSet)
    {
        _contentManager = content;
        _graphicsSet = graphicsSet;

        _particleRenderer = new SpriteBatchRenderer();
        _particleRenderer.GraphicsDeviceService = graphics;

        _particleEffects = new Dictionary<string, ParticleEffect>();
    }

    public void AddEffect(string effectName, string effectPath)
    {
        var temp = _contentManager.Load<ParticleEffect>(effectPath);
        _particleEffects.Add(effectName, temp);
    }

    public void LoadContent()
    {
        foreach (ParticleEffect pe in _particleEffects.Values)
        {
            pe.LoadContent(_contentManager);
        }

        _particleRenderer.LoadContent(_contentManager);

        foreach (ParticleEffect pe in _particleEffects.Values)
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
        => new(_graphicsSet, CopyEffect(effectName), _particleRenderer)
        {
            EffectDuration = duration
        };

    public MultiTriggerTimeBasedParticleEffect CreateMultiTriggeredTimeBasedEffectInstance(string effectName)
        => new(_graphicsSet, CopyEffect(effectName), _particleRenderer);

    public TriggeredParticleEffect CreateSingleTriggerEffectInstance(string effectName)
        => new(_graphicsSet, CopyEffect(effectName), _particleRenderer);

    /// <summary>
    ///     Each instance needs its own copy of the effect, otherwise callers sharing an effect name emit into,
    ///     and draw from, the same particle pool.
    /// </summary>
    private ParticleEffect CopyEffect(string effectName)
    {
        if (!_particleEffects.TryGetValue(effectName, out ParticleEffect? registered))
        {
            throw new InvalidOperationException($"Particle effect '{effectName}' has not been registered.");
        }

        ParticleEffect? copy = registered.DeepCopy();
        copy.LoadContent(_contentManager);
        copy.Initialise();

        return copy;
    }
}
