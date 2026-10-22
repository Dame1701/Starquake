// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     A nasty's frame of mind, as one value.
/// </summary>
/// <remarks>
///     Twice the number of nasties needed are kept, and a screen swap copies the live set's state
///     onto the spare set. A record struct copies whole, so what survives a screen change is decided
///     by what is declared here.
///     <para>
///         State a nasty should start fresh with belongs on <see cref="NastyBrain" /> instead, where
///         it is reset deliberately rather than carried by omission.
///     </para>
/// </remarks>
internal record struct NastyMovementState
{
    /// <summary>Which of the six ways of moving this nasty is currently using.</summary>
    public MoveType MovementType { get; set; }

    /// <summary>Whether it is going after Blob or wandering about.</summary>
    public bool AttractedToBlob { get; set; }

    /// <summary>Which way it is facing, and going.</summary>
    public Direction Direction { get; set; }

    /// <summary>How far through its current run it is, and how long that run is.</summary>
    public int StepsMovedCounter { get; set; }

    /// <inheritdoc cref="StepsMovedCounter" />
    public int StepsToMove { get; set; }

    /// <summary>
    ///     Whether it moves every frame regardless of the run counter, as the bouncing types do.
    /// </summary>
    public bool ConstantMovement { get; set; }

    /// <summary>How many pixels a frame it travels.</summary>
    public int MoveSpeed { get; set; }

    /// <summary>
    ///     What it walked into last step, so the movement methods know where to send it next. A
    ///     diagonal stopped on one axis reports only that axis.
    /// </summary>
    public Direction LastCollisionDirection { get; set; }

    /// <summary>
    ///     How far through the current frame of mind it is, and how long that frame of mind lasts.
    /// </summary>
    public int MovementChangeCounter { get; set; }

    /// <inheritdoc cref="MovementChangeCounter" />
    public int MovementChangeCount { get; set; }
}
