// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Input;
using Starquake.Sprites;

namespace Starquake.GameComponents.PresentationScreens;

/// <summary>
///     The boot screen: the picture that fades up while the game loads, the progress bar across the
///     bottom of it, and the wait for a keypress once it is full.
/// </summary>
/// <remarks>
///     Drawn before the game exists, which is why it loads its three sprites directly rather than
///     through the resource manager, and why its viewport is the whole back buffer rather than a
///     render view port: there is no screen effect engine yet to own one.
/// </remarks>
internal class LoadingScreen(
    GraphicsDeviceManager graphics,
    ContentManager content,
    PresentationSettings presentation,
    ToggleKeys input)
{
    /// <summary>
    ///     How much of a colour channel the fade moves each logic frame.
    ///     <see cref="ColourLevels.Full" /> divided by this is how many frames the fade lasts.
    /// </summary>
    private const int BootFadeStep = 4;

    /// <summary>
    ///     How wide the boot progress bar is at full, in the one-pixel slices it is drawn from, and
    ///     where the first slice goes. Each slice is drawn two pixels right of the last.
    /// </summary>
    private const int BootProgressBarSlices = 519;

    /// <inheritdoc cref="BootProgressBarSlices" />
    private const int BootProgressBarXPos = 122;

    /// <inheritdoc cref="BootProgressBarSlices" />
    private const int BootProgressBarYPos = 661;

    /// <inheritdoc cref="BootProgressBarSlices" />
    private const int BootProgressBarSliceSpacing = 2;

    /// <summary>Where the progress bar's backing plate sits on the boot screen.</summary>
    private const int BootProgressBackgroundXPos = 116;

    /// <inheritdoc cref="BootProgressBackgroundXPos" />
    private const int BootProgressBackgroundYPos = 657;

    /// <summary>
    ///     How big the two progress bar sprites are. The bar itself is drawn one slice at a time.
    /// </summary>
    private const int ProgressBackgroundWidth = 1046;

    /// <inheritdoc cref="ProgressBackgroundWidth" />
    private const int ProgressBackgroundHeight = 32;

    /// <inheritdoc cref="ProgressBackgroundWidth" />
    private const int ProgressSliceWidth = 1;

    /// <inheritdoc cref="ProgressBackgroundWidth" />
    private const int ProgressSliceHeight = 24;

    private readonly SpriteCollection _bootProgress = new(
        graphics, content, Assets.Startup.BootProgress, ProgressSliceWidth, ProgressSliceHeight);
    private readonly SpriteCollection _bootProgressBackground = new(
        graphics, content, Assets.Startup.ProgressBarBackground, ProgressBackgroundWidth, ProgressBackgroundHeight);
    private readonly SpriteCollection _bootScreen = new(
        graphics, content, Assets.Startup.BootScreen, CompositionSize.Width, CompositionSize.Height);

    private int _bootProgressBackgroundFade = ColourLevels.Full;
    private int _loadingScreenFade;

    /// <summary>Fades the boot screen up a step.</summary>
    /// <returns>True once it is fully up, which is when the content load may begin.</returns>
    public bool FadeUp()
    {
        _loadingScreenFade += BootFadeStep;

        if (_loadingScreenFade >= ColourLevels.Full)
        {
            _loadingScreenFade = ColourLevels.Full;

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Holds the finished loading screen until a key is pressed, fading the progress bar out while
    ///     it waits. The wait is deliberate, so the boot screen is something to look at and leave
    ///     rather than something that vanishes.
    /// </summary>
    /// <returns>True when the player has pressed something and the game may start.</returns>
    public bool WaitForAKey()
    {
        if (_bootProgressBackgroundFade > 0)
        {
            _bootProgressBackgroundFade -= BootFadeStep;
        }

        if (_bootProgressBackgroundFade < 0)
        {
            _bootProgressBackgroundFade = 0;
        }

        return input.AnyKeyPressed;
    }

    /// <summary>
    ///     Draws the boot screen, its progress bar backing, and as much of the bar as has been earned.
    /// </summary>
    /// <param name="percentComplete">How far through loading the content the game is.</param>
    /// <param name="fullyLoadedPercent">What <paramref name="percentComplete" /> reads at the end.</param>
    public void Draw(int percentComplete, int fullyLoadedPercent)
    {
        // The viewport must be the whole back buffer rather than the composition size. The boot
        // screen is scaled up into it by Presentation.Transform like everything else, and a smaller
        // viewport would only clip the result.
        PresentationParameters backBuffer = graphics.GraphicsDevice.PresentationParameters;

        graphics.GraphicsDevice.Viewport = new Viewport
        {
            X = 0,
            Y = 0,
            Width = backBuffer.BackBufferWidth,
            Height = backBuffer.BackBufferHeight
        };
        graphics.GraphicsDevice.Clear(presentation.ScreenClearColour);

        _bootScreen.Begin(BlendState.Opaque, presentation.Transform);
        _bootScreen.DrawSpriteUnscaled(0, 0, 0, _loadingScreenFade);
        _bootScreen.End();

        _bootProgressBackground.Begin(BlendState.AlphaBlend, presentation.Transform);
        _bootProgressBackground.DrawSpriteUnscaled(
            0, BootProgressBackgroundXPos, BootProgressBackgroundYPos, _bootProgressBackgroundFade);
        _bootProgressBackground.End();

        _bootProgress.Begin(BlendState.AlphaBlend, presentation.Transform);

        int percentBarWidth = (int)(BootProgressBarSlices / (double)fullyLoadedPercent * percentComplete);

        for (int slice = 0; slice < percentBarWidth; slice++)
        {
            _bootProgress.DrawSpriteUnscaled(
                0,
                BootProgressBarXPos + slice * BootProgressBarSliceSpacing,
                BootProgressBarYPos,
                _bootProgressBackgroundFade);
        }

        _bootProgress.End();
    }
}
