// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.SharedLibs;

namespace Starquake.ScreenEditor.Editing;

/// <summary>One reversible change to the map.</summary>
internal interface IEdit
{

    /// <summary>What to call this in the Edit menu, phrased to read after "Undo".</summary>
    string Name { get; }

    void Undo(EditorDocument document);

    void Redo(EditorDocument document);
}

/// <summary>A single tile on a single screen changing from one number to another.</summary>
internal sealed class TileEdit(int screen, int x, int y, int oldTile, int newTile) : IEdit
{
    public string Name => "tile";

    public void Undo(EditorDocument document) => document.SetTile(screen, x, y, oldTile);

    public void Redo(EditorDocument document) => document.SetTile(screen, x, y, newTile);
}

/// <summary>One cell of one tile image's collision mask being made solid or passable.</summary>
internal sealed class CollisionEdit(int tileImage, int cellX, int cellY, bool solid) : IEdit
{
    public string Name => "collision";

    public void Undo(EditorDocument document) => document.SetSolid(tileImage, cellX, cellY, !solid);

    public void Redo(EditorDocument document) => document.SetSolid(tileImage, cellX, cellY, solid);
}

/// <summary>A whole screen being replaced at once, as a paste, fill or clear does.</summary>
internal sealed class ScreenEdit(int screen, int[] oldTiles, int[] newTiles) : IEdit
{
    public string Name => "screen";

    public void Undo(EditorDocument document) => document.RestoreScreen(screen, oldTiles);

    public void Redo(EditorDocument document) => document.RestoreScreen(screen, newTiles);
}

/// <summary>
///     A screen's teleport code changing. Null on either side means no code: setting one for the
///     first time, or removing one.
/// </summary>
internal sealed class TeleportCodeEdit(int screen, string? oldCode, string? newCode) : IEdit
{
    public string Name => "teleport code";

    public void Undo(EditorDocument document) => document.SetTeleportCode(screen, oldCode);

    public void Redo(EditorDocument document) => document.SetTeleportCode(screen, newCode);
}

/// <summary>
///     A screen's signpost being pointed at a different screen. Null on either side means a sign with
///     nothing to say: not yet pointed anywhere, or had its target removed.
/// </summary>
internal sealed class SignpostEdit(int screen, int? oldSignposted, int? newSignposted) : IEdit
{
    public string Name => "signpost";

    public void Undo(EditorDocument document) => document.SetSignpostedScreen(screen, oldSignposted);

    public void Redo(EditorDocument document) => document.SetSignpostedScreen(screen, newSignposted);
}

/// <summary>
///     The planet core moving. Null on either side means a map that does not place it and falls back
///     to the game's default position.
/// </summary>
internal sealed class CoreEdit(
    int oldScreen,
    CorePlacement? oldPlacement,
    int newScreen,
    CorePlacement? newPlacement) : IEdit
{
    public string Name => "core position";

    public void Undo(EditorDocument document) => document.SetCore(oldScreen, oldPlacement);

    public void Redo(EditorDocument document) => document.SetCore(newScreen, newPlacement);
}

/// <summary>
///     Several edits that undo together. Dragging a brush across a screen is one gesture to the
///     person doing it, so it should take one Ctrl+Z to undo, not one per tile crossed.
/// </summary>
internal sealed class CompoundEdit(string name, IReadOnlyList<IEdit> edits) : IEdit
{
    public string Name { get; } = name;

    public void Undo(EditorDocument document)
    {
        // Backwards, so overlapping edits unwind in the reverse of the order they were applied.
        for (int index = edits.Count - 1; index >= 0; index--)
        {
            edits[index].Undo(document);
        }
    }

    public void Redo(EditorDocument document)
    {
        foreach (IEdit edit in edits)
        {
            edit.Redo(document);
        }
    }
}

/// <summary>
///     The undo history. Edits are recorded as they are applied rather than replayed through here, so
///     the stack never applies an edit it was not given both halves of.
/// </summary>
internal sealed class UndoStack
{

    /// <summary>
    ///     How many steps of history to keep. Tile edits are tiny, so this is generous enough that
    ///     one sitting is unlikely to exhaust it.
    /// </summary>
    private const int MaxDepth = 500;

    private readonly List<IEdit> _done = [];
    private readonly List<IEdit> _undone = [];

    private List<IEdit>? _batch;
    private string _batchName = string.Empty;

    public bool CanUndo => _done.Count > 0;

    public bool CanRedo => _undone.Count > 0;

    public string UndoDescription => CanUndo ? "Undo " + _done[^1].Name : "Undo";

    public string RedoDescription => CanRedo ? "Redo " + _undone[^1].Name : "Redo";

    public event EventHandler? Changed;

    /// <summary>
    ///     Starts gathering edits into one history entry. Every <see cref="Record" /> until
    ///     <see cref="Commit" /> joins the same entry.
    /// </summary>
    public void Begin(string name)
    {
        Commit();
        _batch = [];
        _batchName = name;
    }

    public void Record(IEdit edit)
    {
        if (_batch is not null)
        {
            _batch.Add(edit);
            return;
        }

        Push(edit);
    }

    public void Commit()
    {
        List<IEdit>? batch = _batch;
        _batch = null;

        if (batch is null || batch.Count == 0)
        {
            return;
        }

        Push(batch.Count == 1 ? batch[0] : new CompoundEdit(_batchName, batch));
    }

    public void Undo(EditorDocument document)
    {
        if (!CanUndo)
        {
            return;
        }

        IEdit edit = _done[^1];
        _done.RemoveAt(_done.Count - 1);
        edit.Undo(document);
        _undone.Add(edit);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Redo(EditorDocument document)
    {
        if (!CanRedo)
        {
            return;
        }

        IEdit edit = _undone[^1];
        _undone.RemoveAt(_undone.Count - 1);
        edit.Redo(document);
        _done.Add(edit);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _batch = null;
        _done.Clear();
        _undone.Clear();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Push(IEdit edit)
    {
        // A new edit makes anything already undone unreachable.
        _undone.Clear();
        _done.Add(edit);

        if (_done.Count > MaxDepth)
        {
            _done.RemoveAt(0);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
