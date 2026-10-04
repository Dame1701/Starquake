// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.GameComponents;
using Starquake.GameComponents.GameScreens;
using Starquake.Particles;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Animations;

internal class IntroSequence
{
    /// <summary>
    ///     One whole revolution in degrees, which is where <see cref="_shipRotation" /> wraps.
    /// </summary>
    private const float FullTurn = 360f;

    /// <summary>
    ///     How fast the tumble slows once the ship has hit, in degrees per logic frame off the
    ///     rotation speed. It divides the speed the ship enters at, so the spin stops dead rather
    ///     than reversing.
    /// </summary>
    private const float TumbleSlowingStep = 2;

    /// <summary>
    ///     How fast the crash darkens into the caption, and how fast the caption itself comes up
    ///     and goes back down, in colour levels per logic frame.
    /// </summary>
    private const int CrashFadeOutStep = 2;

    /// <inheritdoc cref="CrashFadeOutStep" />
    private const int CaptionFadeStep = 3;

    /// <summary>
    ///     How hard the ground shakes when the ship hits, and how fast that dies away. The shake is
    ///     a random offset of up to this many pixels in each direction.
    /// </summary>
    private const float RumbleFrom = 100;

    /// <inheritdoc cref="RumbleFrom" />
    private const float RumbleFadeStep = 0.5f;

    /// <summary>
    ///     How long the screen holds still before the ship comes into view, and how long it holds
    ///     on the wreck before the caption, in logic frames. A second each.
    /// </summary>
    private const int StartPauseFrames = LogicClock.FramesPerSecond;

    /// <inheritdoc cref="StartPauseFrames" />
    private const int PreFadeOutPauseFrames = LogicClock.FramesPerSecond;

    /// <summary>
    ///     How long the wreck is left on screen after the caption has gone before the game takes
    ///     over, in logic frames. Four seconds, which is the rumble dying out.
    /// </summary>
    private const int EndPauseFrames = LogicClock.FramesPerSecond * 4;

    private readonly BlobDeathAnimation _blobKiller;


    private readonly FaderAnimation _blobShipHazardLightFlash;
    private readonly RandomFaderAnimation _blobShipHeadLightFlash;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly PresentationSettings _presentation;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly ScreenEffectEngine _screenEffects;

    /// <summary>
    ///     The ship's fall, replaced whole at the start of each sequence. Where it comes in, where
    ///     it is going and how fast it is dropping are all its own.
    /// </summary>
    private ShipDescent _descent = new(Point.Zero);
    private int _endPauseFrameCounter;

    private int _preFadeOutPauseCounter;

    private float _rumbleAmount = RumbleFrom;
    private Guid _rumbleSound = Guid.Empty;

    /// <summary>
    ///     Logic frames since the ship came to rest, which is what the wreck's electrics are timed
    ///     off. It keeps counting for the rest of the game: the wreck stays on the landing site and
    ///     goes on failing for as long as Blob is there to see it.
    /// </summary>
    private int _wreckFrame;

    /// <summary>
    ///     The sparks the wreck throws. The same effect Blob's own fragments strike off the ground
    ///     with - a wreck spitting and a part of him bouncing are the same small shower.
    /// </summary>
    private readonly IPositionalTriggerParticleEffect _wreckSparks;



    private Guid _shipExplosionSound = Guid.Empty;

    /// <summary>
    ///     Which way round the ship is drawn, in degrees. Held here and handed to all four sheets the
    ///     ship is composed from, so they turn together: each sheet used to keep its own angle and
    ///     advance it as it was drawn, which tied the spin to the display's refresh rate and left any
    ///     sheet that a frame skipped behind the others.
    /// </summary>
    private float _shipRotation;

    private float _shipRotationSpeed;

    private int _startPauseCounter;

