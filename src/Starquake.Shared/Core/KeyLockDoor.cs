// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.Core;

internal class KeyLockDoor
{
    /// <summary>
    ///     Where the gap sits within the tile, and how wide it is. The door's tile is
    ///     <see cref="ActionTiles.AccessForceField" />, which has nothing painted on it: the wall
    ///     either side is the neighbouring tiles' artwork, and the gap is this lit column down the
    ///     middle-right.
    /// </summary>
    /// <remarks>
    ///     The collision cells, the particles and the sheet of light must all agree about where the
    ///     gap is, so they share one rectangle. Separate offsets could drift into a door drawn where
    ///     Blob cannot walk.
    /// </remarks>
    private const int GapFromTileLeft = 48;

    /// <inheritdoc cref="GapFromTileLeft" />
    private const int GapWidth = TileMetrics.CollisionCellWidth;

    public readonly List<CollisionTile> CollisionTiles;

    /// <summary>
    ///     How hard Blob is leaning on the door, from nothing when away from it to one when standing
    ///     in the gap. A shut door braces against him; an open one has nothing to brace against. See
    ///     <c>ForceFieldSettings.IntensityAt</c>.
    /// </summary>
    public float Brace;

    /// <summary>
    ///     What is left of the burst from the moment the door gave way, and whether it has given way
    ///     at all. A door is found again every time its screen is shown, so both start as a shut door
    ///     starts: Blob arriving with the key is a door opening in front of him, which is the moment
    ///     the burst is for.
    /// </summary>
    public float Flash;

    /// <summary>
    ///     How many logic frames the door has been on screen. The field's own clock for the bands
    ///     travelling up it, the scanner working down it and the flicker, so all three run the same
    ///     whatever the display is doing.
    /// </summary>
    public int Frame;
    public Point Location;

    /// <summary>
    ///     How far open the door is drawn, from nothing when it bars Blob's way to one when it has
    ///     let him through. Eased towards its target rather than set outright.
    ///     <c>ForceFieldSettings</c> reads everything the field is drawn with off this.
    /// </summary>
    public float Openness;

    /// <inheritdoc cref="Flash" />
    public bool WasOpen;

    public KeyLockDoor()
    {
        CollisionTiles = new List<CollisionTile>();
    }

    /// <summary>
    ///     The gap in the wall, in the screen's own pixels: where the cells are laid down, where the
    ///     particles are emitted along, and what the sheet of light is drawn into.
    /// </summary>
    public Rectangle Doorway
        => new(Location.X + GapFromTileLeft, Location.Y, GapWidth, TileMetrics.TileHeight);
}
