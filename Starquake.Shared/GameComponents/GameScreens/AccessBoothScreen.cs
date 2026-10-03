// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

internal class AccessBoothScreen : IGameScreen
{
    /// <summary>
    ///     How long the result is left on screen before the game moves on, in logic frames - one
    ///     second. It was a DateTime compared against DateTime.Now, which is local time: a DST step
    ///     or an NTP correction landing in that second turned the pause into an hour.
    /// </summary>
    private const int ScreenReadWaitFrames = LogicClock.FramesPerSecond;

    /// <summary>
    ///     What <see cref="_screenReadWaitFrames" /> holds before the pause has started.
    /// </summary>
    private const int ScreenReadNotWaiting = -1;

    /// <summary>
    ///     How many digits a booth door asks for, and so how many scanners it shows. The world
    ///     generator draws booth codes from 100 to 999, which is the same three.
    /// </summary>
    private const int DigitsInCode = 3;

    private readonly KeyScanner[] _accessCodes;
    private readonly Blob _blob;

    private readonly BlobAccessBoothAnim _blobAnim1;
    private readonly BlobAccessBoothAnim _blobAnim2;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;
    private readonly StatusPanel _statusPanel;
    private int[] _accessCode = [];

    private int _resultMessageFade;

    /// <summary>
    ///     Which slot each scanner reads, and whether the booth is satisfied. Replaced whole by
    ///     <see cref="CheckInventoryAgainstCode" /> rather than written into a digit at a time.
    /// </summary>
    private AccessCodeScan _scan = new([], false);
    private int _screenReadWaitFrames = ScreenReadNotWaiting;
    private bool _soundPlayed;

    public AccessBoothScreen(
        StatusPanel statusPanel,
        ResourceManager resources,
        BitmapFont font,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        Random randomNumberGenerator,
        ScreenDirectory screenDirectory,
        Shapes shapeDrawer)
    {
        _statusPanel = statusPanel;
        _resources = resources;
        _font = font;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;
        _screenDirectory = screenDirectory;
        _shapeDrawer = shapeDrawer;

        _accessCodes = new KeyScanner[DigitsInCode];

        for (int a = 0; a < _accessCodes.Length; a++)
        {
            _accessCodes[a] = new KeyScanner(_resources, _graphicsSet, _randomNumberGenerator, true);
        }

        _blobAnim1 = new BlobAccessBoothAnim(_resources, _graphicsSet, 164, 800);
        _blobAnim2 = new BlobAccessBoothAnim(_resources, _graphicsSet, 164, 800);

        Reset();
    }

    /// <summary>
    ///     Whether the code the player typed matched. It used to be on IGameScreen, where eight of the
    ///     nine screens answered a constant true and nothing read it through the interface; this is the
    ///     one screen with a real answer, and it passes it on explicitly as the post-change parameter
    ///     rather than leaving anyone to read it off the property.
    /// </summary>
    private bool Success => _scan.Accepted;

    public void Update(GameTime gameTime)
    {
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.MachineryBackground, 0.5f, true);

        _blobAnim1.Update();
        _blobAnim2.Update();

        KeyScanner.ScanCodeInTurn(_accessCodes, _scan, _statusPanel);

        _resultMessageFade = _randomNumberGenerator.Next(200, 256);

