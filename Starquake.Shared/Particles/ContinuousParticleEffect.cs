// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Structure;

namespace Starquake.Particles;

public class ContinuousParticleEffect : IStandardParticleEffect
{
    private readonly GraphicsSet _graphicsSet;
    private readonly ParticleEffect _mercuryParticleEffect;
    private readonly SpriteBatchRenderer _renderer;

    /// <summary>
    ///     How many more logic frames the effect emits for, when it has a duration at all.
    /// </summary>
    private int _framesLeftToEmit;

    public ContinuousParticleEffect(GraphicsSet graphicsSet, ParticleEffect mercuryParticleEffect, SpriteBatchRenderer renderer)
    {
        _graphicsSet = graphicsSet;
        _mercuryParticleEffect = mercuryParticleEffect;
        _renderer = renderer;
        EffectActive = false;
    }

    /// <summary>
    ///     How long the effect emits for, or null to emit until something stops it. Counted off in
    ///     logic frames once <see cref="Start" /> converts it, rather than compared against the
    ///     clock every frame - the game moves everything by a fixed amount per logic frame, and an
    ///     effect timed off DateTime.Now drifts against the thing it is drawn on.
    /// </summary>
    public TimeSpan? EffectDuration { get; init; }

    public ParticleEffect Effect => _mercuryParticleEffect;
    public bool EffectActive { get; private set; }

    public event RaiseEffectComplete? ParticleEffectComplete;

    // Starts an effect that is not triggered
    public void Start()
    {
        if (EffectActive)
        {
            return;
        }

        EffectActive = true;
        _framesLeftToEmit = EffectDuration != null
            ? LogicClock.Seconds(EffectDuration.Value.TotalSeconds)
            : 0;
    }

    // Stops an effect that is not trigger nor has a duration
    public void Stop()
    {
        EffectActive = false;
    }

    public void Reset()
    {
        EffectActive = false;
        _mercuryParticleEffect.Terminate();
    }

    public void Update(int xPos, int yPos, GameTime gameTime)
    {
        if (EffectActive)
        {
            if (EffectDuration == null)
            {
                _mercuryParticleEffect.Trigger(new Vector2(xPos, yPos));
            }
            else if (_framesLeftToEmit > 0)
            {
                _framesLeftToEmit--;
                _mercuryParticleEffect.Trigger(new Vector2(xPos, yPos));
            }
            else if (_mercuryParticleEffect.ActiveParticlesCount == 0)
            {
                // The emitting is over, but the particles already in flight have to be allowed to
                // finish before the effect says it is done
                ParticleEffectComplete?.Invoke();
                EffectActive = false;
            }
        }

        // Mercury must always be updated
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mercuryParticleEffect.Update(deltaSeconds);
    }

    public void Draw()
    {
        // We must always call this!
        if (_graphicsSet.IsRedrawn)
        {
            _renderer.RenderEffect(_mercuryParticleEffect);
        }
    }

    public void Draw(int screenFade)
    {
        if (_graphicsSet.IsRedrawn)
        {
            _renderer.RenderEffect(_mercuryParticleEffect, screenFade);
        }
    }
}
