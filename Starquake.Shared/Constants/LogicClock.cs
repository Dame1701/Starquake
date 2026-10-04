// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The clock the game actually runs on. <c>GameHost</c> turns a variable render loop into
///     exactly this many logic frames a second and the game moves everything by a fixed amount per
///     frame, so a frame is the game's unit of time and a second is only ever this many of them.
///     <para>
///         The rate lived on <c>GameHost</c> alone, which is why the few places that wanted to
///         wait a second reached for <c>DateTime.Now</c> instead and drifted against the frame
///         rate they were drawn at - as well as paying a timezone conversion inside the update
///         path and, because <c>DateTime.Now</c> is local time, standing to wait an hour if a DST
///         step or an NTP correction landed mid-wait.
///     </para>
/// </summary>
internal static class LogicClock
{
    /// <summary>
    ///     How many logic frames make a second. 50 is the PAL rate the original was written for and
    ///     is what the game runs at; 60 is the NTSC speed and 20% faster. <c>GameHost</c> paces
    ///     itself from this and is the only thing that may change it.
    /// </summary>
    public const int FramesPerSecond = 50;

    /// <summary>
    ///     How many frames a given number of seconds is.
    /// </summary>
    public static int Seconds(double seconds) => (int)(seconds * FramesPerSecond);
}
