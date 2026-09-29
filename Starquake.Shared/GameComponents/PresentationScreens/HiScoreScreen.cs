// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

public class HiScoreScreen : GameScreenBase, IGameScreen
{
    private readonly HiScores _hiscoreList;
    private SpriteCollection _backgroundImage;
    private bool _fadeComplete;
    private SpriteCollection _foregroundImage;
    private bool _gotHighScore;
    private HiScore _playersHighScore;

    public HiScoreScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
        _hiscoreList = new HiScores();
    }

    public bool Success => true;

    public void Reset(object data)
    {

    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {
        _fadeComplete = false;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
        _playersHighScore = null;

        // Lets see if we have a place in the high score table
        _gotHighScore = _hiscoreList.GotHighScore(GameEngine.BlobStatusPanel.PlayerScore);

        if (_gotHighScore)
        {
            _playersHighScore = (HiScore)param;
            _playersHighScore.Name = "";

            _hiscoreList.AddScoreToTable(_playersHighScore);

            // Reset the flag so we can display the table
            //gotHighScore = false;
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (!_gotHighScore)
        {
            if (ToggleKeys.GetPressedKey() != Keys.None)
            {
                GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.ScreenClearColour);
                GameEngine.CurrentGameScreen = GameScreenType.Options;
                GameEngine.GetGameScreen(GameScreenType.Options).PerformPostScreenChangeActions(GameScreenType.HiScores, null);
            }
        }
        else
        {
            if (ToggleKeys.GetPressedKey() != Keys.None)
            {
                var temp = ToggleKeys.GetPressedKey();

                var keyPressedString = ToggleKeys.GetPressedKey().ToString().ToUpper();
                var keyPressed = TextHelper.GetAsciiCode(keyPressedString);

                if (_playersHighScore.Name.Length < 6)
                {
                    if (keyPressed > 64 &&
                        keyPressed < 91)
                    {
                        _playersHighScore.Name = _playersHighScore.Name + keyPressedString;
                    }

                    if (ToggleKeys.GetPressedKey() == Keys.Space)
                    {
                        _playersHighScore.Name = _playersHighScore.Name + " ";
                    }
                }

                if (ToggleKeys.GetPressedKey() == Keys.Enter)
                {
                    _gotHighScore = false;
                }


                if (ToggleKeys.GetPressedKey() == Keys.Back)
                {
                    if (_playersHighScore.Name.Length > 0)
                    {
                        _playersHighScore.Name = _playersHighScore.Name.Substring(0, _playersHighScore.Name.Length - 1);
                    }
                }
            }
        }


        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GameEngine.ScreenEffectEngine.SelectRenderViewPort("EntireScreen");
        _backgroundImage.Begin(BlendState.AlphaBlend, gameTime);

        if (_fadeComplete)
        {
            _backgroundImage.DrawSprite(0, 0, 0, Color.White);
        }
        else
        {
            _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXna4Colour(255, 255, 255, (byte)ScreenFade));
        }

        _backgroundImage.End();

        // Draw the high score entry bit if the player has got a high score
        if (_gotHighScore)
        {

            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteTextCentred(1280, 180, "ENCODE YOUR NAME", 2, ColourHelper.GetXna4Colour(9, 132, 255), (byte)ScreenFade);
            GameEngine.Font2.WriteTextCentred(1280, 240, "INTO THE CORE", 2, ColourHelper.GetXna4Colour(255, 255, 255), (byte)ScreenFade);
            GameEngine.Font2.WriteTextCentred(1280, 300, "FOR YOU HAVE ACHIEVED", 2, ColourHelper.GetXna4Colour(255, 255, 255), (byte)ScreenFade);
            GameEngine.Font2.WriteTextCentred(1280, 360, "A HERO'S SCORE", 2, ColourHelper.GetXna4Colour(255, 255, 255), (byte)ScreenFade);

            GameEngine.Font2.WriteTextCentred(1280, 420, _playersHighScore.Name, 2, ColourHelper.GetXna4Colour(9, 132, 255), (byte)ScreenFade);
            GameEngine.Font2.End();
        }
        else
        {
            GameEngine.Font2.Begin(BlendState.AlphaBlend);
            GameEngine.Font2.WriteTextCentred(1280, 180, "CORE OF HEROES", 2, ColourHelper.GetXna4Colour(9, 132, 255), (byte)ScreenFade);
            GameEngine.Font2.End();

            _hiscoreList.Draw(GameEngine, gameTime, 380, 250, ScreenFade);
        }

        _foregroundImage.Begin(BlendState.AlphaBlend, gameTime);

        if (_fadeComplete)
        {
            _foregroundImage.DrawSprite(0, 230, 120, Color.White);
        }
        else
        {
            _foregroundImage.DrawSprite(0, 230, 120, ColourHelper.GetXna4Colour(255, 255, 255, (byte)ScreenFade));
        }

        _foregroundImage.End();
    }

    public override void LoadContent()
    {
        _backgroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("HiScoreScreenBackground");
        _foregroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("HiScoreScreenForeground");
    }
}