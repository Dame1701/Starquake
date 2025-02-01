using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class Starquake : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private GameEngine _gameEngine;

    public static readonly int ScreenWidth = 1280;
    public static readonly int ScreenHeight = 800;

    public Starquake()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        _graphics.IsFullScreen = false;
        _graphics.PreferMultiSampling = false;

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += Window_ClientSizeChanged;
        IsFixedTimeStep = false;
        Content.RootDirectory = "Content";
    }

    void Window_ClientSizeChanged(object sender, EventArgs e)
    {
        if (_graphics == null ||
            Window.ClientBounds.Width == 0 ||
            Window.ClientBounds.Height == 0)
            return;

        _gameEngine?.ScreenEffctEngine?.CalculateScaling(Window.ClientBounds.Width, Window.ClientBounds.Height);
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
        _gameEngine = new GameEngine(Window, _graphics, Content);

        // Initialise anything that needs exist before we load content
        _gameEngine.DefineContent();

        // This will eventually call LoadContent
        base.Initialize();

        // Initialise the Game Engine
        _gameEngine.Initialise();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        _graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        // If the game has already started and we're reloading content then it sounds like the graphics device has lost its handle. Time to bail with an error
        if (_gameEngine.Status == GameEngineStatus.Started)
        {
            throw new Exception(
                "The graphics device was unable to reinitialise. This is usually caused by a faulty video driver or corrupted state. Try rebooting you machine or finding an updated driver.");
        }

        // initialise game engine
        base.LoadContent();
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        if (_graphics == null)
        {
            return;
        }

        // This must happen here, outside of the whole game eco system
        ToggleKeys.Update();

        // Switch to fullscreen if Ctrl and Enter touched
        if (_gameEngine?.Status is GameEngineStatus.StartingGraphicsLoaded 
            or GameEngineStatus.Started 
            or GameEngineStatus.ContentLoaded 
            or GameEngineStatus.StartGame)
        {
            // Allows the game to exit
            if (ToggleKeys.GetPressedKey() == Keys.Escape)
            {
                _gameEngine?.Dispose();

                Exit();
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            _gameEngine?.Update(gameTime);
            base.Update(gameTime);
        }
    }

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        if (_gameEngine != null)
        {
            _gameEngine.Draw(gameTime);

            if (_gameEngine.ScreenEffctEngine != null)
            {
                _gameEngine.ScreenEffctEngine.Draw();
            }

            if (_gameEngine.ScanlinesEnabled)
            {
                // Draw scanlines
                if (_gameEngine.Scanlines != null)
                {
                    _gameEngine.Scanlines.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    _gameEngine.Scanlines.DrawSprite(0, 0, 0, new Color(255, 255, 255, 255));
                    _gameEngine.Scanlines.End();
                }
            }
        }
    }
}