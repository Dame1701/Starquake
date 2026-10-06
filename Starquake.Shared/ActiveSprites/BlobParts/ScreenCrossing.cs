// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites.BlobParts;

/// <summary>
///     Where walking off the edge of a screen puts Blob: the screen he arrives on, and where on it.
/// </summary>
/// <param name="ScreenNumber">The screen he arrives on.</param>
/// <param name="XPos">
///     Where he arrives across. Decided by the edge for a sideways crossing - against the far wall
///     going left, hard against nothing going right - and unchanged going up or down.
/// </param>
/// <param name="YPos">
///     Where he arrives down. Zero coming through the bottom edge, and unchanged going up: a screen
///     crossed upwards has to put him down on something, and what that is depends on whether a
///     shuttle or a tunnel lift is carrying him. <c>Blob</c> settles that; this only says which
///     screen he is settling onto.
/// </param>
internal readonly record struct ScreenCrossing(int ScreenNumber, int XPos, int YPos);
