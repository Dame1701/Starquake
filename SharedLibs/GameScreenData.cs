using MessagePack;

namespace SharedLibs;

[MessagePackObject]
public sealed class GameScreenData
{
    [Key(0)]
    public ScreenTile[,]? ScreenTiles { get; set; }

    [Key(1)]
    public List<CollisionTile>? CollisionMap { get; set; }

    [IgnoreMember]
    public bool KeepNastiesTheSame { get; set; }
    
    [IgnoreMember]
    public Item? PowerUp { get; set; }

    [IgnoreMember]
    public Item? CheopsPyramid { get; set; }

    [IgnoreMember] public List<Item> PickUps { get; set; } = [];

    [IgnoreMember] public string? AccessCode { get; set; }

    [IgnoreMember] public string? CheopsAccessCode { get; set; }

    [IgnoreMember] public bool ScreenVisited { get; set; }

    public GameScreenData()
    {
    }

    public GameScreenData(int tilesAcross, int tilesDown)
    {
        ScreenTiles = new ScreenTile[tilesAcross, tilesDown];
        for (var y = 0; y < tilesDown; y++)
        {
            for (var x = 0; x < tilesAcross; x++)
            {
                ScreenTiles[x, y] = new ScreenTile(-1);
            }
        }
    }
}