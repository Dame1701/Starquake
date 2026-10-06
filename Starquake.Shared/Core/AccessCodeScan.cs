// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     What <see cref="AccessCodeLock.Scan" /> read: which inventory slot answers each digit of the
///     code, and whether every digit was answered.
/// </summary>
/// <remarks>
///     A plain struct rather than a <c>record struct</c>, which the rest of the codebase prefers for
///     a value like this. A record would generate equality over <see cref="SlotToScan" />, and
///     equality on an array is by reference - so two scans that read the same would compare unequal,
///     which is a worse answer than having no equality at all.
/// </remarks>
/// <param name="slotToScan">
///     One entry per digit, in the code's own order: the inventory slot that answers it, or
///     <see cref="AccessCodeLock.NoSlot" />.
/// </param>
/// <param name="accepted">Whether every digit was answered, and so whether the door opens.</param>
internal readonly struct AccessCodeScan(int[] slotToScan, bool accepted)
{
    public int[] SlotToScan { get; } = slotToScan;

    public bool Accepted { get; } = accepted;
}
