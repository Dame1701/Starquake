// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     A door that reads a code off Blob's inventory. The access booths ask for three digits and the
///     Cheops pyramid for two, and both answer the same question: for each digit of the code, is
///     Blob carrying something that satisfies it, and if so which slot is it in.
/// </summary>
/// <remarks>
///     This was written twice, character for character, as <c>CheckInventoryAgainstCode</c> in
///     <c>AccessBoothScreen</c> and <c>CheopsScreen</c> - the only difference between the two copies
///     being the name of the field each wrote its verdict into. Reading the code off the screen was
///     the same twice over too, differing only in which property of the screen data held the string.
///     <para>
///         Neither needs a graphics device, so both are here and both are tested. What stayed behind
///         is the part that does: the <c>KeyScanner</c> animation each digit is shown through, which
///         the screens reset from the slots this hands back. That is the same trade
///         <see cref="Starquake.ActiveSprites.BlobFall" /> makes with the sound of a long fall.
///     </para>
/// </remarks>
internal static class AccessCodeLock
{
    /// <summary>
    ///     Where the ten digit-bearing pick-ups start in the sprite sheet. A pick-up of type 10
    ///     answers the digit 0, 11 answers 1, and so on up to 19 - which is why the scan subtracts
    ///     this before comparing, and why the booth adds it back to draw the digit it wants.
    /// </summary>
    /// <remarks>
    ///     The first twenty-eight types are the core elements, so the ten that read as digits sit
    ///     inside that run rather than after it. <see cref="Collectables" /> names only the two that
    ///     are not elements at all, the card and the key.
    /// </remarks>
    public const int FirstDigitPickup = 10;

    /// <summary>
    ///     What <see cref="AccessCodeScan.SlotToScan" /> holds for a digit nothing in the inventory
    ///     answers, and what an unread code's digits hold.
    /// </summary>
    public const int NoSlot = -1;

    /// <summary>
    ///     Reads a code off the screen it is stored on, one digit per character.
    /// </summary>
    /// <param name="code">
    ///     The code as the world generator wrote it, or null on a screen that has no door. A screen
    ///     with no code leaves every digit at <see cref="NoSlot" />, which nothing in the inventory
    ///     can answer - so a door without a code never opens.
    /// </param>
    /// <param name="digits">How many digits this door asks for: three for a booth, two for the pyramid.</param>
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
    ///     Reads <paramref name="inventory" /> against <paramref name="code" />, a digit at a time.
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
    ///     The first slot holding something that satisfies <paramref name="digit" />, or
    ///     <see cref="NoSlot" /> if Blob is carrying nothing that does.
    /// </summary>
    /// <remarks>
    ///     The access card answers any digit, which is what makes it worth carrying: one card opens
    ///     a door on its own, however many digits that door asks for.
    /// </remarks>
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
