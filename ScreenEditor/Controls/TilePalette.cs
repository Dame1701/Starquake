// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
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
    ///     The tiles reached for most recently, in one row. A screen is usually built from a handful
    ///     of tiles, so having those to hand saves finding them in the sheet over and over.
    /// </summary>
    Recent
}

/// <summary>
///     The tiles available to paint with, drawn from the sheet itself.
/// </summary>
/// <remarks>
///     Drawn rather than built from controls because there are several hundred tiles and each is a
///     single image with a couple of marks on it; four hundred templated list items to achieve the
///     same thing would cost far more than it is worth.
/// </remarks>
internal sealed class TilePalette : Control
{

    /// <summary>
    ///     The space between tiles, which is also the space around the outside.
    /// </summary>
    private const double Gap = 6;

    /// <summary>
    ///     How wide a tile is drawn in the recent row. Fixed, because the row is meant to stay the
    ///     same height whatever the sheet is being shown at.
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
        => Source == TilePaletteSource.Recent ? RecentTileWidth : Session?.PaletteTileWidth ?? 80;

    private double TileHeight
        => TileWidth * TileMetrics.TileHeight / TileMetrics.TileWidth;

    private IReadOnlyList<int> Tiles
        => Session is null
            ? []
            : Source == TilePaletteSource.Recent
                ? Session.RecentTiles
                : Session.VisibleTiles;

    public override void Render(DrawingContext context)
    {
        EditorSession? session = Session;
        if (session?.Sheet is null)
        {
            return;
        }

        IReadOnlyList<int> tiles = Tiles;
        int columns = ColumnsFor(Bounds.Width);

        // The recent row is one row: anything past the end of it is older than the space allows.
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

        // Picking a tile is almost always the prelude to painting with it, and the session puts the
        // brush back in hand for us - choosing something to lay down is what selects the brush now, so
        // asserting it a second time here would be the same rule in two places.
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
            Session.HoverStatus = tile < 0 ? string.Empty : TileCatalogue.Describe(tile);
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverTile = -1;

        if (Session is not null)
        {
            Session.HoverStatus = string.Empty;
        }

        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // Ctrl and the wheel resizes the tiles, which is how every other program that shows a grid
        // of images behaves. Without Ctrl the wheel is left alone so the list still scrolls.
        if (Source != TilePaletteSource.Sheet || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || Session is null)
        {
            return;
        }

        Session.PaletteTileWidth += e.Delta.Y * 8;
        e.Handled = true;
    }

    private static void DrawSpecialMarker(DrawingContext context, Rect destination)
    {
        // A tile the game gives a meaning to looks like any other in the sheet, so it gets a mark.
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

        // The gaps between tiles are not part of any tile, so a click that lands in one selects
        // nothing rather than the tile above and to the left of it.
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
