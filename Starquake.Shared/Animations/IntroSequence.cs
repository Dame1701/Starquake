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
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Animations;

internal class IntroSequence
{
    /// <summary>
    ///     Where the ship comes into view, off the top of the screen and to the left of wherever it is
    ///     going to land. Unlike the crash site this is not on the map: nothing about it is a place on
    ///     a screen, it is just far enough out to be off the picture when the fall starts.
    /// </summary>
    private const int ShipEntryXPos = 200;

    /// <inheritdoc cref="ShipEntryXPos" />
    private const int ShipEntryYPos = -160;

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
    private float _acceleration;

    private int _blobSpaceShipEndXPos;
    private int _blobSpaceShipEndYPos;

    private float _blobSpaceShipXPos, _blobSpaceShipYPos;

    private int _endPauseFrameCount;
    private int _endPauseFrameCounter;

    private int _preFadeOutPause = 50;
    private int _preFadeOutPauseCounter;

    private double _rumbleAmount = 100;
    private Guid _rumbleSound = Guid.Empty;

    private int _sequenceFrameCount;

    private Guid _shipExplosionSound = Guid.Empty;

    private float _shipRotationSpeed;
    private int _startPauseCounter;

    private int _startPauseFrameCount;
    private float _xAmountToMoveEachFrame;

    private float _yAmountToMoveEachFrame;
    public bool SequenceRunning;

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
        _randomNumberGenerator = randomNumberGenerator;

