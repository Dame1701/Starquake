// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.Structure;

public class ResetTile
{

    public ResetTile(TileDetected tileToReset, int screenNumber)
    {
        TileToReset = tileToReset;
        ScreenNumber = screenNumber;
    }

    public TileDetected TileToReset { get; }

    public int ScreenNumber { get; }
}