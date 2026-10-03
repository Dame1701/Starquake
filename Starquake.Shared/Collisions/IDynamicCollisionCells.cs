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
///     <see cref="ScreenCollisionMap.Rebuild" /> throws a screen's whole map away and builds it
///     again from the tiles and the items lying on them, so anything dynamic has to be able to say
///     what it owns and have it put back. Pushing cells straight into the map was why Blob fell
///     through a platform he had just laid: the next thing to change on the screen - a pick-up
///     taken, a shuttle pad boarded, a breakable bridge finishing its dissolve - rebuilt the map and
///     the platform's two cells went with it, while the platform itself was still being drawn.
/// </remarks>
internal interface IDynamicCollisionCells
{
    /// <summary>
    ///     The cells this source has on one screen right now, and nothing at all for a screen it has
    ///     none on. Asked at the end of every rebuild, so it reports what is live at that moment
    ///     rather than what it once added.
    /// </summary>
    IEnumerable<CollisionTile> CellsOnScreen(int screenNumber);
}
