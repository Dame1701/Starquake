// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

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
///     repaint rather than which property changed. Each takes a session and listens to it, and this
///     class is the one place deciding what counts as a change worth repainting for.
/// </remarks>
internal sealed class EditorSession : ViewModelBase
{

    /// <summary>
    ///     How many recently used tiles to keep to hand. Enough for the handful a screen is usually
    ///     built from, few enough to stay a glance rather than a search.
    /// </summary>
    private int[]? _clipboard;

    private int _currentScreen;


    private int _selectedTile;

    private TileSheet? _sheet;


    private EditorTool _tool = EditorTool.Paint;


    public EditorSession()
    {
        View = new CanvasViewOptions(Invalidate);

        Markers = MarkerCatalogue.Paintable
            .Select(entry => new MarkerChoice(this, entry.Marker, entry.Face))
            .ToArray();

        Document = EditorDocument.CreateEmpty();
        AttachDocument(Document);

        Positions = new AuthoredPositions(Document);
        Palette = new TilePaletteState(Document, Invalidate);
    }

    /// <summary>The markers the toolbar offers, in the order it offers them.</summary>
    public IReadOnlyList<MarkerChoice> Markers { get; }

    public UndoStack Undo { get; } = new();

    /// <summary>
    ///     What the editor shows over and around the screen, and how large the palette draws. Kept
    ///     separate because these are preferences about viewing, not about the map.
    /// </summary>
    public CanvasViewOptions View { get; }

    /// <summary>
    ///     The positions the author marks once for the whole planet, and what is wrong with them.
    ///     Cached, because finding a marker means walking all 512 screens.
    /// </summary>
    public AuthoredPositions Positions { get; }

    /// <summary>
    ///     Which of the sheet's tiles the palette offers, and the row of recently used ones.
    ///     Narrowing over 400 tiles down is <see cref="TilePaletteState" />'s own business.
    /// </summary>
    public TilePaletteState Palette { get; }

    public EditorDocument Document { get; private set; }

    public TileSheet? Sheet
    {
        get => _sheet;
        private set => SetField(ref _sheet, value);
    }

    /// <summary>
    ///     The marker in hand, or null when an ordinary tile is in hand. The toolbar buttons bind to
    ///     this: markers have no artwork, so among 400 pieces of scenery they would be a row of
    ///     identical boxes in the palette.
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
    ///     The current screen described in words: its number, where it sits on the map, and any
    ///     special meaning the game gives it.
    /// </summary>
    public string ScreenDescription
    {
        get
        {
            string description = "Screen " + _currentScreen + "  ·  column " + ScreenColumn + ", row " + ScreenRow;

            if (Positions.Core.ScreenNumber == _currentScreen)
            {
                description += "  ·  planet core";
            }

            if (Positions.BlobStart?.ScreenNumber == _currentScreen)
            {
                description += "  ·  Blob starts here";
            }

            // Worth saying wherever the core exit is: its screen keeps the core running and has its
            // nasties switched off, and nothing on the screen itself shows either.
            if (Positions.CoreExit?.ScreenNumber == _currentScreen)
            {
                description += "  ·  Blob leaves the core here";
            }

            // A booth with no code is allowed on a half built screen, but nothing else would show
            // it. A code with no booth matters more: the game will still send somebody here, and
            // there will be no booth for them to arrive in.
            if (CurrentScreenHasTeleportBooth)
            {
                return description + "  ·  teleport code " + (CurrentTeleportCode ?? "not set");
            }

            return CurrentTeleportCode is null
                ? description
                : description + "  ·  teleport code " + CurrentTeleportCode + ", but no booth to arrive in";
        }
    }

    /// <summary>The code that reaches the screen being edited, or null if it has none.</summary>
    public string? CurrentTeleportCode => Document.GetTeleportCode(_currentScreen);

    /// <summary>
    ///     Whether the screen being edited has a teleport booth, and so can be reached by a code.
    /// </summary>
    public bool CurrentScreenHasTeleportBooth => Document.HasTeleportBooth(_currentScreen);

