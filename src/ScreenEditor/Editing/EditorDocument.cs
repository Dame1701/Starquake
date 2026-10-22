// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     A screen a teleport code reaches, named by its code so it can be offered as a choice. This is
///     what a signpost is pointed at.
/// </summary>
internal readonly record struct TeleportDestination(int Screen, string Code);

/// <summary>
///     The map being edited: every screen on the planet, plus the collision mask that says which
///     parts of each tile in the sheet are solid.
/// </summary>
/// <remarks>
///     The two halves mean different things. A screen holds tile numbers, so changing one affects
///     that screen alone. A collision mask belongs to a tile <i>image</i>, so changing one affects
///     every screen using that tile.
///     <para>
///         The file also holds a per-screen collision map, which the game ignores and rebuilds from
///         the masks as Blob walks onto a screen. The editor passes it through untouched rather than
///         maintaining derived data nothing reads.
///     </para>
/// </remarks>
internal sealed class EditorDocument
{

    /// <summary>
    ///     The tile number meaning "nothing here". The game skips these when drawing a screen and
    ///     when building a collision map, so they are holes rather than black tiles.
    /// </summary>
    public const int EmptyTile = -1;

    public EditorDocument(GameScreenData[] screens, int[,,] collisionMasks)
    {
        Screens = screens;
        CollisionMasks = collisionMasks;
    }

    /// <summary>
    ///     Where this map was loaded from, and where <c>Save</c> writes it back. Null for a map
    ///     created but never written.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>Whether there are edits not yet written to <see cref="FilePath" />.</summary>
    public bool IsDirty { get; set; }

    public GameScreenData[] Screens { get; }

    public int[,,] CollisionMasks { get; private set; }

    /// <summary>
    ///     How many tile images the collision masks cover. A sheet with more tiles than this can
    ///     still be painted with, because <see cref="EnsureCollisionCapacity" /> grows the masks.
    /// </summary>
    private int CollisionTileCount => CollisionMasks.GetLength(0);

    /// <summary>How many screens carry a core, which should only ever be none or one.</summary>
    public int CoreCount => CoreSite.CountOn(Screens);

    /// <summary>
    ///     Raised when a single tile on a screen changes, so views holding a rendered copy of the map
    ///     can patch just that part instead of redrawing everything.
    /// </summary>
    public event Action<int, int, int>? TileChanged;

    /// <summary>
    ///     Raised when a tile image's collision mask changes. This can affect any screen, so
    ///     listeners must refresh in full.
    /// </summary>
    public event Action<int>? CollisionMaskChanged;

    /// <summary>
    ///     Raised when the planet core moves. The canvas outlines it and the panel describes it, and
    ///     both have to keep up with an undo.
    /// </summary>
    public event Action? CoreChanged;

    /// <summary>
    ///     Raised when a screen's teleport code changes. Nothing is drawn from it, but two places
    ///     describe the screen and have to keep up with an undo.
    /// </summary>
    public event Action<int>? TeleportCodeChanged;

    /// <summary>
    ///     Raised when a screen's signpost is pointed at a different screen. The canvas draws the
    ///     code over the sign, so this does change the picture.
    /// </summary>
    public event Action<int>? SignpostChanged;

    /// <summary>An empty planet: every screen present but blank, and every tile passable.</summary>
    public static EditorDocument CreateEmpty()
    {
        var screens = new GameScreenData[WorldMap.ScreenCount];
        for (int screen = 0; screen < screens.Length; screen++)
        {
            screens[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);
        }

        int[,,] masks = new int[TunnelTiles.LastMouth + 1,
            TileMetrics.CollisionCellsAcrossTile,
            TileMetrics.CollisionCellsDownTile];

        return new EditorDocument(screens, masks);
    }

    public int GetTile(int screen, int x, int y)
    {
        ScreenTile[,]? tiles = Screens[screen].ScreenTiles;
        return tiles is null ? EmptyTile : tiles[x, y].TileImage;
    }

    public void SetTile(int screen, int x, int y, int tileImage)
    {
        ScreenTile[,]? tiles = Screens[screen].ScreenTiles;
        if (tiles is null || tiles[x, y].TileImage == tileImage)
        {
            return;
        }

        tiles[x, y].TileImage = tileImage;
        IsDirty = true;
        TileChanged?.Invoke(screen, x, y);
    }

    /// <summary>
    ///     Where a marker has been placed, or null if the map has none. Walks every screen, so
    ///     callers should keep the answer rather than ask again on each repaint.
    /// </summary>
    public MarkerPlacement? FindMarker(MarkerTile marker)
        => MarkerTiles.Locate(Screens, marker);

    /// <summary>
    ///     Where the planet core is, or the game's fallback position if the map does not place one.
    /// </summary>
    public CoreSite FindCore() => CoreSite.FindIn(Screens);

    /// <summary>
    ///     Moves the core to a position on a screen, clearing it from whatever screen had it. A
    ///     planet has one core, so <see cref="CoreSite.PlaceOn" /> enforces that as a rule about the
    ///     map rather than about editing it.
    /// </summary>
    public void SetCore(int screen, CorePlacement? placement)
    {
        if (!CoreSite.PlaceOn(Screens, screen, placement))
        {
            return;
        }

        IsDirty = true;
        CoreChanged?.Invoke();
    }

