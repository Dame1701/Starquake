// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Display;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

/// <summary>
///     The game being played: the planet drawn, Blob and everything chasing him stepped, and the
///     score kept. Every other screen is somewhere Blob has gone, and this is where he comes back to.
/// </summary>
/// <remarks>
///     Not a <c>GameScreenBase</c>, which is for the five full-screen pages of text. This is the only
///     other screen that fades itself up, which makes it look like a sixth of those - see that class for
///     why it is not, and <see cref="ScreenFadeUp" /> for the four lines the two genuinely shared.
/// </remarks>
internal class MainGameScreen : IGameScreen
{

    /// <summary>
    ///     How fast a screen fades up when Blob walks onto it, in colour channel levels a frame.
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
    ///     How far the backdrop is slid sideways per column of the map, so that walking east across a
    ///     row of screens pans the sky behind them rather than repeating it.
    /// </summary>
    private const int BackgroundParallaxStep = 16;

    /// <summary>
    ///     How strongly the backdrop is tinted in behind the screen.
    /// </summary>
    private const int BackgroundOpacity = 150;

    /// <summary>
    ///     The starfield behind the planet. Half the planet's parallax, because it is meant to read as
    ///     further away.
    /// </summary>
    private const int StarfieldParallaxStep = BackgroundParallaxStep / 2;

    /// <summary>
    ///     How much of the starfield's own painted alpha comes through. It has little to spare - the
    ///     artwork peaks at about an eighth of full and covers well under one percent of the sheet - so
    ///     this is a gentle knock back rather than the planet's, and there is not much room below it
    ///     before the stars stop being there at all.
    /// </summary>
    private const int StarfieldOpacity = 180;

    /// <summary>
    ///     The colour animations the access booth tiles are given, and how long each runs for. Green
    ///     for a booth Blob has the code for, red for one he has not.
    /// </summary>
    private const int TeleporterAnimationLoops = 1;

    /// <inheritdoc cref="TeleporterAnimationLoops" />
    private const int AccessBoothAnimationLoops = 2;

    /// <inheritdoc cref="TeleporterAnimationLoops" />
    private const int AccessBoothPassedSpeed = 2;

    /// <inheritdoc cref="TeleporterAnimationLoops" />
    private const int AccessBoothFailedSpeed = 3;
    private readonly GameEngine GameEngine;

    private readonly ScreenShake _shake = new();

    private int _framesPlayed;

    private Guid _shakeRumbleSoundId = Guid.Empty;


    private int ScreenFadeSpeed { get; set; }


