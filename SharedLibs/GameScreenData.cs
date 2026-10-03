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

    // Key 1 was the collision map. The screen editor only ever wrote an empty list and the game
    // threw whatever was there away and built its own, so it came off the record rather than
    // staying as a field nothing filled in. The number is left unused so the keys below keep theirs
    // and the shipped file still reads.

    /// <summary>
    ///     The destination code that reaches the teleport booth on this screen, or null for a screen
    ///     with no booth on it. Unlike the access codes below this is part of the map rather than
    ///     drawn afresh each game, because it is something the screen editor authors - see
    ///     <see cref="Starquake.Structure.TeleportCodes" />.
    /// </summary>
    [Key(2)]
    public string? TeleportCode { get; set; }

    /// <summary>
    ///     Where the planet core sits on this screen, or null for the 511 screens that do not carry it.
    ///     Part of the map for the same reason the teleport code is - the screen editor authors it - and
    ///     a position rather than a marker tile because the core does not fit the tile grid. See
    ///     <see cref="Starquake.Structure.CoreSite" />.
    /// </summary>
    [Key(3)]
    public CorePlacement? Core { get; set; }

    /// <summary>
    ///     The screen whose teleport code the signpost on this screen spells out, or null for a
    ///     screen with no signpost on it. Authored in the screen editor like the two above, and a
    ///     screen number rather than the code itself so that renaming a code moves every sign that
    ///     points at it - see <see cref="Starquake.Structure.Signposts" />.
    /// </summary>
    [Key(4)]
    public int? SignpostedScreen { get; set; }

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
