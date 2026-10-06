// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     What one step of the status panel asks of Blob. The panel holds the energy bar and the lives,
///     so it is what notices that the bar has run out or that a death has been paid for - but it
///     draws a widget and has no business reaching into Blob to act on either.
/// </summary>
/// <remarks>
///     It did reach in: it called <c>Kill</c> and assigned <c>State</c> directly. Taking
///     Blob per call rather than holding him made that pair safe to construct in either order, which
///     was the question being asked at the time, and left the panel naming a type from
///     <c>ActiveSprites</c> - so <c>Display</c> and <c>ActiveSprites</c> each needed the other.
///     Answering with a verdict the caller applies is what makes it one-way, and the engine is
///     holding both by then anyway.
/// </remarks>
internal enum BlobInstruction
{
    /// <summary>Nothing to do.</summary>
    None,

    /// <summary>His energy has run out, so he comes apart where he stands.</summary>
    Overload,

    /// <summary>A life has been taken and he has another, so he starts again.</summary>
    Revive,

    /// <summary>That was the last life.</summary>
    EndTheGame
}
