// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.Display;

public class ResetTile
{

    public ResetTile(TileDetected tileToReset, int screenNumber)
    {
        TileToReset = tileToReset;
        ScreenNumber = screenNumber;
    }

    public TileDetected TileToReset { get; set; }

    public int ScreenNumber { get; set; }
}