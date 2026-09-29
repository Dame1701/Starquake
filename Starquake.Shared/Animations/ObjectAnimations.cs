// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Animations;

public class KeyScanner
{

    private readonly bool _flicker;
    private readonly GameEngine _gameEngine;
    private int _keyCodeFade;
    private bool _matched;
    private int _scanLineFade;

    private Direction _scanLineFadeDirection;
    private int _scanLineY;

    public KeyScanner(GameEngine gameEngineReference, bool flicker)
    {
        _flicker = flicker;
        _gameEngine = gameEngineReference;
        Reset(true);
    }
    public bool ScanStarted { get; private set; }

    public bool Scanned { get; private set; }

    public void Reset(bool match)
    {
        _scanLineFade = 255;
        _scanLineY = 0;
        _scanLineFadeDirection = Direction.Down;
        _matched = match;
        Scanned = false;
        ScanStarted = false;
    }

    public void StartScan()
    {
        if (!ScanStarted)
        {
            ScanStarted = true;
            _gameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("ScanCode", 0.3f, false);
        }
    }

    public void Update()
    {
        if (ScanStarted)
        {
            if (_scanLineFadeDirection == Direction.Down)
            {
                _scanLineFade = _scanLineFade - 16;

                if (_scanLineFade < 0)
                {
                    _scanLineFade = 0;
                    _scanLineFadeDirection = Direction.Up;
                }
            }
            else if (_scanLineFadeDirection == Direction.Up)
            {
                _scanLineFade = _scanLineFade + 16;

                if (_scanLineFade > 255)
                {
                    _scanLineFade = 255;
                    _scanLineFadeDirection = Direction.Down;
                }
            }

            if (_scanLineY < 68)
            {
                _scanLineY = _scanLineY + 4;
            }
            else
            {
                Scanned = true;
            }

        }

        if (_flicker)
        {
            _keyCodeFade = _gameEngine.RandomNumberGenerator.Next(100, 256);
        }
        else
        {
            _keyCodeFade = 255;
        }
    }

    public void Draw(int codeNumber, int xPos, int yPos, GameTime gameTime)
    {
        // Draw the code keys
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(codeNumber, xPos + 2, yPos + 6, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_keyCodeFade));
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();

        var height = 0;
        if (_scanLineY > 64)
        {
            height = 64;
        }
        else
        {
            height = _scanLineY;
        }

        // If scan started......
        if (ScanStarted)
        {
            if (_matched)
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).Begin(BlendState.AlphaBlend, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(255, 255, 255, (byte)_keyCodeFade));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).End();

            }
            else
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).Begin(BlendState.AlphaBlend, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(255, 255, 255, (byte)_keyCodeFade));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).End();
            }

            // Draw the scanlines
            _gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").Begin(BlendState.Additive, gameTime);
            _gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").DrawSprite(0, xPos - 4, _scanLineY + yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_scanLineFade));
            _gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").End();
        }
    }
}

public class BlobAccessBoothAnim
{
    private readonly Animation _animLeft;
    private readonly Animation _animRight;
    private readonly Animation _animTurn;
    private readonly int _endPoint;
    private readonly GameEngine _gameEngine;
    private readonly int _startPoint;

    private Direction _blobDirection;
    private int _blobFrame;
    private int _blobX;

    public BlobAccessBoothAnim(GameEngine gameEngineReference, int startPoint, int endPoint)
    {
        _gameEngine = gameEngineReference;
        _startPoint = startPoint;
        _endPoint = endPoint;

        _animLeft = new Animation();
        _animLeft.FirstFrame = 0;
        _animLeft.CurrentFrame = 0;
        _animLeft.LastFrame = 3;
        _animLeft.Yoyo = false;
        _animLeft.Delay = 2;
        _animLeft.Repeat = true;
        _animLeft.AnimateForwards = false;

        _animRight = new Animation();
        _animRight.FirstFrame = 7;
        _animRight.CurrentFrame = 7;
        _animRight.LastFrame = 10;
        _animRight.Yoyo = false;
        _animRight.Delay = 2;
        _animRight.Repeat = true;
        _animRight.AnimateForwards = true;

        _animTurn = new Animation();
        _animTurn.FirstFrame = 4;
        _animTurn.CurrentFrame = 4;
        _animTurn.LastFrame = 10;
        _animTurn.Yoyo = false;
        _animTurn.Delay = 2;
        _animTurn.Repeat = false;
        _animTurn.AnimateForwards = false;
        _animTurn.Finished = false;
    }

    public void Reset(Direction startingDirection)
    {
        if (startingDirection == Direction.Right)
        {
            //blobX = 164;
            _blobX = _startPoint;
            _blobFrame = 10;
        }
        else if (startingDirection == Direction.Left)
        {
            _blobX = _endPoint;
            _blobFrame = 0;
        }

        _blobDirection = startingDirection;
    }

    public void Update()
    {
        if (_blobDirection == Direction.Right)
        {
            _blobX = _blobX + 8;

            if (!_animTurn.Finished)
            {
                _animTurn.GetNextFrame();
            }
            else
            {
                _animRight.GetNextFrame();
            }

            if (_animTurn.Finished)
            {
                _blobFrame = _animRight.CurrentFrame;
            }
            else
            {
                _blobFrame = _animTurn.CurrentFrame;
            }
        }
        else if (_blobDirection == Direction.Left)
        {
            _blobX = _blobX - 8;

            if (!_animTurn.Finished)
            {
                _animTurn.GetNextFrame();
            }
            else
            {
                _animLeft.GetNextFrame();
            }

            if (_animTurn.Finished)
            {
                _blobFrame = _animLeft.CurrentFrame;
            }
            else
            {
                _blobFrame = _animTurn.CurrentFrame;
            }
        }

        if (_blobX > _endPoint)
        {
            _blobDirection = Direction.Left;

            _animTurn.FirstFrame = 0;
            _animTurn.CurrentFrame = 6;
            _animTurn.LastFrame = 6;
            _animTurn.AnimateForwards = false;
            _animTurn.Finished = false;
        }
        else if (_blobX < _startPoint)
        {
            _blobDirection = Direction.Right;

            _animTurn.FirstFrame = 4;
            _animTurn.CurrentFrame = 4;
            _animTurn.LastFrame = 10;
            _animTurn.AnimateForwards = true;
            _animTurn.Finished = false;
        }
    }

    public void Draw(GameTime gameTime)
    {
        // Draw blob animation
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.BlobSpritesName).Begin(BlendState.AlphaBlend, gameTime);
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.BlobSpritesName).DrawSprite(_blobFrame, _blobX, 192);
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.BlobSpritesName).End();
    }
}