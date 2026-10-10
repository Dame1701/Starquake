// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     Where Blob stands inside a teleport booth.
/// </summary>
/// <remarks>
///     Said once because it is asked twice and the two must agree. Walking into a booth is
///     recognised by Blob standing at exactly this spot, and arriving out of one puts him there.
///     Two separate copies could drift into a booth you arrive in but cannot walk out of.
/// </remarks>
internal static class BoothMetrics
{

    /// <summary>
    ///     How far into the booth's tile Blob stands, so he is in the doorway rather than on its
    ///     edge. Left of the tile's middle, because that is where the artwork puts the doorway.
    /// </summary>
    public const int BlobXOffset = 20;

    /// <summary>
    ///     Blob's feet on the floor of the booth. A booth is a whole tile tall and Blob is not, so
    ///     he stands at the bottom rather than floating in the middle.
    /// </summary>
    public const int BlobYOffset = TileMetrics.TileHeight - SpriteMetrics.SpriteHeight;
}
