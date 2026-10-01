// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

internal class HiScoreScreen : GameScreenBase, IGameScreen
{
    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;

    private readonly HiScores _hiscoreList;
    private readonly PlayerStats _stats;
    private bool _fadeComplete;
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

        _backgroundImage = Resources.GetSpriteCollection("HiScoreScreenBackground");
        _foregroundImage = Resources.GetSpriteCollection("HiScoreScreenForeground");
        _hiscoreList = new HiScores();
    }

    /// <summary>
    ///     Called once this screen has been handed control, with the score it needs.
    ///     <para>
    ///         This took a <c>ScreenChange</c> and threw an <see cref="ArgumentException" /> for any
    ///         case but <c>FinalScore</c>, because the signature had to suit all nine other screens
    ///         too. The game over screen is the only way onto this one and always had a score to
    ///         pass, so the throw stood for a mistake nobody could make; it now cannot be written.
    ///     </para>
    /// </summary>
    /// <param name="playersScore">
    ///     The score the player has just finished with, for a name to be typed against it.
    /// </param>
    public void PerformPostScreenChangeActions(HiScore playersScore)
    {
        _fadeComplete = false;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
        _playersHighScore = new HiScore();

        // Lets see if we have a place in the high score table
        _gotHighScore = _hiscoreList.GotHighScore(_stats.PlayerScore);

        if (_gotHighScore)
        {
            _playersHighScore = playersScore;
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
            if (Input.GetPressedKey() != Keys.None)
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
                int keyPressed = TextHelper.GetAsciiCode(keyPressedString);

                if (_playersHighScore.Name.Length < 6)
                {
                    if (keyPressed > 64 &&
                        keyPressed < 91)
                    {
                        _playersHighScore.Name = _playersHighScore.Name + keyPressedString;
                    }

                    if (Input.GetPressedKey() == Keys.Space)
                    {
                        _playersHighScore.Name = _playersHighScore.Name + " ";
                    }
                }

                if (Input.GetPressedKey() == Keys.Enter)
                {
                    _gotHighScore = false;
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

        // Draw the high score entry bit if the player has got a high score
        if (_gotHighScore)
        {

            Font.Begin(BlendState.AlphaBlend);
            Font.WriteTextCentred(1280, 180, "ENCODE YOUR NAME", 2, ColourHelper.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 240, "INTO THE CORE", 2, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 300, "FOR YOU HAVE ACHIEVED", 2, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 360, "A HERO'S SCORE", 2, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);

            Font.WriteTextCentred(1280, 420, _playersHighScore.Name, 2, ColourHelper.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.End();
        }
        else
        {
            Font.Begin(BlendState.AlphaBlend);
            Font.WriteTextCentred(1280, 180, "CORE OF HEROES", 2, ColourHelper.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.End();

            _hiscoreList.Draw(Resources, Font, 380, 250, ScreenFade);
        }

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
