// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     Checks a door's access code against what Blob is carrying. For each digit of the code it
///     finds the inventory slot that matches, if any. Used by the access booths, which ask for three
///     digits, and the Cheops pyramid, which asks for two.
/// </summary>
internal static class AccessCodeLock
{
    /// <summary>
    ///     Pick-up types 10 to 19 match the digits 0 to 9. Subtract this from a pick-up type to get
    ///     its digit; add it to a digit to get the pick-up to draw.
    /// </summary>
    public const int FirstDigitPickup = 10;

    /// <summary>No inventory slot matches this digit.</summary>
    public const int NoSlot = -1;

    /// <summary>
    ///     Splits a code into one digit per character.
    /// </summary>
    /// <param name="code">The code, or null on a screen with no door.</param>
    /// <param name="digits">How many digits the door asks for.</param>
    /// <returns>
    ///     The digits, or all <see cref="NoSlot" /> if there is no code. No pick-up matches
    ///     <see cref="NoSlot" />, so a door with no code never opens.
    /// </returns>
    public static int[] Digits(string? code, int digits)
    {
        int[] read = new int[digits];

        for (int digit = 0; digit < digits; digit++)
        {
            read[digit] = code == null ? NoSlot : int.Parse(code.Substring(digit, 1));
        }

        return read;
    }

    /// <summary>
    ///     Finds which inventory slot matches each digit of the code, and whether they all matched.
    /// </summary>
    public static AccessCodeScan Scan(Collectables[] inventory, int[] code)
    {
        int[] slotToScan = new int[code.Length];
        bool accepted = true;

        for (int digit = 0; digit < code.Length; digit++)
        {
            slotToScan[digit] = SlotAnswering(inventory, code[digit]);

            if (slotToScan[digit] == NoSlot)
            {
                accepted = false;
            }
        }

        return new AccessCodeScan(slotToScan, accepted);
    }

    /// <summary>
    ///     The first slot holding a pick-up that matches <paramref name="digit" />, or
    ///     <see cref="NoSlot" /> if Blob is carrying nothing that does. An access card matches any
    ///     digit, so a single card opens a door on its own.
    /// </summary>
    private static int SlotAnswering(Collectables[] inventory, int digit)
    {
        for (int slot = 0; slot < inventory.Length; slot++)
        {
            if ((int)inventory[slot] - FirstDigitPickup == digit ||
                inventory[slot] == Collectables.AccessCard)
            {
                return slot;
            }
        }

        return NoSlot;
    }
}
