// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.Animations;

internal class BlobDeathAnimation
{
    // The six fragment sprites Blob comes apart into. Deliberately not a straight run
    private static readonly int[] FragmentSpriteNumbers = [0, 1, 1, 2, 3, 5];

    private readonly Animation _blobDeathParticlesOriginal;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly List<BouncingParticle> _bouncingParticles;

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

            return _blobFragmentBounceSparks is { EffectActive: true };
        }
    }

    public void LoadContent()
    {
        _blobFragmentBounceSparks = (IPositionalTriggerParticleEffect)_resources
            .GetParticleEffectInstance("FireSparks", ParticleEffectType.TriggeredMultiLocationTimed, null);

        // Each fragment needs its own explosion emitter, but they all spark off the one effect
        _bouncingParticles.Clear();
        foreach (int fragmentSpriteNumber in FragmentSpriteNumbers)
        {
            _bouncingParticles.Add(new BouncingParticle(_resources, _gameScreens, _randomNumberGenerator, fragmentSpriteNumber)
            {
                Fireball = (IStandardParticleEffect)_resources
                    .GetParticleEffectInstance("BlobFragmentExplosion", ParticleEffectType.Continual, null),
                SparkEffect = _blobFragmentBounceSparks
            });
        }
    }

    public void Trigger(int effectXPosition, int effectYPosition, bool drawBlobParts, int effectMinDuration = 50,
        int effectMaxDuration = 100)
    {
        _drawBlobParts = drawBlobParts;

        foreach (BouncingParticle bouncingParticle in _bouncingParticles)
        {
            bouncingParticle.Trigger(effectXPosition, effectYPosition, effectMinDuration, effectMaxDuration);
        }
    }

    public void Reset()
    {
        foreach (BouncingParticle bouncingParticle in _bouncingParticles)
        {
            bouncingParticle.Reset();
        }
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
    }

    public void Draw(int screenFade)
    {
        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection("BlobFire2Original")
                .Begin(BlendState.AlphaBlend);

            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    bouncingParticle.Draw(
                        _resources.GetSpriteCollection("BlobFire2Original"),
                        screenFade,
                        _blobDeathParticlesOriginal.CurrentFrame);
                }
            }

            _resources.GetSpriteCollection("BlobFire2Original").End();
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
                    _resources.GetSpriteCollection("BlobParts")
                        .Begin(BlendState.AlphaBlend);

                    bouncingParticle.Draw(_resources.GetSpriteCollection("BlobParts"), screenFade);

                    _resources.GetSpriteCollection("BlobParts").End();
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
