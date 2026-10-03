// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     A screen a teleport code reaches, named by the code so that it can be offered as a choice.
///     What a signpost is pointed at is one of these.
/// </summary>
internal readonly record struct TeleportDestination(int Screen, string Code);

/// <summary>
///     The map being edited: every screen on the planet, plus the collision mask that says which
///     parts of each tile in the sheet are solid.
/// </summary>
/// <remarks>
///     <para>
///         The two halves are stored separately in the game's data file and mean quite different
///         things, which is worth keeping in mind while editing. A screen holds tile numbers, so
///         changing one affects that screen alone. A collision mask belongs to a tile <i>image</i>,
///         so changing one affects every screen that has ever used that tile.
///     </para>
///     <para>
///         Each screen used to carry a collision map of its own, but the game threw
///         that away and rebuilds it from the masks the moment Blob walks onto the screen. The
///         editor therefore leaves whatever was in the file untouched rather than trying to keep
///         derived data in step with edits that will never be read.
///     </para>
/// </remarks>
internal sealed class EditorDocument
{

    /// <summary>
    ///     The tile number that means "nothing here". The game skips these when it draws a screen and
    ///     when it builds a collision map, so they are holes rather than black tiles.
    /// </summary>
    public const int EmptyTile = -1;

    public EditorDocument(GameScreenData[] screens, int[,,] collisionMasks)
    {
        Screens = screens;
        CollisionMasks = collisionMasks;
    }

    /// <summary>
    ///     Where this map was loaded from, and where <c>Save</c> will put it back. Null for a map that
    ///     has been created but never written.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    ///     Whether there are edits that have not been written to <see cref="FilePath" /> yet.
    /// </summary>
    public bool IsDirty { get; set; }

    public GameScreenData[] Screens { get; }

    public int[,,] CollisionMasks { get; private set; }

    /// <summary>
    ///     How many tile images the collision masks cover. A tile sheet with more tiles than this can
    ///     still be painted with; <see cref="EnsureCollisionCapacity" /> grows the masks to match.
    /// </summary>
    private int CollisionTileCount => CollisionMasks.GetLength(0);

    /// <summary>
    ///     How many screens carry a core, which should only ever be nought or one.
    /// </summary>
    public int CoreCount => CoreSite.CountOn(Screens);

    /// <summary>
    ///     Raised whenever a single tile on a screen changes, so that views holding a rendered copy of
    ///     the map can patch just the part that moved instead of redrawing everything.
    /// </summary>
    public event Action<int, int, int>? TileChanged;

    /// <summary>
    ///     Raised whenever a tile image's collision mask changes. Unlike a tile edit this can affect
    ///     any screen, so listeners have no choice but to refresh in full.
    /// </summary>
    public event Action<int>? CollisionMaskChanged;

    /// <summary>
    ///     Raised whenever the planet core moves. Nothing else on a screen is drawn from it, but it is
    ///     outlined on the canvas and described in the panel, and both have to keep up with an undo.
    /// </summary>
    public event Action? CoreChanged;

    /// <summary>
    ///     Raised whenever a screen's teleport code changes. Nothing is drawn from it, but the screen
    ///     it belongs to is described in two places that have to keep up with an undo.
    /// </summary>
    public event Action<int>? TeleportCodeChanged;

    /// <summary>
    ///     Raised whenever a screen's signpost is pointed at a different screen. The code the sign
    ///     reads is drawn over it on the canvas, so unlike a teleport code this does change the
    ///     picture.
    /// </summary>
    public event Action<int>? SignpostChanged;

    /// <summary>
    ///     An empty planet: every screen present but blank, and every tile passable.
    /// </summary>
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
    ///     Where a marker has been placed, or null if the map has none of it. Walks every screen, so
    ///     callers that show it are expected to hold on to the answer rather than ask per repaint.
    /// </summary>
    public MarkerPlacement? FindMarker(MarkerTile marker)
        => MarkerTiles.Locate(Screens, marker);

    /// <summary>
    ///     Where the planet core is, or where the game would fall back to if the map does not say.
    /// </summary>
    public CoreSite FindCore() => CoreSite.FindIn(Screens);

    /// <summary>
    ///     Puts the core on a screen at a position, taking it off whatever screen had it. There is one
    ///     core on a planet, so this moves it rather than adding another - and the moving is
    ///     <see cref="CoreSite.PlaceOn" />'s, because it is a rule about the map rather than about
    ///     editing one.
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
    ///     The destination code that reaches a screen's teleport booth, or null for a screen that has
    ///     not been given one.
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
    ///     Whether a screen has a teleport booth on it. A code is only any use to a screen that does:
    ///     it is what that booth's monitor shows, and what has to be typed into any other booth to
    ///     arrive here.
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
    ///     Which screen a code already reaches, or -1 if none does. The game takes the first screen
    ///     whose code matches, so a code used twice would leave the second screen unreachable - which
    ///     is what the editor uses this to refuse.
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
    ///     The screen whose teleport code the signposts on a screen spell out, or null for a screen
    ///     whose signs have not been pointed at one.
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
    ///     Whether a screen has a signpost on it. A signposted screen is only any use to one that
    ///     does - there would be nothing to write the code on otherwise.
    /// </summary>
    public bool HasSignpost(int screen) => Signposts.StandOn(Screens[screen]);

    /// <summary>
    ///     What the signposts on a screen read, or null if there is nothing for them to say.
    /// </summary>
    public string? GetSignpostCode(int screen) => Signposts.CodeShownOn(Screens, screen);

    /// <summary>
    ///     Every screen a sign could be pointed at: the ones that have a code for it to show, in
    ///     code order so that a list of them reads alphabetically rather than by where on the map
    ///     each one happens to sit.
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
    ///     Whether one cell of one tile image blocks movement. Collision is tested on a finer grid
    ///     than the tiles are drawn on, so a platform can be solid along its top edge alone.
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
    ///     Grows the collision masks so that every tile in a newly loaded sheet has one. A sheet with
    ///     fewer tiles than the masks cover is left alone: the spare masks cost nothing and throwing
    ///     them away would lose data if the original sheet were loaded again.
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
    ///     Every tile of one screen, in reading order. Used for copy, paste and undo, all of which
    ///     treat a screen as a single value rather than forty-eight separate ones.
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
