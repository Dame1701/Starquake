// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
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

public class HiScoreScreen : GameScreenBase, IGameScreen
{
    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;

    private readonly HiScores _hiscoreList;
    private readonly StatusPanel _statusPanel;
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
        StatusPanel statusPanel,
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
        _statusPanel = statusPanel;

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection("HiScoreScreenBackground");
        _foregroundImage = Resources.GetSpriteCollection("HiScoreScreenForeground");
        _hiscoreList = new HiScores();
    }


    public void Reset(object? data)
    {

    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {
        _fadeComplete = false;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
        _playersHighScore = new HiScore();

        // Lets see if we have a place in the high score table
        _gotHighScore = _hiscoreList.GotHighScore(_statusPanel.PlayerScore);

        if (_gotHighScore)
        {
            // The game over screen hands the score across so a name can be typed into it
            if (param is not HiScore playersScore)
            {
                throw new ArgumentException(
                    "The high score screen needs the player's score to enter a name against.", nameof(param));
            }

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
                ScreenDirectory.Get(GameScreenType.Options).PerformPostScreenChangeActions(GameScreenType.HiScores, null);
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
