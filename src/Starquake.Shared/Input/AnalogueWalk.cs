// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Input;

/// <summary>
///     F3's walking mode, where the left stick is read as eight directions rather than four and how
///     far it leans sets how fast Blob walks, anywhere from one pixel a frame up to the full step.
///     The keys and the d-pad always walk at the full step.
/// </summary>
/// <remarks>
///     The tunnel lift and the shuttle pad only take Blob at one exact position, and flying the
///     shuttle up through a gap needs Blob exactly under it. All of those sit on a collision cell's
///     edge, so the steps are spread evenly across each cell, and every walk at any speed stops on
///     every cell edge it passes rather than stepping over one.
/// </remarks>
internal static class AnalogueWalk
{
    /// <summary>The smallest lean that counts as a direction, and walks at one pixel a frame.</summary>
    public const float MinimumLean = 0.1f;

    /// <summary>
    ///     The lean that walks at the full step. Short of the stick's rim, so full speed does not
    ///     depend on a stick that reaches all the way to one.
    /// </summary>
    private const float FullSpeedLean = 0.9f;

    /// <summary>
    ///     How far off a direction the stick can lean and still count as it: tan 22.5°, which splits
    ///     the stick into eight equal slices. A diagonal counts as both of its directions, and a push
    ///     that is mostly down still counts as only down, so it lays a platform rather than walking.
    /// </summary>
    private const float SliceSlope = 0.4142f;

    /// <summary>
    ///     Whether a stick leaning <paramref name="along" /> a direction, positive towards it, and
    ///     <paramref name="across" /> it counts as pushing that way.
    /// </summary>
    public static bool Leans(float along, float across)
        => along >= MinimumLean && along > SliceSlope * Math.Abs(across);

    /// <summary>
    ///     How far to walk this frame from <paramref name="xPos" /> for a lean of
    ///     <paramref name="lean" />. The speed the lean asks for is spread evenly over the frames it
    ///     takes to reach the next cell edge, so the steps differ by a pixel at most and the last one
    ///     lands on the edge.
    /// </summary>
    public static int StepFor(float lean, int xPos, bool leftwards, int fullStep)
    {
        float reach = Math.Clamp((Math.Abs(lean) - MinimumLean) / (FullSpeedLean - MinimumLean), 0f, 1f);
        float speed = 1 + (fullStep - 1) * reach;

        int toEdge = DistanceToNextCellEdge(xPos, leftwards);
        int framesToEdge = (int)MathF.Ceiling(toEdge / speed);

        return Math.Min(fullStep, (toEdge + framesToEdge - 1) / framesToEdge);
    }

    private static int DistanceToNextCellEdge(int xPos, bool leftwards)
    {
        const int cell = TileMetrics.CollisionCellWidth;

        int intoCell = (xPos % cell + cell) % cell;

        if (intoCell == 0)
        {
            return cell;
        }

        return leftwards ? intoCell : cell - intoCell;
    }
}
