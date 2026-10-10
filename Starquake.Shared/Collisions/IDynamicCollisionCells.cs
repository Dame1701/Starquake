// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.SharedLibs;

namespace Starquake.Collisions;

/// <summary>
///     Something that puts collision cells on a screen that the screen's own tiles know nothing
///     about - a jump platform Blob has laid, a key-locked door's force field, the elements in
///     the planet core.
/// </summary>
/// <remarks>
///     <see cref="ScreenCollisionMap.Rebuild" /> throws a screen's whole map away and rebuilds it
///     from the tiles and the items on them, so anything dynamic must be able to say what it owns and
///     have it put back. Cells pushed straight into the map instead are lost the next time anything
///     on the screen changes, leaving Blob to fall through a platform he just laid.
/// </remarks>
internal interface IDynamicCollisionCells
{
    /// <summary>
    ///     The cells this source has on one screen right now, and nothing for a screen it has none
    ///     on. Asked at the end of every rebuild, so it must report what is live at that moment
    ///     rather than what it once added.
    /// </summary>
    IEnumerable<CollisionTile> CellsOnScreen(int screenNumber);
}
