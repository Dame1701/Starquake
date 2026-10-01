// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Helpers;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Animations;

internal class KeyScanner
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
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.ScanCode, 0.3f, false);
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

    public void Draw(int codeNumber, int xPos, int yPos)
    {
        // Draw the code keys
        SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
        pickups.Begin(BlendState.AlphaBlend);
        pickups.DrawSprite(codeNumber, xPos + 2, yPos + 6, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
        pickups.End();

        var height = _scanLineY > 64 ? 64 : _scanLineY;

        // If scan started......
        if (ScanStarted)
        {
            if (_matched)
            {
                SpriteCollection accessCodesPassed = _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesPassed);
                accessCodesPassed.Begin(BlendState.AlphaBlend);
                accessCodesPassed.DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
                accessCodesPassed.End();

            }
            else
            {
                SpriteCollection accessCodesFailed = _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesFailed);
                accessCodesFailed.Begin(BlendState.AlphaBlend);
                accessCodesFailed.DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_keyCodeFade));
                accessCodesFailed.End();
            }

            // Draw the scanlines
            SpriteCollection scanLine = _resources.GetSpriteCollection(Assets.Graphics.ScanLine);
            scanLine.Begin(BlendState.Additive);
            scanLine.DrawSprite(0, xPos - 4, _scanLineY + yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_scanLineFade));
            scanLine.End();
        }
    }
}
