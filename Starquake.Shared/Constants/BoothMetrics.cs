// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     Where Blob stands inside a teleport booth.
/// </summary>
/// <remarks>
///     Said once because it is asked twice and the two have to agree: Blob walking into a booth
///     is recognised by him standing at exactly this spot, and Blob arriving out of one is put
///     there. Written as two numbers in two files, a change to either would have made the booth you
///     arrive in a place you could not then walk out of.
/// </remarks>
internal static class BoothMetrics
{

    /// <summary>
    ///     How far into the booth's tile Blob stands, so he is in the doorway rather than on the
    ///     edge of it. Left of the tile's middle, because that is where the artwork puts the doorway.
    /// </summary>
    public const int BlobXOffset = 20;

    /// <summary>
    ///     Blob's feet on the floor of the booth. A booth is a whole tile tall and Blob is not,
    ///     so it stands on the bottom of one rather than floating in the middle.
    /// </summary>
    public const int BlobYOffset = TileMetrics.TileHeight - SpriteMetrics.SpriteHeight;
}
