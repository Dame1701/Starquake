// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Structure;

/// <summary>
///     A tile on the upper layer of a screen and where it sits, which is how the electricity nodes
///     and gates are found to draw their arcs over.
/// </summary>
public sealed record SecondLayerTile(TileType TileType, Point TileLocation);
