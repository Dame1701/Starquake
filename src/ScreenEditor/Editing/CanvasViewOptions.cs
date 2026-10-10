// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ScreenEditor.ViewModels;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     What the editor shows over and around the screen being worked on, and how large the palette
///     draws its tiles. Preferences about viewing, with nothing in them about the map.
/// </summary>
/// <remarks>
///     Each of these is read by something that draws and written by a toolbar toggle. Nothing that
///     edits the map touches them.
///     <para>
///         Changing one needs a repaint, and the canvas already listens to the session's
///         <c>Invalidated</c> event, so this is handed the session's own way of asking for one rather
///         than raising a second event.
///     </para>
/// </remarks>
internal sealed class CanvasViewOptions : ViewModelBase
{

    /// <summary>
    ///     The narrowest and widest a palette tile may be drawn, and the width a session opens at.
    /// </summary>
    private const double NarrowestPaletteTile = 32;

    /// <inheritdoc cref="NarrowestPaletteTile" />
    private const double WidestPaletteTile = 192;

    /// <inheritdoc cref="NarrowestPaletteTile" />
    private const double DefaultPaletteTileWidth = 64;
    private readonly Action _repaint;

    public CanvasViewOptions(Action repaint)
    {
        _repaint = repaint;
    }

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
    ///     How wide to draw a tile in the palette. The sheet holds over 400 tiles, so the range has
    ///     to cover both picking one out by eye and seeing most of them at once.
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
    ///     What the status line says about whatever the pointer is over. Setting it does not ask for
    ///     a repaint, because whatever set it has just drawn.
    /// </summary>
    public string HoverStatus
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;
}
