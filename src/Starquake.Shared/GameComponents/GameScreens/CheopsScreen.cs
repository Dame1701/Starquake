// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.Input;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.World;

namespace Starquake.GameComponents.GameScreens;

internal class CheopsScreen : IGameScreen
{
    /// <summary>
    ///     What <see cref="_itemToSwap" /> holds while no inventory slot has been picked. It is a slot
    ///     index everywhere else, and no inventory is anywhere near this long.
    /// </summary>
    private const int NoItemSelected = 255;

    /// <summary>
    ///     How many elements the pyramid offers, and so how many number keys pick one. The artwork
    ///     counts them out as "1." to "5." across the screen, so this is a fact about the picture as
    ///     much as the choice.
    /// </summary>
    private const int ElementsOnOffer = 5;

    /// <summary>
    ///     How long the result is left on screen before the game moves on, in logic frames. One
    ///     second.
    /// </summary>
    /// <remarks>
    ///     Counted in frames rather than against <c>DateTime.Now</c>, which is local time: a DST step
    ///     or an NTP correction landing in that second would turn the pause into an hour.
    /// </remarks>
    private const int ScreenReadWaitFrames = LogicClock.FramesPerSecond;

    /// <summary>What <see cref="_screenReadWaitFrames" /> holds before the pause has started.</summary>
    private const int ScreenReadNotWaiting = -1;

    /// <summary>
    ///     How many digits the pyramid asks for, and so how many scanners it shows. The world
    ///     generator draws pyramid codes from 10 to 99, which is the same two digits.
    /// </summary>
    private const int DigitsInCode = 2;

