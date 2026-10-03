// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ScreenEditor.ViewModels;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     What the editor shows over and around the screen being worked on, and how large the palette draws
///     its tiles. Preferences about looking, with nothing in them about the map.
/// </summary>
/// <remarks>
///     Every one of these is read by something that draws and written by a toggle on the toolbar, and not
///     one of them is touched by anything that edits. They were six fields on <see cref="EditorSession" />
///     among fourteen others that are about the map itself.
///     <para>
///         Changing one has to repaint, and the canvas already listens to the session's one
///         <c>Invalidated</c> event - so rather than raise a second event for the same purpose, this is
///         handed the session's own way of asking for a repaint.
///     </para>
/// </remarks>
internal sealed class CanvasViewOptions : ViewModelBase
{
    private readonly Action _repaint;

    public CanvasViewOptions(Action repaint) => _repaint = repaint;

    /// <summary>
    ///     The narrowest and widest a palette tile may be drawn, and the width a session opens at.
    /// </summary>
    private const double NarrowestPaletteTile = 32;

    /// <inheritdoc cref="NarrowestPaletteTile" />
    private const double WidestPaletteTile = 192;

    /// <inheritdoc cref="NarrowestPaletteTile" />
    private const double DefaultPaletteTileWidth = 64;

    public bool ShowGrid
    {
        get;
        set
        {
            SetField(ref field, value);
            _repaint();
        }
    } = true;

    public bool ShowCollision
    {
        get;
        set
        {
            SetField(ref field, value);
            _repaint();
        }
    }

    public bool ShowTileNumbers
    {
        get;
        set
        {
            SetField(ref field, value);
            _repaint();
        }
    }

    public bool ShowNeighbours
    {
        get;
        set
        {
            SetField(ref field, value);
            _repaint();
        }
    } = true;

    /// <summary>
    ///     How wide to draw a tile in the palette. The sheet has over four hundred tiles in it, so
    ///     being able to go from picking one out by eye to seeing most of them at once matters.
    /// </summary>
    public double PaletteTileWidth
    {
        get;
        set
        {
            SetField(ref field, Math.Clamp(value, NarrowestPaletteTile, WidestPaletteTile));
            _repaint();
        }
    } = DefaultPaletteTileWidth;

    /// <summary>
    ///     What the status line says about whatever the pointer is over. Written by the surfaces as the
    ///     pointer moves, so it does not ask for a repaint - the thing that set it has just drawn.
    /// </summary>
    public string HoverStatus
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;
}
