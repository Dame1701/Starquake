// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     What a frame standing on a shuttle pad came to. Only the two middle cases change anything, but
///     <see cref="Standing" /> still rebuilds the collision map under Blob, which is why it is not
///     folded in with <see cref="NotOnThePad" />.
/// </summary>
internal enum ShuttleBoarding
{
    /// <summary>Blob is not on a pad, or there is no tile underfoot to be on.</summary>
    NotOnThePad,

    /// <summary>Blob is on a pad and did nothing about it this frame.</summary>
    Standing,

    /// <summary>Up was pressed on an intact pad, so the shuttle is now Blob's.</summary>
    Boarded,

    /// <summary>Blob walked off the shuttle, so the pad it came from goes back.</summary>
    SteppedOff
}
