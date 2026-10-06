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
using Starquake.Engines.ScreenEffects;
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
///     why it is not, and <see cref="Ramp.Up(int,int,int)" /> for the four lines the two genuinely shared.
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
    private readonly GameEngine _gameEngine;

    private readonly ScreenShake _shake = new();

    private int _framesPlayed;

    private Guid _shakeRumbleSoundId = Guid.Empty;

    private Guid _testRumbleSoundId;

    public MainGameScreen(GameEngine gameEngineReference)
    {
        _gameEngine = gameEngineReference;

        ScreenFade = ColourLevels.Full;
    }


    private int ScreenFadeSpeed { get; set; }


    public int ScreenFade { get; set; }

    /// <summary>
    ///     Whether Blob is under the player's control. The intro sequence flies over the same
    ///     screen before handing it over, and four separate blocks of this update used to spell that
    ///     test out.
    /// </summary>
    private bool GameIsInPlay =>
        !_gameEngine.IntroSequence.SequenceRunning ||
        _gameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause;

    /// <summary>
    ///     Whether Blob's death is still playing out. Blob is revived the frame after he reaches
    ///     <see cref="BlobState.Dead" />, so the animation's own state has to keep the routine alive
    ///     while the explosion fades out - which is why this asks it as well as asking Blob.
    /// </summary>
    private bool DeathRoutineRunning
        => _gameEngine.Player.BlobsState is BlobState.Dead or BlobState.Dying or BlobState.Overloading ||
           _gameEngine.BlobKiller.Active;

    /// <summary>
    ///     Whether the world is on screen at all. It is hidden while the intro sequence's caption
    ///     fades in and out over it.
    /// </summary>
    private bool WorldIsVisible =>
        _gameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeUp &&
        _gameEngine.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeDown;

    public void Update(GameTime gameTime)
    {
        _framesPlayed++;

        QuitToGameOverIfAsked();

        HandleTestModeKeys();

        if (GameIsInPlay)
        {
            FadeScreenBackUp();
        }

        _gameEngine.ScreenEffectEngine.Update();
        _gameEngine.GameScreens.Update(
            gameTime,
            _gameEngine.Player.BlobXPos,
            _gameEngine.Player.BlobYPos,
            _gameEngine.Player.BlobInventory.Contains(Collectables.Key));

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

        if (_gameEngine.IntroSequence.SequenceRunning ||
            _gameEngine.IntroSequence.SequenceStage == IntroSequenceStage.Finished)
        {
            _gameEngine.IntroSequence.Update(gameTime);
        }
    }

    public void Draw()
    {
        if (WorldIsVisible)
        {
            DrawBackground();

            _gameEngine.ElectricityNodeGateEngine.Draw();

            _gameEngine.GameScreens.Draw(
                ColourHelper.WhiteFadedTo((byte)ScreenFade),
                _gameEngine.IntroSequence.SequenceRunning, _gameEngine.IntroSequence.SequenceStage);


            _gameEngine.BreakableBridgesEngine.Draw();

            if (GameIsInPlay)
            {
                _gameEngine.Player.Draw(ScreenFade);
            }

            if (!_gameEngine.IntroSequence.SequenceRunning)
            {
                DrawBlobDeath();
                DrawNasties();
            }

            DrawUpperGraphics();

            _gameEngine.TunnelAnimations.Draw((byte)ScreenFade);

            DrawShipSmoke1();

            if (GameIsInPlay)
            {
                _gameEngine.GameScreens.DrawShadowing();
            }

            // Last, so Blob's sparks are in front of the shadows down the screen exits rather than
            // under them.
            _gameEngine.Player.DrawUpper(ScreenFade);
        }

        if (_gameEngine.GameScreens.ScreenNumber == _gameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            _gameEngine.IntroSequence.Draw();
        }

        if (WorldIsVisible)
        {
            DrawShipSmoke2();
        }
    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, ScreenChange change)
    {
        _gameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds();

        if (triggeringScreen != GameScreenType.AccessBooth && triggeringScreen != GameScreenType.Cheops)
        {
            _gameEngine.Player.PerformPostChangeScreenActions();
            _gameEngine.ConveyorNastiesInstance.PerformPostChangeScreenActions();
            _gameEngine.Nasties.PerformPostChangeScreenActions();

            _gameEngine.ElectricityNodeGateEngine.PerformPostChangeScreenActions();
            _gameEngine.ContentResourceManager.GetSoundFxEngine().KillSounds(false);

            _gameEngine.TunnelAnimations.PerformPostChangeScreenActions();
        }

        if (change is ScreenChange.FadeIn)
        {
            ScreenFade = 0;
            ScreenFadeSpeed = ScreenFadeUpSpeed;
        }

        if (triggeringScreen == GameScreenType.MainGame)
        {
            _gameEngine.ScreenEffectEngine.SelectRenderViewPort(ViewPorts.GameArea);
        }

        ScoreFirstVisitToThisScreen();

        FloodOllysScreen();

        // Walking onto the screen that carries the core hands over to the core screen entirely. Which
        // screen that is comes off the map.
        if (_gameEngine.GameScreens.ScreenNumber == _gameEngine.GameScreens.CoreSite.ScreenNumber)
        {
            _gameEngine.ScreenDirectory.Current = GameScreenType.PlanetCore;
        }

        if (triggeringScreen == GameScreenType.Teleporter)
        {
            ScreenFadeSpeed = 16;
            _gameEngine.Player.TriggerBlobTeleportedAnimation();

            // The room arrives unsettled and shivers itself straight. Blob is not held still for
            // it - he has just got here and should be able to walk.
            _gameEngine.ScreenEffectEngine.ExecuteScreenEffect(
                ScreenEffectType.TeleportArrival, -1, TeleportArrivalSettings.Frames, false);
            _gameEngine.GameScreens.ColourAnimations.Start((int)ActionTiles.TeleportBooth, TeleporterAnimationLoops); // Animate the teleporter colour
        }

        // Flash the booth green or red to say whether the code was taken. Only AccessBoothScreen
        // sends this, so the triggering screen no longer has to be checked as well.
        if (change is ScreenChange.AccessCode(var accepted))
        {
            Color flash = accepted ? new Color(0, ColourLevels.Full, 0) : new Color(ColourLevels.Full, 0, 0);
            int speed = accepted ? AccessBoothPassedSpeed : AccessBoothFailedSpeed;

            _gameEngine.GameScreens.ColourAnimations.Start(
                (int)ActionTiles.AccessBoothLeft, AccessBoothAnimationLoops, flash, speed);
            _gameEngine.GameScreens.ColourAnimations.Start(
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

    /// <summary>
    ///     Points for reaching a screen for the first time, once per screen for the whole game.
    /// </summary>
    private void ScoreFirstVisitToThisScreen()
    {
        if (_gameEngine.GameScreens.CurrentGameScreenData.ScreenVisited)
        {
            return;
        }

        _gameEngine.GameScreens.CurrentGameScreenData.ScreenVisited = true;
        _gameEngine.Stats.PlayerScore += ScoreForReachingCore;
    }

    /// <summary>
    ///     The artwork has been swapped under this screen, so anything holding a sprite from the
    ///     set that has gone has to take one from the set that has arrived.
    /// </summary>
    /// <remarks>
    ///     This used to set the screen fade to nought as well, which blacked the picture out and
    ///     ramped it back up over about four tenths of a second - the swap was a blink. The
    ///     transition is <c>ArtworkCrossfade</c>'s now, and it dissolves one set into the other,
    ///     which needs the arriving picture at full strength rather than climbing out of black
    ///     underneath the one being held.
    /// </remarks>
    public void SpritesChanged()
        => _gameEngine.TunnelAnimations.PerformPostChangeScreenActions();

    public void LoadContent() => _gameEngine.Nasties.LoadContent();

    /// <summary>
    ///     A new game: the clock, the score, the core, the planet and everything on it back to how
    ///     they start.
    /// </summary>
    public void Reset()
    {
        _framesPlayed = 0;
        _gameEngine.Session.Paused = false;
        _gameEngine.Stats.Reset();
        _gameEngine.Elements.Reset();
        _gameEngine.GameScreens.Reset(_gameEngine.Elements.ElementsWanted());
        _gameEngine.Player.Reset();
        _gameEngine.Nasties.Reset();
        _gameEngine.IntroSequence.Reset();

        PerformPostScreenChangeActions(GameScreenType.None, new ScreenChange.KeepPicture());
    }

    /// <summary>
    ///     The player's quit key gives up the current game and goes to the game over screen. It
    ///     lived in <see cref="HandleTestModeKeys" /> under a comment calling it a test, which meant
    ///     a key the options screen offers in all four layouts only worked while the debug keys
    ///     were compiled in.
    /// </summary>
    private void QuitToGameOverIfAsked()
    {
        if (_gameEngine.Input.GetPressedKey() != _gameEngine.Session.PlayerKeys.Quit)
        {
            return;
        }

        _gameEngine.ScreenDirectory.Current = GameScreenType.GameOver;
        _gameEngine.ScreenDirectory.GameOver.PerformPostScreenChangeActions();
    }

    /// <summary>
    ///     The debug keys for the main game: J tops up the supplies, T and R the test rumble,
    ///     W jumps to the game complete screen, C replaces every core element, I restarts the
    ///     intro and U runs the unstick routine. Compiled out of a release build entirely by
    ///     <see cref="ConditionalAttribute" />, which removes the calls to this rather than leaving
    ///     a branch that is never taken, so a release has no path to them at all.
    ///     <para>
    ///         The keys that are not debug keys are handled elsewhere and stay live in both: F8
    ///         swaps the artwork, F9 steps the cathode ray tube on a notch, F10 the backdrops,
    ///         F12 fullscreen, and the player's own pause and quit keys.
    ///     </para>
    /// </summary>
    [Conditional("DEBUG")]
    private void HandleTestModeKeys()
    {
        if (_gameEngine.Input.KeyboardState.IsKeyDown(Keys.J))
        {
            _gameEngine.Stats.Platforms = TestModeSuppliesRestored;
            _gameEngine.Stats.FirePower = TestModeSuppliesRestored;
        }

        if (_gameEngine.Input.KeyboardState.IsKeyDown(Keys.T))
        {
            _testRumbleSoundId = _gameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound(Assets.Sounds.Rumble, true);
        }
        else if (_gameEngine.Input.KeyboardState.IsKeyUp(Keys.R))
        {
            if (_testRumbleSoundId != Guid.Empty)
            {
                _gameEngine.ContentResourceManager.GetSoundFxEngine()
                    .StopSound(_testRumbleSoundId, SoundFade.Medium);
            }
        }

        if (_gameEngine.Input.GetPressedKey() == Keys.W)
        {
            JumpToGameCompleteScreen();
        }

        if (_gameEngine.Input.GetPressedKey() == Keys.C)
        {
            ReplaceEveryCoreElement();
        }

        if (_gameEngine.Input.GetPressedKey() == Keys.I)
        {
            _gameEngine.IntroSequence.Reset();
        }

        if (_gameEngine.Input.GetPressedKey() == Keys.U)
        {
            _gameEngine.Player.UnstickBlobFromTiles();
        }
    }

    /// <inheritdoc cref="HandleTestModeKeys" />
    private void JumpToGameCompleteScreen()
    {
        _gameEngine.ScreenDirectory.Current = GameScreenType.GameComplete;
        _gameEngine.ScreenDirectory.GameComplete.PerformPostScreenChangeActions();
    }

    /// <inheritdoc cref="HandleTestModeKeys" />
    private void ReplaceEveryCoreElement()
    {
        foreach (CoreElement element in _gameEngine.Elements.Elements)
        {
            element.ElementState = CoreElementState.Replaced;
        }
    }

    private void FadeScreenBackUp()
        => ScreenFade = Ramp.Up(ScreenFade, ScreenFadeSpeed, ColourLevels.Full);

    private void UpdateBlob(GameTime gameTime)
    {
        if (!_gameEngine.ScreenEffectEngine.HaltBlobAndNasties)
        {
            _gameEngine.Player.Update(gameTime);
        }

        if (DeathRoutineRunning)
        {
            _gameEngine.BlobKiller.Update(gameTime);
        }
    }

    /// <summary>
    ///     Puts the water in on Olly's screen and takes it out again everywhere else.
    /// </summary>
    /// <remarks>
    ///     The water is the one screen effect that does not run down on its own, so leaving the
    ///     screen has to end it - and that is why this runs on every screen change rather than
    ///     only on the one that arrives at him.
    ///     <para>
    ///         Nothing is done if something else is already running. An effect that is mid-way
    ///         through - a tunnel entry, say - would be cut off by this, and the only way onto
    ///         Olly's screen while one is playing is to walk off the end of it, which ends the
    ///         effect of its own accord a frame later.
    ///     </para>
    /// </remarks>
    private void FloodOllysScreen()
    {
        bool withOlly = _gameEngine.GameScreens.ScreenNumber == ScreenRenderer.OllyScreenNumber;

        ScreenEffectBase? running = _gameEngine.ScreenEffectEngine.ActiveScreenEffect;

        if (withOlly)
        {
            if (running is null)
            {
                _gameEngine.ScreenEffectEngine.ExecuteScreenEffect(
                    ScreenEffectType.Water, -1, -1, false);
            }

            return;
        }

        if (running is { ScreenEffectType: ScreenEffectType.Water })
        {
            _gameEngine.ScreenEffectEngine.StopActiveScreenEffect();
        }
    }

    /// <summary>
    ///     The smoke from the wreck of Blob's ship, which only exists on the screen it crashed on.
    /// </summary>
    private void UpdateShipSmoke(GameTime gameTime)
    {
        if (_gameEngine.GameScreens.ScreenNumber == _gameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            _gameEngine.GameScreens.BlobsShipSmoke1.Start();
            _gameEngine.GameScreens.BlobsShipSmoke1.Update(
                _gameEngine.IntroSequence.BlobSpaceShipXPos,
                _gameEngine.IntroSequence.BlobSpaceShipYPos, gameTime);

            _gameEngine.GameScreens.BlobsShipSmoke2.Start();
            _gameEngine.GameScreens.BlobsShipSmoke2.Update(
                _gameEngine.IntroSequence.BlobSpaceShipXPos,
                _gameEngine.IntroSequence.BlobSpaceShipYPos, gameTime);
        }
    }

    private void UpdateWorld(GameTime gameTime)
    {
        if (!_gameEngine.IntroSequence.SequenceRunning)
        {
            if (!_gameEngine.ScreenEffectEngine.HaltBlobAndNasties)
            {
                _gameEngine.Nasties.Update(gameTime);
            }
        }

        _gameEngine.TunnelAnimations.Update();
        _gameEngine.ConveyorNastiesInstance.Update(gameTime);
        _gameEngine.ElectricityNodeGateEngine.Update();

        _gameEngine.BreakableBridgesEngine.Update();
    }

    private void EndGameIfOutOfLives()
    {
        if (_gameEngine.Player.BlobsState != BlobState.GameOver)
        {
            return;
        }

        _gameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);

        _gameEngine.ScreenDirectory.GameOver.PerformPostScreenChangeActions();
        _gameEngine.ScreenDirectory.Current = GameScreenType.GameOver;
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
    /// <summary>
    ///     Knocks the picture about for a moment, for something that has just exploded.
    /// </summary>
    public void JoltScreen() => _shake.Jolt();

    private void ShakeScreen()
    {
        // Null while a screen effect is composing the picture: the view port is its to move, and the
        // rumble is left exactly as it was rather than stopped.
        if (_shake.Step(
                _gameEngine.RandomNumberGenerator,
                _gameEngine.IntroSequence.SequenceRunning,
                _gameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause,
                _gameEngine.ScreenEffectEngine.ActiveScreenEffect != null) is not { } shake)
        {
            return;
        }

        if (shake.Rumbling)
        {
            _shakeRumbleSoundId = _gameEngine.ContentResourceManager.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.Rumble, true);
        }
        else
        {
            _gameEngine.ContentResourceManager.GetSoundFxEngine()
                .StopSound(_shakeRumbleSoundId, SoundFade.Medium);
        }

        if (shake.MovesTheViewPort)
        {
            _gameEngine.ScreenEffectEngine.MoveViewPort(ViewPorts.GameArea, shake.XOffset, shake.YOffset);
        }
    }

    public void ResetScreenFade(int fadeSpeed)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = fadeSpeed;
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
    ///         The fades are worked out in floating point. Written as
    ///         <c>
    ///             BackgroundOpacity /
    ///             ColourLevels.Full * ScreenFade
    ///         </c>
    ///         it was an integer division of 150 by 255, which is 0 -
    ///         so the planet was drawn on every frame at an alpha of nothing, and had been since the
    ///         first commit in the repository. <see cref="ColourHelper.GetXna4Colour(int,int,int,int)" /> had the right
    ///         of it all along.
    ///     </para>
    /// </remarks>
    private void DrawBackground()
    {
        if (!_gameEngine.Presentation.DrawBackgrounds)
        {
            return;
        }

        int screenColumn = _gameEngine.GameScreens.ScreenNumber % WorldMap.ScreensAcross;

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
        SpriteCollection layer = _gameEngine.ContentResourceManager.GetSpriteCollection(spriteName);

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

    private void DrawBlobDeath()
    {
        if (DeathRoutineRunning)
        {
            _gameEngine.BlobKiller.Draw(ScreenFade);
        }
    }

    private void DrawNasties()
    {
        _gameEngine.Nasties.Draw(ScreenFade);
        _gameEngine.ConveyorNastiesInstance.Draw(ScreenFade);
    }

    private void DrawUpperGraphics()
    {
        _gameEngine.ElectricityNodeGateEngine.DrawUpperLayer();
        _gameEngine.GameScreens.DrawUpperLayer(
            ColourHelper.WhiteFadedTo((byte)ScreenFade));
    }

    private void DrawShipSmoke1()
    {
        if (_gameEngine.GameScreens.ScreenNumber == _gameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            _gameEngine.GameScreens.BlobsShipSmoke1.Draw(ScreenFade);
        }
    }

    private void DrawShipSmoke2()
    {
        if (_gameEngine.GameScreens.ScreenNumber == _gameEngine.GameScreens.LandingSite.ScreenNumber)
        {
            _gameEngine.GameScreens.BlobsShipSmoke2.Draw(ScreenFade);
        }
    }
}
