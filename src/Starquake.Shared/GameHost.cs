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
using Starquake.Engines;
using Starquake.Input;

namespace Starquake;

public class GameHost : Game
{
    /// <summary>
    ///     How far the logic clock may fall behind before frames are dropped instead of caught up. A
    ///     stall from a breakpoint, a dragged window or the machine waking otherwise leaves a backlog
    ///     the game fast-forwards through in one go.
    /// </summary>
    private const int MaxCatchUpFrames = 5;

    /// <summary>
    ///     How much game time one logic frame is, whatever <see cref="_logicRate" /> is pacing them
    ///     at. Always a <see cref="LogicClock.FramesPerSecond" />th of a second, so at sixty hertz
    ///     the particles speed up with everything else rather than holding to the wall clock.
    /// </summary>
    private static readonly TimeSpan GameFrameLength = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / LogicClock.FramesPerSecond);
    private readonly GraphicsDeviceManager _graphics;

    /// <summary>
    ///     The frame's input. Owned here because this is what decides when a logic frame happens, and
    ///     input must be read exactly once per frame from exactly one place.
    /// </summary>
    private readonly ToggleKeys _input = new();

    /// <summary>
    ///     How many times a real second the game thinks, and so the speed of the game. The engine
    ///     moves everything by a fixed amount per logic frame and scales nothing by elapsed time,
    ///     exactly as the original did, so this alone decides how fast Blob runs. It need not divide
    ///     into the monitor's refresh rate. F11 switches it, through <c>GlobalHotkeys</c>.
    /// </summary>
    private readonly LogicRate _logicRate;

    /// <summary>The settings the function keys change, as saved at the end of the last run.</summary>
    private readonly Preferences _savedPreferences;

    private readonly PreferencesStore _preferencesStore = new();
    private readonly GameTime _logicGameTime = new();
    private GameEngine? _gameEngine;

    /// <summary>
    ///     Real time banked since the last logic frame, plus the logic clock the game sees. The game
    ///     is never told the real frame time: every logic frame is exactly
    ///     <see cref="GameFrameLength" /> of game time, so anything working off
    ///     <c>ElapsedGameTime</c>, such as the particle engine, stays locked to the same clock as
    ///     everything that counts frames.
    /// </summary>
    private TimeSpan _logicAccumulator;

    private TimeSpan _logicClock;

    private bool _resizingBackBuffer;

    public GameHost()
    {
        // Before anything is loaded, and before the graphics device exists. A normal build does not
        // need it. A NativeAOT one cannot load a particle effect or the collision map without it.
        NativeAot.Register();

        _savedPreferences = _preferencesStore.Read();
        _logicRate = new LogicRate(_savedPreferences.Hertz);

        _graphics = new GraphicsDeviceManager(this);
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.PreferredBackBufferWidth = CompositionSize.Width;
        _graphics.PreferredBackBufferHeight = CompositionSize.Height;
        _graphics.IsFullScreen = false;
        _graphics.PreferMultiSampling = false;

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += Window_ClientSizeChanged;

        // Draw runs once per vertical retrace, so presentation stays smooth and the loop is paced by
        // the display rather than spinning. Update is left free-running because the fixed logic step
        // is driven by hand below: MonoGame's own fixed step cannot subdivide a refresh interval
        // that is not a multiple of the target rate.
        IsFixedTimeStep = false;
        Content.RootDirectory = "Content";
    }

    /// <summary>
    ///     Keeps the back buffer the same size as the window, so the picture is composed at the
    ///     resolution it is displayed at rather than stretched up from the window's starting size.
    ///     The game needs no telling: it always composes at its fixed size and
    ///     PresentationSettings.Transform scales that onto whatever the back buffer now is.
    /// </summary>
    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        if (_resizingBackBuffer ||
            _graphics.IsFullScreen ||
            Window.ClientBounds.Width == 0 ||
            Window.ClientBounds.Height == 0)
        {
            return;
        }

        // ApplyChanges resizes the window, which raises this event straight back.
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

    protected override void Initialize()
    {
        // The engine defines and initialises its own content as it is built. base.Initialize is what
        // goes on to call LoadContent.
        _gameEngine = new GameEngine(
            _graphics, Content, _input, Window, _logicRate, _savedPreferences, _preferencesStore, Exit);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        // Content loading a second time means the graphics device was lost and MonoGame is rebuilding
        // it. The game has no path through that, so it says so rather than carrying on with half its
        // content.
        if (_gameEngine is { Status: GameEngineStatus.Started })
        {
            throw new Exception(
                "The graphics device was unable to reinitialise. This is usually caused by a faulty video driver or corrupted state. Try rebooting you machine or finding an updated driver.");
        }

        base.LoadContent();
    }

    /// <summary>
    ///     Called once per rendered frame, which on a modern display is far more often than the game
    ///     needs to think. Banks the real time that has passed and runs whole logic frames out of it,
    ///     so the game advances at <see cref="_logicRate" /> whatever the monitor does.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        _logicAccumulator += gameTime.ElapsedGameTime;

        // Read afresh each time, because F11 can change it between one call and the next.
        TimeSpan logicFrameLength = _logicRate.FrameLength;
        TimeSpan maxBacklog = logicFrameLength * MaxCatchUpFrames;

        if (_logicAccumulator > maxBacklog)
        {
            _logicAccumulator = maxBacklog;
        }

        while (_logicAccumulator >= logicFrameLength)
        {
            _logicAccumulator -= logicFrameLength;
            _logicClock += GameFrameLength;

            _logicGameTime.ElapsedGameTime = GameFrameLength;
            _logicGameTime.TotalGameTime = _logicClock;

            UpdateLogicFrame(_logicGameTime);
        }
    }

    /// <summary>
    ///     One logic frame. Everything that counts frames rather than seconds belongs here.
    /// </summary>
    /// <param name="gameTime">The logic clock, always exactly one logic frame elapsed.</param>
    private void UpdateLogicFrame(GameTime gameTime)
    {
        // Must run per logic frame rather than per rendered frame: a toggled key is only reported for
        // the one call after the release, so polling more often than the game reads it swallows
        // keypresses.
        _input.Update();

        // Nothing is acted on until the engine is far enough along to have something to act on.
        if (_gameEngine?.Status is GameEngineStatus.StartingGraphicsLoaded or GameEngineStatus.Started or GameEngineStatus.ContentLoaded or GameEngineStatus.StartGame)
        {
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

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        _gameEngine?.Draw(gameTime.ElapsedGameTime);
    }

    /// <summary>
    ///     Shutting down. The only place the engine is torn down, because every way out of the game
    ///     passes through here: the escape key, the gamepad's back button, the window's close box.
    ///     <c>Exit</c> only asks the loop to stop, so tearing down beside it would pull the engine
    ///     apart underneath frames still to be drawn.
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
