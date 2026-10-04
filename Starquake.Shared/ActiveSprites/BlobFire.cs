// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     One shot in flight, and the rules that move it.
/// </summary>
/// <remarks>
///     <para>
///         This used to be six near-identical branches inside <c>Blob.Update</c> - one per firing
///         direction on a shuttle, plus left and right on foot - which between them were the same
///         behaviour written out with the axes swapped and the signs flipped. Two of the six had
///         silently drifted from the other four before they were brought together here.
///     </para>
///     <para>
///         The whole thing is expressed as an axis plus a sign. A shot fired up travels along the
///         vertical axis with a sign of -1; one fired right travels along the horizontal axis with
///         a sign of +1. Deflecting reverses the sign on the axis it was fired along and starts it
///         moving on the other one, so a shot fired up that hits the ceiling comes back down and
///         away to one side. Every boundary test is then derived from the sign: a negative sign
///         stops at nought, a positive one stops at the screen's edge less the shot's own length.
///         The asymmetry that produced the drift is no longer expressible.
///     </para>
/// </remarks>
internal sealed class BlobFire(IFireSurroundings surroundings)
{
    /// <summary>
    ///     The shot itself - a thin sliver <see cref="Width" /> across and <see cref="Length" />
    ///     along, whichever way it happens to be travelling, moving <see cref="Speed" /> pixels a
    ///     frame. The long dimension doubles as the allowance the shot is given at the screen
    ///     edges, so a shot is spent when its far end leaves the playfield rather than when its
    ///     origin does. That is why <see cref="IsAtScreenEdge" /> uses it on both axes.
    /// </summary>
    private const int Width = 2;

    /// <inheritdoc cref="Width" />
    private const int Length = 32;

    /// <inheritdoc cref="Width" />
    private const int Speed = 16;

    /// <summary>
    ///     Where the sparks are thrown from when a shot hits something, measured down from the shot.
    /// </summary>
    private const int SparksYOffset = 24;

    public int X { get; private set; }

    public int Y { get; private set; }

    /// <summary>
    ///     How far along the axis it was fired on the shot has come. Only the point-blank check
    ///     reads it, and that check is measured along the same axis - see <see cref="Advance" />'s
    ///     <c>pointBlankReach</c>. A deflected shot moves on both axes and still counts only the
    ///     one it was fired along, because that is the axis it has to clear the item along.
    /// </summary>
    public int DistanceTravelled { get; private set; }

    public FireDeflection Deflection { get; private set; }

    /// <summary>
    ///     Puts a fresh shot at <paramref name="x" />, <paramref name="y" /> travelling straight.
    /// </summary>
    public void Launch(int x, int y)
    {
        X = x;
        Y = y;
        DistanceTravelled = 0;
        Deflection = FireDeflection.None;
    }

    /// <summary>
    ///     Moves the shot on one frame. Returns false once it has come to an end, at which point
    ///     Blob may fire again.
    /// </summary>
    /// <param name="firedIn">The direction Blob was facing when he fired.</param>
    /// <param name="firedFromShuttle">
    ///     Whether the shot left a shuttle. Only a shot fired from a shuttle deflects; one fired on
    ///     foot stops at the first thing it meets.
    /// </param>
    /// <param name="pointBlankReach">
    ///     Set when Blob is standing on something a shot would otherwise be stopped by, to how
    ///     far the shot has to get before that thing counts. Null when he is standing on nothing.
    ///     Measured along the axis the shot was fired on, and <c>BlobWeapon.PointBlankReach</c> is
    ///     what measures it.
    ///     <para>
    ///         This used to apply to horizontal shots alone, so a shot fired up or down from a
    ///         shuttle while Blob stood over a collectable died where it was born. The asymmetry
    ///         was inherited rather than intended - nothing about a vertical shot makes the item
    ///         underneath him more of an obstacle.
    ///     </para>
    /// </param>
    public bool Advance(Direction firedIn, bool firedFromShuttle, int? pointBlankReach)
    {
        Axis? firedAlong = AxisOf(firedIn);

        if (firedAlong is null)
        {
            // Nothing steps along this direction. On foot the shot is dropped, which is what
            // happens when Blob walks off a shuttle pad mid-shot; from a shuttle it is left
            // where it is.
            return firedFromShuttle;
        }

        if (!firedFromShuttle && firedAlong != Axis.Horizontal)
        {
            return false;
        }

        int firedSign = SignOf(firedIn);

        return Deflection == FireDeflection.None || !firedFromShuttle
            ? AdvanceStraight(firedIn, firedAlong.Value, firedSign, firedFromShuttle, pointBlankReach)
            : AdvanceDeflected(firedAlong.Value, firedSign, pointBlankReach);
    }

