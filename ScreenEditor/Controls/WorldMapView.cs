// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Starquake.Constants;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     The whole planet at a glance, with the screen being edited marked on it.
/// </summary>
/// <remarks>
///     <para>
///         Five hundred and twelve screens is far too many to find your way around by number, and
///         the map is the only thing that shows how they join up. Clicking anywhere on it goes
///         straight to that screen.
///     </para>
///     <para>
///         It is drawn by averaging every tile down to a handful of pixels once, when the sheet is
///         loaded, and then filling a small bitmap from those averages. Drawing the map from the
///         artwork itself would be twenty-four thousand scaled image draws, which is far too many to
///         repeat every time a tile is painted.
///     </para>
/// </remarks>
public sealed class WorldMapView : Control
{

    /// <summary>
    ///     How many pixels of the map one screen takes up. A tile becomes a handful of pixels rather
    ///     than one, which is just enough for a platform to still look like a platform.
    /// </summary>
    private const int ScreenPixelWidth = TileMetrics.TilesAcross * TileSheet.ThumbnailCellsAcross;

    /// <inheritdoc cref="ScreenPixelWidth" />
    private const int ScreenPixelHeight = TileMetrics.TilesDown * TileSheet.ThumbnailCellsDown;

    public static readonly StyledProperty<EditorSession?> SessionProperty =
        AvaloniaProperty.Register<WorldMapView, EditorSession?>(nameof(Session));

    private static readonly IPen ScreenGridPen = new Pen(new SolidColorBrush(Colors.White, 0.08));

    private static readonly IPen RegionGridPen = new Pen(new SolidColorBrush(Colors.White, 0.16));

    private EditorSession? _attached;

    private WriteableBitmap? _bitmap;

    private int _hoverScreen = -1;

    private bool _stale = true;

