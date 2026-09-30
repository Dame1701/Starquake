// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.GameComponents;

/// <summary>
///     Which of the game's screens is being played, and where to find any of the others.
///     <para>
///         "Screen" means two unrelated things in this game and they used to sit side by side on
///         the engine under near-identical names. <c>GameScreens</c> is the planet - two hundred
///         and fifty-six screens of tiles the blob walks around. This is the other one: the mode
///         the game is in, one of <see cref="GameScreenType" />, and the component that draws it.
///         Eleven classes want the planet, five want this, and eight want both, which is why they
///         are two objects rather than the one the plan expected.
///     </para>
///     <para>
///         Switching screen is an assignment to <see cref="Current" /> from whichever screen has
///         decided it is finished - twenty-four places do it. Nothing here acts on the change; the
///         engine reads <see cref="Current" /> each frame and updates and draws whatever it names.
///     </para>
/// </summary>
public sealed class ScreenDirectory
{
    private readonly Dictionary<GameScreenType, IGameScreen> _screens = new();

    /// <summary>
    ///     The screen being played. Starts on the options screen, which is what the game boots
    ///     into.
    /// </summary>
    public GameScreenType Current { get; set; } = GameScreenType.Options;

    /// <summary>
    ///     Every screen registered, in the order they were added.
    /// </summary>
    public IEnumerable<IGameScreen> All => _screens.Values;

    /// <summary>
    ///     The component that draws the screen being played, or null if <see cref="Current" /> has
    ///     no component registered for it. Null rather than a throw because the engine's update and
    ///     draw run every frame regardless of what <see cref="Current" /> has been set to, and have
    ///     always simply skipped a screen they could not find.
    /// </summary>
    public IGameScreen? CurrentScreen => _screens.GetValueOrDefault(Current);

    /// <summary>
    ///     Registers the component that draws one screen. Registering the same screen twice throws,
    ///     as the dictionary underneath always did - two components claiming one screen means one of
    ///     them would never be drawn and nothing would say so.
    /// </summary>
    public void Add(GameScreenType gameScreen, IGameScreen screen) => _screens.Add(gameScreen, screen);

    /// <summary>
    ///     The component that draws one screen. Throws rather than returning null: a screen asking
    ///     for another by name has no way to carry on without it, and the alternative is a null
    ///     dereference somewhere further away from the mistake.
    /// </summary>
    public IGameScreen Get(GameScreenType gameScreen)
    {
        return !_screens.TryGetValue(gameScreen, out IGameScreen? screenToGet)
            ? throw new InvalidOperationException($"There is no '{gameScreen}' screen.")
            : screenToGet;
    }
}
