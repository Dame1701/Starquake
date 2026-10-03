// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using Starquake.ScreenEditor.Editing;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Content;

/// <summary>
///     Reading and writing SQScreenData.dat, the file the game loads the planet from.
/// </summary>
/// <remarks>
///     The format is two MessagePack blobs, each preceded by its length as a four byte integer: the
///     array of screens first, then the collision masks. It is not self-describing, so the editor
///     has to write it back exactly as the game's reader expects rather than in whatever shape is
///     convenient.
/// </remarks>
internal static class ScreenDataFile
{

    /// <summary>
    ///     The name the game looks for. Offered as the default when saving a map that has never been
    ///     written, so that a new map lands somewhere the game can actually load it from.
    /// </summary>
    public const string DefaultFileName = "SQScreenData.dat";

    /// <summary>
    ///     The tile numbers the three oldest markers used to be painted as, and the markers they are
    ///     now - see <see cref="MoveMarkersOutOfTheSheet" />.
    /// </summary>
    private static readonly Dictionary<int, int> RenumberedMarkers = new()
    {
        [390] = (int)MarkerTile.Spacer,
        [391] = (int)MarkerTile.ConveyorNasty,
        [392] = (int)MarkerTile.PreferredCoreLocation
    };

    public static EditorDocument Load(string path)
    {
        using FileStream stream = File.OpenRead(path);

        GameScreenData[] screens = MessagePackSerializer.Deserialize<GameScreenData[]>(ReadBlob(stream));
        int[,,] collisionMasks = MessagePackSerializer.Deserialize<int[,,]>(ReadBlob(stream));

        // Both of these count as unsaved changes, which is the point: they are only really in the
        // map once somebody saves it.
        bool migrated = MoveMarkersOutOfTheSheet(screens);
        bool seeded = SeedOriginalTeleportCodes(screens);

        return new EditorDocument(screens, collisionMasks)
        {
            FilePath = path,
            IsDirty = migrated || seeded
        };
    }

    /// <summary>
    ///     Renumbers the markers of a map saved while they were still tile numbers on the sheet.
    /// </summary>
    /// <remarks>
    ///     The three oldest markers were painted as 390, 391 and 392 - slots on the tile sheet, with
    ///     placeholder artwork that only the editor ever showed. They are <see cref="MarkerTile" />
    ///     numbers now, clear of the sheet, which frees those three slots to be drawn on and used as
    ///     ordinary scenery. A map opened from before that move still says 390, and this is what puts
    ///     it right; without it the game would read those squares as whatever gets painted there next.
    /// </remarks>
    private static bool MoveMarkersOutOfTheSheet(GameScreenData[] screens)
    {
        bool moved = false;

        foreach (GameScreenData screen in screens)
        {
            foreach (ScreenTile tile in screen.ScreenTiles)
            {
                if (RenumberedMarkers.TryGetValue(tile.TileImage, out int marker))
                {
                    tile.TileImage = marker;
                    moved = true;
                }
            }
        }

        return moved;
    }

    /// <summary>
    ///     Fills in the codes the game used to have written into it, for a map saved before they were
    ///     part of it. Only a screen that has a booth and no code of its own is touched, so this can
    ///     neither overwrite a code somebody set nor invent a destination that cannot be arrived at.
    /// </summary>
    private static bool SeedOriginalTeleportCodes(GameScreenData[] screens)
    {
        bool seeded = false;

        foreach ((int screen, string code) in TeleportCodes.Original)
        {
            if (screen >= screens.Length ||
                screens[screen].TeleportCode is not null ||
                !HasTeleportBooth(screens[screen]))
            {
                continue;
            }

            screens[screen].TeleportCode = code;
            seeded = true;
        }

        return seeded;
    }

    private static bool HasTeleportBooth(GameScreenData screen)
    {
        foreach (ScreenTile tile in screen.ScreenTiles)
        {
            if (tile.TileImage == (int)ActionTiles.TeleportBooth)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Writes the map out, keeping one generation of backup. The game reads this file on every
    ///     launch and there is only one copy of it in the repository, so a save that goes wrong
    ///     should cost a rename to put right rather than a trip to source control.
    /// </summary>
    public static void Save(EditorDocument document, string path)
    {
        byte[] screensBlob = MessagePackSerializer.Serialize(document.Screens);
        byte[] collisionBlob = MessagePackSerializer.Serialize(document.CollisionMasks);

        // Build the whole file before touching the old one, so that a failure part way through
        // serialising cannot leave a truncated map behind.
        string temporaryPath = path + ".tmp";

        using (FileStream stream = File.Create(temporaryPath))
        {
            WriteBlob(stream, screensBlob);
            WriteBlob(stream, collisionBlob);
        }

        if (File.Exists(path))
        {
            string backupPath = path + ".bak";
            File.Copy(path, backupPath, true);
        }

        File.Move(temporaryPath, path, true);

        document.FilePath = path;
        document.IsDirty = false;
    }

    private static byte[] ReadBlob(Stream stream)
    {
        byte[] lengthBuffer = new byte[sizeof(int)];
        stream.ReadExactly(lengthBuffer, 0, sizeof(int));

        byte[] blob = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
        stream.ReadExactly(blob, 0, blob.Length);

        return blob;
    }

    private static void WriteBlob(Stream stream, byte[] blob)
    {
        stream.Write(BitConverter.GetBytes(blob.Length), 0, sizeof(int));
        stream.Write(blob, 0, blob.Length);
    }
}
