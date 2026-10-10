// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.SharedLibs;

/// <summary>
///     One cell of a screen that something can walk into: where it is, what is behind it, and who
///     it stops.
/// </summary>
public sealed record CollisionTile(
    int TileImage,
    int XPos,
    int YPos,
    CollisionTileType CollisionTileType,
    CollisionObjectType? ObjectType = null)
{
    /// <summary>
    ///     The only mutable member. A key-locked door flips its cells between stopping everyone and
    ///     stopping only nasties as Blob picks the key up and puts it down, instead of being removed
    ///     from the map and added back.
    /// </summary>
    public CollisionTileType CollisionTileType { get; set; } = CollisionTileType;

    /// <summary>
    ///     Set when the game placed this cell as part of an object. Null for scenery, which is most
    ///     cells.
    /// </summary>
    public CollisionObjectType? ObjectType { get; } = ObjectType;

    /// <summary>
    ///     Creates a cell owned by a game object. Takes <see cref="TileImage" /> and
    ///     <see cref="ObjectType" /> from the same argument so they cannot disagree.
    /// </summary>
    public static CollisionTile ForObject(
        CollisionObjectType objectType,
        int xPos,
        int yPos,
        CollisionTileType collisionTileType)
        => new((int)objectType, xPos, yPos, collisionTileType, objectType);

    public override string ToString()
        => "TileImage: " + TileImage + ", XPos: " + XPos + ", YPos: " + YPos + ", ColType: " + CollisionTileType;
}
