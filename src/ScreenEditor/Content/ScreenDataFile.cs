// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using MessagePack;
using Starquake.ScreenEditor.Editing;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Content;

/// <summary>
///     Reading and writing SQScreenData.dat, the file the game loads the planet from.
/// </summary>
/// <remarks>
///     The format is two MessagePack blobs, each preceded by its length as a four byte integer:
///     the screens first, then the collision masks. It is not self-describing, so the editor must
///     write it back in exactly the shape the game's reader expects.
/// </remarks>
internal static class ScreenDataFile
{

    /// <summary>
    ///     The name the game looks for. Offered as the default when saving a map that has never been
    ///     written, so a new map lands somewhere the game can load it from.
    /// </summary>
    public const string DefaultFileName = "SQScreenData.dat";

    /// <summary>
    ///     Old tile numbers mapped to the markers they are now. See
    ///     <see cref="MoveMarkersOutOfTheSheet" />.
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

        GameScreenData[] screens = MessagePackSerializer.Deserialize<GameScreenData[]>(ReadBlob(stream), ScreenDataSerializer.Options);
        int[,,] collisionMasks = MessagePackSerializer.Deserialize<int[,,]>(ReadBlob(stream), ScreenDataSerializer.Options);

        // Both count as unsaved changes: they only reach the map once somebody saves it.
        bool migrated = MoveMarkersOutOfTheSheet(screens);
        bool seeded = SeedOriginalTeleportCodes(screens);

        return new EditorDocument(screens, collisionMasks)
        {
            FilePath = path,
            IsDirty = migrated || seeded
        };
    }

    /// <summary>
    ///     Renumbers markers stored as tile sheet slots 390, 391 and 392 to their
    ///     <see cref="MarkerTile" /> numbers.
    /// </summary>
    /// <remarks>
    ///     Those three slots are ordinary scenery now. Without this, an old map's markers would be
    ///     read as whatever artwork gets painted into the slots next.
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
    ///     Fills in <see cref="TeleportCodes.Original" /> for a map with no codes stored. Only
    ///     touches screens that have a booth and no code, so it cannot overwrite an authored code or
    ///     invent an unreachable destination.
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
    ///     Writes the map out, keeping one generation of backup. There is only one copy of this file
    ///     in the repository, so a bad save should cost a rename to undo rather than a trip to source
    ///     control.
    /// </summary>
    public static void Save(EditorDocument document, string path)
    {
        byte[] screensBlob = MessagePackSerializer.Serialize(document.Screens, ScreenDataSerializer.Options);
        byte[] collisionBlob = MessagePackSerializer.Serialize(document.CollisionMasks, ScreenDataSerializer.Options);

        // Build the whole file before touching the old one, so a failure part way through cannot
        // leave a truncated map behind.
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
