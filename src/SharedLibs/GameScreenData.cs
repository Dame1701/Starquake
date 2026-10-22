// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

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
    // Not nullable: every consumer indexes straight into this without checking. An unpopulated
    // screen is an empty one, not a null one.
    [Key(0)]
    public ScreenTile[,] ScreenTiles { get; set; } = new ScreenTile[0, 0];

    // Key 1 is deliberately unused. Reusing it would shift the keys below and stop the shipped
    // data file reading.

    /// <summary>
    ///     The code that reaches this screen's teleport booth, or null if it has no booth. Authored
    ///     in the screen editor, so it is saved with the map rather than generated each game. See
    ///     <see cref="Starquake.Structure.TeleportCodes" />.
    /// </summary>
    [Key(2)]
    public string? TeleportCode { get; set; }

    /// <summary>
    ///     Where the planet core sits on this screen, or null for the 511 screens without it.
    ///     A pixel position rather than a marker tile, because the core does not fit the tile grid.
    ///     See <see cref="Starquake.Structure.CoreSite" />.
    /// </summary>
    [Key(3)]
    public CorePlacement? Core { get; set; }

    /// <summary>
    ///     The screen whose teleport code this screen's signpost spells out, or null if it has no
    ///     signpost. A screen number rather than the code itself, so changing a code updates every
    ///     sign pointing at it. See <see cref="Starquake.Structure.Signposts" />.
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
