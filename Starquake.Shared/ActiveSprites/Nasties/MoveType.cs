// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     The six ways a nasty can move. A wandering nasty draws from all but the last, a chasing one
///     from the first two, and a spiky one always uses <see cref="SpikeyMovement" /> - which is why
///     the draws use the count of this enum and its ordering matters.
/// </summary>
internal enum MoveType
{
    RandomMovement,
    RandomUpDownLeftRight,
    DiagonalBouncing,
    DiagonalBouncingInSteps,
    RandomMovementsAllDirections,
    SpikeyMovement
}
