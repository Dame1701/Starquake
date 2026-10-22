// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.Particles;

internal class ContinuousParticleEffect(GraphicsSet graphicsSet, ParticleEffect mercuryParticleEffect, SpriteBatchRenderer renderer) : IStandardParticleEffect
{
    private readonly MercuryEffect _mercury = new(graphicsSet, mercuryParticleEffect, renderer);

    /// <summary>How many more logic frames the effect emits for, if it has a duration.</summary>
    private int _framesLeftToEmit;

    /// <summary>
    ///     How long the effect emits for, or null to emit until something stops it. Counted in logic
    ///     frames once <see cref="Start" /> converts it, rather than compared against the clock each
    ///     frame: the game moves everything a fixed amount per logic frame, so an effect timed off
    ///     <c>DateTime.Now</c> drifts against whatever it is drawn on.
    /// </summary>
    public TimeSpan? EffectDuration { get; init; }

    public ParticleEffect Effect => _mercury.Effect;
    public bool EffectActive { get; private set; }

    public event RaiseEffectComplete? ParticleEffectComplete;

    /// <summary>Starts the effect emitting, and keeps it emitting until <see cref="Stop" />.</summary>
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

    /// <summary>
    ///     Stops it emitting. Particles already in the air are left to age out, which is what makes
    ///     this different from clearing the effect.
    /// </summary>
    public void Stop()
    {
        EffectActive = false;
    }

    public void Reset()
    {
        EffectActive = false;
        _mercury.Terminate();
    }

    public void Update(int xPos, int yPos, GameTime gameTime)
    {
        if (EffectActive)
        {
            if (EffectDuration == null)
            {
                _mercury.Trigger(xPos, yPos);
            }
            else if (_framesLeftToEmit > 0)
            {
                _framesLeftToEmit--;
                _mercury.Trigger(xPos, yPos);
            }
            else if (_mercury.ActiveParticles == 0)
            {
                // The emitting is over, but the particles already in flight must be allowed to finish
                // before the effect reports itself done.
                ParticleEffectComplete?.Invoke();
                EffectActive = false;
            }
        }

        _mercury.Advance(gameTime);
    }

    public void Draw()
        => _mercury.Render();

    public void Draw(int screenFade)
        => _mercury.Render(screenFade);
}
