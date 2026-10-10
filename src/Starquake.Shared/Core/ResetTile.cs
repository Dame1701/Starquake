// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     A tile changed as Blob passed, such as a broken bridge or a taken shuttle pad, and the screen
///     it is on, so it can be put back when the game restarts.
/// </summary>
internal sealed record ResetTile(TileDetected TileToReset, int ScreenNumber);
