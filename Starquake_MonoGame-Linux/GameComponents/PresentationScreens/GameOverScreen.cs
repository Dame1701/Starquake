using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.Fonts;
using StarquakeXNA.Helpers;
using StarquakeXNA.Scoring;

namespace StarquakeXNA.GameComponents
{
    public class GameOverScreen : GameScreenBase, IGameScreen
    {
        private SpriteCollection backgroundImage;
        private SpriteCollection foregroundImage;
        private bool fadeComplete;
        private TimeSpan playTime;

        public bool Success
        {
            get { return true; }
        }

        public GameOverScreen(GameEngine GameEngineReference)
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
            gameEngine.ContentResourceManager.GetSoundFXEngine().DisablePeriodicBackgroundSounds(true);
            IGameScreen mainGameScreen = gameEngine.GetGameScreen(enGameScreen.MainGame);
            playTime = ((MainGameScreen)mainGameScreen).GetPlayTime();

            fadeComplete = false;
            screenFade = 0;
            screenFadeSpeed = 5;
        }

        public override void LoadContent()
        {
            backgroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("GameOverScreenBackground");
            foregroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("GameOverScreenForeground");
        }

        public override void Update(GameTime gameTime)
        {
            if (ToggleKeys.GetPressedKey() != Keys.None)
            {
                HiScore playersScore = new HiScore()
                {
                    Score = gameEngine.BlobStatusPanel.PlayerScore,
                    AdventureScore = gameEngine.GameScreens.GetPercentageOfScreensVisited()
                };

                gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);
                gameEngine.CurrentGameScreen = enGameScreen.HiScores;
                gameEngine.GetGameScreen(enGameScreen.HiScores).PerformPostScreenChangeActions(enGameScreen.GameOver, playersScore);
            }
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            gameEngine.ScreenEffctEngine.SelectRenderViewPort("EntireScreen");
            backgroundImage.Begin(BlendState.AlphaBlend, gameTime);

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
            gameEngine.Font2.WriteTextCentred(1280, 180, "GAME OVER", 2, ColourHelper.GetXNA4Colour(230, 200, 0), (byte)screenFade);
            gameEngine.Font2.WriteText(490, 260, "SCORE", 2, Color.Gray, (byte)screenFade);
            gameEngine.Font2.WriteText(440, 320, "ADVENTURE SCORE", 2, new Color((byte)255, (byte)255, (byte)255, (byte)screenFade), (byte)screenFade);
            gameEngine.Font2.WriteText(470, 380, "TIME TAKEN", 2, new Color((byte)150, (byte)150, (byte)150, (byte)screenFade), (byte)screenFade);
            gameEngine.Font2.WriteText(400, 440, "CORE ELEMENTS", 2, new Color((byte)134, (byte)0, (byte)134, (byte)screenFade), (byte)screenFade);
            gameEngine.Font2.WriteText(460, 500, "REPLACED", 2, new Color((byte)134, (byte)0, (byte)134), (byte)screenFade);
            gameEngine.Font2.End();

            // Player Score
            ProportionalNumbers.Draw(630, 260, gameEngine.BlobStatusPanel.PlayerScore.ToString("000000"), gameEngine, gameTime, ColourHelper.GetXNA4Colour(150, 150, 150, (byte)screenFade));

            // Adventure Score
            ProportionalNumbers.Draw(814, 320, gameEngine.GameScreens.GetPercentageOfScreensVisited().ToString() + "%", gameEngine, gameTime, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));

            // Time Taken
            ProportionalNumbers.Draw(710, 380, playTime.Minutes.ToString() + "." + playTime.Seconds.ToString("00"), gameEngine, gameTime, ColourHelper.GetXNA4Colour(150, 150, 150, (byte)screenFade));

            // Elements replaced
            ProportionalNumbers.Draw(520, 560, gameEngine.Elements.GetElementsReplacedCount().ToString("00"), gameEngine, gameTime, ColourHelper.GetXNA4Colour(134, 0, 134, (byte)screenFade)); 
            
            // Core elements
            gameEngine.Elements.Draw(gameTime, 820, 446, screenFade);

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
