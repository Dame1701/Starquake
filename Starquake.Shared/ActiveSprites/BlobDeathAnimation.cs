// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Animations;
using StarquakeXNA.Engines;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA.ActiveSprites;

public class BouncingParticle
{

    private readonly GameEngine _gameEngine;
    private readonly BouncingAnimation _particleBounce;

    private int _oldXPos;
    private int _oldYPos;

    public BouncingParticle(GameEngine gameEngineReference, int spriteNo)
    {
        _gameEngine = gameEngineReference;
        _particleBounce = new BouncingAnimation(gameEngineReference, spriteNo);
    }

    public IStandardParticleEffect Fireball { get; set; }

    public int XPos { get; private set; }

    public int YPos { get; private set; }

    public bool Triggered => _particleBounce.Triggered;

    public IPositionalTriggerParticleEffect SparkEffect { get; set; }

    public void XBounce()
    {
        _particleBounce.XBounce();
    }

    public void YBounce()
    {
        _particleBounce.YBounce();
    }

    public void Trigger(int xPos, int yPos, int effectMinDuration = 50, int effectMaxDuration = 100)
    {
        XPos = xPos + 16;
        YPos = yPos + 16;

        _oldYPos = YPos;
        _oldXPos = XPos;

        var xSpeed = _gameEngine.RandomNumberGenerator.Next(4, 12);

        var xDirection = _gameEngine.RandomNumberGenerator.Next(0, 2);
        if (xDirection == 0)
        {
            xSpeed = -xSpeed;
        }

        Fireball.Start();

        _particleBounce.Trigger(
            _gameEngine.RandomNumberGenerator.Next(1, 2),
            _gameEngine.RandomNumberGenerator.Next(1, 20),
            xSpeed, effectMinDuration, effectMaxDuration);
    }

    public void Reset()
    {
        _particleBounce.Reset();
    }

    public void Update(GameTime gameTime)
    {
        if (_particleBounce.Triggered)
        {
            var p = _particleBounce.Update();

            int xSpeed;
            int ySpeed;

            if (p.Y > 0)
            {
                // Ensure we don't move more than the size of the collision tilemap
                if (p.Y > 32)
                {
                    p.Y = 32;
                }

                ySpeed = _gameEngine.GameScreens.CheckCollision(Direction.Down, XPos, YPos, 32, 32, p.Y, true);

                if (ySpeed == 0)
                {
                    YPos = _oldYPos;
                    _particleBounce.YBounce();
                    SparkEffect.Start(XPos, YPos);
                }

                // Keep the parts within the screen bounds, makes for a better effect
                if (YPos > _gameEngine.GameScreens.ScreenHeight - 32)
                {
                    YPos = _gameEngine.GameScreens.ScreenHeight - 32;
                    _particleBounce.YBounce();
                    SparkEffect.Start(XPos, YPos);
                }
            }
            else
            {
                // Ensure we don't move more than the size of the collision tilemap
                if (p.Y < -32)
                {
                    p.Y = -32;
                }

                // Absolute value used here to convert the negative to positive for col routine
                ySpeed = _gameEngine.GameScreens.CheckCollision(Direction.Up, XPos, YPos, 32, 32, Math.Abs(p.Y),
                    true);

                if (ySpeed == 0)
                {
                    YPos = _oldYPos;
                    _particleBounce.YBounce();
                    SparkEffect.Start(XPos, YPos);
                }

                // Convert back to negative number
                ySpeed = -ySpeed;

                // Keep the parts within the screen bounds, makes for a better effect
                if (YPos < 32)
                {
                    YPos = 32;
                    _particleBounce.YBounce();
                    SparkEffect.Start(XPos, YPos);
                }
            }

            YPos = YPos + ySpeed;
            _oldYPos = YPos;

            if (p.X > 0)
            {
                // Ensure we don't move more than the size of the collision tilemap
                if (p.X > 32)
                {
                    p.X = 32;
                }

                xSpeed = _gameEngine.GameScreens.CheckCollision(Direction.Right, XPos, YPos, 32, 32, p.X, true);
                if (xSpeed == 0)
                {
                    XPos = _oldXPos;
                    _particleBounce.XBounce();
                    SparkEffect.Start(XPos, YPos);
                }

                // Keep the parts within the screen bounds, makes for a better effect
                if (XPos > _gameEngine.GameScreens.ScreenWidth - 32)
                {
                    XPos = _gameEngine.GameScreens.ScreenWidth - 32;
                    _particleBounce.XBounce();
                    SparkEffect.Start(XPos, YPos);
                }
            }
            else
            {
                // Ensure we don't move more than the size of the collision tilemap
                if (p.X < -32)
                {
                    p.X = 32;
                }

                // Absolute value used here to convert the negative to positive for col routine
                xSpeed = _gameEngine.GameScreens.CheckCollision(Direction.Left, XPos, YPos, 32, 32, Math.Abs(p.X),
                    true);
                if (xSpeed == 0)
                {
                    XPos = _oldXPos;
                    _particleBounce.XBounce();
                    SparkEffect.Start(XPos, YPos);
                }

                // Convert back to negative number
                xSpeed = -xSpeed;

                // Keep the parts within the screen bounds, makes for a better effect
                if (XPos < 32)
                {
                    XPos = 32;
                    _particleBounce.XBounce();
                    SparkEffect.Start(XPos, YPos);
                }
            }

            XPos += xSpeed;
            _oldXPos = XPos;

            Fireball.Update(XPos + 20, YPos + 5, gameTime);
        }
        else
        {
            Fireball.Reset();
        }
    }

    public void Draw(SpriteCollection spriteToDraw, int spriteNumber = -1)
    {
        if (Triggered)
        {
            if (spriteNumber >= 0 && spriteToDraw != null)
            {
                _particleBounce.Draw(spriteToDraw, XPos + 4, YPos + 4, spriteNumber);
            }

            // In addictive mode so draw a few to improve brightness
            Fireball.Draw(_particleBounce.Fade);
        }
    }
}

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