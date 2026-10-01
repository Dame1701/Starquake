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
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

internal class TeleportScreen : GameComponentBase, IGameScreen
{
    private readonly Blob _blob;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly PresentationSettings _presentation;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;
    private readonly ToggleKeys _input;


    private readonly Dictionary<int, string> _teleportCodes;
    /// <summary>
    ///     The destination code as typed so far, one underscore per letter still to be entered.
    /// </summary>
    private const string EmptyCode = "_____";

    private string? _teleportCode;
    private string _currentCode = EmptyCode;
    private string _previousCode = string.Empty;
    private bool _keyPressed;
    private int _keyIdPressed;
    private bool _attemptingTeleport;
    private bool _codeRecognised;
    private int _teleportCounter;
    private int _oldOffsetX;
    private int _oldOffsetY;

    private readonly FaderAnimation[] _keyFades;
    private readonly FaderAnimation _powerSupplyFader;

    private readonly FaderAnimation _teleportingMessageFader;

    public int ScreenFade { get; set; }


    public TeleportScreen(
        ResourceManager resources,
        BitmapFont font,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        PresentationSettings presentation,
        Random randomNumberGenerator,
        ScreenDirectory screenDirectory,
        Shapes shapeDrawer,
        ToggleKeys input)
    {
        _resources = resources;
        _font = font;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _presentation = presentation;
        _randomNumberGenerator = randomNumberGenerator;
        _screenDirectory = screenDirectory;
        _shapeDrawer = shapeDrawer;
        _input = input;

        _keyFades = new FaderAnimation[16];
        _powerSupplyFader = new FaderAnimation(1, null, 100);
        _teleportingMessageFader = new FaderAnimation(6, null, 50);

        for (int a = 0; a < 16; a++)
        {
            _keyFades[a] = new FaderAnimation(4, _randomNumberGenerator);
        }

        _teleportCodes = new Dictionary<int, string>();
        _teleportCodes.Add(40, "groin");
        _teleportCodes.Add(31, "axies");
        _teleportCodes.Add(66, "tooth");
        _teleportCodes.Add(162, "handy");
        _teleportCodes.Add(150, "alien");
        _teleportCodes.Add(213, "pluto");
        _teleportCodes.Add(289, "ascii");
        _teleportCodes.Add(343, "orion");
        _teleportCodes.Add(457, "snowy");
        _teleportCodes.Add(433, "talon");
        _teleportCodes.Add(380, "rocky");
        _teleportCodes.Add(470, "uraiq");
        _teleportCodes.Add(499, "palma");
        _teleportCodes.Add(461, "aisle");
        _teleportCodes.Add(506, "exeon");
    }

    public void Reset()
    {
        _currentCode = EmptyCode;

        _keyPressed = false;
        _keyIdPressed = 0;
        _attemptingTeleport = false;
        _codeRecognised = false;
        _teleportCounter = 0;
        ScreenFade = ColourLevels.Full;

        _oldOffsetX = _presentation.ScreenXOffset;
        _oldOffsetY = _presentation.ScreenYOffset;
    }

    public override void Update(GameTime gameTime)
    {
        _resources.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        foreach (FaderAnimation keyFade in _keyFades)
        {
            keyFade.Update();
        }

        _powerSupplyFader.Update();

        if (!_keyPressed)
        {
            TypeAnyKeyPressed();
        }

        StartTeleportIfCodeComplete();

        if (_attemptingTeleport)
        {
            RunTeleportAttempt();
        }

        if (!_input.KeyboardState.IsKeyDown((Keys)_keyIdPressed))
        {
            _keyPressed = false;
        }

        _teleportCodes.TryGetValue(_gameScreens.ScreenNumber, out _teleportCode);
    }

