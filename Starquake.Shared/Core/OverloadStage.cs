// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     How far through coming apart Blob is.
/// </summary>
/// <remarks>
///     These were three <c>int</c> constants on <c>Blob</c> numbered 0, 1 and 3 - the gap at 2 was there
///     when the port was made and nothing ever held it. The only comparison made of them was against
///     <see cref="Exploded" />, so the numbering carried no meaning the names do not.
///     <para>
///         There used to be a <c>Swelling</c> and a <c>Shrinking</c> between these two, which took
///         him up to one and a half times his size and then back down through zero to draw him
///         inside out. Eleven logic frames for the pair, which is a fifth of a second - too quick
///         to be read as a shape at all, and the cracking it now gives way to says the same thing
///         with time to be seen. The inside-out draw went with it.
///     </para>
/// </remarks>
internal enum OverloadStage
{
    /// <summary>
    ///     Coming apart: swelling and shaking while the charge splits him open and light pours out
    ///     of the cracks. This is the whole of the death now bar the blast.
    /// </summary>
    Cracking,

    /// <summary>
    ///     The blast: shells of him thrown outward, over in a tenth of a second.
    /// </summary>
    Discharging,

    /// <summary>Gone. A death that skips the alarm starts here.</summary>
    Exploded
}
