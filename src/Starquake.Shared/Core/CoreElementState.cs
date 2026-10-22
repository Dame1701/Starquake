// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

internal enum CoreElementState
{
    Missing,

    /// <summary>
    ///     Picked out by the core as the part it is about to take, glowing and shaking in its slot
    ///     while the alarm sounds. The beat before <see cref="Replacing" />.
    /// </summary>
    Identified,

    Replacing,
    Replaced
}
