using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;
using StarquakeXNA.Fonts;
using StarquakeXNA.Scoring;
using StarquakeXNA.Structure;

namespace StarquakeXNA.GameComponents
{
    public class HiScoreScreen : GameScreenBase, IGameScreen
    {
        private SpriteCollection backgroundImage;
        private SpriteCollection foregroundImage;
        private bool fadeComplete;
        private HiScores hiscoreList;
        private bool gotHighScore = false;
        private HiScore playersHighScore;

        public bool Success
        {
            get { return true; }
        }

        public HiScoreScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenFade = 0;
            screenFadeSpeed = 5;
            hiscoreList = new HiScores();
        }

        public void Reset(object Data)
        {

        }

        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
            fadeComplete = false;
            screenFade = 0;
            screenFadeSpeed = 5;
            playersHighScore = null;

            // Lets see if we have a place in the high score table
            gotHighScore = hiscoreList.GotHighScore(gameEngine.BlobStatusPanel.PlayerScore);

            if (gotHighScore)
            {
                playersHighScore = (HiScore)Param;
                playersHighScore.Name = "";

                hiscoreList.AddScoreToTable(playersHighScore);

                // Reset the flag so we can display the table
                //gotHighScore = false;
            }
        }

        public override void LoadContent()
        {
            backgroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("HiScoreScreenBackground");
            foregroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("HiScoreScreenForeground");
        }

        public override void Update(GameTime gameTime)
        {
            if (!gotHighScore)
            {
                if (ToggleKeys.GetPressedKey() != Keys.None)
                {
                    gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);
                    gameEngine.CurrentGameScreen = enGameScreen.Options;
                    gameEngine.GetGameScreen(enGameScreen.Options).PerformPostScreenChangeActions(enGameScreen.HiScores, null);
                }
            }
            else
            {
                if (ToggleKeys.GetPressedKey() != Keys.None)
                {
                    Keys temp = ToggleKeys.GetPressedKey();

                    string keyPressedString = ToggleKeys.GetPressedKey().ToString().ToUpper();
                    int keyPressed = TextHelper.GetAsciiCode(keyPressedString);

                    if (playersHighScore.Name.Length < 6)
                    {
                        if (keyPressed > 64 &&
                            keyPressed < 91)
                        {
                            playersHighScore.Name = playersHighScore.Name + keyPressedString;
                        }

                        if (ToggleKeys.GetPressedKey() == Keys.Space)
                        {
                            playersHighScore.Name = playersHighScore.Name + " ";
                        }
                    }

                    if (ToggleKeys.GetPressedKey() == Keys.Enter)
                    {
                        gotHighScore = false;
                    }



                    if (ToggleKeys.GetPressedKey() == Keys.Back)
                    {
                        if (playersHighScore.Name.Length > 0)
                        {
                            playersHighScore.Name = playersHighScore.Name.Substring(0, playersHighScore.Name.Length - 1);
                        }
                    }
                }
            }


            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            gameEngine.ScreenEffctEngine.SelectRenderViewPort("EntireScreen");
            backgroundImage.Begin(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, gameTime);

            if (fadeComplete)
            {
                backgroundImage.DrawSprite(0, 0, 0, Color.White);
            }
            else
            {
                backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));
            }

            backgroundImage.End();

            // Draw the high score entry bit if the player has got a high score
            if (gotHighScore)
            {

                gameEngine.Font2.Begin(BlendState.AlphaBlend);
                gameEngine.Font2.WriteTextCentred(1280, 180, "ENCODE YOUR NAME", 2, ColourHelper.GetXNA4Colour(9, 132, 255), (byte)screenFade);
                gameEngine.Font2.WriteTextCentred(1280, 240, "INTO THE CORE", 2, ColourHelper.GetXNA4Colour(255, 255, 255), (byte)screenFade);
                gameEngine.Font2.WriteTextCentred(1280, 300, "FOR YOU HAVE ACHIEVED", 2, ColourHelper.GetXNA4Colour(255, 255, 255), (byte)screenFade);
                gameEngine.Font2.WriteTextCentred(1280, 360, "A HERO'S SCORE", 2, ColourHelper.GetXNA4Colour(255, 255, 255), (byte)screenFade);

                gameEngine.Font2.WriteTextCentred(1280, 420, playersHighScore.Name, 2, ColourHelper.GetXNA4Colour(9, 132, 255), (byte)screenFade);
                gameEngine.Font2.End();
            }
            else
            {
                gameEngine.Font2.Begin(BlendState.AlphaBlend);
                gameEngine.Font2.WriteTextCentred(1280, 180, "CORE OF HEROES", 2, ColourHelper.GetXNA4Colour(9, 132, 255), (byte)screenFade);
                gameEngine.Font2.End();

                hiscoreList.Draw(gameEngine, gameTime, 380, 250, screenFade);
            }

            foregroundImage.Begin(BlendState.AlphaBlend, gameTime);

            if (fadeComplete)
            {
                foregroundImage.DrawSprite(0, 230, 120, Color.White);
            }
            else
            {
                foregroundImage.DrawSprite(0, 230, 120, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));
            }

            foregroundImage.End();
        }
    }
}
