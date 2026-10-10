// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Display;

/// <summary>
///     Saves the picture the player is looking at as a PNG. Asked for from the update pass and taken
///     at the end of the draw pass, because the back buffer only holds a finished frame there.
/// </summary>
/// <remarks>
///     The back buffer rather than the frame <see cref="ScreenPresenter" /> composes into, so a shot
///     carries the cathode ray tube's scanlines and glow as the player sees them. The tube is applied
///     on the way to the buffer, so there is no texture holding the finished picture to read instead.
/// </remarks>
internal sealed class Screenshots
{
    private bool _asked;

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
    public bool? SaveIfAsked(GraphicsDevice device)
    {
        if (!_asked)
        {
            return null;
        }

        _asked = false;

        try
        {
            Save(device);

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <remarks>
    ///     Reading the buffer and encoding it both happen on this thread, so a shot costs a frame.
    ///     That is a keypress the player made rather than something the game does to itself, and
    ///     encoding needs the pixels the device will have moved on from by the next one.
    /// </remarks>
    private static void Save(GraphicsDevice device)
    {
        PresentationParameters buffer = device.PresentationParameters;

        Color[] pixels = new Color[buffer.BackBufferWidth * buffer.BackBufferHeight];
        device.GetBackBufferData(pixels);

        MakeOpaque(pixels);

        Directory.CreateDirectory(Folder);

        using var picture = new Texture2D(device, buffer.BackBufferWidth, buffer.BackBufferHeight);
        picture.SetData(pixels);

        using FileStream file = File.Create(NextFilePath());
        picture.SaveAsPng(file, buffer.BackBufferWidth, buffer.BackBufferHeight);
    }

    /// <summary>Turns every pixel fully opaque, which is what the player was looking at.</summary>
    /// <remarks>
    ///     The buffer's alpha is whatever composing the picture happened to leave behind, and the
    ///     display never reads it. A PNG does. The status panel is the worst of it: its artwork is
    ///     almost entirely transparent, reading as black only because the frame beneath was cleared
    ///     to black, and <c>ScreenEffectEngine</c> composes each view port with
    ///     <see cref="BlendState.Opaque" />, so the panel's own alpha is written straight through to
    ///     the buffer. Saved as it stands, the panel comes out see-through and shows whatever the
    ///     picture is viewed against rather than the black that was on screen.
    /// </remarks>
    private static void MakeOpaque(Color[] pixels)
    {
        for (int pixel = 0; pixel < pixels.Length; pixel++)
        {
            pixels[pixel].A = ColourLevels.Full;
        }
    }

    /// <summary>
    ///     Named for the moment it was taken, down to the millisecond, so no two shots can land on
    ///     one name however fast they are asked for.
    /// </summary>
    private static string NextFilePath()
        => Path.Combine(Folder, $"Starquake {DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}.png");
}
