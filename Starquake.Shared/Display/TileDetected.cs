// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.Display;

public class TileDetected
{

    public TileDetected(int tileNumber, int xPosition, int yPosition, int mapXPosition, int mapYPosition)
    {
        TileNumber = tileNumber;
        XPosition = xPosition;
        YPosition = yPosition;
        MapXPosition = mapXPosition;
        MapYPosition = mapYPosition;
    }

    public int TileNumber { get; set; }

    public int XPosition { get; set; }

    public int YPosition { get; set; }

    public int MapXPosition { get; set; }

    public int MapYPosition { get; set; }

    public override string ToString()
        => "TileNo: " + TileNumber + " " + "XPos: " + XPosition + " " + "YPos " +
           YPosition;
}