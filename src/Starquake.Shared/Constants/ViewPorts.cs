// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Constants;

/// <summary>
///     The three render view ports the game composes through, by the name each is registered under.
///     <c>GameEngine.RegisterScreenEffectViewPorts</c> creates them and everything that draws selects
///     one by name. Named here for the same reason as <see cref="Assets" />: a mistyped name fails to
///     compile instead of throwing from <c>ScreenEffectEngine</c> when something tries to draw.
/// </summary>
internal static class ViewPorts
{
    /// <summary>The playfield: the planet's screens, and everything walking about on them.</summary>
    public const string GameArea = "GameArea";

    /// <summary>The status panel above the playfield.</summary>
    public const string StatusArea = "StatusArea";

    /// <summary>The whole composition, which the full-screen presentation pages draw into.</summary>
    public const string EntireScreen = "EntireScreen";
}
