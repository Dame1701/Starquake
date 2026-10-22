// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Sprites;

namespace Starquake.Display;

internal class KeyScanner
{
    /// <summary>
    ///     Where the digit sits inside its slot, so it is centred on the artwork behind it rather
    ///     than against the corner.
    /// </summary>
    private const int KeyXInset = 2;

    /// <inheritdoc cref="KeyXInset" />
    private const int KeyYInset = 6;

    /// <summary>
    ///     How far the scan line overhangs the digit each side, so it reads as sweeping across
    ///     rather than being contained by it.
    /// </summary>
    private const int ScanLineXOutset = 4;
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
    ///     Runs a row of scanners through a code, one digit at a time: the first starts itself and
    ///     each one that finishes starts the next. The slot each digit is read from comes off the
    ///     scan, so the status panel lights up the inventory slot being looked at.
    /// </summary>
    /// <remarks>
    ///     How many digits comes from the array passed in: three for an access booth, two for the
    ///     Cheops pyramid. <see cref="AccessCodeLock" /> holds the part that needs no graphics
    ///     device; this is the animation the digits are shown through, which does.
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
        DrawCodeKey(codeNumber, xPos, yPos);

        if (!ScanStarted)
        {
            return;
        }

        DrawScanVerdict(codeNumber, xPos, yPos);
        DrawScanLine(xPos, yPos);
    }

    /// <summary>The digit the door is asking for, drawn as the pick-up that answers it.</summary>
    private void DrawCodeKey(int codeNumber, int xPos, int yPos)
    {
        SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);

        pickups.Begin(BlendState.AlphaBlend);
        pickups.DrawSprite(codeNumber, xPos + KeyXInset, yPos + KeyYInset,
            Colours.WhiteFadedTo((byte)_keyCodeFade));
        pickups.End();
    }

    /// <summary>
    ///     The digit again, in the colour that says whether Blob is carrying it, revealed down to
    ///     wherever the scan line has reached, so the verdict is painted on as the line passes.
    /// </summary>
    private void DrawScanVerdict(int codeNumber, int xPos, int yPos)
    {
        string sheetName = _matched
            ? _graphicsSet.Names.AccessCodesPassed
            : _graphicsSet.Names.AccessCodesFailed;

        int revealed = Math.Min(_scanLineY, SpriteMetrics.SpriteHeight);

        SpriteCollection verdict = _resources.GetSpriteCollection(sheetName);

        verdict.Begin(BlendState.AlphaBlend);
        verdict.DrawSpriteSubSection(codeNumber, xPos + KeyXInset, yPos + KeyYInset,
            new Rectangle(0, 0, SpriteMetrics.SpriteWidth, revealed),
            Colours.WhiteFadedTo((byte)_keyCodeFade));
        verdict.End();
    }

    /// <summary>The line itself, running down the digit.</summary>
    private void DrawScanLine(int xPos, int yPos)
    {
        SpriteCollection scanLine = _resources.GetSpriteCollection(Assets.Graphics.ScanLine);

        scanLine.Begin(BlendState.Additive);
        scanLine.DrawSprite(0, xPos - ScanLineXOutset, _scanLineY + yPos,
            Colours.WhiteFadedTo((byte)_scanLineFade));
        scanLine.End();
    }
}
