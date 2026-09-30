// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;

namespace Starquake.SharedLibs;

[MessagePackObject]
public sealed class GameScreenData
{

    public GameScreenData()
    {
    }

    public GameScreenData(int tilesAcross, int tilesDown)
    {
        ScreenTiles = new ScreenTile[tilesAcross, tilesDown];
        for (int y = 0; y < tilesDown; y++)
        {
            for (int x = 0; x < tilesAcross; x++)
            {
                ScreenTiles[x, y] = new ScreenTile(-1);
            }
        }
    }
    // Not nullable: every consumer indexes these straight off, and none of them checks.
    // An unpopulated screen is an empty one, not a null one. PickUps and PowerUp below stay
    // nullable, because there null genuinely means the screen has none.
    [Key(0)]
    public ScreenTile[,] ScreenTiles { get; set; } = new ScreenTile[0, 0];

    [Key(1)]
    public List<CollisionTile> CollisionMap { get; set; } = [];

    [IgnoreMember]
    public bool KeepNastiesTheSame { get; set; }

    [IgnoreMember]
    public InteractiveItem? PowerUp { get; set; }

    [IgnoreMember]
    public InteractiveItem? CheopsPyramid { get; set; }

    [IgnoreMember] public List<InteractiveItem> PickUps { get; set; } = [];

    [IgnoreMember] public string? AccessCode { get; set; }

    [IgnoreMember] public string? CheopsAccessCode { get; set; }

    [IgnoreMember] public bool ScreenVisited { get; set; }
}
