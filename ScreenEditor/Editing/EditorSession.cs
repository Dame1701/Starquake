// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.ObjectModel;
using Starquake.Constants;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.ViewModels;
using Starquake.SharedLibs;
using Starquake.Structure;

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
internal sealed class EditorSession : ViewModelBase
{

    /// <summary>
    ///     How many recently used tiles to keep to hand. Enough to cover the handful a screen is
    ///     usually built from, short enough to stay a glance rather than a search.
    /// </summary>
    private const int RecentTileCount = 8;

    private readonly ObservableCollection<int> _recentTiles = [];

    private MarkerPlacement? _blobStart;

    private MarkerPlacement? _coreExit;

    private CoreSite _core = CoreSite.Original;

    private int[]? _clipboard;

    private int _currentScreen;

    private string _hoverStatus = string.Empty;

    private double _paletteTileWidth = 64;

    private int _selectedTile;

    private TileSheet? _sheet;

    private MarkerPlacement? _shipCrashSite;

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
        Markers = MarkerCatalogue.Paintable
            .Select(entry => new MarkerChoice(this, entry.Marker, entry.Face))
            .ToArray();

        Document = EditorDocument.CreateEmpty();
        AttachDocument(Document);
        FindAuthoredPositions();
    }

    /// <summary>
    ///     The markers the toolbar offers, in the order it offers them.
    /// </summary>
    public IReadOnlyList<MarkerChoice> Markers { get; }

    public UndoStack Undo { get; } = new();

    public EditorDocument Document { get; private set; }

    public TileSheet? Sheet
    {
        get => _sheet;
        private set => SetField(ref _sheet, value);
    }

    public IReadOnlyList<int> RecentTiles => _recentTiles;

    /// <summary>
    ///     The marker in hand, or null when what is in hand is an ordinary tile. Markers are picked from
    ///     the toolbar rather than the palette - there is no artwork to show for one, so a row of them
    ///     among four hundred pieces of scenery would be a row of identical boxes - and this is what
    ///     those buttons bind to.
    /// </summary>
    public MarkerTile? SelectedMarker
    {
        get => MarkerTiles.Contains(_selectedTile) ? (MarkerTile)_selectedTile : null;
        set
        {
            if (value is { } marker)
            {
                SelectedTile = (int)marker;
            }
        }
    }

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
            RaiseTeleportPropertiesChanged();
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

            if (_core.ScreenNumber == _currentScreen)
            {
                description += "  ·  planet core";
            }

            if (_blobStart?.ScreenNumber == _currentScreen)
            {
                description += "  ·  Blob starts here";
            }

            // The core's doorstep is worth saying wherever it is, because the screen it sits on is kept
            // running while Blob stands there and has its nasties switched off - neither of which
            // anything on the screen would show.
            if (_coreExit?.ScreenNumber == _currentScreen)
            {
                description += "  ·  Blob leaves the core here";
            }

            // A booth with no code is not an error - a half built screen is allowed - but it is
            // worth saying, because nothing else on the screen would show it. A code on a screen
            // with no booth is worth saying for the opposite reason: the game will still send
            // somebody here, and there will be no booth for them to arrive in.
            if (CurrentScreenHasTeleportBooth)
            {
                return description + "  ·  teleport code " + (CurrentTeleportCode ?? "not set");
            }

            return CurrentTeleportCode is null
                ? description
                : description + "  ·  teleport code " + CurrentTeleportCode + ", but no booth to arrive in";
        }
    }

    /// <summary>
    ///     Where the blob's ship came down, as the map says: the square it is put down on and the square
    ///     the wreck rests in. Held rather than asked for, because finding a marker means walking all
    ///     512 screens and these are read by everything that describes the map.
    /// </summary>
    public MarkerPlacement? BlobStart => _blobStart;

    /// <inheritdoc cref="BlobStart" />
    public MarkerPlacement? ShipCrashSite => _shipCrashSite;

    /// <summary>
    ///     Where Blob is put down on leaving the planet core, as the map says, or null if no square has
    ///     been marked and the game will fall back to the one it shipped with.
    /// </summary>
    public MarkerPlacement? CoreExit => _coreExit;

    /// <summary>
    ///     Where the planet core is. Never null: a map that does not place it reads back as the position
    ///     the game falls back to, which is what it would actually use.
    /// </summary>
    public CoreSite Core => _core;

    /// <summary>
    ///     Whether the map places the core itself, as opposed to reading back the fallback.
    /// </summary>
    public bool CoreIsPlaced => Document.CoreCount > 0;

    /// <summary>
    ///     What is wrong with the landing site, or null when there is nothing wrong with it. The game
    ///     opens on the screen the start position is on, so a missing marker or a wreck on another
    ///     screen means a map that does not begin the way its author meant it to.
    /// </summary>
    public string? LandingSiteProblem
    {
        get
        {
            if (_blobStart is not { } start)
            {
                return _shipCrashSite is null
                    ? "No start position has been placed. Blob has nowhere to begin."
                    : "No start position has been placed, so the crash site is on its own.";
            }

            if (_shipCrashSite is not { } crash)
            {
                return "No crash site has been placed. There would be no wreck to climb out of.";
            }

            return crash.ScreenNumber == start.ScreenNumber
                ? null
                : "The crash site is on screen " + crash.ScreenNumber + ", away from the start on screen " +
                  start.ScreenNumber + ". Both belong on one screen.";
        }
    }

    /// <summary>
    ///     The landing site as the panel shows it, which has to read as something even when half of it
    ///     has not been placed.
    /// </summary>
    public string LandingSiteDisplay
    {
        get
        {
            if (_blobStart is not { } start)
            {
                return "NOT SET";
            }

            return "Screen " + start.ScreenNumber + "  ·  start " + Square(start) +
                   "  ·  crash " + (_shipCrashSite is { } crash ? Square(crash) : "not set");
        }
    }

    /// <summary>
    ///     What the landing site box says underneath: what is wrong with it, or what it means when
    ///     nothing is.
    /// </summary>
    public string LandingSiteNote
        => LandingSiteProblem ?? "A new game opens here, with Blob stepping out of the wreck.";

    /// <summary>
    ///     The destination code that reaches the screen being edited, or null if it has none.
    /// </summary>
    public string? CurrentTeleportCode => Document.GetTeleportCode(_currentScreen);

    /// <summary>
    ///     Whether the screen being edited has a teleport booth on it, and so is a screen a code can
    ///     reach.
    /// </summary>
    public bool CurrentScreenHasTeleportBooth => Document.HasTeleportBooth(_currentScreen);

    /// <summary>
    ///     Whether the teleport box has anything to show: the screen has a booth on it, or it has a
    ///     code left behind by a booth that was erased.
    /// </summary>
    public bool ShowTeleportPanel => CurrentScreenHasTeleportBooth || CurrentTeleportCode is not null;

    /// <summary>
    ///     The code as the box shows it, which has to read as something even when there is none.
    /// </summary>
    public string TeleportCodeDisplay => CurrentTeleportCode ?? "NOT SET";

    /// <summary>
    ///     What the box says underneath the code. All three cases are worth a sentence: a booth
    ///     nothing can reach yet, a booth that can be reached, and a code with no booth left to
    ///     arrive in.
    /// </summary>
    public string TeleportCodeNote
    {
        get
        {
            if (!CurrentScreenHasTeleportBooth)
            {
                return "There is no booth on this screen to arrive in.";
            }

            return CurrentTeleportCode is null
                ? "Until this is set, no booth can reach this screen."
                : "Typed into any other booth, this code transports Blob here.";
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
            RaisePropertyChanged(nameof(SelectedMarker));
            RememberTile(value);

            foreach (MarkerChoice choice in Markers)
            {
                choice.NotifySelectionChanged();
            }

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
    ///     Raised when a teleport booth has been shift-clicked, asking for its code to be set. The
    ///     session has no way to put a dialog up itself, so the window does that and comes back
    ///     through <see cref="SetTeleportCode" />.
    /// </summary>
    public event EventHandler? TeleportCodeRequested;

    public void SetDocument(EditorDocument document)
    {
        DetachDocument(Document);

        Document = document;
        AttachDocument(document);

        Undo.Clear();
        _currentScreen = Math.Clamp(_currentScreen, 0, document.Screens.Length - 1);
        _tilesInUse = null;

        RebuildVisibleTiles();

        FindAuthoredPositions();

        RaisePropertyChanged(nameof(Document));
        RaisePropertyChanged(nameof(CurrentScreen));
        RaiseTeleportPropertiesChanged();

        Invalidate();
    }

    public void SetSheet(TileSheet sheet)
    {
        Sheet?.Dispose();
        Sheet = sheet;

        Document.EnsureCollisionCapacity(sheet.TileCount);

        // A marker survives a change of sheet: it is not on one, so having it in hand says nothing
        // about which artwork is loaded.
        if (!sheet.Contains(_selectedTile) && !MarkerTiles.Contains(_selectedTile))
        {
            SelectedTile = 0;
        }

        // Seed the recent row so that it is never an empty strip waiting for its first tile.
        RememberTile(_selectedTile);

        RebuildVisibleTiles();

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

            case EditorTool.Core:
                // Handled by the canvas, which has the pointer in pixels rather than in tiles.
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

        TakeAwayMarkerElsewhere(tileImage, x, y);

        Document.SetTile(_currentScreen, x, y, tileImage);
        Undo.Record(new TileEdit(_currentScreen, x, y, existing, tileImage));
    }

    /// <summary>
    ///     Clears the one already on the map before a marker there should only be one of is laid down
    ///     somewhere else, so that painting it reads as moving it. Two start positions would leave the
    ///     game picking one of them, which is worse than either answer.
    /// </summary>
    private void TakeAwayMarkerElsewhere(int tileImage, int x, int y)
    {
        if (!MarkerTiles.Contains(tileImage) || !MarkerTiles.IsOnePerMap((MarkerTile)tileImage))
        {
            return;
        }

        if (Document.FindMarker((MarkerTile)tileImage) is not { } placed ||
            (placed.ScreenNumber == _currentScreen && placed.TileX == x && placed.TileY == y))
        {
            return;
        }

        Document.SetTile(placed.ScreenNumber, placed.TileX, placed.TileY, EditorDocument.EmptyTile);
        Undo.Record(new TileEdit(placed.ScreenNumber, placed.TileX, placed.TileY, tileImage,
            EditorDocument.EmptyTile));
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

    /// <summary>
    ///     Puts the planet core where the pointer is, given a position in screen pixels. Rounded to the
    ///     step the core is placed on and held inside the screen, so the whole of it stays visible.
    /// </summary>
    public void PlaceCore(int xPos, int yPos)
    {
        CoreSite existing = Document.FindCore();
        CorePlacement placement = CoreSite.Snap(xPos, yPos);

        bool wasPlaced = CoreIsPlaced;

        if (wasPlaced &&
            existing.ScreenNumber == _currentScreen &&
            existing.XPos == placement.XPos &&
            existing.YPos == placement.YPos)
        {
            return;
        }

        Document.SetCore(_currentScreen, placement);

        Undo.Record(new CoreEdit(
            existing.ScreenNumber,
            wasPlaced ? new CorePlacement(existing.XPos, existing.YPos) : null,
            _currentScreen,
            placement));
    }

    public void RequestTeleportCodeEdit() => TeleportCodeRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Gives the screen being edited a destination code, or takes its code away when passed null.
    ///     What is stored is the normalised code, so that it is what the booth matches against rather
    ///     than however it happened to be typed.
    /// </summary>
    public void SetTeleportCode(string? code)
    {
        string? normalised = code is null ? null : TeleportCodes.Normalise(code);
        if (normalised?.Length == 0)
        {
            normalised = null;
        }

        string? existing = Document.GetTeleportCode(_currentScreen);
        if (existing == normalised)
        {
            return;
        }

        Document.SetTeleportCode(_currentScreen, normalised);
        Undo.Record(new TeleportCodeEdit(_currentScreen, existing, normalised));
    }

    /// <summary>
    ///     What is wrong with a code somebody is typing, or null if nothing is. The booth's keypad can
    ///     only spell five letters, and a code that already belongs to another screen would leave one
    ///     of the two unreachable.
    /// </summary>
    public string? DescribeTeleportCodeProblem(string code)
    {
        string normalised = TeleportCodes.Normalise(code);

        if (!TeleportCodes.IsWellFormed(normalised))
        {
            return "A code is exactly " + TeleportCodes.CodeLength + " letters, A to Z.";
        }

        int owner = Document.FindScreenByTeleportCode(normalised);

        return owner < 0 || owner == _currentScreen
            ? null
            : "Screen " + owner + " already answers to that code.";
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
        document.TeleportCodeChanged += OnTeleportCodeChanged;
        document.CoreChanged += OnCoreChanged;
    }

    private void DetachDocument(EditorDocument document)
    {
        document.TileChanged -= OnTileChanged;
        document.CollisionMaskChanged -= OnCollisionMaskChanged;
        document.TeleportCodeChanged -= OnTeleportCodeChanged;
        document.CoreChanged -= OnCoreChanged;
    }

    private void OnTileChanged(int screen, int x, int y)
    {
        _tilesInUse = null;

        // A booth can have just been painted or erased, which changes what the teleport box has to
        // say about the screen - including whether it is there at all.
        RaiseTeleportPropertiesChanged();

        // So can either half of the landing site, and that is worth a walk of the map: both are
        // described wherever they are, not only on the screen they sit on.
        FindAuthoredPositions();

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

    private void OnCoreChanged()
    {
        FindAuthoredPositions();
        Invalidate();
    }

    private void OnTeleportCodeChanged(int screen)
    {
        RaiseTeleportPropertiesChanged();
        Invalidate();
    }

    /// <summary>
    ///     Announces everything that is worked out from the current screen's teleport code. None of
    ///     it has a backing field to set, so there is nothing to compare and nothing to skip.
    /// </summary>
    private void RaiseTeleportPropertiesChanged()
    {
        RaisePropertyChanged(nameof(ScreenDescription));
        RaisePropertyChanged(nameof(CurrentTeleportCode));
        RaisePropertyChanged(nameof(CurrentScreenHasTeleportBooth));
        RaisePropertyChanged(nameof(ShowTeleportPanel));
        RaisePropertyChanged(nameof(TeleportCodeDisplay));
        RaisePropertyChanged(nameof(TeleportCodeNote));
    }

    /// <summary>
    ///     Reads the authored positions off the map again - the landing site and the core's doorstep.
    ///     Called after any tile edit, which is more often than it is strictly needed - but an edit is a
    ///     keystroke's worth of work and this is a few walks of an array, against having to work out
    ///     which edits could have moved a marker.
    /// </summary>
    private void FindAuthoredPositions()
    {
        _blobStart = Document.FindMarker(MarkerTile.BlobStartPosition);
        _shipCrashSite = Document.FindMarker(MarkerTile.ShipCrashSite);
        _coreExit = Document.FindMarker(MarkerTile.CoreExitPosition);
        _core = Document.FindCore();

        RaisePropertyChanged(nameof(BlobStart));
        RaisePropertyChanged(nameof(ShipCrashSite));
        RaisePropertyChanged(nameof(CoreExit));
        RaisePropertyChanged(nameof(Core));
        RaisePropertyChanged(nameof(CoreIsPlaced));
        RaisePropertyChanged(nameof(LandingSiteProblem));
        RaisePropertyChanged(nameof(LandingSiteDisplay));
        RaisePropertyChanged(nameof(LandingSiteNote));
        RaisePropertyChanged(nameof(ScreenDescription));
    }

    /// <summary>
    ///     A marker's square as a person would say it.
    /// </summary>
    private static string Square(MarkerPlacement placement)
        => placement.TileX + ", " + placement.TileY;

    private void RememberTile(int tileImage)
    {
        // A marker has a button of its own on the toolbar, so it is never something that had to be
        // hunted down in the sheet - which is the only thing this row is for.
        if (tileImage < 0 || MarkerTiles.Contains(tileImage))
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
