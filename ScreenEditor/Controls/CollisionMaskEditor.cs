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
///     The selected tile shown large, with its collision cells marked and clickable.
/// </summary>
/// <remarks>
///     Collision belongs to a tile <i>image</i>, not to a position on a screen: the game tests the
///     blob against a mask attached to the tile number, so making the top of a platform solid here
///     makes it solid everywhere that platform has ever been painted. That is worth knowing before
///     clicking, which is why the panel says so rather than leaving it to be discovered.
/// </remarks>
public sealed class CollisionMaskEditor : Control
{

    /// <summary>
    ///     Room under the tile for the line naming it.
    /// </summary>
    private const double CaptionHeight = 22;

    public static readonly StyledProperty<EditorSession?> SessionProperty =
        AvaloniaProperty.Register<CollisionMaskEditor, EditorSession?>(nameof(Session));

    private static readonly Typeface LabelTypeface = new(FontFamily.Default);

    private static readonly IPen CellPen = new Pen(new SolidColorBrush(Colors.White, 0.22));

    private EditorSession? _attached;

    private int _hoverCellX = -1;

    private int _hoverCellY = -1;

    public CollisionMaskEditor()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    public EditorSession? Session
    {
        get => GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        EditorSession? session = Session;
        if (session?.Sheet is null || !session.Sheet.Contains(session.SelectedTile))
        {
            return;
        }

        Rect area = TileArea();

        context.FillRectangle(EditorColours.VoidBrush, area);
        TileRenderer.Draw(context, session.Sheet, session.SelectedTile, area);

        double cellWidth = area.Width / TileMetrics.CollisionCellsAcrossTile;
        double cellHeight = area.Height / TileMetrics.CollisionCellsDownTile;

        for (int cellY = 0; cellY < TileMetrics.CollisionCellsDownTile; cellY++)
        {
            for (int cellX = 0; cellX < TileMetrics.CollisionCellsAcrossTile; cellX++)
            {
                var cell = new Rect(
                    area.X + cellX * cellWidth,
                    area.Y + cellY * cellHeight,
                    cellWidth,
                    cellHeight);

                if (session.Document.IsSolid(session.SelectedTile, cellX, cellY))
                {
                    context.FillRectangle(EditorColours.CollisionBrush, cell);
                }

                if (cellX == _hoverCellX && cellY == _hoverCellY)
                {
                    context.FillRectangle(EditorColours.HoverBrush, cell);
                }

                context.DrawRectangle(null, CellPen, cell);
            }
        }

        context.DrawRectangle(null, EditorColours.BorderPen, area);

        DrawCaption(context, session, area);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsInfinity(availableSize.Width) ? 220 : availableSize.Width;

        return new Size(width, width * TileMetrics.TileHeight / TileMetrics.TileWidth + CaptionHeight);
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
        if (session is null || !TryHitTest(e.GetPosition(this), out int cellX, out int cellY))
        {
            return;
        }

        session.Undo.Begin("collision");
        session.ToggleCollision(session.SelectedTile, cellX, cellY);
        session.Undo.Commit();

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        bool inside = TryHitTest(e.GetPosition(this), out int cellX, out int cellY);

        if (!inside)
        {
            cellX = -1;
            cellY = -1;
        }

        if (cellX == _hoverCellX && cellY == _hoverCellY)
        {
            return;
        }

        _hoverCellX = cellX;
        _hoverCellY = cellY;

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverCellX = -1;
        _hoverCellY = -1;

        InvalidateVisual();
    }

    private Rect TileArea()
    {
        var available = new Size(Bounds.Width, Math.Max(0, Bounds.Height - CaptionHeight));

        double scale = Math.Min(
            available.Width / TileMetrics.TileWidth,
            available.Height / TileMetrics.TileHeight);

        double width = TileMetrics.TileWidth * scale;
        double height = TileMetrics.TileHeight * scale;

        return new Rect(Math.Floor((Bounds.Width - width) / 2), 0, Math.Floor(width), Math.Floor(height));
    }

    private void DrawCaption(DrawingContext context, EditorSession session, Rect area)
    {
        var text = new FormattedText(
            TileCatalogue.Describe(session.SelectedTile),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            11,
            TileCatalogue.IsSpecial(session.SelectedTile)
                ? EditorColours.AccentBrush
                : EditorColours.MutedBrush);

        context.DrawText(text, new Point(
            Math.Max(0, (Bounds.Width - text.Width) / 2),
            area.Bottom + 6));
    }

    private bool TryHitTest(Point position, out int cellX, out int cellY)
    {
        cellX = -1;
        cellY = -1;

        EditorSession? session = Session;
        if (session?.Sheet is null || !session.Sheet.Contains(session.SelectedTile))
        {
            return false;
        }

        Rect area = TileArea();
        if (!area.Contains(position))
        {
            return false;
        }

        cellX = Math.Clamp(
            (int)((position.X - area.X) / (area.Width / TileMetrics.CollisionCellsAcrossTile)),
            0,
            TileMetrics.CollisionCellsAcrossTile - 1);

        cellY = Math.Clamp(
            (int)((position.Y - area.Y) / (area.Height / TileMetrics.CollisionCellsDownTile)),
            0,
            TileMetrics.CollisionCellsDownTile - 1);

        return true;
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

        InvalidateVisual();
    }

    private void OnSessionInvalidated(object? sender, EventArgs e) => InvalidateVisual();
}
