// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.GameComponents;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

public class ComputerReportScreen : GameScreenBase, IGameScreen
{
    private readonly Random _randomNumberGenerator;


    #region IGameScreen Members

    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;
    private bool _fadeComplete;

    public bool Success => true;

    public ComputerReportScreen(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        Random randomNumberGenerator)
        : base(
            resources,
            font,
            graphics,
            presentation,
            screenEffects,
            screenDirectory)
    {
        _randomNumberGenerator = randomNumberGenerator;

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection("ComputerReportScreenBackground");
        _foregroundImage = Resources.GetSpriteCollection("ComputerReportScreenForeground");
    }

    public override void Update(GameTime gameTime)
    {
        if (ToggleKeys.GetPressedKey() != Keys.None)
        {
            GraphicsDeviceManager.GraphicsDevice.Clear(Presentation.ScreenClearColour);
            ScreenDirectory.Current = GameScreenType.MainGame;

            ScreenDirectory.Get(GameScreenType.MainGame).Reset(null);
            ScreenDirectory.Get(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.ComputerReport, false);
        }

        if (ScreenFade >= ColourLevels.Full)
        {
            if (!_fadeComplete)
            {
                _fadeComplete = true;
            }

            ScreenFade = _randomNumberGenerator.Next(ColourLevels.Full);
            ScreenFadeSpeed = _randomNumberGenerator.Next(20);

            if (ScreenFadeSpeed < 4)
            {
                ScreenFadeSpeed = 4;
            }
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        ScreenEffects.SelectRenderViewPort("EntireScreen");
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

        Font.Begin(BlendState.AlphaBlend);
        Font.WriteTextCentred(1280, 180, "Flight Computer Report", 2, ColourHelper.GetXna4Colour(200, 200, 100), (byte)ScreenFade);
        Font.WriteTextCentred(1280, 260, "Touchdown Imminent Prepare", 2, Color.White, (byte)ScreenFade);
        Font.WriteTextCentred(1280, 320, "For Mission Starquake . . .", 2, Color.White, (byte)ScreenFade);
        Font.WriteTextCentred(1280, 380, "Crash . . . Bang . . . Smash . . .", 2, Color.Red, (byte)ScreenFade);
        Font.WriteTextCentred(1280, 440, "Touchdown", 2, Color.White, (byte)ScreenFade);
        Font.WriteTextCentred(1280, 500, "Comthuter Malthunction", 2, Color.Red, (byte)ScreenFade);
        Font.WriteTextCentred(1280, 560, "Malfunnythinkin . . .", 2, Color.Red, (byte)ScreenFade);
        Font.End();

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


    public void Reset(object? data)
    {

    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {
        _fadeComplete = false;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    #endregion

}