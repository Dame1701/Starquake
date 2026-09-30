// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Sounds;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class MainGameScreen : GameComponentBase, IGameScreen
{

    /// <summary>
    ///     How fast a screen fades up when the blob walks onto it, in colour channel levels a frame.
    /// </summary>
    private const int ScreenFadeUpSpeed = 32;

    /// <summary>
    ///     What reaching the planet core is worth.
    /// </summary>
    private const int ScoreForReachingCore = 250;

    /// <summary>
    ///     The stock of platforms and shots the test-mode cheat key hands over.
    /// </summary>
    private const int TestModeSuppliesRestored = 60;

    /// <summary>
    ///     How the background rumble is decided. Each frame one number in
    ///     <see cref="ShakeOdds" /> is drawn, and drawing <see cref="ShakeTrigger" /> starts a shake
    ///     lasting somewhere between <see cref="ShakeShortest" /> and <see cref="ShakeLongest" />
    ///     frames. The quake that ends the intro is held for <see cref="IntroShakeDuration" />.
    /// </summary>
    private const int ShakeOdds = 2000;

    /// <inheritdoc cref="ShakeOdds" />
    private const int ShakeTrigger = 1;

    /// <inheritdoc cref="ShakeOdds" />
    private const int ShakeShortest = 180;

    /// <inheritdoc cref="ShakeOdds" />
    private const int ShakeLongest = 300;

    /// <inheritdoc cref="ShakeOdds" />
    private const int IntroShakeDuration = 500;

    /// <summary>
    ///     How far the backdrop is slid sideways per column of the map, so that walking east across a
    ///     row of screens pans the sky behind them rather than repeating it.
    /// </summary>
    private const int BackgroundParallaxStep = 16;

    /// <summary>
    ///     How strongly the backdrop is tinted in behind the screen.
    /// </summary>
    private const int BackgroundOpacity = 150;

    /// <summary>
    ///     The colour animations the access booth tiles are given, and how long each runs for. Green
    ///     for a booth the blob has the code for, red for one it has not.
    /// </summary>
    private const int TeleporterAnimationLoops = 1;

    /// <inheritdoc cref="TeleporterAnimationLoops" />
    private const int AccessBoothAnimationLoops = 2;

    /// <inheritdoc cref="TeleporterAnimationLoops" />
    private const int AccessBoothPassedSpeed = 2;

    /// <inheritdoc cref="TeleporterAnimationLoops" />
    private const int AccessBoothFailedSpeed = 3;

    private DateTime _gameStartedTime;

    private Guid _shakeRumbleSoundId = Guid.Empty;

    private bool _shakingScreen;
    private int _shakingScreenCount;
    private int _shakingScreenCounter;

    private int ScreenFadeSpeed { get; set; }

    public bool Success => true;

    public int ScreenFade { get; set; }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
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
            ScreenFadeSpeed = ScreenFadeUpSpeed;
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
            GameEngine.BlobStatusPanel.PlayerScore += ScoreForReachingCore;
        }

        if (GameEngine.GameScreens.ScreenNumber == WorldMap.CoreScreenNumber)
        {
            GameEngine.CurrentGameScreen = GameScreenType.PlanetCore;
        }

        if (triggeringScreen == GameScreenType.Teleporter)
        {
            ScreenFadeSpeed = 16;
            GameEngine.Player.TriggerBlobTeleportedAnimation();
            GameEngine.ColourAnimationEngine.Start((int)ActionTiles.TeleportBooth, TeleporterAnimationLoops); // Animate the teleporter colour
        }

        if (triggeringScreen == GameScreenType.AccessBooth)
        {
            // Test colour animation engine
            if (param != null)
            {
                var success = (bool)param;

                if (success)
                {
                    GameEngine.ColourAnimationEngine.Start((int)ActionTiles.AccessBoothLeft, AccessBoothAnimationLoops, new Color(0, ColourLevels.Full, 0), AccessBoothPassedSpeed);
                    GameEngine.ColourAnimationEngine.Start((int)ActionTiles.AccessBoothRight, AccessBoothAnimationLoops, new Color(0, ColourLevels.Full, 0), AccessBoothPassedSpeed);
                }
                else
                {
                    GameEngine.ColourAnimationEngine.Start((int)ActionTiles.AccessBoothLeft, AccessBoothAnimationLoops, new Color(ColourLevels.Full, 0, 0), AccessBoothFailedSpeed);
                    GameEngine.ColourAnimationEngine.Start((int)ActionTiles.AccessBoothRight, AccessBoothAnimationLoops, new Color(ColourLevels.Full, 0, 0), AccessBoothFailedSpeed);
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
        ScreenFade = ColourLevels.Full;
    }

    public override void LoadContent()
    {
        GameEngine.Nasties.Initialise(null);

        var nastyGraphics = new ContentList(GameEngine.ContentMgr);
        nastyGraphics.AddContent("nastySprite", "Graphics\\nastiesNew");
        GameEngine.Nasties.LoadContent(nastyGraphics);
    }

    // Resets the game back to defaults
    public void Reset(object? data)
    {
        _gameStartedTime = DateTime.Now;
        GameEngine.Paused = false;
        GameEngine.BlobStatusPanel.Reset();
        GameEngine.Elements.Reset();
        GameEngine.GameScreens.Reset();
        GameEngine.Player.Reset();
        GameEngine.Nasties.Reset();
        GameEngine.IntroSequence.Reset();

        PerformPostScreenChangeActions(GameScreenType.None, false);
    }

    private Guid _testRumbleSoundId;

    public override void Update(GameTime gameTime)
    {
        if (GameEngine.InTestMode)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.J))
            {
                GameEngine.BlobStatusPanel.Platforms = TestModeSuppliesRestored;
                GameEngine.BlobStatusPanel.FirePower = TestModeSuppliesRestored;
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
                foreach (var element in GameEngine.Elements.Elements)
                {
                    element.ElementState = CoreElementState.Replaced;
                }
            }

            // Test intro sequence
            if (ToggleKeys.GetPressedKey() == Keys.I)
            {
                GameEngine.IntroSequence.Reset();
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
            if (ScreenFade < ColourLevels.Full)
            {
                ScreenFade = ScreenFade + ScreenFadeSpeed;
            }

            if (ScreenFade > ColourLevels.Full)
            {
                ScreenFade = ColourLevels.Full;
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

            // Update blob's death routine. Blob is revived the frame after he reaches Dead, so
            // the animation's own state has to keep it alive while the explosion fades out
            if (GameEngine.Player.BlobsState == BlobState.Dead ||
                GameEngine.Player.BlobsState == BlobState.Dying ||
                GameEngine.Player.BlobsState == BlobState.Overloading ||
                GameEngine.BlobKiller.Active)
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
                var shakingScreenTime = GameEngine.RandomNumberGenerator.Next(0, ShakeOdds);

                if (shakingScreenTime == ShakeTrigger && !_shakingScreen)
                {
                    _shakingScreenCount = GameEngine.RandomNumberGenerator.Next(ShakeShortest, ShakeLongest);
                    _shakingScreen = true;
                }
            }

            if (GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
            {
                _shakingScreenCount = IntroShakeDuration;
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
                var screenXPos = GameEngine.GameScreens.ScreenNumber % WorldMap.ScreensAcross;

                GameEngine.ContentResourceManager.GetSpriteCollection("Background1")
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("Background1").DrawSprite(0, screenXPos * BackgroundParallaxStep,
                    0, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)(BackgroundOpacity / ColourLevels.Full * ScreenFade)));
                GameEngine.ContentResourceManager.GetSpriteCollection("Background1").End();
            }

            // Draw electricity nodes etc lower layer
            GameEngine.ElectricityNodeGateEngine.Draw(gameTime, ScreenFade);

            // Draw screen
            GameEngine.GameScreens.Draw(
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade),
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
                if (GameEngine.Player.BlobsState is BlobState.Dead or BlobState.Dying or BlobState.Overloading ||
                    GameEngine.BlobKiller.Active)
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
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade),
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