// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.Structure;

/// <summary>
///     What one screen tells the next when it hands over.
/// </summary>
/// <remarks>
///     This was an <c>object?</c> parameter carrying four unrelated meanings, told apart by a null
///     check and an unchecked cast: null meant "fade the new screen up", any non-null value meant
///     "do not", a boxed <c>bool</c> from an access booth also meant whether the code was accepted,
///     and a <c>HiScore</c> from the game over screen carried the score a name is typed against.
///     Two of the four were passing <c>true</c> or <c>false</c> purely to be non-null, which is why
///     the booth's genuine true-or-false was indistinguishable from them.
///     <para>
///         A closed hierarchy says all four out loud, and the compiler now rejects the fifth meaning
///         somebody would otherwise have added. The constructor is private so the four below are the
///         only ones there are.
///     </para>
/// </remarks>
internal abstract record ScreenChange
{
    private ScreenChange()
    {
    }

    /// <summary>
    ///     Nothing to pass on. The screen taking over fades itself up from black, which is the usual
    ///     way into a screen.
    /// </summary>
    public sealed record FadeIn : ScreenChange;

    /// <summary>
    ///     Hand over without fading. The screen is already on display and only its contents are
    ///     changing - stepping between screens, or starting a game on the screen the report left up.
    /// </summary>
    public sealed record KeepPicture : ScreenChange;

    /// <summary>
    ///     The player has just typed a code into an access booth, and whether it was accepted. The
    ///     main game screen flashes the booth either green or red to match.
    /// </summary>
    public sealed record AccessCode(bool Accepted) : ScreenChange;
}
