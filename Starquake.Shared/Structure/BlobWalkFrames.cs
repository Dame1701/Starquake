// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     How Blob's sprite sheet lays out his walk, and which frame a cycle is picked up at when he
///     turns round mid-stride.
/// </summary>
/// <remarks>
///     Left and right are separate runs of frames in the one sheet rather than one run flipped, so
///     turning round means jumping between them - and the two transition frames sit between the
///     runs, next to <see cref="Resting" />, for exactly that jump. Written out as eleven frames:
///     <para>
///         <c>0 1 2 3</c> walking left, <c>4</c> turning to the right, <c>5</c> standing still,
///         <c>6</c> turning to the left, <c>7 8 9 10</c> walking right.
///     </para>
///     <para>
///         That layout is load-bearing and nothing checked it. The turn rule below is two
///         comparisons against the two transition frames, and it only produces a matching pose
///         because each transition frame sits immediately beside the run it is turning away from.
///         Renumbering the sheet without renumbering these would not fail to compile and would not
///         look wrong in a diff; it would make Blob snap to the wrong pose on every turn.
///     </para>
/// </remarks>
internal static class BlobWalkFrames
{
    /// <summary>
    ///     The frame Blob stands still on - the one he is drawn in on a fresh screen and after a
    ///     death.
    /// </summary>
    public const int Resting = 5;

    /// <summary>The run of frames the left walk cycles between.</summary>
    public const int WalkLeftFirst = 0;

    /// <inheritdoc cref="WalkLeftFirst" />
    public const int WalkLeftLast = 3;

    /// <summary>The run the right walk cycles between.</summary>
    public const int WalkRightFirst = 7;

    /// <inheritdoc cref="WalkRightFirst" />
    public const int WalkRightLast = 10;

    /// <summary>
    ///     Where a walk cycle is picked up when Blob turns round mid-stride, so the new direction
    ///     starts from a matching pose instead of snapping back to the first frame.
    /// </summary>
    public const int TurnToLeft = 6;

    /// <inheritdoc cref="TurnToLeft" />
    public const int TurnToRight = 4;

    /// <summary>
    ///     How many logic frames each frame of a walk cycle is held for.
    /// </summary>
    public const int Delay = 2;

    /// <summary>
    ///     The frame the left walk starts from, given the pose Blob is in. Coming out of the right
    ///     run he drops back to <see cref="TurnToLeft" />, which is the pose next to it; already
    ///     facing left or standing still, he carries on from where he is rather than snapping.
    /// </summary>
    public static int TurningLeftFrom(int currentFrame)
        => currentFrame > TurnToLeft ? TurnToLeft : currentFrame;

    /// <summary>
    ///     And the other way. The comparison is the other way round too, because the right run is
    ///     above the transition frames in the sheet and the left run is below them.
    /// </summary>
    public static int TurningRightFrom(int currentFrame)
        => currentFrame < TurnToRight ? TurnToRight : currentFrame;
}
