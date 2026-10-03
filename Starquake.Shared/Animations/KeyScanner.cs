// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
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

    /// <summary>
    ///     Runs a row of scanners through a code, one digit at a time: the first starts itself, and
    ///     each one that finishes starts the next. The slot each digit is read from comes off the
    ///     scan, so the status panel lights up the inventory slot being looked at.
    /// </summary>
    /// <remarks>
    ///     The access booths ask for three digits and the Cheops pyramid for two, and this was
    ///     written out twice - once per screen - differing in nothing but that count, which it now
    ///     takes from the array it is given. <see cref="Starquake.Structure.AccessCodeLock" /> is the
    ///     same split: the part that needs no graphics device went there, and this is the animation
    ///     the digits are shown through, which does.
    /// </remarks>
    public static void ScanCodeInTurn(KeyScanner[] scanners, AccessCodeScan scan, StatusPanel statusPanel)
    {
        if (!scanners[0].Scanned)
        {
            scanners[0].StartScan();
            statusPanel.ScanSlot(scan.SlotToScan[0]);
        }

        for (int digit = 0; digit < scanners.Length; digit++)
        {
            if (scanners[digit].Scanned &&
                digit < scanners.Length - 1 &&
                !scanners[digit + 1].Scanned)
            {
                scanners[digit + 1].StartScan();
                statusPanel.ScanSlot(scan.SlotToScan[digit + 1]);
            }

            scanners[digit].Update();
        }
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
                _scanLineFade -= 16;

                if (_scanLineFade < 0)
                {
                    _scanLineFade = 0;
                    _scanLineFadeDirection = Direction.Up;
                }
            }
            else if (_scanLineFadeDirection == Direction.Up)
            {
                _scanLineFade += 16;

                if (_scanLineFade > ColourLevels.Full)
                {
                    _scanLineFade = ColourLevels.Full;
                    _scanLineFadeDirection = Direction.Down;
                }
            }

            if (_scanLineY < 68)
            {
                _scanLineY += 4;
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
        pickups.DrawSprite(codeNumber, xPos + 2, yPos + 6, ColourHelper.WhiteFadedTo((byte)_keyCodeFade));
        pickups.End();

        var height = _scanLineY > 64 ? 64 : _scanLineY;

        // If scan started......
        if (ScanStarted)
        {
            if (_matched)
            {
                SpriteCollection accessCodesPassed = _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesPassed);
                accessCodesPassed.Begin(BlendState.AlphaBlend);
                accessCodesPassed.DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.WhiteFadedTo((byte)_keyCodeFade));
                accessCodesPassed.End();

            }
            else
            {
                SpriteCollection accessCodesFailed = _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesFailed);
                accessCodesFailed.Begin(BlendState.AlphaBlend);
                accessCodesFailed.DrawSpriteSubSection(codeNumber, xPos + 2, yPos + 6, new Rectangle(0, 0, 64, height), ColourHelper.WhiteFadedTo((byte)_keyCodeFade));
                accessCodesFailed.End();
            }

            // Draw the scanlines
            SpriteCollection scanLine = _resources.GetSpriteCollection(Assets.Graphics.ScanLine);
            scanLine.Begin(BlendState.Additive);
            scanLine.DrawSprite(0, xPos - 4, _scanLineY + yPos, ColourHelper.WhiteFadedTo((byte)_scanLineFade));
            scanLine.End();
        }
    }
}
