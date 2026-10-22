// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Scoring;

/// <summary>
///     The power-ups, in the order the tile sheet holds them. The world generator draws one with
///     <c>Next(8)</c> and the number reaches <see cref="PlayerStats" /> as the sprite it was drawn
///     as, so the order is load-bearing.
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
