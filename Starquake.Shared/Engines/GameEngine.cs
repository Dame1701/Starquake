// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
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
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.GameComponents;
using Starquake.GameComponents.GameScreens;
using Starquake.GameComponents.PresentationScreens;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Engines;

internal class GameEngine : StartableGameComponentBase, IDisposable
{
    /// <summary>
    ///     A percentage, complete. The loader reports its progress as one, so this is what it reports
    ///     when there is nothing left to load.
    /// </summary>
    private const int FullyLoadedPercent = 100;

    /// <summary>
    ///     Where the pause banner is drawn, in playfield coordinates.
    /// </summary>
    private const int PausedOverlayXPos = 400;

    /// <inheritdoc cref="PausedOverlayXPos" />
    private const int PausedOverlayYPos = 200;

    /// <summary>
    ///     What shooting a nasty is worth.
    /// </summary>
    private const int ScoreForShootingNasty = 50;

    /// <summary>
    ///     How many nasties can be on a screen at once.
    /// </summary>
    private const int NastiesPerScreen = 4;
    private readonly ContentManager _contentMgr;

    private readonly GraphicsDeviceManager _graphicsDeviceMgr;

    /// <summary>
    ///     Which of the two sets of artwork the game is drawn with, and the sheet names that go
    ///     with it. Switched by <c>GlobalHotkeys.SwitchGraphicsSetIfAsked</c>; read live by
    ///     everything that draws.
    /// </summary>
    private readonly GraphicsSet _graphicsSet = new();

    /// <summary>
    ///     Closes the game. The host's <c>Exit</c>, held only to hand to
    ///     <see cref="GlobalHotkeys" /> - ending the process is the host's to do, not the engine's,
    ///     and a callback is how the artwork swap already reaches back the other way.
    /// </summary>
    private readonly Action _quitGame;

    /// <summary>
    ///     The host's window. Held only to hand to <see cref="GlobalHotkeys" />, which has to drop
    ///     the window's border when it goes fullscreen; nothing else in the engine touches it.
    /// </summary>
    private readonly GameWindow _window;
    public readonly ResourceManager ContentResourceManager;

    /// <summary>
    ///     How the composed picture is put on the screen - the transform onto the back buffer, the
    ///     colours behind it, and where the game's own screens sit on it.
    /// </summary>
    public readonly PresentationSettings Presentation = new();

    public readonly Random RandomNumberGenerator;
    /// <summary>
    ///     Which of the game's screens is being played, and where to find the others. Not to be
    ///     confused with <see cref="GameScreens" />, which is the planet.
    /// </summary>
    public readonly ScreenDirectory ScreenDirectory = new();

    private bool _disposed;

    /// <summary>
    ///     The game itself, once content has loaded. Null until then, which is what
    ///     <see cref="Game" /> answers for.
    /// </summary>
    private RunningGame? _game;

    /// <summary>
    ///     The four keys that work anywhere. Built once the game can act on them.
    /// </summary>
    private GlobalHotkeys _hotkeys;

    /// <summary>
    ///     The boot screen. Built before anything else, because it is what is on screen while
    ///     everything else is being loaded.
    /// </summary>
    private LoadingScreen _loadingScreen;

    /// <summary>
    ///     The line in the bottom left naming what a hotkey has just switched to. Built here rather
    ///     than in <c>Initialise</c> because it depends on nothing - see
    ///     <c>order-before-machinery</c>: a thing that needs no collaborators cannot be built in
    ///     the wrong phase.
    /// </summary>
    private readonly ModeAnnouncement _announcement = new();

    /// <summary>
    ///     Dissolves one set of artwork into the other when F8 switches between them.
    /// </summary>
    private ArtworkCrossfade _crossfade;

    private bool _particleTestTrigger;

    /// <summary>
    ///     Catches the composed picture and hands it to the back buffer, through the cathode ray
    ///     tube when one is asked for.
    /// </summary>
    private ScreenPresenter _presenter;

    private ScreenEffectEngine _screenEffectEngine;

    /// <summary>
    ///     The debug keys, in a debug build. Null in a release one, which never builds it.
    /// </summary>
    private TestModeKeys? _testModeKeys;

    /// <summary>
    ///     The panel above the playfield. Drawn and updated only while
    ///     <see cref="ScreenDirectory.InPlay" />.
    /// </summary>
    public StatusPanel BlobStatusPanel;

