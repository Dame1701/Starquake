// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     What a click on the screen being edited does.
/// </summary>
/// <remarks>
///     The right button always erases and Alt always picks, whichever tool is selected. Reaching for
///     the toolbar to rub out one tile and again to resume painting makes an editor tiring to use.
/// </remarks>
internal enum EditorTool
{

    /// <summary>Lays the selected tile down, and keeps doing so while the pointer is dragged.</summary>
    Paint,

    /// <summary>Clears tiles back to empty.</summary>
    Erase,

    /// <summary>Floods the run of matching tiles under the pointer with the selected tile.</summary>
    Fill,

    /// <summary>Selects the tile under the pointer, so it can be painted elsewhere.</summary>
    Pick,

    /// <summary>Puts the planet core where the pointer is, and drags it about.</summary>
    /// <remarks>
    ///     The only tool that does not work on the tile grid. The core is one and a half tiles across
    ///     and not tile-aligned, so it is placed at
    ///     <see cref="Starquake.Structure.CoreSite.PositionStep" /> resolution. A planet has one core,
    ///     so placing it anywhere moves it from wherever it was.
    /// </remarks>
    Core
}
