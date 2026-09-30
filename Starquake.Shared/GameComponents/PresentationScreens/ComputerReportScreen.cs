// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

public class ComputerReportScreen : GameScreenBase, IGameScreen
{

    #region IGameScreen Members

    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;
    private bool _fadeComplete;

    public bool Success => true;

    public ComputerReportScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("ComputerReportScreenBackground");
        _foregroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("ComputerReportScreenForeground");
    }

    public override void Update(GameTime gameTime)
    {
        if (ToggleKeys.GetPressedKey() != Keys.None)
        {
            GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.ScreenClearColour);
            GameEngine.CurrentGameScreen = GameScreenType.MainGame;

            GameEngine.GetGameScreen(GameScreenType.MainGame).Reset(null);
            GameEngine.GetGameScreen(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.ComputerReport, false);
        }

        if (ScreenFade >= ColourLevels.Full)
        {
            if (!_fadeComplete)
            {
                _fadeComplete = true;
            }

            ScreenFade = GameEngine.RandomNumberGenerator.Next(ColourLevels.Full);
            ScreenFadeSpeed = GameEngine.RandomNumberGenerator.Next(20);

            if (ScreenFadeSpeed < 4)
            {
                ScreenFadeSpeed = 4;
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
            _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
        }

        _backgroundImage.End();

        GameEngine.Font2.Begin(BlendState.AlphaBlend);
        GameEngine.Font2.WriteTextCentred(1280, 180, "Flight Computer Report", 2, ColourHelper.GetXna4Colour(200, 200, 100), (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 260, "Touchdown Imminent Prepare", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 320, "For Mission Starquake . . .", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 380, "Crash . . . Bang . . . Smash . . .", 2, Color.Red, (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 440, "Touchdown", 2, Color.White, (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 500, "Comthuter Malthunction", 2, Color.Red, (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 560, "Malfunnythinkin . . .", 2, Color.Red, (byte)ScreenFade);
        GameEngine.Font2.End();

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