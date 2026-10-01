// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The three render view ports the game composes through, by the name each is registered under.
///     <c>GameEngine.RegisterScreenEffectViewPorts</c> creates them and everything that draws selects
///     one by name, so the names belong in one place for the same reason <see cref="Assets" />' do: a
///     mistyped one threw out of <c>ScreenEffectEngine</c> at the moment something tried to draw
///     through it, and now does not compile.
/// </summary>
internal static class ViewPorts
{
    /// <summary>
    ///     The playfield - the planet's screens, and everything walking about on them.
    /// </summary>
    public const string GameArea = "GameArea";

    /// <summary>
    ///     The status panel above the playfield.
    /// </summary>
    public const string StatusArea = "StatusArea";

    /// <summary>
    ///     The whole composition, which is what the full-screen presentation pages draw into.
    /// </summary>
    public const string EntireScreen = "EntireScreen";
}
