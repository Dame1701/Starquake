// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.GameComponents;
using Starquake.GameComponents.GameScreens;
using Starquake.GameComponents.PresentationScreens;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Tuning;
using Starquake.World;

namespace Starquake.Engines;

internal class GameEngine : StartableGameComponentBase, IDisposable
{
    /// <summary>
    ///     A percentage, complete. The loader reports progress as one, so this is what it reports when
    ///     there is nothing left to load.
    /// </summary>
    private const int FullyLoadedPercent = 100;

    /// <summary>Where the pause banner is drawn, in playfield coordinates.</summary>
    private const int PausedOverlayXPos = 400;

    /// <inheritdoc cref="PausedOverlayXPos" />
    private const int PausedOverlayYPos = 200;

    /// <summary>What shooting a nasty is worth.</summary>
    private const int ScoreForShootingNasty = 50;

    /// <summary>How many nasties can be on a screen at once.</summary>
    private const int NastiesPerScreen = 4;

    /// <summary>
    ///     The line in the bottom left naming what a hotkey has just switched to. Built here rather
    ///     than in <c>Initialise</c> because it depends on nothing, and a thing with no collaborators
    ///     cannot be built in the wrong phase.
    /// </summary>
    private readonly ModeAnnouncement _announcement = new();
    private readonly ContentManager _contentMgr;

    private readonly GraphicsDeviceManager _graphicsDeviceMgr;

    /// <summary>
    ///     Which of the two sets of artwork the game is drawn with, and the sheet names that go
    ///     with it. Switched by <c>GlobalHotkeys.SwitchGraphicsSetIfAsked</c>; read live by
    ///     everything that draws.
    /// </summary>
    private readonly GraphicsSet _graphicsSet = new();

    /// <summary>
    ///     How fast the game runs. The host's, which paces itself from it; held here only so F11 can
    ///     switch it and the preferences can record it.
    /// </summary>
    private readonly LogicRate _logicRate;

    /// <summary>Where the settings the function keys change are kept between runs.</summary>
    private readonly PreferencesStore _preferencesStore;

    /// <summary>
    ///     Closes the game. The host's <c>Exit</c>, held only to hand to
    ///     <see cref="GlobalHotkeys" />, because ending the process is the host's job rather than the
    ///     engine's.
    /// </summary>
    private readonly Action _quitGame;

    /// <summary>Writes a PNG of the back buffer when F5 has asked for one.</summary>
    private readonly Screenshots _screenshots = new();

    /// <summary>
    ///     The host's window. Held only to hand to <see cref="GlobalHotkeys" />, which drops the
    ///     window's border when going fullscreen. Nothing else in the engine touches it.
    /// </summary>
    private readonly GameWindow _window;
    public readonly ResourceManager ContentResourceManager;

    /// <summary>
    ///     How the composed picture is put on the screen: the transform onto the back buffer, the
    ///     colours behind it, and where the game's own screens sit on it.
    /// </summary>
    public readonly PresentationSettings Presentation = new();

    public readonly Random RandomNumberGenerator;
    /// <summary>
    ///     Which of the game's screens is showing, and where to find the others. Not to be confused
    ///     with <see cref="GameScreens" />, which is the planet.
    /// </summary>
    public readonly ScreenDirectory ScreenDirectory = new();

    /// <summary>Dissolves one set of artwork into the other when F8 switches between them.</summary>
    private ArtworkCrossfade _crossfade;

    private bool _disposed;

    /// <summary>
    ///     The game itself, once content has loaded. Null until then, which <see cref="Game" />
    ///     answers for.
    /// </summary>
    private RunningGame? _game;

    /// <summary>The four keys that work anywhere. Built once the game can act on them.</summary>
    private GlobalHotkeys _hotkeys;

    /// <summary>
    ///     The boot screen. Built before anything else, because it is what is on screen while
    ///     everything else loads.
    /// </summary>
    private LoadingScreen _loadingScreen;

    private bool _particleTestTrigger;

    /// <summary>
    ///     Catches the composed picture and hands it to the back buffer, through the cathode ray tube
    ///     when one is asked for.
    /// </summary>
    private ScreenPresenter _presenter;

