// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using MessagePack;

namespace Starquake.SharedLibs;

/// <summary>
///     Where the planet core sits on the screen that carries it, in pixels from its top left.
/// </summary>
/// <remarks>
///     A pixel position rather than a tile, because the core is one and a half tiles across and is
///     not tile-aligned. <see cref="Starquake.Structure.CoreSite" /> reads this back and decides
///     what resolution it is rounded to.
/// </remarks>
[MessagePackObject]
public sealed record CorePlacement(int XPos, int YPos)
{
    [Key(0)] public int XPos { get; } = XPos;

    [Key(1)] public int YPos { get; } = YPos;
}
