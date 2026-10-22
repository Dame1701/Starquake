// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

/// <summary>
///     What <see cref="AccessCodeLock.Scan" /> read: which inventory slot matches each digit of the
///     code, and whether every digit matched.
/// </summary>
/// <remarks>
///     A plain struct rather than the <c>record struct</c> used elsewhere for a value like this. A
///     record would generate equality over <see cref="SlotToScan" />, and array equality is by
///     reference, so two identical scans would compare unequal. That is worse than no equality.
/// </remarks>
/// <param name="slotToScan">
///     One entry per digit, in the code's order: the inventory slot that matches it, or
///     <see cref="AccessCodeLock.NoSlot" />.
/// </param>
/// <param name="accepted">Whether every digit matched, and so whether the door opens.</param>
internal readonly struct AccessCodeScan(int[] slotToScan, bool accepted)
{
    public int[] SlotToScan { get; } = slotToScan;

    public bool Accepted { get; } = accepted;
}
