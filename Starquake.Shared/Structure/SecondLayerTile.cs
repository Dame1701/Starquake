// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Structure;

public class SecondLayerTile
{
    public Point TileLocation;

    public TileType TileType;
    public SecondLayerTile(TileType tileType, Point tileLocation)
    {
        TileType = tileType;
        TileLocation = tileLocation;
    }
}