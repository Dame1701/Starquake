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

namespace StarquakeXNA.GameComponents
{
    public class GameCompleteScreen : GameScreenBase, IGameScreen
    {
        private SpriteCollection backgroundImage;
        private SpriteCollection foregroundImage;
        private bool fadeComplete;

        public bool Success
        {
            get { return true; }
        }

        public GameCompleteScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenFade = 0;
            screenFadeSpeed = 5;
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

        public override void LoadContent()
        {
            backgroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("GameCompleteScreenBackground");
            foregroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("GameCompleteScreenForeground");
        }

        public override void Update(GameTime gameTime)
        {
            if (ToggleKeys.GetPressedKey() != Keys.None)
            {
                gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);
                gameEngine.CurrentGameScreen = enGameScreen.GameOver;
                gameEngine.GetGameScreen(enGameScreen.GameOver).PerformPostScreenChangeActions(enGameScreen.GameComplete, null);
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

            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            gameEngine.Font2.WriteTextCentred(1280, 150, "THE CORES COMPLETE", 2, ColourHelper.GetXNA4Colour(230, 0, 0), (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 210, "BUT HOW ARE YOU GONNA", 2, Color.Gray, (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 270, "GET HOME WHEN ONLY A", 2, new Color((byte)255, (byte)255, (byte)255, (byte)screenFade), (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 330, "THTUPID LOONY WOULD WANDER", 2, new Color((byte)150, (byte)150, (byte)150, (byte)screenFade), (byte)screenFade);
            gameEngine.Font2.WriteTextCentred(1280, 390, "THIS FAR OUT IN THE GALAXY", 2, new Color((byte)255, (byte)255, (byte)255, (byte)screenFade), (byte)screenFade);
            gameEngine.Font2.End();

            // Core elements
            gameEngine.Elements.Draw(gameTime, 540, 450, screenFade);

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