    /// <summary>
    ///     A letter key goes into the first free slot of the code. The loop over 65 to 90 is A to Z as key
    ///     codes, and only one key registers per press because the caller latches _keyPressed.
    /// </summary>
    private void TypeAnyKeyPressed()
    {
        for (int a = 65; a < 91; a++)
        {
            if (_input.KeyboardState.IsKeyDown((Keys)a))
            {
                _resources.GetSoundFxEngine().PlaySound("TeleportKeyClick", 0.5f, false);

                _keyPressed = true;
                _keyIdPressed = a;

                for (int b = 0; b < 5; b++)
                {
                    if (_currentCode.Substring(b, 1) == "_")
                    {
                        if (b > 0)
                        {
                            _currentCode = _currentCode.Substring(0, b) + (char)a + _currentCode.Substring(b + 1, 5 - (b + 1));
                        }
                        else
                        {
                            _currentCode = (char)a + _currentCode.Substring(b + 1, 5 - (b + 1));
                        }

                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     An underscore is an empty slot, so a code with none left in it is ready to try.
    /// </summary>
    private void StartTeleportIfCodeComplete()
    {
        if (!_currentCode.Contains("_") && !_attemptingTeleport)
        {
            int destinationScreen = FindKey(_teleportCodes, _currentCode);
            if (destinationScreen > -1)
            {
                _codeRecognised = true;

                _gameScreens.ScreenNumber = destinationScreen;
                _resources.GetSoundFxEngine().PlaySound("Teleport", false);
            }
            else
            {
                _resources.GetSoundFxEngine().PlaySound("TeleportInvalidCode", 0.8f, false);
            }

            _attemptingTeleport = true;
        }
    }

    /// <summary>
    ///     Counts out the message, fades the screen if the code was good, and hands back to the main game
    ///     once the animation has run. An unrecognised code plays out the same way but does not move blob.
    /// </summary>
    private void RunTeleportAttempt()
    {
        if (_codeRecognised)
        {
            _teleportingMessageFader.Update(8);
        }
        else
        {
            _teleportingMessageFader.Update(20);
        }

        _teleportCounter++;

        // Only fade the screen out if the code is recognised
        if (_codeRecognised)
        {
            if (ScreenFade > 0)
            {
                ScreenFade = ScreenFade - 3;
            }
            if (ScreenFade < 0)
            {
                ScreenFade = 0;
            }
        }

        if (_teleportCounter > 58)
        {
            _presentation.ScreenXOffset = _oldOffsetX;
            _presentation.ScreenYOffset = _oldOffsetY;

            List<Point>? teleporterLocation = _gameScreens.GetCoordsOfTiles(397);
            if (teleporterLocation != null &&
                teleporterLocation.Count == 1)
            {
                _blob.BlobXPos = teleporterLocation[0].X + 20;
                _blob.BlobYPos = teleporterLocation[0].Y + 32;
            }

            _screenDirectory.MainGame.ScreenFade = 0;

            // only do this if the teleport was successful
            if (_codeRecognised && _currentCode != _previousCode.ToUpper())
            {
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.Teleporter, new ScreenChange.FadeIn());
                _gameScreens.UpdateCollisionMap(true);
            }
            _screenDirectory.Current = GameScreenType.MainGame;
        }
    }

    private static int FindKey(IDictionary<int, string> lookup, string value)
    {
        foreach (KeyValuePair<int, string> pair in lookup)
        {
            if (pair.Value == value.ToLower())
            {
                return pair.Key;
            }
        }

        return -1;
    }


    /// <summary>
    ///     Where each key of the teleporter's keypad sits on the screen, indexed by the same number as
    ///     its overlay sprite. Three rows with gaps in them, so a list rather than a grid.
    /// </summary>
    private static readonly Point[] KeyHighlightPositions =
    [
        new(778, 426),
        new(809, 426),
        new(838, 426),
        new(888, 426),
        new(920, 426),
        new(950, 426),
        new(766, 452),
        new(796, 452),
        new(826, 452),
        new(903, 452),
        new(934, 452),
        new(964, 452),
        new(780, 480),
        new(811, 480),
        new(919, 480),
        new(951, 480)
    ];

    public override void Draw(GameTime gameTime)
    {
        DrawInteriorLowerLayer();
        DrawTeleportPad();
        DrawConsoleText();
        DrawInteriorUpperLayer();
        DrawKeyHighlights();
        DrawPowerSupply();
    }

    private void DrawInteriorLowerLayer()
    {
        SpriteCollection teleportInteriorLower = _resources.GetSpriteCollection("TeleportInteriorLower");
        teleportInteriorLower.Begin(BlendState.AlphaBlend);
        teleportInteriorLower.DrawSprite(0, 0, 0, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));
        teleportInteriorLower.End();
    }

    private void DrawTeleportPad()
    {
        SpriteCollection lowerScreenTiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);
        lowerScreenTiles.Begin(BlendState.AlphaBlend);
        lowerScreenTiles.DrawSprite(397, 550, 96, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));

        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).End();

        SpriteCollection upperScreenTiles = _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles);
        upperScreenTiles.Begin(BlendState.AlphaBlend);
        upperScreenTiles.DrawSprite(7, 550, 96, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));
        upperScreenTiles.End();
    }

    private void DrawConsoleText()
    {
        if (!_attemptingTeleport)
        {
            _previousCode = _teleportCode ?? string.Empty;
        }

        _font.Begin(BlendState.AlphaBlend);

        _font.WriteText(100, 64, "YOU HAVE ENTERED", 2, Color.White, (byte)ScreenFade);
        _font.WriteText(244, 128, "TELEPORT", 2, Color.White, (byte)ScreenFade);
        _font.WriteText(164, 192, "CODE", 2, Color.White, (byte)ScreenFade);

        if (_teleportCode != null)
        {
            _font.WriteText(316, 189, _previousCode, 10, new Color(36, 16, 4), (byte)ScreenFade, 1.2f);
            _font.WriteText(320, 192, _previousCode, 10, new Color(ColourLevels.Full, 180, 0), (byte)ScreenFade);
        }

        _font.WriteText(228, 256, "ENTER TELEPORTAL", 2, Color.White, (byte)ScreenFade);
        _font.WriteText(228, 320, "DESTINATION CODE", 2, Color.White, (byte)ScreenFade);

        if (_attemptingTeleport)
        {
            DrawTeleportOutcome();
        }

        DrawEnteredCode();

        _font.End();
    }

    /// <summary>
    ///     The green or red message, with its blocks closing in from both sides. Drawn between the console
    ///     font's Begin and End, which is why it is not a Draw* of its own.
    /// </summary>
    private void DrawTeleportOutcome()
    {
        if (_codeRecognised)
        {
            // Teleporting text - recognised code
            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(87, 440, 40, 40), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
            _shapeDrawer.FillRectangle(new Rectangle(135, 442, 36, 36), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
            _shapeDrawer.FillRectangle(new Rectangle(179, 446, 28, 28), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
            _shapeDrawer.End();

            _font.WriteText(_presentation.ScreenXOffset + 88, _presentation.ScreenYOffset + 431, "NOW TELEPORTING", new Color(0, ColourLevels.Full - _teleportingMessageFader.FadeLevel, 0), ColourLevels.Full);

            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(556, 446, 28, 28), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
            _shapeDrawer.FillRectangle(new Rectangle(592, 442, 36, 36), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
            _shapeDrawer.FillRectangle(new Rectangle(636, 440, 40, 40), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
            _shapeDrawer.End();
        }
        else
        {
            // Teleporting text - unrecognised code
            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(100, 442, 36, 36), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
            _shapeDrawer.FillRectangle(new Rectangle(144, 446, 28, 28), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
            _shapeDrawer.End();

            _font.WriteText(_presentation.ScreenXOffset + 49, _presentation.ScreenYOffset + 431, "CODE NOT RECOGNISED", new Color(ColourLevels.Full - _teleportingMessageFader.FadeLevel, 0, 0), ColourLevels.Full);

            _shapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _shapeDrawer.FillRectangle(new Rectangle(590, 446, 28, 28), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
            _shapeDrawer.FillRectangle(new Rectangle(626, 442, 36, 36), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
            _shapeDrawer.End();
        }
    }

    private void DrawEnteredCode()
    {
        int teleportLetterLocation = 290;

        for (int a = 0; a < 5; a++)
        {
            string letter = _currentCode[a].ToString();

            _font.WriteText(teleportLetterLocation, 412, letter, 2, new Color(36, 16, 4), (byte)ScreenFade, new Vector2(1f, 0.5f), SpriteEffects.FlipVertically);

            _font.WriteText(teleportLetterLocation, 380, letter, 2, new Color(ColourLevels.Full, 180, 0), (byte)ScreenFade);
            teleportLetterLocation += 50;
        }
    }

    private void DrawInteriorUpperLayer()
    {
        SpriteCollection teleportInteriorUpper = _resources.GetSpriteCollection("TeleportInteriorUpper");
        teleportInteriorUpper.Begin(BlendState.AlphaBlend);
        teleportInteriorUpper.DrawSprite(0, 51, 42, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));
        teleportInteriorUpper.End();
    }

    /// <summary>
    ///     Lights up whichever keys the player is pressing. This was sixteen lines of the same call with a
    ///     different sprite number and position; the positions are now the table they always were.
    /// </summary>
    private void DrawKeyHighlights()
    {
        SpriteCollection overlays = _resources.GetSpriteCollection("TeleporterKeyOverlays");

        overlays.Begin(BlendState.Additive);

        for (int key = 0; key < KeyHighlightPositions.Length; key++)
        {
            overlays.DrawSprite(
                key,
                KeyHighlightPositions[key].X,
                KeyHighlightPositions[key].Y,
                new Color(
                    ColourLevels.Full,
                    ColourLevels.Full,
                    ColourLevels.Full,
                    (int)(_keyFades[key].FadeLevel * ((double)ScreenFade / ColourLevels.Full))));
        }

        overlays.End();
    }

    private void DrawPowerSupply()
    {
        SpriteCollection teleporterPowerSupply = _resources.GetSpriteCollection("TeleporterPowerSupply");
        teleporterPowerSupply.Begin(BlendState.AlphaBlend);
        teleporterPowerSupply.DrawSprite(0, 754, 6, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_powerSupplyFader.FadeLevel * ((double)ScreenFade / ColourLevels.Full))));
        teleporterPowerSupply.End();
    }

}
