// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;

namespace Starquake.SharedLibs;

/// <summary>
///     Where the planet core sits on the screen that carries it, in pixels from its top left.
/// </summary>
/// <remarks>
///     A position rather than a tile, because the core is one and a half tiles across and has never
///     been tile-aligned - see <see cref="Starquake.Structure.CoreSite" />, which is what reads this
///     back and what decides the resolution it is rounded to.
/// </remarks>
[MessagePackObject]
public sealed record CorePlacement(int XPos, int YPos)
{
    [Key(0)] public int XPos { get; } = XPos;

    [Key(1)] public int YPos { get; } = YPos;
}
