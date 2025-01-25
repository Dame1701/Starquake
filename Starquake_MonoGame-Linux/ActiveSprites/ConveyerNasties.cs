using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StarquakeXNA.Particles;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public class ConveyerNasties : GameComponentBase
    {
        #region IGameComponent Members

        public event RaiseInteractionWithBlob OnNastyInteractionWithBlob;

        List<ConveyerNasty> nasties;
        public List<ConveyerNasty> Nasties
        {
            get { return nasties; }
        }

        public int NastiesActive
        {
            get
            {
                if (gameEngine.GameScreens.GetCoordsOfTiles(391) != null)
                {
                    return gameEngine.GameScreens.GetCoordsOfTiles(391).Count;
                }

                return 0;
            }
        }

        public ConveyerNasties(GameEngine GameEngineReference) : base(GameEngineReference)
        {
        }

        public void LoadContent(StarquakeXNA.ContentManagement.ContentList Content)
        {
            nasties = new List<ConveyerNasty>();

            // Create 3 nasties that we will keep re-using
            for (int a = 0; a < 3; a++)
            {
                ConveyerNasty newNasty = 
                    new ConveyerNasty(gameEngine,
                        0,
                        0, 
                        0,
                        false);

                newNasty.LoadContent(Content);
                nasties.Add(newNasty);

            }
        }

        public void Initialise(List<EffectSprite> EffectSprites)
        {
            //for (int a = 0; a < nasties.Count; a++)
            //{
            //    nasties[a].Initialise(null);
            //}
        }

        public void PerformPostChangeScreenActions()
        {
            // See if we need to create some nasties and get on with it

            // Deactivate all nasties first
            for (int a = 0; a < nasties.Count; a++)
            {
                nasties[a].PerformPostChangeScreenActions();
            }

            List<Point> NastiesToGenerate = gameEngine.GameScreens.GetCoordsOfTiles(391);


            if (NastiesToGenerate !=null && NastiesToGenerate.Count > 0)
            {
                // We have some..... So reactivate a few or all...
                for (int a = 0; a < NastiesToGenerate.Count; a++)
                {
                    nasties[a].NastyActive = true;
                    nasties[a].XLocation = NastiesToGenerate[a].X;
                    nasties[a].YLocation = NastiesToGenerate[a].Y;
                    nasties[a].MoveSpeed = gameEngine.RandomNumberGenerator.Next(1, 3);

                    if (nasties[a].MoveSpeed == 1)
                    {
                        nasties[a].AnimationDelay = 6;
                    }
                    else if (nasties[a].MoveSpeed == 2)
                    {
                        nasties[a].AnimationDelay = 4;
                    }
                    else if (nasties[a].MoveSpeed == 3)
                    {
                        nasties[a].AnimationDelay = 2;
                    }

                    nasties[a].Initialise(null);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            for (int a = 0; a < nasties.Count; a++)
            {
                nasties[a].Update(gameTime);

                // Check for a collision between a nasty and Blob and raise an event
                // Since we have a reference to Blob directly, we can send the call to animate contact directly
                // We raise the event to affect the status panel
                // Going to move the offsets etc. in a bit so it's not too precise and unfair!
                if (nasties[a].NastyState == enNastyState.Normal)
                {
                    if (CollisionDetection.Check(
                        gameEngine.Player.BlobXPos + 8,
                        gameEngine.Player.BlobYPos - 32,
                        gameEngine.Player.BlobWidth - 16,
                        64,
                        nasties[a].XLocation,
                        nasties[a].YLocation,
                        64,
                        64))
                    {
                        // Raise the event
                        if (nasties[a].NastyActive)
                        {
                            if (OnNastyInteractionWithBlob != null)
                            {
                                OnNastyInteractionWithBlob(enNastyInterationType.HitBlob, enNastyType.Conveyer);
                            }

                            // Trigger the blob animation
                            gameEngine.Player.BlobTouchedByNasty = true;
                        }
                    }

                    // Has blob shot a nasty
                    if (gameEngine.Player.BlobHasFired)
                    {
                        if (CollisionDetection.Check(
                            gameEngine.Player.BlobFireX,
                            gameEngine.Player.BlobFireY,
                            32,
                            32,
                            nasties[a].XLocation,
                            nasties[a].YLocation,
                            64,
                            64))
                        {
                            if (nasties[a].NastyActive)
                            {
                                gameEngine.Player.RegisterFireHit();
                                nasties[a].NastyState = enNastyState.Shot;

                                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobExplodes");

                                if (OnNastyInteractionWithBlob != null)
                                {
                                    OnNastyInteractionWithBlob(enNastyInterationType.BlobShot, enNastyType.Conveyer);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime, int ScreenFade)
        {
            for (int a = 0; a < nasties.Count; a++)
            {
                if (nasties[a].NastyActive || nasties[a].NastyState == enNastyState.Dead)
                {
                    nasties[a].Draw(gameTime, ScreenFade);
                }
            }
        }

        #endregion

    }

    public class ConveyerNasty : GameComponentBase
    {
        int screenShakeXOffset = 0;
        int screenShakeYOffset = 0;

        int xLocation;
        public int XLocation
        {
            get { return xLocation; }
            set { xLocation = value; }
        }

        enNastyState nastyState;
        public enNastyState NastyState
        {
            get { return nastyState; }
            set { nastyState = value; }
        }

        int yLocation;
        public int YLocation
        {
            get { return yLocation; }
            set { yLocation = value; }
        }

        int moveSpeed;
        public int MoveSpeed
        {
            get { return moveSpeed; }
            set { moveSpeed = value; }
        }

        bool nastyActive;
        public bool NastyActive
        {
            get { return nastyActive; }
            set { nastyActive = value; }
        }

        Animation anim;
        public int AnimationDelay
        {
            get { return anim.Delay; }
            set { anim.Delay = value; }
        }

        int deathFadeLevel = 255;
        float deathScale = 1f;

        #region IGameComponent Members

        SpriteCollection nastySprite;
        IStandardParticleEffect conveyerNastyDies;

        enDirection nastyDirection;

        public ConveyerNasty(
            GameEngine GameEngineReference,
            int Xposition,
            int Yposition,
            int MoveSpeed,
            bool Active) : base(GameEngineReference)
        {
            xLocation = Xposition;
            yLocation = Yposition;
            moveSpeed = MoveSpeed;
            nastyActive = Active;
            anim = new Animation();
            anim.FirstFrame = 0;
            anim.LastFrame = 5;
            anim.Repeat = true;
            anim.Yoyo = false;
            anim.Delay = 2;
            anim.AnimateForwards = true;
            nastyState = enNastyState.Conception;

            deathFadeLevel = 255;
            deathScale = 1f;
        }

        public void LoadContent(StarquakeXNA.ContentManagement.ContentList Content)
        {
            nastySprite = new SpriteCollection(gameEngine, gameEngine.ContentMgr, Content.GetContent("conveyerNasty"), 64, 64);
            conveyerNastyDies = gameEngine.ContentResourceManager.GetParticleEffectInstance("NastyDies", enParticleEffectType.Triggered, null) as IStandardParticleEffect;
            if (conveyerNastyDies != null)
            {
                conveyerNastyDies.ParticleEffectComplete += new RaiseEffectComplete(conveyerNastyDies_ParticleEffectComplete);
            }
        }

        void conveyerNastyDies_ParticleEffectComplete()
        {
            nastyActive = false;
        }

        public void Initialise(List<EffectSprite> EffectSprites)
        {
            nastyState = enNastyState.Conception;
            nastyActive = true;

            int randomDirection = gameEngine.RandomNumberGenerator.Next(0, 2);

            if (randomDirection == 0)
            {
                nastyDirection = enDirection.Left;
            }
            else
            {
                nastyDirection = enDirection.Right;
            }
        }

        public void PerformPostChangeScreenActions()
        {
            if (conveyerNastyDies != null)
            {
                conveyerNastyDies.Stop();
            }

            nastyActive = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (conveyerNastyDies != null)
            {
                // Update the explosion
                conveyerNastyDies.Update(xLocation + screenShakeXOffset + 32, yLocation + screenShakeYOffset + 64, gameTime);
            }

            if (nastyState == enNastyState.Conception)
            {
                deathFadeLevel = 255;
                deathScale = 1f;
                nastyState = enNastyState.Normal;
            }
            else if (nastyState == enNastyState.Normal)
            {
                if (nastyDirection == enDirection.Left)
                {
                    int amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Left, xLocation, yLocation + 32, 64, 64, moveSpeed, false);
                    if (amountToMove > 0)
                    {
                        xLocation = xLocation - amountToMove;
                    }
                    else
                    {
                        nastyDirection = enDirection.Right;
                        anim.AnimateForwards = true;
                    }
                }
                else if (nastyDirection == enDirection.Right)
                {
                    int amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Right, xLocation, yLocation + 32, 64, 64, moveSpeed, false);
                    if (amountToMove > 0)
                    {
                        xLocation = xLocation + amountToMove;
                    }
                    else
                    {
                        nastyDirection = enDirection.Left;
                        anim.AnimateForwards = false;
                    }
                }
                anim.GetNextFrame();
            }
            else if (nastyState == enNastyState.Shot)
            {
                deathFadeLevel = 255;
                deathScale = 1.0f;

                if (conveyerNastyDies != null)
                {
                    conveyerNastyDies.Start();
                }

                nastyState = enNastyState.Dead;
                nastyActive = false;
            }
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime, int ScreenFade)
        {
            if (nastyState == enNastyState.Normal)
            {
                nastySprite.Begin(BlendState.AlphaBlend, gameTime);
                nastySprite.DrawSprite(anim.CurrentFrame, xLocation, yLocation + 32,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                nastySprite.End();
            }
            else if (nastyState == enNastyState.Dead)
            {
                // Draw the explosion
                if (conveyerNastyDies != null)
                {
                    conveyerNastyDies.Draw();
                }

                // Fade the nasty out as it explodes
                if (deathFadeLevel > 0)
                {
                    deathFadeLevel = deathFadeLevel - 20;
                }

                if (deathFadeLevel < 0)
                {
                    deathFadeLevel = 0;
                }

                // Shrink the nasty as it explodes
                if (deathScale > 0)
                {
                    deathScale = deathScale - 0.01f;
                }

                if (deathScale < 0)
                {
                    deathScale = 0;
                }

                if (deathFadeLevel > 0)
                {
                    nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    nastySprite.DrawSprite(anim.CurrentFrame, xLocation, yLocation + 32,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)((double)deathFadeLevel * (double)((double)ScreenFade / 255))), deathScale, 0f);
                    nastySprite.End();
                }
            }
        }

        #endregion
    }
}
