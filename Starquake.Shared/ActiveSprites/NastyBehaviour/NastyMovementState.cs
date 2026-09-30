// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.ActiveSprites.NastyBehaviour;

/// <summary>
///     A nasty's frame of mind, as one value.
/// </summary>
/// <remarks>
///     This exists because of what it replaced. Twice the number of nasties needed are kept, and on
///     a screen swap the spare set is given the live set's state by <c>Nasty.CopyFromSource</c> - a
///     method that assigned seventeen fields by hand, two of them twice, out of a class that had
///     about thirty-five. Anything added to the class afterwards was silently dropped at every
///     screen change, and nothing said so.
///     <para>
///         A record struct copies whole, so the question of which fields are carried is answered by
///         what is declared here rather than by what someone remembered to write down. The state a
///         nasty deliberately starts fresh with stays outside it, on <see cref="NastyBrain" />,
///         where that decision is written down instead of being an omission.
///     </para>
/// </remarks>
public record struct NastyMovementState
{
    /// <summary>Which of the six ways of moving this nasty is currently using.</summary>
    public MoveType MovementType { get; set; }

    /// <summary>Whether it is going after the blob or wandering about.</summary>
    public bool AttractedToBlob { get; set; }

    /// <summary>Which way it is facing, and going.</summary>
    public Direction Direction { get; set; }

    /// <summary>How far through its current run it is, and how long that run is.</summary>
    public int StepsMovedCounter { get; set; }

    /// <inheritdoc cref="StepsMovedCounter" />
    public int StepsToMove { get; set; }

    /// <summary>
    ///     Whether it moves every frame regardless of the run counter, which is what the bouncing
    ///     types do.
    /// </summary>
    public bool ConstantMovement { get; set; }

    /// <summary>How many pixels a frame it travels.</summary>
    public int MoveSpeed { get; set; }

    /// <summary>
    ///     What it walked into last step, so the movement methods know where to send it next. A
    ///     diagonal stopped on one axis only reports that axis.
    /// </summary>
    public Direction LastCollisionDirection { get; set; }

    /// <summary>
    ///     How far through the current frame of mind it is, and how long that frame of mind lasts.
    /// </summary>
    public int MovementChangeCounter { get; set; }

    /// <inheritdoc cref="MovementChangeCounter" />
    public int MovementChangeCount { get; set; }
}
