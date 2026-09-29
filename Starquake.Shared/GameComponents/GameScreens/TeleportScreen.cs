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
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class TeleportScreen : GameComponentBase, IGameScreen
{

    #region IGameScreen Members

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IGameComponent Members

    private SpriteCollection _teleportSprites;
    private SpriteCollection _teleportDistort;
    private SpriteCollection _teleportMonitorScreen;
    private SpriteCollection _teleportMonitorScreenDistortion;
    private TeleportCodeLetter[] _teleportCodeLetters;
    private readonly Dictionary<int, string> _teleportCodes;
    private int _teleportMonitorScreenDistortionYPos;
    private string _teleportCode;
    private string _currentCode;
    private string _previousCode;
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
        : base(gameEngineReference)
    {
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
    }

    public void Reset(object data)
    {
        _currentCode = "_____";

        _keyPressed = false;
        _keyIdPressed = 0;
        _attemptingTeleport = false;
        _codeRecognised = false;
        _teleportCounter = 0;
        ScreenFade = 255;

        _oldOffsetX = GameEngine.ScreenXOffset;
        _oldOffsetY = GameEngine.ScreenYOffset;
    }

    public override void LoadContent()
    {
        _teleportSprites = new SpriteCollection(GameEngine, GameEngine.ContentMgr, "Graphics\\TeleportEntry", 62, 62);
        _teleportDistort = new SpriteCollection(GameEngine, GameEngine.ContentMgr, "Graphics\\TeleportDistort", 44, 29);
        _teleportMonitorScreen = new SpriteCollection(GameEngine, GameEngine.ContentMgr, "Graphics\\TeleportInfoMonitor", 268, 210);
        _teleportMonitorScreenDistortion = new SpriteCollection(GameEngine, GameEngine.ContentMgr, "Graphics\\TeleportInfoMonitorDistortion", 230, 30);

        _teleportMonitorScreenDistortionYPos = GameEngine.ScreenYOffset + 28;
    }

    public override void Update(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("MachineryBackground", 0.5f, true);

        // Run Keyfader animation
        for (var a = 0; a < _keyFades.Length; a++)
        {
            _keyFades[a].Update();
        }

        _powerSupplyFader.Update();

        // Grab key presses
        if (!_keyPressed)
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

        // Code has been entered so try to teleport.....
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

        if (_attemptingTeleport)
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
                GameEngine.ScreenXOffset = _oldOffsetX;
                GameEngine.ScreenYOffset = _oldOffsetY;

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

        if (!Keyboard.GetState().IsKeyDown((Keys)_keyIdPressed))
        {
            _keyPressed = false;
        }

        _teleportCodes.TryGetValue(GameEngine.GameScreens.ScreenNumber, out _teleportCode);

        if (_teleportCodeLetters == null)
        {
            _teleportCodeLetters = new TeleportCodeLetter[5];

            for (var a = 0; a < 5; a++)
            {
                _teleportCodeLetters[a] = new TeleportCodeLetter(_currentCode[a].ToString(), new Point(350 + a * 70, 400));
            }

        }
        else
        {
            for (var a = 0; a < 5; a++)
            {
                _teleportCodeLetters[a].Letter = _currentCode[a].ToString();
                _teleportCodeLetters[a].Location = new Point(350 + a * 70, 400);
            }
        }
    }

    public static int FindKey(IDictionary<int, string> lookup, string value)
    {
        foreach (var pair in lookup)
        {
            if (pair.Value == value.ToLower())
                return pair.Key;
        }

        return -1;
    }


    public override void Draw(GameTime gameTime)
    {
        var drawColour = Color.White;

        var codeEntryLocation = new Point(GameEngine.ScreenXOffset + 100, GameEngine.ScreenYOffset + 230);

        // Draw Interior Lower layer
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").DrawSprite(0, 0, 0, new Color(255, 255, 255, ScreenFade));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").End();


        // Draw teleport graphics
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).DrawSprite(397, 550, 96, new Color(255, 255, 255, ScreenFade));

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).End();

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).DrawSprite(7, 550, 96, new Color(255, 255, 255, ScreenFade));
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).End();

        // Draw text
        if (!_attemptingTeleport)
        {
            _previousCode = _teleportCode;
        }

        GameEngine.Font2.Begin(BlendState.AlphaBlend);

        GameEngine.Font2.WriteText(100, 64, "YOU HAVE ENTERED", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteText(244, 128, "TELEPORT", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteText(164, 192, "CODE", 2, Color.White, (byte)ScreenFade);

        if (_teleportCode != null)
        {
            GameEngine.Font2.WriteText(316, 189, _previousCode, 10, new Color(36, 16, 4), (byte)ScreenFade, 1.2f);
            GameEngine.Font2.WriteText(320, 192, _previousCode, 10, new Color(255, 180, 0), (byte)ScreenFade);
        }

        GameEngine.Font2.WriteText(228, 256, "ENTER TELEPORTAL", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteText(228, 320, "DESTINATION CODE", 2, Color.White, (byte)ScreenFade);


        if (_attemptingTeleport)
        {
            if (_codeRecognised)
            {
                // Teleporting text - recognised code
                GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(87, 440, 40, 40), new Color(0, _teleportingMessageFader.FadeLevel, 0, 255));
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(135, 442, 36, 36), new Color(0, _teleportingMessageFader.FadeLevel, 0, 255));
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(179, 446, 28, 28), new Color(0, _teleportingMessageFader.FadeLevel, 0, 255));
                GameEngine.ShapeDrawer.End();

                GameEngine.Font2.WriteText(GameEngine.ScreenXOffset + 88, GameEngine.ScreenYOffset + 431, "NOW TELEPORTING", new Color(0, 255 - _teleportingMessageFader.FadeLevel, 0), 255);

                GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(556, 446, 28, 28), new Color(0, _teleportingMessageFader.FadeLevel, 0, 255));
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(592, 442, 36, 36), new Color(0, _teleportingMessageFader.FadeLevel, 0, 255));
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(636, 440, 40, 40), new Color(0, _teleportingMessageFader.FadeLevel, 0, 255));
                GameEngine.ShapeDrawer.End();
            }
            else
            {
                // Teleporting text - unrecognised code
                GameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(100, 442, 36, 36), new Color(_teleportingMessageFader.FadeLevel, 0, 0, 255));
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(144, 446, 28, 28), new Color(_teleportingMessageFader.FadeLevel, 0, 0, 255));
                GameEngine.ShapeDrawer.End();

                GameEngine.Font2.WriteText(GameEngine.ScreenXOffset + 49, GameEngine.ScreenYOffset + 431, "CODE NOT RECOGNISED", new Color(255 - _teleportingMessageFader.FadeLevel, 0, 0), 255);

                GameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(590, 446, 28, 28), new Color(_teleportingMessageFader.FadeLevel, 0, 0, 255));
                GameEngine.ShapeDrawer.FillRectangle(new Rectangle(626, 442, 36, 36), new Color(_teleportingMessageFader.FadeLevel, 0, 0, 255));
                GameEngine.ShapeDrawer.End();
            }
        }

        // Draw codes
        if (_teleportCodeLetters != null)
        {
            var teleportLetterLocation = 290;

            for (var a = 0; a < 5; a++)
            {
                GameEngine.Font2.WriteText(teleportLetterLocation, 412, _teleportCodeLetters[a].Letter, 2, new Color(36, 16, 4), (byte)ScreenFade, new Vector2(1f, 0.5f), SpriteEffects.FlipVertically);

                GameEngine.Font2.WriteText(teleportLetterLocation, 380, _teleportCodeLetters[a].Letter, 2, new Color(255, 180, 0), (byte)ScreenFade);
                teleportLetterLocation += 50;
            }
        }

        GameEngine.Font2.End();


        // Draw upper interior layer
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").DrawSprite(0, 51, 42, new Color(255, 255, 255, ScreenFade));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").End();

        // Draw key Highlights
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").Begin(BlendState.Additive, gameTime);

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(0, 778, 426, new Color(255, 255, 255, (int)(_keyFades[0].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(1, 809, 426, new Color(255, 255, 255, (int)(_keyFades[1].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(2, 838, 426, new Color(255, 255, 255, (int)(_keyFades[2].FadeLevel * ((double)ScreenFade / 255))));

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(3, 888, 426, new Color(255, 255, 255, (int)(_keyFades[3].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(4, 920, 426, new Color(255, 255, 255, (int)(_keyFades[4].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(5, 950, 426, new Color(255, 255, 255, (int)(_keyFades[5].FadeLevel * ((double)ScreenFade / 255))));

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(6, 766, 452, new Color(255, 255, 255, (int)(_keyFades[6].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(7, 796, 452, new Color(255, 255, 255, (int)(_keyFades[7].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(8, 826, 452, new Color(255, 255, 255, (int)(_keyFades[8].FadeLevel * ((double)ScreenFade / 255))));

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(9, 903, 452, new Color(255, 255, 255, (int)(_keyFades[9].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(10, 934, 452, new Color(255, 255, 255, (int)(_keyFades[10].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(11, 964, 452, new Color(255, 255, 255, (int)(_keyFades[11].FadeLevel * ((double)ScreenFade / 255))));

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(12, 780, 480, new Color(255, 255, 255, (int)(_keyFades[12].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(13, 811, 480, new Color(255, 255, 255, (int)(_keyFades[13].FadeLevel * ((double)ScreenFade / 255))));

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(14, 919, 480, new Color(255, 255, 255, (int)(_keyFades[14].FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(15, 951, 480, new Color(255, 255, 255, (int)(_keyFades[15].FadeLevel * ((double)ScreenFade / 255))));

        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").End();

        // Draw power supply
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").DrawSprite(0, 754, 6, new Color(255, 255, 255, (int)(_powerSupplyFader.FadeLevel * ((double)ScreenFade / 255))));
        GameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").End();

    }

    #endregion

}

public class TeleportCodeLetter
{

    public TeleportCodeLetter(string letter, Point location)
    {
        this.Letter = letter;
        this.Location = location;
    }

    public string Letter { get; set; }

    public Point Location { get; set; }
}