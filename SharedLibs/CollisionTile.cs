using MessagePack;

namespace SharedLibs;

[MessagePackObject]
[Serializable]
public class CollisionTile
{
    private int tileImage;
        
    [Key(0)]
    public int TileImage
    {
        get { return tileImage; }
        set { tileImage = value; }
    }

    private int xPos;
        
    [Key(1)]
    public int XPos
    {
        get { return xPos; }
        set { xPos = value; }
    }

    private int yPos;
        
    [Key(2)]
    public int YPos
    {
        get { return yPos; }
        set { yPos = value; }
    }

    private CollisionTileType colType;
        
    [Key(3)]
    public CollisionTileType ColType
    {
        get { return colType; }
        set { colType = value; }
    }

    public CollisionTile(int TileImage, int XPos, int YPos, CollisionTileType CollType)
    {
        tileImage = TileImage;
        xPos = XPos;
        yPos = YPos;
        colType = CollType;
    }

    public override string ToString()
    {
        return "TileImage: " + tileImage.ToString() + ", XPos: " + xPos.ToString() + ", YPos: " + yPos.ToString() + ", ColType: " + colType.ToString();
    }
}