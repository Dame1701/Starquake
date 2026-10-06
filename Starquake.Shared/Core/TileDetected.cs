// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     A tile something has been found standing on or about to walk into, and where it met it.
///     <see cref="XPosition" /> and <see cref="YPosition" /> are where inside the tile that was;
///     <see cref="MapXPosition" /> and <see cref="MapYPosition" /> are which tile of the screen's
///     grid it is.
/// </summary>
internal sealed record TileDetected(
    int TileNumber,
    int XPosition,
    int YPosition,
    int MapXPosition,
    int MapYPosition);
