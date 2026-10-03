// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Material.Icons;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     Draws one tile, wherever a tile needs drawing.
/// </summary>
/// <remarks>
///     Every surface in the editor shows the same tiles and has to agree about what they look like,
///     including the ones the game never draws. There is no artwork at all for those - a marker is
///     numbered clear of the sheet - so the editor draws its own panel for them, carrying the same
///     icon as the toolbar button that paints them. See <see cref="MarkerCatalogue" />.
/// </remarks>
internal static class TileRenderer
{

    /// <summary>
    ///     The smallest a marker can be drawn before its label is dropped. Below this the text would
    ///     be a smudge, and the outline alone still says something is there.
    /// </summary>
    private const double MinimumLabelWidth = 44;

    /// <summary>
    ///     The grid the icons are drawn on, which is what their path data is measured in.
    /// </summary>
    private const double IconGridSize = 24;

    /// <summary>
    ///     How far the icon sits in from the panel's corner, and the range it may be drawn at. Below
    ///     the minimum an icon is a smudge, so it is left off rather than drawn illegibly; above the
    ///     maximum it would start competing with the artwork around it.
    /// </summary>
    private const double IconInset = 4;

    /// <inheritdoc cref="IconInset" />
    private const double MinimumIconSize = 14;

    /// <inheritdoc cref="IconInset" />
    private const double MaximumIconSize = 34;

    /// <summary>
    ///     How much of the panel's width the icon takes. A third makes it the thing the eye lands on,
    ///     which is the point of it: a marker is recognised by its icon now and read by its label.
    /// </summary>
    private const double IconWidthShare = 3;

    /// <summary>
    ///     How far the label sits up from the bottom of the panel. It is down there to stay clear of
    ///     the icon in the opposite corner without either of them having to know about the other.
    /// </summary>
    private const double LabelInset = 4;

    /// <summary>
    ///     How wide a panel has to be before the icon earns its corner. Narrower than this and the
    ///     dashed outline alone says something is there.
    /// </summary>
    private const double MinimumIconPanelWidth = 30;

    private static readonly Dictionary<MaterialIconKind, Geometry> IconGeometries = [];

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
        MarkerFace? marker = MarkerCatalogue.Face(tileImage);

        if (marker is null)
        {
            sheet.DrawTile(context, tileImage, destination);
            return;
        }

        DrawMarker(context, marker, destination);
    }

    /// <summary>
    ///     One icon's outline, ready to draw. Parsed once per icon: a screen can hold forty-eight
    ///     markers and every one of them is redrawn on each repaint.
    /// </summary>
    private static Geometry IconGeometry(MaterialIconKind icon)
    {
        if (IconGeometries.TryGetValue(icon, out Geometry? cached))
        {
            return cached;
        }

        Geometry parsed = Geometry.Parse(MaterialIconDataProvider.GetData(icon));
        IconGeometries[icon] = parsed;

        return parsed;
    }

    /// <summary>
    ///     Draws the marker's icon in the top left of its panel, at the size it will still read at.
    /// </summary>
    /// <remarks>
    ///     An overlay, taking no space of its own: the label is laid out across the whole panel as
    ///     though the icon were not there. Reserving width for it is what this first did, and it cost
    ///     "SPACER" its single line - the icon is in one corner and the label along the opposite edge,
    ///     so there was nothing to reserve it from.
    ///     <para>
    ///         Drawn in the accent rather than the marker's own blue, so that a square on the screen
    ///         and the toolbar button that paints it are the same colour as well as the same shape.
    ///     </para>
    /// </remarks>
    private static void DrawIcon(DrawingContext context, MaterialIconKind icon, Rect panel)
    {
        double size = Math.Clamp(panel.Width / IconWidthShare, MinimumIconSize, MaximumIconSize);

        if (panel.Width < MinimumIconPanelWidth || panel.Height < size + IconInset * 2)
        {
            return;
        }

        Geometry geometry = IconGeometry(icon);

        // The icons are drawn on a 24 unit grid, so one scale factor fits any of them to the corner.
        using DrawingContext.PushedState placed = context.PushTransform(
            Matrix.CreateScale(size / IconGridSize, size / IconGridSize) *
            Matrix.CreateTranslation(panel.X + IconInset, panel.Y + IconInset));

        context.DrawGeometry(EditorColours.AccentBrush, null, geometry);
    }

    /// <summary>
    ///     The editor's stand-in for a tile the game does not draw: a soft tinted panel, a dashed
    ///     edge, the icon from the toolbar button that paints it in the top corner, and the word for
    ///     what the position is reserved for along the bottom.
    /// </summary>
    private static void DrawMarker(DrawingContext context, MarkerFace marker, Rect destination)
    {
        // Inset so that neighbouring markers read as separate panels rather than one long band, and
        // so the dashed edge is not clipped in half by the tile boundary.
        Rect panel = destination.Deflate(Math.Min(3, destination.Width / 16));
        double corner = Math.Min(5, panel.Width / 10);

        context.DrawRectangle(MarkerFillBrush, MarkerPen, panel, corner, corner);

        DrawIcon(context, marker.Icon, panel);

        if (panel.Width < MinimumLabelWidth)
        {
            return;
        }

        double available = panel.Width - 6;

        var text = new FormattedText(
            marker.Label,
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
            panel.Bottom - text.Height - LabelInset));
    }
}
