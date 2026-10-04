// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

internal enum CoreElementState
{
    Missing,

    /// <summary>
    ///     Picked out by the core as the part it is about to take, glowing and shaking in its slot
    ///     while the alarm sounds. The beat before <see cref="Replacing" />, which there was none
    ///     of - a part used to simply appear at three times size and shrink.
    /// </summary>
    Identified,

    Replacing,
    Replaced
}
