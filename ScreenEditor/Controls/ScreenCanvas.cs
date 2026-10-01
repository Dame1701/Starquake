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
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     The screen being edited, drawn large with the eight screens around it showing faintly at its
///     edges.
/// </summary>
/// <remarks>
///     Starquake's world does not scroll - the blob walks off one edge and the next screen replaces
///     it whole - so a screen has to be built to line up with its neighbours without ever being seen
///     alongside them in the game. That is what the border of neighbouring tiles is for: a platform
///     that stops a tile short of the edge, or a tunnel mouth with nothing on the other side of it,
///     is obvious here and invisible on a screen drawn on its own.
/// </remarks>
internal sealed class ScreenCanvas : Control
{

    /// <summary>
    ///     How much of a neighbouring screen to show, in tiles. One is enough to line scenery up
    ///     against and small enough to leave the screen being edited the obvious subject.
    /// </summary>
    private const int NeighbourTiles = 1;

    /// <summary>
    ///     How many chequer squares make up one collision cell, each way. Two, so the square is sixteen
    ///     game pixels - fine enough to read as a texture rather than as blocks, and an even number of
    ///     squares to a tile either way, which is what lets the pattern carry across a tile boundary.
    /// </summary>
    private const int ChequerSquaresPerCell = 2;

    /// <inheritdoc cref="ChequerSquaresPerCell" />
    private const int ChequerSquaresAcrossTile = TileMetrics.CollisionCellsAcrossTile * ChequerSquaresPerCell;

    /// <inheritdoc cref="ChequerSquaresPerCell" />
    private const int ChequerSquaresDownTile = TileMetrics.CollisionCellsDownTile * ChequerSquaresPerCell;

    public static readonly StyledProperty<EditorSession?> SessionProperty =
        AvaloniaProperty.Register<ScreenCanvas, EditorSession?>(nameof(Session));

    private static readonly Typeface LabelTypeface = new(FontFamily.Default);

    /// <summary>
    ///     For the teleport code written across a booth. Heavier than the tile numbers because it is
    ///     drawn over artwork rather than over a badge in the corner of a tile.
    /// </summary>
    private static readonly Typeface CodeTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

    /// <summary>
    ///     The planet core's area: a wash with the nine elements picked out inside it.
    /// </summary>
    private static readonly IBrush CoreFillBrush = new SolidColorBrush(EditorColours.Accent, 0.16);

    /// <inheritdoc cref="CoreFillBrush" />
    private static readonly IPen CoreElementPen = new Pen(new SolidColorBrush(EditorColours.Accent, 0.5));

    private EditorSession? _attached;

    private int _hoverX = -1;

    private int _hoverY = -1;

    private EditorTool _strokeTool;

    private bool _placingCore;

    /// <summary>
    ///     Where the pointer is in the screen's own pixels, or null when it is off the screen. The tile
    ///     the pointer is over is not enough for the core, which is placed between tiles.
    /// </summary>
    private Point? _pointerOnScreen;

    private bool _stroking;

