// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     Draws one tile, wherever a tile needs drawing.
/// </summary>
/// <remarks>
///     Every surface in the editor shows the same tiles and has to agree about what they look like,
///     including the four the game never draws. Those get a marker of the editor's own rather than
///     the flat placeholder block the sheet holds for them - see <see cref="TileCatalogue" />.
/// </remarks>
internal static class TileRenderer
{

    /// <summary>
    ///     The smallest a marker can be drawn before its label is dropped. Below this the text would
    ///     be a smudge, and the outline alone still says something is there.
    /// </summary>
    private const double MinimumLabelWidth = 44;

    /// <summary>
    ///     The dashed outline a marker is drawn with. Dashes say "this is an annotation, not
    ///     scenery" without needing to be loud about it.
    /// </summary>
    private static readonly IPen MarkerPen = new Pen(
        new SolidColorBrush(EditorColours.Marker, 0.85),
        1.25,
        new DashStyle([3, 2.5], 0));

    private static readonly IBrush MarkerFillBrush = new SolidColorBrush(EditorColours.Marker, 0.14);

    private static readonly IBrush MarkerTextBrush = new SolidColorBrush(EditorColours.Marker);

    private static readonly Typeface MarkerTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

    public static void Draw(DrawingContext context, TileSheet sheet, int tileImage, Rect destination)
    {
        string? marker = TileCatalogue.MarkerLabel(tileImage);

        if (marker is null)
        {
            sheet.DrawTile(context, tileImage, destination);
            return;
        }

        DrawMarker(context, marker, destination);
    }

    /// <summary>
    ///     The editor's stand-in for a tile the game does not draw: a soft tinted panel, a dashed
    ///     edge, and the word for what the position is reserved for.
    /// </summary>
    private static void DrawMarker(DrawingContext context, string label, Rect destination)
    {
        // Inset so that neighbouring markers read as separate panels rather than one long band, and
        // so the dashed edge is not clipped in half by the tile boundary.
        Rect panel = destination.Deflate(Math.Min(3, destination.Width / 16));
        double corner = Math.Min(5, panel.Width / 10);

        context.DrawRectangle(MarkerFillBrush, MarkerPen, panel, corner, corner);

        if (panel.Width < MinimumLabelWidth)
        {
            return;
        }

        double available = panel.Width - 6;

        var text = new FormattedText(
            label,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            MarkerTypeface,
            Math.Clamp(panel.Width / 7, 8, 13),
            MarkerTextBrush)
        {
            // A label of more than one word wraps rather than being dropped, so that a marker drawn
            // small still says which one it is.
            MaxTextWidth = available,
            TextAlignment = TextAlignment.Center
        };

        if (text.Width > available || text.Height > panel.Height - 4)
        {
            return;
        }

        // The text is centred within MaxTextWidth already, so only the block itself is placed.
        context.DrawText(text, new Point(
            panel.X + (panel.Width - available) / 2,
            panel.Y + (panel.Height - text.Height) / 2));
    }
}
