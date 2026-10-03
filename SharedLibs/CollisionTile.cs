// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.SharedLibs;

/// <summary>
///     One cell of a screen that something can walk into: where it is, what is behind it, and who
///     it stops.
/// </summary>
/// <remarks>
///     This used to be a <c>[MessagePackObject]</c>, because a screen carried its collision map in
///     the data file. It never carried a real one - the screen editor wrote an empty list and the
///     game threw whatever was there away and built its own - so the map came off
///     <c>GameScreenData</c> and this stopped being part of the file format with it.
///     <para>
///         <see cref="ObjectType" /> is the other half of that tidy-up. A cell belonging to a
///         game object rather than to scenery used to be recognised by its
///         <see cref="TileImage" /> being over 999, which meant the image number was at once an
///         index into a tile sheet, a flag, and a discriminant. The number is still there, because
///         <c>CheckRectangles</c> hands it back and a negative would read as "nothing hit", but
///         nothing parses it any more.
///     </para>
/// </remarks>
public sealed record CollisionTile(
    int TileImage,
    int XPos,
    int YPos,
    CollisionTileType CollisionTileType,
    CollisionObjectType? ObjectType = null)
{
    /// <summary>
    ///     Mutable alone among these, because a key-locked door flips its cells between stopping
    ///     everyone and stopping only the nasties as Blob picks the key up and puts it down, rather
    ///     than taking them out of the map and putting them back.
    /// </summary>
    public CollisionTileType CollisionTileType { get; set; } = CollisionTileType;

    /// <summary>
    ///     A cell belonging to something the game placed rather than to a tile the author painted.
    ///     Null for scenery, which is most of them.
    /// </summary>
    public CollisionObjectType? ObjectType { get; } = ObjectType;

    /// <summary>
    ///     A cell owned by a game object. Sets the image number and the discriminant from the one
    ///     argument, so the two cannot drift apart.
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
