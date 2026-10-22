// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     One shot in flight, and the rules that move it.
/// </summary>
/// <remarks>
///     A shot is an axis plus a sign. Fired up means the vertical axis with a sign of -1; fired right
///     means the horizontal axis with a sign of +1. Deflecting reverses the sign on the axis fired
///     along and starts movement on the other, so a shot fired up that hits the ceiling comes back
///     down and away to one side.
///     <para>
///         Every boundary test follows from the sign: a negative sign stops at nought, a positive one
///         at the screen's edge less the shot's own length. Expressing all six firing directions this
///         way is what keeps them from drifting apart.
///     </para>
/// </remarks>
internal sealed class BlobFire(IFireSurroundings surroundings)
{
    /// <summary>
    ///     The shot itself: a sliver <see cref="Width" /> across and <see cref="Length" /> along,
    ///     whichever way it travels, moving <see cref="Speed" /> pixels a frame. The long dimension
    ///     is also the allowance at the screen edges, so a shot is spent when its far end leaves the
    ///     playfield rather than its origin. <see cref="IsAtScreenEdge" /> uses it on both axes.
    /// </summary>
    private const int Width = 2;

    /// <inheritdoc cref="Width" />
    private const int Length = 32;

    /// <inheritdoc cref="Width" />
    private const int Speed = 16;

    /// <summary>
    ///     Where sparks are thrown from when a shot hits something, measured down from the shot.
    /// </summary>
    private const int SparksYOffset = 24;

    public int X { get; private set; }

    public int Y { get; private set; }

    /// <summary>
    ///     How far the shot has travelled along the axis it was fired on. Only the point-blank check
    ///     reads it, measured along the same axis. A deflected shot moves on both axes but still
    ///     counts only the one it was fired along, because that is the axis it must clear the item on.
    /// </summary>
    public int DistanceTravelled { get; private set; }

    public FireDeflection Deflection { get; private set; }

    /// <summary>Puts a fresh shot at <paramref name="x" />, <paramref name="y" />, travelling straight.</summary>
    public void Launch(int x, int y)
    {
        X = x;
        Y = y;
        DistanceTravelled = 0;
        Deflection = FireDeflection.None;
    }

    /// <summary>Moves the shot on one frame.</summary>
    /// <param name="firedIn">The direction Blob was facing when firing.</param>
    /// <param name="firedFromShuttle">
    ///     Whether the shot left a shuttle. Only those deflect; a shot fired on foot stops at the
    ///     first thing it meets.
    /// </param>
    /// <param name="pointBlankReach">
    ///     How far the shot must travel before the thing Blob is standing on counts as an obstacle,
    ///     or null when Blob stands on nothing. Measured along the axis the shot was fired on, by
    ///     <c>BlobWeapon.PointBlankReach</c>. Applies to all four directions.
    /// </param>
    /// <returns>False once the shot has ended, at which point Blob may fire again.</returns>
    public bool Advance(Direction firedIn, bool firedFromShuttle, int? pointBlankReach)
    {
        Axis? firedAlong = AxisOf(firedIn);

        if (firedAlong is null)
        {
            // Nothing steps along this direction. On foot the shot is dropped, which happens when
            // Blob walks off a shuttle pad mid-shot. From a shuttle it stays where it is.
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
    ///     Ends the shot. It goes quietly at the screen's edge and throws sparks anywhere else.
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
    ///     Whether the shot ran out of screen rather than into something. This decides whether
    ///     <see cref="Die" /> throws sparks: a shot reaching the edge goes quietly, one stopped
    ///     anywhere else has hit scenery.
    /// </summary>
    /// <remarks>
    ///     Both bounds are inclusive, because the collision map clamps everything but Blob to the
    ///     screen: a shot fired left stops dead at nought and never gets past it. The sliver is
    ///     measured as <see cref="Width" /> across and <see cref="Length" /> down, so each axis uses
    ///     the right dimension.
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
