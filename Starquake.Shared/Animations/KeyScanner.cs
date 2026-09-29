// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Structure;

namespace Starquake.Animations;

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
        _scanLineFade = ColourLevels.Full;
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

                if (_scanLineFade > ColourLevels.Full)
                {
                    _scanLineFade = ColourLevels.Full;
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
            _keyCodeFade = ColourLevels.Full;
        }
    }

    public void Draw(int codeNumber, int xPos, int yPos, GameTime gameTime)
    {
        // Draw the code keys
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);
        _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(codeNumber, xPos + 2, yPos + 6, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
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
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).End();

            }
            else
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).Begin(BlendState.AlphaBlend, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).End();
            }

            // Draw the scanlines
            _gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").Begin(BlendState.Additive, gameTime);
            _gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").DrawSprite(0, xPos - 4, _scanLineY + yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_scanLineFade));
            _gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").End();
        }
    }
}