    private ScreenEffectEngine _screenEffectEngine;

    /// <summary>The debug keys in a debug build. Null in a release one, which never builds it.</summary>
    private TestModeKeys? _testModeKeys;

    /// <summary>
    ///     The panel above the playfield. Drawn and updated only while
    ///     <see cref="ScreenDirectory.InPlay" />.
    /// </summary>
    public StatusPanel BlobStatusPanel;

    /// <summary>The nine elements the core is missing, and what Blob has done about them.</summary>
    public CoreElements Elements;

    /// <summary>The two bitmap fonts everything that writes text draws through.</summary>
    public BitmapFont Font1;

    /// <inheritdoc cref="Font1" />
    public BitmapFont Font2;

    /// <summary>
    ///     Draws plain filled rectangles, for the screens that frame something with a block of
    ///     colour rather than a sprite.
    /// </summary>
    public Shapes ShapeDrawer;

    /// <summary>
    ///     The player's score, lives and supplies. Held beside the panel that draws them rather than
    ///     inside it, because most of the game reads and writes these where only one class draws them.
    /// </summary>
    public PlayerStats Stats;

    public GameEngine(
        GraphicsDeviceManager graphics,
        ContentManager content,
        ToggleKeys input,
        GameWindow window,
        LogicRate logicRate,
        Preferences savedPreferences,
        PreferencesStore preferencesStore,
        Action quitGame)
    {
        Input = input;
        _graphicsDeviceMgr = graphics;
        _contentMgr = content;
        _window = window;
        _logicRate = logicRate;
        _preferencesStore = preferencesStore;
        _quitGame = quitGame;

        ApplyPreferences(savedPreferences);

        // The one generator the game logic draws from, so a game can be reproduced from its seed.
        // The sound engine deliberately does not get it: its only draw happens on a thread pool
        // timer callback, and System.Random is not thread-safe.
        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);

        ContentResourceManager = new ResourceManager(content, graphics, _graphicsSet);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;

        DefineContent();
        Initialise();