    /// <summary>
    ///     The nine elements the core is missing, and what Blob has done about them.
    /// </summary>
    public CoreElements Elements;

    /// <summary>
    ///     The two bitmap fonts everything that writes text draws through.
    /// </summary>
    public BitmapFont Font1;

    /// <inheritdoc cref="Font1" />
    public BitmapFont Font2;

    /// <summary>
    ///     Draws plain filled rectangles, for the screens that frame something with a block of
    ///     colour rather than a sprite.
    /// </summary>
    public Shapes ShapeDrawer;

    /// <summary>
    ///     The player's score, lives and supplies. Held here beside the panel that draws them rather
    ///     than inside it, because most of the game reads and writes these and only one class draws
    ///     them.
    /// </summary>
    public PlayerStats Stats;

    public GameEngine(
        GraphicsDeviceManager graphics,
        ContentManager content,
        ToggleKeys input,
        GameWindow window,
        Action quitGame)
    {
        Input = input;
        _graphicsDeviceMgr = graphics;
        _contentMgr = content;
        _window = window;
        _quitGame = quitGame;

        // The one generator the game logic draws from, so a game can be reproduced from its seed.
        // The sound engine is deliberately not given it: its only draw happens on a thread pool
        // timer callback, and System.Random is not thread-safe.
        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);

        ContentResourceManager = new ResourceManager(content, graphics, _graphicsSet);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;

