// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     What a frame standing on a shuttle pad came to. Only the two middle cases change anything, but
///     Blob merely <see cref="Standing" /> on a pad still has the collision map rebuilt under him,
///     which is why that is not folded in with <see cref="NotOnThePad" />.
/// </summary>
internal enum ShuttleBoarding
{
    /// <summary>He is not on a pad, or there is no tile under him to be on.</summary>
    NotOnThePad,

    /// <summary>He is on a pad and did nothing about it this frame.</summary>
    Standing,

    /// <summary>He pressed up on an intact pad and the shuttle is now his.</summary>
    Boarded,

    /// <summary>He walked off the shuttle, and the pad it came from goes back.</summary>
    SteppedOff
}
