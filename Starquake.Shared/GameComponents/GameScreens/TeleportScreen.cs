// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.Input;
using Starquake.Sprites;
using Starquake.Structure;
using Starquake.World;

namespace Starquake.GameComponents.GameScreens;

internal class TeleportScreen : IGameScreen
{

    /// <summary>
    ///     The destination code as typed so far, one underscore per letter still to be entered.
    /// </summary>
    private const string EmptyCode = "_____";

    /// <summary>
    ///     How fast the screen darkens once the code is recognised, in colour levels per logic
    ///     frame. Slow enough that the booth is visibly left behind rather than cutting away.
    /// </summary>
    private const int ScreenFadeOutStep = 3;

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
    private readonly Blob _blob;
    private readonly BitmapFont _font;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ToggleKeys _input;

    private readonly Fader[] _keyFades;
    private readonly Fader _powerSupplyFader;
    private readonly PresentationSettings _presentation;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly Shapes _shapeDrawer;

    private readonly Fader _teleportingMessageFader;
    private bool _attemptingTeleport;
    private bool _codeRecognised;
    private string _currentCode = EmptyCode;
    private int _keyIdPressed;
    private bool _keyPressed;
    private int _oldOffsetX;
    private int _oldOffsetY;
    private string _previousCode = string.Empty;

    private string? _teleportCode;
    private int _teleportCounter;

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
        _screenDirectory = screenDirectory;
        _shapeDrawer = shapeDrawer;
        _input = input;

        _keyFades = new Fader[16];
        _powerSupplyFader = new Fader(1, null, 100);
        _teleportingMessageFader = new Fader(6, null, 50);

        for (int a = 0; a < 16; a++)
        {
            _keyFades[a] = new Fader(4, randomNumberGenerator);
        }
    }

    public int ScreenFade { get; set; }

    public void Update(GameTime gameTime)
    {
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.MachineryBackground, 0.5f, true);

        foreach (Fader keyFade in _keyFades)
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

        _teleportCode = _gameScreens.CurrentGameScreenData.TeleportCode;

        // The code of the booth Blob walked into, held across the teleport so RunTeleportAttempt can
        // tell an arrival on a new screen from one that went nowhere. Drawing used to latch this,
        // which left a logic decision depending on a frame having been drawn.
        if (!_attemptingTeleport)
        {
            _previousCode = _teleportCode ?? string.Empty;
        }
    }

    public void Draw()
    {
        DrawInteriorLowerLayer();
        DrawTeleportPad();
        DrawConsoleText();
        DrawInteriorUpperLayer();
        DrawKeyHighlights();
        DrawPowerSupply();
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
                _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.TeleportKeyClick, 0.5f, false);

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
            int destinationScreen = _gameScreens.FindScreenByTeleportCode(_currentCode);
            if (destinationScreen > -1)
            {
                _codeRecognised = true;

                _gameScreens.ScreenNumber = destinationScreen;
                _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.Teleport, false);
            }
            else
            {
                _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.TeleportInvalidCode, 0.8f, false);
            }

            _attemptingTeleport = true;
        }
    }

    /// <summary>
    ///     Counts out the message, fades the screen if the code was good, and hands back to the main game
    ///     once the animation has run. An unrecognised code plays out the same way but does not move Blob.
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

        // An unrecognised code leaves the picture up, because Blob is not going anywhere - there is
        // nothing to fade out to.
        if (_codeRecognised)
        {
            ScreenFade = Ramp.Down(ScreenFade, ScreenFadeOutStep, ColourLevels.None);
        }

        if (_teleportCounter > 58)
        {
            _presentation.ScreenXOffset = _oldOffsetX;
            _presentation.ScreenYOffset = _oldOffsetY;

            // A recognised code moved the screen the moment it was typed, so this puts Blob into a
            // booth on the screen it has arrived at. A code that was not recognised leaves it in the
            // booth it walked into, which is where it already is - moving it then would have carried
            // it across to some other booth on the same screen.
            if (_codeRecognised)
            {
                ArriveInABooth();
            }

            _screenDirectory.MainGame.ScreenFade = 0;

            // A code that merely names the booth Blob is already standing in is not a journey, so
            // the screen it arrived on is not told to set itself up again.
            if (_codeRecognised && _currentCode != _previousCode.ToUpper())
            {
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.Teleporter, new ScreenChange.FadeIn());
                _gameScreens.UpdateCollisionMap(true);
            }
            _screenDirectory.Current = GameScreenType.MainGame;
        }
    }

    /// <summary>
    ///     Steps Blob out of a booth on the screen he has just arrived at.
    /// </summary>
    /// <remarks>
    ///     A screen may have any number of booths on it and Blob arrives in the first, because a
    ///     code belongs to the screen it reaches rather than to any one booth standing on it. A screen
    ///     with none is left alone rather than guessed at: there is nowhere on it a booth would put
    ///     Blob, and dropping him into the scenery would be worse than leaving him where he was.
    /// </remarks>
    private void ArriveInABooth()
    {
        if (_gameScreens.GetCoordsOfTiles((int)ActionTiles.TeleportBooth) is not { Count: > 0 } booths)
        {
            return;
        }

        _blob.XPos = booths[0].X + BoothMetrics.BlobXOffset;
        _blob.YPos = booths[0].Y + BoothMetrics.BlobYOffset;
    }

    private void DrawInteriorLowerLayer()
    {
        SpriteCollection teleportInteriorLower = _resources.GetSpriteCollection(Assets.Graphics.TeleportInteriorLower);
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
        SpriteCollection teleportInteriorUpper = _resources.GetSpriteCollection(Assets.Graphics.TeleportInteriorUpper);
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
        SpriteCollection overlays = _resources.GetSpriteCollection(Assets.Graphics.TeleporterKeyOverlays);

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
        SpriteCollection teleporterPowerSupply = _resources.GetSpriteCollection(Assets.Graphics.TeleporterPowerSupply);
        teleporterPowerSupply.Begin(BlendState.AlphaBlend);
        teleporterPowerSupply.DrawSprite(0, 754, 6, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_powerSupplyFader.FadeLevel * ((double)ScreenFade / ColourLevels.Full))));
        teleporterPowerSupply.End();
    }
}
