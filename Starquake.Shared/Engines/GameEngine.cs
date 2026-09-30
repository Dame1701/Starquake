// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
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
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Engines;

public class GameEngine : StartableGameComponentBase, IDisposable
{
    /// <summary>
    ///     How much of a channel the boot screen fades in by, and the progress bar background fades
    ///     out by, each logic frame. <see cref="ColourLevels.Full" /> divided by this is how many
    ///     frames the fade lasts.
    /// </summary>
    private const int BootFadeStep = 4;

    /// <summary>
    ///     A percentage, complete. The loader reports its progress as one, so this is what it reports
    ///     when there is nothing left to load.
    /// </summary>
    private const int FullyLoadedPercent = 100;

    /// <summary>
    ///     How wide the boot progress bar is at full, in the one-pixel-wide slices it is drawn from,
    ///     and where the first slice goes. Each slice is drawn two pixels further right than the last.
    /// </summary>
    private const int BootProgressBarSlices = 519;

    /// <inheritdoc cref="BootProgressBarSlices" />
    private const int BootProgressBarXPos = 122;

    /// <inheritdoc cref="BootProgressBarSlices" />
    private const int BootProgressBarYPos = 661;

    /// <inheritdoc cref="BootProgressBarSlices" />
    private const int BootProgressBarSliceSpacing = 2;

    /// <summary>
    ///     Where the progress bar's backing plate sits on the boot screen.
    /// </summary>
    private const int BootProgressBackgroundXPos = 116;

    /// <inheritdoc cref="BootProgressBackgroundXPos" />
    private const int BootProgressBackgroundYPos = 657;

    /// <summary>
    ///     Where the pause banner is drawn, in playfield coordinates.
    /// </summary>
    private const int PausedOverlayXPos = 400;

    /// <inheritdoc cref="PausedOverlayXPos" />
    private const int PausedOverlayYPos = 200;

    /// <summary>
    ///     Test-mode cheats: how many lives F2 hands back, and how long the screen-stepping keys wait
    ///     between screens so that holding one down steps rather than races.
    /// </summary>
    private const int TestModeLivesRestored = 5;

    /// <inheritdoc cref="TestModeLivesRestored" />
    private const int TestModeScreenStepDelayMs = 100;

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
    private SpriteCollection _bootProgress;
    private SpriteCollection _bootProgressBackground;
    // Boot graphics
    private SpriteCollection _bootScreen;

    private BreakableBridges? _breakableBridgesEngine;
    private ConveyerNasties? _conveyerNastiesInstance;

    private ElectricityGatesAndNodesEngine? _electricityNodeGateEngine;
    private GameScreenCollection? _gameScreens;

    // Game Sequences
    private IntroSequence? _introSequence;

    // Loading screen
    private int _bootProgressBackgroundFade = ColourLevels.Full;