    /// <summary>The key that takes each offered element, in the order they are drawn.</summary>
    private static readonly Keys[] SelectionKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5];
    private readonly KeyScanner[] _accessCodes;

    private readonly Blob _blob;
    private readonly CoreElements _elements;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ToggleKeys _input;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;
    private readonly StatusPanel _statusPanel;
    private int[] _accessCode = [];
    private bool _elementSelectionMode;

    private InteractiveItem[] _itemsToSelect = [];
    private int _itemToSwap;

    private int _resultMessageFade;

    /// <summary>
    ///     Which slot each scanner reads, and whether the pyramid is satisfied. Replaced whole by
    ///     <see cref="CheckInventoryAgainstCode" /> rather than written a digit at a time.
    /// </summary>
    private AccessCodeScan _scan = new([], false);
    private int _screenReadWaitFrames = ScreenReadNotWaiting;
    private bool _soundPlayed;


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
        Shapes shapeDrawer,
        ToggleKeys input)
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
        _input = input;

        _accessCodes = new KeyScanner[DigitsInCode];

        for (int a = 0; a < _accessCodes.Length; a++)
        {
            _accessCodes[a] = new KeyScanner(_resources, _graphicsSet, _randomNumberGenerator, true);
        }

        Reset();
    }


    public void Update(GameTime gameTime)
    {
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.MachineryBackground, 0.5f, true);

        if (!_elementSelectionMode)
        {
            KeyScanner.ScanCodeInTurn(_accessCodes, _scan, _statusPanel);

            _resultMessageFade = _randomNumberGenerator.Next(200, 256);
        }

        if (_screenReadWaitFrames >= 0)
        {
            _screenReadWaitFrames++;

            if (_screenReadWaitFrames > ScreenReadWaitFrames)
            {
                if (_scan.Accepted) // move into selection mode
                {
                    _elementSelectionMode = true;
                }
                else // Kick you out
                {
                    _screenDirectory.MainGame.ScreenFade = 40;
                    _screenDirectory.Current = GameScreenType.MainGame;
                }
            }
        }

        AnnounceScanResult();

        if (_elementSelectionMode)
        {
            TakeChosenElement();
        }
    }

    public void Draw()
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(298, 100, "CHEOPS PYRAMID", 2, Color.White, ColourLevels.Full);
        _font.End();

        DrawPyramid();

        if (_elementSelectionMode)
        {
            DrawElementSelection();
        }
        else
        {
            DrawElementsOnOffer();
        }
    }

    public void Reset()
    {
        _soundPlayed = false;
        _screenReadWaitFrames = ScreenReadNotWaiting;
        _resultMessageFade = ColourLevels.Full;
        _elementSelectionMode = false;

        ReadAccessCode();
        CheckInventoryAgainstCode();
        ChooseItemToSwap();

        if (_scan.Accepted)
        {
            ChooseElementsOnOffer();
        }
    }

    /// <summary>
    ///     The code is stored on the screen as a string of digits, one per element the pyramid wants.
    /// </summary>
    private void ReadAccessCode()
        => _accessCode = AccessCodeLock.Digits(_gameScreens.CurrentGameScreenData.CheopsAccessCode, DigitsInCode);

    /// <summary>Checks Blob's inventory against the code.</summary>
    private void CheckInventoryAgainstCode()
    {
        _scan = AccessCodeLock.Scan(_blob.Carrying, _accessCode);

        for (int digit = 0; digit < _accessCodes.Length; digit++)
        {
            _accessCodes[digit].Reset(_scan.SlotToScan[digit] != AccessCodeLock.NoSlot);
        }
    }

    /// <summary>
    ///     Picks at random from whatever Blob carries that is not his access card or key. With nothing
    ///     else to give, he gives up the key, or the card if he has no key.
    /// </summary>
    private void ChooseItemToSwap()
    {
        var slotsToSelectFrom = new List<int>();
        _itemToSwap = NoItemSelected;

        for (int a = 0; a < _blob.Carrying.Length; a++)
        {
            if (_blob.Carrying[a] != Collectables.AccessCard &&
                _blob.Carrying[a] != Collectables.Key &&
                _blob.Carrying[a] != Collectables.None)
            {
                slotsToSelectFrom.Add(a);
            }


        }

        if (slotsToSelectFrom.Count > 0)
        {
            _itemToSwap = slotsToSelectFrom[_randomNumberGenerator.Next(0, slotsToSelectFrom.Count)];

            return;
        }

        _itemToSwap = LastSlotHolding(Collectables.Key);

        if (_itemToSwap == NoItemSelected)
        {
            _itemToSwap = LastSlotHolding(Collectables.AccessCard);
        }
    }

    /// <summary>
    ///     Which slot Blob carries one particular thing in, or <see cref="NoItemSelected" /> if he is
    ///     not carrying it. Deliberately the last such slot rather than the first.
    /// </summary>
    private int LastSlotHolding(Collectables item)
    {
        int slot = NoItemSelected;

        for (int a = 0; a < _blob.Carrying.Length; a++)
        {
            if (_blob.Carrying[a] == item)
            {
                slot = a;
            }
        }

        return slot;
    }

    /// <summary>
    ///     Five to choose from: one the pyramid actually wants, plus the item Blob would give up.
    /// </summary>
    private void ChooseElementsOnOffer()
    {
        {
            _itemsToSelect = new InteractiveItem[ElementsOnOffer];

            // There is always one: a core with nothing left to replace has already ended the game
            // before the pyramid can be reached.
            List<CoreElement> activeElements = _elements.GetActiveElements();
            int coreElementSlot = -1;
            int coreElement = 0;

            if (activeElements.Count > 0)
            {
                coreElementSlot = _randomNumberGenerator.Next(0, ElementsOnOffer);
                coreElement = activeElements[_randomNumberGenerator.Next(0, activeElements.Count)].CurrentElement;
            }

            // The item to swap takes the first slot the core element did not.
            int swapItemSlot = coreElementSlot == 0 ? 1 : 0;

            for (int a = 0; a < ElementsOnOffer; a++)
            {
                if (a == coreElementSlot)
                {
                    _itemsToSelect[a] = new InteractiveItem(0, 0, coreElement);
                }
                else if (a == swapItemSlot)
                {
                    _itemsToSelect[a] = new InteractiveItem(0, 0, (int)_blob.Carrying[_itemToSwap]);
                }
                else
                {
                    _itemsToSelect[a] = new InteractiveItem(0, 0, _randomNumberGenerator.Next(0, 28));
                }
            }
        }
    }

    /// <summary>
    ///     The swap itself, once one of the five number keys is pressed: the chosen element replaces
    ///     what Blob gives up, the pyramid is spent, and the game resumes.
    /// </summary>
    /// <remarks>
    ///     Reading the keyboard and handing the screen over belong on the logic clock. From a draw the
    ///     swap would be attempted once per drawn frame, nearly three times a frame at 144Hz, and
    ///     would carry on while the game was paused, because <c>Draw</c> does not stop when
    ///     <c>Update</c> does.
    /// </remarks>
    private void TakeChosenElement()
    {
        for (int slot = 0; slot < ElementsOnOffer; slot++)
        {
            if (!_input.KeyboardState.IsKeyDown(SelectionKeys[slot]))
            {
                continue;
            }

            _blob.Carrying[_itemToSwap] = (Collectables)_itemsToSelect[slot].Type;
            _screenDirectory.MainGame.ScreenFade = 40;
            _screenDirectory.Current = GameScreenType.MainGame;
            _gameScreens.CurrentGameScreenData.CheopsPyramid = null;
            _gameScreens.UpdateCollisionMap(false);
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobPickUp, 0.5f, false);

            return;
        }
    }


    /// <summary>
    ///     The pyramid's verdict, once both scanners have read: the sound, and the start of the wait
    ///     that moves the screen on. Done last, so the wait is a whole
    ///     <see cref="ScreenReadWaitFrames" /> rather than one frame short.
    /// </summary>
    /// <remarks>
    ///     Both belong on the logic clock. From a draw the sound is asked for once per drawn frame and
    ///     needs a latch to stop it playing sixty times a second, and the wait, counted in logic
    ///     frames, would have its length tied to the display's refresh rate.
    /// </remarks>
    private void AnnounceScanResult()
    {
        if (_elementSelectionMode || !_accessCodes[1].Scanned || _soundPlayed)
        {
            return;
        }

        _resources.GetSoundFxEngine()
            .PlaySound(_scan.Accepted ? Assets.Sounds.AccessSuccess : Assets.Sounds.AccessFailed, 1f, false);

        _soundPlayed = true;
        _screenReadWaitFrames = 0;
    }

    private void DrawPyramid()
    {
        if (_graphicsSet.IsOriginal)
        {
            SpriteCollection cheopsPyramidOriginal = _resources.GetSpriteCollection(Assets.Graphics.CheopsPyramidOriginal);
            cheopsPyramidOriginal.Begin(BlendState.AlphaBlend);
            cheopsPyramidOriginal.DrawSprite(0, 750, 69);
            cheopsPyramidOriginal.End();
        }
        else
        {
            SpriteCollection cheopsPyramid = _resources.GetSpriteCollection(Assets.Graphics.CheopsPyramid);
            cheopsPyramid.Begin(BlendState.AlphaBlend);
            cheopsPyramid.DrawSprite(0, 750, 69);
            cheopsPyramid.End();
        }
    }

    /// <summary>What the pyramid holds, before anything has been picked to swap for it.</summary>
    private void DrawElementsOnOffer()
    {
        {
            _font.Begin(BlendState.AlphaBlend);
            _font.WriteText(366, 200, "CHEOPS KEY CODE", 2, Color.White, ColourLevels.Full);
            _font.End();

            for (int a = 0; a < DigitsInCode; a++)
            {
                _accessCodes[a].Draw(_accessCode[a] + AccessCodeLock.FirstDigitPickup, 420 + a * 128, 260);
            }

            if (_accessCodes[1].Scanned)
            {
                if (_scan.Accepted)
                {
                    _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    _shapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(0, _resultMessageFade, 0));
                    _shapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(0, _resultMessageFade, 0));
                    _shapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(0, _resultMessageFade, 0));
                    _shapeDrawer.End();

                    _font.Begin(BlendState.AlphaBlend);
                    _font.WriteText(340, 500, "ACCESS AUTHORISED", 2, new Color(0, ColourLevels.Full, 0), (byte)_resultMessageFade);
                    _font.End();

                    _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    _shapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(0, _resultMessageFade, 0));
                    _shapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(0, _resultMessageFade, 0));
                    _shapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(0, _resultMessageFade, 0));
                    _shapeDrawer.End();
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

                    _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    _shapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(_resultMessageFade, 0, 0, 0));
                    _shapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(_resultMessageFade, 0, 0, 0));
                    _shapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(_resultMessageFade, 0, 0, 0));
                    _shapeDrawer.End();
                }
            }
        }
    }

    /// <summary>The five inventory slots chosen from once a swap has started.</summary>
    private void DrawElementSelection()
    {
        _font.Begin(BlendState.AlphaBlend);
        _font.WriteText(378, 190, "EXCHANGE       FOR", 2, Color.White, ColourLevels.Full);
        _font.WriteText(65, 266, "1.              2.             3.            4.             5.", 2, Color.OrangeRed, ColourLevels.Full);
        _font.WriteText(327, 432, "HIT ANY KEY FROM  TO", 2, Color.CornflowerBlue, ColourLevels.Full);
        _font.WriteText(690, 432, "1", 2, Color.White, ColourLevels.Full);
        _font.WriteText(783, 432, "5", 2, Color.White, ColourLevels.Full);
        _font.End();

        SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
        pickups.Begin(BlendState.AlphaBlend);
        pickups.DrawSprite((int)_blob.Carrying[_itemToSwap], 590, 168, Color.White);

        for (int a = 0; a < ElementsOnOffer; a++)
        {
            pickups.DrawSprite(_itemsToSelect[a].Type, a * 216 + 100, 298, Color.White);
        }

        pickups.End();
    }
}
