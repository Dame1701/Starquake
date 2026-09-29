// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Fonts;
using Starquake.GameComponents.GameScreens;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

public class GameOverScreen : GameScreenBase, IGameScreen
{
    private SpriteCollection _backgroundImage;
    private bool _fadeComplete;
    private SpriteCollection _foregroundImage;
    private TimeSpan _playTime;

    public GameOverScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    public bool Success => true;

    public void Reset(object data)
    {

    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {
        GameEngine.ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);
        var mainGameScreen = GameEngine.GetGameScreen(GameScreenType.MainGame);
        _playTime = ((MainGameScreen)mainGameScreen).GetPlayTime();

        _fadeComplete = false;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    public override void Update(GameTime gameTime)
    {
        if (ToggleKeys.GetPressedKey() != Keys.None)
        {
            var playersScore = new HiScore
            {
                Score = GameEngine.BlobStatusPanel.PlayerScore,
                AdventureScore = GameEngine.GameScreens.GetPercentageOfScreensVisited()
            };

            GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.ScreenClearColour);
            GameEngine.CurrentGameScreen = GameScreenType.HiScores;
            GameEngine.GetGameScreen(GameScreenType.HiScores).PerformPostScreenChangeActions(GameScreenType.GameOver, playersScore);
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
            _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
        }

        _backgroundImage.End();

        GameEngine.Font2.Begin(BlendState.AlphaBlend);
        GameEngine.Font2.WriteTextCentred(1280, 180, "GAME OVER", 2, ColourHelper.GetXna4Colour(230, 200, 0), (byte)ScreenFade);
        GameEngine.Font2.WriteText(490, 260, "SCORE", 2, Color.Gray, (byte)ScreenFade);
        GameEngine.Font2.WriteText(440, 320, "ADVENTURE SCORE", 2, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ScreenFade), (byte)ScreenFade);
        GameEngine.Font2.WriteText(470, 380, "TIME TAKEN", 2, new Color((byte)150, (byte)150, (byte)150, (byte)ScreenFade), (byte)ScreenFade);
        GameEngine.Font2.WriteText(400, 440, "CORE ELEMENTS", 2, new Color((byte)134, (byte)0, (byte)134, (byte)ScreenFade), (byte)ScreenFade);
        GameEngine.Font2.WriteText(460, 500, "REPLACED", 2, new Color(134, 0, 134), (byte)ScreenFade);
        GameEngine.Font2.End();

        // Player Score
        ProportionalNumbers.Draw(630, 260, GameEngine.BlobStatusPanel.PlayerScore.ToString("000000"), GameEngine, gameTime, ColourHelper.GetXna4Colour(150, 150, 150, (byte)ScreenFade));

        // Adventure Score
        ProportionalNumbers.Draw(814, 320, GameEngine.GameScreens.GetPercentageOfScreensVisited() + "%", GameEngine, gameTime, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));

        // Time Taken
        ProportionalNumbers.Draw(710, 380, _playTime.Minutes + "." + _playTime.Seconds.ToString("00"), GameEngine, gameTime, ColourHelper.GetXna4Colour(150, 150, 150, (byte)ScreenFade));

        // Elements replaced
        ProportionalNumbers.Draw(520, 560, GameEngine.Elements.GetElementsReplacedCount().ToString("00"), GameEngine, gameTime, ColourHelper.GetXna4Colour(134, 0, 134, (byte)ScreenFade));

        // Core elements
        GameEngine.Elements.Draw(gameTime, 820, 446, ScreenFade);

        _foregroundImage.Begin(BlendState.AlphaBlend, gameTime);

        if (_fadeComplete)
        {
            _foregroundImage.DrawSprite(0, 230, 120, Color.White);
        }
        else
        {
            _foregroundImage.DrawSprite(0, 230, 120, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
        }

        _foregroundImage.End();
    }

    public override void LoadContent()
    {
        _backgroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("GameOverScreenBackground");
        _foregroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("GameOverScreenForeground");
    }
}