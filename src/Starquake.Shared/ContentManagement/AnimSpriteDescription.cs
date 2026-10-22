// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.ContentManagement;

internal sealed record AnimSpriteDescription(
    string ResourceName,
    string FilePath,
    int Width,
    int Height,
    int TileToAnimate,
    int Delay);
