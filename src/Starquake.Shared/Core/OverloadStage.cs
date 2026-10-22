// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

/// <summary>How far through coming apart Blob is.</summary>
internal enum OverloadStage
{
    /// <summary>
    ///     Coming apart: swelling and shaking while the charge splits Blob open and light pours out
    ///     of the cracks. The whole of the death bar the blast.
    /// </summary>
    Cracking,

    /// <summary>The blast: shells of Blob thrown outward, over in a tenth of a second.</summary>
    Discharging,

    /// <summary>Gone. A death that skips the alarm starts here.</summary>
    Exploded
}