    /// <summary>
    ///     Whether the teleport box has anything to show. True when the screen has a booth, or has a
    ///     code left behind by an erased booth.
    /// </summary>
    public bool ShowTeleportPanel => CurrentScreenHasTeleportBooth || CurrentTeleportCode is not null;

    /// <summary>
    ///     The code as the box shows it. Has to read as something even when there is no code.
    /// </summary>
    public string TeleportCodeDisplay => CurrentTeleportCode ?? "NOT SET";

    /// <summary>Whether the screen being edited has a signpost to write a code across.</summary>
    public bool CurrentScreenHasSignpost => Document.HasSignpost(_currentScreen);

    /// <summary>
    ///     The screen this screen's signposts are pointed at, or null if they are not pointed
    ///     anywhere.
    /// </summary>
    public int? CurrentSignpostedScreen => Document.GetSignpostedScreen(_currentScreen);

    /// <summary>
    ///     Whether the signpost box has anything to show. True when the screen has a sign, or is
    ///     still pointed at by one that has been erased.
    /// </summary>
    public bool ShowSignpostPanel => CurrentScreenHasSignpost || CurrentSignpostedScreen is not null;

    /// <summary>
    ///     The code this screen's signs read, as the box shows it. A sign with no code is drawn
    ///     blank, so the box has to say what the canvas cannot.
    /// </summary>
    public string SignpostCodeDisplay => Document.GetSignpostCode(_currentScreen) ?? "BLANK";

    /// <summary>
    ///     The line under the sign's code. Covers three cases: a sign with nothing on it, a sign
    ///     pointed at a screen whose code has been removed, and a sign doing its job.
    /// </summary>
    public string SignpostNote
    {
        get
        {
            if (!CurrentScreenHasSignpost)
            {
                return "There is no signpost on this screen to write a code on.";
            }

            if (CurrentSignpostedScreen is not { } signposted)
            {
                return "Until this is set, the sign stands blank.";
            }

            return Document.GetSignpostCode(_currentScreen) is null
                ? "Screen " + signposted + " has lost its code, so the sign has nothing to say."
                : "The sign tells the player how to reach screen " + signposted + ".";
        }
    }

    /// <summary>
    ///     The line under the teleport code. Covers three cases: a booth nothing can reach yet, a
    ///     booth that can be reached, and a code with no booth left to arrive in.
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
            Palette.Remember(value);

            foreach (MarkerChoice choice in Markers)
            {
                choice.NotifySelectionChanged();
            }

