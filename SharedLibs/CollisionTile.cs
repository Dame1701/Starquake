using MessagePack;

namespace SharedLibs;

[MessagePackObject]
public sealed record CollisionTile(int TileImage, int XPos, int YPos, CollisionTileType CollisionTileType)
{
    [Key(0)] public int TileImage { get; } = TileImage;

    [Key(1)] public int XPos { get; } = XPos;

    [Key(2)] public int YPos { get; } = YPos;

    [Key(3)]
    public CollisionTileType CollisionTileType
    {
        get;
        set;
    } = CollisionTileType;

    public override string ToString()
    {
        return "TileImage: " + TileImage + ", XPos: " + XPos + ", YPos: " + YPos + ", ColType: " + CollisionTileType;
    }
}