        // Here rather than in ApplyPreferences, which runs before Initialise has built the session.
        Session.AnalogueWalk = savedPreferences.AnalogueWalk;
    }

    /// <summary>
    ///     The frame's input, read once by <c>GameHost</c> and shared by everything that acts on it,
    ///     so no two readers disagree about what is held down.
    /// </summary>
    public ToggleKeys Input { get; }

    /// <summary>
    ///     The game once its content has loaded, or a thrown exception saying it has not. The one place
    ///     in the engine that can answer "not yet", so the nine things behind it need no guards of
    ///     their own. Nothing outside the boot screen runs before the game exists.
    /// </summary>
    private RunningGame Game => NotBeforeContentLoads(_game);

    /// <inheritdoc cref="RunningGame.GameScreens" />
    private GameScreenCollection GameScreens => Game.GameScreens;

    /// <inheritdoc cref="RunningGame.Player" />
    private Blob Player => Game.Player;


    /// <inheritdoc cref="RunningGame.IntroSequence" />
    private IntroSequence IntroSequence => Game.IntroSequence;


    /// <summary>
    ///     The game being played: score, lives, pause and controls. Built by <see cref="Initialise" />,
    ///     which the constructor calls, so it lives as long as the engine. Not one of the nine above,
    ///     because the options screen can change the controls before any content has loaded.
    /// </summary>
    public GameSession Session { get; private set; }

    /// <summary>
    ///     The screen effects. Built by <see cref="Initialise" /> like the session and for the same
    ///     reason: it exists before any content does. Its view ports are registered later, because
    ///     those need dimensions only the loaded content knows.
    /// </summary>
    public ScreenEffectEngine ScreenEffectEngine => _screenEffectEngine;

    /// <summary>
    ///     Called by the host on the way out. The sound engine's timer is the only thing the engine
    ///     owns that must be put down by hand: every texture, font and sound belongs to the content
    ///     manager, and every sprite batch and render target to the graphics device, both of which the
    ///     game disposes itself.
    /// </summary>
    /// <remarks>
    ///     Called from <c>GameHost.Dispose</c>, the only way out of the game. The guard makes a second
    ///     call do nothing, which is what Dispose is required to do.
    ///     <para>
    ///         No finaliser, and so no <c>GC.SuppressFinalize</c>. This holds no unmanaged handle of
    ///         its own, the timer being a managed object with its own finaliser, so a finaliser here
    ///         would be one more thing to get wrong for no benefit.
    ///     </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ContentResourceManager.GetSoundFxEngine().Dispose();
    }

    /// <summary>
    ///     The background rumble follows the pause. Subscribed only once the session is built, by
    ///     which time the resource manager exists, so no null check is needed.
    /// </summary>
    private void Session_OnPausedChanged(bool paused)
    {
        if (paused)
        {
            ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);
        }
        else
        {
            ContentResourceManager.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
        }
    }

    private static T NotBeforeContentLoads<T>(T? component, [CallerMemberName] string name = "")
        where T : class
        => component ?? throw new InvalidOperationException(
            $"'{name}' does not exist until the game's content has loaded.");

    private void Nasties_OnNastyInteractionWithBlob(NastyInteractionType interactionType, NastyType nastyType)
    {
        if (interactionType == NastyInteractionType.BlobShot)
        {
            Stats.PlayerScore += ScoreForShootingNasty;
        }
        else if (interactionType == NastyInteractionType.HitBlob)
        {
            if (nastyType == NastyType.Spiky)
            {
                Player.Kill(false, false);
            }
            else if (nastyType == NastyType.Conveyor)
            {
                Player.Kill(true, false);
            }
            else
            {
                Stats.EnergyLevel -= 1;
            }
        }
    }

    private void Electricity_OnBlobStruckByElectricity(object? sender, EventArgs e)
    {
        Player.Kill(false, false);
    }

    private void Player_OnScreenChanged(GameScreenType triggerScreen)
    {
        ScreenDirectory.MainGame.PerformPostScreenChangeActions(triggerScreen, new ScreenChange.FadeIn());
    }

    /// <summary>
    ///     Knocks the picture about for something that has exploded: a machine Blob has shot, or Blob
    ///     himself.
    /// </summary>
    /// <remarks>
    ///     Handed out as a callback rather than letting a nasty or Blob hold the thing that shakes the
    ///     screen, which belongs to the main game screen.
    /// </remarks>
    private void JoltTheScreen() => ScreenDirectory.MainGame.JoltScreen();

    private void Player_OnBlobActionRaised(BlobAction action, object? data)
        => Stats.BlobHasPerformedAnAction(action, data);

    [MemberNotNull(nameof(BlobStatusPanel), nameof(Stats), nameof(Elements), nameof(_hotkeys),
        nameof(ShapeDrawer), nameof(Session), nameof(_screenEffectEngine))]
    private void Initialise()
    {
        Stats = new PlayerStats(ContentResourceManager.GetSoundFxEngine());
        BlobStatusPanel = new StatusPanel(ContentResourceManager, _graphicsSet, ScreenDirectory, RandomNumberGenerator, Stats);

        Session = new GameSession();
        Session.OnPausedChanged += Session_OnPausedChanged;

        _hotkeys = new GlobalHotkeys(
            Input,
            ScreenDirectory,
            Session,
            Presentation,
            _graphicsSet,
            _graphicsDeviceMgr,
            _window,
            ContentResourceManager,
            _announcement,
            _screenshots,
            _logicRate,
            ArtworkSwapped,
            SavePreferences,
            _quitGame);

        Elements = new CoreElements(Stats, ContentResourceManager, _graphicsSet, RandomNumberGenerator);
        ShapeDrawer = new Shapes(_graphicsDeviceMgr.GraphicsDevice);

        // Built here rather than with its view ports, so that anything constructed from here on -
        // Blob's jump platforms, for one - can be handed it instead of reaching for it later.
        _screenEffectEngine = new ScreenEffectEngine(_graphicsDeviceMgr, Presentation, ContentResourceManager);
    }

    /// <summary>
    ///     Loads what is needed to draw the loading screen, names every other piece of content for the
    ///     background load that <see cref="LoadContent" /> starts, and builds the two components that have
    ///     to exist before any of it arrives.
    /// </summary>
    /// <remarks>
    ///     The naming is <see cref="ContentManifest" />, which is also where the note about keeping
    ///     the registration order exactly as it was found now lives.
    /// </remarks>
    [MemberNotNull(nameof(_loadingScreen), nameof(_presenter), nameof(_crossfade), nameof(Font1),
        nameof(Font2))]
    private void DefineContent()
    {
        _loadingScreen = new LoadingScreen(_graphicsDeviceMgr, _contentMgr, Presentation, Input);

        Status = GameEngineStatus.StartingGraphicsLoaded;

        DefineFonts();

        ContentManifest.RegisterAll(ContentResourceManager);
    }

    /// <summary>
    ///     Puts the game back the way the player left it. Called before anything is loaded or drawn,
    ///     so the artwork is chosen before the first sheet is asked for rather than swapped under it.
    ///     The rate needs nothing here: the host built it from the same preferences.
    /// </summary>
    private void ApplyPreferences(Preferences preferences)
    {
        if (preferences.OriginalGraphics != _graphicsSet.IsOriginal)
        {
            _graphicsSet.Switch();
        }

        Presentation.CrtMode = preferences.CrtMode;
        Presentation.DrawBackgrounds = preferences.Backdrops;
    }

    /// <summary>
    ///     Writes down the settings the function keys change, as they stand. Called by whichever key
    ///     has just changed one, so the file is always what the player last chose.
    /// </summary>
    private void SavePreferences() => _preferencesStore.Write(new Preferences(
        Presentation.CrtMode,
        _graphicsSet.IsOriginal,
        Presentation.DrawBackgrounds,
        _logicRate.Hertz,
        Session.AnalogueWalk));

    /// <summary>
    ///     The artwork has just been swapped, so whatever is drawing has to pick up the new sheets.
    ///     Only the main game screen caches anything derived from them, and only once the intro has
    ///     finished flying the ship in.
    /// </summary>
    private void ArtworkSwapped()
    {
        _crossfade.Start();

        if (IntroSequence.SequenceStage is IntroSequenceStage.Finished or IntroSequenceStage.EndPause)
        {
            ScreenDirectory.MainGame.SpritesChanged();
        }
    }

    /// <summary>
    ///     The two things the engine draws itself: the fonts every screen writes with, and the
    ///     cathode ray tube the finished picture goes through. The loading screen loads its own.
    /// </summary>
    [MemberNotNull(nameof(Font1), nameof(Font2), nameof(_presenter), nameof(_crossfade))]
    private void DefineFonts()
    {
        Font1 = new BitmapFont(_contentMgr.Load<SpriteFont>(Assets.Fonts.ElektoraSmall), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, Colours.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        Font2 = new BitmapFont(_contentMgr.Load<SpriteFont>(Assets.Fonts.ElektoraMedium), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, Colours.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        _presenter = new ScreenPresenter(_graphicsDeviceMgr, ContentResourceManager, Presentation);
        _crossfade = new ArtworkCrossfade(_graphicsDeviceMgr);
    }

    /// <summary>
    ///     Handles status changes from game components. Everything the game cannot build until its
    ///     content exists is built here, on the one change that says the content has arrived.
    /// </summary>
    private void ContentResourceManager_OnGameComponentStatusChanged(string message, int percentComplete, GameEngineStatus status)
    {
        if (status != GameEngineStatus.Started)
        {
            UpdateStatus(message, percentComplete, status);
            return;
        }

        _game = BuildComponentsNeedingLoadedContent();

        // Everything from here reads the game through the engine's own accessors, which answer now
        // that _game is assigned.
        RegisterColourAnimations();
        BuildGameScreens();
        RegisterScreenEffectViewPorts();
        BuildTestModeKeys();

        ContentResourceManager.Initialise();

        UpdateStatus("Content loaded", FullyLoadedPercent, GameEngineStatus.StartGame);
    }

    /// <summary>
    ///     Builds the nine things needing loaded content, in the order they need each other: the planet
    ///     first, then what reads it, then Blob, then what chases him.
    /// </summary>
    /// <remarks>
    ///     Each stays a local until the last is built, because the engine's accessors answer "not yet"
    ///     until <see cref="_game" /> is assigned. Nine fields filled one at a time would each be
    ///     readable half-built.
    /// </remarks>
    private RunningGame BuildComponentsNeedingLoadedContent()
    {
        // The planet. Built here rather than in the constructor, because the screen data and collision
        // map it needs are resources that do not exist until content loads.
        var gameScreens = new GameScreenCollection(
            ContentResourceManager,
            _graphicsSet,
            RandomNumberGenerator,
            ShapeDrawer,
            Font1);

        // Tunnel lift animations. After the planet, which it reads the lift shafts from.
        var tunnelLiftArrows = new TunnelLiftArrows(ContentResourceManager, gameScreens, _graphicsSet);

        // Blob's death throes. After the planet, which each thrown-off fragment bounces around.
        var blobKiller = new BlobDeath(ContentResourceManager, gameScreens, _graphicsSet, RandomNumberGenerator);

        // Its fragments each take a particle effect instance, so this cannot run until the resources
        // are loaded, which is now rather than when the load was started.
        blobKiller.LoadContent();

        // The breakable bridges, before Blob, who breaks them. Each bridge composes itself into a
        // render target and asks the screen effect engine for a view port only when it draws, so it
        // needs the engine to exist but not its view ports.
        var breakableBridges = new BreakableBridges(
            ContentResourceManager,
            gameScreens,
            _graphicsDeviceMgr,
            _graphicsSet,
            RandomNumberGenerator,
            ScreenEffectEngine);

        // Blob. After the planet he walks around and the death animation he triggers, neither of
        // which needs Blob back.
        var player = new Blob(
            ContentResourceManager,
            gameScreens,
            _graphicsDeviceMgr,
            _graphicsSet,
            ScreenDirectory,
            ScreenEffectEngine,
            Stats,
            Session,
            blobKiller,
            breakableBridges,
            RandomNumberGenerator,
            Input,
            JoltTheScreen);
        player.OnScreenChanged += Player_OnScreenChanged;
        player.OnActionRaised += Player_OnBlobActionRaised;

        // Subscribed here rather than with the session's other event in Initialise, so there is no
        // window where the keys can change with no Blob to tell. The options screen can be used
        // before content loads, and Blob reads the current set off the session as he is built.
        Session.OnPlayerKeysChanged += keys => player.Control.PlayerKeySet = keys;

        // The intro flies Blob's ship in over the planet and blows it up, so it comes after both the
        // planet and the death animation.
        var introSequence = new IntroSequence(
            ContentResourceManager,
            Font2,
            gameScreens,
            _graphicsSet,
            Presentation,
            ScreenDirectory,
            ScreenEffectEngine,
            blobKiller,
            RandomNumberGenerator);

        Presentation.ScreenXOffset = (_graphicsDeviceMgr.PreferredBackBufferWidth - gameScreens.ScreenWidth) / 2;
        Presentation.ScreenYOffset = 10;

        // The conveyor nasties and the electricity engine. Both chase Blob and read the planet, so
        // both are built here rather than in Initialise, where neither could exist yet.
        var conveyorNasties = new ConveyorNasties(
            ContentResourceManager,
            gameScreens,
            player,
            RandomNumberGenerator,
            JoltTheScreen);
        conveyorNasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;
        conveyorNasties.LoadContent();

        var electricity = new Electricity(
            ContentResourceManager,
            gameScreens,
            _graphicsSet,
            player,
            RandomNumberGenerator,
            conveyorNasties);
        electricity.OnBlobStruckByElectricity += Electricity_OnBlobStruckByElectricity;
        player.OnDiedAndRespawned += electricity.Reset;

        // The nasties last of the three: they chase Blob, read the planet, and ask the other two
        // where the conveyors and the electricity are.
        var nasties = new Nasties(
            ContentResourceManager,
            gameScreens,
            _graphicsSet,
            player,
            RandomNumberGenerator,
            conveyorNasties,
            electricity,
            JoltTheScreen,
            NastiesPerScreen,
            Input);
        nasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        return new RunningGame
        {
            GameScreens = gameScreens,
            Player = player,
            BreakableBridges = breakableBridges,
            TunnelLiftArrows = tunnelLiftArrows,
            IntroSequence = introSequence,
            Nasties = nasties,
            BlobKiller = blobKiller,
            ConveyorNasties = conveyorNasties,
            Electricity = electricity
        };
    }

    /// <summary>
    ///     Registers the tiles that pulse. Which ones and how is level design rather than the engine's
    ///     decision, so the table belongs to <see cref="ColourAnimatedTiles" />.
    /// </summary>
    private void RegisterColourAnimations()
    {
        foreach (ColourFader animation in ColourAnimatedTiles.All())
        {
            GameScreens.ColourAnimations.RegisterTile(animation);
        }
    }

    /// <summary>
    ///     Builds the ten screens and registers each with <see cref="ScreenDirectory" />, which holds
    ///     them from here on.
    /// </summary>
    private void BuildGameScreens()
    {
        GameScreens.Initialise();

        // The main screen is handed the running game and the standing collaborators rather than the
        // engine they hang off, so nothing needs the engine's forwarding properties to be public.
        var mainScreen = new MainGameScreen(Game, new GameplayContext
        {
            Resources = ContentResourceManager,
            ScreenEffects = ScreenEffectEngine,
            Input = Input,
            Presentation = Presentation,
            ScreenDirectory = ScreenDirectory,
            Session = Session,
            Stats = Stats,
            Elements = Elements,
            RandomNumberGenerator = RandomNumberGenerator
        });

        ScreenDirectory.Add(new OptionsScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            Session,
            Input));
        ScreenDirectory.Add(new ComputerReportScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            RandomNumberGenerator,
            Input));
        ScreenDirectory.Add(new HiScoreScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            Stats,
            Input));
        ScreenDirectory.Add(new GameOverScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            Stats,
            Elements,
            GameScreens,
            Input));
        ScreenDirectory.Add(new GameCompleteScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            Elements,
            Input));

        ScreenDirectory.Add(mainScreen);
        ScreenDirectory.Add(new TeleportScreen(
            ContentResourceManager,
            Font2,
            GameScreens,
            _graphicsSet,
            Player,
            Presentation,
            RandomNumberGenerator,
            ScreenDirectory,
            ShapeDrawer,
            Input));
        ScreenDirectory.Add(new PlanetCoreScreen(
            ContentResourceManager,
            GameScreens,
            _graphicsSet,
            ScreenDirectory,
            Player,
            RandomNumberGenerator,
            Elements,
            ShapeDrawer,
            ScreenEffectEngine,
            BlobStatusPanel));
        ScreenDirectory.Add(new AccessBoothScreen(
            BlobStatusPanel,
            ContentResourceManager,
            Font2,
            GameScreens,
            _graphicsSet,
            Player,
            RandomNumberGenerator,
            ScreenDirectory,
            ShapeDrawer));
        ScreenDirectory.Add(new CheopsScreen(
            BlobStatusPanel,
            ContentResourceManager,
            Elements,
            Font2,
            GameScreens,
            _graphicsSet,
            Player,
            RandomNumberGenerator,
            ScreenDirectory,
            ShapeDrawer,
            Input));

        // Only two of the ten screens have content of their own to load, so they are named rather
        // than reached by downcasting all ten to a base class the other eight implement as a no-op.
        ScreenDirectory.Options.LoadContent();
        ScreenDirectory.MainGame.LoadContent();
    }

    /// <summary>
    ///     The view ports the screen effects draw through. Separate from building the effect engine in
    ///     <see cref="Initialise" />, because the engine needs only a graphics device where a view
    ///     port needs the status panel's height and the planet's dimensions, which are not known until
    ///     content has loaded.
    /// </summary>
    private void RegisterScreenEffectViewPorts()
    {
        ScreenEffectEngine.RegisterViewPort(
            ViewPorts.GameArea,
            Presentation.ScreenXOffset,
            Presentation.ScreenYOffset + BlobStatusPanel.Height + 10,
            GameScreens.ScreenWidth,
            GameScreens.ScreenHeight);

        ScreenEffectEngine.RegisterViewPort(
            ViewPorts.StatusArea,
            Presentation.ScreenXOffset + 2,
            Presentation.ScreenYOffset,
            BlobStatusPanel.Width,
            BlobStatusPanel.Height);

        ScreenEffectEngine.RegisterViewPort(
            ViewPorts.EntireScreen,
            0,
            0,
            CompositionSize.Width,
            CompositionSize.Height);
    }

    /// <summary>
    ///     Starts the content loading and returns at once, since
    ///     <see cref="ResourceManager.LoadContent" /> hands the work to a background thread. Nothing
    ///     needing loaded content may be touched from here: it is all built in
    ///     <see cref="ContentResourceManager_OnGameComponentStatusChanged" /> once the load reports it
    ///     has finished.
    /// </summary>
    private void LoadContent() => ContentResourceManager.LoadContent();

    public void Update(GameTime gameTime)
    {
        // Anything the background content load threw comes out here, on this thread, rather than being
        // lost on the one that threw it.
        ContentResourceManager.ThrowIfLoadFailed();

        // The background rumble's timer runs on the thread pool and only raises a flag. This is
        // where it is acted on, because the sound effect instances belong to this thread.
        ContentResourceManager.GetSoundFxEngine().Update();

        if (Input.GetPressedKey() == Keys.F9)
        {
            Presentation.CrtMode = CrtSettings.Next(Presentation.CrtMode);

            _announcement.Show(CrtSettings.NameOf(Presentation.CrtMode));
            SavePreferences();
        }

        // Stepped here rather than in UpdateRunningGame, so the message also fades on the menus. F9
        // works everywhere, and a message left frozen at full would never go.
        _announcement.Update();
        _crossfade.Update();

        if (Status == GameEngineStatus.StartingGraphicsLoaded)
        {
            if (_loadingScreen.FadeUp())
            {
                Status = GameEngineStatus.Starting;
                LoadContent();
            }
        }
        else if (Status == GameEngineStatus.ContentLoaded)
        {
            // The hop back onto the main thread. The loading task raises ContentLoaded from the
            // thread pool, and everything BuildComponentsNeedingLoadedContent builds must be built
            // here.
            if (_loadingScreen.WaitForAKey())
            {
                ContentResourceManager_OnGameComponentStatusChanged(
                    "Content loaded",
                    FullyLoadedPercent,
                    GameEngineStatus.Started);
            }
        }
        else if (Status == GameEngineStatus.StartGame)
        {
            UpdateRunningGame(gameTime);
        }
    }

    private void UpdateRunningGame(GameTime gameTime)
    {
        // Before the pause, and outside the screen's own update, because the music belongs to the
        // screen rather than to what is happening on it: it follows a hand-over on the frame the
        // hand-over happens, whichever of the screens made it.
        ContentResourceManager.GetMusicEngine().FollowTheScreen(ScreenDirectory.Current);

        _hotkeys.Update();

        HandleTestModeKeys();

        if (!Session.Paused)
        {
            UpdateActiveScreen(gameTime);
        }
    }

    /// <summary>
    ///     The debug keys, which exist only in a debug build. Both this and
    ///     <see cref="BuildTestModeKeys" /> are removed at the call site by
    ///     <see cref="ConditionalAttribute" />, so a release build has no path to them.
    /// </summary>
    [Conditional("DEBUG")]
    private void HandleTestModeKeys()
        => _testModeKeys?.Update();

    /// <inheritdoc cref="HandleTestModeKeys" />
    [Conditional("DEBUG")]
    private void BuildTestModeKeys()
        => _testModeKeys = new TestModeKeys(
            Input, Player, Stats, ScreenDirectory, GameScreens, Elements);

    private void UpdateActiveScreen(GameTime gameTime)
    {
        IGameScreen? screenToUpdate = ScreenDirectory.CurrentScreen;

        UpdateStatusPanel();

        if (screenToUpdate != null)
        {
            screenToUpdate.Update(gameTime);
            UpdateScreensLeftRunningOffScreen(screenToUpdate, gameTime);
        }

        if (!_particleTestTrigger)
        {
            _particleTestTrigger = true;
        }
    }

    private void UpdateStatusPanel()
    {
        if (!ScreenDirectory.InPlay)
        {
            return;
        }

        BlobInstruction instruction = BlobStatusPanel.Update(
            Player.Carrying,
            Player.State,
            !IntroSequence.SequenceRunning || IntroSequence.SequenceStage == IntroSequenceStage.EndPause);

        switch (instruction)
        {
            case BlobInstruction.Overload:
            {
                Player.Kill(false, true);
                break;
            }
            case BlobInstruction.Revive:
            {
                Player.State = BlobState.Normal;
                break;
            }
            case BlobInstruction.EndTheGame:
            {
                Player.State = BlobState.GameOver;
                break;
            }
        }
    }

    /// <summary>
    ///     Keeps the planet core's guardians and particle effects moving while Blob is away from it,
    ///     as long as he is on the screen approaching it.
    /// </summary>
    private void UpdateScreensLeftRunningOffScreen(IGameScreen screenToUpdate, GameTime gameTime)
    {
        if (screenToUpdate == ScreenDirectory.PlanetCore)
        {
            return;
        }

        // The core's exit screen, so the core is still working when Blob walks back in. Read off the
        // map rather than written down, because the author chooses where the core is.
        if (GameScreens.ScreenNumber == GameScreens.CoreExit.ScreenNumber)
        {
            ScreenDirectory.PlanetCore.UpdateOffScreen(gameTime);
        }

    }

    private void CalculatePresentationTransform()
    {
        // The back buffer, not GraphicsDevice.Viewport. The boot screen parks the viewport at the
        // composition size and the render viewports move it around as they please.
        PresentationParameters? backBuffer = _graphicsDeviceMgr.GraphicsDevice.PresentationParameters;

        Presentation.Recalculate(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
    }

    /// <param name="sinceLastDraw">
    ///     How long ago the last frame went out, which only the phosphor's decay reads. Real elapsed
    ///     time rather than a logic frame, because this is the one thing in the game that is properly
    ///     a property of the display's rate rather than the game's.
    /// </param>
    public void Draw(TimeSpan sinceLastDraw)
    {
        CalculatePresentationTransform();

        // Before the clear, because what the crossfade wants is what is still in the frame: the last
        // picture drawn with the artwork being left behind.
        _crossfade.HoldThePictureBeingLeft(_presenter.EnsureFrame());

        RenderTarget2D caughtFrame = _presenter.BeginFrame();

        if (Status == GameEngineStatus.StartGame)
        {
            if (ScreenDirectory.InPlay)
            {
                ScreenEffectEngine.SelectRenderViewPort(ViewPorts.StatusArea);
                BlobStatusPanel.Draw();
            }

            ScreenEffectEngine.SelectRenderViewPort(ViewPorts.GameArea);
            _graphicsDeviceMgr.GraphicsDevice.Clear(Presentation.ScreenClearColour);

            IGameScreen? screenToDraw = ScreenDirectory.CurrentScreen;
            screenToDraw?.Draw();

            if (Session.Paused)
            {
                Overlays.Paused(PausedOverlayXPos, PausedOverlayYPos, ShapeDrawer, Font2);
            }
        }
        else if (Status is GameEngineStatus.Starting or
                 GameEngineStatus.StartingGraphicsLoaded or
                 GameEngineStatus.ContentLoaded)
        {
            _loadingScreen.Draw(PercentComplete, FullyLoadedPercent);
        }

        _screenEffectEngine.Draw(caughtFrame);

        // What is left of the artwork being left behind, over the artwork arriving.
        _crossfade.Draw();

        // Over the finished picture but before the tube, so the message is part of what the tube
        // shows rather than something laid on the glass.
        _announcement.Draw(Font2, Presentation.Transform);

        _presenter.Present(sinceLastDraw);

        // After the picture has reached the buffer and after the announcement was drawn, so the
        // shot is the finished frame and the message about it lands in the next one.
        if (_screenshots.SaveIfAsked(_graphicsDeviceMgr.GraphicsDevice) is { } saved)
        {
            _announcement.Show(saved ? "SCREENSHOT SAVED" : "SCREENSHOT FAILED");
        }
    }
}
