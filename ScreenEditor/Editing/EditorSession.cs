// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Starquake.Constants;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.ViewModels;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     Everything the editor is currently doing: the map, the tile sheet it is being drawn with,
///     where in the world the view is, and which tile and tool the next click will use.
/// </summary>
/// <remarks>
///     The views are custom drawn rather than composed from controls, so they need to know when to
///     repaint rather than which property changed. Each one takes a session and listens to it; this
///     class is the single place that decides what counts as a change worth repainting for.
/// </remarks>
public sealed class EditorSession : ViewModelBase
{

    /// <summary>
    ///     How many recently used tiles to keep to hand. Enough to cover the handful a screen is
    ///     usually built from, short enough to stay a glance rather than a search.
    /// </summary>
    private const int RecentTileCount = 8;

    private readonly ObservableCollection<int> _recentTiles = [];

    private int[]? _clipboard;

    private int _currentScreen;

    private string _hoverStatus = string.Empty;

    private double _paletteTileWidth = 64;

    private int _selectedTile;

    private TileSheet? _sheet;

    private bool _showCollision;

    private bool _showGrid = true;

    private bool _showNeighbours = true;

    private bool _showTileNumbers;

    private string _tileFilter = string.Empty;

    private HashSet<int>? _tilesInUse;

    private EditorTool _tool = EditorTool.Paint;

    private bool _usedTilesOnly;

    private IReadOnlyList<int> _visibleTiles = [];

    public EditorSession()
    {
        Document = EditorDocument.CreateEmpty();
        AttachDocument(Document);
    }

    public UndoStack Undo { get; } = new();

    public EditorDocument Document { get; private set; }

    public TileSheet? Sheet
    {
        get => _sheet;
        private set => SetField(ref _sheet, value);
    }

    public IReadOnlyList<int> RecentTiles => _recentTiles;

    public int CurrentScreen
    {
        get => _currentScreen;
        set
        {
            int clamped = Math.Clamp(value, 0, Document.Screens.Length - 1);
            if (clamped == _currentScreen)
            {
                return;
            }

            SetField(ref _currentScreen, clamped);
            RaisePropertyChanged(nameof(ScreenColumn));
            RaisePropertyChanged(nameof(ScreenRow));
            RaisePropertyChanged(nameof(ScreenDescription));
            Invalidate();
        }
    }

    public int ScreenColumn => _currentScreen % WorldMap.ScreensAcross;

    public int ScreenRow => _currentScreen / WorldMap.ScreensAcross;

    /// <summary>
    ///     The current screen as a person would say it: its number, where it sits on the map, and
    ///     whether the game gives it any special meaning.
    /// </summary>
    public string ScreenDescription
    {
        get
        {
            string description = "Screen " + _currentScreen + "  ·  column " + ScreenColumn + ", row " + ScreenRow;

            return _currentScreen switch
            {
                WorldMap.CoreScreenNumber => description + "  ·  planet core",
                WorldMap.CoreApproachScreenNumber => description + "  ·  core approach",
                _ => description
            };
        }
    }

    public int SelectedTile
    {
        get => _selectedTile;
        set
        {
            if (value == _selectedTile)
            {
                return;
            }

            SetField(ref _selectedTile, value);
            RaisePropertyChanged(nameof(SelectedTileDescription));
            RememberTile(value);
            Invalidate();
        }
    }

    public string SelectedTileDescription => TileCatalogue.Describe(_selectedTile);