    public IntroSequence(
        ResourceManager resources,
        BitmapFont font,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        PresentationSettings presentation,
        ScreenDirectory screenDirectory,
        ScreenEffectEngine screenEffects,
        BlobDeathAnimation blobKiller,
        Random randomNumberGenerator)
    {
        _resources = resources;
        _font = font;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _presentation = presentation;
        _screenDirectory = screenDirectory;
        _screenEffects = screenEffects;
        _blobKiller = blobKiller;

        _wreckSparks = (IPositionalTriggerParticleEffect)_resources.GetParticleEffectInstance(
            Assets.Effects.FireSparks, ParticleEffectType.TriggeredMultiLocationTimed, null);
        _randomNumberGenerator = randomNumberGenerator;

        _blobShipHazardLightFlash = new FaderAnimation(10);
        _blobShipHeadLightFlash = new RandomFaderAnimation(4, _randomNumberGenerator, 100);
    }

    /// <summary>
    ///     Whether the crash is still playing. The game reads it to know whether to let Blob move,
    ///     and the sequence is the only thing that sets it.
    /// </summary>
    public bool SequenceRunning { get; private set; }

    public int BlobSpaceShipXPos => (int)_descent.Position.X + _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip).SpriteWidth / 2;

    public int BlobSpaceShipYPos => (int)_descent.Position.Y + _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip).SpriteHeight / 2;
    /// <summary>
    ///     The caption that plays once Blob's ship has come down: what he is here for, and the
    ///     joke the game tells about it, in one breath.
    /// </summary>
    /// <remarks>
    ///     This was "Prepare for mission Starquake ...", which was wrong twice. It is not a
    ///     briefing - the caption comes *after* the crash, so there is nothing left to prepare for
    ///     - and it is written in a voice the game does not use anywhere else. The 1985 inlay is
    ///     dramatic and then immediately undercuts itself: "If the core of this planet is not
    ///     rebuilt it will implode causing the whole universe to go 'Ka Boomf!' - Strewth, a
    ///     Starquake...", and on why Blob was chosen for it, "Nope - 'Cause all the other guys
    ///     have pranged their spaceships." These lines are that sentence, cut to fit, and the
    ///     "Betta" is the inlay's register rather than a typo - it belongs with "Strewth" and
    ///     "'Cause", and correcting it to "Better" would take the voice out of it.
    /// </remarks>
    private static readonly string[] Caption =
    [
        "Betta rebuild that core",
        "or the universe goes Ka Boomf!"
    ];

    /// <summary>
    ///     Where the caption sits and how far apart its lines are. Centred on the playfield rather
    ///     than indented by a number that happened to suit the old caption's length - the playfield
    ///     and not the composition, because the intro is drawn into the game area's own render
    ///     target, which is 1024 wide against the composition's 1280. Centring on the wider of the
    ///     two put the caption 128 pixels right of where it belonged. That the old caption's
    ///     hand-set indent of 180 is almost exactly where centring in 1024 lands it is the clue
    ///     that said so - which
    ///     is also why it is broken across lines at all: fifty-three characters will not go across
    ///     the screen at this size, where the thirty the break leaves will.
    /// </summary>
    private const int CaptionTopLine = 180;

    /// <inheritdoc cref="CaptionTopLine" />
    private const int CaptionLineSpacing = 56;

    public IntroSequenceStage SequenceStage { get; private set; } = IntroSequenceStage.None;

    /// <summary>
    ///     The screen the sequence fades up over. Looked up rather than held, so the sequence cannot
    ///     be built before the screen it needs exists.
    /// </summary>
    private MainGameScreen MainScreen => _screenDirectory.MainGame;

    public void Reset()
    {
        MainScreen.ScreenFade = ColourLevels.Full;

        _rumbleAmount = RumbleFrom;

        _preFadeOutPauseCounter = 0;
        _startPauseCounter = 0;
        _endPauseFrameCounter = 0;

        SequenceStage = IntroSequenceStage.InitialPause;
        SequenceRunning = true;

        _shipRotationSpeed = 10f;

        // Where it comes down is the square the author marked on the map. The ship is drawn from its
        // top left and the two graphics sets draw it at different sizes, so the square's middle is
        // what the two have in common and half a ship is taken off it here.
        SpriteCollection ship = _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip);
        Point crashCentre = _gameScreens.ShipCrashCentre;

        _descent = new ShipDescent(new Point(
            crashCentre.X - ship.SpriteWidth / 2,
            crashCentre.Y - ship.SpriteHeight / 2));


        _resources.GetSoundFxEngine().StopSound(_shipExplosionSound, SoundFade.None);

        _gameScreens.BlobsShipSmoke1.Reset();
        _gameScreens.BlobsShipSmoke2.Reset();
    }

    /// <summary>
    ///     The stages run in the order they are tested here. InitialPause is its own test rather than part
    ///     of the chain because it hands straight on to ShipFalling in the same frame.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _shipRotation = _shipRotation < FullTurn
            ? _shipRotation + _shipRotationSpeed
            : 0f;

        if (SequenceStage == IntroSequenceStage.InitialPause)
        {
            UpdateInitialPause();
        }

        if (SequenceStage == IntroSequenceStage.ShipFalling)
        {
            UpdateShipFalling();
        }
        else if (SequenceStage == IntroSequenceStage.ShipExploding)
        {
            UpdateShipExploding();
        }
        else if (SequenceStage == IntroSequenceStage.PreFadePause)
        {
            UpdatePreFadePause(gameTime);
        }
        else if (SequenceStage == IntroSequenceStage.CaptionFadeUp)
        {
            UpdateCaptionFadeUp();
        }
        else if (SequenceStage == IntroSequenceStage.CaptionFadeDown)
        {
            UpdateCaptionFadeDown();
        }
        else if (SequenceStage == IntroSequenceStage.EndPause || SequenceStage == IntroSequenceStage.Finished)
        {
            UpdateEndPause(gameTime);
        }
    }

    private void UpdateInitialPause()
    {
        if (_startPauseCounter < StartPauseFrames)
        {
            if (_startPauseCounter == 0)
            {
                _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobShipFall);
            }

            _startPauseCounter++;
        }
        else
        {
            SequenceStage = IntroSequenceStage.ShipFalling;
        }
    }

    private void UpdateShipFalling()
    {
        if (!_descent.Landed)
        {
            _descent.Step();

            return;
        }

        SequenceStage = IntroSequenceStage.ShipExploding;
        _blobKiller.Trigger(_descent.Rest.X, _descent.Rest.Y, false, 100, 200);
    }

    private void UpdateShipExploding()
    {
        SequenceStage = IntroSequenceStage.PreFadePause;
        _shipExplosionSound = _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobShipExplodes);
        _presentation.ScreenClearColour = Color.White;
    }

    private void UpdatePreFadePause(GameTime gameTime)
    {
        _presentation.ScreenClearColour = Color.Black;
        _shipRotationSpeed = Ramp.Down(_shipRotationSpeed, TumbleSlowingStep, 0f);

        if (_preFadeOutPauseCounter >= PreFadeOutPauseFrames)
        {
            if (MainScreen.ScreenFade > ColourLevels.None)
            {
                MainScreen.ScreenFade = Ramp.Down(MainScreen.ScreenFade, CrashFadeOutStep, ColourLevels.None);
            }
            else
            {
                MainScreen.ResetScreenFade(CrashFadeOutStep);
                MainScreen.ScreenFade = 0;
                SequenceStage = IntroSequenceStage.CaptionFadeUp;

                // The wreckage stops here. It is thrown for a hundred to two hundred frames, and
                // the intro stops updating it at this point - but MainGameScreen picks it straight
                // back up, because its DeathRoutineRunning counts BlobKiller.Active and the parts
                // are still in the air. So the ship's fragments went on bouncing and clanging
                // through the caption and into the game, which is the noise that has been heard
                // over the opening for as long as anybody can remember. By now the screen has
                // faded to black and there is nothing of them left to see.
                _blobKiller.Reset();
                _resources.GetSoundFxEngine().StopSound(_shipExplosionSound, SoundFade.Fast);
            }
        }
        else
        {
            _preFadeOutPauseCounter++;
        }

        if (_rumbleAmount > 0)
        {
            int xShakeOffset = _randomNumberGenerator.Next(0, (int)_rumbleAmount);
            int yShakeOffset = _randomNumberGenerator.Next(0, (int)_rumbleAmount);

            _rumbleAmount = Ramp.Down(_rumbleAmount, RumbleFadeStep, 0f);

            _screenEffects.MoveViewPort(ViewPorts.GameArea, xShakeOffset, yShakeOffset);

            _rumbleSound = _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.Rumble, true);
        }

        _blobKiller.Update(gameTime);
    }

    private void UpdateCaptionFadeUp()
    {
        if (MainScreen.ScreenFade < ColourLevels.Full)
        {
            MainScreen.ScreenFade = Ramp.Up(MainScreen.ScreenFade, CaptionFadeStep, ColourLevels.Full);
        }
        else
        {
            SequenceStage = IntroSequenceStage.CaptionFadeDown;
        }
    }

    private void UpdateCaptionFadeDown()
    {
        if (MainScreen.ScreenFade > ColourLevels.None)
        {
            MainScreen.ScreenFade = Ramp.Down(MainScreen.ScreenFade, CaptionFadeStep, ColourLevels.None);
        }
        else
        {
            SequenceStage = IntroSequenceStage.EndPause;
        }
    }

    /// <remarks>
    ///     Takes the clock only for the wreck's sparks, which outlive the sequence: this is the
    ///     one update that keeps running once the intro has finished.
    /// </remarks>
    private void UpdateEndPause(GameTime gameTime)
    {
        if (SequenceStage == IntroSequenceStage.EndPause)
        {
            if (_endPauseFrameCounter < EndPauseFrames)
            {
                _endPauseFrameCounter++;
            }
            else
            {
                _resources.GetSoundFxEngine().StopSound(_rumbleSound, SoundFade.Fast);
                SequenceStage = IntroSequenceStage.Finished;
                SequenceRunning = false;
            }
        }

        _blobShipHazardLightFlash.Update();
        _blobShipHeadLightFlash.Update();

        SparkOffTheWreck(gameTime);

        _wreckFrame++;
    }

    /// <summary>
    ///     The occasional spark off the wreck once it has come to rest. Started on one frame and
    ///     left to live its own half second, which is why this only has to decide when and where.
    /// </summary>
    /// <remarks>
    ///     Only once the ship is down and still - while it is falling and exploding there is
    ///     already more than enough happening to it - and only for the redrawn artwork, as the
    ///     rest of the 1985 set is left as it was.
    /// </remarks>
    private void SparkOffTheWreck(GameTime gameTime)
    {
        if (_graphicsSet.IsOriginal ||
            SequenceStage is not (IntroSequenceStage.Finished or IntroSequenceStage.EndPause))
        {
            return;
        }

        if (ShipWreckSettings.SparksThisFrame(_wreckFrame))
        {
            SpriteCollection hull = _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip);

            Point from = ShipWreckSettings.SparkAt(
                _wreckFrame, hull.SpriteWidth, hull.SpriteHeight);

            _wreckSparks.Start(
                _descent.Rest.X + hull.SpriteWidth / 2 + from.X,
                _descent.Rest.Y + hull.SpriteHeight / 2 + from.Y);
        }

        _wreckSparks.Update(gameTime);
    }

    public void Draw()
    {
        // The ship is not on screen for either caption, which bookend the sequence.
        if (SequenceStage != IntroSequenceStage.CaptionFadeUp && SequenceStage != IntroSequenceStage.CaptionFadeDown)
        {
            if (_graphicsSet.IsOriginal)
            {
                SpriteCollection originalShip = _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip);
                originalShip.Begin(BlendState.AlphaBlend);

                // Upright and square on the crash site once it has landed, still tumbling before that.
                // The resting position used to be a pair of numbers written down here as well, which
                // happened to be where the fall ended - so it is asked for rather than repeated. It
                // cannot be the live position: the fall overshoots its target by up to a frame's
                // travel, and the rotating overload leaves the ship at whatever angle it stopped at.
                if (SequenceStage == IntroSequenceStage.Finished || SequenceStage == IntroSequenceStage.CaptionFadeUp || SequenceStage == IntroSequenceStage.EndPause)
                {
                    originalShip.DrawSprite(0, _descent.Rest.X, _descent.Rest.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade));
                }
                else
                {
                    originalShip.DrawSprite(0, _descent.Position.X, _descent.Position.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, MathHelper.ToRadians(_shipRotation));
                }

                originalShip.End();
            }
            else
            {
                SpriteCollection blobsShip = _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip);
                blobsShip.Begin(BlendState.AlphaBlend);
                blobsShip.DrawSprite(0, _descent.Position.X, _descent.Position.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, MathHelper.ToRadians(_shipRotation));
                blobsShip.End();

                if (SequenceStage != IntroSequenceStage.Finished && SequenceStage != IntroSequenceStage.EndPause)
                {
                    SpriteCollection blobsShipHazardLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHazardLights);
                    blobsShipHazardLights.Begin(BlendState.AlphaBlend);
                    blobsShipHazardLights.DrawSprite(0, _descent.Position.X, _descent.Position.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, 0), 1.0f, MathHelper.ToRadians(_shipRotation));
                    blobsShipHazardLights.End();

                    SpriteCollection blobsShipHeadLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHeadLights);
                    blobsShipHeadLights.Begin(BlendState.AlphaBlend);
                    blobsShipHeadLights.DrawSprite(0, _descent.Position.X, _descent.Position.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, MathHelper.ToRadians(_shipRotation));
                    blobsShipHeadLights.End();
                }
                else
                {
                    SpriteCollection blobsShipHazardLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHazardLights);
                    blobsShipHazardLights.Begin(BlendState.AlphaBlend);
                    blobsShipHazardLights.DrawSprite(0, _descent.Position.X, _descent.Position.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_blobShipHazardLightFlash.FadeLevel * ((double)MainScreen.ScreenFade / ColourLevels.Full))), 1.0f, MathHelper.ToRadians(_shipRotation));
                    blobsShipHazardLights.End();

                    SpriteCollection blobsShipHeadLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHeadLights);
                    blobsShipHeadLights.Begin(BlendState.AlphaBlend);
                    blobsShipHeadLights.DrawSprite(0, _descent.Position.X, _descent.Position.Y, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_blobShipHeadLightFlash.FadeLevel * ((double)MainScreen.ScreenFade / ColourLevels.Full))), 1.0f, MathHelper.ToRadians(_shipRotation));
                    blobsShipHeadLights.End();
                }
            }

            if (SequenceStage == IntroSequenceStage.PreFadePause)
            {
                _blobKiller.Draw(MainScreen.ScreenFade);
            }

            _wreckSparks.Draw(MainScreen.ScreenFade);
        }
        else
        {
            _font.Begin(BlendState.AlphaBlend);

            for (int line = 0; line < Caption.Length; line++)
            {
                _font.WriteTextCentred(
                    TileMetrics.PlayAreaWidth,
                    CaptionTopLine + line * CaptionLineSpacing,
                    Caption[line],
                    2,
                    Color.White,
                    (byte)MainScreen.ScreenFade);
            }

            _font.End();
        }
    }
}
