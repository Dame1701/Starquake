// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Display;

/// <summary>
///     How the composed picture is put on the screen: the transform that scales it onto the back
///     buffer, the colours behind it, and where on it the game's own screens sit.
///     <para>
///         Not a service - there is nothing to ask it to do but recalculate the transform. It is
///         the state that used to be seven loose fields on the engine, read by the fourteen places
///         that draw and written by the four that change what the picture looks like.
///     </para>
/// </summary>
public sealed class PresentationSettings
{
    /// <summary>
    ///     The game is composed at a fixed <see cref="CompositionSize.Width" /> by
    ///     <see cref="CompositionSize.Height" /> and only scaled onto the back buffer as the very
    ///     last step, so nothing in the game has to know or care whether it is in a small window,
    ///     a dragged-out one, or fullscreen on a 6K display. The scale is uniform so the picture
    ///     is never stretched, and whatever is left over becomes an even border - top and bottom,
    ///     or left and right. Recalculated every frame rather than hung off a resize event, so a
    ///     fullscreen toggle, a window drag and a change of display all just work.
    /// </summary>
    public Matrix Transform { get; private set; } = Matrix.Identity;

    /// <summary>
    ///     What the graphics device is cleared to before anything is drawn. The intro sequence
    ///     flashes it white; everything else wants black.
    /// </summary>
    public Color ScreenClearColour { get; set; } = Color.Black;

    /// <summary>
    ///     What shows through behind a game screen's tiles. A core element about to be collected
    ///     flashes this up and fades it back down.
    /// </summary>
    public Color BackgroundColour { get; set; } = Color.Black;

    /// <summary>
    ///     Whether the backdrops behind the tile layers are drawn at all. The original graphics
    ///     have none, and F10 turns them off by hand.
    /// </summary>
    public bool DrawBackgrounds { get; set; } = true;

    /// <summary>
    ///     Whether the CRT scanline overlay is drawn over the finished picture.
    /// </summary>
    public bool ScanlinesEnabled { get; set; } = true;

    /// <summary>
    ///     Where the game's own screens are placed within the composition, which is not the whole
    ///     of it - the status panel takes the top, and the screen is centred in what is left. The
    ///     teleport screen moves these while it plays and puts them back afterwards.
    /// </summary>
    public int ScreenXOffset { get; set; }

    /// <inheritdoc cref="ScreenXOffset" />
    public int ScreenYOffset { get; set; }

    /// <summary>
    ///     Works out the scale and border for a back buffer of the given size. Pure arithmetic on
    ///     two numbers, kept apart from <see cref="Recalculate" /> so it can be checked without a
    ///     graphics device.
    /// </summary>
    public static Matrix TransformFor(int backBufferWidth, int backBufferHeight)
    {
        float scale = Math.Min(
            backBufferWidth / (float)CompositionSize.Width,
            backBufferHeight / (float)CompositionSize.Height);

        float borderX = (backBufferWidth - CompositionSize.Width * scale) / 2f;
        float borderY = (backBufferHeight - CompositionSize.Height * scale) / 2f;

        return
            Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(borderX, borderY, 0f);
    }

    /// <summary>
    ///     Points <see cref="Transform" /> at a back buffer of the given size.
    /// </summary>
    public void Recalculate(int backBufferWidth, int backBufferHeight)
        => Transform = TransformFor(backBufferWidth, backBufferHeight);
}
