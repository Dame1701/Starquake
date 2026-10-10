// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The tunnel mouths that join one screen to the next. Walking into a left-hand mouth steps the
///     screen number down, a right-hand one steps it up. There are ten of each because the artwork
///     varies with the scenery the tunnel is cut into; they all behave the same.
///     <para>
///         The tile numbers form one unbroken run with the two directions alternating, so a left
///         mouth is always even and its right-hand partner is the next number up.
///     </para>
/// </summary>
public static class TunnelTiles
{
    /// <summary>The first and last tile numbers of the run, both inclusive.</summary>
    public const int FirstMouth = LeftMouth1;

    /// <inheritdoc cref="FirstMouth" />
    public const int LastMouth = RightMouth10;

    /// <summary>How many tunnel mouths there are in each direction.</summary>
    public const int MouthPairs = 10;

    /// <summary>
    ///     The step from one mouth to the next of the same direction. Two, because left and right
    ///     mouths alternate.
    /// </summary>
    public const int MouthStride = 2;

    /// <summary>The left-hand mouths. Walking into one moves Blob to the previous screen.</summary>
    public const int LeftMouth1 = 420;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth2 = 422;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth3 = 424;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth4 = 426;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth5 = 428;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth6 = 430;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth7 = 432;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth8 = 434;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth9 = 436;

    /// <inheritdoc cref="LeftMouth1" />
    public const int LeftMouth10 = 438;

    /// <summary>The right-hand mouths. Walking into one moves Blob to the next screen.</summary>
    public const int RightMouth1 = 421;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth2 = 423;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth3 = 425;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth4 = 427;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth5 = 429;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth6 = 431;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth7 = 433;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth8 = 435;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth9 = 437;

    /// <inheritdoc cref="RightMouth1" />
    public const int RightMouth10 = 439;

    /// <summary>
    ///     Whether <paramref name="tileImage" /> is a left-hand mouth, the kind that steps the screen
    ///     number down.
    /// </summary>
    public static bool IsLeftMouth(int tileImage)
        => tileImage is >= FirstMouth and <= LastMouth && tileImage % MouthStride == 0;

    /// <summary>
    ///     Whether <paramref name="tileImage" /> is a right-hand mouth, the kind that steps the
    ///     screen number up.
    /// </summary>
    public static bool IsRightMouth(int tileImage)
        => tileImage is >= FirstMouth and <= LastMouth && tileImage % MouthStride != 0;
}
