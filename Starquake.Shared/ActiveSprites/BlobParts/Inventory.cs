// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites.BlobParts;

/// <summary>
///     The four things the blob can carry, and the one move he can make with them: press down on a
///     pick-up and the back of the queue is dropped on the floor, everything shuffles along, and
///     whatever he is standing on goes in at the front.
/// </summary>
/// <remarks>
///     A queue rather than a set of slots, which is what makes the game's puzzles what they are: to
///     get at an item three places back, the blob has to cycle everything in front of it out onto
///     the floor and pick it all up again.
/// </remarks>
public class Inventory
{
    /// <summary>
    ///     How many things the blob can carry. Picking something up pushes the whole inventory along
    ///     one and drops whatever falls off the end.
    /// </summary>
    public const int Capacity = 4;

    public Inventory()
    {
        Items = new Collectables[Capacity];

        for (int slot = 0; slot < Items.Length; slot++)
        {
            Items[slot] = Collectables.None;
        }
    }

    /// <summary>
    ///     What is held, front first. The status panel draws these and the access booths read them.
    /// </summary>
    public Collectables[] Items { get; }

    /// <summary>
    ///     Drops the back of the queue, shuffles along, and picks up whatever the blob is standing
    ///     on - which is never the item just dropped, even though it lands under his feet.
    /// </summary>
    public void SwapWithFloor(Rectangle blob, List<InteractiveItem> pickUpsOnScreen)
    {
        InteractiveItem? itemDropped = DropLastHeldItem(blob, pickUpsOnScreen);

        ShuffleAlong();
        PickUpItemUnderfoot(blob, pickUpsOnScreen, itemDropped);
    }

    private void ShuffleAlong()
    {
        for (int slot = Items.Length - 1; slot > 0; slot--)
        {
            Items[slot] = Items[slot - 1];
            Items[slot - 1] = Collectables.None;
        }
    }

    /// <summary>
    ///     Drops whatever is at the back of the inventory onto the floor, snapped to the collision
    ///     grid, and hands it back so the pick-up pass can tell it apart from anything already there.
    /// </summary>
    private InteractiveItem? DropLastHeldItem(Rectangle blob, List<InteractiveItem> pickUpsOnScreen)
    {
        if (Items[^1] == Collectables.None)
        {
            return null;
        }

        int itemXPos = blob.X / TileMetrics.CollisionCellWidth * TileMetrics.CollisionCellWidth;
        int itemYPos = blob.Y / TileMetrics.CollisionCellHeight * TileMetrics.CollisionCellHeight;

        var droppedItem = new InteractiveItem(itemXPos, itemYPos, (int)Items[^1]);

        pickUpsOnScreen.Add(droppedItem);

        // Nothing held here now
        Items[^1] = Collectables.None;

        return droppedItem;
    }

    private void PickUpItemUnderfoot(
        Rectangle blob,
        List<InteractiveItem> pickUpsOnScreen,
        InteractiveItem? itemDropped)
    {
        for (int a = 0; a < pickUpsOnScreen.Count; a++)
        {
            if (!CollisionDetection.Check(
                    blob.X,
                    blob.Y,
                    blob.Width,
                    blob.Height,
                    pickUpsOnScreen[a].XLocation + SpriteMetrics.SpriteWidth / 4,
                    pickUpsOnScreen[a].YLocation + TileMetrics.CollisionCellHeight,
                    TileMetrics.CollisionCellWidth,
                    TileMetrics.CollisionCellHeight))
            {
                continue;
            }

            // Make sure it's not what we just dropped....
            if (itemDropped != null && itemDropped == pickUpsOnScreen[a])
            {
                continue;
            }

            Items[0] = (Collectables)pickUpsOnScreen[a].Type;
            pickUpsOnScreen.RemoveAt(a);

            break;
        }
    }
}