        _blobShipHazardLightFlash = new FaderAnimation(10);
        _blobShipHeadLightFlash = new RandomFaderAnimation(4, _randomNumberGenerator, 100);
    }

    public int BlobSpaceShipXPos => (int)_blobSpaceShipXPos + _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip).SpriteWidth / 2;

    public int BlobSpaceShipYPos => (int)_blobSpaceShipYPos + _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip).SpriteHeight / 2;
    public IntroSequenceStage SequenceStage { get; private set; } = IntroSequenceStage.None;

    /// <summary>
    ///     The screen the sequence fades up over. Looked up rather than held, so the sequence cannot
    ///     be built before the screen it needs exists.
    /// </summary>
    private MainGameScreen MainScreen => _screenDirectory.MainGame;

    public void Reset()
    {
        MainScreen.ScreenFade = ColourLevels.Full;

        _rumbleAmount = 100;

        _preFadeOutPause = 50;
        _preFadeOutPauseCounter = 0;

        _startPauseFrameCount = 50;
        _startPauseCounter = 0;

        _endPauseFrameCount = 200;
        _endPauseFrameCounter = 0;

        SequenceStage = IntroSequenceStage.InitialPause;
        SequenceRunning = true;

        _shipRotationSpeed = 10f;
        _sequenceFrameCount = 20;

        // Where it comes down is the square the author marked on the map. The ship is drawn from its
        // top left and the two graphics sets draw it at different sizes, so the square's middle is
        // what the two have in common and half a ship is taken off it here.
        SpriteCollection ship = _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip);
        Point crashCentre = _gameScreens.ShipCrashCentre;

        _blobSpaceShipEndXPos = crashCentre.X - ship.SpriteWidth / 2;
        _blobSpaceShipEndYPos = crashCentre.Y - ship.SpriteHeight / 2;

        _xAmountToMoveEachFrame = (_blobSpaceShipEndXPos - (float)ShipEntryXPos) / _sequenceFrameCount;
        _yAmountToMoveEachFrame = (_blobSpaceShipEndYPos - (float)ShipEntryYPos) / _sequenceFrameCount;

        _blobSpaceShipXPos = ShipEntryXPos;
        _blobSpaceShipYPos = ShipEntryYPos;
        _acceleration = 0;


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
        if (SequenceStage == IntroSequenceStage.InitialPause)
        {
            UpdateInitialPause();
        }

        if (SequenceStage == IntroSequenceStage.ShipFalling)
        {
            UpdateShipFalling();
        }
        else if (SequenceStage == IntroSequenceStage.ShipExploading)
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
            UpdateEndPause();
        }
    }

    private void UpdateInitialPause()
    {
        if (_startPauseCounter < _startPauseFrameCount)
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
        //if (sequenceCounter < sequenceFrameCount)
        if (_blobSpaceShipYPos < _blobSpaceShipEndYPos)
        {
            _blobSpaceShipXPos = _blobSpaceShipXPos + _xAmountToMoveEachFrame;
            _blobSpaceShipYPos = _blobSpaceShipYPos + _yAmountToMoveEachFrame * _acceleration;

            if (_blobSpaceShipYPos + 100 > _blobSpaceShipEndYPos)
            {
                _acceleration = 1f;
            }
            else
            {
                _acceleration = _acceleration + 0.1f;
            }
        }
        else
        {
            SequenceStage = IntroSequenceStage.ShipExploading;
            _blobKiller.Trigger(_blobSpaceShipEndXPos, _blobSpaceShipEndYPos, false, 100, 200);
        }
    }

    private void UpdateShipExploding()
    {
        SequenceStage = IntroSequenceStage.PreFadePause;
        _shipExplosionSound = _resources.GetSoundFxEngine().PlaySound("BlobShipExplodes");
        _presentation.ScreenClearColour = Color.White;
    }

    private void UpdatePreFadePause(GameTime gameTime)
    {
        _presentation.ScreenClearColour = Color.Black;
        if (_shipRotationSpeed > 0)
        {
            _shipRotationSpeed = _shipRotationSpeed - 2;
        }

        if (_preFadeOutPauseCounter >= _preFadeOutPause)
        {
            if (MainScreen.ScreenFade > 0)
            {
                if (MainScreen.ScreenFade - 2 < 0)
                {
                    MainScreen.ScreenFade = 0;
                }
                else
                {
                    MainScreen.ScreenFade = MainScreen.ScreenFade - 2;
                }
            }
            else
            {
                MainScreen.ResetScreenFade(2);
                MainScreen.ScreenFade = 0;
                SequenceStage = IntroSequenceStage.CaptionFadeUp;
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

            _rumbleAmount = _rumbleAmount - 0.5;

            _screenEffects.MoveViewPort("GameArea", xShakeOffset, yShakeOffset);

            // Play rumble sound
            _rumbleSound = _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.Rumble, true);
        }

        _blobKiller.Update(gameTime);
    }

    private void UpdateCaptionFadeUp()
    {
        if (MainScreen.ScreenFade < ColourLevels.Full)
        {
            if (MainScreen.ScreenFade + 3 < ColourLevels.Full)
            {
                MainScreen.ScreenFade = MainScreen.ScreenFade + 3;
            }
            else
            {
                MainScreen.ScreenFade = ColourLevels.Full;
            }
        }
        else
        {
            SequenceStage = IntroSequenceStage.CaptionFadeDown;
        }
    }

    private void UpdateCaptionFadeDown()
    {
        if (MainScreen.ScreenFade > 0)
        {
            if (MainScreen.ScreenFade - 3 > 0)
            {
                MainScreen.ScreenFade = MainScreen.ScreenFade - 3;
            }
            else
            {
                MainScreen.ScreenFade = 0;
            }
        }
        else
        {
            SequenceStage = IntroSequenceStage.EndPause;
        }
    }

    private void UpdateEndPause()
    {
        if (SequenceStage == IntroSequenceStage.EndPause)
        {
            if (_endPauseFrameCounter < _endPauseFrameCount)
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
    }

    public void Draw()
    {
        // Draw spaceship
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
                    originalShip.DrawSprite(0, _blobSpaceShipEndXPos, _blobSpaceShipEndYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade));
                }
                else
                {
                    originalShip.DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                }

                originalShip.End();
            }
            else
            {
                SpriteCollection blobsShip = _resources.GetSpriteCollection(_graphicsSet.Names.BlobsShip);
                blobsShip.Begin(BlendState.AlphaBlend);
                blobsShip.DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                blobsShip.End();

                if (SequenceStage != IntroSequenceStage.Finished && SequenceStage != IntroSequenceStage.EndPause)
                {
                    SpriteCollection blobsShipHazardLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHazardLights);
                    blobsShipHazardLights.Begin(BlendState.AlphaBlend);
                    blobsShipHazardLights.DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, 0), 1.0f, _shipRotationSpeed, true, false);
                    blobsShipHazardLights.End();

                    SpriteCollection blobsShipHeadLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHeadLights);
                    blobsShipHeadLights.Begin(BlendState.AlphaBlend);
                    blobsShipHeadLights.DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                    blobsShipHeadLights.End();
                }
                else
                {
                    SpriteCollection blobsShipHazardLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHazardLights);
                    blobsShipHazardLights.Begin(BlendState.AlphaBlend);
                    blobsShipHazardLights.DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_blobShipHazardLightFlash.FadeLevel * ((double)MainScreen.ScreenFade / ColourLevels.Full))), 1.0f, _shipRotationSpeed, true, false);
                    blobsShipHazardLights.End();

                    SpriteCollection blobsShipHeadLights = _resources.GetSpriteCollection(Assets.Graphics.BlobsShipHeadLights);
                    blobsShipHeadLights.Begin(BlendState.AlphaBlend);
                    blobsShipHeadLights.DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_blobShipHeadLightFlash.FadeLevel * ((double)MainScreen.ScreenFade / ColourLevels.Full))), 1.0f, _shipRotationSpeed, true, false);
                    blobsShipHeadLights.End();
                }
            }

            if (SequenceStage == IntroSequenceStage.PreFadePause)
            {
                _blobKiller.Draw(MainScreen.ScreenFade);
            }
        }
        else
        {
            _font.Begin(BlendState.AlphaBlend);
            _font.WriteText(180, 210, "Prepare for mission Starquake ...", 2, Color.White, (byte)MainScreen.ScreenFade);
            _font.End();
        }
    }
}
