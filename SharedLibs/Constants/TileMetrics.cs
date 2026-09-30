// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The shape of a game screen. Starquake's world is a grid of fixed-size screens rather than a
///     scrolling map: the blob walks off one edge and the next screen replaces it whole, so every
///     screen is laid out to exactly these dimensions and nothing in the game ever sees a partial
///     one.
/// </summary>
public static class TileMetrics
{
    /// <summary>
    ///     How many tiles make up one screen. The original's screens were 32x24 character cells,
    ///     which is this 8x6 grid of tiles at four cells across and four down apiece.
    /// </summary>
    public const int TilesAcross = 8;

    /// <inheritdoc cref="TilesAcross" />
    public const int TilesDown = 6;

    /// <summary>
    ///     The size one tile is drawn at. Tiles are the large blocks the screen is built from - the
    ///     platforms, walls and scenery - and every tile sheet is cut into frames of this size.
    /// </summary>
    public const int TileWidth = 128;

    /// <inheritdoc cref="TileWidth" />
    public const int TileHeight = 96;

    /// <summary>
    ///     The size of one cell of the collision grid. Collision is tested against a finer grid than
    ///     the tiles are drawn on, so a tile can be solid in part - the top of a platform, one edge of
    ///     a wall - rather than only ever wholly solid or wholly empty. A tile is
    ///     <see cref="TileWidth" />/<see cref="CollisionCellWidth" /> cells across by
    ///     <see cref="TileHeight" />/<see cref="CollisionCellHeight" /> cells down.
    /// </summary>
    public const int CollisionCellWidth = 32;

    /// <inheritdoc cref="CollisionCellWidth" />
    public const int CollisionCellHeight = 32;

    /// <summary>
    ///     How many collision cells one tile is divided into.
    /// </summary>
    public const int CollisionCellsAcrossTile = TileWidth / CollisionCellWidth;

    /// <inheritdoc cref="CollisionCellsAcrossTile" />
    public const int CollisionCellsDownTile = TileHeight / CollisionCellHeight;

    /// <summary>
    ///     The size of the playfield - the part of the picture the game screens are drawn into, below
    ///     the status panel. This is a whole number of tiles by definition, so it is derived rather
    ///     than stated.
    /// </summary>
    public const int PlayAreaWidth = TilesAcross * TileWidth;

    /// <inheritdoc cref="PlayAreaWidth" />
    public const int PlayAreaHeight = TilesDown * TileHeight;
}
