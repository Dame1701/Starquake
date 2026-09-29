// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Engines;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class BlobDeathAnimation : GameComponentBase
{
    private readonly Animation _blobDeathParticlesOriginal;

    private IPositionalTriggerParticleEffect _blobFragmentBounceSparks;
    private bool _drawBlobParts;

    public BlobDeathAnimation(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        BouncingParticles =
        [
            new BouncingParticle(gameEngineReference, 0),
            new BouncingParticle(gameEngineReference, 1),
            new BouncingParticle(gameEngineReference, 1),
            new BouncingParticle(gameEngineReference, 2),
            new BouncingParticle(gameEngineReference, 3),
            new BouncingParticle(gameEngineReference, 5)
        ];

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

    public override void LoadContent()
    {
        _blobFragmentBounceSparks =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks",
                ParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

        foreach (var t in BouncingParticles)
        {
            t.Fireball =
                GameEngine.ContentResourceManager.GetParticleEffectInstance("BlobFragmentExplosion",
                    ParticleEffectType.Continual, null) as IStandardParticleEffect;
            t.SparkEffect = _blobFragmentBounceSparks;
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
            if (bouncingParticle.Triggered)
            {
                bouncingParticle.Update(gameTime);
            }
        }

        if (GameEngine.UsingOldGraphics)
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
        if (GameEngine.UsingOldGraphics)
        {
            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

            foreach (var bouncingParticle in BouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    bouncingParticle.Draw(GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original"),
                        _blobDeathParticlesOriginal.CurrentFrame);
                }
            }

            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
        }
        else
        {
            foreach (var bouncingParticle in BouncingParticles)
            {
                if (!bouncingParticle.Triggered)
                {
                    continue;
                }

                if (_drawBlobParts)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobParts")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    bouncingParticle.Draw(GameEngine.ContentResourceManager.GetSpriteCollection("BlobParts"));
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobParts").End();
                }
                else
                {
                    bouncingParticle.Draw(null);
                }
            }

            if (_blobFragmentBounceSparks is { EffectActive: true })
            {
                _blobFragmentBounceSparks.Draw(screenFade);
            }
        }
    }
}