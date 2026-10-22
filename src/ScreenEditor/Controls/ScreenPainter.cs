// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Starquake.Constants;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     Draws the screen being edited: its artwork, the eight neighbours showing faintly at its edges,
///     and whichever overlays are switched on over the top.
/// </summary>
/// <remarks>
///     Nothing here touches the control. It reads the session, a <see cref="ScreenLayout" /> and a
///     <see cref="Hover" />, and writes to the drawing context, which keeps the canvas about input
///     and this about pixels.
/// </remarks>
internal static class ScreenPainter
{
    /// <summary>
    ///     How many chequer squares make up one collision cell, each way. Two gives a sixteen pixel
    ///     square, which reads as a texture rather than as blocks, and an even number of squares per
    ///     tile both ways, so the pattern carries across a tile boundary.
    /// </summary>
    private const int ChequerSquaresPerCell = 2;

    /// <inheritdoc cref="ChequerSquaresPerCell" />
    private const int ChequerSquaresAcrossTile = TileMetrics.CollisionCellsAcrossTile * ChequerSquaresPerCell;

    /// <inheritdoc cref="ChequerSquaresPerCell" />
    private const int ChequerSquaresDownTile = TileMetrics.CollisionCellsDownTile * ChequerSquaresPerCell;

    /// <summary>
    ///     Where a signpost's code sits on its tile, as a fraction of the tile: the middle of the
    ///     board across, the foot of the lettering down. Matches how the game lines the code up
    ///     (<see cref="Signposts" />), so the canvas shows where the letters will really land.
    /// </summary>
    private const double BoardCentreX =
        (Signposts.TextLeft + Signposts.TextWidth / 2.0) / TileMetrics.TileWidth;

    /// <inheritdoc cref="BoardCentreX" />
    private const double BoardBaselineY =
        (Signposts.TextTop + Signposts.TextHeight) / (double)TileMetrics.TileHeight;

    private static readonly Typeface LabelTypeface = new(FontFamily.Default);

    /// <summary>
    ///     For the teleport code written across a booth. Heavier than the tile numbers because it is
    ///     drawn over artwork rather than over a badge.
    /// </summary>
    private static readonly Typeface CodeTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

    /// <summary>The planet core's area: a wash with the nine elements picked out inside it.</summary>
    private static readonly IBrush CoreFillBrush = new SolidColorBrush(EditorColours.Accent, 0.16);

    /// <inheritdoc cref="CoreFillBrush" />
    private static readonly IPen CoreElementPen = new Pen(new SolidColorBrush(EditorColours.Accent, 0.5));

    /// <summary>
    ///     How solid a signpost's code is drawn. Matches the game's ink, so the board shows through
    ///     the letters rather than being covered by them.
    /// </summary>
    private static readonly IBrush SignpostInkBrush = new SolidColorBrush(Colors.Black, 0.75);

    /// <summary>
    ///     The game's own typeface, used instead of the editor's label font so a signpost shows the
    ///     face the game will write the code in. Bold to match the game's own thickening, see
    ///     <c>ScreenRenderer.SignpostStrokeWidening</c>.
    /// </summary>
    private static readonly Typeface SignpostTypeface = new(
        new FontFamily("avares://ScreenEditor/Fonts/elektora.ttf#Elektora"),
        FontStyle.Normal,
        FontWeight.Bold);

    /// <summary>
    ///     Draws one frame of the canvas. The session decides which overlays are drawn; this method
    ///     decides the order they go down in.
    /// </summary>
    public static void Paint(DrawingContext context, EditorSession? session, Size bounds, Hover hover)
    {
        context.FillRectangle(EditorColours.VoidBrush, new Rect(bounds));

        if (session?.Sheet is null)
        {
            DrawPlaceholder(context, bounds);

            return;
        }

        ScreenLayout layout = ScreenLayout.For(bounds, session.View.ShowNeighbours);

        if (session.View.ShowNeighbours)
        {
            DrawNeighbours(context, session, layout);
        }

        DrawScreen(context, session, layout);

        if (session.View.ShowCollision)
        {
            DrawCollision(context, session, layout);
        }

        if (session.View.ShowGrid)
        {
            DrawGrid(context, layout);
        }

        if (session.View.ShowTileNumbers)
        {
            DrawTileNumbers(context, session, layout);
        }

        DrawCore(context, session, layout);

        DrawTeleportCodes(context, session, layout);

        DrawSignpostCodes(context, session, layout);

        DrawHover(context, session, layout, hover);

        context.DrawRectangle(null, EditorColours.ScreenEdgePen, layout.Screen);
    }

    private static void DrawScreen(DrawingContext context, EditorSession session, ScreenLayout layout)
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
    ///     The square is half a collision cell and the pattern is counted from the position on the
    ///     screen, not within the tile. Both keep it in phase across a tile boundary: a tile is three
    ///     cells down, so counting whole cells from a tile corner would put every tile below the first
    ///     out of step and meet two rows of the same colour at the seam.
    /// </remarks>
    private static void DrawEmptyTile(DrawingContext context, ScreenLayout layout, Rect destination, int tileX, int tileY)
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

    private static void DrawNeighbours(DrawingContext context, EditorSession session, ScreenLayout layout)
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

