using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectMercury.Renderers;
using Microsoft.Xna.Framework;

namespace StarquakeXNA.Particles
{
    public interface IParticleEffect
    {
        ProjectMercury.ParticleEffect Effect { get; }
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
        TriggeredMultiLocationTimed,
    }

    public class ParticleEngine
    {
        public class ContinuousParticleEffect : IStandardParticleEffect
        {
            private TimeSpan? effectDuration;
            public TimeSpan? EffectDuration
            {
                get { return effectDuration; }
                set { effectDuration = value; }
            }

            private DateTime effectLastTriggered;
            private TimeSpan? triggerPeriod;
            public TimeSpan? TriggerPeriod
            {
                get { return triggerPeriod; }
                set { triggerPeriod = value; }
            }
 
            internal ProjectMercury.ParticleEffect mercuryParticleEffect;
            public ProjectMercury.ParticleEffect Effect
            {
                get { return mercuryParticleEffect; }
            }

            internal SpriteBatchRenderer renderer;
            private GameEngine gameEngineReference;

            DateTime effectStartTime;
            
            bool effectActive;
            public bool EffectActive
            {
                get { return effectActive; }
            }

            public event RaiseEffectComplete ParticleEffectComplete;

            public ContinuousParticleEffect(GameEngine GameEngineReference)
            {
                gameEngineReference = GameEngineReference;
                effectDuration = new TimeSpan(0);
                effectActive = false;
            }

            // Starts an effect that is not triggered
            public void Start()
            {
                if (effectActive == false)
                {
                    effectActive = true;
                    if (effectDuration != null)
                    {
                        effectStartTime = System.DateTime.Now;
                    }

                    if (triggerPeriod != null)
                    {
                        effectLastTriggered = System.DateTime.Now;
                    }
                }
            }

            // Stops an effect that is not trigger nor has a duration
            public void Stop()
            {
                effectActive = false;
            }

            public void Reset()
            {
                effectActive = false;
                mercuryParticleEffect.Terminate();
            }

            public void Update(int XPos, int YPos, GameTime GameTime)
            {
                if (effectActive)
                {
                    if (effectDuration != null)
                    {
                        if (effectStartTime + effectDuration <
                                System.DateTime.Now)
                        {
                            // Continual trigger effects need to allow the particle engine to finish drawing
                            if (mercuryParticleEffect.ActiveParticlesCount == 0)
                            {
                                if (ParticleEffectComplete != null)
                                {
                                    ParticleEffectComplete();
                                }
                                effectActive = false;
                            }
                        }
                        else
                        {
                            if (triggerPeriod != null)
                            {
                                if (System.DateTime.Now > (effectLastTriggered + triggerPeriod))
                                {
                                    mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                                    effectLastTriggered = System.DateTime.Now;
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
                        if (triggerPeriod != null)
                        {
                            if (System.DateTime.Now > (effectLastTriggered + triggerPeriod))
                            {
                                mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                                effectLastTriggered = System.DateTime.Now;
                            }
                        }
                        else
                        {
                            mercuryParticleEffect.Trigger(new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos)));
                        }
                    }
                }               

                // Mercury must always be updated
                float deltaSeconds = (float)GameTime.ElapsedGameTime.TotalSeconds;
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
            internal ProjectMercury.ParticleEffect mercuryParticleEffect;
            internal SpriteBatchRenderer renderer;
            private GameEngine gameEngineReference;

            bool effectActive;
            public bool EffectActive
            {
                get { return effectActive; }
            }

            public ProjectMercury.ParticleEffect Effect
            {
                get { return mercuryParticleEffect; }
            }

            bool triggered;

            public event RaiseEffectComplete ParticleEffectComplete;

            public TriggeredParticleEffect(GameEngine GameEngineReference)
            {
                gameEngineReference = GameEngineReference;
                effectActive = false;
                triggered = false;
            }

            // Starts an effect that is not triggered
            public void Start()
            {
                if (effectActive)
                {
                    // Kill the previous effect
                    mercuryParticleEffect.Terminate();
                    effectActive = false;
                    triggered = false;
                }

                triggered = false;
                effectActive = true;
            }

            // Stops an effect playing
            public void Stop()
            {
                mercuryParticleEffect.Terminate();
                effectActive = false;
                triggered = false;
            }

            public void Reset()
            {
                effectActive = false;
                mercuryParticleEffect.Terminate();
            }

            public void Update(int XPos, int YPos, GameTime GameTime)
            {
                if (effectActive)
                {
                    if (!triggered)
                    {
                        Vector2 position = new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos));
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
                            effectActive = false;
                            mercuryParticleEffect.Terminate();

                            if (ParticleEffectComplete != null)
                            {
                                ParticleEffectComplete();
                            }
                        }
                    }
                    
                }

