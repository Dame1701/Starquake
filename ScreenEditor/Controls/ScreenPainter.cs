// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
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
///     Starquake's world does not scroll - the blob walks off one edge and the next screen replaces it
///     whole - so a screen has to be built to line up with its neighbours without ever being seen
///     alongside them in the game. That is what the border of neighbouring tiles is for: a platform that
///     stops a tile short of the edge, or a tunnel mouth with nothing on the other side of it, is
///     obvious here and invisible on a screen drawn on its own.
///     <para>
///         Nothing here touches the control. It reads the session, a <see cref="ScreenLayout" /> and a
///         <see cref="Hover" />, and writes to the drawing context - which is what lets the canvas be
///         about input and this be about pixels.
///     </para>
/// </remarks>
internal static class ScreenPainter
{
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

    /// <summary>
    ///     One frame of the canvas. Which overlays are drawn is the session's to say; the order they go
    ///     down in is this method's, and it is the whole of the drawing policy.
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
    ///     The square is half a collision cell, and which squares are filled is worked out from the
    ///     position on the screen rather than within the tile. Both of those are about the pattern
    ///     running on across a tile boundary. It used to be a whole cell and counted from the tile's own
    ///     corner, and a tile is three cells down - an odd number, so every tile below the first started
    ///     the pattern out of phase and two rows of the same colour met at the seam. Halving the square
    ///     makes the count even either way; counting from the screen is what makes it not matter.
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
                            dx == 0 ? x : dx < 0 ? -ScreenLayout.NeighbourTiles : TileMetrics.TilesAcross,
                            dy == 0 ? y : dy < 0 ? -ScreenLayout.NeighbourTiles : TileMetrics.TilesDown));
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
    ///     Writes the screen's destination code across every teleport booth on it. The code is the
    ///     one thing about a screen that is not a tile number, and the booth is where anybody looking
    ///     for it would look. A booth without one says so, because a booth no code reaches is a
    ///     screen that cannot be teleported to.
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
    ///     The planet core's area, outlined where it sits on this screen, with the nine elements it holds
    ///     marked out inside it.
    /// </summary>
    /// <remarks>
    ///     Drawn rather than described because the core is the one authored thing that is not a tile: it
    ///     is one and a half tiles across by two down and sits at a position of its own, so the only way
    ///     to see whether it fits the room around it is to see it. An unplaced core is drawn faintly at
    ///     the position the game would fall back to.
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
