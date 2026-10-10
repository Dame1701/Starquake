// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Starquake.Constants;
using Starquake.ScreenEditor.Editing;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     The screen being edited, drawn large with the eight screens around it showing faintly at its
///     edges.
/// </summary>
/// <remarks>
///     The world does not scroll, so a screen must line up with its neighbours even though the game
///     never shows them together. The border of neighbouring tiles is what makes a platform that
///     stops a tile short, or a tunnel mouth with nothing behind it, visible while editing.
/// </remarks>
internal sealed class ScreenCanvas : Control
{

    public static readonly StyledProperty<EditorSession?> SessionProperty =
        AvaloniaProperty.Register<ScreenCanvas, EditorSession?>(nameof(Session));

    private readonly SessionSubscription _session;

    private int _hoverX = -1;

    private int _hoverY = -1;

    private bool _placingCore;

    /// <summary>
    ///     Where the pointer is in the screen's own pixels, or null when it is off the screen. The
    ///     tile alone is not enough for the core, which is placed between tiles.
    /// </summary>
    private Point? _pointerOnScreen;

    private EditorTool _strokeTool;

    private bool _stroking;

    public ScreenCanvas()
    {
        _session = new SessionSubscription(InvalidateVisual);

        Focusable = true;

        // The artwork is pixel art and the overlays are hairlines. Both need to land on whole pixels
        // rather than be smoothed across them.
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    public EditorSession? Session
    {
        get => GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public override void Render(DrawingContext context)
        => ScreenPainter.Paint(context, Session, Bounds.Size, CurrentHover());

    /// <summary>
    ///     What the pointer is over and what would be laid down there. The canvas decides which tool
    ///     the ghost shows, because a stroke in progress keeps the tool it started with.
    /// </summary>
    private Hover CurrentHover()
        => new(
            _hoverX,
            _hoverY,
            _stroking || _placingCore
                ? _strokeTool
                : Session?.Tool ?? EditorTool.Paint,
            _pointerOnScreen);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SessionProperty)
        {
            _session.Attach(change.GetNewValue<EditorSession?>());
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

        // Shift and click on a booth sets its destination code. Handled before the tool is applied,
        // so opening the dialog cannot also paint over the booth it was aimed at.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            session.Document.GetTile(session.CurrentScreen, x, y) == (int)ActionTiles.TeleportBooth)
        {
            session.RequestTeleportCodeEdit();
            e.Handled = true;

            return;
        }

        // On a signpost it asks which teleporter the sign points at. What a sign says is not a tile
        // number, so there is nowhere else on the canvas to change it.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            session.Document.GetTile(session.CurrentScreen, x, y) == (int)ActionTiles.TeleportSignpost)
        {
            session.RequestSignpostEdit();
            e.Handled = true;

            return;
        }

        _strokeTool = ToolFor(point, e.KeyModifiers, session.Tool);

        // The core is a position rather than a tile, so it is placed from the pointer in screen
        // pixels and skips the tile-based tools.
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

        // The core is placed between tiles, so following it needs a repaint per pixel rather than
        // per tile. Only while its tool is in hand, or every other tool would repaint needlessly.
        Point? previous = _pointerOnScreen;
        _pointerOnScreen = inside ? ScreenPixel(session, position) : null;

        if (moved || session.Tool == EditorTool.Core && _pointerOnScreen != previous)
        {
            ReportHover(session);
            InvalidateVisual();
        }

        if (_placingCore)
        {
            PlaceCore(session, e.GetPosition(this));
            return;
        }

        // Only paint and erase repeat while dragging. The other tools act once per press.
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
            Session.View.HoverStatus = string.Empty;
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
    ///     Puts the core where the pointer is, in the screen's own pixels so the position means the
    ///     same thing at any canvas size. The pointer marks the middle of the core, not its corner.
    /// </summary>
    private void PlaceCore(EditorSession session, Point position)
    {
        Point pixel = ScreenPixel(session, position);

        session.PlaceCore(
            (int)pixel.X - CoreSite.AreaWidth / 2,
            (int)pixel.Y - CoreSite.AreaHeight / 2);
    }


    private bool TryHitTest(Point position, out int x, out int y)
    {
        x = -1;
        y = -1;

        EditorSession? session = Session;
        if (session?.Sheet is null)
        {
            return false;
        }

        ScreenLayout layout = ScreenLayout.For(Bounds.Size, session.View.ShowNeighbours);
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
            session.View.HoverStatus = string.Empty;
            return;
        }

        int tile = session.Document.GetTile(session.CurrentScreen, _hoverX, _hoverY);
        string status = _hoverX + ", " + _hoverY + "  ·  " + TileCatalogue.Describe(tile);

        // A booth carries a code beyond its tile number, and nothing on screen shows it, so the
        // status bar gives the code and how to change it.
        if (tile == (int)ActionTiles.TeleportBooth)
        {
            status += "  ·  code " + (session.CurrentTeleportCode ?? "not set") + "  ·  shift-click to set";
        }

        // A sign's tile number does not say which teleporter it advertises either.
        if (tile == (int)ActionTiles.TeleportSignpost)
        {
            status += "  ·  reads " + (session.Document.GetSignpostCode(session.CurrentScreen) ?? "blank") +
                      "  ·  shift-click to set";
        }

        // The core is not a tile, so nothing above mentions it. The status bar is the only way to
        // tell it apart from the scenery underneath.
        CoreSite core = session.Positions.Core;

        if (core.ScreenNumber == session.CurrentScreen)
        {
            status += "  ·  core at " + core.XPos + ", " + core.YPos +
                      (session.Positions.CoreIsPlaced ? string.Empty : " (not placed - this is the fallback)");
        }

        session.View.HoverStatus = status;
    }

    /// <summary>
    ///     Converts a pointer position to the screen's own pixels, which is what the core is placed
    ///     in. Not static, because it needs the control's size.
    /// </summary>
    private Point ScreenPixel(EditorSession session, Point position)
    {
        ScreenLayout layout = ScreenLayout.For(Bounds.Size, session.View.ShowNeighbours);
        double scale = layout.Screen.Width / TileMetrics.PlayAreaWidth;

        return new Point(
            (position.X - layout.Screen.X) / scale,
            (position.Y - layout.Screen.Y) / scale);
    }
}
