// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     How Blob's sprite sheet lays out the walk, and which frame a cycle resumes at after turning
///     round mid-stride.
/// </summary>
/// <remarks>
///     Left and right are separate runs of frames rather than one run flipped, so turning means
///     jumping between them. The two transition frames sit between the runs, beside
///     <see cref="Resting" />, for that jump. All eleven frames:
///     <para>
///         <c>0 1 2 3</c> walking left, <c>4</c> turning right, <c>5</c> standing still, <c>6</c>
///         turning left, <c>7 8 9 10</c> walking right.
///     </para>
///     <para>
///         This layout is load-bearing. The turn rule below is two comparisons against the
///         transition frames, and it only produces a matching pose because each transition frame
///         sits immediately beside the run it turns away from. Renumbering the sheet without
///         renumbering these still compiles and still looks right in a diff, but makes Blob snap to
///         the wrong pose on every turn.
///     </para>
/// </remarks>
internal static class BlobWalkFrames
{
    /// <summary>
    ///     The standing frame, used on a fresh screen and after a death.
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
    ///     Where a walk cycle resumes after turning round mid-stride, so the new direction starts
    ///     from a matching pose instead of snapping to the first frame.
    /// </summary>
    public const int TurnToLeft = 6;

    /// <inheritdoc cref="TurnToLeft" />
    public const int TurnToRight = 4;

    /// <summary>How many logic frames each frame of a walk cycle is held for.</summary>
    public const int Delay = 2;

    /// <summary>
    ///     The frame the left walk starts from, given the current pose. Coming out of the right run
    ///     drops back to <see cref="TurnToLeft" />, the pose beside it. Already facing left or
    ///     standing still carries on from where it is.
    /// </summary>
    public static int TurningLeftFrom(int currentFrame)
        => currentFrame > TurnToLeft ? TurnToLeft : currentFrame;

    /// <summary>
    ///     The same for the right walk. The comparison is reversed, because the right run sits above
    ///     the transition frames in the sheet and the left run below them.
    /// </summary>
    public static int TurningRightFrom(int currentFrame)
        => currentFrame < TurnToRight ? TurnToRight : currentFrame;
}
