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
///     The five keys that work anywhere in the game rather than belonging to a screen: pause,
///     fullscreen, the backdrops, the artwork swap and quit.
/// </summary>
/// <remarks>
///     These are not the composition root and they are not the frame loop, which is what
///     <c>GameEngine</c> is for. Three of them are about the session and the presentation and
///     nothing else; the fourth, the artwork swap, has to tell whoever is drawing that the sprites
///     underneath them have changed, and it does that through a callback the engine supplies
///     rather than by holding the engine.
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
    Action artworkSwapped,
    Action quitGame)
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

    public void Update()
    {
        TogglePauseIfAsked();
        ToggleFullScreenIfAsked();
        ToggleBackgroundsIfAsked();
        SwitchGraphicsSetIfAsked();
        QuitIfAsked();
    }

    /// <summary>
    ///     Q closes the game, but only while it is paused. Escape closes it from anywhere and is
    ///     handled by the host itself, which is where ending the process belongs; this one is here
    ///     because the pause it reads is the session's, which this class already owns.
    /// </summary>
    /// <remarks>
    ///     The pause is what makes Q safe to take. Q is not a spare key during play: the original
    ///     Spectrum layout binds it to <em>up</em>, so quitting on it unconditionally ended the game
    ///     every time that player tried to climb. It is also <c>PlayerKeySet.Quit</c> in two of the
    ///     four layouts, which <c>MainGameScreen</c> reads to abandon a game to the Game Over screen.
    ///     Pausing first settles both: nothing is being steered, and while the hand-over to Game Over
    ///     is still reached in the same logic frame, the process ends before it can be seen.
    ///     <para>
    ///         Only an in-play screen can be paused - <see cref="TogglePauseIfAsked" /> will not pause
    ///         anything else - so on the menus Escape remains the way out.
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
    ///     The teleporter is deliberately not pausable - it is the one in-play screen where standing
    ///     still is the point, so pausing it would do nothing and only confuse.
    /// </summary>
    private void TogglePauseIfAsked()
    {
        if (!InPlay() || screenDirectory.Current == GameScreenType.Teleporter)
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
    ///     A toggle rather than IsKeyDown: held down, that flipped fullscreen once per logic frame for
    ///     as long as the key was held. The game pauses itself afterwards so the player does not die
    ///     while the window is resizing - on every in-play screen but the teleporter, which cannot pause.
    /// </remarks>
    private void ToggleFullScreenIfAsked()
    {
        if (input.GetPressedKey() != Keys.F12)
        {
            return;
        }

        if (InPlay() && screenDirectory.Current != GameScreenType.Teleporter)
        {
            // Automatically pause the game after switching to or from fullscreen so the player
            // doesn't die unexpectedly
            session.Paused = true;
        }

        try
        {
            ToggleFullScreen();
        }
        catch { }
    }

    private void ToggleBackgroundsIfAsked()
    {
        if (input.GetPressedKey() == Keys.F10 && screenDirectory.Current == GameScreenType.MainGame)
        {
            presentation.DrawBackgrounds = !presentation.DrawBackgrounds;
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

        artworkSwapped();

        resources.GetSoundFxEngine().PlaySound(Assets.Sounds.AccessSuccess, 1f, false);
    }

    private bool InPlay()
        => Array.IndexOf(InPlayScreens, screenDirectory.Current) >= 0;

    /// <summary>
    ///     Go fullscreen at the resolution the desktop is already running at, so the picture is
    ///     composed for the display it lands on instead of being stretched up from the windowed
    ///     size. The game itself never changes size: it always composes at
    ///     <see cref="CompositionSize.Width" /> x <see cref="CompositionSize.Height" /> and
    ///     Presentation.Transform scales that onto whatever back buffer we end up with.
    ///     <para>
    ///         A hardware mode switch was used here and is not needed: the game scales onto any back
    ///         buffer, so there is nothing to gain from changing the display's mode, and on a Retina
    ///         Mac it left the window server stroking a one point #333333 outline round all four
    ///         edges of the display. Measured rather than guessed - the window itself sat at exactly
    ///         0,0 by the full display size and its own contents were black to the last pixel on
    ///         every edge, so the grey was composited outside it and was not the game's to draw or
    ///         to clear. Borderless with no mode switch is the combination that has no outline.
    ///     </para>
    ///     <para>
    ///         Borderless alone was tried before and rejected, because on its own it gives a window
    ///         clamped to the desktop area *below* the menu bar - 39 points short and 39 points down.
    ///         Dropping the window's border is what stops that: a bordered window cannot cover the
    ///         menu bar, and a borderless one can. The two changes only work together.
    ///     </para>
    /// </summary>
    private void ToggleFullScreen()
    {
        graphics.HardwareModeSwitch = false;

        bool goingFullScreen = !graphics.IsFullScreen;

        if (goingFullScreen)
        {
            // Before the switch, not after: SDL ignores a border change on a window that is
            // already fullscreen, so asking for it afterwards does nothing at all.
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
            // And after the switch coming back, for that same reason - which is the whole of why
            // this is not one assignment either side of ApplyChanges. Asking for the border while
            // the window was still fullscreen left it borderless in a window, with no title bar
            // and so no way to close the game with the mouse.
            window.IsBorderless = false;
        }
    }
}
