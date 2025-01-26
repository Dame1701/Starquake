using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA;
using StarquakeXNA.GameComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarquakeXNA.Structure;

namespace Starquake_XNA.Animation
{
    public class IntroSequence : GameComponentBase
    {
        public bool SequenceRunning;

        public int BlobSpaceShipXPos
        {
            get { return (int)blobSpaceShipXPos + (gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).SpriteWidth / 2); }
        }

        public int BlobSpaceShipYPos
        {
            get { return (int)blobSpaceShipYPos + (gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).SpriteHeight / 2); }
        }

        int blobSpaceShipStartXPos;
        int blobSpaceShipStartYPos;

        int blobSpaceShipEndXPos;
        int blobSpaceShipEndYPos;

        float blobSpaceShipXPos, blobSpaceShipYPos;

        float yAmountToMoveEachFrame;
        float xAmountToMoveEachFrame;
        float acceleration = 0;

        int sequenceFrameCount;
        int sequenceCounter;

        int PreFadeOutPause = 50;
        int PreFadeOutPauseCounter = 0;

        int StartPauseFrameCount = 0;
        int StartPauseCounter = 0;

        int EndPauseFrameCount = 0;
        int EndPauseFrameCounter = 0;
        
        float ShipRotationSpeed;

        double RumbleAmount = 100;

        Guid ShipExplosionSound = Guid.Empty;
        Guid RumbleSound = Guid.Empty;

        MainGameScreen mainScreen = null;

        FaderAnimation blobShipHazardLightFlash;
        RandomFaderAnimation blobShipHeadLightFlash;

        enIntroSequenceStage sequenceStage = enIntroSequenceStage.None;
        public enIntroSequenceStage SequenceStage
        {
            get { return sequenceStage; }
        }

        public void Reset(MainGameScreen MainScreen)
        {
            if (mainScreen == null)
            {
                mainScreen = MainScreen;
            }

            mainScreen.ScreenFade = 255;

            RumbleAmount = 100;

            PreFadeOutPause = 50;
            PreFadeOutPauseCounter = 0;

            StartPauseFrameCount = 50;
            StartPauseCounter = 0;

            EndPauseFrameCount = 200;
            EndPauseFrameCounter = 0;

            sequenceStage = enIntroSequenceStage.InitialPause;
            SequenceRunning = true;

            ShipRotationSpeed = 10f;
            sequenceFrameCount = 20;

            blobSpaceShipStartXPos = 200;
            blobSpaceShipStartYPos = -160;

            blobSpaceShipEndXPos = 610;
            blobSpaceShipEndYPos = 280;

            xAmountToMoveEachFrame = ((float)blobSpaceShipEndXPos - (float)blobSpaceShipStartXPos) / (float)sequenceFrameCount;
            yAmountToMoveEachFrame = ((float)blobSpaceShipEndYPos - (float)blobSpaceShipStartYPos) / (float)sequenceFrameCount;

            blobSpaceShipXPos = blobSpaceShipStartXPos;
            blobSpaceShipYPos = blobSpaceShipStartYPos;
            acceleration = 0;

            sequenceCounter = 0;

            gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(ShipExplosionSound, SoundFade.None);

            gameEngine.GameScreens.BlobsShipSmoke1.Reset();
            gameEngine.GameScreens.BlobsShipSmoke2.Reset();
        }

        public IntroSequence(GameEngine GameEngine) : base (GameEngine) 
        {
            blobShipHazardLightFlash = new FaderAnimation(10);
            blobShipHeadLightFlash = new RandomFaderAnimation(4, gameEngine.RandomNumberGenerator, 100);
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if (sequenceStage == enIntroSequenceStage.InitialPause)
            {
                if (StartPauseCounter < StartPauseFrameCount)
                {
                    if (StartPauseCounter == 0)
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobShipFall");
                    }

                    StartPauseCounter++;
                }
                else
                {
                    sequenceStage = enIntroSequenceStage.ShipFalling;
                }
            }
            if (sequenceStage == enIntroSequenceStage.ShipFalling)
            {
                //if (sequenceCounter < sequenceFrameCount)
                if (blobSpaceShipYPos < blobSpaceShipEndYPos)
                {
                    blobSpaceShipXPos = blobSpaceShipXPos + xAmountToMoveEachFrame;
                    blobSpaceShipYPos = blobSpaceShipYPos + (yAmountToMoveEachFrame * acceleration);

                    sequenceCounter++;

                    if (blobSpaceShipYPos + 100 > blobSpaceShipEndYPos)
                    {
                        acceleration = 1f;
                    }
                    else
                    {
                        acceleration = acceleration + 0.1f;
                    }
                }
                else
                {
                    sequenceStage = enIntroSequenceStage.ShipExploading;
                    gameEngine.BlobKiller.Trigger(blobSpaceShipEndXPos, blobSpaceShipEndYPos, false, 100, 200);
                }
            }
            else if (sequenceStage == enIntroSequenceStage.ShipExploading)
            {
                sequenceStage = enIntroSequenceStage.PreFadePause;
                ShipExplosionSound = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobShipExplodes");
                gameEngine.ScreenClearColour = Color.White;
            }
            else if (sequenceStage == enIntroSequenceStage.PreFadePause)
            {
                gameEngine.ScreenClearColour = Color.Black;
                if (ShipRotationSpeed > 0)
                {
                    ShipRotationSpeed = ShipRotationSpeed - 2;
                }

                if (PreFadeOutPauseCounter >= PreFadeOutPause)
                {
                    if (mainScreen.ScreenFade > 0)
                    {
                        if (mainScreen.ScreenFade - 2 < 0)
                        {
                            mainScreen.ScreenFade = 0;
                        }
                        else
                        {
                            mainScreen.ScreenFade = mainScreen.ScreenFade - 2;
                        }
                    }
                    else
                    {
                        mainScreen.ResetScreenFade(2);
                        mainScreen.ScreenFade = 0;
                        sequenceStage = enIntroSequenceStage.CaptionFadeUp;
                    }
                }
                else
                {
                    PreFadeOutPauseCounter++;
                }

                if (RumbleAmount > 0)
                {
                    int XShakeOffset = gameEngine.RandomNumberGenerator.Next(0, (int)RumbleAmount);
                    int YShakeOffset = gameEngine.RandomNumberGenerator.Next(0, (int)RumbleAmount);

                    RumbleAmount = RumbleAmount - 0.5;

                    gameEngine.ScreenEffctEngine.MoveViewPort("GameArea", XShakeOffset, YShakeOffset);

                    // Play rumble sound
                    RumbleSound = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("Rumble", true);
                }

                gameEngine.BlobKiller.Update(gameTime);
            }
            else if (sequenceStage == enIntroSequenceStage.CaptionFadeUp)
            {
                if (mainScreen.ScreenFade < 255)
                {
                    if (mainScreen.ScreenFade + 3 < 255)
                    {
                        mainScreen.ScreenFade = mainScreen.ScreenFade + 3;
                    }
                    else
                    {
                        mainScreen.ScreenFade = 255;
                    }
                }
                else
                {
                    sequenceStage = enIntroSequenceStage.CaptionFadeDown;
                }
            }
            else if (sequenceStage == enIntroSequenceStage.CaptionFadeDown)
            {
                if (mainScreen.ScreenFade > 0)
                {
                    if (mainScreen.ScreenFade - 3 > 0)
                    {
                        mainScreen.ScreenFade = mainScreen.ScreenFade - 3;
                    }
                    else
                    {
                        mainScreen.ScreenFade = 0;
                    }
                }
                else
                {
                    sequenceStage = enIntroSequenceStage.EndPause;
                }
            }
            else if (sequenceStage == enIntroSequenceStage.EndPause || sequenceStage == enIntroSequenceStage.Finished)
            {
                if (sequenceStage == enIntroSequenceStage.EndPause)
                {
                    if (EndPauseFrameCounter < EndPauseFrameCount)
                    {
                        EndPauseFrameCounter++;
                    }
                    else
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(RumbleSound, SoundFade.Fast);
                        sequenceStage = enIntroSequenceStage.Finished;
                        SequenceRunning = false;
                    }
                }

                blobShipHazardLightFlash.Update();
                blobShipHeadLightFlash.Update();
            }

        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            // Draw spaceship
            if (sequenceStage != enIntroSequenceStage.CaptionFadeUp && sequenceStage != enIntroSequenceStage.CaptionFadeDown)
            {
                
               
                if (gameEngine.UsingOldGraphics)
                {   
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).Begin(BlendState.AlphaBlend, gameTime);
                    if (sequenceStage == enIntroSequenceStage.Finished || sequenceStage == enIntroSequenceStage.CaptionFadeUp || sequenceStage == enIntroSequenceStage.EndPause)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).DrawSprite(0, 640, 288, new Color(255, 255, 255, mainScreen.ScreenFade));
                    }
                    else
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).DrawSprite(0, blobSpaceShipXPos, blobSpaceShipYPos, new Color(255, 255, 255, mainScreen.ScreenFade), 1.0f, ShipRotationSpeed, true, false);
                    }

                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).End();
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).DrawSprite(0, blobSpaceShipXPos, blobSpaceShipYPos, new Color(255, 255, 255, mainScreen.ScreenFade), 1.0f, ShipRotationSpeed, true, false);                
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).End();

                    if (sequenceStage != enIntroSequenceStage.Finished && sequenceStage != enIntroSequenceStage.EndPause)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").Begin(BlendState.AlphaBlend, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").DrawSprite(0, blobSpaceShipXPos, blobSpaceShipYPos, new Color(255, 255, 255, 0), 1.0f, ShipRotationSpeed, true, false);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").End();

                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").Begin(BlendState.AlphaBlend, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").DrawSprite(0, blobSpaceShipXPos, blobSpaceShipYPos, new Color(255, 255, 255, mainScreen.ScreenFade), 1.0f, ShipRotationSpeed, true, false);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").End();
                    }
                    else
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").Begin(BlendState.AlphaBlend, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").DrawSprite(0, blobSpaceShipXPos, blobSpaceShipYPos, new Color(255, 255, 255, (int)(blobShipHazardLightFlash.FadeLevel * ((double)mainScreen.ScreenFade / 255))), 1.0f, ShipRotationSpeed, true, false);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").End();

                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").Begin(BlendState.AlphaBlend, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").DrawSprite(0, blobSpaceShipXPos, blobSpaceShipYPos, new Color(255, 255, 255, (int)(blobShipHeadLightFlash.FadeLevel * ((double)mainScreen.ScreenFade / 255))), 1.0f, ShipRotationSpeed, true, false);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").End();
                    }
                }
               
                if (sequenceStage == enIntroSequenceStage.PreFadePause)
                {
                    gameEngine.BlobKiller.Draw(gameTime, mainScreen.ScreenFade);
                }
            }
            else 
            {
                gameEngine.Font2.Begin(BlendState.AlphaBlend);
                gameEngine.Font2.WriteText(180, 210, "Prepare for mission Starquake ...", 2, Color.White, (byte)mainScreen.ScreenFade);
                gameEngine.Font2.End();
            }
        }
    }
}
