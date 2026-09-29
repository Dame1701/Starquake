// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class BouncingAnimation
{
    private readonly double friction = 1.00;

    private readonly GameEngine gameEngine;

    private readonly int spriteNo;

    private int effectDuration;
    private int effectDurationCounter;

    private double fade = 255;
    private int fadeSpeed;
    private double gravity;
    private float rotation;
    private float rotationSpeed;

    private float scale = 1.0f;
    private int xDelta;
    private double yDelta;

    public BouncingAnimation(GameEngine GameEngineReference, int SpriteNo)
    {
        spriteNo = SpriteNo;
        gameEngine = GameEngineReference;
    }
    public int Fade => (int)fade;
    public bool Triggered { get; private set; }

    public void Reset()
    {
        Triggered = false;
    }

    public void Trigger(double Gravity, int InitialYSpeed, int XSpeed, int EffectMinDuration = 50, int EffectMaxDuration = 100)
    {
        yDelta = InitialYSpeed;
        xDelta = XSpeed;
        gravity = Gravity;
        effectDurationCounter = 0;
        Triggered = true;
        fade = 255;
        scale = 1.0f;
        effectDuration = gameEngine.RandomNumberGenerator.Next(EffectMinDuration, EffectMaxDuration);
        fadeSpeed = gameEngine.RandomNumberGenerator.Next(10, 30);
        rotationSpeed = (float)(gameEngine.RandomNumberGenerator.NextDouble() - 0.5);
    }

    public void YBounce()
    {
        if (Triggered)
        {
            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPartClang", false);
            yDelta = -yDelta;
        }
    }

    public void XBounce()
    {
        if (Triggered)
        {
            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPartClang", false);
            xDelta = -xDelta;
        }
    }

    public Point Update()
    {
        if (Triggered)
        {
            yDelta += gravity;
            yDelta /= friction;

            rotation = rotation + rotationSpeed;

            if (scale > 0)
            {
                scale = scale - 1f / effectDuration;
            }
            else
            {
                scale = 0;
            }

            if (fade > 0)
            {
                fade = fade - 255 / (double)effectDuration;
            }
            else
            {
                fade = 0;
            }

            // Fade the particles out
            if (effectDurationCounter > effectDuration)
            {
                Triggered = false;
            }

            effectDurationCounter++;
        }

        return new Point(xDelta, (int)yDelta);
    }

    public void Draw(SpriteCollection SpriteToDraw, int xPos, int yPos, int SpriteNumberOverride = -1)
    {
        if (SpriteNumberOverride >= 0)
        {
            SpriteToDraw.DrawSprite(SpriteNumberOverride, xPos, yPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fade), scale, rotation);
        }
        else
        {
            SpriteToDraw.DrawSprite(spriteNo, xPos, yPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fade), scale, rotation);
        }
    }
}