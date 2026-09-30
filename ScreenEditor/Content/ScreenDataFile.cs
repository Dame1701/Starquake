// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.IO;
using MessagePack;
using Starquake.ScreenEditor.Editing;
using Starquake.SharedLibs;

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
public static class ScreenDataFile
{

    /// <summary>
    ///     The name the game looks for. Offered as the default when saving a map that has never been
    ///     written, so that a new map lands somewhere the game can actually load it from.
    /// </summary>
    public const string DefaultFileName = "SQScreenData.dat";

    public static EditorDocument Load(string path)
    {
        using var stream = File.OpenRead(path);

        var screens = MessagePackSerializer.Deserialize<GameScreenData[]>(ReadBlob(stream));
        var collisionMasks = MessagePackSerializer.Deserialize<int[,,]>(ReadBlob(stream));

        return new EditorDocument(screens, collisionMasks)
        {
            FilePath = path
        };
    }

    /// <summary>
    ///     Writes the map out, keeping one generation of backup. The game reads this file on every
    ///     launch and there is only one copy of it in the repository, so a save that goes wrong
    ///     should cost a rename to put right rather than a trip to source control.
    /// </summary>
    public static void Save(EditorDocument document, string path)
    {
        var screensBlob = MessagePackSerializer.Serialize(document.Screens);
        var collisionBlob = MessagePackSerializer.Serialize(document.CollisionMasks);

        // Build the whole file before touching the old one, so that a failure part way through
        // serialising cannot leave a truncated map behind.
        var temporaryPath = path + ".tmp";

        using (var stream = File.Create(temporaryPath))
        {
            WriteBlob(stream, screensBlob);
            WriteBlob(stream, collisionBlob);
        }

        if (File.Exists(path))
        {
            var backupPath = path + ".bak";
            File.Copy(path, backupPath, true);
        }

        File.Move(temporaryPath, path, true);

        document.FilePath = path;
        document.IsDirty = false;
    }

    private static byte[] ReadBlob(Stream stream)
    {
        var lengthBuffer = new byte[sizeof(int)];
        stream.ReadExactly(lengthBuffer, 0, sizeof(int));

        var blob = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
        stream.ReadExactly(blob, 0, blob.Length);

        return blob;
    }

    private static void WriteBlob(Stream stream, byte[] blob)
    {
        stream.Write(BitConverter.GetBytes(blob.Length), 0, sizeof(int));
        stream.Write(blob, 0, blob.Length);
    }
}
