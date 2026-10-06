// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;
using Starquake.Tuning;
using Starquake.World;

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
    private readonly GameplayContext _context;

    private readonly RunningGame _game;

    private readonly ScreenShake _shake = new();

    private int _framesPlayed;

    private Guid _shakeRumbleSoundId = Guid.Empty;

    private Guid _testRumbleSoundId;

    public MainGameScreen(RunningGame game, GameplayContext context)
    {
        _game = game;
        _context = context;

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
        !_game.IntroSequence.SequenceRunning ||
        _game.IntroSequence.SequenceStage == IntroSequenceStage.EndPause;

    /// <summary>
    ///     Whether Blob's death is still playing out. Blob is revived the frame after he reaches
    ///     <see cref="BlobState.Dead" />, so the animation's own state has to keep the routine alive
    ///     while the explosion fades out - which is why this asks it as well as asking Blob.
    /// </summary>
    private bool DeathRoutineRunning
        => _game.Player.State is BlobState.Dead or BlobState.Dying or BlobState.Overloading ||
           _game.BlobKiller.Active;

    /// <summary>
    ///     Whether the world is on screen at all. It is hidden while the intro sequence's caption
    ///     fades in and out over it.
    /// </summary>
    private bool WorldIsVisible =>
        _game.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeUp &&
        _game.IntroSequence.SequenceStage != IntroSequenceStage.CaptionFadeDown;

    public void Update(GameTime gameTime)
    {
        _framesPlayed++;

        QuitToGameOverIfAsked();

        HandleTestModeKeys();

        if (GameIsInPlay)
        {
            FadeScreenBackUp();
        }

        _context.ScreenEffects.Update();
        _game.GameScreens.Update(
            gameTime,
            _game.Player.XPos,
            _game.Player.YPos,
            _game.Player.Carrying.Contains(Collectables.Key));

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

        if (_game.IntroSequence.SequenceRunning ||
            _game.IntroSequence.SequenceStage == IntroSequenceStage.Finished)
        {
            _game.IntroSequence.Update(gameTime);
        }
    }

    public void Draw()
    {
        if (WorldIsVisible)
        {
            DrawBackground();

            _game.Electricity.Draw();

            _game.GameScreens.Renderer.Draw(
                Colours.WhiteFadedTo((byte)ScreenFade),
                _game.IntroSequence.SequenceRunning, _game.IntroSequence.SequenceStage);


            _game.BreakableBridges.Draw();

            if (GameIsInPlay)
            {
                _game.Player.Draw(ScreenFade);
            }

            if (!_game.IntroSequence.SequenceRunning)
            {
                DrawBlobDeath();
                DrawNasties();
            }

            DrawUpperGraphics();

            _game.TunnelLiftArrows.Draw((byte)ScreenFade);

            DrawShipSmoke1();

            if (GameIsInPlay)
            {
                _game.GameScreens.Renderer.DrawShadowing();
            }

            // Last, so Blob's sparks are in front of the shadows down the screen exits rather than
            // under them.
            _game.Player.DrawUpper(ScreenFade);
        }

        if (_game.GameScreens.ScreenNumber == _game.GameScreens.LandingSite.ScreenNumber)
        {
            _game.IntroSequence.Draw();
        }

        if (WorldIsVisible)
        {
            DrawShipSmoke2();
        }
    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, ScreenChange change)
    {
        _context.Resources.GetSoundFxEngine().DisablePeriodicBackgroundSounds();

        if (triggeringScreen != GameScreenType.AccessBooth && triggeringScreen != GameScreenType.Cheops)
        {
            _game.Player.PerformPostChangeScreenActions();
            _game.ConveyorNasties.PerformPostChangeScreenActions();
            _game.Nasties.PerformPostChangeScreenActions();

            _game.Electricity.PerformPostChangeScreenActions();
            _context.Resources.GetSoundFxEngine().KillSounds(false);

            _game.TunnelLiftArrows.PerformPostChangeScreenActions();
        }

        if (change is ScreenChange.FadeIn)
        {
            ScreenFade = 0;
            ScreenFadeSpeed = ScreenFadeUpSpeed;
        }

        if (triggeringScreen == GameScreenType.MainGame)
        {
            _context.ScreenEffects.SelectRenderViewPort(ViewPorts.GameArea);
        }

        ScoreFirstVisitToThisScreen();

        FloodOllysScreen();

        // Walking onto the screen that carries the core hands over to the core screen entirely. Which
        // screen that is comes off the map.
        if (_game.GameScreens.ScreenNumber == _game.GameScreens.CoreSite.ScreenNumber)
        {
            _context.ScreenDirectory.Current = GameScreenType.PlanetCore;
        }

        if (triggeringScreen == GameScreenType.Teleporter)
        {
            ScreenFadeSpeed = 16;
            _game.Player.TriggerTeleportedAnimation();

            // The room arrives unsettled and shivers itself straight. Blob is not held still for
            // it - he has just got here and should be able to walk.
            _context.ScreenEffects.ExecuteScreenEffect(
                ScreenEffectType.TeleportArrival, -1, TeleportArrivalSettings.Frames, false);
            _game.GameScreens.ColourAnimations.Start((int)ActionTiles.TeleportBooth, TeleporterAnimationLoops); // Animate the teleporter colour
        }

        // Flash the booth green or red to say whether the code was taken. Only AccessBoothScreen
        // sends this, so the triggering screen no longer has to be checked as well.
        if (change is ScreenChange.AccessCode(var accepted))
        {
            Color flash = accepted ? new Color(0, ColourLevels.Full, 0) : new Color(ColourLevels.Full, 0, 0);
            int speed = accepted ? AccessBoothPassedSpeed : AccessBoothFailedSpeed;

            _game.GameScreens.ColourAnimations.Start(
                (int)ActionTiles.AccessBoothLeft, AccessBoothAnimationLoops, flash, speed);
            _game.GameScreens.ColourAnimations.Start(
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
        if (_game.GameScreens.CurrentGameScreenData.ScreenVisited)
        {
            return;
        }

        _game.GameScreens.CurrentGameScreenData.ScreenVisited = true;
        _context.Stats.PlayerScore += ScoreForReachingCore;
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
        => _game.TunnelLiftArrows.PerformPostChangeScreenActions();

    public void LoadContent() => _game.Nasties.LoadContent();

    /// <summary>
    ///     A new game: the clock, the score, the core, the planet and everything on it back to how
    ///     they start.
    /// </summary>
    public void Reset()
    {
        _framesPlayed = 0;
        _context.Session.Paused = false;
        _context.Stats.Reset();
        _context.Elements.Reset();
        _game.GameScreens.Reset(_context.Elements.ElementsWanted());
        _game.Player.Reset();
        _game.Nasties.Reset();
        _game.IntroSequence.Reset();

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
        if (_context.Input.GetPressedKey() != _context.Session.PlayerKeys.Quit)
        {
            return;
        }

        _context.ScreenDirectory.Current = GameScreenType.GameOver;
        _context.ScreenDirectory.GameOver.PerformPostScreenChangeActions();
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
        if (_context.Input.KeyboardState.IsKeyDown(Keys.J))
        {
            _context.Stats.Platforms = TestModeSuppliesRestored;
            _context.Stats.FirePower = TestModeSuppliesRestored;
        }

        if (_context.Input.KeyboardState.IsKeyDown(Keys.T))
        {
            _testRumbleSoundId = _context.Resources.GetSoundFxEngine().PlaySound(Assets.Sounds.Rumble, true);
        }
        else if (_context.Input.KeyboardState.IsKeyUp(Keys.R))
        {
            if (_testRumbleSoundId != Guid.Empty)
            {
                _context.Resources.GetSoundFxEngine()
                    .StopSound(_testRumbleSoundId, SoundFade.Medium);
            }
        }

        if (_context.Input.GetPressedKey() == Keys.W)
        {
            JumpToGameCompleteScreen();
        }

        if (_context.Input.GetPressedKey() == Keys.C)
        {
            ReplaceEveryCoreElement();
        }

        if (_context.Input.GetPressedKey() == Keys.I)
        {
            _game.IntroSequence.Reset();
        }

        if (_context.Input.GetPressedKey() == Keys.U)
        {
            _game.Player.UnstickFromTiles();
        }
    }

    /// <inheritdoc cref="HandleTestModeKeys" />
    private void JumpToGameCompleteScreen()
    {
        _context.ScreenDirectory.Current = GameScreenType.GameComplete;
        _context.ScreenDirectory.GameComplete.PerformPostScreenChangeActions();
    }

    /// <inheritdoc cref="HandleTestModeKeys" />
    private void ReplaceEveryCoreElement()
    {
        foreach (CoreElement element in _context.Elements.Elements)
        {
            element.ElementState = CoreElementState.Replaced;
        }
    }

    private void FadeScreenBackUp()
        => ScreenFade = Ramp.Up(ScreenFade, ScreenFadeSpeed, ColourLevels.Full);

    private void UpdateBlob(GameTime gameTime)
    {
        if (!_context.ScreenEffects.HaltBlobAndNasties)
        {
            _game.Player.Update(gameTime);
        }

        if (DeathRoutineRunning)
        {
            _game.BlobKiller.Update(gameTime);
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
        bool withOlly = _game.GameScreens.ScreenNumber == ScreenRenderer.OllyScreenNumber;

        ScreenEffectBase? running = _context.ScreenEffects.ActiveScreenEffect;

        if (withOlly)
        {
            if (running is null)
            {
                _context.ScreenEffects.ExecuteScreenEffect(
                    ScreenEffectType.Water, -1, -1, false);
            }

            return;
        }

        if (running is { ScreenEffectType: ScreenEffectType.Water })
        {
            _context.ScreenEffects.StopActiveScreenEffect();
        }
    }

    /// <summary>
    ///     The smoke from the wreck of Blob's ship, which only exists on the screen it crashed on.
    /// </summary>
    private void UpdateShipSmoke(GameTime gameTime)
    {
        if (_game.GameScreens.ScreenNumber == _game.GameScreens.LandingSite.ScreenNumber)
        {
            _game.GameScreens.BlobsShipSmoke1.Start();
            _game.GameScreens.BlobsShipSmoke1.Update(
                _game.IntroSequence.BlobSpaceShipXPos,
                _game.IntroSequence.BlobSpaceShipYPos, gameTime);

            _game.GameScreens.BlobsShipSmoke2.Start();
            _game.GameScreens.BlobsShipSmoke2.Update(
                _game.IntroSequence.BlobSpaceShipXPos,
                _game.IntroSequence.BlobSpaceShipYPos, gameTime);
        }
    }

    private void UpdateWorld(GameTime gameTime)
    {
        if (!_game.IntroSequence.SequenceRunning)
        {
            if (!_context.ScreenEffects.HaltBlobAndNasties)
            {
                _game.Nasties.Update(gameTime);
            }
        }

        _game.TunnelLiftArrows.Update();
        _game.ConveyorNasties.Update(gameTime);
        _game.Electricity.Update();

        _game.BreakableBridges.Update();
    }

    private void EndGameIfOutOfLives()
    {
        if (_game.Player.State != BlobState.GameOver)
        {
            return;
        }

        _context.Resources.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);

        _context.ScreenDirectory.GameOver.PerformPostScreenChangeActions();
        _context.ScreenDirectory.Current = GameScreenType.GameOver;
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
                _context.RandomNumberGenerator,
                _game.IntroSequence.SequenceRunning,
                _game.IntroSequence.SequenceStage == IntroSequenceStage.EndPause,
                _context.ScreenEffects.ActiveScreenEffect != null) is not { } shake)
        {
            return;
        }

        if (shake.Rumbling)
        {
            _shakeRumbleSoundId = _context.Resources.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.Rumble, true);
        }
        else
        {
            _context.Resources.GetSoundFxEngine()
                .StopSound(_shakeRumbleSoundId, SoundFade.Medium);
        }

        if (shake.MovesTheViewPort)
        {
            _context.ScreenEffects.MoveViewPort(ViewPorts.GameArea, shake.XOffset, shake.YOffset);
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
    ///         first commit in the repository. <see cref="Colours.GetXna4Colour(int,int,int,int)" /> had the right
    ///         of it all along.
    ///     </para>
    /// </remarks>
    private void DrawBackground()
    {
        if (!_context.Presentation.DrawBackgrounds)
        {
            return;
        }

        int screenColumn = _game.GameScreens.ScreenNumber % WorldMap.ScreensAcross;

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
        SpriteCollection layer = _context.Resources.GetSpriteCollection(spriteName);

        layer.Begin(BlendState.AlphaBlend);
        layer.DrawSprite(
            0,
            xOffset,
            0,
            Colours.GetXna4Colour(
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
            _game.BlobKiller.Draw(ScreenFade);
        }
    }

    private void DrawNasties()
    {
        _game.Nasties.Draw(ScreenFade);
        _game.ConveyorNasties.Draw(ScreenFade);
    }

    private void DrawUpperGraphics()
    {
        _game.Electricity.DrawUpperLayer();
        _game.GameScreens.Renderer.DrawUpperLayer(
            Colours.WhiteFadedTo((byte)ScreenFade));
    }

    private void DrawShipSmoke1()
    {
        if (_game.GameScreens.ScreenNumber == _game.GameScreens.LandingSite.ScreenNumber)
        {
            _game.GameScreens.BlobsShipSmoke1.Draw(ScreenFade);
        }
    }

    private void DrawShipSmoke2()
    {
        if (_game.GameScreens.ScreenNumber == _game.GameScreens.LandingSite.ScreenNumber)
        {
            _game.GameScreens.BlobsShipSmoke2.Draw(ScreenFade);
        }
    }
}
