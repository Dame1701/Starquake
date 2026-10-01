// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     A tile that was changed as the blob passed - a bridge broken, a shuttle pad taken - and the
///     screen it is on, so it can be put back when the game restarts.
/// </summary>
internal sealed record ResetTile(TileDetected TileToReset, int ScreenNumber);
