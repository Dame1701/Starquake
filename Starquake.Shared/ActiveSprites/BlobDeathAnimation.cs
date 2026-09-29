// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class BouncingParticle
{

    private readonly GameEngine _gameEngine;
    private readonly BouncingAnimation _particleBounce;

    private int _oldXPos;
    private int _oldYPos;

    public BouncingParticle(GameEngine GameEngineReference, int SpriteNo)
    {
        _gameEngine = GameEngineReference;
        _particleBounce = new BouncingAnimation(GameEngineReference, SpriteNo);
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

                ySpeed = _gameEngine.GameScreens.CheckCollision(enDirection.Down, XPos, YPos, 32, 32, p.Y, true);

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
                ySpeed = _gameEngine.GameScreens.CheckCollision(enDirection.Up, XPos, YPos, 32, 32, Math.Abs(p.Y),
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

                xSpeed = _gameEngine.GameScreens.CheckCollision(enDirection.Right, XPos, YPos, 32, 32, p.X, true);
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
                xSpeed = _gameEngine.GameScreens.CheckCollision(enDirection.Left, XPos, YPos, 32, 32, Math.Abs(p.X),
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

    public void Draw(SpriteCollection SpriteToDraw, int SpriteNumber = -1)
    {
        if (Triggered)
        {
            if (SpriteNumber >= 0 && SpriteToDraw != null)
            {
                _particleBounce.Draw(SpriteToDraw, XPos + 4, YPos + 4, SpriteNumber);
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

    public BlobDeathAnimation(GameEngine GameEngineReference)
        : base(GameEngineReference)
    {
        BouncingParticles =
        [
            new BouncingParticle(GameEngineReference, 0),
            new BouncingParticle(GameEngineReference, 1),
            new BouncingParticle(GameEngineReference, 1),
            new BouncingParticle(GameEngineReference, 2),
            new BouncingParticle(GameEngineReference, 3),
            new BouncingParticle(GameEngineReference, 5)
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
            gameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks",
                enParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

        foreach (var t in BouncingParticles)
        {
            t.Fireball =
                gameEngine.ContentResourceManager.GetParticleEffectInstance("BlobFragmentExplosion",
                    enParticleEffectType.Continual, null) as IStandardParticleEffect;
            t.SparkEffect = _blobFragmentBounceSparks;
        }
    }

    public void Trigger(int EffectXPosition, int EffectYPosition, bool DrawBlobParts, int EffectMinDuration = 50,
        int EffectMaxDuration = 100)
    {
        _drawBlobParts = DrawBlobParts;

        foreach (var bouncingParticle in BouncingParticles)
        {
            bouncingParticle.Trigger(EffectXPosition, EffectYPosition, EffectMinDuration, EffectMaxDuration);
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

        if (gameEngine.UsingOldGraphics)
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
        if (gameEngine.UsingOldGraphics)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

            foreach (var bouncingParticle in BouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    bouncingParticle.Draw(gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original"),
                        _blobDeathParticlesOriginal.CurrentFrame);
                }
            }

            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
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
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobParts")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    bouncingParticle.Draw(gameEngine.ContentResourceManager.GetSpriteCollection("BlobParts"));
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobParts").End();
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