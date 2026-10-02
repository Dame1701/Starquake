// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Starquake.ScreenEditor.Editing;

namespace Starquake.ScreenEditor.Controls;

/// <summary>
///     What the pointer is over, and what would be laid down there. The ghost the painter draws needs
///     all of it: which tile for an ordinary tool, and the position within the screen for the core,
///     which is placed between tiles rather than on one.
/// </summary>
/// <remarks>
///     This is the whole of what the painter needs from the canvas that the session does not already
///     carry. It is a value rather than six fields reached into, which is what lets the painter be a
///     static class with no view of the control at all - and the tool is passed in because a stroke in
///     progress keeps the tool it started with, which only the canvas knows.
/// </remarks>
/// <param name="TileX">The tile under the pointer, or -1 when the pointer is off the screen.</param>
/// <param name="TileY">
///     <inheritdoc cref="TileX" path="/summary" />
/// </param>
/// <param name="Tool">The tool whose ghost should be drawn - the stroke's, if one is in progress.</param>
/// <param name="PointerOnScreen">
///     Where the pointer is in the screen's own pixels, or null when it is off the screen.
/// </param>
internal readonly record struct Hover(int TileX, int TileY, EditorTool Tool, Point? PointerOnScreen);
