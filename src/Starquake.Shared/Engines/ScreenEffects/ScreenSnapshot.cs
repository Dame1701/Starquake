// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Engines.ScreenEffects;

/// <summary>
///     A copy of what a view port was showing, kept so an effect can still draw the picture it is
///     taking the player away from after the game has moved on to the next one.
/// </summary>
/// <remarks>
///     A view port's own target is reused every frame, so by the time a transition draws, the screen
///     being left has already been painted over. The copy costs one target the size of the play area
///     and one blit when the effect starts.
///     <para>
///         Taken during <c>Update</c>, before anything has drawn this frame, so what the source
///         target still holds is the last frame actually shown: the screen being left, even though
///         the screen number has moved on. The targets are created
///         <see cref="RenderTargetUsage.PreserveContents" />, which is what guarantees that.
///     </para>
/// </remarks>
internal sealed class ScreenSnapshot(GraphicsDeviceManager graphics)
{
    private SpriteBatch? _copier;
    private RenderTarget2D? _picture;

    /// <summary>The last picture taken, or null before anything has asked for one.</summary>
    public Texture2D? Picture => _picture;

    /// <summary>Copies what <paramref name="source" /> is holding now.</summary>
    public void Capture(RenderTarget2D source)
    {
        GraphicsDevice device = graphics.GraphicsDevice;

        _picture ??= new RenderTarget2D(device, source.Width, source.Height, false, SurfaceFormat.Color,
            DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
        _copier ??= new SpriteBatch(device);

        device.SetRenderTarget(_picture);
        device.Clear(Color.Black);

        _copier.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _copier.Draw(source, Vector2.Zero, Color.White);
        _copier.End();

        device.SetRenderTarget(null);
    }
}
