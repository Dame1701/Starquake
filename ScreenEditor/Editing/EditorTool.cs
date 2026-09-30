// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     What a click on the screen being edited does.
/// </summary>
/// <remarks>
///     The pointer's other buttons and the modifier keys reach the same tools without leaving the
///     one that is selected - the right button always erases and Alt always picks - because reaching
///     for the toolbar to rub out one tile and again to carry on painting is the sort of thing that
///     makes an editor tiring to use.
/// </remarks>
public enum EditorTool
{

    /// <summary>
    ///     Lays the selected tile down, and keeps laying it down while the pointer is dragged.
    /// </summary>
    Paint,

    /// <summary>
    ///     Clears tiles back to empty.
    /// </summary>
    Erase,

    /// <summary>
    ///     Floods the run of matching tiles the pointer is over with the selected tile.
    /// </summary>
    Fill,

    /// <summary>
    ///     Selects whatever tile the pointer is over, so it can be painted elsewhere.
    /// </summary>
    Pick
}
