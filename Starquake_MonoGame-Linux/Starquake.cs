using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.Structure;

namespace StarquakeXNA
{
    public class Starquake : Game
    {
        GraphicsDeviceManager graphics;
        private GameEngine gameEngine;

        public static int ScreenWidth = 1280;
        public static int ScreenHeight = 800;

        /// <summary>  
        // Main constructor
        /// </summary>  
        public Starquake()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.PreferredBackBufferWidth = ScreenWidth;
            graphics.PreferredBackBufferHeight = ScreenHeight;
            graphics.IsFullScreen = false;
            graphics.PreferMultiSampling = false;

            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
            this.IsFixedTimeStep = false;
            Content.RootDirectory = "Content";
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (graphics == null || 
                this.Window.ClientBounds.Width == 0 ||
                this.Window.ClientBounds.Height == 0)
                return;

            if (gameEngine != null)
            {
                if (gameEngine.ScreenEffctEngine != null)
                {
                    gameEngine.ScreenEffctEngine.CalculateScaling(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
                }
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            gameEngine = new GameEngine(this.Window, graphics, Content);
            
            // Initialise anything that needs exist before we load content
            gameEngine.DefineContent();
            
            // This will eventually call LoadContent
            base.Initialize();

            // Initialise the Game Engine
            gameEngine.Initialise();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            // If the game has already started and we're reloading content then it sounds like the graphics device has lost its handle. Time to bail with an error
            if (gameEngine.Status == GameEngineStatus.Started)
            {
                throw new Exception("The graphics device was unable to reinitialise. This is usually caused by a faulty video driver or corrupted state. Try rebooting you machine or finding an updated driver.");
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
            if (graphics == null)
            {
                return;
            }

            // This must happen here,, outside of the whole game eco system
            ToggleKeys.Update();

            // Switch to fullscreen if Ctrl and Enter touched
            if (gameEngine != null)
            {
                if (gameEngine.Status == GameEngineStatus.StartingGraphicsLoaded || 
                    gameEngine.Status == GameEngineStatus.Started ||
                    gameEngine.Status == GameEngineStatus.ContentLoaded ||
                    gameEngine.Status == GameEngineStatus.StartGame)
                {
                    // Allows the game to exit
                    if (ToggleKeys.GetPressedKey() == Keys.Escape)
                    {
                        if (gameEngine != null)
                        {
                            gameEngine.Dispose();
                        }

                        this.Exit();
                    }

                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                    {
                        this.Exit();
                    }

                    gameEngine.Update(gameTime);
                    base.Update(gameTime);

                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (gameEngine != null)
            {
                gameEngine.Draw(gameTime);

                if (gameEngine.ScreenEffctEngine != null)
                {
                    gameEngine.ScreenEffctEngine.Draw();
                }

                if (gameEngine.ScanlinesEnabled)
                {
                    // Draw scanlines
                    if (gameEngine.Scanlines != null)
                    {
                        gameEngine.Scanlines.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.Scanlines.DrawSprite(0, 0, 0, new Color(255, 255, 255, 255));
                        gameEngine.Scanlines.End();
                    }
                }
            }
        }
    }
}
