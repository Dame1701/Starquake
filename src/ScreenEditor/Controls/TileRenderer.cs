// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

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
///     Every surface in the editor draws tiles through here so they all agree. Markers are numbered
///     clear of the sheet and have no artwork, so the editor draws its own panel for them carrying
///     the same icon as the toolbar button that paints them. See <see cref="MarkerCatalogue" />.
/// </remarks>
internal static class TileRenderer
{

    /// <summary>
    ///     The narrowest a marker panel can be and still carry its label. Below this the text would
    ///     be a smudge, and the dashed outline alone still shows something is there.
    /// </summary>
    private const double MinimumLabelWidth = 44;

    /// <summary>The grid the icon path data is measured on.</summary>
    private const double IconGridSize = 24;

    /// <summary>
    ///     How far the icon sits in from the panel's corner, and the size range it may be drawn at.
    ///     Below the minimum an icon is a smudge and is left off; above the maximum it competes with
    ///     the artwork around it.
    /// </summary>
    private const double IconInset = 4;

    /// <inheritdoc cref="IconInset" />
    private const double MinimumIconSize = 14;

    /// <inheritdoc cref="IconInset" />
    private const double MaximumIconSize = 34;

    /// <summary>
    ///     What fraction of the panel's width the icon takes. A third makes the icon what the eye
    ///     lands on, so a marker is recognised by its icon and confirmed by its label.
    /// </summary>
    private const double IconWidthShare = 3;

    /// <summary>
    ///     How far the label sits up from the bottom of the panel. Along the bottom edge keeps it
    ///     clear of the icon in the opposite corner without either needing to know about the other.
    /// </summary>
    private const double LabelInset = 4;

    /// <summary>The narrowest a panel can be and still carry an icon.</summary>
    private const double MinimumIconPanelWidth = 30;

    private static readonly Dictionary<MaterialIconKind, Geometry> IconGeometries = [];

    /// <summary>
    ///     The dashed outline a marker is drawn with. Dashes mark it as an annotation rather than
    ///     scenery without being loud about it.
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
    ///     One icon's outline, ready to draw. Parsed once per icon, because a screen can hold 48
    ///     markers and all of them are redrawn on every repaint.
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
    ///     An overlay taking no space of its own: the label is laid out across the whole panel as
    ///     though the icon were not there. The icon sits in one corner and the label along the
    ///     opposite edge, so reserving width for the icon would needlessly cost a label its line.
    ///     <para>
    ///         Drawn in the accent rather than the marker's own blue, so a square on the screen
    ///         matches the toolbar button that paints it in colour as well as shape.
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

        // Every icon is drawn on the same grid, so one scale factor fits any of them to the corner.
        using DrawingContext.PushedState placed = context.PushTransform(
            Matrix.CreateScale(size / IconGridSize, size / IconGridSize) *
            Matrix.CreateTranslation(panel.X + IconInset, panel.Y + IconInset));

        context.DrawGeometry(EditorColours.AccentBrush, null, geometry);
    }

    /// <summary>
    ///     Draws the editor's stand-in for a tile the game never draws: a tinted panel with a dashed
    ///     edge, the toolbar button's icon in the top corner, and what the position is reserved for
    ///     along the bottom.
    /// </summary>
    private static void DrawMarker(DrawingContext context, MarkerFace marker, Rect destination)
    {
        // Inset so neighbouring markers read as separate panels rather than one long band, and so
        // the tile boundary does not clip the dashed edge in half.
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
            // A label of more than one word wraps rather than being dropped, so a small marker
            // still says which one it is.
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
