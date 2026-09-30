// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.GameComponents;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class CheopsScreen : GameComponentBase, IGameScreen
{
    private readonly Blob _blob;
    private readonly CoreElements _elements;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;
    private readonly StatusPanel _statusPanel;


    /// <summary>
    ///     The value <see cref="_itemToSwap" /> holds while no inventory slot has been picked yet. It
    ///     is a slot index everywhere else, and no inventory is anywhere near this long.
    /// </summary>
    private const int NoItemSelected = 255;


    #region IGameScreen Members

    private int _resultMessageFade;
    private readonly int[] _accessCode;
    private readonly int[] _inventorySlotToScan;
    private readonly KeyScanner[] _accessCodes;
    private bool _codeSucceeded;
    private DateTime _screenReadWait;
    private bool _elementSelectionMode;
    private bool _soundPlayed;

    private InteractiveItem[] _itemsToSelect = [];
    private int _itemToSwap;

    public int ScreenFade { get; set; }

    public bool Success => true;

    public CheopsScreen(
        StatusPanel statusPanel,
        ResourceManager resources,
        CoreElements elements,
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
        _elements = elements;
        _font = font;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;
        _screenDirectory = screenDirectory;
        _shapeDrawer = shapeDrawer;

        _accessCode = new int[2]
        {
            -1,
            -1
        };
        _accessCodes = new KeyScanner[2];
        _inventorySlotToScan = new int[2];

        for (var a = 0; a < _accessCodes.Length; a++)
        {
            _accessCodes[a] = new KeyScanner(_resources, _graphicsSet, _randomNumberGenerator, true);
        }

        Reset(null);
    }

    public void Reset(object? data)
    {
        _soundPlayed = false;
        _screenReadWait = new DateTime(0);
        _resultMessageFade = ColourLevels.Full;
        _elementSelectionMode = false;

        ReadAccessCode();
        CheckInventoryAgainstCode();
        ChooseItemToSwap();

        if (_codeSucceeded)
        {
            ChooseElementsOnOffer();
        }
    }

    /// <summary>
    ///     The code is stored on the screen as a string of digits, one per element the pyramid wants.
    /// </summary>
    private void ReadAccessCode()
    {
        if (_gameScreens.CurrentGameScreenData.CheopsAccessCode != null)
        {
            for (var a = 0; a < _accessCode.Length; a++)
            {
                _accessCode[a] = int.Parse(_gameScreens.CurrentGameScreenData.CheopsAccessCode.Substring(a, 1));
            }
        }
    }

    /// <summary>
    ///     Scan blob's inventory to see if he is carrying the right stuff.
    /// </summary>
    private void CheckInventoryAgainstCode()
    {
        _codeSucceeded = true;

        for (var b = 0; b < _accessCode.Length; b++)
        {
            var match = false;

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
                _codeSucceeded = match;
            }

            _accessCodes[b].Reset(match);
        }
    }

    /// <summary>
    ///     Picks at random from whatever blob is carrying that is not his access card or key. If that is
    ///     nothing at all he has to give up the key, or the card if he has no key.
    /// </summary>
    private void ChooseItemToSwap()
    {
        var slotsToSelectFrom = new List<int>();
        _itemToSwap = NoItemSelected;

        for (var a = 0; a < _blob.BlobInventory.Length; a++)
        {
            if (_blob.BlobInventory[a] != Collectables.AccessCard &&
                _blob.BlobInventory[a] != Collectables.Key &&
                _blob.BlobInventory[a] != Collectables.None)
            {
                slotsToSelectFrom.Add(a);
            }


        }

        if (slotsToSelectFrom.Count == 0)
        {
            for (var a = 0; a < _blob.BlobInventory.Length; a++)
            {
                if (_blob.BlobInventory[a] == Collectables.Key)
                {
                    _itemToSwap = a;
                }
            }

            // No key so use the access card
            if (_itemToSwap == NoItemSelected)
            {
                for (var a = 0; a < _blob.BlobInventory.Length; a++)
                {
                    if (_blob.BlobInventory[a] == Collectables.AccessCard)
                    {
                        _itemToSwap = a;
                    }
                }
            }
        }
        else
        {
            _itemToSwap = slotsToSelectFrom[_randomNumberGenerator.Next(0, slotsToSelectFrom.Count)];
        }
    }

    /// <summary>
    ///     Five to choose from: one the pyramid actually wants, and the item blob would be giving up.
    /// </summary>
    private void ChooseElementsOnOffer()
    {
    {
        _itemsToSelect = new InteractiveItem[5];

        // Get an element from the core if there is one, if there isn't we've completed the game and this should never happen....
        var activeElements = _elements.GetActiveElements();
        var coreElementSlot = -1;
        var coreElement = 0;

        if (activeElements.Count > 0)
        {
            coreElementSlot = _randomNumberGenerator.Next(0, 5);
            coreElement = activeElements[_randomNumberGenerator.Next(0, activeElements.Count)].CurrentElement;
        }

        // The item to swap takes the first slot the core element did not
        var swapItemSlot = coreElementSlot == 0 ? 1 : 0;

        for (var a = 0; a < 5; a++)
        {
            if (a == coreElementSlot)
            {
                _itemsToSelect[a] = new InteractiveItem(0, 0, coreElement);
            }
            else if (a == swapItemSlot)
            {
                _itemsToSelect[a] = new InteractiveItem(0, 0, (int)_blob.BlobInventory[_itemToSwap]);
            }
            else
            {
                _itemsToSelect[a] = new InteractiveItem(0, 0, _randomNumberGenerator.Next(0, 28));
            }
        }
    }
    }


    public override void Update(GameTime gameTime)
    {
        _resources.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        if (!_elementSelectionMode)
        {
            // Start first scan off....
            if (!_accessCodes[0].Scanned)
            {
                _accessCodes[0].StartScan();
                _statusPanel.ScanSlot(_inventorySlotToScan[0]);
            }

            // Follow with the subsequent ones...
            for (var a = 0; a < _accessCodes.Length; a++)
            {
                if (_accessCodes[a].Scanned)
                {
                    if (a < 1 && !_accessCodes[a + 1].Scanned)
                    {
                        _accessCodes[a + 1].StartScan();
                        _statusPanel.ScanSlot(_inventorySlotToScan[a + 1]);
                    }
                }
                _accessCodes[a].Update();
            }

            _resultMessageFade = _randomNumberGenerator.Next(200, 256);
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
                    _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
                    _screenDirectory.Current = GameScreenType.MainGame;
                }
            }
        }

    }


    public override void Draw(GameTime gameTime)
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(298, 100, "CHEOPS PYRAMID", 2, Color.White, ColourLevels.Full);
        _font.End();

        DrawPyramid(gameTime);

        if (_elementSelectionMode)
        {
            DrawElementSelection(gameTime);
        }
        else
        {
            DrawElementsOnOffer(gameTime);
        }
    }

    private void DrawPyramid(GameTime gameTime)
    {
        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection("CheopsPyramidOriginal").Begin(BlendState.AlphaBlend, gameTime);
            _resources.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0, 750, 69);
            _resources.GetSpriteCollection("CheopsPyramidOriginal").End();
        }
        else
        {
            _resources.GetSpriteCollection("CheopsPyramid").Begin(BlendState.AlphaBlend, gameTime);
            _resources.GetSpriteCollection("CheopsPyramid").DrawSprite(0, 750, 69);
            _resources.GetSpriteCollection("CheopsPyramid").End();
        }
    }

    /// <summary>
    ///     What the pyramid is holding, before the player has picked anything to swap for it.
    /// </summary>
    private void DrawElementsOnOffer(GameTime gameTime)
    {
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(366, 200, "CHEOPS KEY CODE", 2, Color.White, ColourLevels.Full);
        _font.End();

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
                _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                _shapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(0, _resultMessageFade, 0));
                _shapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(0, _resultMessageFade, 0));
                _shapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(0, _resultMessageFade, 0));
                _shapeDrawer.End();

                _font.Begin(BlendState.AlphaBlend);
                _font.WriteText(340, 500, "ACCESS AUTHORISED", 2, new Color(0, ColourLevels.Full, 0), (byte)_resultMessageFade);
                _font.End();

                _shapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                _shapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(0, _resultMessageFade, 0));
                _shapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(0, _resultMessageFade, 0));
                _shapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(0, _resultMessageFade, 0));
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
                _shapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
                _shapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
                _shapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
                _shapeDrawer.End();

                _font.Begin(BlendState.AlphaBlend);
                _font.WriteText(330, 500, "ACCESS CODE INVALID", 2, Color.Red, (byte)_resultMessageFade);
                _font.End();

                _shapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                _shapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
                _shapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
                _shapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
                _shapeDrawer.End();

                // Don't allow the sound to be called more than once
                if (!_soundPlayed)
                {
                    _resources.GetSoundFxEngine().PlaySound("AccessFailed", 1f, false);
                    _soundPlayed = true;
                }
            }
        }
    }
    }

    /// <summary>
    ///     The five inventory slots the player chooses from once a swap has been started.
    /// </summary>
    private void DrawElementSelection(GameTime gameTime)
    {
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(378, 190, "EXCHANGE       FOR", 2, Color.White, ColourLevels.Full);
        _font.WriteText(65, 266, "1.              2.             3.            4.             5.", 2, Color.OrangeRed, ColourLevels.Full);
        _font.WriteText(327, 432, "HIT ANY KEY FROM  TO", 2, Color.CornflowerBlue, ColourLevels.Full);
        _font.WriteText(690, 432, "1", 2, Color.White, ColourLevels.Full);
        _font.WriteText(783, 432, "5", 2, Color.White, ColourLevels.Full);

        _font.End();

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).Begin(BlendState.AlphaBlend, gameTime);

        // Need to select something from the inventory randomly ignoring access card and key unless they are the only items....
        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite((int)_blob.BlobInventory[_itemToSwap], 590, 168, Color.White);

        for (var a = 0; a < 5; a++)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite(_itemsToSelect[a].Type, a * 216 + 100, 298, Color.White);
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();

        if (Keyboard.GetState().IsKeyDown(Keys.D1))
        {
            _blob.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[0].Type;
            _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
            _screenDirectory.Current = GameScreenType.MainGame;
            _gameScreens.CurrentGameScreenData.CheopsPyramid = null;
            _gameScreens.UpdateCollisionMap(false);
            _resources.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
        }
        else if (Keyboard.GetState().IsKeyDown(Keys.D2))
        {
            _blob.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[1].Type;
            _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
            _screenDirectory.Current = GameScreenType.MainGame;
            _gameScreens.CurrentGameScreenData.CheopsPyramid = null;
            _gameScreens.UpdateCollisionMap(false);
            _resources.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
        }
        else if (Keyboard.GetState().IsKeyDown(Keys.D3))
        {
            _blob.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[2].Type;
            _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
            _screenDirectory.Current = GameScreenType.MainGame;
            _gameScreens.CurrentGameScreenData.CheopsPyramid = null;
            _gameScreens.UpdateCollisionMap(false);
            _resources.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
        }
        else if (Keyboard.GetState().IsKeyDown(Keys.D4))
        {
            _blob.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[3].Type;
            _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
            _screenDirectory.Current = GameScreenType.MainGame;
            _gameScreens.CurrentGameScreenData.CheopsPyramid = null;
            _gameScreens.UpdateCollisionMap(false);
            _resources.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
        }
        else if (Keyboard.GetState().IsKeyDown(Keys.D5))
        {
            _blob.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[4].Type;
            _screenDirectory.Get(GameScreenType.MainGame).ScreenFade = 40;
            _screenDirectory.Current = GameScreenType.MainGame;
            _gameScreens.CurrentGameScreenData.CheopsPyramid = null;
            _gameScreens.UpdateCollisionMap(false);
            _resources.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
        }
    }
    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {

    }

    #endregion

}