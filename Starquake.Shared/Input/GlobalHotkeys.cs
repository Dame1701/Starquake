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
using Starquake.GameComponents;
using Starquake.Structure;

namespace Starquake.Input;

/// <summary>
///     The four keys that work anywhere in the game rather than belonging to a screen: pause,
///     fullscreen, the backdrops and the artwork swap.
/// </summary>
/// <remarks>
///     These are not the composition root and they are not the frame loop, which is what
///     <c>GameEngine</c> is for. Three of them are about the session and the presentation and
///     nothing else; the fourth, the artwork swap, has to tell whoever is drawing that the sprites
///     underneath them have changed, and it does that through a callback the engine supplies
///     rather than by holding the engine.
/// </remarks>
internal class GlobalHotkeys
{
    /// <summary>
    ///     The screens the player is considered to be in play on. Pause and fullscreen only mean
    ///     something on these.
    /// </summary>
    private static readonly GameScreenType[] InPlayScreens =
    [
        GameScreenType.Cheops,
        GameScreenType.MainGame,
        GameScreenType.PlanetCore,
        GameScreenType.Teleporter,
        GameScreenType.AccessBooth
    ];

    private readonly Action _artworkSwapped;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly ToggleKeys _input;
    private readonly PresentationSettings _presentation;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly GameSession _session;

    public GlobalHotkeys(
        ToggleKeys input,
        ScreenDirectory screenDirectory,
        GameSession session,
        PresentationSettings presentation,
        GraphicsSet graphicsSet,
        GraphicsDeviceManager graphics,
        ResourceManager resources,
        Action artworkSwapped)
    {
        _input = input;
        _screenDirectory = screenDirectory;
        _session = session;
        _presentation = presentation;
        _graphicsSet = graphicsSet;
        _graphicsDeviceManager = graphics;
        _resources = resources;
        _artworkSwapped = artworkSwapped;
    }

    public void Update()
    {
        TogglePauseIfAsked();
        ToggleFullScreenIfAsked();
        ToggleBackgroundsIfAsked();
        SwitchGraphicsSetIfAsked();
    }

    /// <summary>
    ///     The teleporter is deliberately not pausable - it is the one in-play screen where standing
    ///     still is the point, so pausing it would do nothing and only confuse.
    /// </summary>
    private void TogglePauseIfAsked()
    {
        if (!InPlay() || _screenDirectory.Current == GameScreenType.Teleporter)
        {
            return;
        }

        if (_input.GetPressedKey() != _session.PlayerKeys.Pause)
        {
            return;
        }

        _session.Paused = !_session.Paused;

        if (_session.Paused)
        {
            _resources.GetSoundFxEngine().KillSounds(true);
        }
    }

    /// <remarks>
    ///     A toggle rather than IsKeyDown: held down, that flipped fullscreen once per logic frame for
    ///     as long as the key was held. The game pauses itself afterwards so the player does not die
    ///     while the window is resizing - on every in-play screen but the teleporter, which cannot pause.
    /// </remarks>
    private void ToggleFullScreenIfAsked()
    {
        if (_input.GetPressedKey() != Keys.F12)
        {
            return;
        }

        if (InPlay() && _screenDirectory.Current != GameScreenType.Teleporter)
        {
            // Automatically pause the game after switching to or from fullscreen so the player
            // doesn't die unexpectedly
            _session.Paused = true;
        }

        try
        {
            ToggleFullScreen();
        }
        catch { }
    }

    private void ToggleBackgroundsIfAsked()
    {
        if (_input.GetPressedKey() == Keys.F10 && _screenDirectory.Current == GameScreenType.MainGame)
        {
            _presentation.DrawBackgrounds = !_presentation.DrawBackgrounds;
        }
    }

    private void SwitchGraphicsSetIfAsked()
    {
        if (_input.GetPressedKey() != Keys.F8)
        {
            return;
        }

        _graphicsSet.Switch();

        // The original graphics have no backdrops drawn for them.
        _presentation.DrawBackgrounds = _graphicsSet.IsRedrawn;

        _artworkSwapped();

        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.AccessSuccess, 1f, false);
    }

    private bool InPlay()
        => Array.IndexOf(InPlayScreens, _screenDirectory.Current) >= 0;

    /// <summary>
    ///     Go fullscreen at the resolution the desktop is already running at, so the picture is
    ///     composed for the display it lands on instead of being stretched up from the windowed
    ///     size. The game itself never changes size: it always composes at
    ///     <see cref="CompositionSize.Width" /> x <see cref="CompositionSize.Height" /> and
    ///     Presentation.Transform scales that onto whatever back buffer we end up with.
    ///     HardwareModeSwitch has to be on. Borderless (the false case) looks like the friendlier
    ///     option, but on macOS it produces a window clamped to the desktop area *below* the menu
    ///     bar - a back buffer 39 points short, sat 39 points down the screen, with the picture
    ///     centred inside that rather than inside the display.
    /// </summary>
    private void ToggleFullScreen()
    {
        _graphicsDeviceManager.HardwareModeSwitch = true;

        if (_graphicsDeviceManager.IsFullScreen)
        {
            _graphicsDeviceManager.PreferredBackBufferWidth = CompositionSize.Width;
            _graphicsDeviceManager.PreferredBackBufferHeight = CompositionSize.Height;
        }
        else
        {
            DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            _graphicsDeviceManager.PreferredBackBufferWidth = desktop.Width;
            _graphicsDeviceManager.PreferredBackBufferHeight = desktop.Height;
        }

        _graphicsDeviceManager.IsFullScreen = !_graphicsDeviceManager.IsFullScreen;
        _graphicsDeviceManager.ApplyChanges();
    }
}
