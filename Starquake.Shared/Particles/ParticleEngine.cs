// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;

namespace StarquakeXNA.Particles;

public interface IParticleEffect
{
    ParticleEffect Effect { get; }
    bool EffectActive { get; }
    event RaiseEffectComplete ParticleEffectComplete;
    void Stop();
    void Reset();
    void Draw();
    void Draw(int screenFade);
}

// All standard triggered and continual with and without duration particle effects
public interface IStandardParticleEffect : IParticleEffect
{
    void Start();
    void Update(int XPos, int YPos, GameTime GameTime);
}

// A particle effect that can be initialised from multiple positions with their own durational lifetimes, an example are the post fire sparks
public interface IPositionalTriggerParticleEffect : IParticleEffect
{
    void Start(int XPos, int YPos);
    void Update(GameTime gameTime);
}

public enum enParticleEffectType
{
    Triggered,
    Continual,
    TriggeredMultiLocationTimed
}

public class ParticleEngine
{
    private readonly GameEngine gameEngineInstance;
    private readonly Dictionary<string, ParticleEffect> particleEffects;

    private readonly SpriteBatchRenderer particleRenderer;

    public ParticleEngine(GameEngine GameEngineInstance)
    {
        gameEngineInstance = GameEngineInstance;
        particleRenderer = new SpriteBatchRenderer();
        particleRenderer.GraphicsDeviceService = gameEngineInstance.GraphicsDeviceMgr;

        particleEffects = new Dictionary<string, ParticleEffect>();
    }

    public void AddEffect(string EffectName, string EffectPath)
    {
        var temp = gameEngineInstance.ContentMgr.Load<ParticleEffect>(EffectPath);
        particleEffects.Add(EffectName, temp);
    }

    public void LoadContent()
    {
        foreach (var pe in particleEffects.Values)
        {
            pe.LoadContent(gameEngineInstance.ContentMgr);
        }

        particleRenderer.LoadContent(gameEngineInstance.ContentMgr);

        foreach (var pe in particleEffects.Values)
        {
            pe.Initialise();
        }
    }

    /// <summary>
    ///     Returns a Particle effect based on the effect name given if it's single trigger it will only trigger once
    /// </summary>
    /// <param name="EffectName"></param>
    /// <param name="SingleTrigger"></param>
    /// <param name="Duration">
    ///     Single trigger how long until we signal the effect is complete, for non single trigger, how long
    ///     we want to play the routine
    /// </param>
    public ContinuousParticleEffect CreateContinualEffectInstance(string EffectName, TimeSpan? Duration)
    {
        if (particleEffects.ContainsKey(EffectName))
        {
            var cpe = new ContinuousParticleEffect(gameEngineInstance);
            cpe.EffectDuration = Duration;
            cpe.mercuryParticleEffect = particleEffects[EffectName];
            cpe.renderer = particleRenderer;

            return cpe;
        }

        //throw new Exception("Effect: '" + EffectName + "' has not been registered");
        return null;
    }

    public MultiTriggerTimeBasedParticleEffect CreateMultiTriggeredTimeBasedEffectInstance(string EffectName)
    {
        // Deep copy the effect
        // Return a class like the above but with the single trigger stuff in it
        // take the single trigger stuff out of the class above to avoid confusiojn
        // DONE :)
        if (particleEffects.ContainsKey(EffectName))
        {
            var mtpe = new MultiTriggerTimeBasedParticleEffect(gameEngineInstance);
            mtpe.mercuryParticleEffect = particleEffects[EffectName].DeepCopy();
            mtpe.mercuryParticleEffect.LoadContent(gameEngineInstance.ContentMgr);
            mtpe.mercuryParticleEffect.Initialise();
            mtpe.renderer = particleRenderer;

            return mtpe;
        }

        //throw new Exception("Effect: '" + EffectName + "' has not been registered");
        return null;
    }

    public TriggeredParticleEffect CreateSingleTriggerEffectInstance(string EffectName)
    {
        // Deep copy the effect
        // Return a class like the above but with the single trigger stuff in it
        // take the single trigger stuff out of the class above to avoid confusiojn
        // DONE :)
        if (particleEffects.ContainsKey(EffectName))
        {
            var tpe = new TriggeredParticleEffect(gameEngineInstance);
            tpe.mercuryParticleEffect = particleEffects[EffectName].DeepCopy();
            tpe.mercuryParticleEffect.LoadContent(gameEngineInstance.ContentMgr);
            tpe.mercuryParticleEffect.Initialise();
            tpe.renderer = particleRenderer;

            return tpe;
        }

        //throw new Exception("Effect: '" + EffectName + "' has not been registered");
        return null;
    }

    public class ContinuousParticleEffect : IStandardParticleEffect
    {
        private readonly GameEngine gameEngineReference;

        private DateTime effectLastTriggered;

        private DateTime effectStartTime;

        internal ParticleEffect mercuryParticleEffect;

        internal SpriteBatchRenderer renderer;

        public ContinuousParticleEffect(GameEngine GameEngineReference)
        {
            gameEngineReference = GameEngineReference;
            EffectDuration = new TimeSpan(0);
            EffectActive = false;
        }

        public TimeSpan? EffectDuration { get; set; }

        public TimeSpan? TriggerPeriod { get; set; }

        public ParticleEffect Effect => mercuryParticleEffect;
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
                    effectStartTime = DateTime.Now;
                }

                if (TriggerPeriod != null)
                {
                    effectLastTriggered = DateTime.Now;
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
            mercuryParticleEffect.Terminate();
        }

