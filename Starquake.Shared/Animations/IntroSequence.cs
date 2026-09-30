// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.GameComponents.GameScreens;
using Starquake.Sounds;
using Starquake.Structure;

namespace Starquake.Animations;

public class IntroSequence : GameComponentBase
{
    private readonly GameEngine GameEngine;

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

    public IntroSequence(GameEngine gameEngine)
    {
        GameEngine = gameEngine;

        _blobShipHazardLightFlash = new FaderAnimation(10);
        _blobShipHeadLightFlash = new RandomFaderAnimation(4, GameEngine.RandomNumberGenerator, 100);
    }

    public int BlobSpaceShipXPos => (int)_blobSpaceShipXPos + GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).SpriteWidth / 2;

    public int BlobSpaceShipYPos => (int)_blobSpaceShipYPos + GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).SpriteHeight / 2;
    public IntroSequenceStage SequenceStage { get; private set; } = IntroSequenceStage.None;

    /// <summary>
    ///     The screen the sequence fades up over. Looked up rather than held, so the sequence cannot
    ///     be built before the screen it needs exists.
    /// </summary>
    private MainGameScreen MainScreen => (MainGameScreen)GameEngine.GetGameScreen(GameScreenType.MainGame);

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

    /// <summary>
    ///     The stages run in the order they are tested here. InitialPause is its own test rather than part
    ///     of the chain because it hands straight on to ShipFalling in the same frame.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        if (SequenceStage == IntroSequenceStage.InitialPause)
        {
            UpdateInitialPause();
        }

        if (SequenceStage == IntroSequenceStage.ShipFalling)
        {
            UpdateShipFalling(gameTime);
        }
        else if (SequenceStage == IntroSequenceStage.ShipExploading)
        {
            UpdateShipExploding(gameTime);
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
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobShipFall");
            }

            _startPauseCounter++;
        }
        else
        {
            SequenceStage = IntroSequenceStage.ShipFalling;
        }
    }

    private void UpdateShipFalling(GameTime gameTime)
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

    private void UpdateShipExploding(GameTime gameTime)
    {
        SequenceStage = IntroSequenceStage.PreFadePause;
        _shipExplosionSound = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobShipExplodes");
        GameEngine.Presentation.ScreenClearColour = Color.White;
    }

    private void UpdatePreFadePause(GameTime gameTime)
    {
        GameEngine.Presentation.ScreenClearColour = Color.Black;
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
            var xShakeOffset = GameEngine.RandomNumberGenerator.Next(0, (int)_rumbleAmount);
            var yShakeOffset = GameEngine.RandomNumberGenerator.Next(0, (int)_rumbleAmount);

            _rumbleAmount = _rumbleAmount - 0.5;

            GameEngine.ScreenEffectEngine.MoveViewPort("GameArea", xShakeOffset, yShakeOffset);

            // Play rumble sound
            _rumbleSound = GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("Rumble", true);
        }

        GameEngine.BlobKiller.Update(gameTime);
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
                GameEngine.ContentResourceManager.GetSoundFxEngine().StopSound(_rumbleSound, SoundFade.Fast);
                SequenceStage = IntroSequenceStage.Finished;
                SequenceRunning = false;
            }
        }

        _blobShipHazardLightFlash.Update();
        _blobShipHeadLightFlash.Update();
    }

    public override void Draw(GameTime gameTime)
    {
        // Draw spaceship
        if (SequenceStage != IntroSequenceStage.CaptionFadeUp && SequenceStage != IntroSequenceStage.CaptionFadeDown)
        {


            if (GameEngine.GraphicsSet.IsOriginal)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).Begin(BlendState.AlphaBlend, gameTime);
                if (SequenceStage == IntroSequenceStage.Finished || SequenceStage == IntroSequenceStage.CaptionFadeUp || SequenceStage == IntroSequenceStage.EndPause)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).DrawSprite(0, 640, 288, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade));
                }
                else
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                }

                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).End();
            }
            else
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.BlobsShip).End();

                if (SequenceStage != IntroSequenceStage.Finished && SequenceStage != IntroSequenceStage.EndPause)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, 0), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").End();

                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MainScreen.ScreenFade), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").End();
                }
                else
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_blobShipHazardLightFlash.FadeLevel * ((double)MainScreen.ScreenFade / ColourLevels.Full))), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHazardLights").End();

                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").Begin(BlendState.AlphaBlend, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").DrawSprite(0, _blobSpaceShipXPos, _blobSpaceShipYPos, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_blobShipHeadLightFlash.FadeLevel * ((double)MainScreen.ScreenFade / ColourLevels.Full))), 1.0f, _shipRotationSpeed, true, false);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobsShipHeadLights").End();
                }
            }

            if (SequenceStage == IntroSequenceStage.PreFadePause)
            {
                GameEngine.BlobKiller.Draw(gameTime, MainScreen.ScreenFade);
            }
        }
        else
        {
            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteText(180, 210, "Prepare for mission Starquake ...", 2, Color.White, (byte)MainScreen.ScreenFade);
            GameEngine.Font2.End();
        }
    }
}