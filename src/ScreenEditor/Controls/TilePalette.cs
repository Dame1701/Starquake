// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Starquake.Constants;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     Which set of tiles a <see cref="TilePalette" /> is showing.
/// </summary>
internal enum TilePaletteSource
{

    /// <summary>
    ///     The whole tile sheet, less whatever the filter has hidden. Wraps to fill its width.
    /// </summary>
    Sheet,

    /// <summary>
    ///     The tiles used most recently, in one row. A screen is usually built from a handful of
    ///     tiles, so keeping those to hand saves finding them in the sheet repeatedly.
    /// </summary>
    Recent
}

/// <summary>
///     The tiles available to paint with, drawn from the sheet itself.
/// </summary>
/// <remarks>
///     Drawn directly rather than built from controls. There are several hundred tiles and each is
///     one image with a couple of marks on it, so templated list items would cost far more than they
///     are worth.
/// </remarks>
internal sealed class TilePalette : Control
{

    /// <summary>The space between tiles, which is also the space around the outside.</summary>
    private const double Gap = 6;

    /// <summary>
    ///     How wide a tile is drawn in the recent row. Fixed, so the row keeps its height whatever
    ///     zoom the sheet is shown at.
    /// </summary>
    private const double RecentTileWidth = 56;

    public static readonly StyledProperty<EditorSession?> SessionProperty =
        AvaloniaProperty.Register<TilePalette, EditorSession?>(nameof(Session));

    public static readonly StyledProperty<TilePaletteSource> SourceProperty =
        AvaloniaProperty.Register<TilePalette, TilePaletteSource>(nameof(Source));

    private static readonly Typeface LabelTypeface = new(FontFamily.Default);

    private EditorSession? _attached;

    private int _hoverTile = -1;

    public TilePalette()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    public EditorSession? Session
    {
        get => GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public TilePaletteSource Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private double TileWidth
        => Source == TilePaletteSource.Recent ? RecentTileWidth : Session?.View.PaletteTileWidth ?? 80;

    private double TileHeight
        => TileWidth * TileMetrics.TileHeight / TileMetrics.TileWidth;

    private IReadOnlyList<int> Tiles
        => Session is null
            ? []
            : Source == TilePaletteSource.Recent
                ? Session.Palette.RecentTiles
                : Session.Palette.VisibleTiles;

    public override void Render(DrawingContext context)
    {
        EditorSession? session = Session;
        if (session?.Sheet is null)
        {
            return;
        }

        IReadOnlyList<int> tiles = Tiles;
        int columns = ColumnsFor(Bounds.Width);

        // The recent list is a single row, so anything past its end does not fit.
        int count = Source == TilePaletteSource.Recent ? Math.Min(tiles.Count, columns) : tiles.Count;

        for (int index = 0; index < count; index++)
        {
            int tile = tiles[index];
            Rect destination = TileRect(index, columns);

            if (destination.Top > Bounds.Height || destination.Bottom < 0)
            {
                continue;
            }

            TileRenderer.Draw(context, session.Sheet, tile, destination);

            if (tile == _hoverTile)
            {
                context.FillRectangle(EditorColours.HoverBrush, destination);
            }

            if (tile == session.SelectedTile)
            {
                context.DrawRectangle(null, EditorColours.AccentPen, destination.Inflate(1.5));
            }

            if (TileCatalogue.IsSpecial(tile))
            {
                DrawSpecialMarker(context, destination);
            }

            DrawNumber(context, tile, destination);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        IReadOnlyList<int> tiles = Tiles;

        if (Source == TilePaletteSource.Recent)
        {
            return new Size(
                double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                TileHeight + Gap * 2);
        }

        double width = double.IsInfinity(availableSize.Width) ? TileWidth + Gap * 2 : availableSize.Width;
        int columns = ColumnsFor(width);
        int rows = tiles.Count == 0 ? 0 : (tiles.Count + columns - 1) / columns;

        return new Size(width, rows * (TileHeight + Gap) + Gap);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SessionProperty)
        {
            Reattach(change.GetNewValue<EditorSession?>());
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        EditorSession? session = Session;
        int tile = TileAt(e.GetPosition(this));

        if (session is null || tile < 0)
        {
            return;
        }

        // The session already switches to the brush when a tile is chosen, so doing it here too
        // would put the same rule in two places.
        session.SelectedTile = tile;

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        int tile = TileAt(e.GetPosition(this));
        if (tile == _hoverTile)
        {
            return;
        }

        _hoverTile = tile;

        if (Session is not null)
        {
            Session.View.HoverStatus = tile < 0 ? string.Empty : TileCatalogue.Describe(tile);
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverTile = -1;

        if (Session is not null)
        {
            Session.View.HoverStatus = string.Empty;
        }

        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // Ctrl and the wheel resizes the tiles. Without Ctrl the wheel is left alone so the list
        // still scrolls.
        if (Source != TilePaletteSource.Sheet || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || Session is null)
        {
            return;
        }

        Session.View.PaletteTileWidth += e.Delta.Y * 8;
        e.Handled = true;
    }

    private static void DrawSpecialMarker(DrawingContext context, Rect destination)
    {
        // A tile the game gives a meaning to looks like any other, so it gets a mark.
        var centre = new Point(destination.Right - 7, destination.Y + 7);

        context.DrawEllipse(new SolidColorBrush(EditorColours.Void, 0.7), null, centre, 5, 5);
        context.DrawEllipse(EditorColours.AccentBrush, null, centre, 3, 3);
    }

    private void DrawNumber(DrawingContext context, int tile, Rect destination)
    {
        var text = new FormattedText(
            tile.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            10,
            EditorColours.MutedBrush);

        var backing = new Rect(
            destination.X,
            destination.Bottom - text.Height - 2,
            text.Width + 8,
            text.Height + 2);

        context.FillRectangle(new SolidColorBrush(EditorColours.Void, 0.72), backing, 2);
        context.DrawText(text, new Point(backing.X + 4, backing.Y + 1));
    }

    private int ColumnsFor(double width)
        => Math.Max(1, (int)((width - Gap) / (TileWidth + Gap)));

    private Rect TileRect(int index, int columns)
    {
        int row = index / columns;
        int column = index - row * columns;

        return new Rect(
            Gap + column * (TileWidth + Gap),
            Gap + row * (TileHeight + Gap),
            TileWidth,
            TileHeight);
    }

    private int TileAt(Point position)
    {
        IReadOnlyList<int> tiles = Tiles;
        int columns = ColumnsFor(Bounds.Width);

        int column = (int)((position.X - Gap) / (TileWidth + Gap));
        int row = (int)((position.Y - Gap) / (TileHeight + Gap));

        if (column < 0 || column >= columns || row < 0)
        {
            return -1;
        }

        int count = Source == TilePaletteSource.Recent ? Math.Min(tiles.Count, columns) : tiles.Count;

        int index = row * columns + column;
        if (index < 0 || index >= count)
        {
            return -1;
        }

        // The gaps between tiles belong to no tile, so a click landing in one selects nothing.
        return TileRect(index, columns).Contains(position) ? tiles[index] : -1;
    }

    private void Reattach(EditorSession? session)
    {
        if (_attached is not null)
        {
            _attached.Invalidated -= OnSessionInvalidated;
        }

        _attached = session;

        if (_attached is not null)
        {
            _attached.Invalidated += OnSessionInvalidated;
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    private void OnSessionInvalidated(object? sender, EventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
    }
}
