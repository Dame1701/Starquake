// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     How much of a cathode ray tube is laid over the finished picture. F9 steps through these in
///     order and back to <see cref="None" />, so each press adds one thing and the fourth takes
///     the lot away.
/// </summary>
internal enum CrtMode
{
    /// <summary>
    ///     The picture as the game composed it, drawn straight to the back buffer with no filter
    ///     pass at all.
    /// </summary>
    None,

    /// <summary>
    ///     Scanlines, on a period locked to whole output pixels.
    /// </summary>
    Scanlines,

    /// <summary>
    ///     Scanlines, a phosphor grille and the beam's glow into its neighbours. Still flat, so
    ///     the picture lines up with the window.
    /// </summary>
    Phosphor,

    /// <summary>
    ///     All of that, bowed out like a real tube, with the corners falling away.
    /// </summary>
    Tube
}
