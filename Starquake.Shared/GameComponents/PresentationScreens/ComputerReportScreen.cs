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
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

internal class ComputerReportScreen : GameScreenBase, IGameScreen
{
    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;
    private readonly Random _randomNumberGenerator;
    private bool _fadeComplete;

    /// <summary>
    ///     How long the page has been on screen, in logic frames, which is the whole of what the
    ///     malfunction is driven by - see <see cref="MalfunctionSettings" />.
    /// </summary>
    private int _framesShown;

    public ComputerReportScreen(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        Random randomNumberGenerator,
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
        _randomNumberGenerator = randomNumberGenerator;

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection(Assets.Graphics.ComputerReportScreenBackground);
        _foregroundImage = Resources.GetSpriteCollection(Assets.Graphics.ComputerReportScreenForeground);
    }

    public override void Update(GameTime gameTime)
    {
        if (Input.GetPressedKey() != Keys.None)
        {
            GraphicsDeviceManager.GraphicsDevice.Clear(Presentation.ScreenClearColour);
            ScreenDirectory.Current = GameScreenType.MainGame;

            ScreenDirectory.MainGame.Reset();
            ScreenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.ComputerReport, new ScreenChange.KeepPicture());
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

        _framesShown++;

        base.Update(gameTime);
    }

    public void Draw()
    {
        ScreenEffects.SelectRenderViewPort(ViewPorts.EntireScreen);

        // Asked for before the page is drawn and applied after it, when the view port is composed:
        // what the malfunction has to break up is the finished page, so that the text comes apart
        // along with the artwork under it rather than swimming about on top of it.
        ScreenEffects.ComposeViewPortThrough(ViewPorts.EntireScreen, Malfunctioning());

        _backgroundImage.Begin(BlendState.AlphaBlend);

        if (_fadeComplete)
        {
            _backgroundImage.DrawSprite(0, 0, 0, Color.White);
        }
        else
        {
            _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.WhiteFadedTo((byte)ScreenFade));
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

        _foregroundImage.Begin(BlendState.AlphaBlend);

        if (_fadeComplete)
        {
            _foregroundImage.DrawSprite(0, 230, 120, Color.White);
        }
        else
        {
            _foregroundImage.DrawSprite(0, 230, 120, ColourHelper.WhiteFadedTo((byte)ScreenFade));
        }

        _foregroundImage.End();
    }

    public void PerformPostScreenChangeActions()
    {
        _fadeComplete = false;
        _framesShown = 0;
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    /// <summary>
    ///     The display failing, set up for this frame. <c>Shaders/Malfunction.fx</c> does the work;
    ///     everything handed to it is a function of how long the page has been up, so it fails the
    ///     same way every time anybody reads it.
    /// </summary>
    private Effect Malfunctioning()
    {
        Effect failing = Resources.GetEffect(Assets.Shaders.Malfunction);

        failing.Parameters["Breakup"].SetValue(MalfunctionSettings.BreakupAt(_framesShown));
        failing.Parameters["Phase"].SetValue(MalfunctionSettings.PhaseAt(_framesShown));
        failing.Parameters["Roll"].SetValue(MalfunctionSettings.RollAt(_framesShown));
        failing.Parameters["BandScale"].SetValue(MalfunctionSettings.BandScale);
        failing.Parameters["Slip"].SetValue(MalfunctionSettings.Slip);
        failing.Parameters["ChromaSplit"].SetValue(MalfunctionSettings.ChromaSplit);
        failing.Parameters["Dropout"].SetValue(MalfunctionSettings.Dropout);
        failing.Parameters["SignalColour"].SetValue(MalfunctionSettings.SignalColour);

        return failing;
    }
}
