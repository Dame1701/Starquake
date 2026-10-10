// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Core;

/// <summary>
///     A tile on a screen's upper layer and where it sits, which is how the electricity nodes and
///     gates are found to draw their arcs over.
/// </summary>
internal sealed record SecondLayerTile(TileType TileType, Point TileLocation);
