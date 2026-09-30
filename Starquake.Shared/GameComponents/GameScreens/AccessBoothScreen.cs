// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class AccessBoothScreen : GameComponentBase, IGameScreen
{

    private readonly int[] _accessCode;

    private readonly KeyScanner[] _accessCodes;

    private readonly BlobAccessBoothAnim _blobAnim1;
    private readonly BlobAccessBoothAnim _blobAnim2;
    private readonly int[] _inventorySlotToScan;

    private int _resultMessageFade;
    private DateTime _screenReadWait;
    private bool _soundPlayed;

    public AccessBoothScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        _accessCode = new int[3]
        {
            -1,
            -1,
            -1
        };
        _accessCodes = new KeyScanner[3];
        _inventorySlotToScan = new int[3];

        for (var a = 0; a < _accessCodes.Length; a++)
        {
            _accessCodes[a] = new KeyScanner(GameEngine, true);
        }

        _blobAnim1 = new BlobAccessBoothAnim(gameEngineReference, 164, 800);
        _blobAnim2 = new BlobAccessBoothAnim(gameEngineReference, 164, 800);

        Reset(null);
    }

    public int ScreenFade { get; set; }

    public bool Success { get; private set; }

    public override void Update(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        _blobAnim1.Update();
        _blobAnim2.Update();

        // Start first scan off....
        if (!_accessCodes[0].Scanned)
        {
            _accessCodes[0].StartScan();
            GameEngine.BlobStatusPanel.ScanSlot(_inventorySlotToScan[0]);
        }

        // Follow with the subsequent ones...
        for (var a = 0; a < _accessCodes.Length; a++)
        {
            if (_accessCodes[a].Scanned)
            {
                if (a < 2 && !_accessCodes[a + 1].Scanned)
                {
                    _accessCodes[a + 1].StartScan();
                    GameEngine.BlobStatusPanel.ScanSlot(_inventorySlotToScan[a + 1]);
                }
            }
            _accessCodes[a].Update();
        }

        _resultMessageFade = GameEngine.RandomNumberGenerator.Next(200, 256);

        if (_screenReadWait.Ticks > 0)
        {
            if (_screenReadWait + new TimeSpan(0, 0, 1) < DateTime.Now)
            {
                GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                GameEngine.GetGameScreen(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.AccessBooth, Success);

                GameEngine.CurrentGameScreen = GameScreenType.MainGame;
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GameEngine.Font2.Begin(BlendState.AlphaBlend);
        GameEngine.Font2.WriteText(350, 100, "SECURITY  DOOR", 2, Color.White, ColourLevels.Full);
        GameEngine.Font2.WriteText(370, 300, "ACCESS CODE", 2, Color.White, ColourLevels.Full);
        GameEngine.Font2.End();

        DrawBoothDoors(gameTime);

        _blobAnim1.Draw(gameTime);
        _blobAnim2.Draw(gameTime);

        DrawDoorHighlights(gameTime);

        for (var a = 0; a < 3; a++)
        {
            _accessCodes[a].Draw(_accessCode[a] + 10, 348 + a * 128, 364, gameTime);
        }

        if (_accessCodes[2].Scanned)
        {
            DrawScanResult();
        }
    }

    private void DrawBoothDoors(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).DrawSprite(399, 380, 160);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).DrawSprite(400, 508, 160);

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).End();
    }

    private void DrawDoorHighlights(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).DrawSprite(9, 380, 160);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).DrawSprite(10, 508, 160);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).End();
    }

    /// <summary>
    ///     What the booth says once all three scanners have read. Also starts the wait that sends blob back
    ///     to the game, which is why a Draw is allowed to change state here.
    /// </summary>
    private void DrawScanResult()
    {
        if (Success)
        {
            GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(167, 498, 40, 40), new Color(0, (byte)_resultMessageFade, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(215, 500, 36, 36), new Color(0, (byte)_resultMessageFade, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(259, 504, 28, 28), new Color(0, (byte)_resultMessageFade, 0));
            GameEngine.ShapeDrawer.End();

            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteText(310, 500, "ACCESS AUTHORISED", 2, new Color(0, ColourLevels.Full, 0), (byte)_resultMessageFade);
            GameEngine.Font2.End();

            GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(756, 504, 28, 28), new Color(0, (byte)_resultMessageFade, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(792, 500, 36, 36), new Color(0, (byte)_resultMessageFade, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(836, 498, 40, 40), new Color(0, (byte)_resultMessageFade, 0));
            GameEngine.ShapeDrawer.End();

            // Don't allow the sound to be called more than once
            if (!_soundPlayed)
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("AccessSuccess", 1f, false);
                _soundPlayed = true;
            }
        }
        else
        {
            GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(157, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(205, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(249, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
            GameEngine.ShapeDrawer.End();

            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteText(300, 500, "ACCESS CODE INVALID", 2, Color.Red, (byte)_resultMessageFade);
            GameEngine.Font2.End();

            GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(746, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(782, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
            GameEngine.ShapeDrawer.FillRectangle(new Rectangle(826, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
            GameEngine.ShapeDrawer.End();

            // Don't allow the sound to be called more than once
            if (!_soundPlayed)
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("AccessFailed", 1f, false);
                _soundPlayed = true;
            }
        }

        // Start the pause timer
        if (_screenReadWait.Ticks == 0)
        {
            _screenReadWait = DateTime.Now;
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

        _screenReadWait = new DateTime(0);
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
        if (GameEngine.GameScreens.CurrentGameScreenData.AccessCode != null)
        {
            for (var a = 0; a < _accessCode.Length; a++)
            {
                _accessCode[a] = int.Parse(GameEngine.GameScreens.CurrentGameScreenData.AccessCode.Substring(a, 1));
            }
        }
    }

    /// <summary>
    ///     Scan blob's inventory to see if he is carrying the right stuff.
    /// </summary>
    private void CheckInventoryAgainstCode()
    {
        Success = true;

        for (var b = 0; b < _accessCode.Length; b++)
        {
            var match = false;

            int a;
            for (a = 0; a < GameEngine.Player.BlobInventory.Length; a++)
            {
                if ((int)GameEngine.Player.BlobInventory[a] - 10 == _accessCode[b] ||
                    GameEngine.Player.BlobInventory[a] == Collectables.AccessCard)
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
        var temp = data as object[];

        if (temp != null)
        {

            if ((Direction)temp[0] == Direction.Left)
            {
                if (Success)
                {
                    GameEngine.Player.BlobXPos = ((int)temp[1] - 1) * 128 + 16;
                    GameEngine.Player.BlobFrame = 0;
                }
                else
                {
                    GameEngine.Player.BlobXPos = (int)temp[1] * 128 + 48;
                    GameEngine.Player.BlobFrame = 0;
                }
            }
            else if ((Direction)temp[0] == Direction.Right)
            {
                if (Success)
                {
                    GameEngine.Player.BlobXPos = ((int)temp[1] + 1) * 128 + 48;
                    GameEngine.Player.BlobFrame = 10;
                }
                else
                {
                    GameEngine.Player.BlobXPos = (int)temp[1] * 128 + 16;
                    GameEngine.Player.BlobFrame = 10;
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