// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.Animations;

internal class BlobDeathAnimation
{
    /// <summary>
    ///     The six fragment sprites Blob comes apart into. Deliberately not a straight run: the
    ///     repeated 1 and the gap before 5 are the artwork as it is, not a mistake to be tidied into
    ///     0 to 5.
    /// </summary>
    private static readonly int[] FragmentSpriteNumbers = [0, 1, 1, 2, 3, 5];

    private readonly Animation _blobDeathParticlesOriginal;
    private readonly List<BouncingParticle> _bouncingParticles;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    /// <summary>
    ///     The ring thrown out at the moment he comes apart. One for the whole death rather than
    ///     one per fragment: six rings from six fragments would be a smear rather than a blast.
    /// </summary>
    private readonly Shockwave _shockwave;

    private IPositionalTriggerParticleEffect? _blobFragmentBounceSparks;

    private bool _drawBlobParts;

    public BlobDeathAnimation(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _randomNumberGenerator = randomNumberGenerator;

        _bouncingParticles = new List<BouncingParticle>(FragmentSpriteNumbers.Length);

        _shockwave = new Shockwave(
            resources, randomNumberGenerator, ShockwaveSettings.BlobDiameter, ShockwaveSettings.BlobFrames);

        _blobDeathParticlesOriginal = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 3,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = true
        };
    }

    public bool Triggered
    {
        get
        {
            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // True while anything still needs updating or drawing. The fragments' particles outlive
    // the bounce itself, so this stays set for as long as the tail takes to fade
    public bool Active
    {
        get
        {
            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered || bouncingParticle.HasActiveParticles)
                {
                    return true;
                }
            }

            // The shockwave is deliberately not counted. This is what holds Blob dead until the
            // death has finished playing, and the ring is a little over half a second against the
            // fragments' one to two - so it can never be the last thing still running, and
            // counting it could only ever hold him dead for longer than his death lasts.
            return _blobFragmentBounceSparks is { EffectActive: true };
        }
    }

    public void LoadContent()
    {
        _blobFragmentBounceSparks = (IPositionalTriggerParticleEffect)_resources
            .GetParticleEffectInstance(Assets.Effects.FireSparks, ParticleEffectType.TriggeredMultiLocationTimed, null);

        // Each fragment needs its own explosion emitter, but they all spark off the one effect
        _bouncingParticles.Clear();
        foreach (int fragmentSpriteNumber in FragmentSpriteNumbers)
        {
            _bouncingParticles.Add(new BouncingParticle(_resources, _gameScreens, _randomNumberGenerator, fragmentSpriteNumber)
            {
                Fireball = (IStandardParticleEffect)_resources
                    .GetParticleEffectInstance(Assets.Effects.BlobFragmentExplosion, ParticleEffectType.Continual, null),
                SparkEffect = _blobFragmentBounceSparks
            });
        }
    }

    /// <remarks>
    ///     Both of Blob's deaths come through here - the overload he builds up to and the instant
    ///     one a spike or a gate gives him - because an instant death is an overload that begins
    ///     already exploded. So the shockwave is started once, here, and belongs to both.
    /// </remarks>
    public void Trigger(int effectXPosition, int effectYPosition, bool drawBlobParts, int effectMinDuration = 50,
        int effectMaxDuration = 100)
    {
        _drawBlobParts = drawBlobParts;

        _shockwave.Trigger(effectXPosition, effectYPosition);

        // One turn for the whole star, so the parts stay evenly spread around it but the burst is
        // not in the same attitude every time Blob dies.
        double spin = _randomNumberGenerator.NextDouble() * 2 * Math.PI;

        for (int point = 0; point < _bouncingParticles.Count; point++)
        {
            _bouncingParticles[point].Trigger(
                effectXPosition, effectYPosition, point, _bouncingParticles.Count, spin,
                effectMinDuration, effectMaxDuration);
        }
    }

    /// <summary>
    ///     Stops the whole thing dead: the fragments, the particles still in the air behind them,
    ///     and the shockwave.
    /// </summary>
    /// <remarks>
    ///     This used to reset only the bounces, which left every emitter still holding its
    ///     particles - so <see cref="Active" /> stayed true on their account and the clangs carried
    ///     on being struck. The intro's crash is the one caller that needs it to mean stop, and it
    ///     needs it to mean stop completely.
    /// </remarks>
    public void Reset()
    {
        foreach (BouncingParticle bouncingParticle in _bouncingParticles)
        {
            bouncingParticle.Reset();
            bouncingParticle.Fireball.Stop();
            bouncingParticle.Fireball.Reset();
        }

        _blobFragmentBounceSparks?.Stop();
        _shockwave.Stop();
    }

    public void Update(GameTime gameTime)
    {
        foreach (BouncingParticle bouncingParticle in _bouncingParticles)
        {
            if (bouncingParticle.Triggered || bouncingParticle.HasActiveParticles)
            {
                bouncingParticle.Update(gameTime);
            }
        }

        if (_graphicsSet.IsOriginal)
        {
            _blobDeathParticlesOriginal.GetNextFrame();
        }

        if (_blobFragmentBounceSparks is { EffectActive: true })
        {
            _blobFragmentBounceSparks.Update(gameTime);
        }

        _shockwave.Update();
    }

    public void Draw(int screenFade)
    {
        // Under the fragments, so they come out of it rather than through it. Drawn for both sets
        // of artwork: the ring is the blast and not a piece of the redrawn look.
        _shockwave.Draw(screenFade);

        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original)
                .Begin(BlendState.AlphaBlend);

            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    bouncingParticle.Draw(
                        _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original),
                        screenFade,
                        _blobDeathParticlesOriginal.CurrentFrame);
                }
            }

            _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original).End();
        }
        else
        {
            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle is { Triggered: false, HasActiveParticles: false })
                {
                    continue;
                }

                if (_drawBlobParts)
                {
                    _resources.GetSpriteCollection(Assets.Graphics.BlobParts)
                        .Begin(BlendState.AlphaBlend);

                    bouncingParticle.Draw(_resources.GetSpriteCollection(Assets.Graphics.BlobParts), screenFade);

                    _resources.GetSpriteCollection(Assets.Graphics.BlobParts).End();
                }
                else
                {
                    bouncingParticle.Draw(null, screenFade);
                }
            }

            if (_blobFragmentBounceSparks is { EffectActive: true })
            {
                _blobFragmentBounceSparks.Draw(screenFade);
            }
        }
    }
}
