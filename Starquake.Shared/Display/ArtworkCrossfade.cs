// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.Display;

/// <summary>
///     Dissolves the picture from one set of artwork into the other when F8 switches between them,
///     so the swap reads as the planet changing rather than as the game blinking.
/// </summary>
/// <remarks>
///     Holds the last frame drawn with the artwork being left and fades it out over the frames drawn
///     with the artwork arrived at. A frozen frame rather than the live scene drawn twice, and not as
///     a shortcut: the draw path in this codebase steps the state it is drawing, so drawing the scene
///     twice would advance every animation counter twice. <c>GODCLASSES.md</c> lists where.
///     <para>
///         So the held picture does not move while the live one underneath it does. That reads as a
///         dissolve over a third of a second, which is why the fade is this short and why it is not
///         offered as a general screen transition.
///     </para>
/// </remarks>
internal sealed class ArtworkCrossfade(GraphicsDeviceManager graphics)
{
    /// <summary>
    ///     How much of the old picture is taken away each logic frame. A third of a second: long
    ///     enough to read as a dissolve, short enough that the frozen picture has not visibly stopped.
    /// </summary>
    private static readonly int FadeStep = ColourLevels.Full / LogicClock.Seconds(0.35);

    private readonly SpriteBatch _batch = new(graphics.GraphicsDevice);

    private RenderTarget2D? _heldPicture;

    private bool _holdTheNextFrameCaught;

    private int _fade;

    /// <summary>
    ///     How much of the old picture is still showing. Nought when no swap is dissolving.
    /// </summary>
    public int Fade => _fade;

    /// <summary>
    ///     Reports that the artwork has just been switched. Called from the logic frame that did it.
    ///     The picture to hold is the one already drawn, so nothing is captured until the next draw
    ///     comes round and can catch it before the clear.
    /// </summary>
    public void Start()
    {
        _holdTheNextFrameCaught = true;
        _fade = ColourLevels.Full;
    }

    /// <summary>
    ///     One logic frame of the dissolve. Counted in logic frames and never drawn ones, for the
    ///     reason <see cref="ModeAnnouncement" /> gives.
    /// </summary>
    public void Update() => _fade = Ramp.Down(_fade, FadeStep, ColourLevels.None);

    /// <summary>
    ///     Takes the copy, if a swap has just happened. Must be called before the frame is cleared
    ///     for this draw, because what is wanted is what is still in it: the last picture drawn with
    ///     the artwork being left behind.
    /// </summary>
    public void HoldThePictureBeingLeft(RenderTarget2D frame)
    {
        if (!_holdTheNextFrameCaught)
        {
            return;
        }

        _holdTheNextFrameCaught = false;

        RenderTarget2D held = HeldFor(frame.Width, frame.Height);

        graphics.GraphicsDevice.SetRenderTarget(held);
        graphics.GraphicsDevice.Clear(Color.Transparent);

        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _batch.Draw(frame, Vector2.Zero, Color.White);
        _batch.End();
    }

    /// <summary>
    ///     Lays what is left of the old picture over the new one. Drawn into the frame rather than
    ///     onto the back buffer, so the tube treats the dissolve as part of the picture.
    /// </summary>
    public void Draw()
    {
        if (_fade <= ColourLevels.None || _heldPicture is not { } held)
        {
            return;
        }

        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _batch.Draw(held, Vector2.Zero, new Color(_fade, _fade, _fade, _fade));
        _batch.End();
    }

    /// <remarks>
    ///     Opaque rather than alpha-blended, and premultiplied at the point of drawing. Everything
    ///     the game composes is premultiplied, so a half-faded picture must be half in its colours as
    ///     well as its alpha or it washes out instead of fading.
    /// </remarks>
    private RenderTarget2D HeldFor(int width, int height)
    {
        if (_heldPicture is { } existing && existing.Width == width && existing.Height == height)
        {
            return existing;
        }

        _heldPicture?.Dispose();

        _heldPicture = new RenderTarget2D(
            graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        return _heldPicture;
    }
}