                // Each direction contributes only the row or column of its screen that touches this
                // one, since the rest falls outside the border.
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
                            dx == 0 ? x : dx < 0 ? -ScreenLayout.NeighbourTiles : TileMetrics.TilesAcross,
                            dy == 0 ? y : dy < 0 ? -ScreenLayout.NeighbourTiles : TileMetrics.TilesDown));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Which row or column of a neighbouring screen touches this one. A screen to the side
    ///     contributes its far column, one above or below its far row, and an unoffset direction
    ///     contributes everything.
    /// </summary>
    private static (int First, int Last) StripRange(int direction, int extent)
        => direction switch
        {
            < 0 => (extent - ScreenLayout.NeighbourTiles, extent - 1),
            > 0 => (0, ScreenLayout.NeighbourTiles - 1),
            _ => (0, extent - 1)
        };

    private static void DrawCollision(DrawingContext context, EditorSession session, ScreenLayout layout)
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

    private static void DrawGrid(DrawingContext context, ScreenLayout layout)
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

    private static void DrawTileNumbers(DrawingContext context, EditorSession session, ScreenLayout layout)
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
    ///     Writes the screen's destination code across every teleport booth on it. A booth with no
    ///     code says so, because a screen no code reaches cannot be teleported to.
    /// </summary>
    private static void DrawTeleportCodes(DrawingContext context, EditorSession session, ScreenLayout layout)
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
    ///     Writes what each signpost reads across the sign itself. The artwork is a blank board, so
    ///     without this there would be no way to see what a sign says without opening a dialog.
    /// </summary>
    private static void DrawSignpostCodes(DrawingContext context, EditorSession session, ScreenLayout layout)
    {
        if (!session.CurrentScreenHasSignpost)
        {
            return;
        }

        string? code = session.Document.GetSignpostCode(session.CurrentScreen);

        var text = new FormattedText(
            code ?? "BLANK",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            SignpostTypeface,
            13,
            code is null ? EditorColours.MutedBrush : SignpostInkBrush);

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (session.Document.GetTile(session.CurrentScreen, x, y) != (int)ActionTiles.TeleportSignpost)
                {
                    continue;
                }

                // Over the board, not the middle of the tile. The sign hangs from the top of its
                // square and the post takes up most of the artwork below it.
                Rect sign = layout.TileRect(x, y);
                var where = new Point(
                    sign.X + sign.Width * BoardCentreX - text.Width / 2,
                    sign.Y + sign.Height * BoardBaselineY - text.Baseline);

                // A real code is drawn as the game draws it: black straight onto the gold board,
                // which needs nothing behind the letters. "BLANK" is a note rather than a preview,
                // so it keeps the chip the editor's other notes are drawn on.
                if (code is null)
                {
                    var backing = new Rect(where.X - 6, where.Y - 3, text.Width + 12, text.Height + 6);

                    context.FillRectangle(new SolidColorBrush(EditorColours.Void, 0.8), backing, 4);
                }

                context.DrawText(text, where);
            }
        }
    }

    /// <summary>
    ///     Outlines the planet core where it sits on this screen, with its nine elements marked
    ///     inside.
    /// </summary>
    /// <remarks>
    ///     Drawn rather than described because the core is one and a half tiles across by two down at
    ///     a position of its own, so seeing whether it fits the room needs seeing it. An unplaced core
    ///     is drawn faintly at the fallback position.
    /// </remarks>
    private static void DrawCore(DrawingContext context, EditorSession session, ScreenLayout layout)
    {
        CoreSite core = session.Positions.Core;

        if (core.ScreenNumber != session.CurrentScreen)
        {
            return;
        }

        using DrawingContext.PushedState faded = context.PushOpacity(session.Positions.CoreIsPlaced ? 1 : 0.45);

        DrawCoreArea(context, CoreArea(layout, core.XPos, core.YPos));
    }

    /// <summary>
    ///     Draws the core: a wash, its nine elements, and an edge. One method, so the core following
    ///     the pointer looks the same as one already placed.
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
    private static Rect CoreArea(ScreenLayout layout, int xPos, int yPos)
    {
        double scale = layout.Screen.Width / TileMetrics.PlayAreaWidth;

        return new Rect(
            layout.Screen.X + xPos * scale,
            layout.Screen.Y + yPos * scale,
            CoreSite.AreaWidth * scale,
            CoreSite.AreaHeight * scale);
    }

    private static void DrawHover(DrawingContext context, EditorSession session, ScreenLayout layout, Hover hover)
    {
        if (hover.TileX < 0 || hover.TileY < 0)
        {
            return;
        }

        EditorTool tool = hover.Tool;

        // The core is not a tile, so its ghost is the core itself at the position it would land on -
        // snapped exactly as placing it would snap, so what is shown is what would be stamped down.
        if (tool == EditorTool.Core)
        {
            if (hover.PointerOnScreen is { } pointer)
            {
                CorePlacement landing = CoreSite.Snap(
                    (int)pointer.X - CoreSite.AreaWidth / 2,
                    (int)pointer.Y - CoreSite.AreaHeight / 2);

                using DrawingContext.PushedState ghost = context.PushOpacity(0.55);

                DrawCoreArea(context, CoreArea(layout, landing.XPos, landing.YPos));
            }

            return;
        }

        Rect destination = layout.TileRect(hover.TileX, hover.TileY);

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

    private static void DrawPlaceholder(DrawingContext context, Size bounds)
    {
        var text = new FormattedText(
            "No tile sheet loaded.  File ▸ Load tile sheet",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            14,
            EditorColours.MutedBrush);

        context.DrawText(text, new Point(
            (bounds.Width - text.Width) / 2,
            (bounds.Height - text.Height) / 2));
    }
}
