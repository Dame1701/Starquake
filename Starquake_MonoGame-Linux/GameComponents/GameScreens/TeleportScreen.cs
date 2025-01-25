using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using StarquakeXNA.Helpers;
using Starquake_XNA;

namespace StarquakeXNA.GameComponents
{
    public class TeleportScreen : GameComponentBase, IGameScreen
    {
        #region IGameComponent Members

        SpriteCollection teleportSprites;
        SpriteCollection teleportDistort;
        SpriteCollection teleportMonitorScreen;
        SpriteCollection teleportMonitorScreenDistortion;
        TeleportCodeLetter[] teleportCodeLetters;
        Dictionary<int, string> teleportCodes;
        int teleportMonitorScreenDistortionYPos;
        string teleportCode;
        string currentCode;
        string previousCode;
        bool keyPressed = false;
        int keyIDPressed = 0;
        bool attemptingTeleport = false;
        bool codeRecognised = false;
        int teleportCounter = 0;
        int oldOffsetX;
        int oldOffsetY;

        int screenFade;
        FaderAnimation[] keyFades;
        FaderAnimation powerSupplyFader;

        FaderAnimation teleportingMessageFader;

        public int ScreenFade
        {
            get { return screenFade; }
            set { screenFade = value; }
        }

        public bool Success
        {
            get { return true; }
        }

        public TeleportScreen (GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            keyFades = new FaderAnimation[16];
            powerSupplyFader = new FaderAnimation(1, null, 100);
            teleportingMessageFader = new FaderAnimation(6, null, 50);

            for (int a = 0; a < 16; a++)
            {
                keyFades[a] = new FaderAnimation(4, gameEngine.RandomNumberGenerator);
            }

            teleportCodes = new Dictionary<int, string>();
            teleportCodes.Add(40, "groin");
            teleportCodes.Add(31, "axies");
            teleportCodes.Add(66, "tooth");
            teleportCodes.Add(162, "handy");
            teleportCodes.Add(150, "alien");
            teleportCodes.Add(213, "pluto");
            teleportCodes.Add(289, "ascii");
            teleportCodes.Add(343, "orion");
            teleportCodes.Add(457, "snowy");
            teleportCodes.Add(433, "talon");
            teleportCodes.Add(380, "rocky");
            teleportCodes.Add(470, "uraiq");
            teleportCodes.Add(499, "palma");
            teleportCodes.Add(461, "aisle");
            teleportCodes.Add(506, "exeon");
        }

        public void Reset(object Data)
        {
            currentCode = "_____";

            keyPressed = false;
            keyIDPressed = 0;
            attemptingTeleport = false;
            codeRecognised = false;
            teleportCounter = 0;
            screenFade = 255;

            oldOffsetX = gameEngine.ScreenXOffset;
            oldOffsetY = gameEngine.ScreenYOffset;
        }

        public override void LoadContent()
        {
            teleportSprites = new SpriteCollection(gameEngine, gameEngine.ContentMgr, "Graphics\\TeleportEntry", 62, 62);
            teleportDistort = new SpriteCollection(gameEngine, gameEngine.ContentMgr, "Graphics\\TeleportDistort", 44, 29);
            teleportMonitorScreen = new SpriteCollection(gameEngine, gameEngine.ContentMgr, "Graphics\\TeleportInfoMonitor", 268, 210);
            teleportMonitorScreenDistortion = new SpriteCollection(gameEngine, gameEngine.ContentMgr, "Graphics\\TeleportInfoMonitorDistortion", 230, 30);

            teleportDistort.CurrentSpriteEffect = enSpriteEffect.Distortion;
            teleportMonitorScreenDistortion.CurrentSpriteEffect = enSpriteEffect.Distortion;

            teleportMonitorScreenDistortionYPos = gameEngine.ScreenYOffset + 28;
        }

