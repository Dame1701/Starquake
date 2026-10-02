// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Starquake.Constants;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     Where everything goes for a given control size.
/// </summary>
/// <remarks>
///     Sizes are worked out from the collision cell rather than from the tile, so that the tile
///     grid, the collision grid and the artwork all land on whole pixels. A tile is four cells
///     across by three down, so a whole number of pixels per cell is a whole number per tile and
///     the 4:3 shape of a tile is kept exactly.
///     <para>
///         Shared rather than private to the painter, because the canvas needs the same geometry to
///         turn a pointer position back into a tile.
///     </para>
/// </remarks>
internal readonly record struct ScreenLayout(double Cell, Rect Screen)
{
    /// <summary>
    ///     How much of a neighbouring screen to show, in tiles. One is enough to line scenery up
    ///     against and small enough to leave the screen being edited the obvious subject.
    /// </summary>
    public const int NeighbourTiles = 1;

    public double TileWidth => Cell * TileMetrics.CollisionCellsAcrossTile;

    public double TileHeight => Cell * TileMetrics.CollisionCellsDownTile;

    public static ScreenLayout For(Size available, bool showNeighbours)
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

        return new ScreenLayout(cell, screen);
    }

    public Rect TileRect(int x, int y)
        => new(
            Screen.X + x * TileWidth,
            Screen.Y + y * TileHeight,
            TileWidth,
            TileHeight);
}
