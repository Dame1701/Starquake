// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     What one step of the status panel asks of Blob. The panel holds the energy bar and the lives,
///     so it notices that the bar has run out or that a death has been paid for, but it draws a widget
///     and has no business reaching into Blob to act on either.
/// </summary>
/// <remarks>
///     Answering with a verdict the caller applies keeps the dependency one-way: the panel names no
///     type from <c>ActiveSprites</c>, and the engine holds both and applies the answer.
/// </remarks>
internal enum BlobInstruction
{
    /// <summary>Nothing to do.</summary>
    None,

    /// <summary>Blob's energy has run out, so he comes apart where he stands.</summary>
    Overload,

    /// <summary>A life has been taken and another remains, so Blob starts again.</summary>
    Revive,

    /// <summary>That was the last life.</summary>
    EndTheGame
}
