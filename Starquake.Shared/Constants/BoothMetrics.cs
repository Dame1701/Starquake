// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     Where the blob stands inside a teleport booth.
/// </summary>
/// <remarks>
///     Said once because it is asked twice and the two have to agree: the blob walking into a booth
///     is recognised by it standing at exactly this spot, and the blob arriving out of one is put
///     there. Written as two numbers in two files, a change to either would have made the booth you
///     arrive in a place you could not then walk out of.
/// </remarks>
internal static class BoothMetrics
{

    /// <summary>
    ///     How far into the booth's tile the blob stands, so it is in the doorway rather than on the
    ///     edge of it. Left of the tile's middle, because that is where the artwork puts the doorway.
    /// </summary>
    public const int BlobXOffset = 20;

    /// <summary>
    ///     The blob's feet on the floor of the booth. A booth is a whole tile tall and the blob is not,
    ///     so it stands on the bottom of one rather than floating in the middle.
    /// </summary>
    public const int BlobYOffset = TileMetrics.TileHeight - SpriteMetrics.SpriteHeight;
}
