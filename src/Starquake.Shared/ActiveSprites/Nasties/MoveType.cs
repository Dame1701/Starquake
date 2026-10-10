// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     The six ways a nasty can move. The order is load-bearing: a wandering nasty draws from all but
///     the last, a chasing one from the first two, and the random draws are made over counts taken
///     from this enum.
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
