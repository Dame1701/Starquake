// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
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

    private readonly SpriteCollection _backgroundImage;
    private readonly CoreElements _elements;
    private readonly SpriteCollection _foregroundImage;
    private readonly GameScreenCollection _gameScreens;
    private readonly StatusPanel _statusPanel;
    private bool _fadeComplete;
    private TimeSpan _playTime;

    public GameOverScreen(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        StatusPanel statusPanel,
        CoreElements elements,
        GameScreenCollection gameScreens)
        : base(
            resources,
            font,
            graphics,
            presentation,
            screenEffects,
            screenDirectory)
    {
        _statusPanel = statusPanel;
        _elements = elements;
        _gameScreens = gameScreens;

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection("GameOverScreenBackground");
        _foregroundImage = Resources.GetSpriteCollection("GameOverScreenForeground");
    }

    public bool Success => true;

    public void Reset(object? data)
    {

    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {
        Resources.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);
        IGameScreen mainGameScreen = ScreenDirectory.Get(GameScreenType.MainGame);
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
                Score = _statusPanel.PlayerScore,
                AdventureScore = _gameScreens.GetPercentageOfScreensVisited()
            };

            GraphicsDeviceManager.GraphicsDevice.Clear(Presentation.ScreenClearColour);
            ScreenDirectory.Current = GameScreenType.HiScores;
            ScreenDirectory.Get(GameScreenType.HiScores).PerformPostScreenChangeActions(GameScreenType.GameOver, playersScore);
        }
        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        ScreenEffects.SelectRenderViewPort("EntireScreen");
        _backgroundImage.Begin(BlendState.AlphaBlend);

        if (_fadeComplete)
        {
            _backgroundImage.DrawSprite(0, 0, 0, Color.White);
        }
        else
        {
            _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
        }

        _backgroundImage.End();

        Font.Begin(BlendState.AlphaBlend);
        Font.WriteTextCentred(1280, 180, "GAME OVER", 2, ColourHelper.GetXna4Colour(230, 200, 0), (byte)ScreenFade);
        Font.WriteText(490, 260, "SCORE", 2, Color.Gray, (byte)ScreenFade);
        Font.WriteText(440, 320, "ADVENTURE SCORE", 2, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ScreenFade), (byte)ScreenFade);
        Font.WriteText(470, 380, "TIME TAKEN", 2, new Color((byte)150, (byte)150, (byte)150, (byte)ScreenFade), (byte)ScreenFade);
        Font.WriteText(400, 440, "CORE ELEMENTS", 2, new Color((byte)134, (byte)0, (byte)134, (byte)ScreenFade), (byte)ScreenFade);
        Font.WriteText(460, 500, "REPLACED", 2, new Color(134, 0, 134), (byte)ScreenFade);
        Font.End();

        // Player Score
        ProportionalNumbers.Draw(630, 260, _statusPanel.PlayerScore.ToString("000000"), Resources, ColourHelper.GetXna4Colour(150, 150, 150, (byte)ScreenFade));

        // Adventure Score
        ProportionalNumbers.Draw(814, 320, _gameScreens.GetPercentageOfScreensVisited() + "%", Resources, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));

        // Time Taken
        ProportionalNumbers.Draw(710, 380, _playTime.Minutes + "." + _playTime.Seconds.ToString("00"), Resources, ColourHelper.GetXna4Colour(150, 150, 150, (byte)ScreenFade));

        // _elements replaced
        ProportionalNumbers.Draw(520, 560, _elements.GetElementsReplacedCount().ToString("00"), Resources, ColourHelper.GetXna4Colour(134, 0, 134, (byte)ScreenFade));

        // Core elements
        _elements.Draw(820, 446, ScreenFade);

        _foregroundImage.Begin(BlendState.AlphaBlend);

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
}
