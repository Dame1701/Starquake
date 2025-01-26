using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA.GameComponents
{
    public class MainGameScreen : GameComponentBase, IGameScreen
    {
        int screenFadeSpeed;

        public int ScreenFadeSpeed
        {
            get { return screenFadeSpeed; }
            set { screenFadeSpeed = value; }
        }

        public bool Success
        {
            get { return true; }
        }

        private DateTime gameStartedTime;

        public TimeSpan GetPlayTime()
        {
            return DateTime.Now - gameStartedTime;
        }

        int screenFade;

        public int ScreenFade
        {
            get { return screenFade; }
            set { screenFade = value; }
        }

        bool shakingScreen = false;
        int shakingScreenCounter = 0;
        int shakingScreenCount = 0;

        Guid rumbleSoundID = Guid.Empty;

        #region IGameComponent Members

        public MainGameScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenFade = 255;
        }

        public override void LoadContent()
        {
            gameEngine.Nasties.Initialise(null);

            ContentList nastyGraphics = new ContentList(gameEngine.ContentMgr);
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
                    for (int a = 0; a < gameEngine.Elements.Elements.Length; a++)
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
                if (screenFade < 255)
                {
                    screenFade = screenFade + screenFadeSpeed;
                }

                if (screenFade > 255)
                {
                    screenFade = 255;
                }
            }

            // Update the screen effect engine
            gameEngine.ScreenEffctEngine.Update();
            gameEngine.GameScreens.Update(gameTime);

            if (!gameEngine.IntroSequence.SequenceRunning ||
                gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
            {
                if (gameEngine.ScreenEffctEngine.HaltBlobAndNasties == false)
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
                    if (gameEngine.ScreenEffctEngine.HaltBlobAndNasties == false)
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
                int XShakeOffset = 0;
                int YShakeOffset = 0;

                if (!gameEngine.IntroSequence.SequenceRunning)
                {
                    int shakingScreenTime = gameEngine.RandomNumberGenerator.Next(0, 2000);

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
            screenFade = 0;
            screenFadeSpeed = FadeSpeed;
        }

        public override void Draw(GameTime gameTime)
        {
            if (gameEngine.IntroSequence.SequenceStage != enIntroSequenceStage.CaptionFadeUp &&
                gameEngine.IntroSequence.SequenceStage != enIntroSequenceStage.CaptionFadeDown)
            {
                // Draw background
                if (gameEngine.DrawBackgrounds)
                {
                    int ScreenXPos = gameEngine.GameScreens.ScreenNumber % 16;

                    gameEngine.ContentResourceManager.GetSpriteCollection("Background1")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("Background1").DrawSprite(0, ScreenXPos * 16,
                        0, new Color((byte)255, (byte)255, (byte)255, (byte)(150 / 255 * screenFade)));
                    gameEngine.ContentResourceManager.GetSpriteCollection("Background1").End();
                }

                // Draw electricity nodes etc lower layer
                gameEngine.ElectricityNodeGateEngine.Draw(gameTime, screenFade);

                // Draw screen
                gameEngine.GameScreens.Draw(
                    ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade),
                    gameTime, gameEngine.IntroSequence.SequenceRunning, gameEngine.IntroSequence.SequenceStage);

                // Draw breakable bridges
                gameEngine.BreakableBridgesEngine.Draw(gameTime);

                if (!gameEngine.IntroSequence.SequenceRunning ||
                    gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
                {
                    // Draw blob
                    gameEngine.Player.Draw(
                        gameTime,
                        screenFade);
                }

                if (!gameEngine.IntroSequence.SequenceRunning)
                {
                    // Draw blob's death routine
                    if (gameEngine.Player.BlobsState is enBlobState.Dead or enBlobState.Dying or enBlobState.Overloading)
                    {
                        gameEngine.BlobKiller.Draw(gameTime, screenFade);
                    }

                    // Draw nasties
                    gameEngine.Nasties.Draw(
                        gameTime,
                        screenFade);

                    gameEngine.ConveyerNastiesInstance.Draw(
                        gameTime,
                        screenFade);
                }

                // Draw upper graphics
                gameEngine.ElectricityNodeGateEngine.DrawUpperLayer(gameTime,
                    screenFade);

                gameEngine.GameScreens.DrawUpperLayer(
                    ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade),
                    gameTime);

                // Draw tunnel animations
                gameEngine.TunnelAnimations.Draw(gameTime, (byte)screenFade);

                // Draw blob's ship smoke
                if (gameEngine.GameScreens.ScreenNumber == 8)
                {
                    if (gameEngine.GameScreens.BlobsShipSmoke1 != null)
                    {
                        gameEngine.GameScreens.BlobsShipSmoke1.Draw(screenFade);
                    }
                }

                if (!gameEngine.IntroSequence.SequenceRunning ||
                    gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
                {
                    // Screen Exit shadows
                    gameEngine.GameScreens.DrawShadowing(gameTime);
                }

                // Draw any sprites/effects that must go in front of shadows etc.
                gameEngine.Player.DrawUpper(screenFade);
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
                        gameEngine.GameScreens.BlobsShipSmoke2.Draw(screenFade);
                    }
                }
            }
        }

        #endregion

        public void SpritesChanged()
        {
            // Initialise the Tunnel lift animations
            gameEngine.TunnelAnimations.PerformPostChangeScreenActions();

            screenFade = 0;
            screenFadeSpeed = 12;
        }

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
                screenFade = 0;
                screenFadeSpeed = 32;
            }

            // Clear the viewport
            if (TriggeringScreen == enGameScreen.MainGame)
            {
                // Reset viewport back to game area
                gameEngine.ScreenEffctEngine.SelectRenderViewPort("GameArea");
            }

            // Update score for each new screen visited
            if (gameEngine.GameScreens.CurrentGameScreenData.ScreenVisited == false)
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
                screenFadeSpeed = 16;
                gameEngine.Player.TriggerBlobTeleportedAnimation();
                gameEngine.ColourAnimationEngine.Start(397, 1); // Animate the teleporter colour
            }

            if (TriggeringScreen == enGameScreen.AccessBooth)
            {
                // Test colour animation engine
                if (Param != null)
                {
                    bool success = (bool)Param;

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
    }
}