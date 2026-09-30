// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class AccessBoothScreen : GameComponentBase, IGameScreen
{


    private readonly int[] _accessCode;

    private readonly KeyScanner[] _accessCodes;
    private readonly Blob _blob;

    private readonly BlobAccessBoothAnim _blobAnim1;
    private readonly BlobAccessBoothAnim _blobAnim2;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly int[] _inventorySlotToScan;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;
    private readonly StatusPanel _statusPanel;

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

    private int _resultMessageFade;
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

        _accessCode = new int[3]
        {
            -1,
            -1,
            -1
        };
        _accessCodes = new KeyScanner[3];
        _inventorySlotToScan = new int[3];

        for (int a = 0; a < _accessCodes.Length; a++)
        {
            _accessCodes[a] = new KeyScanner(_resources, _graphicsSet, _randomNumberGenerator, true);
        }

        _blobAnim1 = new BlobAccessBoothAnim(_resources, _graphicsSet, 164, 800);
        _blobAnim2 = new BlobAccessBoothAnim(_resources, _graphicsSet, 164, 800);

        Reset(null);
    }

    public int ScreenFade { get; set; }

    /// <summary>
    ///     Whether the code the player typed matched. It used to be on IGameScreen, where eight of the
    ///     nine screens answered a constant true and nothing read it through the interface; this is the
    ///     one screen with a real answer, and it passes it on explicitly as the post-change parameter
    ///     rather than leaving anyone to read it off the property.
    /// </summary>
    private bool Success { get; set; }

    public override void Update(GameTime gameTime)
    {
        _resources.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        _blobAnim1.Update();
        _blobAnim2.Update();

        // Start first scan off....
        if (!_accessCodes[0].Scanned)
        {
            _accessCodes[0].StartScan();
            _statusPanel.ScanSlot(_inventorySlotToScan[0]);
        }

        // Follow with the subsequent ones...
        for (int a = 0; a < _accessCodes.Length; a++)
        {
            if (_accessCodes[a].Scanned)
            {
                if (a < 2 && !_accessCodes[a + 1].Scanned)
                {
                    _accessCodes[a + 1].StartScan();
                    _statusPanel.ScanSlot(_inventorySlotToScan[a + 1]);
                }
            }
            _accessCodes[a].Update();
        }

        _resultMessageFade = _randomNumberGenerator.Next(200, 256);

        if (_screenReadWaitFrames >= 0)
        {
            _screenReadWaitFrames++;

            if (_screenReadWaitFrames > ScreenReadWaitFrames)
            {
                _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
                _screenDirectory.Get(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.AccessBooth, Success);

                _screenDirectory.Current = GameScreenType.MainGame;
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(350, 100, "SECURITY  DOOR", 2, Color.White, ColourLevels.Full);
        _font.WriteText(370, 300, "ACCESS CODE", 2, Color.White, ColourLevels.Full);
        _font.End();

        DrawBoothDoors();

        _blobAnim1.Draw();
        _blobAnim2.Draw();

        DrawDoorHighlights();

        for (int a = 0; a < 3; a++)
        {
            _accessCodes[a].Draw(_accessCode[a] + 10, 348 + a * 128, 364);
        }

        if (_accessCodes[2].Scanned)
        {
            DrawScanResult();
        }
    }

    private void DrawBoothDoors()
    {
        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).Begin(BlendState.AlphaBlend);
        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).DrawSprite(399, 380, 160);
        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).DrawSprite(400, 508, 160);

        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).End();
    }

    private void DrawDoorHighlights()
    {
        _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).Begin(BlendState.AlphaBlend);
        _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).DrawSprite(9, 380, 160);
        _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).DrawSprite(10, 508, 160);
        _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).End();
    }

    /// <summary>
    ///     What the booth says once all three scanners have read. Also starts the wait that sends blob back
    ///     to the game, which is why a Draw is allowed to change state here.
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

            _shapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(756, 504, 28, 28), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.FillRectangle(new Rectangle(792, 500, 36, 36), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.FillRectangle(new Rectangle(836, 498, 40, 40), new Color(0, (byte)_resultMessageFade, 0));
            _shapeDrawer.End();

            // Don't allow the sound to be called more than once
            if (!_soundPlayed)
            {
                _resources.GetSoundFxEngine().PlaySound("AccessSuccess", 1f, false);
                _soundPlayed = true;
            }
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

            _shapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(746, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.FillRectangle(new Rectangle(782, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.FillRectangle(new Rectangle(826, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
            _shapeDrawer.End();

            // Don't allow the sound to be called more than once
            if (!_soundPlayed)
            {
                _resources.GetSoundFxEngine().PlaySound("AccessFailed", 1f, false);
                _soundPlayed = true;
            }
        }

        // Start the pause timer
        if (_screenReadWaitFrames == ScreenReadNotWaiting)
        {
            _screenReadWaitFrames = 0;
        }

        _soundPlayed = true;
    }

    #region IGameScreen Members

    public void Reset(object? data)
    {
        _soundPlayed = false;
        _blobAnim1.Reset(Direction.Left);
        _blobAnim2.Reset(Direction.Right);

        ReadAccessCode();

        _screenReadWaitFrames = ScreenReadNotWaiting;
        _resultMessageFade = ColourLevels.Full;

        CheckInventoryAgainstCode();

        if (data != null)
        {
            MoveBlobToTheBoothDoor(data);
        }
    }

    /// <summary>
    ///     The code is stored on the screen as a string of digits, one per scanner.
    /// </summary>
    private void ReadAccessCode()
    {
        if (_gameScreens.CurrentGameScreenData.AccessCode != null)
        {
            for (int a = 0; a < _accessCode.Length; a++)
            {
                _accessCode[a] = int.Parse(_gameScreens.CurrentGameScreenData.AccessCode.Substring(a, 1));
            }
        }
    }

    /// <summary>
    ///     Scan blob's inventory to see if he is carrying the right stuff.
    /// </summary>
    private void CheckInventoryAgainstCode()
    {
        Success = true;

        for (int b = 0; b < _accessCode.Length; b++)
        {
            bool match = false;

            int a;
            for (a = 0; a < _blob.BlobInventory.Length; a++)
            {
                if ((int)_blob.BlobInventory[a] - 10 == _accessCode[b] ||
                    _blob.BlobInventory[a] == Collectables.AccessCard)
                {
                    match = true;
                    break;
                }
            }

            if (match)
            {
                _inventorySlotToScan[b] = a;
            }
            else
            {
                _inventorySlotToScan[b] = -1;
            }

            // if we don't have a match the shows over!
            if (!match)
            {
                Success = match;
            }

            _accessCodes[b].Reset(match);
        }
    }

    /// <summary>
    ///     Puts blob on the far side of the door he walked into, which is why the caller passes which side
    ///     he came from.
    /// </summary>
    private void MoveBlobToTheBoothDoor(object data)
    {
        {
            object[]? temp = data as object[];

            if (temp != null)
            {

                if ((Direction)temp[0] == Direction.Left)
                {
                    if (Success)
                    {
                        _blob.BlobXPos = ((int)temp[1] - 1) * 128 + 16;
                        _blob.BlobFrame = 0;
                    }
                    else
                    {
                        _blob.BlobXPos = (int)temp[1] * 128 + 48;
                        _blob.BlobFrame = 0;
                    }
                }
                else if ((Direction)temp[0] == Direction.Right)
                {
                    if (Success)
                    {
                        _blob.BlobXPos = ((int)temp[1] + 1) * 128 + 48;
                        _blob.BlobFrame = 10;
                    }
                    else
                    {
                        _blob.BlobXPos = (int)temp[1] * 128 + 16;
                        _blob.BlobFrame = 10;
                    }
                }
            }

        }
    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {
        //throw new NotImplementedException();
    }

    #endregion

}