        DefineContent();
        Initialise();
    }

    /// <summary>
    ///     The frame's input, read once by <c>GameHost</c> and shared by everything that acts on it,
    ///     so no two readers can disagree about what is held down.
    /// </summary>
    public ToggleKeys Input { get; }

    /// <summary>
    ///     The game once its content has loaded, or a thrown exception saying it has not. This is the one
    ///     place in the engine that can answer "not yet": the nine things behind it were each a nullable
    ///     field under its own identical guard, which read as nine separate doubts rather than the single
    ///     real one. Nothing outside the boot screen runs before the game exists.
    /// </summary>
    private RunningGame Game => NotBeforeContentLoads(_game);

    /// <inheritdoc cref="RunningGame.GameScreens" />
    public GameScreenCollection GameScreens => Game.GameScreens;

    /// <inheritdoc cref="RunningGame.Player" />
    public Blob Player => Game.Player;

    /// <inheritdoc cref="RunningGame.BreakableBridges" />
    public BreakableBridges BreakableBridgesEngine => Game.BreakableBridges;

    /// <inheritdoc cref="RunningGame.TunnelAnimations" />
    public TunnelLiftAnimationEngine TunnelAnimations => Game.TunnelAnimations;

    /// <inheritdoc cref="RunningGame.IntroSequence" />
    public IntroSequence IntroSequence => Game.IntroSequence;

    /// <inheritdoc cref="RunningGame.Nasties" />
    public Nasties Nasties => Game.Nasties;

    /// <inheritdoc cref="RunningGame.BlobKiller" />
    public BlobDeathAnimation BlobKiller => Game.BlobKiller;

    /// <inheritdoc cref="RunningGame.ConveyorNasties" />
    public ConveyorNasties ConveyorNastiesInstance => Game.ConveyorNasties;

    /// <inheritdoc cref="RunningGame.ElectricityNodeGates" />
    public ElectricityGatesAndNodesEngine ElectricityNodeGateEngine => Game.ElectricityNodeGates;

    /// <summary>
    ///     The game being played - score, lives, pause and controls. Built by <see cref="Initialise" />,
    ///     which the constructor calls, so it exists for as long as the engine does: the options screen can
    ///     change the controls before any content has loaded, which is why it is not one of the nine above.
    /// </summary>
    public GameSession Session { get; private set; }

    /// <summary>
    ///     The screen effects. Built by <see cref="Initialise" /> like the session, and for the same reason
    ///     it is not one of the nine: it exists before any content does. Its view ports are registered
    ///     later, because those need dimensions only the loaded content knows.
    /// </summary>
    public ScreenEffectEngine ScreenEffectEngine => _screenEffectEngine;

    /// <summary>
    ///     Called by the host on the way out. The sound engine's timer is the only thing the engine
    ///     owns that has to be put down by hand: every texture, font and sound belongs to the content
    ///     manager, and every sprite batch and render target to the graphics device, both of which
    ///     the game disposes itself.
    /// </summary>
    /// <summary>
    ///     The one thing the engine owns that outlives the game if it is not put down: the sound
    ///     engine's timer. Everything else it holds belongs to the content manager, which disposes
    ///     it with the rest of the content.
    /// </summary>
    /// <remarks>
    ///     Called once today, from <c>GameHost.Dispose</c>, which is the only way out of the game.
    ///     The guard is there so that staying true stays cheap rather than depending on that: a
    ///     second call does nothing, which is what Dispose is required to do.
    ///     <para>
    ///         No finaliser and so no <c>GC.SuppressFinalize</c>. This holds no unmanaged handle of
    ///         its own - the timer is a managed object with its own finaliser - so a finaliser here
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
    ///     The background rumble follows the pause. The null check the old setter carried is gone
    ///     with it: it was there because the constructor assigned Session.Paused before the resource manager
    ///     existed, and this is only subscribed to once the session is built, by which time it does.
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
                Player.KillBlob(false, false);
            }
            else if (nastyType == NastyType.Conveyor)
            {
                Player.KillBlob(true, false);
            }
            else
            {
                Stats.EnergyLevel -= 1;
            }
        }
    }

    private void ElectricityNodeGateEngine_OnBlobStruckByElectricity(object? sender, EventArgs e)
    {
        Player.KillBlob(false, false);
    }

    private void Player_OnScreenChanged(GameScreenType triggerScreen)
    {
        ScreenDirectory.MainGame.PerformPostScreenChangeActions(triggerScreen, new ScreenChange.FadeIn());
    }

    /// <summary>
    ///     Knocks the picture about for something that has exploded - a machine Blob has shot, or
    ///     Blob himself.
    /// </summary>
    /// <remarks>
    ///     Handed out as a callback rather than letting a nasty or Blob hold the thing that shakes
    ///     the screen, which is the main game screen's and has no business being reached into from
    ///     an actor.
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
            ArtworkSwapped,
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
        Font1 = new BitmapFont(_contentMgr.Load<SpriteFont>(Assets.Fonts.ElektoraSmall), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        Font2 = new BitmapFont(_contentMgr.Load<SpriteFont>(Assets.Fonts.ElektoraMedium), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        _presenter = new ScreenPresenter(_graphicsDeviceMgr, ContentResourceManager, Presentation);
        _crossfade = new ArtworkCrossfade(_graphicsDeviceMgr);
    }

    /// <summary>
    ///     Captures all status changes from game components. Everything the game cannot build until its
    ///     content exists is built here, on the one status change that says the content has arrived.
    /// </summary>
    private void ContentResourceManager_OnGameComponentStatusChanged(string message, int percentComplete, GameEngineStatus status)
    {
        if (status != GameEngineStatus.Started)
        {
            UpdateStatus(message, percentComplete, status);
            return;
        }

        _game = BuildComponentsNeedingLoadedContent();

        // Everything from here reads the game through the engine's own accessors, which answer now that
        // _game is assigned.
        RegisterColourAnimations();
        BuildGameScreens();
        RegisterScreenEffectViewPorts();
        BuildTestModeKeys();

        ContentResourceManager.Initialise();

        UpdateStatus("Content loaded", FullyLoadedPercent, GameEngineStatus.StartGame);
    }

    /// <summary>
    ///     Builds the nine things that need loaded content, in the order they need each other: the planet
    ///     first, then what reads it, then Blob, then what chases him. Each is a local until the last
    ///     is built, because the engine's own accessors for them answer "not yet" until <see cref="_game" />
    ///     is assigned - which is the point of doing it this way rather than nine fields filled one at a
    ///     time.
    /// </summary>
    private RunningGame BuildComponentsNeedingLoadedContent()
    {
        // The planet. Built here rather than in the constructor because the screen data and the collision
        // map it needs are resources, and those do not exist until content loads.
        var gameScreens = new GameScreenCollection(
            ContentResourceManager,
            _graphicsSet,
            Elements,
            RandomNumberGenerator,
            ShapeDrawer,
            Font1,
            TileMetrics.TilesAcross,
            TileMetrics.TilesDown,
            TileMetrics.CollisionCellWidth,
            TileMetrics.CollisionCellHeight);

        // Tunnel lift animations. After the planet, which it reads the lift shafts from.
        var tunnelAnimations = new TunnelLiftAnimationEngine(ContentResourceManager, gameScreens, _graphicsSet);

        // Blob's death throes. After the planet, which each thrown-off fragment bounces around.
        var blobKiller = new BlobDeathAnimation(ContentResourceManager, gameScreens, _graphicsSet, RandomNumberGenerator);

        // Its fragments each take a particle effect instance, so this cannot run until the resources are
        // loaded - which is now, not when the load was started.
        blobKiller.LoadContent();

        // The breakable bridges, before Blob, who breaks them. Each bridge composes itself into a
        // render target and then asks the screen effect engine for a view port to draw through, but only
        // when it draws - so it needs the engine to exist, not its view ports.
        var breakableBridges = new BreakableBridges(
            ContentResourceManager,
            gameScreens,
            _graphicsDeviceMgr,
            _graphicsSet,
            RandomNumberGenerator,
            ScreenEffectEngine);

        // Blob. After the planet he walks around and the death animation he triggers, neither of which
        // needs Blob back.
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
        player.OnBlobActionRaised += Player_OnBlobActionRaised;

        // Subscribed here rather than with the session's other event in Initialise, so that there is no
        // window in which the keys can change and there is no Blob to tell. The options screen can be used
        // before content has loaded, and Blob reads the current set off the session as he is built.
        Session.OnPlayerKeysChanged += keys => player.BlobsControl.PlayerKeySet = keys;

        // The intro sequence flies Blob's ship in over the planet and blows it up, so it comes after
        // both the planet and the death animation.
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
        // both are built here rather than in Initialise, where neither existed yet.
        var conveyorNasties = new ConveyorNasties(
            ContentResourceManager,
            gameScreens,
            player,
            RandomNumberGenerator,
            JoltTheScreen);
        conveyorNasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;
        conveyorNasties.LoadContent();

        var electricityNodeGates = new ElectricityGatesAndNodesEngine(
            ContentResourceManager,
            gameScreens,
            _graphicsSet,
            player,
            RandomNumberGenerator,
            conveyorNasties);
        electricityNodeGates.OnBlobStruckByElectricity += ElectricityNodeGateEngine_OnBlobStruckByElectricity;
        player.OnBlobDiedAndRespawned += electricityNodeGates.Reset;

        // The nasties last of the three: they chase Blob, read the planet, and ask the other two where
        // the conveyors and the electricity are.
        var nasties = new Nasties(
            ContentResourceManager,
            gameScreens,
            _graphicsSet,
            player,
            RandomNumberGenerator,
            conveyorNasties,
            electricityNodeGates,
            JoltTheScreen,
            NastiesPerScreen,
            Input);
        nasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        return new RunningGame
        {
            GameScreens = gameScreens,
            Player = player,
            BreakableBridges = breakableBridges,
            TunnelAnimations = tunnelAnimations,
            IntroSequence = introSequence,
            Nasties = nasties,
            BlobKiller = blobKiller,
            ConveyorNasties = conveyorNasties,
            ElectricityNodeGates = electricityNodeGates
        };
    }

    /// <summary>
    ///     The tiles that pulse. Which ones and how is level design rather than anything the engine
    ///     decides, so the table is <see cref="ColourAnimatedTiles" />'s and this only registers it.
    /// </summary>
    private void RegisterColourAnimations()
    {
        foreach (ColourFaderAnimation animation in ColourAnimatedTiles.All())
        {
            GameScreens.ColourAnimations.RegisterTile(animation);
        }
    }

    /// <summary>
    ///     Builds the ten screens and registers each with <see cref="ScreenDirectory" />, which is what
    ///     holds them from here on - including the main game screen, which this used to keep a field of
    ///     its own for as well.
    /// </summary>
    private void BuildGameScreens()
    {
        GameScreens.Initialise();

        var mainScreen = new MainGameScreen(this);

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

        // Two of the ten screens have content of their own to load, in the order the loop that
        // used to do this walked them. It reached LoadContent by downcasting every screen back to
        // a shared base class, where the other eight inherited it as a no-op.
        ScreenDirectory.Options.LoadContent();
        ScreenDirectory.MainGame.LoadContent();
    }

    /// <summary>
    ///     The view ports the screen effects draw through. Separate from building the effect engine,
    ///     which happens in <see cref="Initialise" />: the engine itself needs only a graphics
    ///     device, while a view port needs the status panel's height and the planet's dimensions,
    ///     and those are not known until content has loaded.
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
    ///     Starts the content loading and returns at once - <see cref="ResourceManager.LoadContent" />
    ///     hands the work to a background worker. Nothing that needs loaded content may be touched
    ///     from here; it is all built in
    ///     <see cref="ContentResourceManager_OnGameComponentStatusChanged" /> when the worker says it
    ///     has finished.
    /// </summary>
    private void LoadContent() => ContentResourceManager.LoadContent();

    public void Update(GameTime gameTime)
    {
        // Anything the background content load threw comes out here, on this thread, rather than
        // being lost on the one that threw it.
        ContentResourceManager.ThrowIfLoadFailed();

        // The background rumble's timer runs on the thread pool and only raises a flag. This is
        // where it is acted on, because the sound effect instances belong to this thread.
        ContentResourceManager.GetSoundFxEngine().Update();

        if (Input.GetPressedKey() == Keys.F9)
        {
            Presentation.CrtMode = CrtSettings.Next(Presentation.CrtMode);

            _announcement.Show(CrtSettings.NameOf(Presentation.CrtMode));
        }

        // Stepped here rather than in UpdateRunningGame, so the message fades on the menus too -
        // F9 works everywhere and a message left frozen at full would never go.
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
            // thread pool, and everything BuildComponentsNeedingLoadedContent builds has to be
            // built here.
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
        _hotkeys.Update();

        HandleTestModeKeys();

        if (!Session.Paused)
        {
            UpdateActiveScreen(gameTime);
        }
    }

    /// <summary>
    ///     The debug keys, which only exist in a debug build - both this and
    ///     <see cref="BuildTestModeKeys" /> are removed at the call site by
    ///     <see cref="ConditionalAttribute" />, so a release has no path to them.
    /// </summary>
    [Conditional("DEBUG")]
    private void HandleTestModeKeys()
        => _testModeKeys?.Update();

    /// <inheritdoc cref="HandleTestModeKeys" />
    [Conditional("DEBUG")]
    private void BuildTestModeKeys()
        => _testModeKeys = new TestModeKeys(
            Input, Player, Stats, ScreenEffectEngine, ScreenDirectory, GameScreens, Elements);

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
        if (ScreenDirectory.InPlay)
        {
            BlobStatusPanel.Update(
                Player.BlobInventory,
                Player,
                !IntroSequence.SequenceRunning || IntroSequence.SequenceStage == IntroSequenceStage.EndPause);
        }
    }

    /// <summary>
    ///     The planet core keeps its guardians and particle effects moving while Blob is away
    ///     from it, as long as he is on the screen that approaches it.
    ///     <para>
    ///         This walked all ten screens testing each for an <c>IRealTimeGameScreen</c> the core
    ///         was the only implementor of, then cast it back to call the one member. Naming the
    ///         core is what the loop was doing, at more length.
    ///     </para>
    /// </summary>
    private void UpdateScreensLeftRunningOffScreen(IGameScreen screenToUpdate, GameTime gameTime)
    {
        if (screenToUpdate == ScreenDirectory.PlanetCore)
        {
            return;
        }

        // The core's doorstep, so the core is still working when Blob walks back into it. Read off
        // the map rather than written down, because the author chooses where the core is.
        if (GameScreens.ScreenNumber == GameScreens.CoreExit.ScreenNumber)
        {
            ScreenDirectory.PlanetCore.UpdateOffScreen(gameTime);
        }

    }

    private void CalculatePresentationTransform()
    {
        // The back buffer, not GraphicsDevice.Viewport: the boot screen parks the viewport at
        // the composition size and the render viewports move it around as they please.
        PresentationParameters? backBuffer = _graphicsDeviceMgr.GraphicsDevice.PresentationParameters;

        Presentation.Recalculate(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
    }

    /// <param name="sinceLastDraw">
    ///     How long ago the last frame went out, which only the phosphor's decay reads. Real
    ///     elapsed time rather than a logic frame - this is the one thing in the game that is
    ///     properly a property of the display's rate and not the game's.
    /// </param>
    public void Draw(TimeSpan sinceLastDraw)
    {
        CalculatePresentationTransform();

        // Before the clear, because what the crossfade wants is what is still in the frame: the
        // last picture drawn with the artwork being left behind.
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
        // is showing rather than something laid on the glass.
        _announcement.Draw(Font2, Presentation.Transform);

        _presenter.Present(sinceLastDraw);
    }

}