    private bool AdvanceStraight(
        Direction travellingIn,
        Axis along,
        int sign,
        bool canDeflect,
        int? pointBlankReach)
    {
        int step = surroundings.DistanceClear(travellingIn, X, Y, Width, Length, Speed);

        if (WithinPointBlankReach(pointBlankReach))
        {
            step = Speed;
        }

        if (canDeflect && step == 0)
        {
            Deflection = surroundings.ChooseDeflection();
            surroundings.ThrowSparks(X, Y + SparksYOffset);

            // It deflects this frame and travels the next one.
            return true;
        }

        bool atScreenEdge = IsAtScreenEdge(along, sign);

        if (step > 0 && !atScreenEdge)
        {
            Move(along, sign * step);
            DistanceTravelled += step;

            return true;
        }

        Die(atScreenEdge);
        return false;
    }

    private bool AdvanceDeflected(Axis firedAlong, int firedSign, int? pointBlankReach)
    {
        // Deflecting reverses the axis it was fired along and opens up the other one.
        int deflectedSign = Deflection == FireDeflection.Left ? -1 : 1;
        int signX = firedAlong == Axis.Horizontal ? -firedSign : deflectedSign;
        int signY = firedAlong == Axis.Vertical ? -firedSign : deflectedSign;

        int stepX = surroundings.DistanceClear(DirectionAlong(Axis.Horizontal, signX), X, Y, Width, Length, Speed);
        int stepY = surroundings.DistanceClear(DirectionAlong(Axis.Vertical, signY), X, Y, Width, Length, Speed);

        if (WithinPointBlankReach(pointBlankReach))
        {
            stepX = Speed;
            stepY = Speed;
        }

        bool atScreenEdge = IsAtScreenEdge(Axis.Horizontal, signX) || IsAtScreenEdge(Axis.Vertical, signY);

        if (stepX > 0 && stepY > 0 && !atScreenEdge)
        {
            X += signX * stepX;
            Y += signY * stepY;
            DistanceTravelled += firedAlong == Axis.Horizontal ? stepX : stepY;

            return true;
        }

        Die(atScreenEdge);
        return false;
    }

    /// <summary>
    ///     A shot that runs out of room goes quietly at the screen's edge and throws sparks
    ///     anywhere else.
    /// </summary>
    private void Die(bool atScreenEdge)
    {
        if (!atScreenEdge)
        {
            surroundings.ThrowSparks(X, Y + SparksYOffset);
        }
    }

    private bool WithinPointBlankReach(int? pointBlankReach)
        => pointBlankReach is { } reach && DistanceTravelled < reach;

    /// <summary>
    ///     Whether the shot has run out of screen rather than run into something. What hangs off it
    ///     is whether <see cref="Die" /> throws sparks: a shot that reaches the edge goes quietly,
    ///     and one stopped anywhere else has hit scenery and says so.
    /// </summary>
    /// <remarks>
    ///     This was wrong twice over, and the two faults hid each other. It measured the shot as
    ///     <see cref="Length" /> along both axes, where the box is <see cref="Width" /> across and
    ///     <see cref="Length" /> down - so a shot travelling right was called spent thirty pixels
    ///     early, which is what made that direction look correct. And the low side tested for the
    ///     shot being <em>past</em> the edge rather than at it, which it never reaches: the
    ///     collision map clamps anything that is not Blob to the screen, so a shot fired left stops
    ///     dead at nought with its position still not below it, and threw sparks as though it had
    ///     hit a wall. The measurement is right on both axes now and both bounds are inclusive.
    /// </remarks>
    private bool IsAtScreenEdge(Axis along, int sign)
    {
        bool sideways = along == Axis.Horizontal;

        int position = sideways ? X : Y;
        int extent = sideways ? Width : Length;
        int screen = sideways ? surroundings.ScreenWidth : surroundings.ScreenHeight;

        return sign < 0
            ? position <= 0
            : position + extent >= screen;
    }

    private void Move(Axis along, int amount)
    {
        if (along == Axis.Horizontal)
        {
            X += amount;
        }
        else
        {
            Y += amount;
        }
    }

    private static Axis? AxisOf(Direction direction)
        => direction switch
        {
            Direction.Left or Direction.Right => Axis.Horizontal,
            Direction.Up or Direction.Down => Axis.Vertical,
            _ => null
        };

    private static int SignOf(Direction direction)
        => direction is Direction.Left or Direction.Up ? -1 : 1;

    private static Direction DirectionAlong(Axis along, int sign)
        => along == Axis.Horizontal
            ? sign < 0 ? Direction.Left : Direction.Right
            : sign < 0
                ? Direction.Up
                : Direction.Down;

    private enum Axis
    {
        Horizontal,
        Vertical
    }
}
