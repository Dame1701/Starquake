// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;

namespace Starquake.SharedLibs;

[MessagePackObject]
public class ScreenTile
{
    public ScreenTile()
    {
    }

    public ScreenTile(int tileImage)
    {
        TileImage = tileImage;
    }
    
    [Key(0)]
    public int TileImage { get; set; }
}