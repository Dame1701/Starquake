// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.SharedLibs;

public sealed record InteractiveItem(int XLocation, int YLocation, int Type)
{
    public int XLocation { get; } = XLocation;

    public int YLocation { get; } = YLocation;

    public int Type { get; set; } = Type;
}
