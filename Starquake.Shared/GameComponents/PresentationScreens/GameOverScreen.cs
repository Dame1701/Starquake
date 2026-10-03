// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

internal class GameOverScreen : GameScreenBase, IGameScreen
{
    /// <summary>
    ///     Where each of the four figures sits, against the caption to its left.
    /// </summary>
    private const int ScoreXPos = 630;

    /// <inheritdoc cref="ScoreXPos" />
    private const int ScoreYPos = 260;

    /// <inheritdoc cref="ScoreXPos" />
    private const int AdventureScoreXPos = 814;

    /// <inheritdoc cref="ScoreXPos" />
    private const int AdventureScoreYPos = 320;

    /// <inheritdoc cref="ScoreXPos" />
    private const int TimeTakenXPos = 710;

    /// <inheritdoc cref="ScoreXPos" />
    private const int TimeTakenYPos = 380;

    /// <inheritdoc cref="ScoreXPos" />
    private const int ElementsReplacedXPos = 520;

    /// <inheritdoc cref="ScoreXPos" />
    private const int ElementsReplacedYPos = 560;

    /// <summary>
    ///     The grey the score and the time are written in, dimmer than their captions so the figure
    ///     reads as the answer rather than competing with the label.
    /// </summary>
    private const int DimGrey = 150;

    /// <summary>
    ///     The purple the core element count is written in, matching its own caption.
    /// </summary>
    private const int CorePurpleRed = 134;

    /// <inheritdoc cref="CorePurpleRed" />
    private const int CorePurpleBlue = 134;

    private readonly SpriteCollection _backgroundImage;
    private readonly CoreElements _elements;
    private readonly SpriteCollection _foregroundImage;
    private readonly GameScreenCollection _gameScreens;
    private readonly PlayerStats _stats;
    private TimeSpan _playTime;

    public GameOverScreen(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        PlayerStats stats,
        CoreElements elements,
        GameScreenCollection gameScreens,
        ToggleKeys input)
        : base(
            resources,
            font,
            graphics,
            presentation,
            screenEffects,
            screenDirectory,
            input)
    {
        _stats = stats;
        _elements = elements;
        _gameScreens = gameScreens;

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection(Assets.Graphics.GameOverScreenBackground);
        _foregroundImage = Resources.GetSpriteCollection(Assets.Graphics.GameOverScreenForeground);
    }

    public override void Update(GameTime gameTime)
    {
        if (Input.GetPressedKey() != Keys.None)
        {
            var playersScore = new HiScore
            {
                Score = _stats.PlayerScore,
                AdventureScore = _gameScreens.GetPercentageOfScreensVisited()
            };

            GraphicsDeviceManager.GraphicsDevice.Clear(Presentation.ScreenClearColour);
            ScreenDirectory.Current = GameScreenType.HiScores;
            ScreenDirectory.HiScores.PerformPostScreenChangeActions(playersScore);
        }
        base.Update(gameTime);
    }

    public void Draw()
    {
        ScreenEffects.SelectRenderViewPort(ViewPorts.EntireScreen);
        _backgroundImage.Begin(BlendState.AlphaBlend);

        _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.WhiteFadedTo((byte)ScreenFade));

        _backgroundImage.End();

        Font.Begin(BlendState.AlphaBlend);
        Font.WriteTextCentred(1280, 180, "GAME OVER", 2, ColourHelper.GetXna4Colour(230, 200, 0), (byte)ScreenFade);
        Font.WriteText(490, 260, "SCORE", 2, Color.Gray, (byte)ScreenFade);
        Font.WriteText(440, 320, "ADVENTURE SCORE", 2, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ScreenFade), (byte)ScreenFade);
        Font.WriteText(470, 380, "TIME TAKEN", 2, new Color((byte)150, (byte)150, (byte)150, (byte)ScreenFade), (byte)ScreenFade);
        Font.WriteText(400, 440, "CORE ELEMENTS", 2, new Color((byte)134, (byte)0, (byte)134, (byte)ScreenFade), (byte)ScreenFade);
        Font.WriteText(460, 500, "REPLACED", 2, new Color(134, 0, 134), (byte)ScreenFade);
        Font.End();

        DrawPlayerScore();
        DrawAdventureScore();
        DrawTimeTaken();
        DrawElementsReplaced();

        DrawCoreElements();
    }

    public void PerformPostScreenChangeActions()
    {
        Resources.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);
        _playTime = ScreenDirectory.MainGame.GetPlayTime();

        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    /// <summary>
    ///     What the player scored, against the SCORE caption.
    /// </summary>
    private void DrawPlayerScore()
        => ProportionalNumbers.Draw(ScoreXPos, ScoreYPos, _stats.PlayerScore.ToString("000000"),
            Resources, ColourHelper.GetXna4Colour(DimGrey, DimGrey, DimGrey, (byte)ScreenFade));

    /// <summary>
    ///     How much of the planet was walked, as a percentage of its screens.
    /// </summary>
    private void DrawAdventureScore()
        => ProportionalNumbers.Draw(AdventureScoreXPos, AdventureScoreYPos,
            _gameScreens.GetPercentageOfScreensVisited() + "%",
            Resources, ColourHelper.WhiteFadedTo((byte)ScreenFade));

    /// <summary>
    ///     How long the game lasted, as minutes and seconds.
    /// </summary>
    private void DrawTimeTaken()
        => ProportionalNumbers.Draw(TimeTakenXPos, TimeTakenYPos,
            _playTime.Minutes + "." + _playTime.Seconds.ToString("00"),
            Resources, ColourHelper.GetXna4Colour(DimGrey, DimGrey, DimGrey, (byte)ScreenFade));

    /// <summary>
    ///     How many of the nine core elements were put back.
    /// </summary>
    private void DrawElementsReplaced()
        => ProportionalNumbers.Draw(ElementsReplacedXPos, ElementsReplacedYPos,
            _elements.GetElementsReplacedCount().ToString("00"),
            Resources, ColourHelper.GetXna4Colour(CorePurpleRed, 0, CorePurpleBlue, (byte)ScreenFade));

    private void DrawCoreElements()
    {
        _elements.Draw(820, 446, ScreenFade);
        _foregroundImage.Begin(BlendState.AlphaBlend);
        _foregroundImage.DrawSprite(0, 230, 120, ColourHelper.WhiteFadedTo((byte)ScreenFade));
        _foregroundImage.End();
    }
}
