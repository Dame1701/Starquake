// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA.GameComponents;

public class MainGameScreen : GameComponentBase, IGameScreen
{

    private DateTime gameStartedTime;

    private Guid rumbleSoundID = Guid.Empty;

    private bool shakingScreen;
    private int shakingScreenCount;
    private int shakingScreenCounter;

    public int ScreenFadeSpeed { get; set; }

    public bool Success => true;

    public int ScreenFade { get; set; }

    public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
    {
        gameEngine.ContentResourceManager.GetSoundFXEngine().DisablePeriodicBackgroundSounds();

        if (TriggeringScreen != enGameScreen.AccessBooth && TriggeringScreen != enGameScreen.Cheops)
        {
            gameEngine.Player.PerformPostChangeScreenActions();
            gameEngine.ConveyerNastiesInstance.PerformPostChangeScreenActions();
            gameEngine.Nasties.PerformPostChangeScreenActions();

            gameEngine.ElectricityNodeGateEngine.PerformPostChangeScreenActions();
            gameEngine.ContentResourceManager.GetSoundFXEngine().KillSounds(false);

            // Initialise the Tunnel lift animations
            gameEngine.TunnelAnimations.PerformPostChangeScreenActions();
        }

        if (Param == null)
        {
            ScreenFade = 0;
            ScreenFadeSpeed = 32;
        }

        // Clear the viewport
        if (TriggeringScreen == enGameScreen.MainGame)
        {
            // Reset viewport back to game area
            gameEngine.ScreenEffctEngine.SelectRenderViewPort("GameArea");
        }

        // Update score for each new screen visited
        if (!gameEngine.GameScreens.CurrentGameScreenData.ScreenVisited)
        {
            gameEngine.GameScreens.CurrentGameScreenData.ScreenVisited = true;
            gameEngine.BlobStatusPanel.PlayerScore += 250;
        }

        if (gameEngine.GameScreens.ScreenNumber == 199)
        {
            gameEngine.CurrentGameScreen = enGameScreen.PlanetCore;
        }

        if (TriggeringScreen == enGameScreen.Teleporter)
        {
            ScreenFadeSpeed = 16;
            gameEngine.Player.TriggerBlobTeleportedAnimation();
            gameEngine.ColourAnimationEngine.Start(397, 1); // Animate the teleporter colour
        }

        if (TriggeringScreen == enGameScreen.AccessBooth)
        {
            // Test colour animation engine
            if (Param != null)
            {
                var success = (bool)Param;

                if (success)
                {
                    gameEngine.ColourAnimationEngine.Start(399, 2, new Color(0, 255, 0), 2);
                    gameEngine.ColourAnimationEngine.Start(400, 2, new Color(0, 255, 0), 2);
                }
                else
                {
                    gameEngine.ColourAnimationEngine.Start(399, 2, new Color(255, 0, 0), 3);
                    gameEngine.ColourAnimationEngine.Start(400, 2, new Color(255, 0, 0), 3);
                }
            }
        }
    }

    public TimeSpan GetPlayTime()
        => DateTime.Now - gameStartedTime;

    public void SpritesChanged()
    {
        // Initialise the Tunnel lift animations
        gameEngine.TunnelAnimations.PerformPostChangeScreenActions();

        ScreenFade = 0;
        ScreenFadeSpeed = 12;
    }

    #region IGameComponent Members

    public MainGameScreen(GameEngine GameEngineReference)
        : base(GameEngineReference)
    {
        ScreenFade = 255;
    }

    public override void LoadContent()
    {
        gameEngine.Nasties.Initialise(null);

        var nastyGraphics = new ContentList(gameEngine.ContentMgr);
        nastyGraphics.AddContent("nastySprite", "Graphics\\nastiesNew");
        gameEngine.Nasties.LoadContent(nastyGraphics);
    }

    // Resets the game back to defaults
    public void Reset(object Data)
    {
        gameStartedTime = DateTime.Now;
        gameEngine.Paused = false;
        gameEngine.BlobStatusPanel.Reset();
        gameEngine.Elements.Reset();
        gameEngine.GameScreens.Reset();
        gameEngine.Player.Reset();
        gameEngine.Nasties.Reset();
        gameEngine.IntroSequence.Reset(this);

        PerformPostScreenChangeActions(enGameScreen.None, false);
    }

    private Guid RumbleSoundID;

