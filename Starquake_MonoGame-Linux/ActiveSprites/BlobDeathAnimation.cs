using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Particles;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public class BouncingParticle
    {
        BouncingAnimation particleBounce;

        IStandardParticleEffect fireball;
        public IStandardParticleEffect Fireball
        {
            get { return fireball; }
            set { fireball = value; }
        }

        int oldXPos;
        int oldYPos;

        int xPos;
        public int XPos
        {
            get { return xPos; }
        }

        int yPos;
        public int YPos
        {
            get { return yPos; }
        }

        public bool Triggered
        {
            get { return particleBounce.Triggered; }
        }

        GameEngine gameEngine;

        

        IPositionalTriggerParticleEffect sparkEffect;
        public IPositionalTriggerParticleEffect SparkEffect
        {
            get { return sparkEffect; }
            set { sparkEffect = value; }
        }

        public BouncingParticle(GameEngine GameEngineReference, int SpriteNo)
        {
            gameEngine = GameEngineReference;
            particleBounce = new BouncingAnimation(GameEngineReference, SpriteNo);
        }

        public void XBounce()
        {
            particleBounce.XBounce();
        }

        public void YBounce()
        {
            particleBounce.YBounce();
        }

        public void Trigger(int XPos, int YPos, int EffectMinDuration = 50, int EffectMaxDuration = 100)
        {
            xPos = XPos + 16;
            yPos = YPos + 16;

            oldYPos = yPos;
            oldXPos = xPos;

            int xDirection = gameEngine.RandomNumberGenerator.Next(0, 2);

            int xSpeed = 0;
            xSpeed = gameEngine.RandomNumberGenerator.Next(4, 12);

            if (xDirection == 0)
            {
                xSpeed = -xSpeed;
            }

            fireball.Start();

            particleBounce.Trigger(
                gameEngine.RandomNumberGenerator.Next(1, 2),
                gameEngine.RandomNumberGenerator.Next(1, 20),
                xSpeed, EffectMinDuration, EffectMaxDuration);
        }

        public void Update(GameTime gameTime)
        {
            if (particleBounce.Triggered)
            {
                Point p = particleBounce.Update();

                int xSpeed = 0;
                int ySpeed = 0;


                if (p.Y > 0)
                {
                    // Ensure we don't move more than the size of the collision tilemap
                    if (p.Y > 32)
                    {
                        p.Y = 32;
                    }

                    ySpeed = gameEngine.GameScreens.CheckCollision(enDirection.Down, xPos, yPos, 32, 32, p.Y, true);

                    if (ySpeed == 0)
                    {
                        yPos = oldYPos;
                        particleBounce.YBounce();
                        sparkEffect.Start(xPos, yPos);
                    }

                    // Keep the parts within the screen bounds, makes for a better effect
                    if (yPos > gameEngine.GameScreens.ScreenHeight - 32)
                    {
                        yPos = gameEngine.GameScreens.ScreenHeight - 32;
                        particleBounce.YBounce();
                        sparkEffect.Start(xPos, yPos);
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
                    ySpeed = gameEngine.GameScreens.CheckCollision(enDirection.Up, xPos, yPos, 32, 32, Math.Abs(p.Y), true);

                    if (ySpeed == 0)
                    {
                        yPos = oldYPos;
                        particleBounce.YBounce();
                        sparkEffect.Start(xPos, yPos);
                    }

                    // Convert back to negative number
                    ySpeed = -ySpeed;

                    // Keep the parts within the screen bounds, makes for a better effect
                    if (yPos < 32)
                    {
                        yPos = 32;
                        particleBounce.YBounce();
                        sparkEffect.Start(xPos, yPos);
                    }
                }

                yPos = yPos + ySpeed;
                oldYPos = yPos;

                if (p.X > 0)
                {
                    // Ensure we don't move more than the size of the collision tilemap
                    if (p.X > 32)
                    {
                        p.X = 32;
                    }

                    xSpeed = gameEngine.GameScreens.CheckCollision(enDirection.Right, xPos, yPos, 32, 32, p.X, true);
                    if (xSpeed == 0)
                    {
                        xPos = oldXPos;
                        particleBounce.XBounce();
                        sparkEffect.Start(xPos, yPos);
                    }

                    // Keep the parts within the screen bounds, makes for a better effect
                    if (xPos > gameEngine.GameScreens.ScreenWidth - 32)
                    {
                        xPos = gameEngine.GameScreens.ScreenWidth - 32;
                        particleBounce.XBounce();
                        sparkEffect.Start(xPos, yPos);
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
                    xSpeed = gameEngine.GameScreens.CheckCollision(enDirection.Left, xPos, yPos, 32, 32, Math.Abs(p.X), true);
                    if (xSpeed == 0)
                    {
                        xPos = oldXPos;
                        particleBounce.XBounce();
                        sparkEffect.Start(xPos, yPos);
                    }

                    // Convert back to negative number
                    xSpeed = -xSpeed;

                    // Keep the parts within the screen bounds, makes for a better effect
                    if (xPos < 32)
                    {
                        xPos = 32;
                        particleBounce.XBounce();
                        sparkEffect.Start(xPos, yPos);
                    }
                }

                xPos = xPos + xSpeed;
                oldXPos = xPos;

                fireball.Update(XPos + 20, YPos + 5, gameTime);
            }
            else
            {
                fireball.Reset();
            }
        }

        public void Draw(SpriteCollection SpriteToDraw, int SpriteNumber = -1)
        {
            if (Triggered)
            {
                if (SpriteNumber >= 0 && SpriteToDraw !=null)
                {
                    particleBounce.Draw(SpriteToDraw, xPos + 4, yPos + 4, SpriteNumber);
                }

                // In addictive mode so draw a few to improve brightness
                fireball.Draw(particleBounce.Fade);
            }
        }
    }

    public class BlobDeathAnimation : GameComponentBase
    {
        List<BouncingParticle> bps;
        Animation blobDeathParticlesOriginal;
        bool drawBlobParts;

        //private IStandardParticleEffect blobFragmentFireball;
        private IPositionalTriggerParticleEffect blobFragmentBounceSparks;

        public List<BouncingParticle> BouncingParticles
        {
            get { return bps; }
        }

        public bool Triggered
        {
            get
            {
                for (int a = 0; a < bps.Count; a++)
                {
                    if (bps[a].Triggered)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public BlobDeathAnimation(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            bps = new List<BouncingParticle>();

            bps.Add(new BouncingParticle(GameEngineReference, 0));
            bps.Add(new BouncingParticle(GameEngineReference, 1));
            bps.Add(new BouncingParticle(GameEngineReference, 1));
            bps.Add(new BouncingParticle(GameEngineReference, 2));
            bps.Add(new BouncingParticle(GameEngineReference, 3));
            bps.Add(new BouncingParticle(GameEngineReference, 5));

            blobDeathParticlesOriginal = new Animation();
            blobDeathParticlesOriginal = new Animation();
            blobDeathParticlesOriginal.FirstFrame = 0;
            blobDeathParticlesOriginal.CurrentFrame = 0;
            blobDeathParticlesOriginal.LastFrame = 3;
            blobDeathParticlesOriginal.Yoyo = false;
            blobDeathParticlesOriginal.Delay = 2;
            blobDeathParticlesOriginal.Repeat = true;
            blobDeathParticlesOriginal.AnimateForwards = true;
        }

        public override void LoadContent()
        {
            blobFragmentBounceSparks = gameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks", enParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

            for (int a = 0; a < bps.Count; a++)
            {
                bps[a].Fireball = gameEngine.ContentResourceManager.GetParticleEffectInstance("BlobFragmentExplosion", enParticleEffectType.Continual, null) as IStandardParticleEffect;
                bps[a].SparkEffect = blobFragmentBounceSparks;
            }
        }

        public void Trigger(int EffectXPosition, int EffectYPosition, bool DrawBlobParts, int EffectMinDuration = 50, int EffectMaxDuration = 100)
        {
            drawBlobParts = DrawBlobParts;

            for (int a = 0; a < bps.Count; a++)
            {
                bps[a].Trigger(EffectXPosition, EffectYPosition, EffectMinDuration, EffectMaxDuration);
            }
        }

        public override void Update(GameTime gameTime)
        {
            for (int a = 0; a < bps.Count; a++)
            {
                if (bps[a].Triggered)
                {
                    bps[a].Update(gameTime);
                }
            }

            if (gameEngine.UsingOldGraphics)
            {
                blobDeathParticlesOriginal.GetNextFrame();
            }

            if (blobFragmentBounceSparks != null && blobFragmentBounceSparks.EffectActive)
            {
                blobFragmentBounceSparks.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime, int screenFade)
        {
            if (gameEngine.UsingOldGraphics)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

                for (int a = 0; a < bps.Count; a++)
                {
                    if (bps[a].Triggered)
                    {
                        bps[a].Draw(gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original"), blobDeathParticlesOriginal.CurrentFrame);
                    }
                }

                gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
            }
            else
            {
                for (int a = 0; a < bps.Count; a++)
                {
                    if (bps[a].Triggered)
                    {
                        if (drawBlobParts)
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobParts").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                            bps[a].Draw(gameEngine.ContentResourceManager.GetSpriteCollection("BlobParts"));
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobParts").End();
                        }
                        else
                        {
                            bps[a].Draw(null);
                        }
                    } 
                }

                if (blobFragmentBounceSparks != null && blobFragmentBounceSparks.EffectActive)
                {
                    blobFragmentBounceSparks.Draw(screenFade);
                }
            }
        }
    }
}
