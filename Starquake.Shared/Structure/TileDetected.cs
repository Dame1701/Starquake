// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

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

    public int TileNumber { get; }

    public int XPosition { get; }

    public int YPosition { get; }

    public int MapXPosition { get; }

    public int MapYPosition { get; }

    public override string ToString()
        => "TileNo: " + TileNumber + " " + "XPos: " + XPosition + " " + "YPos " +
           YPosition;
}