    public int ScreenFade { get; set; }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, ScreenChange change)
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

        if (change is ScreenChange.FadeIn)
        {
            ScreenFade = 0;
            ScreenFadeSpeed = ScreenFadeUpSpeed;
        }

        // Clear the viewport
        if (triggeringScreen == GameScreenType.MainGame)
        {
            // Reset viewport back to game area
            GameEngine.ScreenEffectEngine.SelectRenderViewPort(ViewPorts.GameArea);
        }

        // Update score for each new screen visited
        if (!GameEngine.GameScreens.CurrentGameScreenData.ScreenVisited)
        {
            GameEngine.GameScreens.CurrentGameScreenData.ScreenVisited = true;
            GameEngine.Stats.PlayerScore += ScoreForReachingCore;
        }

        // Walking onto the screen that carries the core hands over to the core screen entirely. Which
        // screen that is comes off the map.
        if (GameEngine.GameScreens.ScreenNumber == GameEngine.GameScreens.CoreSite.ScreenNumber)
        {
            GameEngine.ScreenDirectory.Current = GameScreenType.PlanetCore;
        }

        if (triggeringScreen == GameScreenType.Teleporter)
        {
            ScreenFadeSpeed = 16;
            GameEngine.Player.TriggerBlobTeleportedAnimation();
            GameEngine.GameScreens.ColourAnimations.Start((int)ActionTiles.TeleportBooth, TeleporterAnimationLoops); // Animate the teleporter colour
        }

        // Flash the booth green or red to say whether the code was taken. Only AccessBoothScreen
        // sends this, so the triggering screen no longer has to be checked as well.
        if (change is ScreenChange.AccessCode(var accepted))
        {
            Color flash = accepted ? new Color(0, ColourLevels.Full, 0) : new Color(ColourLevels.Full, 0, 0);
            int speed = accepted ? AccessBoothPassedSpeed : AccessBoothFailedSpeed;

            GameEngine.GameScreens.ColourAnimations.Start(
                (int)ActionTiles.AccessBoothLeft, AccessBoothAnimationLoops, flash, speed);
            GameEngine.GameScreens.ColourAnimations.Start(
                (int)ActionTiles.AccessBoothRight, AccessBoothAnimationLoops, flash, speed);
        }
    }

    /// <summary>
    ///     How long this game has been going, counted in logic frames rather than off the clock so
    ///     that it measures time the game actually ran for - a stall, a breakpoint or a DST step
    ///     does not lengthen it.
    /// </summary>
    public TimeSpan GetPlayTime()
        => TimeSpan.FromSeconds((double)_framesPlayed / LogicClock.FramesPerSecond);

    public void SpritesChanged()
    {
        // Initialise the Tunnel lift animations
        GameEngine.TunnelAnimations.PerformPostChangeScreenActions();

        ScreenFade = 0;
        ScreenFadeSpeed = 12;
    }

    public MainGameScreen(GameEngine gameEngineReference)
    {
        GameEngine = gameEngineReference;

        ScreenFade = ColourLevels.Full;
    }

    public void LoadContent() => GameEngine.Nasties.LoadContent();

    // Resets the game back to defaults
    public void Reset()
    {
        _framesPlayed = 0;
        GameEngine.Session.Paused = false;
        GameEngine.Stats.Reset();
        GameEngine.Elements.Reset();
        GameEngine.GameScreens.Reset();
        GameEngine.Player.Reset();
        GameEngine.Nasties.Reset();
        GameEngine.IntroSequence.Reset();

        PerformPostScreenChangeActions(GameScreenType.None, new ScreenChange.KeepPicture());
    }

    private Guid _testRumbleSoundId;

    public void Update(GameTime gameTime)
    {
        _framesPlayed++;

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
    ///     Whether Blob is under the player's control. The intro sequence flies over the same
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
        if (GameEngine.Input.GetPressedKey() != GameEngine.Session.PlayerKeys.Quit)
        {
            return;
        }

        GameEngine.ScreenDirectory.Current = GameScreenType.GameOver;
        GameEngine.ScreenDirectory.GameOver.PerformPostScreenChangeActions();
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
        if (GameEngine.Input.KeyboardState.IsKeyDown(Keys.J))
        {
            GameEngine.Stats.Platforms = TestModeSuppliesRestored;
            GameEngine.Stats.FirePower = TestModeSuppliesRestored;
        }

        if (GameEngine.Input.KeyboardState.IsKeyDown(Keys.T))
        {
            _testRumbleSoundId = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound(Assets.Sounds.Rumble, true);
        }
        else if (GameEngine.Input.KeyboardState.IsKeyUp(Keys.R))
        {
            if (_testRumbleSoundId != Guid.Empty)
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine()
                    .StopSound(_testRumbleSoundId, SoundFade.Medium);
            }
        }

        // Test game complete screen
        if (GameEngine.Input.GetPressedKey() == Keys.W)
        {
            GameEngine.ScreenDirectory.Current = GameScreenType.GameComplete;
            GameEngine.ScreenDirectory.GameComplete.PerformPostScreenChangeActions();
        }

        // Test completing core
        if (GameEngine.Input.GetPressedKey() == Keys.C)
        {
            foreach (CoreElement element in GameEngine.Elements.Elements)
            {
                element.ElementState = CoreElementState.Replaced;
            }
        }

        // Test intro sequence
        if (GameEngine.Input.GetPressedKey() == Keys.I)
        {
            GameEngine.IntroSequence.Reset();
        }

        // Test Unstick routine
        if (GameEngine.Input.GetPressedKey() == Keys.U)
        {
            GameEngine.Player.UnstickBlobFromTiles();
        }
    }

    private void FadeScreenBackUp()
        => ScreenFade = ScreenFadeUp.Advanced(ScreenFade, ScreenFadeSpeed);

    private void UpdateBlob(GameTime gameTime)
    {
        if (!GameEngine.ScreenEffectEngine.HaltBlobAndNasties)
        {
            GameEngine.Player.Update(gameTime);
        }

        // Update Blob's death routine. Blob is revived the frame after he reaches Dead, so
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
    ///     The smoke from the wreck of Blob's ship, which only exists on the screen it crashed on.
    /// </summary>
    private void UpdateShipSmoke(GameTime gameTime)
    {
        if (GameEngine.GameScreens.ScreenNumber == GameEngine.GameScreens.LandingSite.ScreenNumber)
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

        GameEngine.TunnelAnimations.Update();
        GameEngine.ConveyerNastiesInstance.Update(gameTime);
        GameEngine.ElectricityNodeGateEngine.Update();

        // Update breakable bridges
        GameEngine.BreakableBridgesEngine.Update();
    }

    private void EndGameIfOutOfLives()
    {
        if (GameEngine.Player.BlobsState == BlobState.GameOver)
        {
            // Disable the periodic sound engine and kill all sounds immediately
            GameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);

            // Change to gameover screen
            GameEngine.ScreenDirectory.GameOver.PerformPostScreenChangeActions();
            GameEngine.ScreenDirectory.Current = GameScreenType.GameOver;
        }
    }

    /// <summary>
    ///     The occasional rumble. It starts itself at random, always runs once as the intro sequence hands
    ///     over, and is suppressed while a screen effect owns the viewport.
    /// </summary>
    /// <summary>
    ///     One frame of the ground moving. Whether it moves and by how much is
    ///     <see cref="ScreenShake" />'s; the rumble and the view port are this screen's, because only it
    ///     has the sound engine and the screen effects.
    /// </summary>
    private void ShakeScreen()
    {
        // Null while a screen effect is composing the picture: the view port is its to move, and the
        // rumble is left exactly as it was rather than stopped.
        if (_shake.Step(
                GameEngine.RandomNumberGenerator,
                GameEngine.IntroSequence.SequenceRunning,
                GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause,
                GameEngine.ScreenEffectEngine.ActiveScreenEffect != null) is not { } shake)
        {
            return;
        }

        if (shake.Rumbling)
        {
            _shakeRumbleSoundId = GameEngine.ContentResourceManager.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.Rumble, true);
        }
        else
        {
            GameEngine.ContentResourceManager.GetSoundFxEngine()
                .StopSound(_shakeRumbleSoundId, SoundFade.Medium);
        }

        if (shake.MovesTheViewPort)
        {
            GameEngine.ScreenEffectEngine.MoveViewPort(ViewPorts.GameArea, shake.XOffset, shake.YOffset);
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

    public void Draw()
    {
        if (WorldIsVisible)
        {
            DrawBackground();

            GameEngine.ElectricityNodeGateEngine.Draw();

            GameEngine.GameScreens.Draw(
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade),
                GameEngine.IntroSequence.SequenceRunning, GameEngine.IntroSequence.SequenceStage);


            GameEngine.BreakableBridgesEngine.Draw();

            if (GameIsInPlay)
            {
                GameEngine.Player.Draw(ScreenFade);
            }

            if (!GameEngine.IntroSequence.SequenceRunning)
            {
                DrawBlobDeath();
                DrawNasties();
            }

            DrawUpperGraphics();

            GameEngine.TunnelAnimations.Draw((byte)ScreenFade);

            DrawShipSmoke1();

            if (GameIsInPlay)
            {
                // Screen Exit shadows
                GameEngine.GameScreens.DrawShadowing();
            }

            // Draw any sprites/effects that must go in front of shadows etc.
            GameEngine.Player.DrawUpper(ScreenFade);
        }

        if (GameEngine.GameScreens.ScreenNumber == GameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            GameEngine.IntroSequence.Draw();
        }

        if (WorldIsVisible)
        {
            DrawShipSmoke2();
        }
    }

    /// <summary>
    ///     The planet behind the scenery, in two layers shifted sideways by different amounts per
    ///     screen so that walking along a row reads as travelling across it. Both move with the column
    ///     only, which is deliberate: going up or down a row leaves them where they were.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The stars are drawn first and move at half the planet's rate, which is what makes the
    ///         two read as being at different distances. Both sheets are exactly a playfield in size
    ///         but their artwork is inset - the planet's occupies roughly the middle 680 pixels - so
    ///         sliding them sideways by up to <see cref="BackgroundParallaxStep" /> times fifteen never
    ///         exposes an edge. That inset is what sets the ceiling on the step.
    ///     </para>
    ///     <para>
    ///         The fades are worked out in floating point. Written as <c>BackgroundOpacity /
    ///         ColourLevels.Full * ScreenFade</c> it was an integer division of 150 by 255, which is 0 -
    ///         so the planet was drawn on every frame at an alpha of nothing, and had been since the
    ///         first commit in the repository. <see cref="ColourHelper.GetXna4Colour" /> had the right
    ///         of it all along.
    ///     </para>
    /// </remarks>
    private void DrawBackground()
    {
        if (!GameEngine.Presentation.DrawBackgrounds)
        {
            return;
        }

        int screenColumn = GameEngine.GameScreens.ScreenNumber % WorldMap.ScreensAcross;

        DrawBackgroundLayer("Background2", screenColumn * StarfieldParallaxStep, StarfieldOpacity);
        DrawBackgroundLayer("Background1", screenColumn * BackgroundParallaxStep, BackgroundOpacity);
    }

    /// <summary>
    ///     One layer of the background, at a given offset and strength. The strength is how much of the
    ///     layer's own painted alpha comes through, so a layer drawn at <see cref="ColourLevels.Full" />
    ///     is as the artwork has it.
    /// </summary>
    private void DrawBackgroundLayer(string spriteName, int xOffset, int opacity)
    {
        SpriteCollection layer = GameEngine.ContentResourceManager.GetSpriteCollection(spriteName);

        layer.Begin(BlendState.AlphaBlend);
        layer.DrawSprite(
            0,
            xOffset,
            0,
            ColourHelper.GetXna4Colour(
                ColourLevels.Full,
                ColourLevels.Full,
                ColourLevels.Full,
                (int)((float)opacity / ColourLevels.Full * ScreenFade)));
        layer.End();
    }

    /// <summary>
    ///     Blob is revived the frame after he reaches Dead, so the animation's own state has to keep it
    ///     drawing while the explosion fades out.
    /// </summary>
    private void DrawBlobDeath()
    {
        // Draw Blob's death routine
        if (GameEngine.Player.BlobsState is BlobState.Dead or BlobState.Dying or BlobState.Overloading ||
            GameEngine.BlobKiller.Active)
        {
            GameEngine.BlobKiller.Draw(ScreenFade);
        }

    }

    private void DrawNasties()
    {
        GameEngine.Nasties.Draw(ScreenFade);

        GameEngine.ConveyerNastiesInstance.Draw(ScreenFade);
    }

    private void DrawUpperGraphics()
    {
        GameEngine.ElectricityNodeGateEngine.DrawUpperLayer();

        GameEngine.GameScreens.DrawUpperLayer(
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
    }

    private void DrawShipSmoke1()
    {
        if (GameEngine.GameScreens.ScreenNumber == GameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            if (GameEngine.GameScreens.BlobsShipSmoke1 != null)
            {
                GameEngine.GameScreens.BlobsShipSmoke1.Draw(ScreenFade);
            }
        }
    }

    private void DrawShipSmoke2()
    {
        if (GameEngine.GameScreens.ScreenNumber == GameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            if (GameEngine.GameScreens.BlobsShipSmoke2 != null)
            {
                GameEngine.GameScreens.BlobsShipSmoke2.Draw(ScreenFade);
            }
        }
    }

}
