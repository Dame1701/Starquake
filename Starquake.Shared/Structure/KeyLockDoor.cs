// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.SharedLibs;

namespace Starquake.Structure;

public class KeyLockDoor
{
    public readonly List<CollisionTile> CollisionTiles;
    public Point Location;

    public KeyLockDoor()
    {
        CollisionTiles = new List<CollisionTile>();
    }
}