// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     One reversible change to the map.
/// </summary>
public interface IEdit
{

    /// <summary>
    ///     What to call this in the Edit menu, phrased so that it reads after "Undo".
    /// </summary>
    string Name { get; }

    void Undo(EditorDocument document);

    void Redo(EditorDocument document);
}

/// <summary>
///     A single tile on a single screen changing from one number to another.
/// </summary>
public sealed class TileEdit(int screen, int x, int y, int oldTile, int newTile) : IEdit
{
    public string Name => "tile";

    public void Undo(EditorDocument document) => document.SetTile(screen, x, y, oldTile);

    public void Redo(EditorDocument document) => document.SetTile(screen, x, y, newTile);
}

/// <summary>
///     One cell of one tile image's collision mask being made solid or passable.
/// </summary>
public sealed class CollisionEdit(int tileImage, int cellX, int cellY, bool solid) : IEdit
{
    public string Name => "collision";

    public void Undo(EditorDocument document) => document.SetSolid(tileImage, cellX, cellY, !solid);

    public void Redo(EditorDocument document) => document.SetSolid(tileImage, cellX, cellY, solid);
}

/// <summary>
///     A whole screen being replaced at once, as a paste, a fill or a clear does.
/// </summary>
public sealed class ScreenEdit(int screen, int[] oldTiles, int[] newTiles) : IEdit
{
    public string Name => "screen";

    public void Undo(EditorDocument document) => document.RestoreScreen(screen, oldTiles);

    public void Redo(EditorDocument document) => document.RestoreScreen(screen, newTiles);
}

/// <summary>
///     Several edits that should undo together. A brush stroke is the reason this exists: dragging
///     across a screen is one gesture to the person doing it, so it should take one press of Ctrl+Z
///     to take back rather than one per tile the pointer happened to cross.
/// </summary>
public sealed class CompoundEdit(string name, IReadOnlyList<IEdit> edits) : IEdit
{
    public string Name { get; } = name;

    public void Undo(EditorDocument document)
    {
        // Backwards, so that overlapping edits unwind in the order they were laid down.
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
///     The undo history. Edits are recorded as they are applied rather than replayed through here,
///     so the stack never applies an edit it has not been given both halves of.
/// </summary>
public sealed class UndoStack
{

    /// <summary>
    ///     How many steps of history to keep. Tile edits are tiny, so this is generous enough that
    ///     running out of it in one sitting is unlikely.
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
        // Anything undone is unreachable once a new edit is made on top of it.
        _undone.Clear();
        _done.Add(edit);

        if (_done.Count > MaxDepth)
        {
            _done.RemoveAt(0);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
