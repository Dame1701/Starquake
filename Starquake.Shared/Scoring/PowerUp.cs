// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Scoring;

/// <summary>
///     The power-ups, in the order the tile sheet holds them - the world generator draws one of
///     these with <c>Next(8)</c> and the number reaches <see cref="PlayerStats" /> as the sprite it
///     was drawn as. They were eight named ints compared against in an if/else chain.
/// </summary>
internal enum PowerUp
{
    EnergyMedium = 0,
    EnergySmall = 1,
    EnergyFull = 2,
    FirePowerMedium = 3,
    FirePowerSmall = 4,
    Platforms = 5,
    WhateverIsLowest = 6,
    ExtraLife = 7
}
