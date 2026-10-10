// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The clock the game runs on. <c>GameHost</c> turns a variable render loop into exactly this
///     many logic frames a second and the game moves everything by a fixed amount per frame, so a
///     frame is the game's unit of time and a second is only ever this many of them.
/// </summary>
/// <remarks>
///     Anything that waits should count frames through here rather than reading the wall clock.
///     <c>DateTime.Now</c> drifts against the frame rate, and being local time it can wait an extra
///     hour if a DST step or an NTP correction lands mid-wait.
/// </remarks>
internal static class LogicClock
{
    /// <summary>
    ///     How many logic frames make a second. 50 is the PAL rate the original was written for and
    ///     what the game runs at; 60 is NTSC and 20% faster. <c>GameHost</c> paces itself from this
    ///     and is the only thing that may change it.
    /// </summary>
    public const int FramesPerSecond = 50;

    /// <summary>How many frames a given number of seconds is.</summary>
    public static int Seconds(double seconds) => (int)(seconds * FramesPerSecond);
}
