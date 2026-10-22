// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.ActiveSprites;

/// <summary>
///     Where walking off a screen edge puts Blob: which screen, and where on it.
/// </summary>
/// <param name="ScreenNumber">The screen arrived on.</param>
/// <param name="XPos">
///     Where Blob arrives across. Set by the edge for a sideways crossing, and unchanged going up or
///     down.
/// </param>
/// <param name="YPos">
///     Where Blob arrives down. Zero through the bottom edge, and unchanged going up. A screen
///     crossed upwards needs something to land on, which depends on whether a shuttle or a tunnel
///     lift is carrying Blob, so <c>Blob</c> settles that.
/// </param>
internal readonly record struct ScreenCrossing(int ScreenNumber, int XPos, int YPos);
