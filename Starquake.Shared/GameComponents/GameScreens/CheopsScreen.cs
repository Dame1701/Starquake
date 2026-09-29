// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Animations;
using Starquake.Engines;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class CheopsScreen : GameComponentBase, IGameScreen
{

    #region IGameScreen Members

    private int _resultMessageFade;
    private readonly int[] _accessCode;
    private readonly int[] _inventorySlotToScan;
    private readonly KeyScanner[] _accessCodes;
    private bool _codeSucceeded;
    private DateTime _screenReadWait;
    private bool _elementSelectionMode;
    private bool _soundPlayed;

    private InteractiveItem[] _itemsToSelect;
    private int _itemToSwap;

    public int ScreenFade { get; set; }

    public bool Success => true;

    public CheopsScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        _accessCode = new int[2]
        {
            -1,
            -1
        };
        _accessCodes = new KeyScanner[2];
        _inventorySlotToScan = new int[2];

        for (var a = 0; a < _accessCodes.Length; a++)
        {
            _accessCodes[a] = new KeyScanner(GameEngine, true);
        }

        Reset(null);
    }

    public void Reset(object data)
    {
        _soundPlayed = false;
        _screenReadWait = new DateTime(0);
        _resultMessageFade = 255;
        _elementSelectionMode = false;

        if (GameEngine.GameScreens.CurrentGameScreenData.CheopsAccessCode != null)
        {
            for (var a = 0; a < _accessCode.Length; a++)
            {
                _accessCode[a] = int.Parse(GameEngine.GameScreens.CurrentGameScreenData.CheopsAccessCode.Substring(a, 1));
            }
        }

        // Scan blob's inventory to see if we have the right stuff
        _codeSucceeded = true;

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
                _codeSucceeded = match;
            }

            _accessCodes[b].Reset(match);
        }

        // Pick an item from the inventory to swap
        var slotsToSelectFrom = new List<int>();
        _itemToSwap = 255;

        for (var a = 0; a < GameEngine.Player.BlobInventory.Length; a++)
        {
            if (GameEngine.Player.BlobInventory[a] != Collectables.AccessCard &&
                GameEngine.Player.BlobInventory[a] != Collectables.Key &&
                GameEngine.Player.BlobInventory[a] != Collectables.None)
            {
                slotsToSelectFrom.Add(a);
            }


        }

        if (slotsToSelectFrom.Count == 0)
        {
            for (var a = 0; a < GameEngine.Player.BlobInventory.Length; a++)
            {
                if (GameEngine.Player.BlobInventory[a] == Collectables.Key)
                {
                    _itemToSwap = a;
                }
            }

            // No key so use the access card
            if (_itemToSwap == 255)
            {
                for (var a = 0; a < GameEngine.Player.BlobInventory.Length; a++)
                {
                    if (GameEngine.Player.BlobInventory[a] == Collectables.AccessCard)
                    {
                        _itemToSwap = a;
                    }
                }
            }
        }
        else
        {
            _itemToSwap = slotsToSelectFrom[GameEngine.RandomNumberGenerator.Next(0, slotsToSelectFrom.Count)];
        }

        // Assign 5 elements to choose from, one being a required element, the other beingthe item to swap
        if (_codeSucceeded)
        {
            _itemsToSelect = new InteractiveItem[5];

            // Get an element from the core if there is one, if there isn't we've completed the game and this should never happen....
            var activeElements = GameEngine.Elements.GetActiveElements();
            if (activeElements.Count > 0)
            {
                _itemsToSelect[GameEngine.RandomNumberGenerator.Next(0, 5)] =
                    new InteractiveItem(
                        0,
                        0,
                        activeElements[GameEngine.RandomNumberGenerator.Next(0, activeElements.Count)].CurrentElement);
            }

            var populatedSwapItem = false;

            // Populate the other 3 slots
            for (var a = 0; a < 5; a++)
            {
                if (_itemsToSelect[a] == null)
                {
                    if (!populatedSwapItem)
                    {
                        populatedSwapItem = true;
                        _itemsToSelect[a] = new InteractiveItem(0, 0, (int)GameEngine.Player.BlobInventory[_itemToSwap]);
                    }
                    else
                    {
                        _itemsToSelect[a] = new InteractiveItem(0, 0, GameEngine.RandomNumberGenerator.Next(0, 28));
                    }
                }
            }
        }
    }


    public override void Update(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        if (!_elementSelectionMode)
        {
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
                    if (a < 1 && !_accessCodes[a + 1].Scanned)
                    {
                        _accessCodes[a + 1].StartScan();
                        GameEngine.BlobStatusPanel.ScanSlot(_inventorySlotToScan[a + 1]);
                    }
                }
                _accessCodes[a].Update();
            }

            _resultMessageFade = GameEngine.RandomNumberGenerator.Next(200, 256);
        }

        if (_screenReadWait.Ticks > 0)
        {
            if (_screenReadWait + new TimeSpan(0, 0, 1) < DateTime.Now)
            {
                if (_codeSucceeded) // move into selection mode
                {
                    _elementSelectionMode = true;
                }
                else // Kick you out
                {
                    GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                    GameEngine.CurrentGameScreen = GameScreenType.MainGame;
                }
            }
        }

    }


    public override void Draw(GameTime gameTime)
    {
        GameEngine.Font2.Begin(BlendState.AlphaBlend);
        GameEngine.Font2.WriteText(298, 100, "CHEOPS PYRAMID", 2, Color.White, 255);
        GameEngine.Font2.End();

        //gameEngine.SQText.WriteText("CHEOPS PYRAMID", 198, 100, gameTime, Color.White);

        if (GameEngine.UsingOldGraphics)
        {
            GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").Begin(BlendState.AlphaBlend, gameTime);
            GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0, 750, 69);
            GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").End();
        }
        else
        {
            GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").Begin(BlendState.AlphaBlend, gameTime);
            GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").DrawSprite(0, 750, 69);
            GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").End();
        }

        if (!_elementSelectionMode)
        {
            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteText(366, 200, "CHEOPS KEY CODE", 2, Color.White, 255);
            GameEngine.Font2.End();

            for (var a = 0; a < 2; a++)
            {
                _accessCodes[a].Draw(_accessCode[a] + 10, 420 + a * 128, 260, gameTime);
            }

            // Draw the result
            if (_accessCodes[1].Scanned)
            {
                // Start the pause timer
                if (_screenReadWait.Ticks == 0)
                {
                    _screenReadWait = DateTime.Now;
                }

                if (_codeSucceeded)
                {
                    GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(0, _resultMessageFade, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(0, _resultMessageFade, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(0, _resultMessageFade, 0));
                    GameEngine.ShapeDrawer.End();

                    GameEngine.Font2.Begin(BlendState.AlphaBlend);
                    GameEngine.Font2.WriteText(340, 500, "ACCESS AUTHORISED", 2, new Color(0, 255, 0), (byte)_resultMessageFade);
                    GameEngine.Font2.End();

                    GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(0, _resultMessageFade, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(0, _resultMessageFade, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(0, _resultMessageFade, 0));
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
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
                    GameEngine.ShapeDrawer.End();

                    GameEngine.Font2.Begin(BlendState.AlphaBlend);
                    GameEngine.Font2.WriteText(330, 500, "ACCESS CODE INVALID", 2, Color.Red, (byte)_resultMessageFade);
                    GameEngine.Font2.End();

                    GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
                    GameEngine.ShapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
                    GameEngine.ShapeDrawer.End();

                    // Don't allow the sound to be called more than once
                    if (!_soundPlayed)
                    {
                        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("AccessFailed", 1f, false);
                        _soundPlayed = true;
                    }
                }
            }
        }
        else // We're in selection mode
        {
            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteText(378, 190, "EXCHANGE       FOR", 2, Color.White, 255);
            GameEngine.Font2.WriteText(65, 266, "1.              2.             3.            4.             5.", 2, Color.OrangeRed, 255);
            GameEngine.Font2.WriteText(327, 432, "HIT ANY KEY FROM  TO", 2, Color.CornflowerBlue, 255);
            GameEngine.Font2.WriteText(690, 432, "1", 2, Color.White, 255);
            GameEngine.Font2.WriteText(783, 432, "5", 2, Color.White, 255);

            GameEngine.Font2.End();

            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);

            // Need to select something from the inventory randomly ignoring access card and key unless they are the only items....
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).DrawSprite((int)GameEngine.Player.BlobInventory[_itemToSwap], 590, 168, Color.White);

            for (var a = 0; a < 5; a++)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).DrawSprite(_itemsToSelect[a].Type, a * 216 + 100, 298, Color.White);
            }

            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).End();

            if (Keyboard.GetState().IsKeyDown(Keys.D1))
            {
                GameEngine.Player.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[0].Type;
                GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                GameEngine.CurrentGameScreen = GameScreenType.MainGame;
                GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                GameEngine.GameScreens.UpdateCollisionMap(false);
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.D2))
            {
                GameEngine.Player.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[1].Type;
                GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                GameEngine.CurrentGameScreen = GameScreenType.MainGame;
                GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                GameEngine.GameScreens.UpdateCollisionMap(false);
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.D3))
            {
                GameEngine.Player.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[2].Type;
                GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                GameEngine.CurrentGameScreen = GameScreenType.MainGame;
                GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                GameEngine.GameScreens.UpdateCollisionMap(false);
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.D4))
            {
                GameEngine.Player.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[3].Type;
                GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                GameEngine.CurrentGameScreen = GameScreenType.MainGame;
                GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                GameEngine.GameScreens.UpdateCollisionMap(false);
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.D5))
            {
                GameEngine.Player.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[4].Type;
                GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 40;
                GameEngine.CurrentGameScreen = GameScreenType.MainGame;
                GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                GameEngine.GameScreens.UpdateCollisionMap(false);
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            }
        }
    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {

    }

    #endregion

}