    public WorldMapView()
    {
        // The map is a downscale of an already tiny bitmap, so smoothing it reads better than the
        // hard pixels the editing surfaces want.
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.MediumQuality);
    }

    public EditorSession? Session
    {
        get => GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    private static int WorldPixelWidth => WorldMap.ScreensAcross * ScreenPixelWidth;

    private static int WorldPixelHeight => WorldMap.ScreensDown * ScreenPixelHeight;

    public override void Render(DrawingContext context)
    {
        EditorSession? session = Session;
        if (session is null)
        {
            return;
        }

        Rect area = MapArea();

        context.FillRectangle(EditorColours.VoidBrush, area);

        if (session.Sheet is not null)
        {
            Refresh(session);

            if (_bitmap is not null)
            {
                context.DrawImage(_bitmap, new Rect(_bitmap.Size), area);
            }
        }

        DrawGrid(context, area);

        if (_hoverScreen >= 0)
        {
            context.FillRectangle(EditorColours.HoverBrush, ScreenRect(area, _hoverScreen));
        }

        context.DrawRectangle(null, EditorColours.AccentPen, ScreenRect(area, session.CurrentScreen).Inflate(1));
        context.DrawRectangle(null, EditorColours.BorderPen, area);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // The planet is much taller than it is wide, so the map takes whatever width it is given and
        // asks for the height that keeps it in proportion.
        double width = double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width;

        return new Size(width, width * WorldPixelHeight / WorldPixelWidth);
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
        GoToScreenUnder(e.GetPosition(this));

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        Point position = e.GetPosition(this);
        int screen = ScreenUnder(position);

        if (screen != _hoverScreen)
        {
            _hoverScreen = screen;
            ReportHover();
            InvalidateVisual();
        }

        // Dragging across the map scrubs through screens, which is a quick way to find a room when
        // you know roughly where it is but not what number it has.
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            GoToScreenUnder(position);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverScreen = -1;

        if (Session is not null)
        {
            Session.HoverStatus = string.Empty;
        }

        InvalidateVisual();
    }

    private Rect MapArea()
    {
        double scale = Math.Min(Bounds.Width / WorldPixelWidth, Bounds.Height / WorldPixelHeight);

        double width = WorldPixelWidth * scale;
        double height = WorldPixelHeight * scale;

        return new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
    }

    private static Rect ScreenRect(Rect area, int screen)
    {
        double width = area.Width / WorldMap.ScreensAcross;
        double height = area.Height / WorldMap.ScreensDown;

        return new Rect(
            area.X + screen % WorldMap.ScreensAcross * width,
            area.Y + screen / WorldMap.ScreensAcross * height,
            width,
            height);
    }

    private int ScreenUnder(Point position)
    {
        Rect area = MapArea();
        if (!area.Contains(position))
        {
            return -1;
        }

        int column = (int)((position.X - area.X) / (area.Width / WorldMap.ScreensAcross));
        int row = (int)((position.Y - area.Y) / (area.Height / WorldMap.ScreensDown));

        column = Math.Clamp(column, 0, WorldMap.ScreensAcross - 1);
        row = Math.Clamp(row, 0, WorldMap.ScreensDown - 1);

        return row * WorldMap.ScreensAcross + column;
    }

    private void GoToScreenUnder(Point position)
    {
        int screen = ScreenUnder(position);

        if (screen >= 0 && Session is not null)
        {
            Session.CurrentScreen = screen;
        }
    }

    private void ReportHover()
    {
        EditorSession? session = Session;
        if (session is null)
        {
            return;
        }

        session.HoverStatus = _hoverScreen < 0
            ? string.Empty
            : "Screen " + _hoverScreen +
              "  ·  column " + _hoverScreen % WorldMap.ScreensAcross +
              ", row " + _hoverScreen / WorldMap.ScreensAcross;
    }

    private static void DrawGrid(DrawingContext context, Rect area)
    {
        double screenWidth = area.Width / WorldMap.ScreensAcross;
        double screenHeight = area.Height / WorldMap.ScreensDown;

        for (int column = 1; column < WorldMap.ScreensAcross; column++)
        {
            double x = area.X + column * screenWidth;

            // Every fourth line a little stronger, so that a screen can be counted off against the
            // map's edge without following one line all the way across.
            context.DrawLine(column % 4 == 0 ? RegionGridPen : ScreenGridPen,
                new Point(x, area.Y),
                new Point(x, area.Bottom));
        }

        for (int row = 1; row < WorldMap.ScreensDown; row++)
        {
            double y = area.Y + row * screenHeight;

            context.DrawLine(row % 4 == 0 ? RegionGridPen : ScreenGridPen,
                new Point(area.X, y),
                new Point(area.Right, y));
        }
    }

    /// <summary>
    ///     An opaque colour as the premultiplied BGRA word the map's bitmap holds.
    /// </summary>
    private static uint Premultiplied(Color colour)
        => 0xFFu << 24 | (uint)colour.R << 16 | (uint)colour.G << 8 | colour.B;

    private static Color Blend(Color from, Color to, double amount)
        => Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));

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

        _stale = true;
        InvalidateVisual();
    }

    private void OnSessionInvalidated(object? sender, EventArgs e)
    {
        _stale = true;
        InvalidateVisual();
    }

    /// <summary>
    ///     Repaints the map bitmap from the document. Filling it outright is around three hundred
    ///     thousand pixel writes, which is quick enough that tracking which screens changed would cost
    ///     more in complexity than it saves in time.
    /// </summary>
    private void Refresh(EditorSession session)
    {
        if (!_stale && _bitmap is not null)
        {
            return;
        }

        _stale = false;
        _bitmap ??= new WriteableBitmap(
            new PixelSize(WorldPixelWidth, WorldPixelHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        TileSheet sheet = session.Sheet!;
        EditorDocument document = session.Document;
        uint[] thumbnails = sheet.ThumbnailColours;

        uint voidColour = Premultiplied(EditorColours.Void);

        // A marker tile is not drawn in the game, so showing its placeholder artwork here would put
        // a block of flat magenta on the map where the player sees nothing. It is still worth
        // knowing one is there, so it goes down as a dim tint instead of either extreme.
        uint markerColour = Premultiplied(Blend(EditorColours.Void, EditorColours.Marker, 0.28));

        using ILockedFramebuffer frame = _bitmap.Lock();

        unsafe
        {
            byte* pixels = (byte*)frame.Address;

            for (int screen = 0; screen < WorldMap.ScreenCount && screen < document.Screens.Length; screen++)
            {
                int screenLeft = screen % WorldMap.ScreensAcross * ScreenPixelWidth;
                int screenTop = screen / WorldMap.ScreensAcross * ScreenPixelHeight;

                for (int tileY = 0; tileY < TileMetrics.TilesDown; tileY++)
                {
                    for (int tileX = 0; tileX < TileMetrics.TilesAcross; tileX++)
                    {
                        int tile = document.GetTile(screen, tileX, tileY);
                        bool marker = TileCatalogue.IsMarker(tile);

                        int source = sheet.Contains(tile) && !marker
                            ? tile * TileSheet.ThumbnailCellsAcross * TileSheet.ThumbnailCellsDown
                            : -1;

                        uint flat = marker ? markerColour : voidColour;

                        for (int cellY = 0; cellY < TileSheet.ThumbnailCellsDown; cellY++)
                        {
                            uint* row = (uint*)(pixels +
                                                (long)(screenTop + tileY * TileSheet.ThumbnailCellsDown + cellY) *
                                                frame.RowBytes);

                            for (int cellX = 0; cellX < TileSheet.ThumbnailCellsAcross; cellX++)
                            {
                                row[screenLeft + tileX * TileSheet.ThumbnailCellsAcross + cellX] =
                                    source < 0
                                        ? flat
                                        : thumbnails[source + cellY * TileSheet.ThumbnailCellsAcross + cellX];
                            }
                        }
                    }
                }
            }
        }
    }
}
