// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The tunnel mouths that join one screen to the next. There are ten of them because the artwork
///     draws a tunnel differently depending on the scenery it is cut into, but they all behave
///     alike: walking into a left-hand mouth steps the screen number down, a right-hand one steps it
///     up. They are laid out as one contiguous run of tile numbers with the two directions
///     interleaved, so a left mouth is always even and its right-hand partner is the next number up.
/// </summary>
public static class TunnelTiles
{
    /// <summary>
    ///     The first and last tile numbers of the run, for code that sweeps the whole set rather than
    ///     testing one tile. The bounds are inclusive.
    /// </summary>
    public const int FirstMouth = LeftMouth1;

    /// <inheritdoc cref="FirstMouth" />
    public const int LastMouth = RightMouth10;

    /// <summary>
    ///     How many tunnel mouths there are of each direction.
    /// </summary>
    public const int MouthPairs = 10;

    /// <summary>
    ///     The distance from one mouth of a given direction to the next, which is two because the
    ///     left and right mouths alternate.
    /// </summary>
    public const int MouthStride = 2;

    /// <summary>
    ///     The ten left-hand tunnel mouths. Walking into one of these moves Blob to the previous
    ///     screen.
    /// </summary>
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

    /// <summary>
    ///     The ten right-hand tunnel mouths. Walking into one of these moves Blob to the next
    ///     screen.
    /// </summary>
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
    ///     Whether <paramref name="tileImage" /> is one of the ten left-hand mouths - the tiles that
    ///     step the screen number down when Blob walks into one.
    /// </summary>
    /// <remarks>
    ///     The run is contiguous and the two directions alternate from an even first mouth, which is
    ///     what <see cref="FirstMouth" />, <see cref="LastMouth" /> and <see cref="MouthStride" />
    ///     are for. Those three were already here and the question was still being asked by naming
    ///     all ten, four times over - twice in <c>Blob</c> and twice in <c>ScreenRenderer</c>.
    /// </remarks>
    public static bool IsLeftMouth(int tileImage)
        => tileImage is >= FirstMouth and <= LastMouth && tileImage % MouthStride == 0;

    /// <summary>
    ///     Whether <paramref name="tileImage" /> is one of the ten right-hand mouths - the tiles that
    ///     step the screen number up.
    /// </summary>
    /// <inheritdoc cref="IsLeftMouth" />
    public static bool IsRightMouth(int tileImage)
        => tileImage is >= FirstMouth and <= LastMouth && tileImage % MouthStride != 0;
}
