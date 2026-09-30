// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Structure;

namespace Starquake.Animations;

public class KeyScanner
{

    private readonly bool _flicker;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private int _keyCodeFade;
    private bool _matched;
    private int _scanLineFade;

    private Direction _scanLineFadeDirection;
    private int _scanLineY;

    public KeyScanner(ResourceManager resources, GraphicsSet graphicsSet, Random randomNumberGenerator, bool flicker)
    {
        _flicker = flicker;
        _resources = resources;
        _graphicsSet = graphicsSet;
        _randomNumberGenerator = randomNumberGenerator;
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
            _resources.GetSoundFxEngine().PlaySound("ScanCode", 0.3f, false);
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
            _keyCodeFade = _randomNumberGenerator.Next(100, 256);
        }
        else
        {
            _keyCodeFade = ColourLevels.Full;
        }
    }

    public void Draw(int codeNumber, int xPos, int yPos, GameTime gameTime)
    {
        // Draw the code keys
        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).Begin(BlendState.AlphaBlend, gameTime);
        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite(codeNumber, xPos + 2, yPos + 6, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();

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
                _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesPassed).Begin(BlendState.AlphaBlend, gameTime);
                _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesPassed).DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
                _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesPassed).End();

            }
            else
            {
                _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesFailed).Begin(BlendState.AlphaBlend, gameTime);
                _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesFailed).DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
                _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesFailed).End();
            }

            // Draw the scanlines
            _resources.GetSpriteCollection("ScanLine").Begin(BlendState.Additive, gameTime);
            _resources.GetSpriteCollection("ScanLine").DrawSprite(0, xPos - 4, _scanLineY + yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_scanLineFade));
            _resources.GetSpriteCollection("ScanLine").End();
        }
    }
}