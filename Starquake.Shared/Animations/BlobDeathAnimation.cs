// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.Animations;

public class BlobDeathAnimation : GameComponentBase
{
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    // The six fragment sprites Blob comes apart into. Deliberately not a straight run
    private static readonly int[] FragmentSpriteNumbers = [0, 1, 1, 2, 3, 5];

    private readonly Animation _blobDeathParticlesOriginal;

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

        BouncingParticles = new List<BouncingParticle>(FragmentSpriteNumbers.Length);

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

    public List<BouncingParticle> BouncingParticles { get; }

    public bool Triggered
    {
        get
        {
            foreach (var bouncingParticle in BouncingParticles)
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
            foreach (var bouncingParticle in BouncingParticles)
            {
                if (bouncingParticle.Triggered || bouncingParticle.HasActiveParticles)
                {
                    return true;
                }
            }

            return _blobFragmentBounceSparks is { EffectActive: true };
        }
    }

    public override void LoadContent()
    {
        _blobFragmentBounceSparks =
            (IPositionalTriggerParticleEffect)_resources.GetParticleEffectInstance("FireSparks",
                ParticleEffectType.TriggeredMultiLocationTimed, null);

        // Each fragment needs its own explosion emitter, but they all spark off the one effect
        BouncingParticles.Clear();
        foreach (var fragmentSpriteNumber in FragmentSpriteNumbers)
        {
            BouncingParticles.Add(new BouncingParticle(_resources, _gameScreens, _randomNumberGenerator, fragmentSpriteNumber)
            {
                Fireball =
                    (IStandardParticleEffect)_resources.GetParticleEffectInstance("BlobFragmentExplosion",
                        ParticleEffectType.Continual, null),
                SparkEffect = _blobFragmentBounceSparks
            });
        }
    }

    public void Trigger(int effectXPosition, int effectYPosition, bool drawBlobParts, int effectMinDuration = 50,
        int effectMaxDuration = 100)
    {
        _drawBlobParts = drawBlobParts;

        foreach (var bouncingParticle in BouncingParticles)
        {
            bouncingParticle.Trigger(effectXPosition, effectYPosition, effectMinDuration, effectMaxDuration);
        }
    }

    public void Reset()
    {
        foreach (var bouncingParticle in BouncingParticles)
        {
            bouncingParticle.Reset();
        }
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var bouncingParticle in BouncingParticles)
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

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection("BlobFire2Original")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

            foreach (var bouncingParticle in BouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    bouncingParticle.Draw(_resources.GetSpriteCollection("BlobFire2Original"),
                        screenFade, _blobDeathParticlesOriginal.CurrentFrame);
                }
            }

            _resources.GetSpriteCollection("BlobFire2Original").End();
        }
        else
        {
            foreach (var bouncingParticle in BouncingParticles)
            {
                if (!bouncingParticle.Triggered && !bouncingParticle.HasActiveParticles)
                {
                    continue;
                }

                if (_drawBlobParts)
                {
                    _resources.GetSpriteCollection("BlobParts")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    bouncingParticle.Draw(_resources.GetSpriteCollection("BlobParts"),
                        screenFade);
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