    /// <summary>
    ///     The code that reaches a screen's teleport booth, or null if it has not been given one.
    /// </summary>
    public string? GetTeleportCode(int screen)
        => Screens[screen].TeleportCode;

    public void SetTeleportCode(int screen, string? code)
    {
        if (Screens[screen].TeleportCode == code)
        {
            return;
        }

        Screens[screen].TeleportCode = code;
        IsDirty = true;
        TeleportCodeChanged?.Invoke(screen);
    }

    /// <summary>
    ///     Whether a screen has a teleport booth on it. A code is only useful to a screen that does,
    ///     since the booth's monitor is what shows it.
    /// </summary>
    public bool HasTeleportBooth(int screen)
    {
        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (GetTile(screen, x, y) == (int)ActionTiles.TeleportBooth)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Which screen a code already reaches, or -1 if none does. The game takes the first match,
    ///     so a code used twice leaves the second screen unreachable. The editor refuses that.
    /// </summary>
    public int FindScreenByTeleportCode(string code)
    {
        for (int screen = 0; screen < Screens.Length; screen++)
        {
            if (string.Equals(Screens[screen].TeleportCode, code, StringComparison.OrdinalIgnoreCase))
            {
                return screen;
            }
        }

        return -1;
    }

    /// <summary>
    ///     The screen whose teleport code a screen's signposts spell out, or null if its signs have
    ///     not been pointed anywhere.
    /// </summary>
    public int? GetSignpostedScreen(int screen)
        => Screens[screen].SignpostedScreen;

    public void SetSignpostedScreen(int screen, int? signposted)
    {
        if (Screens[screen].SignpostedScreen == signposted)
        {
            return;
        }

        Screens[screen].SignpostedScreen = signposted;
        IsDirty = true;
        SignpostChanged?.Invoke(screen);
    }

    /// <summary>
    ///     Whether a screen has a signpost on it. Without one there is nothing to write a code on.
    /// </summary>
    public bool HasSignpost(int screen) => Signposts.StandOn(Screens[screen]);

    /// <summary>What a screen's signposts read, or null if they have nothing to say.</summary>
    public string? GetSignpostCode(int screen) => Signposts.CodeShownOn(Screens, screen);

    /// <summary>
    ///     Every screen a sign could point at, meaning every screen with a code to show. Ordered by
    ///     code, so a list of them reads alphabetically rather than by position on the map.
    /// </summary>
    public IReadOnlyList<TeleportDestination> TeleportDestinations()
    {
        var destinations = new List<TeleportDestination>();

        for (int screen = 0; screen < Screens.Length; screen++)
        {
            if (Screens[screen].TeleportCode is { } code)
            {
                destinations.Add(new TeleportDestination(screen, code));
            }
        }

        destinations.Sort((first, second) => string.CompareOrdinal(first.Code, second.Code));

        return destinations;
    }

    /// <summary>
    ///     Whether one cell of one tile image blocks movement. The collision grid is finer than the
    ///     tile grid, so a platform can be solid along its top edge alone.
    /// </summary>
    public bool IsSolid(int tileImage, int cellX, int cellY)
        => tileImage >= 0 &&
           tileImage < CollisionTileCount &&
           CollisionMasks[tileImage, cellX, cellY] == 1;

    public void SetSolid(int tileImage, int cellX, int cellY, bool solid)
    {
        if (tileImage < 0 || tileImage >= CollisionTileCount)
        {
            return;
        }

        int value = solid ? 1 : 0;
        if (CollisionMasks[tileImage, cellX, cellY] == value)
        {
            return;
        }

        CollisionMasks[tileImage, cellX, cellY] = value;
        IsDirty = true;
        CollisionMaskChanged?.Invoke(tileImage);
    }

    /// <summary>
    ///     Grows the collision masks so every tile in a newly loaded sheet has one. A smaller sheet
    ///     leaves them alone: the spare masks cost nothing, and discarding them would lose data if
    ///     the larger sheet were loaded again.
    /// </summary>
    public void EnsureCollisionCapacity(int tileCount)
    {
        if (tileCount <= CollisionTileCount)
        {
            return;
        }

        int[,,] grown = new int[tileCount,
            CollisionMasks.GetLength(1),
            CollisionMasks.GetLength(2)];

        for (int tile = 0; tile < CollisionTileCount; tile++)
        {
            for (int x = 0; x < CollisionMasks.GetLength(1); x++)
            {
                for (int y = 0; y < CollisionMasks.GetLength(2); y++)
                {
                    grown[tile, x, y] = CollisionMasks[tile, x, y];
                }
            }
        }

        CollisionMasks = grown;
        IsDirty = true;
    }

    /// <summary>
    ///     Every tile of one screen, in reading order. Copy, paste and undo all treat a screen as one
    ///     value rather than 48 separate ones.
    /// </summary>
    public int[] CaptureScreen(int screen)
    {
        int[] captured = new int[TileMetrics.TilesAcross * TileMetrics.TilesDown];
        int index = 0;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                captured[index++] = GetTile(screen, x, y);
            }
        }

        return captured;
    }

    public void RestoreScreen(int screen, int[] tiles)
    {
        int index = 0;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                SetTile(screen, x, y, tiles[index++]);
            }
        }
    }
}
