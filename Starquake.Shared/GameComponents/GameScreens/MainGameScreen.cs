// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Sounds;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class MainGameScreen : GameComponentBase, IGameScreen
{

    private DateTime _gameStartedTime;

    private Guid _shakeRumbleSoundId = Guid.Empty;

    private bool _shakingScreen;
    private int _shakingScreenCount;
    private int _shakingScreenCounter;

    public int ScreenFadeSpeed { get; set; }

    public bool Success => true;

    public int ScreenFade { get; set; }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {
        GameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds();

        if (triggeringScreen != GameScreenType.AccessBooth && triggeringScreen != GameScreenType.Cheops)
        {
            GameEngine.Player.PerformPostChangeScreenActions();
            GameEngine.ConveyerNastiesInstance.PerformPostChangeScreenActions();
            GameEngine.Nasties.PerformPostChangeScreenActions();

            GameEngine.ElectricityNodeGateEngine.PerformPostChangeScreenActions();
            GameEngine.ContentResourceManager.GetSoundFxEngine().KillSounds(false);

            // Initialise the Tunnel lift animations
            GameEngine.TunnelAnimations.PerformPostChangeScreenActions();
        }

        if (param == null)
        {
            ScreenFade = 0;
            ScreenFadeSpeed = 32;
        }

        // Clear the viewport
        if (triggeringScreen == GameScreenType.MainGame)
        {
            // Reset viewport back to game area
            GameEngine.ScreenEffectEngine.SelectRenderViewPort("GameArea");
        }

        // Update score for each new screen visited
        if (!GameEngine.GameScreens.CurrentGameScreenData.ScreenVisited)
        {
            GameEngine.GameScreens.CurrentGameScreenData.ScreenVisited = true;
            GameEngine.BlobStatusPanel.PlayerScore += 250;
        }

        if (GameEngine.GameScreens.ScreenNumber == 199)
        {
            GameEngine.CurrentGameScreen = GameScreenType.PlanetCore;
        }

        if (triggeringScreen == GameScreenType.Teleporter)
        {
            ScreenFadeSpeed = 16;
            GameEngine.Player.TriggerBlobTeleportedAnimation();
            GameEngine.ColourAnimationEngine.Start(397, 1); // Animate the teleporter colour
        }

        if (triggeringScreen == GameScreenType.AccessBooth)
        {
            // Test colour animation engine
            if (param != null)
            {
                var success = (bool)param;

                if (success)
                {
                    GameEngine.ColourAnimationEngine.Start(399, 2, new Color(0, 255, 0), 2);
                    GameEngine.ColourAnimationEngine.Start(400, 2, new Color(0, 255, 0), 2);
                }
                else
                {
                    GameEngine.ColourAnimationEngine.Start(399, 2, new Color(255, 0, 0), 3);
                    GameEngine.ColourAnimationEngine.Start(400, 2, new Color(255, 0, 0), 3);
                }
            }
        }
    }

    public TimeSpan GetPlayTime()
        => DateTime.Now - _gameStartedTime;

    public void SpritesChanged()
    {
        // Initialise the Tunnel lift animations
        GameEngine.TunnelAnimations.PerformPostChangeScreenActions();

        ScreenFade = 0;
        ScreenFadeSpeed = 12;
    }

    #region IGameComponent Members

    public MainGameScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenFade = 255;
    }

    public override void LoadContent()
    {
        GameEngine.Nasties.Initialise(null);

        var nastyGraphics = new ContentList(GameEngine.ContentMgr);
        nastyGraphics.AddContent("nastySprite", "Graphics\\nastiesNew");
        GameEngine.Nasties.LoadContent(nastyGraphics);
    }

    // Resets the game back to defaults
    public void Reset(object data)
    {
        _gameStartedTime = DateTime.Now;
        GameEngine.Paused = false;
        GameEngine.BlobStatusPanel.Reset();
        GameEngine.Elements.Reset();
        GameEngine.GameScreens.Reset();
        GameEngine.Player.Reset();
        GameEngine.Nasties.Reset();
        GameEngine.IntroSequence.Reset(this);

        PerformPostScreenChangeActions(GameScreenType.None, false);
    }

    private Guid _testRumbleSoundId;

    public override void Update(GameTime gameTime)
    {
        if (GameEngine.InTestMode)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.J))
            {
                GameEngine.BlobStatusPanel.Platforms = 60;
                GameEngine.BlobStatusPanel.FirePower = 60;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.T))
            {
                _testRumbleSoundId = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("Rumble", true);
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.R))
            {
                if (_testRumbleSoundId != Guid.Empty)
                {
                    GameEngine.ContentResourceManager.GetSoundFxEngine()
                        .StopSound(_testRumbleSoundId, SoundFade.Medium);
                }
            }

            // Test quit to game over screen
            if (ToggleKeys.GetPressedKey() == GameEngine.PlayerKeys.Quit)
            {
                GameEngine.CurrentGameScreen = GameScreenType.GameOver;
                GameEngine.GetGameScreen(GameScreenType.GameOver)
                    .PerformPostScreenChangeActions(GameScreenType.MainGame, null);
            }

            // Test game complete screen
            if (ToggleKeys.GetPressedKey() == Keys.W)
            {
                GameEngine.CurrentGameScreen = GameScreenType.GameComplete;
                GameEngine.GetGameScreen(GameScreenType.GameComplete)
                    .PerformPostScreenChangeActions(GameScreenType.MainGame, null);
            }

            // Test completing core
            if (ToggleKeys.GetPressedKey() == Keys.C)
            {
                for (var a = 0; a < GameEngine.Elements.Elements.Length; a++)
                {
                    GameEngine.Elements.Elements[a].ElementState = CoreElementState.Replaced;
                }
            }

            // Test intro sequence
            if (ToggleKeys.GetPressedKey() == Keys.I)
            {
                GameEngine.IntroSequence.Reset(this);
            }

            // Test Unstick routine
            if (ToggleKeys.GetPressedKey() == Keys.U)
            {
                GameEngine.Player.UnstickBlobFromTiles();
            }
        }

        // Fade screen back up
        if (!GameEngine.IntroSequence.SequenceRunning ||
            GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
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
        GameEngine.ScreenEffectEngine.Update();
        GameEngine.GameScreens.Update(gameTime);

        if (!GameEngine.IntroSequence.SequenceRunning ||
            GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
        {
            if (!GameEngine.ScreenEffectEngine.HaltBlobAndNasties)
            {
                GameEngine.Player.Update(gameTime);
            }

            // Update blob's death routine
            if (GameEngine.Player.BlobsState == BlobState.Dead ||
                GameEngine.Player.BlobsState == BlobState.Dying ||
                GameEngine.Player.BlobsState == BlobState.Overloading)
            {
                GameEngine.BlobKiller.Update(gameTime);
            }
        }

        // Update Blob's ship smoke
        if (GameEngine.GameScreens.ScreenNumber == 8)
        {
            if (GameEngine.GameScreens.BlobsShipSmoke1 != null)
            {
                GameEngine.GameScreens.BlobsShipSmoke1.Start();
                GameEngine.GameScreens.BlobsShipSmoke1.Update(GameEngine.IntroSequence.BlobSpaceShipXPos,
                    GameEngine.IntroSequence.BlobSpaceShipYPos, gameTime);
            }

            if (GameEngine.GameScreens.BlobsShipSmoke2 != null)
            {
                GameEngine.GameScreens.BlobsShipSmoke2.Start();
                GameEngine.GameScreens.BlobsShipSmoke2.Update(GameEngine.IntroSequence.BlobSpaceShipXPos,
                    GameEngine.IntroSequence.BlobSpaceShipYPos, gameTime);
            }
        }

        if (!GameEngine.IntroSequence.SequenceRunning ||
            GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
        {
            // Put the delay stuff in here
            if (!GameEngine.IntroSequence.SequenceRunning)
            {
                if (!GameEngine.ScreenEffectEngine.HaltBlobAndNasties)
                {
                    GameEngine.Nasties.Update(gameTime);
                }
            }

            GameEngine.TunnelAnimations.Update(gameTime);
            GameEngine.ConveyerNastiesInstance.Update(gameTime);
            GameEngine.ElectricityNodeGateEngine.Update();

            // Update breakable bridges
            GameEngine.BreakableBridgesEngine.Update();

            // No lives left - so finish the game
            if (GameEngine.Player.BlobsState == BlobState.Gameover)
            {
                // Disable the periodic sound engine and kill all soudns immediately
                GameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);

                // Change to gameover screen
                GameEngine.GetGameScreen(GameScreenType.GameOver)
                    .PerformPostScreenChangeActions(GameScreenType.MainGame, null);
                GameEngine.CurrentGameScreen = GameScreenType.GameOver;
            }

            // Randomly decide when to shake the screen
            var xShakeOffset = 0;
            var yShakeOffset = 0;

            if (!GameEngine.IntroSequence.SequenceRunning)
            {
                var shakingScreenTime = GameEngine.RandomNumberGenerator.Next(0, 2000);

                if (shakingScreenTime == 1 && !_shakingScreen)
                {
                    _shakingScreenCount = GameEngine.RandomNumberGenerator.Next(180, 300);
                    _shakingScreen = true;
                }
            }

            if (GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
            {
                _shakingScreenCount = 500;
                _shakingScreen = true;
            }

            // Shake the screen, but only if a screen effect is not active
            if (GameEngine.ScreenEffectEngine.ActiveScreenEffect == null)
            {
                if (_shakingScreen)
                {
                    xShakeOffset = GameEngine.RandomNumberGenerator.Next(0, 5);
                    yShakeOffset = GameEngine.RandomNumberGenerator.Next(0, 5);

                    _shakingScreenCounter++;

                    // Play rumble sound
                    _shakeRumbleSoundId = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("Rumble", true);

                    if (_shakingScreenCounter > _shakingScreenCount)
                    {
                        _shakingScreenCounter = 0;
                        _shakingScreen = false;
                    }
                }
                else
                {
                    if (xShakeOffset > 0)
                    {
                        xShakeOffset = xShakeOffset - 1;
                    }

                    if (yShakeOffset > 0)
                    {
                        yShakeOffset = yShakeOffset - 1;
                    }

                    GameEngine.ContentResourceManager.GetSoundFxEngine()
                        .StopSound(_shakeRumbleSoundId, SoundFade.Medium);
                }

                if (yShakeOffset > 0 && xShakeOffset > 0)
                {
                    GameEngine.ScreenEffectEngine.MoveViewPort("GameArea", xShakeOffset, yShakeOffset);
                }
            }
        }

        if (GameEngine.IntroSequence.SequenceRunning ||
            GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.Finished)
        {
            GameEngine.IntroSequence.Update(gameTime);
        }
    }

    public void ResetScreenFade(int fadeSpeed)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = fadeSpeed;
    }

    public override void Draw(GameTime gameTime)
    {
        if (GameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeUp &&
            GameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeDown)
        {
            // Draw background
            if (GameEngine.DrawBackgrounds)
            {
                var screenXPos = GameEngine.GameScreens.ScreenNumber % 16;

                GameEngine.ContentResourceManager.GetSpriteCollection("Background1")
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("Background1").DrawSprite(0, screenXPos * 16,
                    0, new Color((byte)255, (byte)255, (byte)255, (byte)(150 / 255 * ScreenFade)));
                GameEngine.ContentResourceManager.GetSpriteCollection("Background1").End();
            }

            // Draw electricity nodes etc lower layer
            GameEngine.ElectricityNodeGateEngine.Draw(gameTime, ScreenFade);

            // Draw screen
            GameEngine.GameScreens.Draw(
                ColourHelper.GetXna4Colour(255, 255, 255, (byte)ScreenFade),
                gameTime, GameEngine.IntroSequence.SequenceRunning, GameEngine.IntroSequence.SequenceStage);

            // Draw breakable bridges
            GameEngine.BreakableBridgesEngine.Draw(gameTime);

            if (!GameEngine.IntroSequence.SequenceRunning ||
                GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
            {
                // Draw blob
                GameEngine.Player.Draw(
                    gameTime,
                    ScreenFade);
            }

            if (!GameEngine.IntroSequence.SequenceRunning)
            {
                // Draw blob's death routine
                if (GameEngine.Player.BlobsState is BlobState.Dead or BlobState.Dying or BlobState.Overloading)
                {
                    GameEngine.BlobKiller.Draw(gameTime, ScreenFade);
                }

                // Draw nasties
                GameEngine.Nasties.Draw(
                    gameTime,
                    ScreenFade);

                GameEngine.ConveyerNastiesInstance.Draw(
                    gameTime,
                    ScreenFade);
            }

            // Draw upper graphics
            GameEngine.ElectricityNodeGateEngine.DrawUpperLayer(gameTime,
                ScreenFade);

            GameEngine.GameScreens.DrawUpperLayer(
                ColourHelper.GetXna4Colour(255, 255, 255, (byte)ScreenFade),
                gameTime);

            // Draw tunnel animations
            GameEngine.TunnelAnimations.Draw(gameTime, (byte)ScreenFade);

            // Draw blob's ship smoke
            if (GameEngine.GameScreens.ScreenNumber == 8)
            {
                if (GameEngine.GameScreens.BlobsShipSmoke1 != null)
                {
                    GameEngine.GameScreens.BlobsShipSmoke1.Draw(ScreenFade);
                }
            }

            if (!GameEngine.IntroSequence.SequenceRunning ||
                GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
            {
                // Screen Exit shadows
                GameEngine.GameScreens.DrawShadowing(gameTime);
            }

            // Draw any sprites/effects that must go in front of shadows etc.
            GameEngine.Player.DrawUpper(ScreenFade);
        }

        if (GameEngine.GameScreens.ScreenNumber == 8)
        {
            GameEngine.IntroSequence.Draw(gameTime);
        }

        // Draw blob's ship smoke
        if (GameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeUp &&
            GameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeDown)
        {
            if (GameEngine.GameScreens.ScreenNumber == 8)
            {
                if (GameEngine.GameScreens.BlobsShipSmoke2 != null)
                {
                    GameEngine.GameScreens.BlobsShipSmoke2.Draw(ScreenFade);
                }
            }
        }
    }

    #endregion

}