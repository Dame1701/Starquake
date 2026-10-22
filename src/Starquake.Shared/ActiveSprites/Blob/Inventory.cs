// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;
using Starquake.SharedLibs;

namespace Starquake.ActiveSprites;

/// <summary>
///     The four things Blob can carry, and the one move available with them. Pressing down on a
///     pick-up drops the back of the queue on the floor, shuffles everything along, and takes in
///     whatever Blob is standing on at the front.
/// </summary>
/// <remarks>
///     A queue rather than a set of slots, which is where the game's puzzles come from. Reaching an
///     item three places back means cycling everything in front of it onto the floor and picking it
///     all up again.
/// </remarks>
internal class Inventory
{
    /// <summary>
    ///     How many things Blob can carry. Picking something up pushes the whole inventory along one
    ///     and drops whatever falls off the end.
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
    ///     Drops the back of the queue, shuffles along, and picks up whatever Blob is standing on.
    ///     Never the item just dropped, even though it lands underfoot.
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
    ///     Drops whatever is at the back onto the floor, snapped to the collision grid.
    /// </summary>
    /// <returns>The dropped item, so the pick-up pass can tell it from anything already there.</returns>
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

            // The item just dropped lies on this square, and picking it straight back up would undo
            // the swap.
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
