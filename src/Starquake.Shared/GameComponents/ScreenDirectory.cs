// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Core;
using Starquake.GameComponents.GameScreens;
using Starquake.GameComponents.PresentationScreens;

namespace Starquake.GameComponents;

/// <summary>
///     Which of the game's screens is being played, and where to find any of the others.
///     <para>
///         "Screen" means two unrelated things in this game. <c>GameScreens</c> is the planet, 256
///         screens of tiles Blob walks around. This is the other: the mode the game is in, one of
///         <see cref="GameScreenType" />, and the component that draws it. They are two objects
///         because most classes want one or the other rather than both.
///     </para>
///     <para>
///         Switching screen is an assignment to <see cref="Current" /> from whichever screen decides
///         it is finished. Nothing here acts on the change: the engine reads
///         <see cref="CurrentScreen" /> each frame and updates and draws whatever it names.
///     </para>
///     <para>
///         The ten properties below are typed, and deliberately so. A screen handing over names the
///         one screen it hands to, and what it says to it is particular to that screen: the main game
///         screen's fade level, the high score screen's score to type a name against, the access
///         booth's door to put Blob through. A single <c>Get</c> returning <see cref="IGameScreen" />
///         would force all of that onto one interface, which is how empty and throwing members end up
///         on screens that have no such thing. Nothing casts.
///     </para>
/// </summary>
internal sealed class ScreenDirectory
{
    /// <summary>
    ///     The screens Blob is on the planet for. Everything else, such as the options, the high
    ///     scores and the game over screen, is the game talking about itself rather than being played.
    /// </summary>
    private static readonly GameScreenType[] InPlayScreens =
    [
        GameScreenType.Cheops,
        GameScreenType.MainGame,
        GameScreenType.PlanetCore,
        GameScreenType.Teleporter,
        GameScreenType.AccessBooth
    ];

    private readonly Dictionary<GameScreenType, IGameScreen> _screens = new();

    private AccessBoothScreen? _accessBooth;
    private CheopsScreen? _cheops;
    private ComputerReportScreen? _computerReport;
    private GameCompleteScreen? _gameComplete;
    private GameOverScreen? _gameOver;
    private HiScoreScreen? _hiScores;
    private MainGameScreen? _mainGame;
    private OptionsScreen? _options;
    private PlanetCoreScreen? _planetCore;
    private TeleportScreen? _teleporter;

    /// <summary>The screen being played. Starts on the options screen, which the game boots into.</summary>
    public GameScreenType Current { get; set; } = GameScreenType.Options;

    /// <summary>
    ///     The component that draws the screen being played, or null if <see cref="Current" /> has no
    ///     component registered. Null rather than a throw, because the engine's update and draw run
    ///     every frame whatever <see cref="Current" /> is set to and simply skip a screen they cannot
    ///     find.
    /// </summary>
    public IGameScreen? CurrentScreen => _screens.GetValueOrDefault(Current);

    /// <summary>
    ///     Whether the game is being played rather than talking about itself. The status panel is drawn
    ///     and updated only while it is, as are the global hotkeys and the drain on Blob's energy.
    /// </summary>
    public bool InPlay => Array.IndexOf(InPlayScreens, Current) >= 0;

    public AccessBoothScreen AccessBooth => Registered(_accessBooth, GameScreenType.AccessBooth);

    public CheopsScreen Cheops => Registered(_cheops, GameScreenType.Cheops);

    public ComputerReportScreen ComputerReport => Registered(_computerReport, GameScreenType.ComputerReport);

    public GameCompleteScreen GameComplete => Registered(_gameComplete, GameScreenType.GameComplete);

    public GameOverScreen GameOver => Registered(_gameOver, GameScreenType.GameOver);

    public HiScoreScreen HiScores => Registered(_hiScores, GameScreenType.HiScores);

    public MainGameScreen MainGame => Registered(_mainGame, GameScreenType.MainGame);

    public OptionsScreen Options => Registered(_options, GameScreenType.Options);

    public PlanetCoreScreen PlanetCore => Registered(_planetCore, GameScreenType.PlanetCore);

    public TeleportScreen Teleporter => Registered(_teleporter, GameScreenType.Teleporter);

    /// <summary>
    ///     Registers the component that draws one screen. The primitive the ten typed overloads below
    ///     go through: they add the screen exactly as this does and also record it under its own type,
    ///     so screens handing over to it get it back typed.
    ///     <para>
    ///         Registering the same screen twice throws. Two components claiming one screen means one
    ///         of them would never be drawn and nothing would say so.
    ///     </para>
    /// </summary>
    public void Add(GameScreenType gameScreen, IGameScreen screen) => _screens.Add(gameScreen, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(AccessBoothScreen screen) => _accessBooth = Added(GameScreenType.AccessBooth, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(CheopsScreen screen) => _cheops = Added(GameScreenType.Cheops, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(ComputerReportScreen screen) => _computerReport = Added(GameScreenType.ComputerReport, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(GameCompleteScreen screen) => _gameComplete = Added(GameScreenType.GameComplete, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(GameOverScreen screen) => _gameOver = Added(GameScreenType.GameOver, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(HiScoreScreen screen) => _hiScores = Added(GameScreenType.HiScores, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(MainGameScreen screen) => _mainGame = Added(GameScreenType.MainGame, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(OptionsScreen screen) => _options = Added(GameScreenType.Options, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(PlanetCoreScreen screen) => _planetCore = Added(GameScreenType.PlanetCore, screen);

    /// <inheritdoc cref="Add(GameScreenType,IGameScreen)" />
    public void Add(TeleportScreen screen) => _teleporter = Added(GameScreenType.Teleporter, screen);

    /// <summary>
    ///     Throws rather than returning null. A screen asking for another by name cannot carry on
    ///     without it, and the alternative is a null dereference further from the mistake.
    /// </summary>
    private static TScreen Registered<TScreen>(TScreen? screen, GameScreenType gameScreen)
        where TScreen : class, IGameScreen
        => screen ?? throw new InvalidOperationException($"There is no '{gameScreen}' screen.");

    /// <summary>
    ///     Registers a screen and hands it straight back, so a typed overload records it in its own
    ///     field only once the duplicate check has passed.
    /// </summary>
    private TScreen Added<TScreen>(GameScreenType gameScreen, TScreen screen)
        where TScreen : class, IGameScreen
    {
        Add(gameScreen, screen);

        return screen;
    }
}
