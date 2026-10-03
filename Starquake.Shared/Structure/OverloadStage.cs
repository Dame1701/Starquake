// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     How far through coming apart Blob is.
/// </summary>
/// <remarks>
///     These were three <c>int</c> constants on <c>Blob</c> numbered 0, 1 and 3 - the gap at 2 was there
///     when the port was made and nothing ever held it. The only comparison made of them was against
///     <see cref="Exploded" />, so the numbering carried no meaning the names do not.
/// </remarks>
internal enum OverloadStage
{
    /// <summary>Growing towards his largest.</summary>
    Swelling,

    /// <summary>Shrinking back through zero, which draws him inside out.</summary>
    Shrinking,

    /// <summary>Gone. A death that skips the alarm starts here.</summary>
    Exploded
}
