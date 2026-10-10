// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Sprites;

namespace Starquake.GameComponents.PresentationScreens;

internal class HiScoreScreen : GameScreenBase, IGameScreen
{
    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;

    private readonly HiScores _hiScoreList;
    private readonly PlayerStats _stats;
    private bool _gotHighScore;
    private HiScore _playersHighScore = new();

    public HiScoreScreen(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        PlayerStats stats,
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

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection(Assets.Graphics.HiScoreScreenBackground);
        _foregroundImage = Resources.GetSpriteCollection(Assets.Graphics.HiScoreScreenForeground);
        _hiScoreList = new HiScores(new HiScoreStore());
    }

    public override void Update(GameTime gameTime)
    {
        if (!_gotHighScore)
        {
            if (Input.AnyKeyPressed)
            {
                GraphicsDeviceManager.GraphicsDevice.Clear(Presentation.ScreenClearColour);
                ScreenDirectory.Current = GameScreenType.Options;
                ScreenDirectory.Options.PerformPostScreenChangeActions();
            }
        }
        else
        {
            if (Input.GetPressedKey() != Keys.None)
            {
                string keyPressedString = Input.GetPressedKey().ToString().ToUpper();
                int keyPressed = AsciiCodes.GetAsciiCode(keyPressedString);

                if (_playersHighScore.Name.Length < 6)
                {
                    if (keyPressed is > 64 and < 91)
                    {
                        _playersHighScore.Name += keyPressedString;
                    }

                    if (Input.GetPressedKey() == Keys.Space)
                    {
                        _playersHighScore.Name += " ";
                    }
                }

                if (Input.GetPressedKey() == Keys.Enter)
                {
                    // The entry has been in the table since PerformPostScreenChangeActions and the
                    // letters typed into it in place, so this is the first moment there is a finished
                    // table worth keeping.
                    _gotHighScore = false;
                    _hiScoreList.Save();
                }


                if (Input.GetPressedKey() == Keys.Back)
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

    public void Draw()
    {
        ScreenEffects.SelectRenderViewPort(ViewPorts.EntireScreen);
        _backgroundImage.Begin(BlendState.AlphaBlend);

        _backgroundImage.DrawSprite(0, 0, 0, Colours.WhiteFadedTo((byte)ScreenFade));

        _backgroundImage.End();

        if (_gotHighScore)
        {
            Font.Begin(BlendState.AlphaBlend);
            Font.WriteTextCentred(1280, 180, "ENCODE YOUR NAME", 2, Colours.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 240, "INTO THE CORE", 2, Colours.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 300, "FOR YOU HAVE ACHIEVED", 2, Colours.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 360, "A HERO'S SCORE", 2, Colours.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);

            Font.WriteTextCentred(1280, 420, _playersHighScore.Name, 2, Colours.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.End();
        }
        else
        {
            Font.Begin(BlendState.AlphaBlend);
            Font.WriteTextCentred(1280, 180, "CORE OF HEROES", 2, Colours.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.End();

            _hiScoreList.Draw(Resources, Font, 380, 250, ScreenFade);
        }

        _foregroundImage.Begin(BlendState.AlphaBlend);

        _foregroundImage.DrawSprite(0, 230, 120, Colours.WhiteFadedTo((byte)ScreenFade));

        _foregroundImage.End();
    }

    /// <summary>
    ///     Called once this screen has been handed control, with the score it needs. Typed, so there
    ///     is no case to reject: the game over screen is the only way onto this one and always has a
    ///     score to pass.
    /// </summary>
    /// <param name="playersScore">
    ///     The score the player has just finished with, for a name to be typed against it.
    /// </param>
    public void PerformPostScreenChangeActions(HiScore playersScore)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
        _playersHighScore = new HiScore();

        _gotHighScore = _hiScoreList.GotHighScore(_stats.PlayerScore);

        if (_gotHighScore)
        {
            _playersHighScore = playersScore;
            _playersHighScore.Name = string.Empty;

            _hiScoreList.AddScoreToTable(_playersHighScore);
        }
    }
}
