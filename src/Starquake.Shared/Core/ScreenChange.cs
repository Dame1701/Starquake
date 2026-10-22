// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

/// <summary>
///     What one screen tells the next when it hands over.
/// </summary>
/// <remarks>
///     A closed hierarchy, so each meaning is named and the compiler rejects a fifth being added.
///     The constructor is private, so the cases below are the only ones there are.
/// </remarks>
internal abstract record ScreenChange
{
    private ScreenChange()
    {
    }

    /// <summary>
    ///     Nothing to pass on. The screen taking over fades itself up from black, which is the usual
    ///     way in.
    /// </summary>
    public sealed record FadeIn : ScreenChange;

    /// <summary>
    ///     Hand over without fading. The screen is already on display and only its contents change,
    ///     as when stepping between screens or starting a game on the screen the report left up.
    /// </summary>
    public sealed record KeepPicture : ScreenChange;

    /// <summary>
    ///     A code has just been typed into an access booth, and whether it was accepted. The main
    ///     game screen flashes the booth green or red to match.
    /// </summary>
    public sealed record AccessCode(bool Accepted) : ScreenChange;
}
