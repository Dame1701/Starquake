// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Structure;
using Starquake.Tuning;

namespace Starquake.Display;

/// <summary>
///     Catches the composed picture in a render target and hands it to the back buffer, through
///     <c>Shaders/Crt.fx</c> when a cathode ray tube is asked for and straight across when it is
///     not.
/// </summary>
/// <remarks>
///     The catch is unconditional, and that is deliberate even though the plain mode could draw to
///     the back buffer directly. Two things want the finished picture as a texture rather than as
///     something already presented: the tube's glow and curve have to sample it, and
///     <see cref="ArtworkCrossfade" /> has to keep the frame before an artwork swap so it can be
///     dissolved into the frame after. Making the catch depend on the tube's mode would mean the
///     crossfade only worked while the tube was on. The cost is one full-screen blit a frame in
///     the plain mode, against a game that already composes through three render targets.
///     <para>
///         What it replaced was a 1280x800 texture of alternating half-black rows blended on top,
///         which cost four megabytes of video memory to say "every other row is darker" and was
///         then resampled by the presentation transform's fractional scale into a flat grey wash
///         with a slow beat running through it.
///     </para>
/// </remarks>
internal sealed class ScreenPresenter(
    GraphicsDeviceManager graphics,
    ResourceManager resources,
    PresentationSettings presentation)
{
    private readonly SpriteBatch _batch = new(graphics.GraphicsDevice);

    private RenderTarget2D? _frame;

    private RenderTarget2D? _afterglow;

    /// <summary>
    ///     The target the picture is composed into, sized to the back buffer and rebuilt whenever
    ///     the window changes. Still holding the last frame drawn when this is called, which is
    ///     what lets <see cref="ArtworkCrossfade" /> take a copy before the clear.
    /// </summary>
    /// <remarks>
    ///     <c>PreserveContents</c> because the game switches render targets several times a frame -
    ///     every <c>SelectRenderViewPort</c> does - so the picture has to survive being left and
    ///     come back to.
    /// </remarks>
    public RenderTarget2D EnsureFrame()
    {
        PresentationParameters buffer = graphics.GraphicsDevice.PresentationParameters;

        if (_frame is { } existing &&
            existing.Width == buffer.BackBufferWidth &&
            existing.Height == buffer.BackBufferHeight)
        {
            return existing;
        }

        _frame?.Dispose();

        _frame = new RenderTarget2D(
            graphics.GraphicsDevice, buffer.BackBufferWidth, buffer.BackBufferHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        return _frame;
    }

    /// <summary>
    ///     Points the device at the frame and empties it, ready for the picture to be drawn.
    /// </summary>
    /// <remarks>
    ///     The boot screen draws straight to whatever target is current and the screen effect
    ///     engine has no view ports to compose yet, so this is the only thing that points it at
    ///     the frame.
    /// </remarks>
    public RenderTarget2D BeginFrame()
    {
        RenderTarget2D frame = EnsureFrame();

        graphics.GraphicsDevice.SetRenderTarget(frame);
        graphics.GraphicsDevice.Clear(presentation.ScreenClearColour);

        return frame;
    }

    /// <summary>
    ///     Draws the caught frame to the back buffer.
    /// </summary>
    /// <param name="sinceLastDraw">
    ///     How long ago the last frame was drawn, for the phosphor to decay by. Real elapsed time:
    ///     <c>GameHost</c> sets <c>IsFixedTimeStep</c> false and draws once per vertical retrace,
    ///     so this is what actually passed and not a nominal frame.
    /// </param>
    public void Present(TimeSpan sinceLastDraw)
    {
        RenderTarget2D frame = EnsureFrame();

        GlowOnFromTheLastFrame(frame, sinceLastDraw);

        graphics.GraphicsDevice.SetRenderTarget(null);
        graphics.GraphicsDevice.Clear(presentation.ScreenClearColour);

        Effect? crt = EffectFor(presentation.CrtMode, frame.Width, frame.Height);

        // Opaque, and one texel of the frame to one pixel of the buffer: this is the whole picture
        // and there is nothing underneath it to blend with.
        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp,
            null, null, crt);
        _batch.Draw(frame, Vector2.Zero, Color.White);
        _batch.End();
    }

    /// <summary>
    ///     Lays what is left of the last picture into this one, and keeps the result to lay into
    ///     the next. A phosphor goes on glowing after the beam has moved on, which is what makes
    ///     something crossing the screen leave a short streak behind it rather than appearing in a
    ///     new place each frame.
    /// </summary>
    /// <remarks>
    ///     A feedback buffer: what is kept has already been decayed, so the trail falls away
    ///     exponentially rather than ending abruptly, and nothing has to remember how many frames
    ///     ago anything happened.
    ///     <para>
    ///         Done to the picture before it reaches the shader, so the streak is scanned and
    ///         bowed along with everything else rather than sitting on the glass in front of it.
    ///     </para>
    /// </remarks>
    private void GlowOnFromTheLastFrame(RenderTarget2D frame, TimeSpan sinceLastDraw)
    {
        if (!CrtSettings.ShowsAfterglow(presentation.CrtMode))
        {
            return;
        }

        RenderTarget2D afterglow = AfterglowFor(frame.Width, frame.Height);

        int retained = (int)(CrtSettings.AfterglowRetainedOver(sinceLastDraw.TotalSeconds)
            * ColourLevels.Full);

        // Premultiplied in all four channels: everything the game composes is premultiplied, so a
        // half-faded picture has to be half in its colours as well as in its alpha.
        graphics.GraphicsDevice.SetRenderTarget(frame);

        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _batch.Draw(afterglow, Vector2.Zero, new Color(retained, retained, retained, retained));
        _batch.End();

        graphics.GraphicsDevice.SetRenderTarget(afterglow);

        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _batch.Draw(frame, Vector2.Zero, Color.White);
        _batch.End();
    }

    private RenderTarget2D AfterglowFor(int width, int height)
    {
        if (_afterglow is { } existing && existing.Width == width && existing.Height == height)
        {
            return existing;
        }

        _afterglow?.Dispose();

        _afterglow = new RenderTarget2D(
            graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        return _afterglow;
    }

    /// <summary>
    ///     The shader pass for a mode, or null for <see cref="CrtMode.None" />, which wants the
    ///     picture across unchanged and so wants no shader at all.
    /// </summary>
    private Effect? EffectFor(CrtMode mode, int backBufferWidth, int backBufferHeight)
    {
        if (mode == CrtMode.None)
        {
            return null;
        }

        Effect crt = resources.GetEffect(Assets.Shaders.Crt);

        float scale = CrtSettings.ScaleFor(backBufferWidth, backBufferHeight);

        crt.Parameters["BackBufferSize"].SetValue(
            new Vector2(backBufferWidth, backBufferHeight));
        crt.Parameters["CompositionRect"].SetValue(
            CrtSettings.CompositionRectFor(backBufferWidth, backBufferHeight));

        crt.Parameters["ScanlinePeriod"].SetValue((float)CrtSettings.ScanlinePeriodFor(scale));
        crt.Parameters["ScanlineDepth"].SetValue(CrtSettings.ScanlineDepth);
        crt.Parameters["Gain"].SetValue(CrtSettings.GainFor(mode));

        crt.Parameters["MaskPeriod"].SetValue(CrtSettings.MaskPeriod);
        crt.Parameters["MaskDepth"].SetValue(CrtSettings.MaskDepth);

        // The reach is given in output pixels and the shader samples in texture coordinates, so it
        // is handed over already divided by the buffer it will be read from.
        crt.Parameters["GlowStep"].SetValue(new Vector2(
            CrtSettings.GlowRadius / backBufferWidth,
            CrtSettings.GlowRadius / backBufferHeight));
        crt.Parameters["GlowAmount"].SetValue(CrtSettings.GlowAmount);

        crt.Parameters["Curvature"].SetValue(CrtSettings.Curvature);
        crt.Parameters["VignetteAmount"].SetValue(CrtSettings.VignetteAmount);

        crt.CurrentTechnique = crt.Techniques[CrtSettings.TechniqueFor(mode)];

        return crt;
    }
}
