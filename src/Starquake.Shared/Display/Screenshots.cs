// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Display;

/// <summary>
///     Saves the picture the player is looking at as a PNG. Asked for from the update pass and taken
///     at the end of the draw pass, once the frame is finished.
/// </summary>
/// <remarks>
///     The picture is drawn a second time, through the cathode ray tube, into a render target of its
///     own, so a shot carries the tube's scanlines and glow as the player sees them. It is not read
///     off the back buffer: MonoGame 3.8.6's Vulkan backend reads the buffer by submitting the frame
///     early on the swapchain's own semaphores, and the real present then submits on them again.
///     MoltenVK took that a few seconds later as a crash in <c>vkQueueSubmit</c>. Reading a render
///     target flushes without them.
///     <para>
///         The read has to come before anything is drawn to the back buffer. The flush submits
///         everything recorded so far, and drawing on the swapchain image that early would be
///         drawing on an image that may not have been handed over yet.
///     </para>
/// </remarks>
internal sealed class Screenshots
{
    private bool _asked;

    /// <summary>
    ///     Kept from one shot to the next rather than thrown away after each, because 3.8.6's Vulkan
    ///     backend cannot safely dispose of a resource the frame being recorded has used.
    /// </summary>
    private RenderTarget2D? _picture;

    /// <summary>
    ///     Beside the player's own pictures rather than the executable. On a Mac the game runs from
    ///     inside a signed <c>.app</c>, and on every platform the build output is thrown away and
    ///     rewritten.
    /// </summary>
    public static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "Starquake",
        "Screenshots");

    public void Ask() => _asked = true;

    /// <summary>Takes the shot, if one has been asked for.</summary>
    /// <returns>
    ///     Whether a file was written, or null if nothing was asked for. The caller is the one that
    ///     announces it, so that the message is shown after the pixels have been read rather than
    ///     being drawn into the picture.
    /// </returns>
    public bool? SaveIfAsked(GraphicsDevice device, ScreenPresenter presenter)
    {
        if (!_asked)
        {
            return null;
        }

        _asked = false;

        RenderTarget2D picture = PictureFor(device);
        presenter.PresentInto(picture);

        try
        {
            Save(picture);

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>The target to draw the shot into, sized to the back buffer.</summary>
    private RenderTarget2D PictureFor(GraphicsDevice device)
    {
        PresentationParameters buffer = device.PresentationParameters;

        if (_picture is { } existing &&
            existing.Width == buffer.BackBufferWidth &&
            existing.Height == buffer.BackBufferHeight)
        {
            return existing;
        }

        _picture?.Dispose();
        _picture = new RenderTarget2D(device, buffer.BackBufferWidth, buffer.BackBufferHeight);

        return _picture;
    }

    /// <remarks>
    ///     Reading the target and encoding it both happen on this thread, so a shot costs a frame.
    ///     That is a keypress the player made rather than something the game does to itself.
    /// </remarks>
    private static void Save(RenderTarget2D picture)
    {
        Directory.CreateDirectory(Folder);

        using FileStream file = File.Create(NextFilePath());
        picture.SaveAsPng(file, picture.Width, picture.Height);
    }

    /// <summary>
    ///     Named for the moment it was taken, down to the millisecond, so no two shots can land on
    ///     one name however fast they are asked for.
    /// </summary>
    private static string NextFilePath()
        => Path.Combine(Folder, $"Starquake {DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}.png");
}
