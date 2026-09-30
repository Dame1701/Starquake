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
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class TeleportScreen : GameComponentBase, IGameScreen
{
    private readonly GameEngine GameEngine;

    #region IGameScreen Members

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IGameComponent Members

    private readonly SpriteCollection _teleportSprites;
    private readonly SpriteCollection _teleportDistort;
    private readonly SpriteCollection _teleportMonitorScreen;
    private readonly SpriteCollection _teleportMonitorScreenDistortion;
    private readonly TeleportCodeLetter[] _teleportCodeLetters;
    private readonly Dictionary<int, string> _teleportCodes;
    /// <summary>
    ///     The destination code as typed so far, one underscore per letter still to be entered.
    /// </summary>
    private const string EmptyCode = "_____";

    private int _teleportMonitorScreenDistortionYPos;
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

    public bool Success => true;

    public TeleportScreen(GameEngine gameEngineReference)
    {
        GameEngine = gameEngineReference;

        _keyFades = new FaderAnimation[16];
        _powerSupplyFader = new FaderAnimation(1, null, 100);
        _teleportingMessageFader = new FaderAnimation(6, null, 50);

        for (var a = 0; a < 16; a++)
        {
            _keyFades[a] = new FaderAnimation(4, GameEngine.RandomNumberGenerator);
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

        _teleportSprites = new SpriteCollection(GameEngine.GraphicsDeviceMgr, GameEngine.ContentMgr, "Graphics\\TeleportEntry", 62, 62);
        _teleportDistort = new SpriteCollection(GameEngine.GraphicsDeviceMgr, GameEngine.ContentMgr, "Graphics\\TeleportDistort", 44, 29);
        _teleportMonitorScreen = new SpriteCollection(GameEngine.GraphicsDeviceMgr, GameEngine.ContentMgr, "Graphics\\TeleportInfoMonitor", 268, 210);
        _teleportMonitorScreenDistortion = new SpriteCollection(GameEngine.GraphicsDeviceMgr, GameEngine.ContentMgr, "Graphics\\TeleportInfoMonitorDistortion", 230, 30);

        _teleportMonitorScreenDistortionYPos = GameEngine.Presentation.ScreenYOffset + 28;

        _teleportCodeLetters = new TeleportCodeLetter[5];
        for (var a = 0; a < 5; a++)
        {
            _teleportCodeLetters[a] = new TeleportCodeLetter(_currentCode[a].ToString(), new Point(350 + a * 70, 400));
        }
    }

    public void Reset(object? data)
    {
        _currentCode = EmptyCode;

        _keyPressed = false;
        _keyIdPressed = 0;
        _attemptingTeleport = false;
        _codeRecognised = false;
        _teleportCounter = 0;
        ScreenFade = ColourLevels.Full;

        _oldOffsetX = GameEngine.Presentation.ScreenXOffset;
        _oldOffsetY = GameEngine.Presentation.ScreenYOffset;
    }

    public override void Update(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        foreach (var keyFade in _keyFades)
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

        if (!Keyboard.GetState().IsKeyDown((Keys)_keyIdPressed))
        {
            _keyPressed = false;
        }

        _teleportCodes.TryGetValue(GameEngine.GameScreens.ScreenNumber, out _teleportCode);

        for (var a = 0; a < 5; a++)
        {
            _teleportCodeLetters[a].Letter = _currentCode[a].ToString();
            _teleportCodeLetters[a].Location = new Point(350 + a * 70, 400);
        }
    }

    /// <summary>
    ///     A letter key goes into the first free slot of the code. The loop over 65 to 90 is A to Z as key
    ///     codes, and only one key registers per press because the caller latches _keyPressed.
    /// </summary>
    private void TypeAnyKeyPressed()
    {
        for (var a = 65; a < 91; a++)
        {
            if (Keyboard.GetState().IsKeyDown((Keys)a))
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("TeleportKeyClick", 0.5f, false);

                _keyPressed = true;
                _keyIdPressed = a;

                for (var b = 0; b < 5; b++)
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
            var destinationScreen = FindKey(_teleportCodes, _currentCode);
            if (destinationScreen > -1)
            {
                _codeRecognised = true;

                GameEngine.GameScreens.ScreenNumber = destinationScreen;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("Teleport", false);
            }
            else
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("TeleportInvalidCode", 0.8f, false);
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
            GameEngine.Presentation.ScreenXOffset = _oldOffsetX;
            GameEngine.Presentation.ScreenYOffset = _oldOffsetY;

            var teleporterLocation = GameEngine.GameScreens.GetCoordsOfTiles(397);
            if (teleporterLocation != null &&
                teleporterLocation.Count == 1)
            {
                GameEngine.Player.BlobXPos = teleporterLocation[0].X + 20;
                GameEngine.Player.BlobYPos = teleporterLocation[0].Y + 32;
            }

            GameEngine.GetGameScreen(GameScreenType.MainGame).ScreenFade = 0;

            // only do this if the teleport was successful
            if (_codeRecognised && _currentCode != _previousCode.ToUpper())
            {
                GameEngine.GetGameScreen(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.Teleporter, null);
                GameEngine.GameScreens.UpdateCollisionMap(true);
            }
            GameEngine.CurrentGameScreen = GameScreenType.MainGame;
        }
    }

    private static int FindKey(IDictionary<int, string> lookup, string value)
    {
        foreach (var pair in lookup)
        {
            if (pair.Value == value.ToLower())
                return pair.Key;
        }

        return -1;
    }


    /// <summary>
    ///     Where each key of the teleporter's keypad sits on the screen, indexed by the same number as
    ///     its overlay sprite. Three rows with gaps in them, so a list rather than a grid.
    /// </summary>
    private static readonly Point[] KeyHighlightPositions =
    [
        new Point(778, 426),
        new Point(809, 426),
        new Point(838, 426),
        new Point(888, 426),
        new Point(920, 426),
        new Point(950, 426),
        new Point(766, 452),
        new Point(796, 452),
        new Point(826, 452),
        new Point(903, 452),
        new Point(934, 452),
        new Point(964, 452),
        new Point(780, 480),
        new Point(811, 480),
        new Point(919, 480),
        new Point(951, 480)
    ];

    public override void Draw(GameTime gameTime)
    {
        DrawInteriorLowerLayer(gameTime);
        DrawTeleportPad(gameTime);
        DrawConsoleText();
        DrawInteriorUpperLayer(gameTime);
        DrawKeyHighlights(gameTime);
        DrawPowerSupply(gameTime);
    }

    private void DrawInteriorLowerLayer(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").DrawSprite(0, 0, 0, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").End();
    }

    private void DrawTeleportPad(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.LowerScreenTiles).Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.LowerScreenTiles).DrawSprite(397, 550, 96, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.LowerScreenTiles).End();

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.UpperScreenTiles).Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.UpperScreenTiles).DrawSprite(7, 550, 96, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.UpperScreenTiles).End();
    }

    private void DrawConsoleText()
    {
        if (!_attemptingTeleport)
        {
            _previousCode = _teleportCode ?? string.Empty;
        }

        GameEngine.Font2.Begin(BlendState.AlphaBlend);

        GameEngine.Font2.WriteText(100, 64, "YOU HAVE ENTERED", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteText(244, 128, "TELEPORT", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteText(164, 192, "CODE", 2, Color.White, (byte)ScreenFade);

        if (_teleportCode != null)
        {
            GameEngine.Font2.WriteText(316, 189, _previousCode, 10, new Color(36, 16, 4), (byte)ScreenFade, 1.2f);
            GameEngine.Font2.WriteText(320, 192, _previousCode, 10, new Color(ColourLevels.Full, 180, 0), (byte)ScreenFade);
        }

        GameEngine.Font2.WriteText(228, 256, "ENTER TELEPORTAL", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteText(228, 320, "DESTINATION CODE", 2, Color.White, (byte)ScreenFade);

        if (_attemptingTeleport)
        {
            DrawTeleportOutcome();
        }

        DrawEnteredCode();

        GameEngine.Font2.End();
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
        GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(87, 440, 40, 40), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(135, 442, 36, 36), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(179, 446, 28, 28), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.End();

        GameEngine.Font2.WriteText(GameEngine.Presentation.ScreenXOffset + 88, GameEngine.Presentation.ScreenYOffset + 431, "NOW TELEPORTING", new Color(0, ColourLevels.Full - _teleportingMessageFader.FadeLevel, 0), ColourLevels.Full);

        GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(556, 446, 28, 28), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(592, 442, 36, 36), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(636, 440, 40, 40), new Color(0, _teleportingMessageFader.FadeLevel, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.End();
    }
    else
    {
        // Teleporting text - unrecognised code
        GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(100, 442, 36, 36), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(144, 446, 28, 28), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.End();

        GameEngine.Font2.WriteText(GameEngine.Presentation.ScreenXOffset + 49, GameEngine.Presentation.ScreenYOffset + 431, "CODE NOT RECOGNISED", new Color(ColourLevels.Full - _teleportingMessageFader.FadeLevel, 0, 0), ColourLevels.Full);

        GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(590, 446, 28, 28), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.FillRectangle(new Rectangle(626, 442, 36, 36), new Color(_teleportingMessageFader.FadeLevel, 0, 0, ColourLevels.Full));
        GameEngine.ShapeDrawer.End();
    }
    }

    private void DrawEnteredCode()
    {
        var teleportLetterLocation = 290;

        for (var a = 0; a < 5; a++)
        {
            GameEngine.Font2.WriteText(teleportLetterLocation, 412, _teleportCodeLetters[a].Letter, 2, new Color(36, 16, 4), (byte)ScreenFade, new Vector2(1f, 0.5f), SpriteEffects.FlipVertically);

            GameEngine.Font2.WriteText(teleportLetterLocation, 380, _teleportCodeLetters[a].Letter, 2, new Color(ColourLevels.Full, 180, 0), (byte)ScreenFade);
            teleportLetterLocation += 50;
        }
    }

    private void DrawInteriorUpperLayer(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").DrawSprite(0, 51, 42, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ScreenFade));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").End();
    }

    /// <summary>
    ///     Lights up whichever keys the player is pressing. This was sixteen lines of the same call with a
    ///     different sprite number and position; the positions are now the table they always were.
    /// </summary>
    private void DrawKeyHighlights(GameTime gameTime)
    {
        var overlays = GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays");

        overlays.Begin(BlendState.Additive, gameTime);

        for (var key = 0; key < KeyHighlightPositions.Length; key++)
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

    private void DrawPowerSupply(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").DrawSprite(0, 754, 6, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (int)(_powerSupplyFader.FadeLevel * ((double)ScreenFade / ColourLevels.Full))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").End();
    }

    #endregion

}