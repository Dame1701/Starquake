// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Starquake.ScreenEditor.Editing;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     What the pointer is over, and what would be laid down there. The painter needs the tile for an
///     ordinary tool and the pixel position for the core, which is placed between tiles.
/// </summary>
/// <remarks>
///     Everything the painter needs from the canvas that the session does not already carry. Passing
///     it as one value is what lets the painter be a static class with no view of the control.
/// </remarks>
/// <param name="TileX">The tile under the pointer, or -1 when the pointer is off the screen.</param>
/// <param name="TileY">
///     <inheritdoc cref="TileX" path="/summary" />
/// </param>
/// <param name="Tool">
///     The tool whose ghost should be drawn. A stroke in progress keeps the tool it started with, and
///     only the canvas knows that.
/// </param>
/// <param name="PointerOnScreen">
///     Where the pointer is in the screen's own pixels, or null when it is off the screen.
/// </param>
internal readonly record struct Hover(int TileX, int TileY, EditorTool Tool, Point? PointerOnScreen);