    private int _loadingScreenFade;

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
        ContentManager content)
    {
        // Setup graphics device and content management
        _graphicsDeviceMgr = graphics;
        ContentMgr = content;
        Presentation.BackgroundColour = Color.Black;

        // Before the resource manager, which hands it to the sound engine.
        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);

        ContentResourceManager = new ResourceManager(content, graphics, RandomNumberGenerator, _graphicsSet);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;


        // Starting screen will be the title screen later.....

        DefineContent();
        Initialise();
    }

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
    public void Dispose()
    {
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
            BlobStatusPanel.PlayerScore += ScoreForShootingNasty;
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
                BlobStatusPanel.EnergyLevel = BlobStatusPanel.EnergyLevel - 1;
            }
        }
    }

    private void ElectricityNodeGateEngine_OnBlobStruckByElectricity(object? sender, EventArgs e)
    {
        Player.KillBlob(false, false);
    }

    private void Player_OnScreenChanged(GameScreenType triggerScreen)
    {
        MainScreen.PerformPostScreenChangeActions(triggerScreen, null);
    }

    private void Player_OnBlobActionRaised(BlobAction action, object? data)
    {
        // inform status panel that something has happened
        BlobStatusPanel.BlobHasPerformedAnAction(action, data);

    }

    [MemberNotNull(nameof(BlobStatusPanel), nameof(Elements),
        nameof(ShapeDrawer))]
    private void Initialise()
    {
        BlobStatusPanel = new StatusPanel(ContentResourceManager, _graphicsSet, ScreenDirectory, RandomNumberGenerator);
        BlobStatusPanel.Initialise(null);

        _session = new GameSession();
        _session.OnPausedChanged += Session_OnPausedChanged;
        _session.OnPlayerKeysChanged += Session_OnPlayerKeysChanged;

        // Core Elements
        Elements = new CoreElements(BlobStatusPanel, ContentResourceManager, _graphicsSet, Presentation, RandomNumberGenerator);

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
    [MemberNotNull(nameof(_bootScreen), nameof(_bootProgressBackground), nameof(_bootProgress),
        nameof(_scanlines), nameof(Font1), nameof(Font2))]
    private void DefineContent()
    {
        DefineBootGraphics();

        Status = GameEngineStatus.StartingGraphicsLoaded;

        DefineFonts();

        ContentManifest.RegisterAll(ContentResourceManager);
    }

    [MemberNotNull(nameof(_bootScreen), nameof(_bootProgressBackground), nameof(_bootProgress),
        nameof(_scanlines))]
    private void DefineBootGraphics()
    {
        _bootScreen = new SpriteCollection(_graphicsDeviceMgr, ContentMgr, @"Graphics\\BootScreen2", CompositionSize.Width, CompositionSize.Height);
        _bootProgressBackground = new SpriteCollection(_graphicsDeviceMgr, ContentMgr, @"Graphics\\ProgressBarBackground", 1046, 32);
        _bootProgress = new SpriteCollection(_graphicsDeviceMgr, ContentMgr, @"Graphics\\BootProgress", 1, 24);
        _scanlines = new SpriteCollection(_graphicsDeviceMgr, ContentMgr, @"Graphics\\Scanlines2", CompositionSize.Width, CompositionSize.Height);
    }

    [MemberNotNull(nameof(Font1), nameof(Font2))]
    private void DefineFonts()
    {
        Font1 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraSmall"), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        Font2 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraMedium"), new SpriteBatch(_graphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
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
            BlobStatusPanel,
            Session,
            BlobKiller,
            BreakableBridgesEngine,
            RandomNumberGenerator);
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
        ConveyerNastiesInstance.Initialise(null);

        var converyerNastyGraphics = new ContentList(ContentMgr);
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
            NastiesPerScreen);
        Nasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;
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
        GameScreens.Initialise(null);

        _mainScreen = new MainGameScreen(this);

        ScreenDirectory.Add(GameScreenType.Options, new OptionsScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            Session));
        ScreenDirectory.Add(GameScreenType.ComputerReport, new ComputerReportScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            RandomNumberGenerator));
        ScreenDirectory.Add(GameScreenType.HiScores, new HiScoreScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            BlobStatusPanel));
        ScreenDirectory.Add(GameScreenType.GameOver, new GameOverScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            BlobStatusPanel,
            Elements,
            GameScreens));
        ScreenDirectory.Add(GameScreenType.GameComplete, new GameCompleteScreen(
            ContentResourceManager,
            Font2,
            _graphicsDeviceMgr,
            Presentation,
            ScreenEffectEngine,
            ScreenDirectory,
            Elements));

        ScreenDirectory.Add(GameScreenType.MainGame, _mainScreen);
        ScreenDirectory.Add(GameScreenType.Teleporter, new TeleportScreen(
            ContentResourceManager,
            Font2,
            GameScreens,
            _graphicsSet,
            Player,
            Presentation,
            RandomNumberGenerator,
            ScreenDirectory,
            ShapeDrawer));
        ScreenDirectory.Add(GameScreenType.PlanetCore, new PlanetCoreScreen(
            ContentResourceManager,
            GameScreens,
            _graphicsSet,
            Presentation,
            ScreenDirectory,
            Session,
            Player,
            RandomNumberGenerator,
            Elements));
        ScreenDirectory.Add(GameScreenType.AccessBooth, new AccessBoothScreen(
            BlobStatusPanel,
            ContentResourceManager,
            Font2,
            GameScreens,
            _graphicsSet,
            Player,
            RandomNumberGenerator,
            ScreenDirectory,
            ShapeDrawer));
        ScreenDirectory.Add(GameScreenType.Cheops, new CheopsScreen(
            BlobStatusPanel,
            ContentResourceManager,
            Elements,
            Font2,
            GameScreens,
            _graphicsSet,
            Player,
            RandomNumberGenerator,
            ScreenDirectory,
            ShapeDrawer));

        foreach (IGameScreen screen in ScreenDirectory.All)
        {
            ((GameComponentBase)screen).LoadContent();
        }
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

    // Resets the game back to defaults - need to sort all of this out!
    public void Reset()
    {
        MainScreen.Reset(null);
    }

    public void Update(GameTime gameTime)
    {
        // Anything the background content load threw comes out here, on this thread, rather than
        // being lost on the one that threw it.
        ContentResourceManager.ThrowIfLoadFailed();

        if (ToggleKeys.GetPressedKey() == Keys.F9)
        {
            Presentation.ScanlinesEnabled = !Presentation.ScanlinesEnabled;
        }

        if (Status == GameEngineStatus.StartingGraphicsLoaded)
        {
            FadeUpTheLoadingScreen();
        }
        else if (Status == GameEngineStatus.ContentLoaded)
        {
            WaitToLeaveTheLoadingScreen();
        }
        else if (Status == GameEngineStatus.StartGame)
        {
            UpdateRunningGame(gameTime);
        }
    }

    /// <summary>
    ///     Holds the finished loading screen until a key is pressed, fading the progress bar out
    ///     while it waits. The wait is deliberate - it has been here since the first commit - so the
    ///     boot screen is something to look at and leave rather than something that vanishes.
    ///     <para>
    ///         It is also the hop back onto the main thread. The loading task raises
    ///         <see cref="GameEngineStatus.ContentLoaded" /> from the thread pool, and everything
    ///         <see cref="BuildComponentsNeedingLoadedContent" /> builds has to be built here.
    ///     </para>
    /// </summary>
    private void WaitToLeaveTheLoadingScreen()
    {
        // Fade out the progress bar background
        if (_bootProgressBackgroundFade > 0)
        {
            _bootProgressBackgroundFade -= BootFadeStep;
        }

        if (_bootProgressBackgroundFade < 0)
        {
            _bootProgressBackgroundFade = 0;
        }

        if (ToggleKeys.GetPressedKey() != Keys.None)
        {
            ContentResourceManager_OnGameComponentStatusChanged(
                "Content loaded",
                FullyLoadedPercent,
                GameEngineStatus.Started);
        }
    }

    private void FadeUpTheLoadingScreen()
    {
        _loadingScreenFade += BootFadeStep;

        if (_loadingScreenFade > ColourLevels.Full)
        {
            _loadingScreenFade = ColourLevels.Full;
        }

        if (_loadingScreenFade == ColourLevels.Full)
        {
            Status = GameEngineStatus.Starting;
            LoadContent();
        }
    }

    private void UpdateRunningGame(GameTime gameTime)
    {
        TogglePauseIfAsked();
        ToggleFullScreenIfAsked();
        ToggleBackgroundsIfAsked();
        SwitchGraphicsSetIfAsked();

        HandleTestModeKeys();

        if (!Session.Paused)
        {
            UpdateActiveScreen(gameTime);
        }
    }

    /// <summary>
    ///     The teleporter is deliberately not pausable - it is the one in-play screen where standing
    ///     still is the point, so pausing it would do nothing and only confuse.
    /// </summary>
    private void TogglePauseIfAsked()
    {
        if (ScreenDirectory.Current == GameScreenType.Cheops ||
            ScreenDirectory.Current == GameScreenType.MainGame ||
            ScreenDirectory.Current == GameScreenType.PlanetCore ||
            ScreenDirectory.Current == GameScreenType.Teleporter ||
            ScreenDirectory.Current == GameScreenType.AccessBooth)
        {
            if (ScreenDirectory.Current != GameScreenType.Teleporter)
            {
                if (ToggleKeys.GetPressedKey() == Session.PlayerKeys.Pause)
                {
                    Session.Paused = !Session.Paused;

                    if (Session.Paused)
                    {
                        ContentResourceManager.GetSoundFxEngine().KillSounds(true);
                    }
                }
            }
        }
    }

    /// <remarks>
    ///     A toggle rather than IsKeyDown: held down, that flipped fullscreen once per logic frame for
    ///     as long as the key was held. The game pauses itself afterwards so the player does not die
    ///     while the window is resizing - on every in-play screen but the teleporter, which cannot pause.
    /// </remarks>
    private void ToggleFullScreenIfAsked()
    {
        if (ToggleKeys.GetPressedKey() == Keys.F12)
        {
            if (ScreenDirectory.Current is
                GameScreenType.Cheops or
                GameScreenType.MainGame or
                GameScreenType.PlanetCore or
                GameScreenType.AccessBooth)
            {
                // Automatically pause the game after switching to or from fullscreen so the player doesn't die unexpectantly
                if (!Session.Paused)
                {
                    Session.Paused = true;
                }
            }

            try
            {
                ToggleFullScreen();
            }
            catch { }
        }
    }

    private void ToggleBackgroundsIfAsked()
    {
        if (ToggleKeys.GetPressedKey() == Keys.F10)
        {
            if (ScreenDirectory.Current == GameScreenType.MainGame)
            {
                Presentation.DrawBackgrounds = !Presentation.DrawBackgrounds;
            }
        }
    }

    private void SwitchGraphicsSetIfAsked()
    {
        if (ToggleKeys.GetPressedKey() == Keys.F8)
        {
            _graphicsSet.Switch();

            // The original graphics have no backdrops drawn for them.
            Presentation.DrawBackgrounds = _graphicsSet.IsRedrawn;

            if (IntroSequence.SequenceStage == IntroSequenceStage.Finished || IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
            {
                ((MainGameScreen)ScreenDirectory.Get(GameScreenType.MainGame)).SpritesChanged();
            }

            ContentResourceManager.GetSoundFxEngine().PlaySound("AccessSuccess", 1f, false);
        }
    }

    /// <summary>
    ///     The debug keys: F1 kills the blob, F2 refills its lives, F3 runs the fade
    ///     effect, the numeric keypad steps between screens, and K and L drive the shuttle-tag
    ///     colour animations. Compiled out of a release build entirely by
    ///     <see cref="ConditionalAttribute" />, which removes the calls to this rather than leaving
    ///     a branch that is never taken, so a release has no path to them at all.
    ///     <para>
    ///         The keys that are not debug keys are handled elsewhere and stay live in both: F8
    ///         swaps the artwork, F9 the scanlines, F10 the backdrops, F12 fullscreen, and the
    ///         player's own pause and quit keys.
    ///     </para>
    /// </summary>
    [Conditional("DEBUG")]
    private void HandleTestModeKeys()
    {
        if (Keyboard.GetState().IsKeyDown(Keys.F1))
        {
            if (Player.BlobsState == BlobState.Normal)
            {
                Player.KillBlob(false, false);
            }
        }

        // Give us some lives back (Testing)
        if (Keyboard.GetState().IsKeyDown(Keys.F2))
        {
            BlobStatusPanel.BlobLives = TestModeLivesRestored;
            Player.BlobsState = BlobState.Dead;
        }

        if (ToggleKeys.GetPressedKey() == Keys.F3)
        {
            ScreenEffectEngine.ExecuteScreenEffect(ScreenEffectType.AdditiveFade, -1, 50, false);
        }

        if (ScreenDirectory.Current == GameScreenType.MainGame)
        {
            StepBetweenScreens();
        }
    }

    private void StepBetweenScreens()
    {
        if (Keyboard.GetState().IsKeyDown(Keys.NumPad4))
        {
            if (GameScreens.ScreenNumber > 0)
            {
                GameScreens.ScreenNumber--;
                ScreenDirectory.Get(ScreenDirectory.Current).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.NumPad6))
        {
            if (GameScreens.ScreenNumber < WorldMap.LastScreenNumber)
            {
                GameScreens.ScreenNumber++;
                ScreenDirectory.Get(ScreenDirectory.Current).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.NumPad8))
        {
            if (GameScreens.ScreenNumber - WorldMap.ScreensAcross >= 0)
            {
                GameScreens.ScreenNumber -= WorldMap.ScreensAcross;
                ScreenDirectory.Get(ScreenDirectory.Current).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.NumPad2))
        {
            if (GameScreens.ScreenNumber + WorldMap.ScreensAcross <= WorldMap.LastScreenNumber)
            {
                GameScreens.ScreenNumber += WorldMap.ScreensAcross;
                ScreenDirectory.Get(ScreenDirectory.Current).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }
    }

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

    private void UpdateScreensLeftRunningOffScreen(IGameScreen screenToUpdate, GameTime gameTime)
    {
        foreach (IGameScreen igs in ScreenDirectory.All)
        {
            if (igs is IRealTimeGameScreen && igs != screenToUpdate)
            {
                if (GameScreens.ScreenNumber == ScreenKeptRunningOffScreen)
                {
                    ((IRealTimeGameScreen)igs).UpdateOffScreen(gameTime);
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
        }
    }

    private void CalculatePresentationTransform()
    {
        // The back buffer, not GraphicsDevice.Viewport: the boot screen parks the viewport at
        // the composition size and the render viewports move it around as they please.
        PresentationParameters? backBuffer = _graphicsDeviceMgr.GraphicsDevice.PresentationParameters;

        Presentation.Recalculate(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
    }

    /// <summary>
    ///     Go fullscreen at the resolution the desktop is already running at, so the picture is
    ///     composed for the display it lands on instead of being stretched up from the windowed
    ///     size. The game itself never changes size: it always composes at
    ///     <see cref="CompositionSize.Width" /> x <see cref="CompositionSize.Height" /> and
    ///     Presentation.Transform scales that onto whatever back buffer we end up with.
    ///     HardwareModeSwitch has to be on. Borderless (the false case) looks like the friendlier
    ///     option, but on macOS it produces a window clamped to the desktop area *below* the menu
    ///     bar - a back buffer 39 points short, sat 39 points down the screen, with the picture
    ///     centred inside that rather than inside the display.
    /// </summary>
    private void ToggleFullScreen()
    {
        _graphicsDeviceMgr.HardwareModeSwitch = true;

        if (_graphicsDeviceMgr.IsFullScreen)
        {
            _graphicsDeviceMgr.PreferredBackBufferWidth = CompositionSize.Width;
            _graphicsDeviceMgr.PreferredBackBufferHeight = CompositionSize.Height;
        }
        else
        {
            DisplayMode? desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            _graphicsDeviceMgr.PreferredBackBufferWidth = desktop.Width;
            _graphicsDeviceMgr.PreferredBackBufferHeight = desktop.Height;
        }

        _graphicsDeviceMgr.IsFullScreen = !_graphicsDeviceMgr.IsFullScreen;
        _graphicsDeviceMgr.ApplyChanges();
    }

    public void Draw(GameTime gameTime)
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
                BlobStatusPanel.Draw(gameTime);
            }

            ScreenEffectEngine.SelectRenderViewPort("GameArea"); // GameArea;
            _graphicsDeviceMgr.GraphicsDevice.Clear(Presentation.ScreenClearColour);

            IGameScreen? screenToDraw = ScreenDirectory.CurrentScreen;
            screenToDraw?.Draw(gameTime);

            if (Session.Paused)
            {
                Overlays.Paused(PausedOverlayXPos, PausedOverlayYPos, ShapeDrawer, Font2);
            }
        }
        else
        {
            // We're starting up so draw some nice progress bars and graphics etc.
            // The viewport has to be the whole back buffer, not the composition size: the
            // boot screen is scaled up into it by Presentation.Transform like everything else,
            // and a viewport any smaller would just clip the result.
            PresentationParameters? backBuffer = _graphicsDeviceMgr.GraphicsDevice.PresentationParameters;

            var vp = new Viewport();
            vp.X = 0;
            vp.Y = 0;
            vp.Width = backBuffer.BackBufferWidth;
            vp.Height = backBuffer.BackBufferHeight;

            _graphicsDeviceMgr.GraphicsDevice.Viewport = vp;
            _graphicsDeviceMgr.GraphicsDevice.Clear(Presentation.ScreenClearColour);


            if (Status == GameEngineStatus.Starting ||
                Status == GameEngineStatus.StartingGraphicsLoaded ||
                Status == GameEngineStatus.ContentLoaded)
            {
                _bootScreen.Begin(BlendState.Opaque, Presentation.Transform);
                _bootScreen.DrawSpriteUnscaled(0, 0, 0, _loadingScreenFade);
                _bootScreen.End();

                _bootProgressBackground.Begin(BlendState.AlphaBlend, Presentation.Transform);
                _bootProgressBackground.DrawSpriteUnscaled(0, BootProgressBackgroundXPos, BootProgressBackgroundYPos, _bootProgressBackgroundFade);
                _bootProgressBackground.End();

                // Draw the status percentage
                _bootProgress.Begin(BlendState.AlphaBlend, Presentation.Transform);
                int percentBarWidth = (int)(BootProgressBarSlices / (double)FullyLoadedPercent * PercentComplete);

                for (int a = 0; a < percentBarWidth; a++)
                {
                    _bootProgress.DrawSpriteUnscaled(0, BootProgressBarXPos + a * BootProgressBarSliceSpacing, BootProgressBarYPos, _bootProgressBackgroundFade);
                }

                _bootProgress.End();
                //Vector2 statusMessageSize = Font1.MeasureString(statusMessage);
                //Font1.Begin(BlendState.AlphaBlend);
                //Font1.WriteText((int)(1280 - statusMessageSize.X) / 2, 620, statusMessage, Color.White, ColourLevels.Full);
                //Font1.End();

            }
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

        _scanlines.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, Presentation.Transform);
        _scanlines.DrawSprite(0, 0, 0,
            new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ColourLevels.Full));
        _scanlines.End();
    }
}
