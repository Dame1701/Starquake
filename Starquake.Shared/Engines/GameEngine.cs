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

    /// <summary>
    ///     The screen the off-screen game screens are kept ticking over for, so the core is still
    ///     working when the blob walks back into it.
    /// </summary>
    private const int ScreenKeptRunningOffScreen = WorldMap.CoreApproachScreenNumber;

    /// <summary>
    ///     The screen an inventory hand-over from the core was once done on. The code is still here
    ///     but commented out.
    /// </summary>
    private const int CoreInventoryHandoverScreen = 197;

    public readonly ContentManager ContentMgr;

    public readonly ResourceManager ContentResourceManager;

    //public MercuryParticleEngine ParticleEngine;
    private readonly GraphicsDeviceManager _graphicsDeviceMgr;

    /// <summary>
    ///     Which of the two sets of artwork the game is drawn with, and the sheet names that go
    ///     with it. Switched by <see cref="SwitchGraphicsSetIfAsked" />; read live by everything
    ///     that draws.
    /// </summary>
    private readonly GraphicsSet _graphicsSet = new();

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

    private BlobDeathAnimation? _blobKiller;

    /// <summary>
    ///     The boot screen. Built before anything else, because it is what is on screen while
    ///     everything else is being loaded.
    /// </summary>
    private LoadingScreen _loadingScreen;

    /// <summary>
    ///     The four keys that work anywhere. Built once the game can act on them.
    /// </summary>
    private GlobalHotkeys _hotkeys;

    /// <summary>
    ///     The debug keys, in a debug build. Null in a release one, which never builds it.
    /// </summary>
    private TestModeKeys? _testModeKeys;

    private bool _disposed;
    private BreakableBridges? _breakableBridgesEngine;
    private ConveyerNasties? _conveyerNastiesInstance;

    private ElectricityGatesAndNodesEngine? _electricityNodeGateEngine;
    private GameScreenCollection? _gameScreens;

    // Game Sequences
    private IntroSequence? _introSequence;

    private MainGameScreen? _mainScreen;
    private Nasties? _nasties;


    private bool _particleTestTrigger;

    private Blob? _player;



    private SpriteCollection _scanlines;

    private ScreenEffectEngine? _screenEffectEngine;

    private GameSession? _session;


    // Tunnel lift animations
    private TunnelLiftAnimationEngine? _tunnelAnimations;

    public StatusPanel BlobStatusPanel;

    /// <summary>
    ///     The player's score, lives and supplies. Held here beside the panel that draws them rather
    ///     than inside it, because most of the game reads and writes these and only one class draws
    ///     them.
    /// </summary>
    public PlayerStats Stats;

    // Central objects
    public CoreElements Elements;

    // Font 1
    public BitmapFont Font1;

    // Font 2
    public BitmapFont Font2;

    // Test area
    public Shapes ShapeDrawer;

    // Scaling
    public GameEngine(
        GraphicsDeviceManager graphics,
        ContentManager content,
        ToggleKeys input)
    {
        Input = input;
        // Setup graphics device and content management
        _graphicsDeviceMgr = graphics;
        ContentMgr = content;

        // The one generator the game logic draws from, so a game can be reproduced from its seed.
        // The sound engine is deliberately not given it: its only draw happens on a thread pool
        // timer callback, and System.Random is not thread-safe.
        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);

        ContentResourceManager = new ResourceManager(content, graphics, _graphicsSet);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;


        // Starting screen will be the title screen later.....

        DefineContent();
        Initialise();
    }

    /// <summary>
    ///     The frame's input, read once by <c>GameHost</c> and shared by everything that acts on it,
    ///     so no two readers can disagree about what is held down.
    /// </summary>
    public ToggleKeys Input { get; }

    /// <summary>
    ///     Everything below is built by the <see cref="GameEngineStatus.Started" /> handler, because
    ///     each of them needs resources that are still loading before that. Nothing outside the boot
    ///     screen runs before then, so rather than have several hundred readers ask whether the game
    ///     has loaded yet, each says so once here and says it by failing loudly.
    /// </summary>
    public GameScreenCollection GameScreens => NotBeforeContentLoads(_gameScreens);

    /// <inheritdoc cref="GameScreens" />
    public Blob Player => NotBeforeContentLoads(_player);

    /// <inheritdoc cref="GameScreens" />
    public BreakableBridges BreakableBridgesEngine => NotBeforeContentLoads(_breakableBridgesEngine);

    /// <inheritdoc cref="GameScreens" />
    public TunnelLiftAnimationEngine TunnelAnimations => NotBeforeContentLoads(_tunnelAnimations);

    /// <summary>
    ///     The game being played - score, lives, pause and controls. Does not exist until
    ///     <see cref="Initialise" /> has run, which is before any content loads.
    /// </summary>
    public GameSession Session => NotBeforeContentLoads(_session);

    /// <inheritdoc cref="GameScreens" />
    public IntroSequence IntroSequence => NotBeforeContentLoads(_introSequence);

    /// <inheritdoc cref="GameScreens" />
    public Nasties Nasties => NotBeforeContentLoads(_nasties);

    /// <inheritdoc cref="GameScreens" />
    public BlobDeathAnimation BlobKiller => NotBeforeContentLoads(_blobKiller);

    /// <inheritdoc cref="GameScreens" />
    public ConveyerNasties ConveyerNastiesInstance => NotBeforeContentLoads(_conveyerNastiesInstance);

    /// <inheritdoc cref="GameScreens" />
    public ElectricityGatesAndNodesEngine ElectricityNodeGateEngine => NotBeforeContentLoads(_electricityNodeGateEngine);

    /// <inheritdoc cref="GameScreens" />
    public ScreenEffectEngine ScreenEffectEngine => NotBeforeContentLoads(_screenEffectEngine);

    /// <inheritdoc cref="GameScreens" />
    private MainGameScreen MainScreen => NotBeforeContentLoads(_mainScreen);

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

    private void Session_OnPlayerKeysChanged(PlayerKeySet playerKeys)
    {
        // The blob does not exist until content has loaded, and the options screen can be used
        // before then; it reads the set back off the session when it is built.
        if (_player is not { BlobsControl: not null })
        {
            return;
        }

        Player.BlobsControl.PlayerKeySet = playerKeys;
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
            else if (nastyType == NastyType.Conveyer)
            {
                Player.KillBlob(true, false);
            }
            else
            {
                Stats.EnergyLevel = Stats.EnergyLevel - 1;
            }
        }
    }

    private void ElectricityNodeGateEngine_OnBlobStruckByElectricity(object? sender, EventArgs e)
    {
        Player.KillBlob(false, false);
    }

    private void Player_OnScreenChanged(GameScreenType triggerScreen)
    {
        MainScreen.PerformPostScreenChangeActions(triggerScreen, new ScreenChange.FadeIn());
    }

    private void Player_OnBlobActionRaised(BlobAction action, object? data)
    {
        // inform status panel that something has happened
        Stats.BlobHasPerformedAnAction(action, data);

    }

    [MemberNotNull(nameof(BlobStatusPanel), nameof(Stats), nameof(Elements), nameof(_hotkeys),
        nameof(ShapeDrawer))]
    private void Initialise()
    {
        Stats = new PlayerStats(ContentResourceManager.GetSoundFxEngine());
        BlobStatusPanel = new StatusPanel(ContentResourceManager, _graphicsSet, ScreenDirectory, RandomNumberGenerator, Stats);

        _session = new GameSession();
        _session.OnPausedChanged += Session_OnPausedChanged;
        _session.OnPlayerKeysChanged += Session_OnPlayerKeysChanged;

        _hotkeys = new GlobalHotkeys(
            Input,
            ScreenDirectory,
            Session,
            Presentation,
            _graphicsSet,
            _graphicsDeviceMgr,
            ContentResourceManager,
            ArtworkSwapped);

        // Core Elements
        Elements = new CoreElements(Stats, ContentResourceManager, _graphicsSet, RandomNumberGenerator);

        // Test area
        ShapeDrawer = new Shapes(_graphicsDeviceMgr.GraphicsDevice);

        // Built here rather than with its view ports, so that anything constructed from here on -
        // the blob's jump platforms, for one - can be handed it instead of reaching for it later.
        _screenEffectEngine = new ScreenEffectEngine(_graphicsDeviceMgr, Presentation);
    }

    /// <summary>
    ///     Loads what is needed to draw the loading screen, names every other piece of content for the
    ///     background load that <see cref="LoadContent" /> starts, and builds the two components that have
    ///     to exist before any of it arrives.
    /// </summary>
    /// <remarks>
    ///     The registration order is kept exactly as it was found. <see cref="DefineScreenData" /> is the
    ///     one place it is known to matter, and the rest was not worth risking to tidy.
    /// </remarks>
    [MemberNotNull(nameof(_loadingScreen), nameof(_scanlines), nameof(Font1), nameof(Font2))]
    private void DefineContent()
    {
        _loadingScreen = new LoadingScreen(_graphicsDeviceMgr, ContentMgr, Presentation, Input);

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
        if (IntroSequence.SequenceStage is IntroSequenceStage.Finished or IntroSequenceStage.EndPause)
        {
            ScreenDirectory.MainGame.SpritesChanged();
        }
    }

    /// <summary>
    ///     The two things the engine draws itself: the fonts every screen writes with, and the CRT
    ///     scanline overlay laid over the finished picture. The loading screen loads its own.
    /// </summary>
    [MemberNotNull(nameof(Font1), nameof(Font2), nameof(_scanlines))]
    private void DefineFonts()
    {
        Font1 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraSmall"), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        Font2 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraMedium"), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        _scanlines = new SpriteCollection(_graphicsDeviceMgr, ContentMgr, @"Graphics\Scanlines2", CompositionSize.Width, CompositionSize.Height);
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

        BuildComponentsNeedingLoadedContent();
        RegisterColourAnimations();
        BuildGameScreens();
        RegisterScreenEffectViewPorts();

        ContentResourceManager.Initialise();

        UpdateStatus("Content loaded", FullyLoadedPercent, GameEngineStatus.StartGame);
    }

    /// <summary>
    ///     The blob, the screen collection and the colour animation engine all need resources that do not
    ///     exist until content loads, which is why none of them is built in the constructor.
    /// </summary>
    private void BuildComponentsNeedingLoadedContent()
    {
        // Game Screens. Built here rather than in the constructor because the screen data and
        // the collision map it needs are resources, and those do not exist until content loads
        _gameScreens =
            new GameScreenCollection(
                ContentResourceManager,
                _graphicsSet,
                Elements,
                RandomNumberGenerator,
                TileMetrics.TilesAcross,
                TileMetrics.TilesDown,
                TileMetrics.CollisionCellWidth,
                TileMetrics.CollisionCellHeight);

        // Tunnel lift animations. After the screen collection, which it reads the lift shafts from.
        _tunnelAnimations = new TunnelLiftAnimationEngine(ContentResourceManager, GameScreens, _graphicsSet);

        // The blob's death throes. After the screen collection, which each thrown-off fragment
        // bounces around.
        _blobKiller = new BlobDeathAnimation(ContentResourceManager, GameScreens, _graphicsSet, RandomNumberGenerator);

        // Its fragments each take a particle effect instance, so this cannot run until the
        // resources are loaded - which is now, not when the load was started.
        BlobKiller.LoadContent();

        // The breakable bridges, before the blob that breaks them. Each bridge composes itself into
        // a render target and then asks the screen effect engine for a view port to draw through,
        // but only when it draws - so it needs the engine to exist, not its view ports.
        _breakableBridgesEngine = new BreakableBridges(
            ContentResourceManager,
            GameScreens,
            _graphicsDeviceMgr,
            _graphicsSet,
            RandomNumberGenerator,
            ScreenEffectEngine);

        // The blob. After the planet it walks around and the death animation it triggers, neither of
        // which needs the blob back, and built here rather than in Initialise because the three
        // particle effects he carries are resources that do not exist until content loads.
        _player = new Blob(
            ContentResourceManager,
            GameScreens,
            _graphicsDeviceMgr,
            _graphicsSet,
            ScreenDirectory,
            ScreenEffectEngine,
            Stats,
            Session,
            BlobKiller,
            BreakableBridgesEngine,
            RandomNumberGenerator,
            Input);
        Player.OnScreenChanged += Player_OnScreenChanged;
        Player.OnBlobActionRaised += Player_OnBlobActionRaised;

        // The intro sequence flies the blob's ship in over the planet and blows it up, so it
        // comes after both the planet and the death animation.
        _introSequence = new IntroSequence(
            ContentResourceManager,
            Font2,
            GameScreens,
            _graphicsSet,
            Presentation,
            ScreenDirectory,
            ScreenEffectEngine,
            BlobKiller,
            RandomNumberGenerator);

        Presentation.ScreenXOffset = (_graphicsDeviceMgr.PreferredBackBufferWidth - GameScreens.ScreenWidth) / 2;
        Presentation.ScreenYOffset = 10;

        // The conveyer nasties and the electricity engine. Both chase the blob and read the
        // planet, so both are built here rather than in Initialise, where neither existed yet.
        _conveyerNastiesInstance = new ConveyerNasties(
            ContentResourceManager,
            ContentMgr,
            GameScreens,
            _graphicsDeviceMgr,
            Player,
            RandomNumberGenerator);
        ConveyerNastiesInstance.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        var converyerNastyGraphics = new ContentList();
        converyerNastyGraphics.AddContent("conveyerNasty", "Graphics\\ConveryerNasty");
        ConveyerNastiesInstance.LoadContent(converyerNastyGraphics);

        _electricityNodeGateEngine = new ElectricityGatesAndNodesEngine(
            ContentResourceManager,
            GameScreens,
            _graphicsSet,
            Player,
            RandomNumberGenerator,
            ConveyerNastiesInstance);
        ElectricityNodeGateEngine.OnBlobStruckByElectricity += ElectricityNodeGateEngine_OnBlobStruckByElectricity;
        Player.OnBlobDiedAndRespawned += ElectricityNodeGateEngine.Reset;

        // The nasties last of the three: they chase the blob, read the planet, and ask the other two
        // where the conveyers and the electricity are.
        _nasties = new Nasties(
            ContentResourceManager,
            GameScreens,
            _graphicsSet,
            Session,
            Player,
            RandomNumberGenerator,
            ConveyerNastiesInstance,
            ElectricityNodeGateEngine,
            NastiesPerScreen,
            Input);
        Nasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        BuildTestModeKeys();
    }

    /// <summary>
    ///     The tiles that pulse, and how each one pulses. A long literal list, and nothing but a list.
    /// </summary>
    private void RegisterColourAnimations()
    {
        GameScreens.ColourAnimations.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.TeleportBooth,
            UpperTileToDraw = 7,
            Intensity = 4,
            TransitionColour = new Color(0, 0, ColourLevels.Full),
            Speed = 8
        }); // Teleport booths
        GameScreens.ColourAnimations.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.AccessBoothLeft,
            UpperTileToDraw = 9,
            Intensity = 2,
            TransitionColour = new Color(0, ColourLevels.Full, 0),
            Speed = 32
        }); // Access tunnel left
        GameScreens.ColourAnimations.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.AccessBoothRight,
            UpperTileToDraw = 10,
            Intensity = 2,
            TransitionColour = new Color(ColourLevels.Full, 0, 0),
            Speed = 32
        }); // Access tunnel right
        GameScreens.ColourAnimations.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.ShuttleTag1,
            UpperTileToDraw = -1,
            Intensity = 2,
            TransitionColour = new Color(ColourLevels.Full, ColourLevels.Full, 0),
            Speed = 16
        }); // Yellow teleport tag
        GameScreens.ColourAnimations.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.ShuttleTag3,
            UpperTileToDraw = -1,
            Intensity = 6,
            TransitionColour = new Color(ColourLevels.Full, 0, 0),
            Speed = 16
        }); // Red teleport tag
        GameScreens.ColourAnimations.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.ShuttleTag2,
            UpperTileToDraw = -1,
            Intensity = 6,
            TransitionColour = new Color(0, 100, ColourLevels.Full),
            Speed = 16
        }); // Blue teleport tag
    }

    /// <summary>
    ///     Only the main game screen is kept in a field of its own, because two places in this class talk
    ///     to it directly rather than through the dictionary.
    /// </summary>
    private void BuildGameScreens()
    {
        GameScreens.Initialise();

        _mainScreen = new MainGameScreen(this);

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

        ScreenDirectory.Add(_mainScreen);
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
            Session,
            Player,
            RandomNumberGenerator,
            Elements));
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
        _mainScreen.LoadContent();
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
            "GameArea",
            Presentation.ScreenXOffset,
            Presentation.ScreenYOffset + BlobStatusPanel.Height + 10,
            GameScreens.ScreenWidth,
            GameScreens.ScreenHeight);

        ScreenEffectEngine.RegisterViewPort(
            "StatusArea",
            Presentation.ScreenXOffset + 2,
            Presentation.ScreenYOffset,
            BlobStatusPanel.Width,
            BlobStatusPanel.Height);

        ScreenEffectEngine.RegisterViewPort(
            "EntireScreen",
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
            Presentation.ScanlinesEnabled = !Presentation.ScanlinesEnabled;
        }

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
            Input, Player, Stats, ScreenEffectEngine, ScreenDirectory, GameScreens);

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
        if (ScreenDirectory.Current is
            GameScreenType.Cheops or
            GameScreenType.MainGame or
            GameScreenType.PlanetCore or
            GameScreenType.Teleporter or
            GameScreenType.AccessBooth)
        {
            BlobStatusPanel.Update(
                Player.BlobInventory,
                Player,
                !IntroSequence.SequenceRunning || IntroSequence.SequenceStage == IntroSequenceStage.EndPause);
        }
    }

    /// <summary>
    ///     The planet core keeps its guardians and particle effects moving while the blob is away
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

        if (GameScreens.ScreenNumber == ScreenKeptRunningOffScreen)
        {
            ScreenDirectory.PlanetCore.UpdateOffScreen(gameTime);
        }

        if (GameScreens.ScreenNumber == CoreInventoryHandoverScreen)
        {
            /*
            int counter = 4;
            if (Elements.GetActiveElements().Count < 4)
            {
                counter = Elements.GetActiveElements().Count;
            }

            for (int a = 0; a < counter; a++)
            {
                Player.BlobInventory[a] = (enCollectables)Elements.GetActiveElements()[a].CurrentElement;
            }
             */
        }
    }

    private void CalculatePresentationTransform()
    {
        // The back buffer, not GraphicsDevice.Viewport: the boot screen parks the viewport at
        // the composition size and the render viewports move it around as they please.
        PresentationParameters? backBuffer = _graphicsDeviceMgr.GraphicsDevice.PresentationParameters;

        Presentation.Recalculate(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
    }

    public void Draw()
    {
        CalculatePresentationTransform();

        if (Status == GameEngineStatus.StartGame)
        {
            // Status panel
            if (ScreenDirectory.Current == GameScreenType.Cheops ||
                ScreenDirectory.Current == GameScreenType.MainGame ||
                ScreenDirectory.Current == GameScreenType.PlanetCore ||
                ScreenDirectory.Current == GameScreenType.Teleporter ||
                ScreenDirectory.Current == GameScreenType.AccessBooth)
            {
                ScreenEffectEngine.SelectRenderViewPort("StatusArea"); // StatusPanelArea;
                BlobStatusPanel.Draw();
            }

            ScreenEffectEngine.SelectRenderViewPort("GameArea"); // GameArea;
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

        // Screen effects compose over whatever was just drawn, and only exist once the game does
        _screenEffectEngine?.Draw();

        DrawScanlines();
    }

    /// <summary>
    ///     The CRT scanline overlay, laid over the finished picture as the very last thing drawn.
    ///     Scaled and centred with the composition rather than laid over the letterbox borders as
    ///     well, which is why it draws through the presentation transform.
    ///     <para>
    ///         <c>GameHost</c> used to do this, reaching in for the sprites, the flag and the
    ///         transform. It is the game's own overlay and the game draws it; the host's business is
    ///         pacing frames, not deciding what the picture looks like.
    ///     </para>
    /// </summary>
    private void DrawScanlines()
    {
        if (!Presentation.ScanlinesEnabled)
        {
            return;
        }

        _scanlines.Begin(BlendState.AlphaBlend, Presentation.Transform);
        _scanlines.DrawSprite(0, 0, 0,
            new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ColourLevels.Full));
        _scanlines.End();
    }
}
