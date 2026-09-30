// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Diagnostics;
using System.Linq;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
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
    private readonly GameEngine GameEngine;

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
            GameEngine.ScreenDirectory.Current = GameScreenType.PlanetCore;
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
    {
        GameEngine = gameEngineReference;

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
        GameEngine.Session.Paused = false;
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
        QuitToGameOverIfAsked();

        HandleTestModeKeys();

        if (GameIsInPlay)
        {
            FadeScreenBackUp();
        }

        GameEngine.ScreenEffectEngine.Update();
        GameEngine.GameScreens.Update(
            gameTime,
            GameEngine.Player.BlobXPos,
            GameEngine.Player.BlobYPos,
            GameEngine.Player.BlobInventory.Contains(Collectables.Key));

        if (GameIsInPlay)
        {
            UpdateBlob(gameTime);
        }

        UpdateShipSmoke(gameTime);

        if (GameIsInPlay)
        {
            UpdateWorld(gameTime);
            EndGameIfOutOfLives();
            ShakeScreen();
        }

        if (GameEngine.IntroSequence.SequenceRunning ||
            GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.Finished)
        {
            GameEngine.IntroSequence.Update(gameTime);
        }
    }

    /// <summary>
    ///     Whether the blob is under the player's control. The intro sequence flies over the same
    ///     screen before handing it over, and four separate blocks of this update used to spell that
    ///     test out.
    /// </summary>
    private bool GameIsInPlay =>
        !GameEngine.IntroSequence.SequenceRunning ||
        GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause;

    /// <summary>
    ///     The player's quit key gives up the current game and goes to the game over screen. It
    ///     lived in <see cref="HandleTestModeKeys" /> under a comment calling it a test, which meant
    ///     a key the options screen offers in all four layouts only worked while the debug keys
    ///     were compiled in.
    /// </summary>
    private void QuitToGameOverIfAsked()
    {
        if (ToggleKeys.GetPressedKey() != GameEngine.Session.PlayerKeys.Quit)
        {
            return;
        }

        GameEngine.ScreenDirectory.Current = GameScreenType.GameOver;
        GameEngine.ScreenDirectory.Get(GameScreenType.GameOver)
            .PerformPostScreenChangeActions(GameScreenType.MainGame, null);
    }

    /// <summary>
    ///     The debug keys for the main game: J tops up the supplies, T and R the test rumble,
    ///     W jumps to the game complete screen, C replaces every core element, I restarts the
    ///     intro and U runs the unstick routine. Compiled out of a release build entirely by
    ///     <see cref="ConditionalAttribute" />, which removes the calls to this rather than leaving
    ///     a branch that is never taken, so a release has no path to them at all.
    ///     <para>
    ///         The keys that are not debug keys are handled elsewhere and stay live in both: F8
    ///         swaps the artwork, F9 the scanlines, F10 the backdrops, F12 fullscreen, and the
    ///         player's own pause and quit keys.
    ///     </para>
    /// </summary>
    [Conditional("DEBUG")]
    private void HandleTestModeKeys()
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

        // Test game complete screen
        if (ToggleKeys.GetPressedKey() == Keys.W)
        {
            GameEngine.ScreenDirectory.Current = GameScreenType.GameComplete;
            GameEngine.ScreenDirectory.Get(GameScreenType.GameComplete)
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

    private void FadeScreenBackUp()
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

    private void UpdateBlob(GameTime gameTime)
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

    /// <summary>
    ///     The smoke from the wreck of the blob's ship, which only exists on the screen it crashed on.
    /// </summary>
    private void UpdateShipSmoke(GameTime gameTime)
    {
        if (GameEngine.GameScreens.ScreenNumber == WorldMap.ShipWreckScreenNumber)
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
    }

    private void UpdateWorld(GameTime gameTime)
    {
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
    }

    private void EndGameIfOutOfLives()
    {
        if (GameEngine.Player.BlobsState == BlobState.Gameover)
        {
            // Disable the periodic sound engine and kill all soudns immediately
            GameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);

            // Change to gameover screen
            GameEngine.ScreenDirectory.Get(GameScreenType.GameOver)
                .PerformPostScreenChangeActions(GameScreenType.MainGame, null);
            GameEngine.ScreenDirectory.Current = GameScreenType.GameOver;
        }
    }

    /// <summary>
    ///     The occasional rumble. It starts itself at random, always runs once as the intro sequence hands
    ///     over, and is suppressed while a screen effect owns the viewport.
    /// </summary>
    private void ShakeScreen()
    {
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

    public void ResetScreenFade(int fadeSpeed)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = fadeSpeed;
    }

    /// <summary>
    ///     Whether the world is on screen at all. It is hidden while the intro sequence's caption
    ///     fades in and out over it.
    /// </summary>
    private bool WorldIsVisible =>
        GameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeUp &&
        GameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeDown;

    public override void Draw(GameTime gameTime)
    {
        if (WorldIsVisible)
        {
            DrawBackground(gameTime);

            GameEngine.ElectricityNodeGateEngine.Draw(gameTime, ScreenFade);

            GameEngine.GameScreens.Draw(
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade),
                gameTime, GameEngine.IntroSequence.SequenceRunning, GameEngine.IntroSequence.SequenceStage);


            GameEngine.BreakableBridgesEngine.Draw(gameTime);

            if (GameIsInPlay)
            {
                GameEngine.Player.Draw(gameTime, ScreenFade);
            }

            if (!GameEngine.IntroSequence.SequenceRunning)
            {
                DrawBlobDeath(gameTime);
                DrawNasties(gameTime);
            }

            DrawUpperGraphics(gameTime);

            GameEngine.TunnelAnimations.Draw(gameTime, (byte)ScreenFade);

            DrawShipSmoke1();

            if (GameIsInPlay)
            {
                // Screen Exit shadows
                GameEngine.GameScreens.DrawShadowing(gameTime);
            }

            // Draw any sprites/effects that must go in front of shadows etc.
            GameEngine.Player.DrawUpper(ScreenFade);
        }

        if (GameEngine.GameScreens.ScreenNumber == WorldMap.ShipWreckScreenNumber)
        {
            GameEngine.IntroSequence.Draw(gameTime);
        }

        if (WorldIsVisible)
        {
            DrawShipSmoke2();
        }
    }

    private void DrawBackground(GameTime gameTime)
    {
        if (GameEngine.Presentation.DrawBackgrounds)
        {
            var screenXPos = GameEngine.GameScreens.ScreenNumber % WorldMap.ScreensAcross;

            GameEngine.ContentResourceManager.GetSpriteCollection("Background1")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            GameEngine.ContentResourceManager.GetSpriteCollection("Background1").DrawSprite(0, screenXPos * BackgroundParallaxStep,
                0, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)(BackgroundOpacity / ColourLevels.Full * ScreenFade)));
            GameEngine.ContentResourceManager.GetSpriteCollection("Background1").End();
        }
    }

    /// <summary>
    ///     Blob is revived the frame after he reaches Dead, so the animation's own state has to keep it
    ///     drawing while the explosion fades out.
    /// </summary>
    private void DrawBlobDeath(GameTime gameTime)
    {
        // Draw blob's death routine
        if (GameEngine.Player.BlobsState is BlobState.Dead or BlobState.Dying or BlobState.Overloading ||
            GameEngine.BlobKiller.Active)
        {
            GameEngine.BlobKiller.Draw(gameTime, ScreenFade);
        }

    }

    private void DrawNasties(GameTime gameTime)
    {
        GameEngine.Nasties.Draw(
            gameTime,
            ScreenFade);

        GameEngine.ConveyerNastiesInstance.Draw(
            gameTime,
            ScreenFade);
    }

    private void DrawUpperGraphics(GameTime gameTime)
    {
        GameEngine.ElectricityNodeGateEngine.DrawUpperLayer(gameTime,
            ScreenFade);

        GameEngine.GameScreens.DrawUpperLayer(
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade),
            gameTime);
    }

    private void DrawShipSmoke1()
    {
        if (GameEngine.GameScreens.ScreenNumber == WorldMap.ShipWreckScreenNumber)
        {
            if (GameEngine.GameScreens.BlobsShipSmoke1 != null)
            {
                GameEngine.GameScreens.BlobsShipSmoke1.Draw(ScreenFade);
            }
        }
    }

    private void DrawShipSmoke2()
    {
        if (GameEngine.GameScreens.ScreenNumber == WorldMap.ShipWreckScreenNumber)
        {
            if (GameEngine.GameScreens.BlobsShipSmoke2 != null)
            {
                GameEngine.GameScreens.BlobsShipSmoke2.Draw(ScreenFade);
            }
        }
    }

    #endregion

}