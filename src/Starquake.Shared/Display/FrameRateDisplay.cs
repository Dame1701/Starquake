// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Fonts;

namespace Starquake.Display;

/// <summary>
///     The frame rate in the bottom right, switched on and off by F6. It shows how many frames
///     were drawn over the last second and the longest gap between two of them.
/// </summary>
/// <remarks>
///     The longest frame is there because an average hides a hitch. A display at sixty hertz that
///     drops one frame in six still reads as about fifty, while its longest frame doubles to 33ms.
///     <para>
///         This counts draws, so unlike <see cref="ModeAnnouncement" /> it is stepped from the draw
///         pass, with the real time since the last one.
///     </para>
/// </remarks>
internal sealed class FrameRateDisplay
{
    /// <summary>How far in from the right of the composition the reading ends.</summary>
    private const int RightMargin = 24;

    /// <summary>
    ///     How far the bottom of the capitals sits above the bottom of the composition, the same as
    ///     <see cref="ModeAnnouncement" /> so the two sit on one line.
    /// </summary>
    private const int BottomMargin = 20;

    /// <summary>How long each reading is counted over before it is shown.</summary>
    private static readonly TimeSpan CountedOver = TimeSpan.FromSeconds(1);

    private TimeSpan _counted;

    private int _frames;

    private TimeSpan _longest;

    public bool Showing { get; set; }

    /// <summary>The last full second's reading, or empty until one second has been counted.</summary>
    public string Reading { get; private set; } = string.Empty;

    /// <summary>
    ///     Counts one drawn frame. Called while hidden as well, so a reading is ready the moment F6
    ///     shows it.
    /// </summary>
    public void Count(TimeSpan sinceLastDraw)
    {
        _counted += sinceLastDraw;
        _frames++;

        if (sinceLastDraw > _longest)
        {
            _longest = sinceLastDraw;
        }

        if (_counted < CountedOver)
        {
            return;
        }

        int perSecond = (int)Math.Round(_frames / _counted.TotalSeconds);

        Reading = string.Create(CultureInfo.InvariantCulture,
            $"{perSecond} FPS   LONGEST {_longest.TotalMilliseconds:0.0}MS");

        _counted = TimeSpan.Zero;
        _frames = 0;
        _longest = TimeSpan.Zero;
    }

    /// <summary>Draws the reading over the finished picture.</summary>
    /// <remarks>
    ///     <paramref name="transform" /> is read rather than applied, for the reason
    ///     <see cref="ModeAnnouncement.Draw" /> gives.
    /// </remarks>
    public void Draw(BitmapFont font, Matrix transform)
    {
        if (!Showing || Reading.Length == 0)
        {
            return;
        }

        float scale = transform.M11;

        int lineTop = CompositionSize.Height - BottomMargin - font.CapitalTop - font.CapitalHeight;
        float lineLeft = CompositionSize.Width - RightMargin - font.MeasureString(Reading).X;

        font.Begin(BlendState.AlphaBlend);
        font.WriteScaledText(
            (int)(transform.Translation.X + lineLeft * scale),
            (int)(transform.Translation.Y + lineTop * scale),
            Reading,
            Color.White,
            (byte)ColourLevels.Full,
            scale);
        font.End();
    }
}