    public ScreenCanvas()
    {
        Focusable = true;

        // The artwork is pixel art and the overlays are hairlines; both want to land on whole
        // pixels rather than be smoothed across them.
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
        context.FillRectangle(EditorColours.VoidBrush, new Rect(Bounds.Size));

        EditorSession? session = Session;
        if (session?.Sheet is null)
        {
            DrawPlaceholder(context);
            return;
        }

        Layout layout = Layout.For(Bounds.Size, session.ShowNeighbours);

        if (session.ShowNeighbours)
        {
            DrawNeighbours(context, session, layout);
        }

        DrawScreen(context, session, layout);

        if (session.ShowCollision)
        {
            DrawCollision(context, session, layout);
        }

        if (session.ShowGrid)
        {
            DrawGrid(context, layout);
        }

        if (session.ShowTileNumbers)
        {
            DrawTileNumbers(context, session, layout);
        }

        DrawCore(context, session, layout);

        DrawTeleportCodes(context, session, layout);

        DrawHover(context, session, layout);

        context.DrawRectangle(null, EditorColours.ScreenEdgePen, layout.Screen);
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
        if (session?.Sheet is null)
        {
            return;
        }

        Focus();

        PointerPoint point = e.GetCurrentPoint(this);
        if (!TryHitTest(point.Position, out int x, out int y))
        {
            return;
        }

        // Shift and click on a booth sets its destination code. Taken before the tool is applied, so
        // that the gesture which opens the dialog cannot also paint over the booth it was aimed at.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            session.Document.GetTile(session.CurrentScreen, x, y) == (int)ActionTiles.TeleportBooth)
        {
            session.RequestTeleportCodeEdit();
            e.Handled = true;

            return;
        }

        _strokeTool = ToolFor(point, e.KeyModifiers, session.Tool);

        // The core is a position rather than a tile, so it is placed from the pointer in screen pixels
        // and does not go through the tile-based tools at all.
        if (_strokeTool == EditorTool.Core)
        {
            _placingCore = true;
            session.Undo.Begin("core position");
            PlaceCore(session, point.Position);

            e.Pointer.Capture(this);
            e.Handled = true;

            return;
        }

        _stroking = true;

        session.Undo.Begin(DescribeStroke(_strokeTool));
        session.ApplyTool(_strokeTool, x, y);

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        EditorSession? session = Session;
        if (session is null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        bool inside = TryHitTest(position, out int x, out int y);
        bool moved = inside ? x != _hoverX || y != _hoverY : _hoverX >= 0;

        _hoverX = inside ? x : -1;
        _hoverY = inside ? y : -1;

        // The core is placed between tiles, so following it needs a repaint per pixel rather than per
        // tile - but only while its tool is in hand, or every other tool would repaint far more often
        // than it has any reason to.
        Point? previous = _pointerOnScreen;
        _pointerOnScreen = inside ? ScreenPixel(session, position) : null;

        if (moved || (session.Tool == EditorTool.Core && _pointerOnScreen != previous))
        {
            ReportHover(session);
            InvalidateVisual();
        }

        if (_placingCore)
        {
            PlaceCore(session, e.GetPosition(this));
            return;
        }

        // Dragging paints a trail; the other two tools would either repeat themselves pointlessly
        // or fight with the pointer, so they act once per press.
        if (_stroking && inside && moved &&
            _strokeTool is EditorTool.Paint or EditorTool.Erase)
        {
            session.ApplyTool(_strokeTool, x, y);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndStroke();

        e.Pointer.Capture(null);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndStroke();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverX = -1;
        _hoverY = -1;
        _pointerOnScreen = null;

        if (Session is not null)
        {
            Session.HoverStatus = string.Empty;
        }

        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        EditorSession? session = Session;
        if (session is null || e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        // Walking the map with the arrow keys is the same gesture as walking it in the game, and
        // it beats reaching for the world map every time the next screen along needs a look.
        switch (e.Key)
        {
            case Key.Left:
                session.MoveScreen(-1, 0);
                break;

            case Key.Right:
                session.MoveScreen(1, 0);
                break;

            case Key.Up:
                session.MoveScreen(0, -1);
                break;

            case Key.Down:
                session.MoveScreen(0, 1);
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    private static EditorTool ToolFor(PointerPoint point, KeyModifiers modifiers, EditorTool selected)
    {
        if (point.Properties.IsRightButtonPressed)
        {
            return EditorTool.Erase;
        }

        // Alt to pick is the habit every paint program has trained, and it saves a trip to the
        // toolbar in the middle of laying tiles down.
        return modifiers.HasFlag(KeyModifiers.Alt) ? EditorTool.Pick : selected;
    }

    private static string DescribeStroke(EditorTool tool)
        => tool switch
        {
            EditorTool.Erase => "erase",
            EditorTool.Fill => "fill",
            _ => "paint"
        };

    private void EndStroke()
    {
        if (!_stroking && !_placingCore)
        {
            return;
        }

        _stroking = false;
        _placingCore = false;
        Session?.Undo.Commit();
    }

    /// <summary>
    ///     Puts the core where the pointer is, measured in the screen's own pixels so that the position
    ///     means the same thing whatever size the canvas is drawn at. The pointer is taken as the middle
    ///     of the core rather than its corner, which is how dragging something about ought to feel.
    /// </summary>
    private void PlaceCore(EditorSession session, Point position)
    {
        Point pixel = ScreenPixel(session, position);

        session.PlaceCore(
            (int)pixel.X - CoreSite.AreaWidth / 2,
            (int)pixel.Y - CoreSite.AreaHeight / 2);
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

    private bool TryHitTest(Point position, out int x, out int y)
    {
        x = -1;
        y = -1;

        EditorSession? session = Session;
        if (session?.Sheet is null)
        {
            return false;
        }

        Layout layout = Layout.For(Bounds.Size, session.ShowNeighbours);
        if (!layout.Screen.Contains(position))
        {
            return false;
        }

        x = (int)((position.X - layout.Screen.X) / layout.TileWidth);
        y = (int)((position.Y - layout.Screen.Y) / layout.TileHeight);

        x = Math.Clamp(x, 0, TileMetrics.TilesAcross - 1);
        y = Math.Clamp(y, 0, TileMetrics.TilesDown - 1);

        return true;
    }

    private void ReportHover(EditorSession session)
    {
        if (_hoverX < 0)
        {
            session.HoverStatus = string.Empty;
            return;
        }

        int tile = session.Document.GetTile(session.CurrentScreen, _hoverX, _hoverY);
        string status = _hoverX + ", " + _hoverY + "  ·  " + TileCatalogue.Describe(tile);

        // A booth is the one tile that carries something beyond its number, and nothing on the screen
        // shows what that something is, so the status bar says it and how to change it.
        if (tile == (int)ActionTiles.TeleportBooth)
        {
            status += "  ·  code " + (session.CurrentTeleportCode ?? "not set") + "  ·  shift-click to set";
        }

        // The core is not a tile, so nothing above would mention it. Worth saying where the pointer is
        // over it, because that is the only way to tell it apart from the scenery underneath.
        CoreSite core = session.Core;

        if (core.ScreenNumber == session.CurrentScreen)
        {
            status += "  ·  core at " + core.XPos + ", " + core.YPos +
                      (session.CoreIsPlaced ? string.Empty : " (not placed - this is the fallback)");
        }

        session.HoverStatus = status;
    }

    private void DrawPlaceholder(DrawingContext context)
    {
        var text = new FormattedText(
            "No tile sheet loaded.  File ▸ Load tile sheet",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            14,
            EditorColours.MutedBrush);

        context.DrawText(text, new Point(
            (Bounds.Width - text.Width) / 2,
            (Bounds.Height - text.Height) / 2));
    }

    private void DrawScreen(DrawingContext context, EditorSession session, Layout layout)
    {
        TileSheet sheet = session.Sheet!;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                Rect destination = layout.TileRect(x, y);
                int tile = session.Document.GetTile(session.CurrentScreen, x, y);

                if (tile == EditorDocument.EmptyTile)
                {
                    DrawEmptyTile(context, layout, destination, x, y);
                    continue;
                }

                TileRenderer.Draw(context, sheet, tile, destination);
            }
        }
    }

    /// <summary>
    ///     Fills a tile position that holds nothing with a chequer, which marks it as a hole.
    /// </summary>
    /// <remarks>
    ///     The square is half a collision cell, and which squares are filled is worked out from the
    ///     position on the screen rather than within the tile. Both of those are about the pattern
    ///     running on across a tile boundary. It used to be a whole cell and counted from the tile's own
    ///     corner, and a tile is three cells down - an odd number, so every tile below the first started
    ///     the pattern out of phase and two rows of the same colour met at the seam. Halving the square
    ///     makes the count even either way; counting from the screen is what makes it not matter.
    /// </remarks>
    private static void DrawEmptyTile(DrawingContext context, Layout layout, Rect destination, int tileX, int tileY)
    {
        double square = layout.Cell / ChequerSquaresPerCell;

        for (int down = 0; down < ChequerSquaresDownTile; down++)
        {
            for (int across = 0; across < ChequerSquaresAcrossTile; across++)
            {
                int screenX = tileX * ChequerSquaresAcrossTile + across;
                int screenY = tileY * ChequerSquaresDownTile + down;

                if ((screenX + screenY) % 2 != 0)
                {
                    continue;
                }

                context.FillRectangle(
                    EditorColours.VoidChequerBrush,
                    new Rect(
                        destination.X + across * square,
                        destination.Y + down * square,
                        square,
                        square));
            }
        }
    }

    private void DrawNeighbours(DrawingContext context, EditorSession session, Layout layout)
    {
        TileSheet sheet = session.Sheet!;
        EditorDocument document = session.Document;

        int column = session.ScreenColumn;
        int row = session.ScreenRow;

        using DrawingContext.PushedState dimmed = context.PushOpacity(0.4);

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int neighbourColumn = column + dx;
                int neighbourRow = row + dy;

                if (neighbourColumn < 0 || neighbourColumn >= WorldMap.ScreensAcross ||
                    neighbourRow < 0 || neighbourRow >= WorldMap.ScreensDown)
                {
                    continue;
                }

                int neighbour = neighbourRow * WorldMap.ScreensAcross + neighbourColumn;

                // Only the tiles that fall inside the border are worth drawing, so each direction
                // contributes the one row or column of its screen that touches this one.
                (int firstX, int lastX) = StripRange(dx, TileMetrics.TilesAcross);
                (int firstY, int lastY) = StripRange(dy, TileMetrics.TilesDown);

                for (int y = firstY; y <= lastY; y++)
                {
                    for (int x = firstX; x <= lastX; x++)
                    {
                        int tile = document.GetTile(neighbour, x, y);
                        if (tile == EditorDocument.EmptyTile)
                        {
                            continue;
                        }

                        TileRenderer.Draw(context, sheet, tile, layout.TileRect(
                            dx == 0 ? x : dx < 0 ? -NeighbourTiles : TileMetrics.TilesAcross,
                            dy == 0 ? y : dy < 0 ? -NeighbourTiles : TileMetrics.TilesDown));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Which row or column of a neighbouring screen touches this one, given the direction it lies
    ///     in. A screen to the side contributes its far column, one above or below its far row, and
    ///     the direction that is not offset contributes everything.
    /// </summary>
    private static (int First, int Last) StripRange(int direction, int extent)
        => direction switch
        {
            < 0 => (extent - NeighbourTiles, extent - 1),
            > 0 => (0, NeighbourTiles - 1),
            _ => (0, extent - 1)
        };

    private void DrawCollision(DrawingContext context, EditorSession session, Layout layout)
    {
        EditorDocument document = session.Document;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tile = document.GetTile(session.CurrentScreen, x, y);
                if (tile == EditorDocument.EmptyTile)
                {
                    continue;
                }

                Rect destination = layout.TileRect(x, y);

                for (int cellY = 0; cellY < TileMetrics.CollisionCellsDownTile; cellY++)
                {
                    for (int cellX = 0; cellX < TileMetrics.CollisionCellsAcrossTile; cellX++)
                    {
                        if (!document.IsSolid(tile, cellX, cellY))
                        {
                            continue;
                        }

                        context.FillRectangle(
                            EditorColours.CollisionBrush,
                            new Rect(
                                destination.X + cellX * layout.Cell,
                                destination.Y + cellY * layout.Cell,
                                layout.Cell,
                                layout.Cell));
                    }
                }
            }
        }

        for (int line = 0; line <= TileMetrics.TilesAcross * TileMetrics.CollisionCellsAcrossTile; line++)
        {
            double x = layout.Screen.X + line * layout.Cell;
            context.DrawLine(EditorColours.CollisionGridPen,
                new Point(x, layout.Screen.Y),
                new Point(x, layout.Screen.Bottom));
        }

        for (int line = 0; line <= TileMetrics.TilesDown * TileMetrics.CollisionCellsDownTile; line++)
        {
            double y = layout.Screen.Y + line * layout.Cell;
            context.DrawLine(EditorColours.CollisionGridPen,
                new Point(layout.Screen.X, y),
                new Point(layout.Screen.Right, y));
        }
    }

    private static void DrawGrid(DrawingContext context, Layout layout)
    {
        for (int column = 0; column <= TileMetrics.TilesAcross; column++)
        {
            double x = layout.Screen.X + column * layout.TileWidth;
            context.DrawLine(EditorColours.GridPen,
                new Point(x, layout.Screen.Y),
                new Point(x, layout.Screen.Bottom));
        }

        for (int row = 0; row <= TileMetrics.TilesDown; row++)
        {
            double y = layout.Screen.Y + row * layout.TileHeight;
            context.DrawLine(EditorColours.GridPen,
                new Point(layout.Screen.X, y),
                new Point(layout.Screen.Right, y));
        }
    }

    private void DrawTileNumbers(DrawingContext context, EditorSession session, Layout layout)
    {
        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tile = session.Document.GetTile(session.CurrentScreen, x, y);
                if (tile == EditorDocument.EmptyTile)
                {
                    continue;
                }

                bool special = TileCatalogue.IsSpecial(tile);

                var text = new FormattedText(
                    tile.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,
                    11,
                    special ? EditorColours.AccentBrush : EditorColours.TextBrush);

                Rect destination = layout.TileRect(x, y);
                var backing = new Rect(destination.X + 3, destination.Y + 3, text.Width + 8, text.Height + 2);

                context.FillRectangle(new SolidColorBrush(EditorColours.Void, 0.75), backing, 3);
                context.DrawText(text, new Point(backing.X + 4, backing.Y + 1));
            }
        }
    }

    /// <summary>
    ///     Writes the screen's destination code across every teleport booth on it. The code is the
    ///     one thing about a screen that is not a tile number, and the booth is where anybody looking
    ///     for it would look. A booth without one says so, because a booth no code reaches is a
    ///     screen that cannot be teleported to.
    /// </summary>
    private static void DrawTeleportCodes(DrawingContext context, EditorSession session, Layout layout)
    {
        if (!session.CurrentScreenHasTeleportBooth)
        {
            return;
        }

        string? code = session.CurrentTeleportCode;

        var text = new FormattedText(
            code ?? "NO CODE",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            CodeTypeface,
            13,
            code is null ? EditorColours.MutedBrush : EditorColours.AccentBrush);

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (session.Document.GetTile(session.CurrentScreen, x, y) != (int)ActionTiles.TeleportBooth)
                {
                    continue;
                }

                Rect booth = layout.TileRect(x, y);
                var backing = new Rect(
                    booth.Center.X - (text.Width + 12) / 2,
                    booth.Center.Y - (text.Height + 6) / 2,
                    text.Width + 12,
                    text.Height + 6);

                context.FillRectangle(new SolidColorBrush(EditorColours.Void, 0.8), backing, 4);
                context.DrawText(text, new Point(backing.X + 6, backing.Y + 3));
            }
        }
    }

    /// <summary>
    ///     The planet core's area, outlined where it sits on this screen, with the nine elements it holds
    ///     marked out inside it.
    /// </summary>
    /// <remarks>
    ///     Drawn rather than described because the core is the one authored thing that is not a tile: it
    ///     is one and a half tiles across by two down and sits at a position of its own, so the only way
    ///     to see whether it fits the room around it is to see it. An unplaced core is drawn faintly at
    ///     the position the game would fall back to.
    /// </remarks>
    private static void DrawCore(DrawingContext context, EditorSession session, Layout layout)
    {
        CoreSite core = session.Core;

        if (core.ScreenNumber != session.CurrentScreen)
        {
            return;
        }

        using DrawingContext.PushedState faded = context.PushOpacity(session.CoreIsPlaced ? 1 : 0.45);

        DrawCoreArea(context, CoreArea(layout, core.XPos, core.YPos));
    }

    /// <summary>
    ///     The core as the canvas draws it, wherever it is being drawn: a wash, the nine elements picked
    ///     out inside it, and an edge. One method, so the core following the pointer is the same picture
    ///     as the core already placed rather than a sketch of it.
    /// </summary>
    private static void DrawCoreArea(DrawingContext context, Rect area)
    {
        context.FillRectangle(CoreFillBrush, area);

        double element = area.Width / CoreSite.SpritesAcross;

        for (int down = 0; down < CoreSite.SpritesDown; down++)
        {
            for (int across = 0; across < CoreSite.SpritesAcross; across++)
            {
                context.DrawRectangle(null, CoreElementPen, new Rect(
                    area.X + across * element,
                    area.Y + down * element,
                    element,
                    element));
            }
        }

        context.DrawRectangle(null, EditorColours.AccentPen, area);
    }

    /// <summary>
    ///     Where a core at a screen position lands on the canvas.
    /// </summary>
    private static Rect CoreArea(Layout layout, int xPos, int yPos)
    {
        double scale = layout.Screen.Width / TileMetrics.PlayAreaWidth;

        return new Rect(
            layout.Screen.X + xPos * scale,
            layout.Screen.Y + yPos * scale,
            CoreSite.AreaWidth * scale,
            CoreSite.AreaHeight * scale);
    }

    /// <summary>
    ///     A pointer position in the screen's own pixels, which is what the core is placed in. An
    ///     instance method rather than a static one because it needs the control's size to know what the
    ///     canvas is drawn at.
    /// </summary>
    private Point ScreenPixel(EditorSession session, Point position)
    {
        Layout layout = Layout.For(Bounds.Size, session.ShowNeighbours);
        double scale = layout.Screen.Width / TileMetrics.PlayAreaWidth;

        return new Point(
            (position.X - layout.Screen.X) / scale,
            (position.Y - layout.Screen.Y) / scale);
    }

    private void DrawHover(DrawingContext context, EditorSession session, Layout layout)
    {
        if (_hoverX < 0 || _hoverY < 0)
        {
            return;
        }

        EditorTool tool = _stroking || _placingCore ? _strokeTool : session.Tool;

        // The core is not a tile, so its ghost is the core itself at the position it would land on -
        // snapped exactly as placing it would snap, so what is shown is what would be stamped down.
        if (tool == EditorTool.Core)
        {
            if (_pointerOnScreen is { } pointer)
            {
                CorePlacement landing = CoreSite.Snap(
                    (int)pointer.X - CoreSite.AreaWidth / 2,
                    (int)pointer.Y - CoreSite.AreaHeight / 2);

                using DrawingContext.PushedState ghost = context.PushOpacity(0.55);

                DrawCoreArea(context, CoreArea(layout, landing.XPos, landing.YPos));
            }

            return;
        }

        Rect destination = layout.TileRect(_hoverX, _hoverY);

        // A ghost of what is about to be laid down beats guessing from a swatch on the far side of
        // the window whether the tile in hand is the right one.
        if (tool is EditorTool.Paint or EditorTool.Fill &&
            (session.Sheet!.Contains(session.SelectedTile) || TileCatalogue.IsMarker(session.SelectedTile)))
        {
            using (context.PushOpacity(0.55))
            {
                TileRenderer.Draw(context, session.Sheet, session.SelectedTile, destination);
            }
        }
        else
        {
            context.FillRectangle(EditorColours.HoverBrush, destination);
        }

        context.DrawRectangle(null, EditorColours.AccentPen, destination.Deflate(1));
    }

    /// <summary>
    ///     Where everything goes for a given control size.
    /// </summary>
    /// <remarks>
    ///     Sizes are worked out from the collision cell rather than from the tile, so that the tile
    ///     grid, the collision grid and the artwork all land on whole pixels. A tile is four cells
    ///     across by three down, so a whole number of pixels per cell is a whole number per tile and
    ///     the 4:3 shape of a tile is kept exactly.
    /// </remarks>
    private readonly record struct Layout(double Cell, Rect Screen)
    {

        public double TileWidth => Cell * TileMetrics.CollisionCellsAcrossTile;

        public double TileHeight => Cell * TileMetrics.CollisionCellsDownTile;

        public static Layout For(Size available, bool showNeighbours)
        {
            int margin = showNeighbours ? NeighbourTiles : 0;

            int cellsAcross = (TileMetrics.TilesAcross + margin * 2) * TileMetrics.CollisionCellsAcrossTile;
            int cellsDown = (TileMetrics.TilesDown + margin * 2) * TileMetrics.CollisionCellsDownTile;

            double cell = Math.Max(2, Math.Floor(Math.Min(
                available.Width / cellsAcross,
                available.Height / cellsDown)));

            double totalWidth = cellsAcross * cell;
            double totalHeight = cellsDown * cell;

            var screen = new Rect(
                Math.Floor((available.Width - totalWidth) / 2) +
                margin * TileMetrics.CollisionCellsAcrossTile * cell,
                Math.Floor((available.Height - totalHeight) / 2) +
                margin * TileMetrics.CollisionCellsDownTile * cell,
                TileMetrics.TilesAcross * TileMetrics.CollisionCellsAcrossTile * cell,
                TileMetrics.TilesDown * TileMetrics.CollisionCellsDownTile * cell);

            return new Layout(cell, screen);
        }

        public Rect TileRect(int x, int y)
            => new(
                Screen.X + x * TileWidth,
                Screen.Y + y * TileHeight,
                TileWidth,
                TileHeight);
    }
}