    public EditorTool Tool
    {
        get => _tool;
        set
        {
            SetField(ref _tool, value);
            Invalidate();
        }
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            SetField(ref _showGrid, value);
            Invalidate();
        }
    }

    public bool ShowCollision
    {
        get => _showCollision;
        set
        {
            SetField(ref _showCollision, value);
            Invalidate();
        }
    }

    public bool ShowTileNumbers
    {
        get => _showTileNumbers;
        set
        {
            SetField(ref _showTileNumbers, value);
            Invalidate();
        }
    }

    public bool ShowNeighbours
    {
        get => _showNeighbours;
        set
        {
            SetField(ref _showNeighbours, value);
            Invalidate();
        }
    }

    /// <summary>
    ///     How wide to draw a tile in the palette. The sheet has over four hundred tiles in it, so
    ///     being able to go from picking one out by eye to seeing most of them at once matters.
    /// </summary>
    public double PaletteTileWidth
    {
        get => _paletteTileWidth;
        set
        {
            SetField(ref _paletteTileWidth, Math.Clamp(value, 32, 192));
            Invalidate();
        }
    }

    /// <summary>
    ///     Narrows the palette to tiles whose number or name matches. Typing "tunnel" to find the
    ///     tunnel mouths beats scrolling to the bottom of the sheet and counting.
    /// </summary>
    public string TileFilter
    {
        get => _tileFilter;
        set
        {
            SetField(ref _tileFilter, value);
            RebuildVisibleTiles();
        }
    }

    /// <summary>
    ///     Hides tiles that no screen uses. The sheet holds a fair number that the finished map never
    ///     calls for, and hiding them makes the ones it does use easier to work through.
    /// </summary>
    public bool UsedTilesOnly
    {
        get => _usedTilesOnly;
        set
        {
            SetField(ref _usedTilesOnly, value);
            RebuildVisibleTiles();
        }
    }

    public IReadOnlyList<int> VisibleTiles
    {
        get => _visibleTiles;
        private set => SetField(ref _visibleTiles, value);
    }

    /// <summary>
    ///     What the pointer is currently over, for the status bar.
    /// </summary>
    public string HoverStatus
    {
        get => _hoverStatus;
        set => SetField(ref _hoverStatus, value);
    }

    public bool CanPaste => _clipboard is not null;

    /// <summary>
    ///     Raised when a view's picture of the map is no longer accurate and needs drawing again.
    /// </summary>
    public event EventHandler? Invalidated;

    /// <summary>
    ///     Raised when the map itself has been swapped out, rather than merely edited.
    /// </summary>
    public event EventHandler? DocumentReplaced;

    public void SetDocument(EditorDocument document)
    {
        DetachDocument(Document);

        Document = document;
        AttachDocument(document);

        Undo.Clear();
        _currentScreen = Math.Clamp(_currentScreen, 0, document.Screens.Length - 1);
        _tilesInUse = null;

        RebuildVisibleTiles();

        RaisePropertyChanged(nameof(Document));
        RaisePropertyChanged(nameof(CurrentScreen));
        RaisePropertyChanged(nameof(ScreenDescription));

        DocumentReplaced?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void SetSheet(TileSheet sheet)
    {
        Sheet?.Dispose();
        Sheet = sheet;

        Document.EnsureCollisionCapacity(sheet.TileCount);

        if (!sheet.Contains(_selectedTile))
        {
            SelectedTile = 0;
        }

        // Seed the recent row so that it is never an empty strip waiting for its first tile.
        RememberTile(_selectedTile);

        RebuildVisibleTiles();

        DocumentReplaced?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void Invalidate() => Invalidated?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Applies the given tool at a tile position on the current screen, recording whatever it does
    ///     so that it can be undone. Callers are expected to have opened a batch on the undo stack for
    ///     the whole gesture.
    /// </summary>
    public void ApplyTool(EditorTool tool, int x, int y)
    {
        switch (tool)
        {
            case EditorTool.Paint:
                PaintTile(x, y, _selectedTile);
                break;

            case EditorTool.Erase:
                PaintTile(x, y, EditorDocument.EmptyTile);
                break;

            case EditorTool.Fill:
                FloodFill(x, y, _selectedTile);
                break;

            case EditorTool.Pick:
                SelectedTile = Document.GetTile(_currentScreen, x, y);
                break;
        }
    }

    private void PaintTile(int x, int y, int tileImage)
    {
        int existing = Document.GetTile(_currentScreen, x, y);
        if (existing == tileImage)
        {
            return;
        }

        Document.SetTile(_currentScreen, x, y, tileImage);
        Undo.Record(new TileEdit(_currentScreen, x, y, existing, tileImage));
    }

    /// <summary>
    ///     Replaces the connected run of identical tiles the given position is part of. Screens are
    ///     small, so this is the plain four-way flood rather than anything clever.
    /// </summary>
    private void FloodFill(int x, int y, int tileImage)
    {
        int target = Document.GetTile(_currentScreen, x, y);
        if (target == tileImage)
        {
            return;
        }

        var pending = new Stack<(int X, int Y)>();
        pending.Push((x, y));

        while (pending.Count > 0)
        {
            (int cellX, int cellY) = pending.Pop();

            if (cellX < 0 || cellY < 0 ||
                cellX >= TileMetrics.TilesAcross || cellY >= TileMetrics.TilesDown ||
                Document.GetTile(_currentScreen, cellX, cellY) != target)
            {
                continue;
            }

            PaintTile(cellX, cellY, tileImage);

            pending.Push((cellX - 1, cellY));
            pending.Push((cellX + 1, cellY));
            pending.Push((cellX, cellY - 1));
            pending.Push((cellX, cellY + 1));
        }
    }

    public void ToggleCollision(int tileImage, int cellX, int cellY)
    {
        bool solid = !Document.IsSolid(tileImage, cellX, cellY);

        Document.SetSolid(tileImage, cellX, cellY, solid);
        Undo.Record(new CollisionEdit(tileImage, cellX, cellY, solid));
    }

    public void CopyScreen() => _clipboard = Document.CaptureScreen(_currentScreen);

    public void PasteScreen()
    {
        if (_clipboard is null)
        {
            return;
        }

        ReplaceScreen(_clipboard);
    }

    public void ClearScreen()
    {
        int[] blank = new int[TileMetrics.TilesAcross * TileMetrics.TilesDown];
        Array.Fill(blank, EditorDocument.EmptyTile);

        ReplaceScreen(blank);
    }

    /// <summary>
    ///     Paints the whole of the current screen with the selected tile, which is the quickest way to
    ///     start a screen that is mostly one piece of scenery.
    /// </summary>
    public void FloodScreen()
    {
        int[] filled = new int[TileMetrics.TilesAcross * TileMetrics.TilesDown];
        Array.Fill(filled, _selectedTile);

        ReplaceScreen(filled);
    }

    /// <summary>
    ///     Steps to the screen that lies in the given direction. The map is a grid of screen numbers
    ///     rather than a scrolling world, so moving down is a whole row's worth of numbers and moving
    ///     off one edge would land on the other - which is why this stops at the edges instead.
    /// </summary>
    public void MoveScreen(int columns, int rows)
    {
        int column = ScreenColumn + columns;
        int row = ScreenRow + rows;

        if (column < 0 || column >= WorldMap.ScreensAcross ||
            row < 0 || row >= WorldMap.ScreensDown)
        {
            return;
        }

        CurrentScreen = row * WorldMap.ScreensAcross + column;
    }

    private void ReplaceScreen(int[] tiles)
    {
        int[] existing = Document.CaptureScreen(_currentScreen);
        if (existing.SequenceEqual(tiles))
        {
            return;
        }

        int[] replacement = (int[])tiles.Clone();

        Document.RestoreScreen(_currentScreen, replacement);
        Undo.Record(new ScreenEdit(_currentScreen, existing, replacement));
        Undo.Commit();
    }

    private void AttachDocument(EditorDocument document)
    {
        document.TileChanged += OnTileChanged;
        document.CollisionMaskChanged += OnCollisionMaskChanged;
    }

    private void DetachDocument(EditorDocument document)
    {
        document.TileChanged -= OnTileChanged;
        document.CollisionMaskChanged -= OnCollisionMaskChanged;
    }

    private void OnTileChanged(int screen, int x, int y)
    {
        _tilesInUse = null;

        if (_usedTilesOnly)
        {
            // The palette is showing only what the map uses, so painting can have added a tile to
            // that list or taken the last of one away.
            RebuildVisibleTiles();
            return;
        }

        Invalidate();
    }

    private void OnCollisionMaskChanged(int tileImage) => Invalidate();

    private void RememberTile(int tileImage)
    {
        if (tileImage < 0)
        {
            return;
        }

        _recentTiles.Remove(tileImage);
        _recentTiles.Insert(0, tileImage);

        while (_recentTiles.Count > RecentTileCount)
        {
            _recentTiles.RemoveAt(_recentTiles.Count - 1);
        }

        RaisePropertyChanged(nameof(RecentTiles));
    }

    private void RebuildVisibleTiles()
    {
        if (Sheet is null)
        {
            VisibleTiles = [];
            return;
        }

        HashSet<int>? inUse = _usedTilesOnly ? TilesInUse() : null;
        string filter = _tileFilter.Trim();
        var visible = new List<int>(Sheet.TileCount);

        for (int tile = 0; tile < Sheet.TileCount; tile++)
        {
            if (inUse is not null && !inUse.Contains(tile))
            {
                continue;
            }

            if (filter.Length > 0 && !Matches(tile, filter))
            {
                continue;
            }

            visible.Add(tile);
        }

        VisibleTiles = visible;
        Invalidate();
    }

    private static bool Matches(int tileImage, string filter)
    {
        if (tileImage.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string? name = TileCatalogue.SpecialName(tileImage);

        return name is not null && name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private HashSet<int> TilesInUse()
    {
        if (_tilesInUse is not null)
        {
            return _tilesInUse;
        }

        var tiles = new HashSet<int>();

        for (int screen = 0; screen < Document.Screens.Length; screen++)
        {
            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    tiles.Add(Document.GetTile(screen, x, y));
                }
            }
        }

        _tilesInUse = tiles;

        return tiles;
    }
}
