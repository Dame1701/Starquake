// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.GameComponents.GameScreens;
using Starquake.GameComponents.PresentationScreens;
using Starquake.GameComponents;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.SharedLibs;
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

    private BlobDeathAnimation? _blobKiller;

    public StatusPanel BlobStatusPanel;

    private GameSession? _session;
    private SpriteCollection _bootProgress;
    private SpriteCollection _bootProgressBackground;
    private int _bootProgressBackgroundFade = ColourLevels.Full;
    // Boot graphics
    private SpriteCollection _bootScreen;

    private BreakableBridges? _breakableBridgesEngine;

    private ColourAnimationEngine? _colourAnimationEngine;
    public readonly ContentManager ContentMgr;

    public readonly ResourceManager ContentResourceManager;
    private ConveyerNasties? _conveyerNastiesInstance;
    /// <summary>
    ///     Which of the game's screens is being played, and where to find the others. Not to be
    ///     confused with <see cref="GameScreens" />, which is the planet.
    /// </summary>
    public readonly ScreenDirectory ScreenDirectory = new();

    private ElectricityGatesAndNodesEngine? _electricityNodeGateEngine;

    // Central objects
    public CoreElements Elements;

    // Font 1
    public BitmapFont Font1;

    // Font 2
    public BitmapFont Font2;
    private GameScreenCollection? _gameScreens;

    //public MercuryParticleEngine ParticleEngine;
    public readonly GraphicsDeviceManager GraphicsDeviceMgr;

    // Game Sequences
    public IntroSequence IntroSequence;

    // Loading screen
    private int _loadingScreenFade;

    private MainGameScreen? _mainScreen;
    public Nasties Nasties;


    private GameWindow _parentWindow;
    private bool _particleTestTrigger;

    private Blob? _player;


    private int _powerUpType;

    /// <summary>
    ///     How the composed picture is put on the screen - the transform onto the back buffer, the
    ///     colours behind it, and where the game's own screens sit on it.
    /// </summary>
    public readonly PresentationSettings Presentation = new();

    public readonly Random RandomNumberGenerator;

    /// <summary>
    ///     Which of the two sets of artwork the game is drawn with, and the sheet names that go
    ///     with it. Switched by <see cref="SwitchGraphicsSetIfAsked" />; read live by everything
    ///     that draws.
    /// </summary>
    public readonly GraphicsSet GraphicsSet = new();

    public SpriteCollection Scanlines;

    private ScreenEffectEngine? _screenEffectEngine;

    // Test area
    public Shapes ShapeDrawer;


    // Tunnel lift animations
    private TunnelLiftAnimationEngine? _tunnelAnimations;

    // Scaling
    public GameEngine(
        GameWindow parentWindow,
        GraphicsDeviceManager graphics,
        ContentManager content)
    {
        _parentWindow = parentWindow;

        // Setup graphics device and content management
        GraphicsDeviceMgr = graphics;
        ContentMgr = content;
        Presentation.BackgroundColour = Color.Black;

        // Before the resource manager, which hands it to the sound engine.
        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);

        ContentResourceManager = new ResourceManager(content, graphics, RandomNumberGenerator, GraphicsSet);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;


        // Starting screen will be the title screen later.....

        DefineContent();
        Initialise();
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

        // Unbinding every movement key is how the options screen says "use the joypad",
        // and its joystick option unbinds all five - the four directions and fire.
        Player.BlobsControl.UseJoyPad =
            playerKeys.Up == Keys.None &&
            playerKeys.Down == Keys.None &&
            playerKeys.Left == Keys.None &&
            playerKeys.Right == Keys.None &&
            playerKeys.Fire == Keys.None;
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

    /// <summary>
    ///     The one engine, for the few sprites that need the graphics device before they are given
    ///     a reference to it. Only callable once the engine has been constructed.
    /// </summary>
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
    public ColourAnimationEngine ColourAnimationEngine => NotBeforeContentLoads(_colourAnimationEngine);

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
    public BlobDeathAnimation BlobKiller => NotBeforeContentLoads(_blobKiller);

    /// <inheritdoc cref="GameScreens" />
    public ConveyerNasties ConveyerNastiesInstance => NotBeforeContentLoads(_conveyerNastiesInstance);

    /// <inheritdoc cref="GameScreens" />
    public ElectricityGatesAndNodesEngine ElectricityNodeGateEngine => NotBeforeContentLoads(_electricityNodeGateEngine);

    /// <inheritdoc cref="GameScreens" />
    public ScreenEffectEngine ScreenEffectEngine => NotBeforeContentLoads(_screenEffectEngine);

    /// <inheritdoc cref="GameScreens" />
    private MainGameScreen MainScreen => NotBeforeContentLoads(_mainScreen);

    private static T NotBeforeContentLoads<T>(T? component, [CallerMemberName] string name = "")
        where T : class
        => component ?? throw new InvalidOperationException(
            $"'{name}' does not exist until the game's content has loaded.");

    private void Nasties_OnNastyInteractionWithBlob(NastyInteractionType interactionType, NastyType nastyType)
    {
        if (interactionType == NastyInteractionType.BlobShot)
        {
            BlobStatusPanel.PlayerScore = BlobStatusPanel.PlayerScore + ScoreForShootingNasty;
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

        if (action == BlobAction.GotPowerUp && data is int powerUpType)
        {
            _powerUpType = powerUpType;
        }

    }

    [MemberNotNull(nameof(IntroSequence), nameof(BlobStatusPanel), nameof(Elements),
        nameof(Nasties), nameof(ShapeDrawer))]
    private void Initialise()
    {
        // Sequences
        IntroSequence = new IntroSequence(this);

        BlobStatusPanel = new StatusPanel(this);
        BlobStatusPanel.Initialise(null);

        _session = new GameSession();
        _session.OnPausedChanged += Session_OnPausedChanged;
        _session.OnPlayerKeysChanged += Session_OnPlayerKeysChanged;

        // Core Elements
        Elements = new CoreElements(BlobStatusPanel, ContentResourceManager, GraphicsSet, Presentation, RandomNumberGenerator);

        // Nasties
        Nasties = new Nasties(this, NastiesPerScreen);
        Nasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        // Test area
        ShapeDrawer = new Shapes(GraphicsDeviceMgr.GraphicsDevice);
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
        nameof(Scanlines), nameof(Font1), nameof(Font2))]
    private void DefineContent()
    {
        DefineBootGraphics();

        Status = GameEngineStatus.StartingGraphicsLoaded;

        DefineFonts();

        DefineSharedGraphics();
        DefineScreenData();
        DefinePresentationScreenGraphics();
        DefineGameScreenTiles();
        DefineBlobGraphics();
        DefinePickUpAndNastyGraphics();
        DefineAccessBoothGraphics();
        DefineCheopsGraphics();
        DefineTeleporterGraphics();
        DefineElectricityGraphics();
        DefineOddSizedGraphics();
        DefineStatusPanelGraphics();
        DefineParticleEffects();
        DefineAnimationTiles();
        DefineSounds();
    }

    [MemberNotNull(nameof(_bootScreen), nameof(_bootProgressBackground), nameof(_bootProgress),
        nameof(Scanlines))]
    private void DefineBootGraphics()
    {
        _bootScreen = new SpriteCollection(GraphicsDeviceMgr, ContentMgr, @"Graphics\\BootScreen2", GameHost.ScreenWidth, GameHost.ScreenHeight);
        _bootProgressBackground = new SpriteCollection(GraphicsDeviceMgr, ContentMgr, @"Graphics\\ProgressBarBackground", 1046, 32);
        _bootProgress = new SpriteCollection(GraphicsDeviceMgr, ContentMgr, @"Graphics\\BootProgress", 1, 24);
        Scanlines = new SpriteCollection(GraphicsDeviceMgr, ContentMgr, @"Graphics\\Scanlines2", GameHost.ScreenWidth, GameHost.ScreenHeight);
    }

    [MemberNotNull(nameof(Font1), nameof(Font2))]
    private void DefineFonts()
    {
        Font1 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraSmall"), new SpriteBatch(GraphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
        Font2 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraMedium"), new SpriteBatch(GraphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);
    }

    private void DefineSharedGraphics()
    {
        ContentResourceManager.AddSpriteResource("Background1", "Graphics\\Background1", 1024, 576);
        //ContentResourceManager.AddSpriteResource("Background2", "Graphics\\Background2", 1024, 576);
        ContentResourceManager.AddSpriteResource("ProportionalNumbers", "Graphics\\ProportionalNumbers", 27, 26);
    }

    /// <summary>
    ///     Both resources read the same file, and the reader takes them in the order they are named here,
    ///     so these two must stay in this order and in this method.
    /// </summary>
    private void DefineScreenData()
    {
        ContentResourceManager.AddDataResource("GameScreens", "Data/SQScreenData.dat", typeof(GameScreenData[]));
        ContentResourceManager.AddDataResource("ScreenCollisionData", "Data/SQScreenData.dat", typeof(int[,,]));
    }

    private void DefinePresentationScreenGraphics()
    {
        ContentResourceManager.AddSpriteResource("OptionScreenBackground", "Graphics\\OptionsScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("OptionScreenForeground", "Graphics\\OptionsScreenForeground", 821, 554);

        ContentResourceManager.AddSpriteResource("ComputerReportScreenBackground", "Graphics\\ComputerReportScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("ComputerReportScreenForeground", "Graphics\\ComputerReportScreenForeground", 821, 554);

        ContentResourceManager.AddSpriteResource("GameOverScreenBackground", "Graphics\\GameOverScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("GameOverScreenForeground", "Graphics\\GameOverScreenForeground", 821, 554);

        ContentResourceManager.AddSpriteResource("HiScoreScreenBackground", "Graphics\\HiScoreScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("HiScoreScreenForeground", "Graphics\\HiScoreScreenForeground", 821, 554);

        ContentResourceManager.AddSpriteResource("GameCompleteScreenBackground", "Graphics\\GameCompleteScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("GameCompleteScreenForeground", "Graphics\\GameCompleteScreenForeground", 821, 554);
    }

    private void DefineGameScreenTiles()
    {
        ContentResourceManager.AddSpriteResource("UpperScreenTiles", "Graphics\\UpperScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("UpperScreenTilesOriginal", "Graphics\\UpperScreenTilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        ContentResourceManager.AddSpriteResource("LowerScreenTiles", "Graphics\\StartilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("LowerScreenTilesOriginal", "Graphics\\StartilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        ContentResourceManager.AddSpriteResource("LowerScreenTilesDark", "Graphics\\startilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("ExitShadows", "Graphics\\ExitShadows", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private void DefineBlobGraphics()
    {
        //ContentResourceManager.AddSpriteResource(gameEngine.GraphicsSet.Names.BlobSprites, "Graphics\\blobNew", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        //ContentResourceManager.AddSpriteResource(gameEngine.GraphicsSet.Names.BlobSprites, "Graphics\\blobNewAlt", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("BlobSprites", "Graphics\\blobNewAlt2", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("BlobSpritesOriginal", "Graphics\\BlobOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        ContentResourceManager.AddSpriteResource("BlobParts", "Graphics\\blobParts", 32, 32);
        ContentResourceManager.AddSpriteResource("BlobShuttle", "Graphics\\BlobShuttle", 64, 32);
        ContentResourceManager.AddSpriteResource("BlobShuttleOriginal", "Graphics\\BlobShuttleOriginal", 64, 32);

        ContentResourceManager.AddSpriteResource("BlobFire1", "Graphics\\BlobFire1", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("BlobFire2", "Graphics\\BlobFireNew2", 32, 32);
        ContentResourceManager.AddSpriteResource("BlobFire2Original", "Graphics\\BlobFire2Original", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        ContentResourceManager.AddSpriteResource("BlobFire2Overlay", "Graphics\\BlobFire2Overlay", 48, 48);
        ContentResourceManager.AddSpriteResource("JumpPlatforms", "Graphics\\BlobJumpPlatform", 64, 32);
        ContentResourceManager.AddSpriteResource("JumpPlatformsOriginal", "Graphics\\BlobJumpPlatformOriginal", 64, 32);
    }

    private void DefinePickUpAndNastyGraphics()
    {
        ContentResourceManager.AddSpriteResource("PowerUps", "Graphics\\PowerUps", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PowerUpsOriginal", "Graphics\\PowerUpsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("Pickups", "Graphics\\Pickups", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PickupsOriginal", "Graphics\\PickupsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        ContentResourceManager.AddSpriteResource("Nasties", "Graphics\\Nasties", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("NastiesOriginal", "Graphics\\NastiesOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private void DefineAccessBoothGraphics()
    {
        ContentResourceManager.AddSpriteResource("ScanLine", "Graphics\\ScanLine", 74, 9);

        ContentResourceManager.AddSpriteResource("AccessCodesFailed", "Graphics\\AccessCodesFailed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesPassed", "Graphics\\AccessCodesPassed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesGray", "Graphics\\AccessCodesGray", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PickupsIlluminated", "Graphics\\PickupsIlluminated", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        ContentResourceManager.AddSpriteResource("AccessCodesFailedOriginal", "Graphics\\AccessCodesFailedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesPassedOriginal", "Graphics\\AccessCodesPassedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesGrayOriginal", "Graphics\\AccessCodesGrayOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PickupsIlluminatedOriginal", "Graphics\\PickupsIlluminatedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private void DefineCheopsGraphics()
    {
        ContentResourceManager.AddSpriteResource("CheopsPyramid", "Graphics\\CheopsPyramid", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("CheopsPyramidOriginal", "Graphics\\CheopsPyramidOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private void DefineTeleporterGraphics()
    {
        ContentResourceManager.AddSpriteResource("TeleportInteriorLower", "Graphics\\TeleportInteriorLower", 1020, 543);
        ContentResourceManager.AddSpriteResource("TeleportInteriorUpper", "Graphics\\TeleportInteriorUpper", 654, 460);
        ContentResourceManager.AddSpriteResource("TeleporterKeyOverlays", "Graphics\\TeleporterKeyOverlays", 23, 23);
        ContentResourceManager.AddSpriteResource("TeleporterPowerSupply", "Graphics\\TeleporterPowerSupply", 246, 402);
    }

    private void DefineElectricityGraphics()
    {
        ContentResourceManager.AddSpriteResource("ElectricityNodeElectricity1", "Graphics\\ElectricityAnim", 70, 64);
        ContentResourceManager.AddSpriteResource("ElectricityNodeElectricity2", "Graphics\\ElectricityAnim", 70, 64);
        ContentResourceManager.AddSpriteResource("NodeLights", "Graphics\\NodeLight", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("GateLights", "Graphics\\GateLight", 32, 32);
        ContentResourceManager.AddSpriteResource("ElectricityOriginal", "Graphics\\ElectricityOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private void DefineOddSizedGraphics()
    {
        ContentResourceManager.AddSpriteResource("OllyBackground", "Graphics\\OllyBackground", 256, 192);
        ContentResourceManager.AddSpriteResource("OllyLower", "Graphics\\OllyLower", 256, 192);
        ContentResourceManager.AddSpriteResource("OllyUpper", "Graphics\\OllyUpper", 125, 120);
        ContentResourceManager.AddSpriteResource("OllyEyes", "Graphics\\OllyEyes", 44, 8);
        ContentResourceManager.AddSpriteResource("OllyOriginal", "Graphics\\OllyOriginal", 256, 192);

        ContentResourceManager.AddSpriteResource("BlobsShipOriginal", "Graphics\\BlobsSpaceShipOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("BlobsShip", "Graphics\\BlobsSpaceShip", 156, 128);
        ContentResourceManager.AddSpriteResource("BlobsShipHazardLights", "Graphics\\BlobsSpaceShipHazardLights", 156, 128);
        ContentResourceManager.AddSpriteResource("BlobsShipHeadLights", "Graphics\\BlobsSpaceShipHeadLights", 156, 128);


        ContentResourceManager.AddSpriteResource("BlankingTile", "Graphics\\BlankingTile", TileMetrics.TileWidth, TileMetrics.TileHeight);

        ContentResourceManager.AddSpriteResource("BreakableBridge", "Graphics\\BreakableBridge", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("BreakableBridgeOriginal", "Graphics\\BreakableBridgeOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private void DefineStatusPanelGraphics()
    {
        ContentResourceManager.AddSpriteResource("StatusPanelGraphic", "Graphics\\StatusPanelNew", 1020, 181);
        ContentResourceManager.AddSpriteResource("EnerygyBar", "Graphics\\EnergyBar", 256, 30);
        ContentResourceManager.AddSpriteResource("JumpBar", "Graphics\\JumpBar", 240, 30);
        ContentResourceManager.AddSpriteResource("AmmoBar", "Graphics\\AmmoBar", 240, 30);
        ContentResourceManager.AddSpriteResource("PanelNumbers", "Graphics\\PanelNumbers", 29, 29);
        ContentResourceManager.AddSpriteResource("StatusPanelShadow", "Graphics\\StatusPanelShadow", 1020, 50);
        ContentResourceManager.AddSpriteResource("StatusPanelOverlay", "Graphics\\StatusPanelOverlay", 966, 135);

        ContentResourceManager.AddSpriteResource("ParticleSprite", "Graphics\\BouncingParticle", 32, 32);
        ContentResourceManager.AddSpriteResource("CollisionTile", "Graphics\\CollisionTile", TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight);
    }

    private void DefineParticleEffects()
    {
        ContentResourceManager.AddParticleResource("BeamMeUp_Small", "ParticleEffects\\BeamMeUp_Small");
        ContentResourceManager.AddParticleResource("FireTrail", "ParticleEffects\\FireTrail");

        ContentResourceManager.AddParticleResource("Fireball", "ParticleEffects\\Fireball");
        ContentResourceManager.AddParticleResource("NastyDies", "ParticleEffects\\NastyDies");
        ContentResourceManager.AddParticleResource("NastySplat", "ParticleEffects\\NastySplat");

        ContentResourceManager.AddParticleResource("BinaryEffect", "ParticleEffects\\BinaryEffect");
        ContentResourceManager.AddParticleResource("BasicSmokeSmall", "ParticleEffects\\BasicSmokeSmall");

        ContentResourceManager.AddParticleResource("BasicSmokePlume", "ParticleEffects\\BasicSmokePlume");
        ContentResourceManager.AddParticleResource("BasicSmokePlume2", "ParticleEffects\\BasicSmokePlume2");
        ContentResourceManager.AddParticleResource("Sparks", "ParticleEffects\\Sparks");

        ContentResourceManager.AddParticleResource("CoreSparks", "ParticleEffects\\CoreSparks");
        ContentResourceManager.AddParticleResource("CoreReplacementParticleEffect", "ParticleEffects\\CoreReplacementParticleEffect");
        ContentResourceManager.AddParticleResource("FireSparks", "ParticleEffects\\FireSparks");

        ContentResourceManager.AddParticleResource("ForceField", "ParticleEffects\\ForceField");
        ContentResourceManager.AddParticleResource("BlobFragmentExplosion", "ParticleEffects\\BlobFragmentExplosion");
    }

    private void DefineAnimationTiles()
    {
        ContentResourceManager.AddAnimatedSpriteResource("BreakableBridges", "Graphics\\BreakableBridgeAnim", TileMetrics.TileWidth, 8, (int)ActionTiles.BreakableBridges, 4);
        ContentResourceManager.AddAnimatedSpriteResource("TeleporterPadsOccupied", "Graphics\\TeleporterPadAnimationOccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.ShuttlePlatform, 8);
        ContentResourceManager.AddAnimatedSpriteResource("TeleporterPadsUnoccupied", "Graphics\\TeleporterPadAnimationUnoccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.RemovedShuttlePlatform, 1);
    }

    private void DefineSounds()
    {
        ContentResourceManager.AddSoundResource("ScanCode", "Sounds\\ScanCode", SoundEffectInstanceType.Multi, 4, false);
        ContentResourceManager.AddSoundResource("AccessSuccess", "Sounds\\AccessSuccess", SoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("AccessFailed", "Sounds\\AccessFailed", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("MachineryBackground", "Sounds\\MachineryBackground", SoundEffectInstanceType.Single, 0, true);

        ContentResourceManager.AddSoundResource("Tunnel", "Sounds\\Tunnel", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("TunnelSwish1", "Sounds\\TunnelSwish1", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("TunnelSwish2", "Sounds\\TunnelSwish2", SoundEffectInstanceType.Single, 0, false);

        ContentResourceManager.AddSoundResource("NastyDeath", "Sounds\\NastyDeath", SoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobLazer", "Sounds\\BlobLazer", SoundEffectInstanceType.Multi, 2, true);
        ContentResourceManager.AddSoundResource("ElectricityNodes", "Sounds\\ElectricityNodes", SoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BridgeBreak", "Sounds\\BridgeBreak", SoundEffectInstanceType.Single, 0, true);
        ContentResourceManager.AddSoundResource("BlobRun", "Sounds\\BlobRun", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobFalls", "Sounds\\BlobFalls", SoundEffectInstanceType.Single, 0, true);
        ContentResourceManager.AddSoundResource("TunnelLift", "Sounds\\TunnelLift", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("NastyAppear", "Sounds\\NastyAppear", SoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("Rumble", "Sounds\\Rumble", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobJumps", "Sounds\\BlobJump", SoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobJumpsNoPlatforms", "Sounds\\BlobJumpNoPlatforms", SoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobPickUp", "Sounds\\BlobPickUp", SoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobOverloadAlarm", "Sounds\\BlobOverloadAlarm", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobShipFall", "Sounds\\BlobShipFall", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobShipExplodes", "Sounds\\BlobShipExplodes", SoundEffectInstanceType.Single, 0, false);

        ContentResourceManager.AddSoundResource("BlobExplodes", "Sounds\\BlobExplodes", SoundEffectInstanceType.Multi, 4, false);
        ContentResourceManager.AddSoundResource("BlobPartClang", "Sounds\\BlobPartClang", SoundEffectInstanceType.Multi, 8, false);

        ContentResourceManager.AddSoundResource("NastyAppears", "Sounds\\BlobExplodes", SoundEffectInstanceType.Multi, 4, false);
        ContentResourceManager.AddSoundResource("TeleportKeyClick", "Sounds\\TeleportKeyClick", SoundEffectInstanceType.Multi, 5, false);
        ContentResourceManager.AddSoundResource("TeleportInvalidCode", "Sounds\\TeleportInvalidCode", SoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("Teleport", "Sounds\\Teleport", SoundEffectInstanceType.Multi, 3, false);

        ContentResourceManager.AddSoundResource("PowerUp1", "Sounds\\PowerUp1", SoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("PowerUp2", "Sounds\\PowerUp2", SoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("PowerUp3", "Sounds\\PowerUp3", SoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("LifePowerUp", "Sounds\\LifePowerUp", SoundEffectInstanceType.Multi, 2, false);

        ContentResourceManager.AddSoundResource("BackgroundSound1", "Sounds\\BackgroundSound1", SoundEffectInstanceType.Single, 0, false);
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

        // The breakable bridges last: each bridge composes itself into a render target and then
        // asks the screen effect engine for the view port to draw it through, and that engine is
        // what RegisterScreenEffectViewPorts has only just built.
        _breakableBridgesEngine = new BreakableBridges(
            ContentResourceManager,
            GameScreens,
            GraphicsDeviceMgr,
            GraphicsSet,
            RandomNumberGenerator,
            ScreenEffectEngine);

        ContentResourceManager.Initialise();

        UpdateStatus("Content loaded", FullyLoadedPercent, GameEngineStatus.StartGame);
    }

    /// <summary>
    ///     The blob, the screen collection and the colour animation engine all need resources that do not
    ///     exist until content loads, which is why none of them is built in the constructor.
    /// </summary>
    private void BuildComponentsNeedingLoadedContent()
    {
        // Player (Blob). Built here rather than in Initialise because the three particle
        // effects he carries are resources, and those do not exist until content loads
        _player = new Blob(this);
        Player.OnScreenChanged += Player_OnScreenChanged;
        Player.OnBlobActionRaised += Player_OnBlobActionRaised;

        // Game Screens. Built here rather than in the constructor because the screen data and
        // the collision map it needs are resources, and those do not exist until content loads
        _gameScreens =
            new GameScreenCollection(
                this,
                TileMetrics.TilesAcross,
                TileMetrics.TilesDown,
                0,
                0,
                TileMetrics.CollisionCellWidth,
                TileMetrics.CollisionCellHeight);

        _colourAnimationEngine = new ColourAnimationEngine(ContentResourceManager, GameScreens, GraphicsSet);

        // Tunnel lift animations. After the screen collection, which it reads the lift shafts from.
        _tunnelAnimations = new TunnelLiftAnimationEngine(ContentResourceManager, GameScreens, GraphicsSet);

        // The blob's death throes. After the screen collection, which each thrown-off fragment
        // bounces around.
        _blobKiller = new BlobDeathAnimation(ContentResourceManager, GameScreens, GraphicsSet, RandomNumberGenerator);

        Presentation.ScreenXOffset = (GraphicsDeviceMgr.PreferredBackBufferWidth - GameScreens.ScreenWidth) / 2;
        Presentation.ScreenYOffset = 10;

        ConveyerNastiesInstance.Initialise(null);
        // The conveyer nasties and the electricity engine. Both chase the blob and read the
        // planet, so both are built here rather than in Initialise, where neither existed yet.
        _conveyerNastiesInstance = new ConveyerNasties(
            ContentResourceManager,
            ContentMgr,
            GameScreens,
            GraphicsDeviceMgr,
            Player,
            RandomNumberGenerator);
        ConveyerNastiesInstance.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        var converyerNastyGraphics = new ContentList(ContentMgr);
        converyerNastyGraphics.AddContent("conveyerNasty", "Graphics\\ConveryerNasty");
        ConveyerNastiesInstance.LoadContent(converyerNastyGraphics);

        _electricityNodeGateEngine = new ElectricityGatesAndNodesEngine(
            ContentResourceManager,
            GameScreens,
            GraphicsSet,
            Player,
            RandomNumberGenerator,
            ConveyerNastiesInstance);
        ElectricityNodeGateEngine.OnBlobStruckByElectricity += ElectricityNodeGateEngine_OnBlobStruckByElectricity;
    }

    /// <summary>
    ///     The tiles that pulse, and how each one pulses. A long literal list, and nothing but a list.
    /// </summary>
    private void RegisterColourAnimations()
    {
        ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.TeleportBooth,
            UpperTileToDraw = 7,
            Intensity = 4,
            TransitionColour = new Color(0, 0, ColourLevels.Full),
            Speed = 8
        }); // Teleport booths
        ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.AccessBoothLeft,
            UpperTileToDraw = 9,
            Intensity = 2,
            TransitionColour = new Color(0, ColourLevels.Full, 0),
            Speed = 32
        }); // Access tunnel left
        ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.AccessBoothRight,
            UpperTileToDraw = 10,
            Intensity = 2,
            TransitionColour = new Color(ColourLevels.Full, 0, 0),
            Speed = 32
        }); // Access tunnel right
        ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.ShuttleTag1,
            UpperTileToDraw = -1,
            Intensity = 2,
            TransitionColour = new Color(ColourLevels.Full, ColourLevels.Full, 0),
            Speed = 16
        }); // Yellow teleport tag
        ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
        {
            TileNo = (int)ActionTiles.ShuttleTag3,
            UpperTileToDraw = -1,
            Intensity = 6,
            TransitionColour = new Color(ColourLevels.Full, 0, 0),
            Speed = 16
        }); // Red teleport tag
        ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
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

        ScreenDirectory.Add(GameScreenType.Options, new OptionsScreen(this));
        ScreenDirectory.Add(GameScreenType.ComputerReport, new ComputerReportScreen(this));
        ScreenDirectory.Add(GameScreenType.HiScores, new HiScoreScreen(this));
        ScreenDirectory.Add(GameScreenType.GameOver, new GameOverScreen(this));
        ScreenDirectory.Add(GameScreenType.GameComplete, new GameCompleteScreen(this));

        ScreenDirectory.Add(GameScreenType.MainGame, _mainScreen);
        ScreenDirectory.Add(GameScreenType.Teleporter, new TeleportScreen(this));
        ScreenDirectory.Add(GameScreenType.PlanetCore, new PlanetCoreScreen(this, Elements));
        ScreenDirectory.Add(GameScreenType.AccessBooth, new AccessBoothScreen(this));
        ScreenDirectory.Add(GameScreenType.Cheops, new CheopsScreen(this));

        foreach (var screen in ScreenDirectory.All)
        {
            ((GameComponentBase)screen).LoadContent();
        }
    }

    private void RegisterScreenEffectViewPorts()
    {
        _screenEffectEngine = new ScreenEffectEngine(GameHost.ScreenWidth, GameHost.ScreenHeight, GraphicsDeviceMgr, Presentation);

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
            GameHost.ScreenWidth,
            GameHost.ScreenHeight);
    }

    private void LoadContent()
    {
        ContentResourceManager.LoadContent();
        BlobKiller.LoadContent();
    }

    // Resets the game back to defaults - need to sort all of this out!
    public void Reset()
    {
        MainScreen.Reset(null);
    }

    public void Update(GameTime gameTime)
    {
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

        // Wait for a keypress before leaving the loading screen
        if (ToggleKeys.GetPressedKey() != Keys.None)
        {
            ContentResourceManager_OnGameComponentStatusChanged("Content loaded", FullyLoadedPercent, GameEngineStatus.Started);
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
        if (ScreenDirectory.Current == GameScreenType.Cheops ||
            ScreenDirectory.Current == GameScreenType.MainGame ||
            ScreenDirectory.Current == GameScreenType.PlanetCore ||
            ScreenDirectory.Current == GameScreenType.AccessBooth)
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
        GraphicsSet.Switch();

        // The original graphics have no backdrops drawn for them.
        Presentation.DrawBackgrounds = GraphicsSet.IsRedrawn;

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
            TestColourAnimations();
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
                if (GameScreens.ScreenNumber + WorldMap.ScreensAcross <= WorldMap.ScreenCount)
                {
                    GameScreens.ScreenNumber += WorldMap.ScreensAcross;
                    ScreenDirectory.Get(ScreenDirectory.Current).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                    Thread.Sleep(TestModeScreenStepDelayMs);
                }
            }
    }

    private void TestColourAnimations()
    {
            if (ToggleKeys.GetPressedKey() == Keys.K)
            {
                ColourAnimationEngine.Start((int)ActionTiles.ShuttleTag1, -1);
                ColourAnimationEngine.Start((int)ActionTiles.ShuttleTag3, -1);
                ColourAnimationEngine.Start((int)ActionTiles.ShuttleTag2, -1);
            }

            if (ToggleKeys.GetPressedKey() == Keys.L)
            {
                ColourAnimationEngine.Stop((int)ActionTiles.ShuttleTag1);
                ColourAnimationEngine.Stop((int)ActionTiles.ShuttleTag3);
                ColourAnimationEngine.Stop((int)ActionTiles.ShuttleTag2);
            }
    }

    private void UpdateActiveScreen(GameTime gameTime)
    {
        var screenToUpdate = ScreenDirectory.CurrentScreen;

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
        if (ScreenDirectory.Current == GameScreenType.Cheops ||
            ScreenDirectory.Current == GameScreenType.MainGame ||
            ScreenDirectory.Current == GameScreenType.PlanetCore ||
            ScreenDirectory.Current == GameScreenType.Teleporter ||
            ScreenDirectory.Current == GameScreenType.AccessBooth)
        {
            BlobStatusPanel.Update(Player.BlobInventory);
        }
    }

    private void UpdateScreensLeftRunningOffScreen(IGameScreen screenToUpdate, GameTime gameTime)
    {
            foreach (var igs in ScreenDirectory.All)
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
        var backBuffer = GraphicsDeviceMgr.GraphicsDevice.PresentationParameters;

        Presentation.Recalculate(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
    }

    /// <summary>
    ///     Go fullscreen at the resolution the desktop is already running at, so the picture is
    ///     composed for the display it lands on instead of being stretched up from the windowed
    ///     size. The game itself never changes size: it always composes at
    ///     <see cref="GameHost.ScreenWidth" /> x <see cref="GameHost.ScreenHeight" /> and
    ///     Presentation.Transform scales that onto whatever back buffer we end up with.
    ///     HardwareModeSwitch has to be on. Borderless (the false case) looks like the friendlier
    ///     option, but on macOS it produces a window clamped to the desktop area *below* the menu
    ///     bar - a back buffer 39 points short, sat 39 points down the screen, with the picture
    ///     centred inside that rather than inside the display.
    /// </summary>
    private void ToggleFullScreen()
    {
        GraphicsDeviceMgr.HardwareModeSwitch = true;

        if (GraphicsDeviceMgr.IsFullScreen)
        {
            GraphicsDeviceMgr.PreferredBackBufferWidth = GameHost.ScreenWidth;
            GraphicsDeviceMgr.PreferredBackBufferHeight = GameHost.ScreenHeight;
        }
        else
        {
            var desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            GraphicsDeviceMgr.PreferredBackBufferWidth = desktop.Width;
            GraphicsDeviceMgr.PreferredBackBufferHeight = desktop.Height;
        }

        GraphicsDeviceMgr.IsFullScreen = !GraphicsDeviceMgr.IsFullScreen;
        GraphicsDeviceMgr.ApplyChanges();
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
            GraphicsDeviceMgr.GraphicsDevice.Clear(Presentation.ScreenClearColour);

            var screenToDraw = ScreenDirectory.CurrentScreen;
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
            var backBuffer = GraphicsDeviceMgr.GraphicsDevice.PresentationParameters;

            var vp = new Viewport();
            vp.X = 0;
            vp.Y = 0;
            vp.Width = backBuffer.BackBufferWidth;
            vp.Height = backBuffer.BackBufferHeight;

            GraphicsDeviceMgr.GraphicsDevice.Viewport = vp;
            GraphicsDeviceMgr.GraphicsDevice.Clear(Presentation.ScreenClearColour);


            if (Status == GameEngineStatus.Starting ||
                Status == GameEngineStatus.StartingGraphicsLoaded ||
                Status == GameEngineStatus.ContentLoaded)
            {
                _bootScreen.Begin(BlendState.Opaque, gameTime, Presentation.Transform);
                _bootScreen.DrawSpriteUnscaled(0, 0, 0, _loadingScreenFade);
                _bootScreen.End();

                _bootProgressBackground.Begin(BlendState.AlphaBlend, gameTime, Presentation.Transform);
                _bootProgressBackground.DrawSpriteUnscaled(0, BootProgressBackgroundXPos, BootProgressBackgroundYPos, _bootProgressBackgroundFade);
                _bootProgressBackground.End();

                // Draw the status percentage
                _bootProgress.Begin(BlendState.AlphaBlend, gameTime, Presentation.Transform);
                var percentBarWidth = (int)(BootProgressBarSlices / (double)FullyLoadedPercent * PercentComplete);

                for (var a = 0; a < percentBarWidth; a++)
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
    }
    
    public void Dispose()
    {
        ContentResourceManager.GetSoundFxEngine().Dispose();
    }
}