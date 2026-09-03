using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA
{
    public class BouncingAnimation
    {
        double gravity = 0;
        double yDelta = 0;
        int xDelta = 0;
        double friction = 1.00;
        int fadeSpeed = 0;

        int effectDuration = 0;
        int effectDurationCounter = 0;

        double fade = 255;
        public int Fade
        {
            get { return (int)fade; }
        }

        int spriteNo;
        float rotation = 0f;
        float rotationSpeed = 0f;

        float scale = 1.0f;

        GameEngine gameEngine;
        bool triggered = false;
        public bool Triggered
        {
            get { return triggered; }
        }

        public BouncingAnimation(GameEngine GameEngineReference, int SpriteNo)
        {
            spriteNo = SpriteNo;
            gameEngine = GameEngineReference;
        }

        public void Reset()
        {
            triggered = false;
        }

        public void Trigger(double Gravity, int InitialYSpeed, int XSpeed, int EffectMinDuration = 50, int EffectMaxDuration = 100)
        {
            yDelta = InitialYSpeed;
            xDelta = XSpeed;
            gravity = Gravity;
            effectDurationCounter = 0;
            triggered = true;
            fade = 255;
            scale = 1.0f;
            effectDuration = gameEngine.RandomNumberGenerator.Next(EffectMinDuration, EffectMaxDuration);
            fadeSpeed = gameEngine.RandomNumberGenerator.Next(10, 30);
            rotationSpeed = (float)(gameEngine.RandomNumberGenerator.NextDouble() - 0.5);
        }

        public void YBounce()
        {
            if (triggered)
            {
                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPartClang", false); 
                yDelta = -yDelta;
            }
        }

        public void XBounce()
        {
            if (triggered)
            {
                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPartClang", false); 
                xDelta = -xDelta;
            }
        }

        public Point Update()
        {
            if (triggered)
            {
                yDelta += gravity;
                yDelta /= friction;
                
                rotation = rotation + rotationSpeed;

                if (scale > 0)
                {
                    scale = scale - (1f / (float)effectDuration);
                }
                else
                {
                    scale = 0;
                }

                if (fade > 0)
                {
                    fade = fade - ((double)255 / (double)effectDuration);
                }
                else
                {
                    fade = 0;
                }

                // Fade the particles out
                if (effectDurationCounter > effectDuration)
                {
                    triggered = false;
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
}
