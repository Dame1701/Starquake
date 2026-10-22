// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Core;

/// <summary>
///     How the composed picture is put on the screen: the transform that scales it onto the back
///     buffer, the colours behind it, and where the game's own screens sit on it.
///     <para>
///         State rather than a service. There is nothing to ask it to do but recalculate the
///         transform.
///     </para>
/// </summary>
internal sealed class PresentationSettings
{
    /// <summary>
    ///     Scales the composition onto the back buffer, as the last step before presenting. The game
    ///     is composed at a fixed <see cref="CompositionSize.Width" /> by
    ///     <see cref="CompositionSize.Height" />, so nothing in it needs to know whether it is in a
    ///     small window or fullscreen on a 6K display.
    ///     <para>
    ///         The scale is uniform, so the picture is never stretched and what is left over becomes
    ///         an even border. Recalculated every frame rather than hung off a resize event, so a
    ///         fullscreen toggle, a window drag and a change of display all work.
    ///     </para>
    /// </summary>
    public Matrix Transform { get; private set; } = Matrix.Identity;

    /// <summary>
    ///     What the graphics device is cleared to before anything is drawn. The intro sequence
    ///     flashes it white; everything else wants black.
    /// </summary>
    public Color ScreenClearColour { get; set; } = Color.Black;

    /// <summary>
    ///     Whether the backdrops behind the tile layers are drawn. The original graphics have none,
    ///     and F10 turns them off by hand.
    /// </summary>
    public bool DrawBackgrounds { get; set; } = true;

    /// <summary>
    ///     How much of a cathode ray tube the finished picture is put through. Stepped by F9, and
    ///     starting at <see cref="CrtMode.Scanlines" /> because that is how the game has always
    ///     looked.
    /// </summary>
    public CrtMode CrtMode { get; set; } = CrtMode.Scanlines;

    /// <summary>
    ///     Where the game's own screens sit within the composition, which is not the whole of it:
    ///     the status panel takes the top and the screen is centred in what is left. The teleport
    ///     screen moves these while it plays and puts them back afterwards.
    /// </summary>
    public int ScreenXOffset { get; set; }

    /// <inheritdoc cref="ScreenXOffset" />
    public int ScreenYOffset { get; set; }

    /// <summary>
    ///     Works out the scale and border for a back buffer of the given size. Kept apart from
    ///     <see cref="Recalculate" /> so the arithmetic can be checked without a graphics device.
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

    /// <summary>Points <see cref="Transform" /> at a back buffer of the given size.</summary>
    public void Recalculate(int backBufferWidth, int backBufferHeight)
        => Transform = TransformFor(backBufferWidth, backBufferHeight);
}
