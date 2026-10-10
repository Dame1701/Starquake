// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The music, as heard rather than as played; see <c>MusicEngine</c> for what starts and stops
///     it.
/// </summary>
internal static class MusicSettings
{
    /// <summary>
    ///     How long a screen is up before its music starts, in logic frames. A screen arrives with
    ///     its own noise - a fade, a wipe, the sound of whatever got the player here - and a piece
    ///     landing on top of that reads as part of the hand-over rather than as the screen's music.
    /// </summary>
    public static readonly int StartDelayFrames = LogicClock.Seconds(0.5);
}
