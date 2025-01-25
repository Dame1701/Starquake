using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;

namespace StarquakeXNA.GameComponents
{
    public class ComputerReportScreen : GameScreenBase, IGameScreen
    {
        #region IGameScreen Members

        private SpriteCollection backgroundImage;
        private SpriteCollection foregroundImage;
        private bool fadeComplete;

        public bool Success
        {
            get { return true; }
        }

        public ComputerReportScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenFade = 0;
            screenFadeSpeed = 5;
        }

        public override void LoadContent()
        {
            backgroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("ComputerReportScreenBackground");
            foregroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("ComputerReportScreenForeground");
        }

        public override void Update(GameTime gameTime)
        {
            if (ToggleKeys.GetPressedKey() != Keys.None)
            {
                gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);
                gameEngine.CurrentGameScreen = enGameScreen.MainGame;

                gameEngine.GetGameScreen(enGameScreen.MainGame).Reset(null);
                gameEngine.GetGameScreen(enGameScreen.MainGame).PerformPostScreenChangeActions(enGameScreen.ComputerReport, false);
            }

            if (screenFade >= 255)
            {
                if (fadeComplete == false)
                {
                    fadeComplete = true;
                }

                screenFade = gameEngine.RandomNumberGenerator.Next(255);
                screenFadeSpeed = gameEngine.RandomNumberGenerator.Next(20);

                if (screenFadeSpeed < 4)
                {
                    screenFadeSpeed = 4;
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
                backgroundImage.DrawSprite(0, 0, 0,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));
            }
            
            backgroundImage.End();

            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            gameEngine.Font2.WriteTextCentred(1280, 180, "Flight Computer Report",  2, ColourHelper.GetXNA4Colour(200, 200, 100), (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 260, "Touchdown Imminent Prepare", 2, Color.White, (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 320, "For Mission Starquake . . .", 2, Color.White, (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 380, "Crash . . . Bang . . . Smash . . .", 2, Color.Red, (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 440, "Touchdown", 2, Color.White, (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 500, "Comthuter Malthunction", 2, Color.Red, (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 560, "Malfunnythinkin . . .", 2, Color.Red, (byte)screenFade);
            gameEngine.Font2.End();

            foregroundImage.Begin(BlendState.AlphaBlend, gameTime);

            if (fadeComplete)
            {
                foregroundImage.DrawSprite(0, 230, 120, Color.White);
            }
            else
            {
                foregroundImage.DrawSprite(0, 230, 120,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));
            }

            foregroundImage.End();
        }



        public void Reset(object Data)
        {
            
        }

        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
            fadeComplete = false;
            screenFade = 0;
            screenFadeSpeed = 5;
        }

        #endregion
    }
}
