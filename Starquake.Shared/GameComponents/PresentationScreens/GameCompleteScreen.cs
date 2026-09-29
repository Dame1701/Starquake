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

public class GameCompleteScreen : GameScreenBase, IGameScreen
{
    private SpriteCollection _backgroundImage;
    private bool _fadeComplete;
    private SpriteCollection _foregroundImage;

    public GameCompleteScreen(GameEngine gameEngineReference)
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
        _fadeComplete = false;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    public override void Update(GameTime gameTime)
    {
        if (ToggleKeys.GetPressedKey() != Keys.None)
        {
            GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.ScreenClearColour);
            GameEngine.CurrentGameScreen = GameScreenType.GameOver;
            GameEngine.GetGameScreen(GameScreenType.GameOver).PerformPostScreenChangeActions(GameScreenType.GameComplete, null);
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
        GameEngine.Font2.WriteTextCentred(1280, 150, "THE CORES COMPLETE", 2, ColourHelper.GetXna4Colour(230, 0, 0), (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 210, "BUT HOW ARE YOU GONNA", 2, Color.Gray, (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 270, "GET HOME WHEN ONLY A", 2, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ScreenFade), (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 330, "THTUPID LOONY WOULD WANDER", 2, new Color((byte)150, (byte)150, (byte)150, (byte)ScreenFade), (byte)ScreenFade);
        GameEngine.Font2.WriteTextCentred(1280, 390, "THIS FAR OUT IN THE GALAXY", 2, new Color((byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ColourLevels.Full, (byte)ScreenFade), (byte)ScreenFade);
        GameEngine.Font2.End();

        // Core elements
        GameEngine.Elements.Draw(gameTime, 540, 450, ScreenFade);

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
        _backgroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("GameCompleteScreenBackground");
        _foregroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("GameCompleteScreenForeground");
    }
}