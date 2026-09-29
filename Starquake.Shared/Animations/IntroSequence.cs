// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Engines;
using StarquakeXNA.GameComponents.GameScreens;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Animations;

public class IntroSequence : GameComponentBase
{

    private readonly FaderAnimation _blobShipHazardLightFlash;
    private readonly RandomFaderAnimation _blobShipHeadLightFlash;
    private float _acceleration;

    private int _blobSpaceShipEndXPos;
    private int _blobSpaceShipEndYPos;

    private int _blobSpaceShipStartXPos;
    private int _blobSpaceShipStartYPos;

    private float _blobSpaceShipXPos, _blobSpaceShipYPos;

    private int _endPauseFrameCount;
    private int _endPauseFrameCounter;

    private MainGameScreen _mainScreen;

    private int _preFadeOutPause = 50;
    private int _preFadeOutPauseCounter;

    private double _rumbleAmount = 100;
    private Guid _rumbleSound = Guid.Empty;
    private int _sequenceCounter;

    private int _sequenceFrameCount;
    public bool SequenceRunning;

    private Guid _shipExplosionSound = Guid.Empty;

    private float _shipRotationSpeed;
    private int _startPauseCounter;

    private int _startPauseFrameCount;
    private float _xAmountToMoveEachFrame;

    private float _yAmountToMoveEachFrame;

    public IntroSequence(GameEngine gameEngine) : base(gameEngine)
    {
        _blobShipHazardLightFlash = new FaderAnimation(10);
        _blobShipHeadLightFlash = new RandomFaderAnimation(4, GameEngine.RandomNumberGenerator, 100);
    }