            // Choosing something to lay down puts the brush back in hand. Fill is left alone,
            // because flooding with a tile just chosen is reasonable. Fill never holds a marker.
            if (value != EditorDocument.EmptyTile &&
                (MarkerTiles.Contains(value) || _tool is EditorTool.Erase or EditorTool.Pick or EditorTool.Core))
            {
                Tool = EditorTool.Paint;
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
            SelectedTile = WhatGoesInHandFor(value);

            SetField(ref _tool, value);
            Invalidate();
        }
    }

    /// <summary>
    ///     What goes back in hand when the brush is picked up empty: the last ordinary tile used, or
    ///     the first of the sheet if none has been. A session opens with the same thing.
    /// </summary>
    /// <remarks>
    ///     The palette's recent row never holds a marker or an empty tile, so its head is always an
    ///     ordinary tile. See <see cref="TilePaletteState.Remember" />.
    /// </remarks>
    private int LastOrdinaryTile => Palette.RecentTiles.Count > 0 ? Palette.RecentTiles[0] : 0;


    /// <summary>What the pointer is currently over, for the status bar.</summary>
    public bool CanPaste => _clipboard is not null;

    /// <summary>
    ///     What should be in hand for a given tool. A lit palette swatch or marker button means "this
    ///     is what would be laid down", so a tool that lays nothing must leave them dark and a tool
    ///     that lays something must have something to lay.
    /// </summary>
    private int WhatGoesInHandFor(EditorTool tool)
        => tool switch
        {
            // Nothing: the rubber ignores what is in hand, the dropper is about to replace it, and
            // the core is placed by position rather than as a tile.
            EditorTool.Erase or EditorTool.Pick or EditorTool.Core => EditorDocument.EmptyTile,

            // Something, or the brush paints holes. Picking it up after erasing restores the tile
            // that was in hand before.
            EditorTool.Paint when _selectedTile == EditorDocument.EmptyTile => LastOrdinaryTile,

            // Something, and never a marker. A marker is a single authored position, so flooding a
            // screen with one makes no sense.
            EditorTool.Fill when _selectedTile == EditorDocument.EmptyTile || SelectedMarker is not null
                => LastOrdinaryTile,

            _ => _selectedTile
        };

    /// <summary>Raised when a view's picture of the map is stale and needs drawing again.</summary>
    public event EventHandler? Invalidated;

    /// <summary>
    ///     Raised when a teleport booth is shift-clicked to set its code. The session cannot show a
    ///     dialog, so the window does and comes back through <see cref="SetTeleportCode" />.
    /// </summary>
    public event EventHandler? TeleportCodeRequested;

    /// <inheritdoc cref="TeleportCodeRequested" />
    public event EventHandler? SignpostRequested;

    public void SetDocument(EditorDocument document)
    {
        DetachDocument(Document);

        Document = document;
        AttachDocument(document);

        Undo.Clear();
        _currentScreen = Math.Clamp(_currentScreen, 0, document.Screens.Length - 1);

        Palette.Rebind(Sheet, document);

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

        // A marker survives a change of sheet, because it is not on one.
        if (!sheet.Contains(_selectedTile) && !MarkerTiles.Contains(_selectedTile))
        {
            SelectedTile = 0;
        }

        // Seed the recent row so it is never an empty strip waiting for its first tile.
        Palette.Remember(_selectedTile);

        Palette.Rebind(sheet, Document);

        Invalidate();
    }

    private void Invalidate() => Invalidated?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Applies a tool at a tile position on the current screen, recording what it does so it can
    ///     be undone. The caller must have opened a batch on the undo stack for the whole gesture.
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
                // Handled by the canvas, which has the pointer in pixels rather than tiles.
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
    ///     Clears an existing one-per-map marker before the same marker is laid down elsewhere, so
    ///     painting it reads as moving it. Two start positions would leave the game picking one,
    ///     which is worse than either answer.
    /// </summary>
    private void TakeAwayMarkerElsewhere(int tileImage, int x, int y)
    {
        if (!MarkerTiles.Contains(tileImage) || !MarkerTiles.IsOnePerMap((MarkerTile)tileImage))
        {
            return;
        }

        if (Document.FindMarker((MarkerTile)tileImage) is not { } placed ||
            placed.ScreenNumber == _currentScreen && placed.TileX == x && placed.TileY == y)
        {
            return;
        }

        Document.SetTile(placed.ScreenNumber, placed.TileX, placed.TileY, EditorDocument.EmptyTile);
        Undo.Record(new TileEdit(placed.ScreenNumber, placed.TileX, placed.TileY, tileImage,
            EditorDocument.EmptyTile));
    }

    /// <summary>
    ///     Replaces the connected run of identical tiles a position belongs to. A plain four-way
    ///     flood, because screens are small.
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
    ///     Puts the planet core at a position given in screen pixels. Rounded to the core's placement
    ///     step and clamped inside the screen, so all of it stays visible.
    /// </summary>
    public void PlaceCore(int xPos, int yPos)
    {
        CoreSite existing = Document.FindCore();
        CorePlacement placement = CoreSite.Snap(xPos, yPos);

        bool wasPlaced = Positions.CoreIsPlaced;

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

    /// <inheritdoc cref="RequestTeleportCodeEdit" />
    public void RequestSignpostEdit() => SignpostRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Gives the screen being edited a destination code, or removes its code when passed null.
    ///     Stores the normalised form, which is what the booth matches against.
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
    ///     Points this screen's signposts at a screen, or leaves them blank when passed null. Stores
    ///     the screen rather than its code, so changing that code changes what the sign reads.
    /// </summary>
    public void SetSignpostedScreen(int? signposted)
    {
        int? existing = Document.GetSignpostedScreen(_currentScreen);
        if (existing == signposted)
        {
            return;
        }

        Document.SetSignpostedScreen(_currentScreen, signposted);
        Undo.Record(new SignpostEdit(_currentScreen, existing, signposted));
    }

    /// <summary>
    ///     What is wrong with a code being typed, or null if nothing is. The booth's keypad can only
    ///     spell five letters, and a code already used by another screen would leave one of the two
    ///     unreachable.
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
    ///     Paints the whole current screen with the selected tile. The quickest way to start a screen
    ///     that is mostly one piece of scenery.
    /// </summary>
    public void FloodScreen()
    {
        int[] filled = new int[TileMetrics.TilesAcross * TileMetrics.TilesDown];
        Array.Fill(filled, _selectedTile);

        ReplaceScreen(filled);
    }

    /// <summary>
    ///     Steps to the screen in the given direction. The map is a grid of screen numbers, so moving
    ///     down is a whole row's worth of numbers. Moving off one edge would land on the other, so
    ///     this stops at the edges instead.
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
        document.SignpostChanged += OnSignpostChanged;
        document.CoreChanged += OnCoreChanged;
    }

    private void DetachDocument(EditorDocument document)
    {
        document.TileChanged -= OnTileChanged;
        document.CollisionMaskChanged -= OnCollisionMaskChanged;
        document.TeleportCodeChanged -= OnTeleportCodeChanged;
        document.SignpostChanged -= OnSignpostChanged;
        document.CoreChanged -= OnCoreChanged;
    }

    private void OnTileChanged(int screen, int x, int y)
    {

        // A booth may have just been painted or erased, which changes what the teleport box says,
        // including whether it appears at all.
        RaiseTeleportPropertiesChanged();

        // So may either half of the landing site, which is worth a walk of the map because both are
        // described wherever they are, not only on their own screen.
        FindAuthoredPositions();

        // Painting may have added a tile to the set the map uses, or removed the last of one. The
        // palette redraws itself when it is showing that set; otherwise this does.
        if (!Palette.MapChanged())
        {
            Invalidate();
        }
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

    private void OnSignpostChanged(int screen)
    {
        RaiseTeleportPropertiesChanged();
        Invalidate();
    }

    /// <summary>
    ///     Announces everything derived from the current screen's teleport code and signpost. None of
    ///     it has a backing field, so there is nothing to compare against and nothing to skip.
    /// </summary>
    private void RaiseTeleportPropertiesChanged()
    {
        RaisePropertyChanged(nameof(ScreenDescription));
        RaisePropertyChanged(nameof(CurrentTeleportCode));
        RaisePropertyChanged(nameof(CurrentScreenHasTeleportBooth));
        RaisePropertyChanged(nameof(ShowTeleportPanel));
        RaisePropertyChanged(nameof(TeleportCodeDisplay));
        RaisePropertyChanged(nameof(TeleportCodeNote));
        RaisePropertyChanged(nameof(CurrentScreenHasSignpost));
        RaisePropertyChanged(nameof(CurrentSignpostedScreen));
        RaisePropertyChanged(nameof(ShowSignpostPanel));
        RaisePropertyChanged(nameof(SignpostCodeDisplay));
        RaisePropertyChanged(nameof(SignpostNote));
    }

    /// <summary>
    ///     Reads the authored positions back out of the map and announces the screen description.
    /// </summary>
    /// <remarks>
    ///     Called after any tile edit, which is more often than strictly needed. An edit is a
    ///     keystroke's worth of work and this is a few walks of an array, which is cheaper than
    ///     working out which edits could have moved a marker.
    /// </remarks>
    private void FindAuthoredPositions()
    {
        Positions.Refresh(Document);

        RaisePropertyChanged(nameof(ScreenDescription));
    }
}
