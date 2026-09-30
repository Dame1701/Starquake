// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Input;
using Starquake.Structure;

namespace Starquake;

public class GameHost : Game
{
    /// <summary>
    ///     How many times a second the game thinks. This is the speed of the game: the engine moves
    ///     everything by a fixed amount per logic frame and scales nothing by elapsed time, exactly as
    ///     the original did, so this number is the only thing deciding how fast the blob runs.
    ///     50 matches the PAL original. 60 is the NTSC speed and is 20% faster. Anything is allowed -
    ///     it does not have to divide into the monitor's refresh rate, and the game runs at this speed
    ///     on every machine regardless of what the display is doing.
    /// </summary>
    private const double LogicFramesPerSecond = LogicClock.FramesPerSecond;

    /// <summary>
    ///     How far behind we let the logic clock fall before dropping frames instead of catching up.
    ///     A stall - a breakpoint, dragging the window, the machine waking - otherwise leaves a
    ///     backlog that the game fast-forwards through all in one go.
    /// </summary>
    private const int MaxCatchUpFrames = 5;

    private static readonly TimeSpan LogicFrameLength = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond / LogicFramesPerSecond));

    private readonly GraphicsDeviceManager _graphics;

    /// <summary>
    ///     The frame's input. Owned here because this is what decides when a logic frame happens,
    ///     and it has to be read exactly once per frame from exactly one place.
    /// </summary>
    private readonly ToggleKeys _input = new();
    private readonly GameTime _logicGameTime = new();
    private GameEngine? _gameEngine;

    /// <summary>
    ///     Real time banked since the last logic frame, plus the logic clock the game sees. The game
    ///     is never told the real frame time: every logic frame is exactly one
    ///     <see cref="LogicFramesPerSecond" />th of a second long, so anything that does work off
    ///     ElapsedGameTime (the particle engine) stays locked to the same clock as everything that
    ///     counts frames.
    /// </summary>
    private TimeSpan _logicAccumulator;

    private TimeSpan _logicClock;

    private bool _resizingBackBuffer;

    public GameHost()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.PreferredBackBufferWidth = CompositionSize.Width;
        _graphics.PreferredBackBufferHeight = CompositionSize.Height;
        _graphics.IsFullScreen = false;
        _graphics.PreferMultiSampling = false;

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += Window_ClientSizeChanged;

        // Draw runs once per vertical retrace so presentation stays smooth and the loop is paced
        // by the display rather than spinning. Update is left free-running because the fixed
        // fifty hertz logic step is driven by hand below - MonoGame's own fixed step cannot
        // subdivide a refresh interval that is not a multiple of the target rate.
        IsFixedTimeStep = false;
        Content.RootDirectory = "Content";
    }

    /// <summary>
    ///     Keep the back buffer the same size as the window the user just dragged, so the picture is
    ///     composed at the resolution it is actually displayed at rather than being stretched up from
    ///     whatever size the window happened to start at. The game itself needs no telling: it always
    ///     composes at its fixed size and PresentationSettings.Transform scales that onto whatever
    ///     the back buffer now is.
    /// </summary>
    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        if (_graphics == null ||
            _resizingBackBuffer ||
            _graphics.IsFullScreen ||
            Window.ClientBounds.Width == 0 ||
            Window.ClientBounds.Height == 0)
        {
            return;
        }

        // ApplyChanges resizes the window, which raises this event straight back at us.
        _resizingBackBuffer = true;

        try
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
        }
        finally
        {
            _resizingBackBuffer = false;
        }
    }

    /// <summary>
    ///     Allows the game to perform any initialization it needs to before starting to run.
    ///     This is where it can query for any required services and load any non-graphic
    ///     related content.  Calling base.Initialize will enumerate through any components
    ///     and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
        // The engine defines and initialises its own content as it is built
        _gameEngine = new GameEngine(_graphics, Content, _input);

        // This will eventually call LoadContent
        base.Initialize();
    }

    /// <summary>
    ///     LoadContent will be called once per game and is the place to load
    ///     all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        _graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        // If the game has already started and we're reloading content then it sounds like the graphics device has lost its handle. Time to bail with an error
        if (_gameEngine is { Status: GameEngineStatus.Started })
        {
            throw new Exception(
                "The graphics device was unable to reinitialise. This is usually caused by a faulty video driver or corrupted state. Try rebooting you machine or finding an updated driver.");
        }

        // initialise game engine
        base.LoadContent();
    }

    /// <summary>
    ///     Called once per rendered frame, which on a modern display is far more often than the game
    ///     wants to think. Bank the real time that has passed and run whole logic frames out of it,
    ///     so the game advances at <see cref="LogicFramesPerSecond" /> no matter what the monitor does.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        if (_graphics == null)
        {
            return;
        }

        _logicAccumulator += gameTime.ElapsedGameTime;

        TimeSpan maxBacklog = LogicFrameLength * MaxCatchUpFrames;

        if (_logicAccumulator > maxBacklog)
        {
            _logicAccumulator = maxBacklog;
        }

        while (_logicAccumulator >= LogicFrameLength)
        {
            _logicAccumulator -= LogicFrameLength;
            _logicClock += LogicFrameLength;

            _logicGameTime.ElapsedGameTime = LogicFrameLength;
            _logicGameTime.TotalGameTime = _logicClock;

            UpdateLogicFrame(_logicGameTime);
        }
    }

    /// <summary>
    ///     One logic frame. Everything that counts frames rather than seconds belongs in here.
    /// </summary>
    /// <param name="gameTime">The logic clock, always exactly one logic frame elapsed.</param>
    private void UpdateLogicFrame(GameTime gameTime)
    {
        // This must happen here, outside of the whole game eco system. It has to run per logic
        // frame rather than per rendered frame: a toggled key is only reported for the one call
        // after the release, so polling it more often than the game reads it swallows keypresses.
        _input.Update();

        // Switch to fullscreen if Ctrl and Enter touched
        if (_gameEngine?.Status is GameEngineStatus.StartingGraphicsLoaded or GameEngineStatus.Started or GameEngineStatus.ContentLoaded or GameEngineStatus.StartGame)
        {
            // Allows the game to exit
            if (_input.GetPressedKey() == Keys.Escape)
            {
                Exit();
            }

            if (_input.GamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            _gameEngine?.Update(gameTime);
            base.Update(gameTime);
        }
    }

    /// <summary>
    ///     This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        _gameEngine?.Draw(gameTime);
    }

    /// <summary>
    ///     Shutting down. This is the only place the engine is torn down, because it is the only one
    ///     every way out of the game passes through - the escape key, the gamepad's back button, the
    ///     window's close box - and because Exit only asks the loop to stop, so a teardown done next
    ///     to it would be pulling the engine apart underneath the frames still to be drawn.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameEngine?.Dispose();
        }

        base.Dispose(disposing);
    }
}