    public int BlobSpaceShipXPos => (int)_blobSpaceShipXPos + GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).SpriteWidth / 2;

    public int BlobSpaceShipYPos => (int)_blobSpaceShipYPos + GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).SpriteHeight / 2;
    public IntroSequenceStage SequenceStage { get; private set; } = IntroSequenceStage.None;

    public void Reset(MainGameScreen mainScreen)
    {
        if (_mainScreen == null)
        {
            _mainScreen = mainScreen;
        }

        _mainScreen.ScreenFade = 255;

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

        _blobSpaceShipStartXPos = 200;
        _blobSpaceShipStartYPos = -160;

        _blobSpaceShipEndXPos = 610;
        _blobSpaceShipEndYPos = 280;

        _xAmountToMoveEachFrame = (_blobSpaceShipEndXPos - (float)_blobSpaceShipStartXPos) / _sequenceFrameCount;
        _yAmountToMoveEachFrame = (_blobSpaceShipEndYPos - (float)_blobSpaceShipStartYPos) / _sequenceFrameCount;

        _blobSpaceShipXPos = _blobSpaceShipStartXPos;
        _blobSpaceShipYPos = _blobSpaceShipStartYPos;
        _acceleration = 0;

        _sequenceCounter = 0;

        GameEngine.ContentResourceManager.GetSoundFxEngine().StopSound(_shipExplosionSound, SoundFade.None);

        GameEngine.GameScreens.BlobsShipSmoke1.Reset();
        GameEngine.GameScreens.BlobsShipSmoke2.Reset();
    }

    public override void Update(GameTime gameTime)
    {
        if (SequenceStage == IntroSequenceStage.InitialPause)
        {
            if (_startPauseCounter < _startPauseFrameCount)
            {
                if (_startPauseCounter == 0)
                {
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobShipFall");
                }

                _startPauseCounter++;
            }
            else
            {
                SequenceStage = IntroSequenceStage.ShipFalling;
            }
        }
        if (SequenceStage == IntroSequenceStage.ShipFalling)
        {
            //if (sequenceCounter < sequenceFrameCount)
            if (_blobSpaceShipYPos < _blobSpaceShipEndYPos)
            {
                _blobSpaceShipXPos = _blobSpaceShipXPos + _xAmountToMoveEachFrame;
                _blobSpaceShipYPos = _blobSpaceShipYPos + _yAmountToMoveEachFrame * _acceleration;

                _sequenceCounter++;

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
                GameEngine.BlobKiller.Trigger(_blobSpaceShipEndXPos, _blobSpaceShipEndYPos, false, 100, 200);
            }
        }
        else if (SequenceStage == IntroSequenceStage.ShipExploading)
        {
            SequenceStage = IntroSequenceStage.PreFadePause;
            _shipExplosionSound = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobShipExplodes");
            GameEngine.ScreenClearColour = Color.White;
        }
        else if (SequenceStage == IntroSequenceStage.PreFadePause)
        {
            GameEngine.ScreenClearColour = Color.Black;
            if (_shipRotationSpeed > 0)
            {
                _shipRotationSpeed = _shipRotationSpeed - 2;
            }

            if (_preFadeOutPauseCounter >= _preFadeOutPause)
            {
                if (_mainScreen.ScreenFade > 0)
                {
                    if (_mainScreen.ScreenFade - 2 < 0)
                    {
                        _mainScreen.ScreenFade = 0;
                    }
                    else
                    {
                        _mainScreen.ScreenFade = _mainScreen.ScreenFade - 2;
                    }
                }
                else
                {
                    _mainScreen.ResetScreenFade(2);
                    _mainScreen.ScreenFade = 0;
                    SequenceStage = IntroSequenceStage.CaptionFadeUp;
                }
            }
            else
            {
                _preFadeOutPauseCounter++;
            }

            if (_rumbleAmount > 0)
            {
                var xShakeOffset = GameEngine.RandomNumberGenerator.Next(0, (int)_rumbleAmount);
                var yShakeOffset = GameEngine.RandomNumberGenerator.Next(0, (int)_rumbleAmount);

                _rumbleAmount = _rumbleAmount - 0.5;

                GameEngine.ScreenEffctEngine.MoveViewPort("GameArea", xShakeOffset, yShakeOffset);

                // Play rumble sound
                _rumbleSound = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("Rumble", true);
            }

            GameEngine.BlobKiller.Update(gameTime);
        }
        else if (SequenceStage == IntroSequenceStage.CaptionFadeUp)
        {
            if (_mainScreen.ScreenFade < 255)
            {
                if (_mainScreen.ScreenFade + 3 < 255)
                {
                    _mainScreen.ScreenFade = _mainScreen.ScreenFade + 3;
                }
                else
                {
                    _mainScreen.ScreenFade = 255;
                }
            }
            else
            {
                SequenceStage = IntroSequenceStage.CaptionFadeDown;
            }
        }
        else if (SequenceStage == IntroSequenceStage.CaptionFadeDown)
        {
            if (_mainScreen.ScreenFade > 0)
            {
                if (_mainScreen.ScreenFade - 3 > 0)
                {
                    _mainScreen.ScreenFade = _mainScreen.ScreenFade - 3;
                }
                else
                {
                    _mainScreen.ScreenFade = 0;
                }
            }
            else
            {
                SequenceStage = IntroSequenceStage.EndPause;
            }
        }
        else if (SequenceStage == IntroSequenceStage.EndPause || SequenceStage == IntroSequenceStage.Finished)
        {
            if (SequenceStage == IntroSequenceStage.EndPause)
            {
                if (_endPauseFrameCounter < _endPauseFrameCount)
                {
                    _endPauseFrameCounter++;
                }
                else
                {
                    GameEngine.ContentResourceManager.GetSoundFxEngine().StopSound(_rumbleSound, SoundFade.Fast);
                    SequenceStage = IntroSequenceStage.Finished;
                    SequenceRunning = false;
                }
            }

            _blobShipHazardLightFlash.Update();
            _blobShipHeadLightFlash.Update();
        }

    }

    public override void Draw(GameTime gameTime)
    {
        // Draw spaceship
        if (SequenceStage != IntroSequenceStage.CaptionFadeUp && SequenceStage != IntroSequenceStage.CaptionFadeDown)
        {


            if (GameEngine.UsingOldGraphics)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).Begin(BlendState.AlphaBlend, gameTime);
                if (SequenceStage == IntroSequenceStage.Finished || SequenceStage == IntroSequenceStage.CaptionFadeUp || SequenceStage == IntroSequenceStage.EndPause)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).DrawSprite(0, 640, 288, new Color(255, 255, 255, _mainScreen.ScreenFade));
                }
                else
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(255, 255, 255, _mainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                }

                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).End();
            }
            else
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(255, 255, 255, _mainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobsShipName).End();

                if (SequenceStage != IntroSequenceStage.Finished && SequenceStage != IntroSequenceStage.EndPause)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(255, 255, 255, 0), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").End();

                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(255, 255, 255, _mainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").End();
                }
                else
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(255, 255, 255, (int)(_blobShipHazardLightFlash.FadeLevel * ((double)_mainScreen.ScreenFade / 255))), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").End();

                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(255, 255, 255, (int)(_blobShipHeadLightFlash.FadeLevel * ((double)_mainScreen.ScreenFade / 255))), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").End();
                }
            }

            if (SequenceStage == IntroSequenceStage.PreFadePause)
            {
                GameEngine.BlobKiller.Draw(gameTime, _mainScreen.ScreenFade);
            }
        }
        else
        {
            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteText(180, 210, "Prepare for mission Starquake ...", 2, Color.White, (byte)_mainScreen.ScreenFade);
            GameEngine.Font2.End();
        }
    }
}