        public void Update(int XPos, int YPos, GameTime GameTime)
        {
            if (EffectActive)
            {
                if (EffectDuration != null)
                {
                    if (effectStartTime + EffectDuration <
                        DateTime.Now)
                    {
                        // Continual trigger effects need to allow the particle engine to finish drawing
                        if (mercuryParticleEffect.ActiveParticlesCount == 0)
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
                            if (DateTime.Now > effectLastTriggered + TriggerPeriod)
                            {
                                mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                                effectLastTriggered = DateTime.Now;
                            }
                        }
                        else
                        {
                            mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                        }
                    }
                }
                else
                {
                    // Trigger always
                    if (TriggerPeriod != null)
                    {
                        if (DateTime.Now > effectLastTriggered + TriggerPeriod)
                        {
                            mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                            effectLastTriggered = DateTime.Now;
                        }
                    }
                    else
                    {
                        mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                    }
                }
            }

            // Mercury must always be updated
            var deltaSeconds = (float)GameTime.ElapsedGameTime.TotalSeconds;
            mercuryParticleEffect.Update(deltaSeconds);
        }

        public void Draw()
        {
            // We must always call this!
            if (!gameEngineReference.UsingOldGraphics)
            {
                renderer.RenderEffect(mercuryParticleEffect);
            }
        }

        public void Draw(int screenFade)
        {
            if (!gameEngineReference.UsingOldGraphics)
            {
                renderer.RenderEffect(mercuryParticleEffect, screenFade);
            }
        }
    }

    public class TriggeredParticleEffect : IStandardParticleEffect
    {
        private readonly GameEngine gameEngineReference;

        internal ParticleEffect mercuryParticleEffect;
        internal SpriteBatchRenderer renderer;

        private bool triggered;

        public TriggeredParticleEffect(GameEngine GameEngineReference)
        {
            gameEngineReference = GameEngineReference;
            EffectActive = false;
            triggered = false;
        }
        public bool EffectActive { get; private set; }

        public ParticleEffect Effect => mercuryParticleEffect;

        public event RaiseEffectComplete ParticleEffectComplete;

        // Starts an effect that is not triggered
        public void Start()
        {
            if (EffectActive)
            {
                // Kill the previous effect
                mercuryParticleEffect.Terminate();
                EffectActive = false;
                triggered = false;
            }

            triggered = false;
            EffectActive = true;
        }

        // Stops an effect playing
        public void Stop()
        {
            mercuryParticleEffect.Terminate();
            EffectActive = false;
            triggered = false;
        }

        public void Reset()
        {
            EffectActive = false;
            mercuryParticleEffect.Terminate();
        }

        public void Update(int XPos, int YPos, GameTime GameTime)
        {
            if (EffectActive)
            {
                if (!triggered)
                {
                    var position = new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos));
                    mercuryParticleEffect.Trigger(ref position);

                    // Workaround for a bizzare bug that seems to stop the particle from working
                    // unless we trigger until we get an active partical count.....
                    if (mercuryParticleEffect.ActiveParticlesCount > 0)
                    {
                        triggered = true;
                    }
                }
                else
                {
                    if (mercuryParticleEffect.ActiveParticlesCount == 0)
                    {
                        triggered = false;
                        EffectActive = false;
                        mercuryParticleEffect.Terminate();

                        if (ParticleEffectComplete != null)
                        {
                            ParticleEffectComplete();
                        }
                    }
                }

            }

            // Mercury must always be updated
            var deltaSeconds = (float)GameTime.ElapsedGameTime.TotalSeconds;
            mercuryParticleEffect.Update(deltaSeconds);
        }

        public void Draw()
        {
            if (!gameEngineReference.UsingOldGraphics)
            {
                // We must always call this!
                renderer.RenderEffect(mercuryParticleEffect);
            }
        }

        public void Draw(int screenFade)
        {
            if (!gameEngineReference.UsingOldGraphics)
            {
                renderer.RenderEffect(mercuryParticleEffect, screenFade);
            }
        }
    }

    public class MultiTriggerTimeBasedParticleEffect : IPositionalTriggerParticleEffect
    {
        private readonly GameEngine gameEngineReference;
        internal ParticleEffect mercuryParticleEffect;
        internal SpriteBatchRenderer renderer;

        public MultiTriggerTimeBasedParticleEffect(GameEngine GameEngineReference)
        {
            gameEngineReference = GameEngineReference;
        }
        public event RaiseEffectComplete ParticleEffectComplete;

        public ParticleEffect Effect => mercuryParticleEffect;

        public void Start(int XPos, int YPos)
        {
            var position = new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos));
            mercuryParticleEffect.Trigger(ref position);
        }

        public void Update(GameTime GameTime)
        {
            var deltaSeconds = (float)GameTime.ElapsedGameTime.TotalSeconds;
            mercuryParticleEffect.Update(deltaSeconds);

            if (mercuryParticleEffect.ActiveParticlesCount == 0)
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
                if (mercuryParticleEffect.ActiveParticlesCount > 0)
                {
                    return true;
                }
                return false;
            }
        }


        public void Stop()
        {
            mercuryParticleEffect.Terminate();
        }

        public void Reset()
        {
            mercuryParticleEffect.Terminate();
        }

        public void Draw()
        {
            if (!gameEngineReference.UsingOldGraphics)
            {
                // We must always call this!
                if (mercuryParticleEffect.ActiveParticlesCount > 0)
                {
                    renderer.RenderEffect(mercuryParticleEffect);
                }
            }
        }

        public void Draw(int screenFade)
        {
            if (!gameEngineReference.UsingOldGraphics)
            {
                // We must always call this!
                if (mercuryParticleEffect.ActiveParticlesCount > 0)
                {
                    renderer.RenderEffect(mercuryParticleEffect, screenFade);
                }
            }
        }
    }
}