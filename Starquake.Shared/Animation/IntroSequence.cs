// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA;
using StarquakeXNA.GameComponents;
using StarquakeXNA.Structure;

namespace Starquake_XNA.Animation;

public class IntroSequence : GameComponentBase
{

    private readonly FaderAnimation blobShipHazardLightFlash;
    private readonly RandomFaderAnimation blobShipHeadLightFlash;
    private float acceleration;

    private int blobSpaceShipEndXPos;
    private int blobSpaceShipEndYPos;

    private int blobSpaceShipStartXPos;
    private int blobSpaceShipStartYPos;

    private float blobSpaceShipXPos, blobSpaceShipYPos;

    private int EndPauseFrameCount;
    private int EndPauseFrameCounter;

    private MainGameScreen mainScreen;

    private int PreFadeOutPause = 50;
    private int PreFadeOutPauseCounter;

    private double RumbleAmount = 100;
    private Guid RumbleSound = Guid.Empty;
    private int sequenceCounter;

    private int sequenceFrameCount;
    public bool SequenceRunning;

    private Guid ShipExplosionSound = Guid.Empty;

    private float ShipRotationSpeed;
    private int StartPauseCounter;

    private int StartPauseFrameCount;
    private float xAmountToMoveEachFrame;

    private float yAmountToMoveEachFrame;

    public IntroSequence(GameEngine GameEngine) : base(GameEngine)
    {
        blobShipHazardLightFlash = new FaderAnimation(10);
        blobShipHeadLightFlash = new RandomFaderAnimation(4, gameEngine.RandomNumberGenerator, 100);
    }

    public int BlobSpaceShipXPos => (int)blobSpaceShipXPos + gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).SpriteWidth / 2;

    public int BlobSpaceShipYPos => (int)blobSpaceShipYPos + gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).SpriteHeight / 2;
    public enIntroSequenceStage SequenceStage { get; private set; } = enIntroSequenceStage.None;

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

        SequenceStage = enIntroSequenceStage.InitialPause;
        SequenceRunning = true;

        ShipRotationSpeed = 10f;
        sequenceFrameCount = 20;

        blobSpaceShipStartXPos = 200;
        blobSpaceShipStartYPos = -160;

        blobSpaceShipEndXPos = 610;
        blobSpaceShipEndYPos = 280;

        xAmountToMoveEachFrame = (blobSpaceShipEndXPos - (float)blobSpaceShipStartXPos) / sequenceFrameCount;
        yAmountToMoveEachFrame = (blobSpaceShipEndYPos - (float)blobSpaceShipStartYPos) / sequenceFrameCount;

        blobSpaceShipXPos = blobSpaceShipStartXPos;
        blobSpaceShipYPos = blobSpaceShipStartYPos;
        acceleration = 0;

        sequenceCounter = 0;

        gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(ShipExplosionSound, SoundFade.None);

        gameEngine.GameScreens.BlobsShipSmoke1.Reset();
        gameEngine.GameScreens.BlobsShipSmoke2.Reset();
    }

    public override void Update(GameTime gameTime)
    {
        if (SequenceStage == enIntroSequenceStage.InitialPause)
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
                SequenceStage = enIntroSequenceStage.ShipFalling;
            }
        }
        if (SequenceStage == enIntroSequenceStage.ShipFalling)
        {
            //if (sequenceCounter < sequenceFrameCount)
            if (blobSpaceShipYPos < blobSpaceShipEndYPos)
            {
                blobSpaceShipXPos = blobSpaceShipXPos + xAmountToMoveEachFrame;
                blobSpaceShipYPos = blobSpaceShipYPos + yAmountToMoveEachFrame * acceleration;

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
                SequenceStage = enIntroSequenceStage.ShipExploading;
                gameEngine.BlobKiller.Trigger(blobSpaceShipEndXPos, blobSpaceShipEndYPos, false, 100, 200);
            }
        }
        else if (SequenceStage == enIntroSequenceStage.ShipExploading)
        {
            SequenceStage = enIntroSequenceStage.PreFadePause;
            ShipExplosionSound = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobShipExplodes");
            gameEngine.ScreenClearColour = Color.White;
        }
        else if (SequenceStage == enIntroSequenceStage.PreFadePause)
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
                    SequenceStage = enIntroSequenceStage.CaptionFadeUp;
                }
            }
            else
            {
                PreFadeOutPauseCounter++;
            }

            if (RumbleAmount > 0)
            {
                var XShakeOffset = gameEngine.RandomNumberGenerator.Next(0, (int)RumbleAmount);
                var YShakeOffset = gameEngine.RandomNumberGenerator.Next(0, (int)RumbleAmount);

                RumbleAmount = RumbleAmount - 0.5;

                gameEngine.ScreenEffctEngine.MoveViewPort("GameArea", XShakeOffset, YShakeOffset);

                // Play rumble sound
                RumbleSound = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("Rumble", true);
            }

            gameEngine.BlobKiller.Update(gameTime);
        }
        else if (SequenceStage == enIntroSequenceStage.CaptionFadeUp)
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
                SequenceStage = enIntroSequenceStage.CaptionFadeDown;
            }
        }
        else if (SequenceStage == enIntroSequenceStage.CaptionFadeDown)
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
                SequenceStage = enIntroSequenceStage.EndPause;
            }
        }
        else if (SequenceStage == enIntroSequenceStage.EndPause || SequenceStage == enIntroSequenceStage.Finished)
        {
            if (SequenceStage == enIntroSequenceStage.EndPause)
            {
                if (EndPauseFrameCounter < EndPauseFrameCount)
                {
                    EndPauseFrameCounter++;
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(RumbleSound, SoundFade.Fast);
                    SequenceStage = enIntroSequenceStage.Finished;
                    SequenceRunning = false;
                }
            }

            blobShipHazardLightFlash.Update();
            blobShipHeadLightFlash.Update();
        }

    }

    public override void Draw(GameTime gameTime)
    {
        // Draw spaceship
        if (SequenceStage != enIntroSequenceStage.CaptionFadeUp && SequenceStage != enIntroSequenceStage.CaptionFadeDown)
        {


            if (gameEngine.UsingOldGraphics)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobsShipName).Begin(BlendState.AlphaBlend, gameTime);
                if (SequenceStage == enIntroSequenceStage.Finished || SequenceStage == enIntroSequenceStage.CaptionFadeUp || SequenceStage == enIntroSequenceStage.EndPause)
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

                if (SequenceStage != enIntroSequenceStage.Finished && SequenceStage != enIntroSequenceStage.EndPause)
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

            if (SequenceStage == enIntroSequenceStage.PreFadePause)
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