        if (_screenReadWaitFrames >= 0)
        {
            _screenReadWaitFrames++;

            if (_screenReadWaitFrames > ScreenReadWaitFrames)
            {
                _screenDirectory.MainGame.ScreenFade = 40;
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.AccessBooth, new ScreenChange.AccessCode(Success));

                _screenDirectory.Current = GameScreenType.MainGame;
            }
        }

        AnnounceScanResult();
    }

    public void Draw()
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(350, 100, "SECURITY  DOOR", 2, Color.White, ColourLevels.Full);
        _font.WriteText(370, 300, "ACCESS CODE", 2, Color.White, ColourLevels.Full);
        _font.End();

        DrawBoothDoors();

        _blobAnim1.Draw();
        _blobAnim2.Draw();

        DrawDoorHighlights();

        for (int a = 0; a < DigitsInCode; a++)
        {
            _accessCodes[a].Draw(_accessCode[a] + AccessCodeLock.FirstDigitPickup, 348 + a * 128, 364);
        }

        if (_accessCodes[2].Scanned)
        {
            DrawScanResult();
        }
    }

    /// <summary>
    ///     The booth's verdict, once all three scanners have read: the sound, and the start of the
    ///     wait that sends Blob back to the game. Done last so the wait is a whole
    ///     <see cref="ScreenReadWaitFrames" /> long rather than one frame short of it.
    /// </summary>
    /// <remarks>
    ///     This was <see cref="DrawScanResult" />'s job, which meant the sound was asked for once per
    ///     drawn frame and needed a latch to stop it playing sixty times a second. Both belong on the
    ///     logic clock: the wait is counted in logic frames, so starting it from a draw tied the
    ///     length of the pause to the display's refresh rate.
    /// </remarks>
    private void AnnounceScanResult()
    {
        if (!_accessCodes[2].Scanned || _soundPlayed)
        {
            return;
        }

        _resources.GetSoundFxEngine()
            .PlaySound(Success ? Assets.Sounds.AccessSuccess : Assets.Sounds.AccessFailed, 1f, false);

        _soundPlayed = true;
        _screenReadWaitFrames = 0;
    }

    private void DrawBoothDoors()
    {
        SpriteCollection lowerScreenTiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);
        lowerScreenTiles.Begin(BlendState.AlphaBlend);
        lowerScreenTiles.DrawSprite(399, 380, 160);
        lowerScreenTiles.DrawSprite(400, 508, 160);

        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).End();
    }

    private void DrawDoorHighlights()
    {
        SpriteCollection upperScreenTiles = _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles);
        upperScreenTiles.Begin(BlendState.AlphaBlend);
        upperScreenTiles.DrawSprite(9, 380, 160);
        upperScreenTiles.DrawSprite(10, 508, 160);
        upperScreenTiles.End();
    }

    /// <summary>
    ///     What the booth says once all three scanners have read. The sound and the wait that sends
    ///     Blob back to the game are <see cref="AnnounceScanResult" />'s, so this only draws.
    /// </summary>
    private void DrawScanResult()
    {
        if (Success)
        {
            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(167, 498, 40, 40), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.FillRectangle(new Rectangle(215, 500, 36, 36), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.FillRectangle(new Rectangle(259, 504, 28, 28), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.End();

            _font.Begin(BlendState.AlphaBlend);
            _font.WriteText(310, 500, "ACCESS AUTHORISED", 2, new Color(0, ColourLevels.Full, 0), (byte)_resultMessageFade);
            _font.End();

            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(756, 504, 28, 28), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.FillRectangle(new Rectangle(792, 500, 36, 36), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.FillRectangle(new Rectangle(836, 498, 40, 40), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.End();
        }
        else
        {
            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(157, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.FillRectangle(new Rectangle(205, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.FillRectangle(new Rectangle(249, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.End();

            _font.Begin(BlendState.AlphaBlend);
            _font.WriteText(300, 500, "ACCESS CODE INVALID", 2, Color.Red, (byte)_resultMessageFade);
            _font.End();

            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(746, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.FillRectangle(new Rectangle(782, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.FillRectangle(new Rectangle(826, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.End();
        }
    }

    /// <summary>
    ///     Reads the booth's code and checks Blob's inventory against it, leaving Blob
    ///     where it is. This is the form the constructor uses, before there is a door to arrive
    ///     through.
    /// </summary>
    private void Reset()
    {
        _soundPlayed = false;
        _blobAnim1.Reset(Direction.Left);
        _blobAnim2.Reset(Direction.Right);

        ReadAccessCode();

        _screenReadWaitFrames = ScreenReadNotWaiting;
        _resultMessageFade = ColourLevels.Full;

        CheckInventoryAgainstCode();
    }

    /// <inheritdoc cref="Reset()" />
    /// <param name="approachedFrom">Which side of the booth door Blob walked in from.</param>
    /// <param name="doorMapXPosition">The map column the booth door stands in.</param>
    public void Reset(Direction approachedFrom, int doorMapXPosition)
    {
        Reset();

        MoveBlobToTheBoothDoor(approachedFrom, doorMapXPosition);
    }

    /// <summary>
    ///     The code is stored on the screen as a string of digits, one per scanner.
    /// </summary>
    private void ReadAccessCode()
        => _accessCode = AccessCodeLock.Digits(_gameScreens.CurrentGameScreenData.AccessCode, DigitsInCode);

    /// <summary>
    ///     Scan Blob's inventory to see if he is carrying the right stuff.
    /// </summary>
    private void CheckInventoryAgainstCode()
    {
        _scan = AccessCodeLock.Scan(_blob.BlobInventory, _accessCode);

        for (int digit = 0; digit < _accessCodes.Length; digit++)
        {
            _accessCodes[digit].Reset(_scan.SlotToScan[digit] != AccessCodeLock.NoSlot);
        }
    }

    /// <summary>
    ///     Puts Blob on the far side of the door he walked into, which is why the caller passes which side
    ///     he came from.
    ///     <para>
    ///         The two arguments used to arrive as an <c>object[]</c> unboxed by index - a
    ///         <see cref="Direction" /> at 0 and an <c>int</c> at 1 - because
    ///         <c>IGameScreen.Reset</c> took a single <c>object?</c>. Nothing checked either cast,
    ///         and a null meant "don't move him" rather than "no data".
    ///     </para>
    /// </summary>
    private void MoveBlobToTheBoothDoor(Direction approachedFrom, int doorMapXPosition)
    {
        switch (approachedFrom)
        {
            case Direction.Left:
                _blob.BlobXPos = Success
                    ? (doorMapXPosition - 1) * 128 + 16
                    : doorMapXPosition * 128 + 48;
                _blob.BlobFrame = 0;
                break;
            case Direction.Right:
                _blob.BlobXPos = Success
                    ? (doorMapXPosition + 1) * 128 + 48
                    : doorMapXPosition * 128 + 16;
                _blob.BlobFrame = 10;
                break;
        }
    }
}
