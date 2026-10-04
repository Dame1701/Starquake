// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

internal class KeyLockDoor
{
    /// <summary>
    ///     Where the gap is within the tile, and how wide it is. The door's tile is
    ///     <see cref="ActionTiles.AccessForceField" />, which has nothing painted on it at all -
    ///     the wall either side of the gap is the neighbouring tiles' artwork, and the gap itself
    ///     is this column down the middle-right of the tile with light in it.
    /// </summary>
    /// <remarks>
    ///     The collision cells, the particles and the sheet of light all have to agree about where
    ///     the gap is, and they used to be three sets of offsets written down separately - 48 for
    ///     the cells, 52 and 76 for the particle columns, 60 for the bright core. They are one
    ///     rectangle now, so a door that is drawn where Blob cannot walk is a thing that cannot
    ///     happen rather than a thing that has not happened yet.
    /// </remarks>
    private const int GapFromTileLeft = 48;

    /// <inheritdoc cref="GapFromTileLeft" />
    private const int GapWidth = TileMetrics.CollisionCellWidth;

    public readonly List<CollisionTile> CollisionTiles;
    public Point Location;

    /// <summary>
    ///     How far open the door is drawn, from nothing when it bars Blob's way to one when it has
    ///     let him through. Eased towards whichever it should be rather than set - see
    ///     <c>ForceFieldSettings</c>, which is where everything the field is drawn with is read off
    ///     this.
    /// </summary>
    public float Openness;

    /// <summary>
    ///     What is left of the burst at the moment the door gave way, and whether it has given way
    ///     at all. A door is found again every time its screen is shown, so both start where a shut
    ///     door starts: Blob arriving with the key in his hands is a door opening in front of him,
    ///     which is the moment the burst is there for.
    /// </summary>
    public float Flash;

    /// <inheritdoc cref="Flash" />
    public bool WasOpen;

    /// <summary>
    ///     How hard Blob is leaning on the door, from nothing when he is away from it to one when
    ///     he is standing in the gap. A shut door braces against him and an open one has nothing to
    ///     brace against - see <c>ForceFieldSettings.IntensityAt</c>.
    /// </summary>
    public float Brace;

    /// <summary>
    ///     How many logic frames the door has been on screen. The field's own clock - the bands
    ///     travelling up it, the scanner working down it, the flicker - rather than the game's, so
    ///     that it is the same whatever the display is doing.
    /// </summary>
    public int Frame;

    public KeyLockDoor()
    {
        CollisionTiles = new List<CollisionTile>();
    }

    /// <summary>
    ///     The gap in the wall, in the screen's own pixels: what the cells are laid down, what the
    ///     particles are emitted along, and what the sheet of light is drawn into.
    /// </summary>
    public Rectangle Doorway
        => new(Location.X + GapFromTileLeft, Location.Y, GapWidth, TileMetrics.TileHeight);
}
