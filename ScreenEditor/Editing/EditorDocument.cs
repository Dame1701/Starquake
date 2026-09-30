// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     The map being edited: every screen on the planet, plus the collision mask that says which
///     parts of each tile in the sheet are solid.
/// </summary>
/// <remarks>
///     <para>
///         The two halves are stored separately in the game's data file and mean quite different
///         things, which is worth keeping in mind while editing. A screen holds tile numbers, so
///         changing one affects that screen alone. A collision mask belongs to a tile <i>image</i>,
///         so changing one affects every screen that has ever used that tile.
///     </para>
///     <para>
///         Each screen also carries a <see cref="GameScreenData.CollisionMap" />, but the game throws
///         that away and rebuilds it from the masks the moment the blob walks onto the screen. The
///         editor therefore leaves whatever was in the file untouched rather than trying to keep
///         derived data in step with edits that will never be read.
///     </para>
/// </remarks>
public sealed class EditorDocument
{

    /// <summary>
    ///     The tile number that means "nothing here". The game skips these when it draws a screen and
    ///     when it builds a collision map, so they are holes rather than black tiles.
    /// </summary>
    public const int EmptyTile = -1;

    private readonly GameScreenData[] _screens;

    private int[,,] _collisionMasks;

    public EditorDocument(GameScreenData[] screens, int[,,] collisionMasks)
    {
        _screens = screens;
        _collisionMasks = collisionMasks;
    }

    /// <summary>
    ///     Raised whenever a single tile on a screen changes, so that views holding a rendered copy of
    ///     the map can patch just the part that moved instead of redrawing everything.
    /// </summary>
    public event Action<int, int, int>? TileChanged;

    /// <summary>
    ///     Raised whenever a tile image's collision mask changes. Unlike a tile edit this can affect
    ///     any screen, so listeners have no choice but to refresh in full.
    /// </summary>
    public event Action<int>? CollisionMaskChanged;

    /// <summary>
    ///     Where this map was loaded from, and where <c>Save</c> will put it back. Null for a map that
    ///     has been created but never written.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    ///     Whether there are edits that have not been written to <see cref="FilePath" /> yet.
    /// </summary>
    public bool IsDirty { get; set; }

    public GameScreenData[] Screens => _screens;

    public int[,,] CollisionMasks => _collisionMasks;

    /// <summary>
    ///     How many tile images the collision masks cover. A tile sheet with more tiles than this can
    ///     still be painted with; <see cref="EnsureCollisionCapacity" /> grows the masks to match.
    /// </summary>
    private int CollisionTileCount => _collisionMasks.GetLength(0);

    /// <summary>
    ///     An empty planet: every screen present but blank, and every tile passable.
    /// </summary>
    public static EditorDocument CreateEmpty()
    {
        var screens = new GameScreenData[WorldMap.ScreenCount];
        for (var screen = 0; screen < screens.Length; screen++)
        {
            screens[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown)
            {
                CollisionMap = []
            };
        }

        var masks = new int[TunnelTiles.LastMouth + 1,
            TileMetrics.CollisionCellsAcrossTile,
            TileMetrics.CollisionCellsDownTile];

        return new EditorDocument(screens, masks);
    }

    public int GetTile(int screen, int x, int y)
    {
        var tiles = _screens[screen].ScreenTiles;
        return tiles is null ? EmptyTile : tiles[x, y].TileImage;
    }

    public void SetTile(int screen, int x, int y, int tileImage)
    {
        var tiles = _screens[screen].ScreenTiles;
        if (tiles is null || tiles[x, y].TileImage == tileImage)
        {
            return;
        }

        tiles[x, y].TileImage = tileImage;
        IsDirty = true;
        TileChanged?.Invoke(screen, x, y);
    }

    /// <summary>
    ///     Whether one cell of one tile image blocks movement. Collision is tested on a finer grid
    ///     than the tiles are drawn on, so a platform can be solid along its top edge alone.
    /// </summary>
    public bool IsSolid(int tileImage, int cellX, int cellY)
        => tileImage >= 0 &&
           tileImage < CollisionTileCount &&
           _collisionMasks[tileImage, cellX, cellY] == 1;

    public void SetSolid(int tileImage, int cellX, int cellY, bool solid)
    {
        if (tileImage < 0 || tileImage >= CollisionTileCount)
        {
            return;
        }

        var value = solid ? 1 : 0;
        if (_collisionMasks[tileImage, cellX, cellY] == value)
        {
            return;
        }

        _collisionMasks[tileImage, cellX, cellY] = value;
        IsDirty = true;
        CollisionMaskChanged?.Invoke(tileImage);
    }

    /// <summary>
    ///     Grows the collision masks so that every tile in a newly loaded sheet has one. A sheet with
    ///     fewer tiles than the masks cover is left alone: the spare masks cost nothing and throwing
    ///     them away would lose data if the original sheet were loaded again.
    /// </summary>
    public void EnsureCollisionCapacity(int tileCount)
    {
        if (tileCount <= CollisionTileCount)
        {
            return;
        }

        var grown = new int[tileCount,
            _collisionMasks.GetLength(1),
            _collisionMasks.GetLength(2)];

        for (var tile = 0; tile < CollisionTileCount; tile++)
        {
            for (var x = 0; x < _collisionMasks.GetLength(1); x++)
            {
                for (var y = 0; y < _collisionMasks.GetLength(2); y++)
                {
                    grown[tile, x, y] = _collisionMasks[tile, x, y];
                }
            }
        }

        _collisionMasks = grown;
        IsDirty = true;
    }

    /// <summary>
    ///     Every tile of one screen, in reading order. Used for copy, paste and undo, all of which
    ///     treat a screen as a single value rather than forty-eight separate ones.
    /// </summary>
    public int[] CaptureScreen(int screen)
    {
        var captured = new int[TileMetrics.TilesAcross * TileMetrics.TilesDown];
        var index = 0;

        for (var y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (var x = 0; x < TileMetrics.TilesAcross; x++)
            {
                captured[index++] = GetTile(screen, x, y);
            }
        }

        return captured;
    }

    public void RestoreScreen(int screen, int[] tiles)
    {
        var index = 0;

        for (var y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (var x = 0; x < TileMetrics.TilesAcross; x++)
            {
                SetTile(screen, x, y, tiles[index++]);
            }
        }
    }

    /// <summary>
    ///     Whether a screen has nothing painted on it at all. The world map draws these differently so
    ///     that the parts of the planet still to be built stand out from the parts that are done.
    /// </summary>
    public bool IsScreenEmpty(int screen)
    {
        for (var y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (var x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (GetTile(screen, x, y) != EmptyTile)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
