// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The shape of a game screen. The world is a grid of fixed-size screens, not a scrolling map.
///     Blob walks off one edge and the next screen replaces it whole, so every screen is exactly
///     these dimensions and nothing ever sees a partial one.
/// </summary>
public static class TileMetrics
{
    /// <summary>
    ///     How many tiles make up one screen. The original used 32x24 character cells, which is this
    ///     8x6 grid at four cells per tile each way.
    /// </summary>
    public const int TilesAcross = 8;

    /// <inheritdoc cref="TilesAcross" />
    public const int TilesDown = 6;

    /// <summary>
    ///     The size one tile is drawn at. Tiles are the large blocks a screen is built from:
    ///     platforms, walls and scenery. Every tile sheet is cut into frames of this size.
    /// </summary>
    public const int TileWidth = 128;

    /// <inheritdoc cref="TileWidth" />
    public const int TileHeight = 96;

    /// <summary>
    ///     The size of one cell of the collision grid. Collision uses a finer grid than tiles are
    ///     drawn on, so a tile can be solid in part, such as the top of a platform or one edge of a
    ///     wall, instead of only wholly solid or wholly empty.
    /// </summary>
    public const int CollisionCellWidth = 32;

    /// <inheritdoc cref="CollisionCellWidth" />
    public const int CollisionCellHeight = 32;

    public const int CollisionCellsAcrossTile = TileWidth / CollisionCellWidth;

    public const int CollisionCellsDownTile = TileHeight / CollisionCellHeight;

    /// <summary>
    ///     The size of the playfield, the area below the status panel that game screens are drawn
    ///     into. Derived, because it is always a whole number of tiles.
    /// </summary>
    public const int PlayAreaWidth = TilesAcross * TileWidth;

    /// <inheritdoc cref="PlayAreaWidth" />
    public const int PlayAreaHeight = TilesDown * TileHeight;
}