                // Mercury must always be updated
                float deltaSeconds = (float)GameTime.ElapsedGameTime.TotalSeconds;
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
            internal ProjectMercury.ParticleEffect mercuryParticleEffect;
            internal SpriteBatchRenderer renderer;
            public event RaiseEffectComplete ParticleEffectComplete;
            private GameEngine gameEngineReference;

            public ProjectMercury.ParticleEffect Effect
            {
                get { return mercuryParticleEffect; }
            }

            public MultiTriggerTimeBasedParticleEffect(GameEngine GameEngineReference)
            {
                gameEngineReference = GameEngineReference;
            }

            public void Start(int XPos, int YPos)
            {
                Vector2 position = new Vector2(gameEngineReference.ScreenEffctEngine.GetParticleX(XPos), gameEngineReference.ScreenEffctEngine.GetParticleY(YPos));
                mercuryParticleEffect.Trigger(ref position);
            }

            public void Update(GameTime GameTime)
            {
                float deltaSeconds = (float)GameTime.ElapsedGameTime.TotalSeconds;
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

        SpriteBatchRenderer particleRenderer;
        GameEngine gameEngineInstance;
        Dictionary<string, ProjectMercury.ParticleEffect> particleEffects;

        public ParticleEngine(GameEngine GameEngineInstance)
        {
            gameEngineInstance = GameEngineInstance;
            particleRenderer = new SpriteBatchRenderer();
            particleRenderer.GraphicsDeviceService = gameEngineInstance.GraphicsDeviceMgr;
            
            particleEffects = new Dictionary<string, ProjectMercury.ParticleEffect>();
        }

        public void AddEffect(string EffectName, string EffectPath)
        {
            ProjectMercury.ParticleEffect temp = gameEngineInstance.ContentMgr.Load<ProjectMercury.ParticleEffect>(EffectPath);
            particleEffects.Add(EffectName, temp);
        }

        public void LoadContent()
        {
            foreach (ProjectMercury.ParticleEffect pe in particleEffects.Values)
            {
                pe.LoadContent(gameEngineInstance.ContentMgr);
            }

            particleRenderer.LoadContent(gameEngineInstance.ContentMgr);

            foreach (ProjectMercury.ParticleEffect pe in particleEffects.Values)
            {
                pe.Initialise();
            }
        }

        /// <summary>
        /// Returns a Particle effect based on the effect name given if it's single trigger it will only trigger once
        /// </summary>
        /// <param name="EffectName"></param>
        /// <param name="SingleTrigger"></param>
        /// <param name="Duration">Single trigger how long until we signal the effect is complete, for non single trigger, how long we want to play the routine</param>
        public ContinuousParticleEffect CreateContinualEffectInstance(string EffectName, TimeSpan? Duration)
        {
            if (particleEffects.ContainsKey(EffectName))
            {
                ContinuousParticleEffect cpe = new ContinuousParticleEffect(gameEngineInstance);
                cpe.EffectDuration = Duration;
                cpe.mercuryParticleEffect = particleEffects[EffectName];
                cpe.renderer = particleRenderer;

                return cpe;
            }
            else
            {
                //throw new Exception("Effect: '" + EffectName + "' has not been registered");
            }

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
                MultiTriggerTimeBasedParticleEffect mtpe = new MultiTriggerTimeBasedParticleEffect(gameEngineInstance);
                mtpe.mercuryParticleEffect = particleEffects[EffectName].DeepCopy();
                mtpe.mercuryParticleEffect.LoadContent(gameEngineInstance.ContentMgr);
                mtpe.mercuryParticleEffect.Initialise();
                mtpe.renderer = particleRenderer;

                return mtpe;
            }
            else
            {
                //throw new Exception("Effect: '" + EffectName + "' has not been registered");
            }

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
                TriggeredParticleEffect tpe = new TriggeredParticleEffect(gameEngineInstance);
                tpe.mercuryParticleEffect = particleEffects[EffectName].DeepCopy();
                tpe.mercuryParticleEffect.LoadContent(gameEngineInstance.ContentMgr);
                tpe.mercuryParticleEffect.Initialise();
                tpe.renderer = particleRenderer;

                return tpe;
            }
            else
            {
                //throw new Exception("Effect: '" + EffectName + "' has not been registered");
            }

            return null;
        }
    }
}