    public override void Update(GameTime gameTime)
    {
        if (gameEngine.InTestMode)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.J))
            {
                gameEngine.BlobStatusPanel.Platforms = 60;
                gameEngine.BlobStatusPanel.FirePower = 60;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.T))
            {
                RumbleSoundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("Rumble", true);
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.R))
            {
                if (RumbleSoundID != Guid.Empty)
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine()
                        .StopSound(RumbleSoundID, SoundFade.Medium);
                }
            }

            // Test quit to game over screen
            if (ToggleKeys.GetPressedKey() == gameEngine.PlayerKeys.Quit)
            {
                gameEngine.CurrentGameScreen = enGameScreen.GameOver;
                gameEngine.GetGameScreen(enGameScreen.GameOver)
                    .PerformPostScreenChangeActions(enGameScreen.MainGame, null);
            }

            // Test game complete screen
            if (ToggleKeys.GetPressedKey() == Keys.W)
            {
                gameEngine.CurrentGameScreen = enGameScreen.GameComplete;
                gameEngine.GetGameScreen(enGameScreen.GameComplete)
                    .PerformPostScreenChangeActions(enGameScreen.MainGame, null);
            }

            // Test completing core
            if (ToggleKeys.GetPressedKey() == Keys.C)
            {
                for (var a = 0; a < gameEngine.Elements.Elements.Length; a++)
                {
                    gameEngine.Elements.Elements[a].ElementState = CoreElementState.Replaced;
                }
            }

            // Test intro sequence
            if (ToggleKeys.GetPressedKey() == Keys.I)
            {
                gameEngine.IntroSequence.Reset(this);
            }

            // Test Unstick routine
            if (ToggleKeys.GetPressedKey() == Keys.U)
            {
                gameEngine.Player.UnstickBlobFromTiles();
            }
        }

        // Fade screen back up
        if (!gameEngine.IntroSequence.SequenceRunning ||
            gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
        {
            if (ScreenFade < 255)
            {
                ScreenFade = ScreenFade + ScreenFadeSpeed;
            }

            if (ScreenFade > 255)
            {
                ScreenFade = 255;
            }
        }

        // Update the screen effect engine
        gameEngine.ScreenEffctEngine.Update();
        gameEngine.GameScreens.Update(gameTime);

        if (!gameEngine.IntroSequence.SequenceRunning ||
            gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
        {
            if (!gameEngine.ScreenEffctEngine.HaltBlobAndNasties)
            {
                gameEngine.Player.Update(gameTime);
            }

            // Update blob's death routine
            if (gameEngine.Player.BlobsState == enBlobState.Dead ||
                gameEngine.Player.BlobsState == enBlobState.Dying ||
                gameEngine.Player.BlobsState == enBlobState.Overloading)
            {
                gameEngine.BlobKiller.Update(gameTime);
            }
        }

        // Update Blob's ship smoke
        if (gameEngine.GameScreens.ScreenNumber == 8)
        {
            if (gameEngine.GameScreens.BlobsShipSmoke1 != null)
            {
                gameEngine.GameScreens.BlobsShipSmoke1.Start();
                gameEngine.GameScreens.BlobsShipSmoke1.Update(gameEngine.IntroSequence.BlobSpaceShipXPos,
                    gameEngine.IntroSequence.BlobSpaceShipYPos, gameTime);
            }

            if (gameEngine.GameScreens.BlobsShipSmoke2 != null)
            {
                gameEngine.GameScreens.BlobsShipSmoke2.Start();
                gameEngine.GameScreens.BlobsShipSmoke2.Update(gameEngine.IntroSequence.BlobSpaceShipXPos,
                    gameEngine.IntroSequence.BlobSpaceShipYPos, gameTime);
            }
        }

        if (!gameEngine.IntroSequence.SequenceRunning ||
            gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
        {
            // Put the delay stuff in here
            if (!gameEngine.IntroSequence.SequenceRunning)
            {
                if (!gameEngine.ScreenEffctEngine.HaltBlobAndNasties)
                {
                    gameEngine.Nasties.Update(gameTime);
                }
            }

            gameEngine.TunnelAnimations.Update(gameTime);
            gameEngine.ConveyerNastiesInstance.Update(gameTime);
            gameEngine.ElectricityNodeGateEngine.Update();

            // Update breakable bridges
            gameEngine.BreakableBridgesEngine.Update();

            // No lives left - so finish the game
            if (gameEngine.Player.BlobsState == enBlobState.Gameover)
            {
                // Disable the periodic sound engine and kill all soudns immediately
                gameEngine.ContentResourceManager.GetSoundFXEngine().DisablePeriodicBackgroundSounds(true);

                // Change to gameover screen
                gameEngine.GetGameScreen(enGameScreen.GameOver)
                    .PerformPostScreenChangeActions(enGameScreen.MainGame, null);
                gameEngine.CurrentGameScreen = enGameScreen.GameOver;
            }

            // Randomly decide when to shake the screen
            var XShakeOffset = 0;
            var YShakeOffset = 0;

            if (!gameEngine.IntroSequence.SequenceRunning)
            {
                var shakingScreenTime = gameEngine.RandomNumberGenerator.Next(0, 2000);

                if (shakingScreenTime == 1 && !shakingScreen)
                {
                    shakingScreenCount = gameEngine.RandomNumberGenerator.Next(180, 300);
                    shakingScreen = true;
                }
            }

            if (gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
            {
                shakingScreenCount = 500;
                shakingScreen = true;
            }

            // Shake the screen, but only if a screen effect is not active
            if (gameEngine.ScreenEffctEngine.ActiveScreenEffect == null)
            {
                if (shakingScreen)
                {
                    XShakeOffset = gameEngine.RandomNumberGenerator.Next(0, 5);
                    YShakeOffset = gameEngine.RandomNumberGenerator.Next(0, 5);

                    shakingScreenCounter++;

                    // Play rumble sound
                    rumbleSoundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("Rumble", true);

                    if (shakingScreenCounter > shakingScreenCount)
                    {
                        shakingScreenCounter = 0;
                        shakingScreen = false;
                    }
                }
                else
                {
                    if (XShakeOffset > 0)
                    {
                        XShakeOffset = XShakeOffset - 1;
                    }

                    if (YShakeOffset > 0)
                    {
                        YShakeOffset = YShakeOffset - 1;
                    }

                    gameEngine.ContentResourceManager.GetSoundFXEngine()
                        .StopSound(rumbleSoundID, SoundFade.Medium);
                }

                if (YShakeOffset > 0 && XShakeOffset > 0)
                {
                    gameEngine.ScreenEffctEngine.MoveViewPort("GameArea", XShakeOffset, YShakeOffset);
                }
            }
        }

        if (gameEngine.IntroSequence.SequenceRunning ||
            gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.Finished)
        {
            gameEngine.IntroSequence.Update(gameTime);
        }
    }

    public void ResetScreenFade(int FadeSpeed)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = FadeSpeed;
    }

    public override void Draw(GameTime gameTime)
    {
        if (gameEngine.IntroSequence.SequenceStage != enIntroSequenceStage.CaptionFadeUp &&
            gameEngine.IntroSequence.SequenceStage != enIntroSequenceStage.CaptionFadeDown)
        {
            // Draw background
            if (gameEngine.DrawBackgrounds)
            {
                var ScreenXPos = gameEngine.GameScreens.ScreenNumber % 16;

                gameEngine.ContentResourceManager.GetSpriteCollection("Background1")
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("Background1").DrawSprite(0, ScreenXPos * 16,
                    0, new Color((byte)255, (byte)255, (byte)255, (byte)(150 / 255 * ScreenFade)));
                gameEngine.ContentResourceManager.GetSpriteCollection("Background1").End();
            }

            // Draw electricity nodes etc lower layer
            gameEngine.ElectricityNodeGateEngine.Draw(gameTime, ScreenFade);

            // Draw screen
            gameEngine.GameScreens.Draw(
                ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade),
                gameTime, gameEngine.IntroSequence.SequenceRunning, gameEngine.IntroSequence.SequenceStage);

            // Draw breakable bridges
            gameEngine.BreakableBridgesEngine.Draw(gameTime);

            if (!gameEngine.IntroSequence.SequenceRunning ||
                gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
            {
                // Draw blob
                gameEngine.Player.Draw(
                    gameTime,
                    ScreenFade);
            }

            if (!gameEngine.IntroSequence.SequenceRunning)
            {
                // Draw blob's death routine
                if (gameEngine.Player.BlobsState is enBlobState.Dead or enBlobState.Dying or enBlobState.Overloading)
                {
                    gameEngine.BlobKiller.Draw(gameTime, ScreenFade);
                }

                // Draw nasties
                gameEngine.Nasties.Draw(
                    gameTime,
                    ScreenFade);

                gameEngine.ConveyerNastiesInstance.Draw(
                    gameTime,
                    ScreenFade);
            }

            // Draw upper graphics
            gameEngine.ElectricityNodeGateEngine.DrawUpperLayer(gameTime,
                ScreenFade);

            gameEngine.GameScreens.DrawUpperLayer(
                ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade),
                gameTime);

            // Draw tunnel animations
            gameEngine.TunnelAnimations.Draw(gameTime, (byte)ScreenFade);

            // Draw blob's ship smoke
            if (gameEngine.GameScreens.ScreenNumber == 8)
            {
                if (gameEngine.GameScreens.BlobsShipSmoke1 != null)
                {
                    gameEngine.GameScreens.BlobsShipSmoke1.Draw(ScreenFade);
                }
            }

            if (!gameEngine.IntroSequence.SequenceRunning ||
                gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
            {
                // Screen Exit shadows
                gameEngine.GameScreens.DrawShadowing(gameTime);
            }

            // Draw any sprites/effects that must go in front of shadows etc.
            gameEngine.Player.DrawUpper(ScreenFade);
        }

        if (gameEngine.GameScreens.ScreenNumber == 8)
        {
            gameEngine.IntroSequence.Draw(gameTime);
        }

        // Draw blob's ship smoke
        if (gameEngine.IntroSequence.SequenceStage != enIntroSequenceStage.CaptionFadeUp &&
            gameEngine.IntroSequence.SequenceStage != enIntroSequenceStage.CaptionFadeDown)
        {
            if (gameEngine.GameScreens.ScreenNumber == 8)
            {
                if (gameEngine.GameScreens.BlobsShipSmoke2 != null)
                {
                    gameEngine.GameScreens.BlobsShipSmoke2.Draw(ScreenFade);
                }
            }
        }
    }

    #endregion

}