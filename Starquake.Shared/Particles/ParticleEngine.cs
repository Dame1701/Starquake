// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Engines;
using Starquake.Structure;

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
            var cpe = new ContinuousParticleEffect(_gameEngineInstance);
            cpe.EffectDuration = duration;
            cpe.MercuryParticleEffect = _particleEffects[effectName];
            cpe.Renderer = _particleRenderer;

            return cpe;
        }

        //throw new Exception("Effect: '" + EffectName + "' has not been registered");
        return null;
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

        //throw new Exception("Effect: '" + EffectName + "' has not been registered");
        return null;
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

        //throw new Exception("Effect: '" + EffectName + "' has not been registered");
        return null;
    }

    public class ContinuousParticleEffect : IStandardParticleEffect
    {
        private readonly GameEngine _gameEngineReference;

        private DateTime _effectLastTriggered;

        private DateTime _effectStartTime;

        internal ParticleEffect MercuryParticleEffect;

        internal SpriteBatchRenderer Renderer;

        public ContinuousParticleEffect(GameEngine gameEngineReference)
        {
            _gameEngineReference = gameEngineReference;
            EffectDuration = new TimeSpan(0);
            EffectActive = false;
        }

        public TimeSpan? EffectDuration { get; set; }

        public TimeSpan? TriggerPeriod { get; set; }

        public ParticleEffect Effect => MercuryParticleEffect;
        public bool EffectActive { get; private set; }

        public event RaiseEffectComplete ParticleEffectComplete;

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
                                MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffctEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffctEngine.GetParticleY(yPos)));
                                _effectLastTriggered = DateTime.Now;
                            }
                        }
                        else
                        {
                            MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffctEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffctEngine.GetParticleY(yPos)));
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
                            MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffctEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffctEngine.GetParticleY(yPos)));
                            _effectLastTriggered = DateTime.Now;
                        }
                    }
                    else
                    {
                        MercuryParticleEffect.Trigger(new Vector2(_gameEngineReference.ScreenEffctEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffctEngine.GetParticleY(yPos)));
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
            if (!_gameEngineReference.UsingOldGraphics)
            {
                Renderer.RenderEffect(MercuryParticleEffect);
            }
        }

        public void Draw(int screenFade)
        {
            if (!_gameEngineReference.UsingOldGraphics)
            {
                Renderer.RenderEffect(MercuryParticleEffect, screenFade);
            }
        }
    }

    public class TriggeredParticleEffect : IStandardParticleEffect
    {
        private readonly GameEngine _gameEngineReference;

        internal ParticleEffect MercuryParticleEffect;
        internal SpriteBatchRenderer Renderer;

        private bool _triggered;

        public TriggeredParticleEffect(GameEngine gameEngineReference)
        {
            _gameEngineReference = gameEngineReference;
            EffectActive = false;
            _triggered = false;
        }
        public bool EffectActive { get; private set; }

        public ParticleEffect Effect => MercuryParticleEffect;

        public event RaiseEffectComplete ParticleEffectComplete;

        // Starts an effect that is not triggered
        public void Start()
        {
            if (EffectActive)
            {
                // Kill the previous effect
                MercuryParticleEffect.Terminate();
                EffectActive = false;
                _triggered = false;
            }

            _triggered = false;
            EffectActive = true;
        }

        // Stops an effect playing
        public void Stop()
        {
            MercuryParticleEffect.Terminate();
            EffectActive = false;
            _triggered = false;
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
                if (!_triggered)
                {
                    var position = new Vector2(_gameEngineReference.ScreenEffctEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffctEngine.GetParticleY(yPos));
                    MercuryParticleEffect.Trigger(ref position);

                    // Workaround for a bizzare bug that seems to stop the particle from working
                    // unless we trigger until we get an active partical count.....
                    if (MercuryParticleEffect.ActiveParticlesCount > 0)
                    {
                        _triggered = true;
                    }
                }
                else
                {
                    if (MercuryParticleEffect.ActiveParticlesCount == 0)
                    {
                        _triggered = false;
                        EffectActive = false;
                        MercuryParticleEffect.Terminate();

                        if (ParticleEffectComplete != null)
                        {
                            ParticleEffectComplete();
                        }
                    }
                }

            }

            // Mercury must always be updated
            var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MercuryParticleEffect.Update(deltaSeconds);
        }

        public void Draw()
        {
            if (!_gameEngineReference.UsingOldGraphics)
            {
                // We must always call this!
                Renderer.RenderEffect(MercuryParticleEffect);
            }
        }

        public void Draw(int screenFade)
        {
            if (!_gameEngineReference.UsingOldGraphics)
            {
                Renderer.RenderEffect(MercuryParticleEffect, screenFade);
            }
        }
    }

    public class MultiTriggerTimeBasedParticleEffect : IPositionalTriggerParticleEffect
    {
        private readonly GameEngine _gameEngineReference;
        internal ParticleEffect MercuryParticleEffect;
        internal SpriteBatchRenderer Renderer;

        public MultiTriggerTimeBasedParticleEffect(GameEngine gameEngineReference)
        {
            _gameEngineReference = gameEngineReference;
        }
        public event RaiseEffectComplete ParticleEffectComplete;

        public ParticleEffect Effect => MercuryParticleEffect;

        public void Start(int xPos, int yPos)
        {
            var position = new Vector2(_gameEngineReference.ScreenEffctEngine.GetParticleX(xPos), _gameEngineReference.ScreenEffctEngine.GetParticleY(yPos));
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
}