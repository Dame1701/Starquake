// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using Starquake.SharedLibs;

namespace Starquake.Tests;

/// <summary>
///     The planet the game actually ships, for the few tests that should be measured against it rather
///     than against a world built by hand.
/// </summary>
/// <remarks>
///     Most tests are better off with a world they describe themselves - a failure you can read beats
///     one you have to go looking for. These are the exception: whether there is anywhere safe to put
///     the key is a question about this map, and a hand-built world cannot answer it. The file is
///     copied next to the test assembly by the same <c>PreserveNewest</c> rule that puts it next to the
///     game.
/// </remarks>
internal static class ShippedMap
{

    public static (GameScreenData[] Screens, int[,,] CollisionMasks) Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Data", "SQScreenData.dat");

        using FileStream stream = File.OpenRead(path);

        return (MessagePackSerializer.Deserialize<GameScreenData[]>(ReadBlob(stream)),
            MessagePackSerializer.Deserialize<int[,,]>(ReadBlob(stream)));
    }

    /// <summary>
    ///     One length-prefixed MessagePack Blob, which is the shape of the file - see
    ///     <c>ScreenDataFile</c> in the screen editor, which is the thing that writes it.
    /// </summary>
    private static byte[] ReadBlob(Stream stream)
    {
        byte[] length = new byte[sizeof(int)];
        stream.ReadExactly(length, 0, sizeof(int));

        byte[] blob = new byte[BitConverter.ToInt32(length, 0)];
        stream.ReadExactly(blob, 0, blob.Length);

        return blob;
    }
}