        public override void Update(GameTime gameTime)
        {
            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("MachineryBackground", 0.5f, true);

            // Run Keyfader animation
            for (int a = 0; a < keyFades.Length; a++)
            {
                keyFades[a].Update();
            }

            powerSupplyFader.Update();

            // Grab key presses
            if (!keyPressed)
            {
                for (int a = 65; a < 91; a++)
                {
                    if (Keyboard.GetState().IsKeyDown((Keys)a))
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("TeleportKeyClick", 0.5f, false);

                        keyPressed = true;
                        keyIDPressed = a;

                        for (int b = 0; b < 5; b++)
                        {
                            if (currentCode.Substring(b, 1) == "_")
                            {
                                if (b > 0)
                                {
                                    currentCode = currentCode.Substring(0, b) + (char)a + currentCode.Substring(b + 1, 5 - (b + 1));
                                }
                                else
                                {
                                    currentCode = (char)a + currentCode.Substring(b + 1, 5 - (b + 1));
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // Code has been entered so try to teleport.....
            if (!currentCode.Contains("_") && !attemptingTeleport)
            {
                int DestinationScreen = FindKey(teleportCodes, currentCode);
                if (DestinationScreen > -1)
                {
                    codeRecognised = true;

                    gameEngine.GameScreens.ScreenNumber = DestinationScreen;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("Teleport", false);
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("TeleportInvalidCode", 0.8f, false);
                }

                attemptingTeleport = true;
            }

            if (attemptingTeleport)
            {
                if (codeRecognised)
                {
                    teleportingMessageFader.Update(8);
                }
                else
                {
                    teleportingMessageFader.Update(20);
                }
                
                teleportCounter++;

                // Only fade the screen out if the code is recognised
                if (codeRecognised)
                {
                    if (screenFade > 0)
                    {
                        screenFade = screenFade - 3;
                    }
                    if (screenFade < 0)
                    {
                        screenFade = 0;
                    }
                }

                if (teleportCounter > 58)
                {
                    gameEngine.ScreenXOffset = oldOffsetX;
                    gameEngine.ScreenYOffset = oldOffsetY;

                    List<Point> teleporterLocation = gameEngine.GameScreens.GetCoordsOfTiles(397);
                    if (teleporterLocation != null &&
                        teleporterLocation.Count == 1)
                    {
                        gameEngine.Player.BlobXPos = teleporterLocation[0].X + 20;
                        gameEngine.Player.BlobYPos = teleporterLocation[0].Y + 32;
                    }

                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 0;

                    // only do this if the teleport was successful
                    if (codeRecognised && currentCode != previousCode.ToUpper())
                    {
                        gameEngine.GetGameScreen(enGameScreen.MainGame).PerformPostScreenChangeActions(enGameScreen.Teleporter, null);
                        gameEngine.GameScreens.UpdateCollisionMap(true);
                    }
                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                }
            }

            if (!Keyboard.GetState().IsKeyDown((Keys)keyIDPressed))
            {
                keyPressed = false;
            }

            teleportCodes.TryGetValue(gameEngine.GameScreens.ScreenNumber, out teleportCode);

            if (teleportCodeLetters == null)
            {
                teleportCodeLetters = new TeleportCodeLetter[5];

                for (int a = 0; a < 5; a++)
                {
                    teleportCodeLetters[a] = new TeleportCodeLetter(currentCode[a].ToString(), new Point(350 + (a * 70), 400));
                }

            }
            else
            {
                for (int a = 0; a < 5; a++)
                {
                    teleportCodeLetters[a].Letter = currentCode[a].ToString();
                    teleportCodeLetters[a].Location = new Point(350 + (a * 70), 400);
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
            Color DrawColour = Color.White;

            Point CodeEntryLocation = new Point(gameEngine.ScreenXOffset + 100, gameEngine.ScreenYOffset + 230);

            // Draw Interior Lower layer
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").Begin(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").DrawSprite(0, 0, 0, new Color(255, 255, 255, ScreenFade));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorLower").End();


            // Draw teleport graphics
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).Begin(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).DrawSprite(397, 550, 96, new Color(255, 255, 255, ScreenFade));

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).End();

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).DrawSprite(7, 550, 96, new Color(255, 255, 255, ScreenFade));
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).End();

            // Draw text
            if (!attemptingTeleport)
            {
                previousCode = teleportCode;
            }

            gameEngine.Font2.Begin(BlendState.AlphaBlend);

            gameEngine.Font2.WriteText(100, 64, "YOU HAVE ENTERED", 2, Color.White, (byte)ScreenFade);
            gameEngine.Font2.WriteText(244, 128, "TELEPORT", 2, Color.White, (byte)ScreenFade);
            gameEngine.Font2.WriteText(164, 192, "CODE", 2, Color.White, (byte)ScreenFade);

            if (teleportCode != null)
            {
                gameEngine.Font2.WriteText(316, 189, previousCode, 10, new Color(36, 16, 4), (byte)ScreenFade, 1.2f);
                gameEngine.Font2.WriteText(320, 192, previousCode, 10, new Color(255, 180, 0), (byte)ScreenFade);
            }

            gameEngine.Font2.WriteText(228, 256, "ENTER TELEPORTAL", 2, Color.White, (byte)ScreenFade);
            gameEngine.Font2.WriteText(228, 320, "DESTINATION CODE", 2, Color.White, (byte)ScreenFade);


            if (attemptingTeleport)
            {
                if (codeRecognised)
                {
                    // Teleporting text - recognised code
                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(87, 440, 40, 40), new Color(0, teleportingMessageFader.FadeLevel , 0, 255));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(135, 442, 36, 36), new Color(0, teleportingMessageFader.FadeLevel, 0, 255));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(179, 446, 28, 28), new Color(0, teleportingMessageFader.FadeLevel, 0, 255));
                    gameEngine.ShapeDrawer.End();

                    gameEngine.Font2.WriteText(gameEngine.ScreenXOffset + 88, gameEngine.ScreenYOffset + 431, "NOW TELEPORTING", new Color(0, 255 - teleportingMessageFader.FadeLevel, 0), 255);

                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(556, 446, 28, 28), new Color(0, teleportingMessageFader.FadeLevel, 0, 255));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(592, 442, 36, 36), new Color(0, teleportingMessageFader.FadeLevel, 0, 255));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(636, 440, 40, 40), new Color(0, teleportingMessageFader.FadeLevel, 0, 255));
                    gameEngine.ShapeDrawer.End();
                }
                else
                {
                    // Teleporting text - unrecognised code
                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(100, 442, 36, 36), new Color(teleportingMessageFader.FadeLevel, 0, 0, 255));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(144, 446, 28, 28), new Color(teleportingMessageFader.FadeLevel, 0, 0, 255));
                    gameEngine.ShapeDrawer.End();

                    gameEngine.Font2.WriteText(gameEngine.ScreenXOffset + 49, gameEngine.ScreenYOffset + 431, "CODE NOT RECOGNISED", new Color(255 - teleportingMessageFader.FadeLevel, 0, 0), 255);

                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(590, 446, 28, 28), new Color(teleportingMessageFader.FadeLevel, 0, 0, 255));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(626, 442, 36, 36), new Color(teleportingMessageFader.FadeLevel, 0, 0, 255));
                    gameEngine.ShapeDrawer.End();
                }
            }

            // Draw codes
            if (teleportCodeLetters != null)
            {
                int TeleportLetterLocation = 290;
                
                for (int a = 0; a < 5; a++)
                {
                    gameEngine.Font2.WriteText(TeleportLetterLocation, 412, teleportCodeLetters[a].Letter, 2, new Color(36, 16, 4), (byte)ScreenFade, new Vector2(1f, 0.5f), SpriteEffects.FlipVertically);

                    gameEngine.Font2.WriteText(TeleportLetterLocation, 380, teleportCodeLetters[a].Letter, 2, new Color(255, 180, 0), (byte)ScreenFade);
                    TeleportLetterLocation += 50;
                }
            }

            gameEngine.Font2.End();


            // Draw upper interior layer
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").Begin(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").DrawSprite(0, 51, 42, new Color(255, 255, 255, ScreenFade));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleportInteriorUpper").End();

            // Draw key Highlights
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").Begin(Microsoft.Xna.Framework.Graphics.BlendState.Additive, gameTime);

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(0, 778, 426, new Color(255, 255, 255, (int)(keyFades[0].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(1, 809, 426, new Color(255, 255, 255, (int)(keyFades[1].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(2, 838, 426, new Color(255, 255, 255, (int)(keyFades[2].FadeLevel * ((double)ScreenFade / 255))));

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(3, 888, 426, new Color(255, 255, 255, (int)(keyFades[3].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(4, 920, 426, new Color(255, 255, 255, (int)(keyFades[4].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(5, 950, 426, new Color(255, 255, 255, (int)(keyFades[5].FadeLevel * ((double)ScreenFade / 255))));

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(6, 766, 452, new Color(255, 255, 255, (int)(keyFades[6].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(7, 796, 452, new Color(255, 255, 255, (int)(keyFades[7].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(8, 826, 452, new Color(255, 255, 255, (int)(keyFades[8].FadeLevel * ((double)ScreenFade / 255))));

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(9, 903, 452, new Color(255, 255, 255, (int)(keyFades[9].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(10, 934, 452, new Color(255, 255, 255, (int)(keyFades[10].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(11, 964, 452, new Color(255, 255, 255, (int)(keyFades[11].FadeLevel * ((double)ScreenFade / 255))));

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(12, 780, 480, new Color(255, 255, 255, (int)(keyFades[12].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(13, 811, 480, new Color(255, 255, 255, (int)(keyFades[13].FadeLevel * ((double)ScreenFade / 255))));

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(14, 919, 480, new Color(255, 255, 255, (int)(keyFades[14].FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").DrawSprite(15, 951, 480, new Color(255, 255, 255, (int)(keyFades[15].FadeLevel * ((double)ScreenFade / 255))));

            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterKeyOverlays").End();

            // Draw power supply
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").Begin(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").DrawSprite(0, 754, 6, new Color(255, 255, 255, (int)(powerSupplyFader.FadeLevel * ((double)ScreenFade / 255))));
            gameEngine.ContentResourceManager.GetSpriteCollection("TeleporterPowerSupply").End();

        }

        #endregion

        #region IGameScreen Members


        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class TeleportCodeLetter
    {
        string letter;
        public string Letter
        {
            get { return letter; }
            set { letter = value; }
        }

        Point location;
        public Point Location
        {
            get { return location; }
            set { location = value; }
        }

        public TeleportCodeLetter(string Letter, Point Location)
        {
            letter = Letter;
            location = Location;
        }
    }
}
