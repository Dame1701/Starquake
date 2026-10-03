// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.Input;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

internal class CheopsScreen : IGameScreen
{
    /// <summary>
    ///     The value <see cref="_itemToSwap" /> holds while no inventory slot has been picked yet. It
    ///     is a slot index everywhere else, and no inventory is anywhere near this long.
    /// </summary>
    private const int NoItemSelected = 255;

    /// <summary>
    ///     How many elements the pyramid puts on offer, which is also how many number keys pick one.
    ///     The artwork counts them out as "1." to "5." across the screen, so the number is a fact
    ///     about the picture as much as about the choice.
    /// </summary>
    private const int ElementsOnOffer = 5;

    /// <summary>
    ///     The key that takes each offered element, in the order they are drawn.
    /// </summary>
    private static readonly Keys[] SelectionKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5];

    private readonly Blob _blob;
    private readonly CoreElements _elements;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;
    private readonly ToggleKeys _input;
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

    /// <summary>
    ///     How many digits the pyramid asks for, and so how many scanners it shows. The world
    ///     generator draws pyramid codes from 10 to 99, which is the same two.
    /// </summary>
    private const int DigitsInCode = 2;

    /// <summary>
    ///     Which slot each scanner reads, and whether the pyramid is satisfied. Replaced whole by
    ///     <see cref="CheckInventoryAgainstCode" /> rather than written into a digit at a time.
    /// </summary>
    private AccessCodeScan _scan = new([], false);

    private int _resultMessageFade;
    private int[] _accessCode = [];
    private readonly KeyScanner[] _accessCodes;
    private int _screenReadWaitFrames = ScreenReadNotWaiting;
    private bool _elementSelectionMode;
    private bool _soundPlayed;

    private InteractiveItem[] _itemsToSelect = [];
    private int _itemToSwap;


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
    ///     Picks at random from whatever Blob is carrying that is not his access card or key. If that is
    ///     nothing at all he has to give up the key, or the card if he has no key.
    /// </summary>
    private void ChooseItemToSwap()
    {
        var slotsToSelectFrom = new List<int>();
        _itemToSwap = NoItemSelected;

        for (int a = 0; a < _blob.BlobInventory.Length; a++)
        {
            if (_blob.BlobInventory[a] != Collectables.AccessCard &&
                _blob.BlobInventory[a] != Collectables.Key &&
                _blob.BlobInventory[a] != Collectables.None)
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
    ///     Which slot Blob is carrying one particular thing in, or <see cref="NoItemSelected" /> if he
    ///     is not carrying it. The last such slot rather than the first, as the two loops this
    ///     replaced both were - they kept overwriting their answer rather than stopping.
    /// </summary>
    private int LastSlotHolding(Collectables item)
    {
        int slot = NoItemSelected;

        for (int a = 0; a < _blob.BlobInventory.Length; a++)
        {
            if (_blob.BlobInventory[a] == item)
            {
                slot = a;
            }
        }

        return slot;
    }

    /// <summary>
    ///     Five to choose from: one the pyramid actually wants, and the item Blob would be giving up.
    /// </summary>
    private void ChooseElementsOnOffer()
    {
        {
            _itemsToSelect = new InteractiveItem[ElementsOnOffer];

            // Get an element from the core if there is one, if there isn't we've completed the game and this should never happen....
            List<CoreElement> activeElements = _elements.GetActiveElements();
            int coreElementSlot = -1;
            int coreElement = 0;

            if (activeElements.Count > 0)
            {
                coreElementSlot = _randomNumberGenerator.Next(0, ElementsOnOffer);
                coreElement = activeElements[_randomNumberGenerator.Next(0, activeElements.Count)].CurrentElement;
            }

            // The item to swap takes the first slot the core element did not
            int swapItemSlot = coreElementSlot == 0 ? 1 : 0;

            for (int a = 0; a < ElementsOnOffer; a++)
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

    /// <summary>
    ///     The swap itself, once the player has pressed one of the five number keys: the chosen
    ///     element replaces what Blob was giving up, the pyramid is spent, and the game resumes.
    /// </summary>
    /// <remarks>
    ///     This was five copies of the same seven lines inside <see cref="DrawElementSelection" />,
    ///     one per key. Reading the keyboard and handing the screen over from a draw meant the swap
    ///     was attempted once per drawn frame rather than once per logic frame - nearly three times
    ///     a frame at 144Hz - and went on being attempted while the game was paused, because
    ///     <c>Draw</c> does not stop when <c>Update</c> does.
    /// </remarks>
    private void TakeChosenElement()
    {
        for (int slot = 0; slot < ElementsOnOffer; slot++)
        {
            if (!_input.KeyboardState.IsKeyDown(SelectionKeys[slot]))
            {
                continue;
            }

            _blob.BlobInventory[_itemToSwap] = (Collectables)_itemsToSelect[slot].Type;
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
    ///     that moves the screen on. Done last so the wait is a whole
    ///     <see cref="ScreenReadWaitFrames" /> long rather than one frame short of it.
    /// </summary>
    /// <remarks>
    ///     This was <see cref="DrawElementsOnOffer" />'s job, which meant the sound was asked for once
    ///     per drawn frame and needed a latch to stop it playing sixty times a second. Both belong on
    ///     the logic clock: the wait is counted in logic frames, so starting it from a draw tied the
    ///     length of the pause to the display's refresh rate.
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

    /// <summary>
    ///     What the pyramid is holding, before the player has picked anything to swap for it.
    /// </summary>
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

            // Draw the result
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

    /// <summary>
    ///     The five inventory slots the player chooses from once a swap has been started.
    /// </summary>
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
        pickups.DrawSprite((int)_blob.BlobInventory[_itemToSwap], 590, 168, Color.White);

        for (int a = 0; a < ElementsOnOffer; a++)
        {
            pickups.DrawSprite(_itemsToSelect[a].Type, a * 216 + 100, 298, Color.White);
        }

        pickups.End();
    }
}
