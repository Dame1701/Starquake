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
using Starquake.Display;
using Starquake.GameComponents;
using Starquake.Input;

namespace Starquake.Engines;

/// <summary>
///     The keys that work anywhere in the game rather than belonging to a screen: pause,
///     fullscreen, the backdrops, the artwork swap, a screenshot and quit. The two that change how
///     the game looks say so through <see cref="ModeAnnouncement" />, because otherwise the only
///     way to know which way a toggle has just gone is to recognise the result.
/// </summary>
/// <remarks>
///     Most of these touch only the session and the presentation. The artwork swap has to tell
///     whoever is drawing that the sprites underneath them changed, and does so through a callback
///     the engine supplies rather than by holding the engine.
/// </remarks>
internal class GlobalHotkeys(
    ToggleKeys input,
    ScreenDirectory screenDirectory,
    GameSession session,
    PresentationSettings presentation,
    GraphicsSet graphicsSet,
    GraphicsDeviceManager graphics,
    GameWindow window,
    ResourceManager resources,
    ModeAnnouncement announce,
    Screenshots screenshots,
    Action artworkSwapped,
    Action quitGame)
{
    public void Update()
    {
        TogglePauseIfAsked();
        ToggleFullScreenIfAsked();
        ToggleBackgroundsIfAsked();
        SwitchGraphicsSetIfAsked();
        TakeScreenshotIfAsked();
        QuitIfAsked();
    }

    /// <summary>
    ///     Q closes the game, but only while it is paused. Escape closes it from anywhere and is
    ///     handled by the host itself, which is where ending the process belongs; this one is here
    ///     because the pause it reads is the session's, which this class already owns.
    /// </summary>
    /// <remarks>
    ///     The pause is what makes Q safe to take. Q is not a spare key during play: the original
    ///     Spectrum layout binds it to <em>up</em>, so quitting on it unconditionally would end the
    ///     game every time that player tried to climb. It is also <c>PlayerKeySet.Quit</c> in two of
    ///     the four layouts, which <c>MainGameScreen</c> reads to abandon a game to the Game Over
    ///     screen. Requiring a pause settles both.
    ///     <para>
    ///         Only an in-play screen can be paused, as <see cref="TogglePauseIfAsked" /> will not
    ///         pause anything else, so Escape remains the way out of the menus.
    ///     </para>
    /// </remarks>
    private void QuitIfAsked()
    {
        if (!session.Paused)
        {
            return;
        }

        if (input.GetPressedKey() == Keys.Q)
        {
            quitGame();
        }
    }

    /// <summary>
    ///     The teleporter is deliberately not pausable. It is the one in-play screen where standing
    ///     still is the point, so pausing it would do nothing and only confuse.
    /// </summary>
    private void TogglePauseIfAsked()
    {
        if (!screenDirectory.InPlay || screenDirectory.Current == GameScreenType.Teleporter)
        {
            return;
        }

        if (input.GetPressedKey() != session.PlayerKeys.Pause)
        {
            return;
        }

        session.Paused = !session.Paused;

        if (session.Paused)
        {
            resources.GetSoundFxEngine().KillSounds(true);
        }
    }

    /// <remarks>
    ///     A toggle rather than <c>IsKeyDown</c>, which held down would flip fullscreen once per logic
    ///     frame. The game pauses itself afterwards so the player does not die while the window
    ///     resizes, on every in-play screen but the teleporter, which cannot pause.
    /// </remarks>
    private void ToggleFullScreenIfAsked()
    {
        if (input.GetPressedKey() != Keys.F12)
        {
            return;
        }

        bool pausedForTheSwitch =
            screenDirectory.InPlay && screenDirectory.Current != GameScreenType.Teleporter;

        if (pausedForTheSwitch)
        {
            // Paused after switching to or from fullscreen, so the player does not die while the
            // window is resizing.
            session.Paused = true;
        }

        try
        {
            ToggleFullScreen();
        }
        catch
        {
            PutTheWindowBack(pausedForTheSwitch);

            throw;
        }
    }

    /// <summary>
    ///     Leaves the window usable after a failed display switch. The caller rethrows: this tidies
    ///     up, it does not handle anything.
    /// </summary>
    /// <remarks>
    ///     Going fullscreen drops the window's border before the switch, because SDL ignores a border
    ///     change on a window that is already fullscreen. So a throw after that line leaves the window
    ///     borderless but still windowed, which is a game with no title bar and no way to close it
    ///     with the mouse, and paused, because the pause is set before the attempt.
    ///     <para>
    ///         The border is put back to match where the device actually ended up, the pause lifted if
    ///         this is what set it, and the player told. It then rethrows: there is no documented
    ///         exception set for a device reset, nothing here can judge whether the graphics device is
    ///         still fit to draw with, and a game that cannot switch its own display has something
    ///         wrong worth seeing rather than hiding behind an F12 that quietly does nothing.
    ///     </para>
    /// </remarks>
    private void PutTheWindowBack(bool pausedForTheSwitch)
    {
        window.IsBorderless = graphics.IsFullScreen;

        if (pausedForTheSwitch)
        {
            session.Paused = false;
        }

        announce.Show("DISPLAY SWITCH FAILED");
    }

    /// <summary>
    ///     F5 asks for a screenshot. Only asks: the picture is read off the back buffer at the end
    ///     of the draw pass, where there is a finished frame to read, and that is also what
    ///     announces the result.
    /// </summary>
    private void TakeScreenshotIfAsked()
    {
        if (input.GetPressedKey() == Keys.F5)
        {
            screenshots.Ask();
        }
    }

    private void ToggleBackgroundsIfAsked()
    {
        if (input.GetPressedKey() == Keys.F10 && screenDirectory.Current == GameScreenType.MainGame)
        {
            presentation.DrawBackgrounds = !presentation.DrawBackgrounds;

            announce.Show(presentation.DrawBackgrounds ? "BACKDROPS ON" : "BACKDROPS OFF");
        }
    }

    private void SwitchGraphicsSetIfAsked()
    {
        if (input.GetPressedKey() != Keys.F8)
        {
            return;
        }

        graphicsSet.Switch();

        // The original graphics have no backdrops drawn for them.
        presentation.DrawBackgrounds = graphicsSet.IsRedrawn;

        announce.Show(graphicsSet.IsRedrawn ? "ENHANCED GRAPHICS" : "ORIGINAL GRAPHICS");

        artworkSwapped();

        resources.GetSoundFxEngine().PlaySound(Assets.Sounds.AccessSuccess, 1f, false);
    }

    /// <summary>
    ///     Goes fullscreen at the desktop's own resolution, so the picture is composed for the display
    ///     it lands on rather than stretched up from the windowed size. The game never changes size:
    ///     it always composes at <see cref="CompositionSize.Width" /> by
    ///     <see cref="CompositionSize.Height" /> and Presentation.Transform scales that onto whatever
    ///     back buffer results.
    ///     <para>
    ///         Borderless with no hardware mode switch, and both parts are needed. A mode switch gains
    ///         nothing, since the game scales onto any back buffer, and on a Retina Mac it leaves the
    ///         window server stroking a one point #333333 outline around all four edges of the
    ///         display: the window itself sits at exactly 0,0 by the full display size with its own
    ///         contents black to the last pixel, so that grey is composited outside it and is not the
    ///         game's to clear.
    ///     </para>
    ///     <para>
    ///         Borderless alone clamps the window to the desktop area <em>below</em> the menu bar, 39
    ///         points short and 39 points down. Dropping the border is what allows covering the menu
    ///         bar, so the two changes only work together.
    ///     </para>
    /// </summary>
    private void ToggleFullScreen()
    {
        graphics.HardwareModeSwitch = false;

        bool goingFullScreen = !graphics.IsFullScreen;

        if (goingFullScreen)
        {
            // Before the switch, not after. SDL ignores a border change on a window that is already
            // fullscreen, so asking afterwards does nothing.
            window.IsBorderless = true;

            DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            graphics.PreferredBackBufferWidth = desktop.Width;
            graphics.PreferredBackBufferHeight = desktop.Height;
        }
        else
        {
            graphics.PreferredBackBufferWidth = CompositionSize.Width;
            graphics.PreferredBackBufferHeight = CompositionSize.Height;
        }

        graphics.IsFullScreen = goingFullScreen;
        graphics.ApplyChanges();

        if (!goingFullScreen)
        {
            // And after the switch coming back, for the same reason, which is why this is not one
            // assignment either side of ApplyChanges. Asking for the border while the window is
            // still fullscreen leaves it borderless in a window, with no title bar and so no way to
            // close the game with the mouse.
            window.IsBorderless = false;
        }
    }
}
