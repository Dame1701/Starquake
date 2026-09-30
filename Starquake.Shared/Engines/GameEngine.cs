// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
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
using Starquake.GameComponents.GameScreens;
using Starquake.GameComponents.PresentationScreens;
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

    private static GameEngine _sInstance;
    public string AccessCodesFailedName = "AccessCodesFailed";
    public string AccessCodesGreyName = "AccessCodesGray";
    public string AccessCodesPassedName = "AccessCodesPassed";
    private GameComponentBase _accessScreen;

    public Color BackgroundColour;

    public BlobDeathAnimation BlobKiller;
    public string BlobSpritesName = "BlobSprites";
    public string BlobsShipName = "BlobsShip";

    public StatusPanel BlobStatusPanel;
    private SpriteCollection _bootProgress;
    private SpriteCollection _bootProgressBackground;
    private int _bootProgressBackgroundFade = ColourLevels.Full;
    // Boot graphics
    private SpriteCollection _bootScreen;

    // Animation tiles
    public SpriteCollection BreakableBridgeAnim;
    public BreakableBridges BreakableBridgesEngine;
    private GameComponentBase _cheopsScreen;

    // Test
    public SpriteCollection CollisionTile;

    // Colour Animation Engine
    public ColourAnimationEngine ColourAnimationEngine;
    private GameComponentBase _computerReportScreen;
    public readonly ContentManager ContentMgr;

    public readonly ResourceManager ContentResourceManager;
    public ConveyerNasties ConveyerNastiesInstance;
    private GameComponentBase _coreScreen;
    public GameScreenType CurrentGameScreen;

    public bool DrawBackgrounds = true;
    public ElectricityGatesAndNodesEngine ElectricityNodeGateEngine;

    // Central objects
    public CoreElements Elements;

    // Font 1
    public BitmapFont Font1;

    // Font 2
    public BitmapFont Font2;
    private GameComponentBase _gameCompleteScreen;
    private GameComponentBase _gameOverScreen;
    public GameScreenCollection GameScreens;

    //public MercuryParticleEngine ParticleEngine;
    public readonly GraphicsDeviceManager GraphicsDeviceMgr;
    private GameComponentBase _hiScoreScreen;

    public readonly bool InTestMode = true;

    // Game Sequences
    public IntroSequence IntroSequence;

    // Loading screen
    private int _loadingScreenFade;

    public string LowerScreenTilesName = "LowerScreenTiles";

    // Initialise Game Components
    private GameComponentBase _mainScreen;
    public Nasties Nasties;

    private GameComponentBase _optionsScreen;

    private GameWindow _parentWindow;
    private bool _particleTestTrigger;

    private bool _paused;
    public string PickupsIlluminatedName = "PickupsIlluminated";
    public string PickupsName = "Pickups";
    public Blob Player;

    private PlayerKeySet _playerKeys;
    public string PowerUpsName = "PowerUps";

    private int _powerUpType;

    /// <summary>
    ///     The game is composed at a fixed <see cref="GameHost.ScreenWidth" /> by
    ///     <see cref="GameHost.ScreenHeight" /> and only scaled onto the back buffer as the very
    ///     last step, so nothing in the game has to know or care whether it is in a small window,
    ///     a dragged-out one, or fullscreen on a 6K display. The scale is uniform so the picture
    ///     is never stretched, and whatever is left over becomes an even border - top and bottom,
    ///     or left and right. Recalculated every frame rather than hung off a resize event, so a
    ///     fullscreen toggle, a window drag and a change of display all just work.
    /// </summary>
    public Matrix PresentationTransform = Matrix.Identity;

    public readonly Random RandomNumberGenerator;
    public SpriteCollection Scanlines;
    public bool ScanlinesEnabled = true;

    public Color ScreenClearColour = Color.Black;

    // Screen Effect engine
    public ScreenEffectEngine ScreenEffectEngine;
    private readonly Dictionary<GameScreenType, IGameScreen> _screens;
    public int ScreenXOffset;
    public int ScreenYOffset;

    // Test area
    public Shapes ShapeDrawer;

    // Fonts
    public ImageFont SqText;
    private GameComponentBase _teleportScreen;
    public CoreElement TestElement1;
    private GameComponentBase _testScreen;

    // Tunnel lift animations
    public TunnelLiftAnimationEngine TunnelAnimations;
    public string UpperScreenTilesName = "UpperScreenTiles";

    public bool UsingOldGraphics;

    // Scaling
    public GameEngine(
        GameWindow parentWindow,
        GraphicsDeviceManager graphics,
        ContentManager content)
    {
        _sInstance = this;

        _parentWindow = parentWindow;

        Paused = false;

        // Setup graphics device and content management
        GraphicsDeviceMgr = graphics;
        ContentMgr = content;
        BackgroundColour = Color.Black;

        ContentResourceManager = new ResourceManager(this);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;

        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);
        _screens = new Dictionary<GameScreenType, IGameScreen>();

        // Starting screen will be the title screen later.....
        CurrentGameScreen = GameScreenType.Options;

        // Player keys
        _playerKeys = new PlayerKeySet();
        _playerKeys.Up = Keys.Up;
        _playerKeys.Down = Keys.Down;
        _playerKeys.Left = Keys.Left;
        _playerKeys.Right = Keys.Right;
        _playerKeys.Fire = Keys.LeftControl;
    }

    public PlayerKeySet PlayerKeys
    {
        get => _playerKeys;
        set
        {
            _playerKeys = value;
            if (Player is { BlobsControl: not null })
            {
                Player.BlobsControl.PlayerKeySet = value;

                if (Player.BlobsControl.PlayerKeySet.Up == Keys.None &&
                    Player.BlobsControl.PlayerKeySet.Down == Keys.None &&
                    Player.BlobsControl.PlayerKeySet.Left == Keys.None &&
                    Player.BlobsControl.PlayerKeySet.Right == Keys.None &&
                    Player.BlobsControl.PlayerKeySet.Left == Keys.None)
                {
                    Player.BlobsControl.UseJoyPad = true;
                }
                else
                {
                    Player.BlobsControl.UseJoyPad = false;
                }
            }
        }
    }

    public bool Paused
    {
        get => _paused;
        set
        {
            _paused = value;
            if (_paused)
            {
                if (ContentResourceManager != null)
                {
                    ContentResourceManager.GetSoundFxEngine().DisablePeriodicBackgroundSounds(true);
                }
            }
            else
            {
                if (ContentResourceManager != null)
                {
                    ContentResourceManager.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
                }
            }
        }
    }

    public static GameEngine GetInstance()
        => _sInstance;

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

    private void ElectricityNodeGateEngine_OnBlobStruckByElectricity(object sender, EventArgs e)
    {
        Player.KillBlob(false, false);
    }

    private void Player_OnScreenChanged(GameScreenType triggerScreen)
    {
        ((MainGameScreen)_mainScreen).PerformPostScreenChangeActions(triggerScreen, null);
    }

    private void Player_OnBlobActionRaised(BlobAction action, object data)
    {
        // inform status panel that something has happened
        BlobStatusPanel.BlobHasPerformedAnAction(action, data);

        if (action == BlobAction.GotPowerUp)
        {
            _powerUpType = (int)data;
        }

    }

    public void Initialise()
    {
        // Sequences
        IntroSequence = new IntroSequence(this);

        BlobStatusPanel = new StatusPanel(this);
        BlobStatusPanel.Initialise(null);

        // Core Elements
        Elements = new CoreElements(this);

        // Nasties
        Nasties = new Nasties(this, NastiesPerScreen);
        Nasties.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        // Conveyer nasties
        ConveyerNastiesInstance = new ConveyerNasties(this);
        ConveyerNastiesInstance.OnNastyInteractionWithBlob += Nasties_OnNastyInteractionWithBlob;

        // Nodes and gates engine
        ElectricityNodeGateEngine = new ElectricityGatesAndNodesEngine(this, ConveyerNastiesInstance);
        ElectricityNodeGateEngine.OnBlobStruckByElectricity += ElectricityNodeGateEngine_OnBlobStruckByElectricity;

        // Animations
        TunnelAnimations = new TunnelLiftAnimationEngine(this);

        // Test area
        ShapeDrawer = new Shapes(GraphicsDeviceMgr.GraphicsDevice);
    }

    public void DefineContent()
    {
        // Load the boot image
        _bootScreen = new SpriteCollection(this, ContentMgr, @"Graphics\\BootScreen2", GameHost.ScreenWidth, GameHost.ScreenHeight);
        _bootProgressBackground = new SpriteCollection(this, ContentMgr, @"Graphics\\ProgressBarBackground", 1046, 32);
        _bootProgress = new SpriteCollection(this, ContentMgr, @"Graphics\\BootProgress", 1, 24);
        Scanlines = new SpriteCollection(this, ContentMgr, @"Graphics\\Scanlines2", GameHost.ScreenWidth, GameHost.ScreenHeight);

        Status = GameEngineStatus.StartingGraphicsLoaded;

        // Font 1
        Font1 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraSmall"), new SpriteBatch(GraphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);

        // Font 2
        Font2 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraMedium"), new SpriteBatch(GraphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full), 12);

        // Blob's death effect
        BlobKiller = new BlobDeathAnimation(this);

        // Backgrounds
        ContentResourceManager.AddSpriteResource("Background1", "Graphics\\Background1", 1024, 576);
        //ContentResourceManager.AddSpriteResource("Background2", "Graphics\\Background2", 1024, 576);

        // Proportional Numbers
        ContentResourceManager.AddSpriteResource("ProportionalNumbers", "Graphics\\ProportionalNumbers", 27, 26);

        // Game data, if these hit the same file they must be presented in order
        ContentResourceManager.AddDataResource("GameScreens", "Data/SQScreenData.dat", typeof(GameScreenData[]));
        ContentResourceManager.AddDataResource("ScreenCollisionData", "Data/SQScreenData.dat", typeof(int[,,]));

        // Option screen graphics
        ContentResourceManager.AddSpriteResource("OptionScreenBackground", "Graphics\\OptionsScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("OptionScreenForeground", "Graphics\\OptionsScreenForeground", 821, 554);

        // Computer report screen graphics
        ContentResourceManager.AddSpriteResource("ComputerReportScreenBackground", "Graphics\\ComputerReportScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("ComputerReportScreenForeground", "Graphics\\ComputerReportScreenForeground", 821, 554);

        // Gameover screen graphics
        ContentResourceManager.AddSpriteResource("GameOverScreenBackground", "Graphics\\GameOverScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("GameOverScreenForeground", "Graphics\\GameOverScreenForeground", 821, 554);

        // HiScore screen graphics
        ContentResourceManager.AddSpriteResource("HiScoreScreenBackground", "Graphics\\HiScoreScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("HiScoreScreenForeground", "Graphics\\HiScoreScreenForeground", 821, 554);

        // Game Complete screen graphics
        ContentResourceManager.AddSpriteResource("GameCompleteScreenBackground", "Graphics\\GameCompleteScreenBackground", GameHost.ScreenWidth, GameHost.ScreenHeight);
        ContentResourceManager.AddSpriteResource("GameCompleteScreenForeground", "Graphics\\GameCompleteScreenForeground", 821, 554);

        // Game screen graphics
        ContentResourceManager.AddSpriteResource("UpperScreenTiles", "Graphics\\UpperScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("UpperScreenTilesOriginal", "Graphics\\UpperScreenTilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        ContentResourceManager.AddSpriteResource("LowerScreenTiles", "Graphics\\StartilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("LowerScreenTilesOriginal", "Graphics\\StartilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        ContentResourceManager.AddSpriteResource("LowerScreenTilesDark", "Graphics\\startilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        ContentResourceManager.AddSpriteResource("ExitShadows", "Graphics\\ExitShadows", TileMetrics.TileWidth, TileMetrics.TileHeight);

        // Blob's graphics
        //ContentResourceManager.AddSpriteResource(gameEngine.BlobSpritesName, "Graphics\\blobNew", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        //ContentResourceManager.AddSpriteResource(gameEngine.BlobSpritesName, "Graphics\\blobNewAlt", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
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

        // PickUps
        ContentResourceManager.AddSpriteResource("PowerUps", "Graphics\\PowerUps", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PowerUpsOriginal", "Graphics\\PowerUpsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("Pickups", "Graphics\\Pickups", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PickupsOriginal", "Graphics\\PickupsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        // Nasties
        ContentResourceManager.AddSpriteResource("Nasties", "Graphics\\Nasties", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("NastiesOriginal", "Graphics\\NastiesOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        // Access booth graphics
        ContentResourceManager.AddSpriteResource("ScanLine", "Graphics\\ScanLine", 74, 9);

        ContentResourceManager.AddSpriteResource("AccessCodesFailed", "Graphics\\AccessCodesFailed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesPassed", "Graphics\\AccessCodesPassed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesGray", "Graphics\\AccessCodesGray", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PickupsIlluminated", "Graphics\\PickupsIlluminated", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        ContentResourceManager.AddSpriteResource("AccessCodesFailedOriginal", "Graphics\\AccessCodesFailedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesPassedOriginal", "Graphics\\AccessCodesPassedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("AccessCodesGrayOriginal", "Graphics\\AccessCodesGrayOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("PickupsIlluminatedOriginal", "Graphics\\PickupsIlluminatedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        // Cheops pyramid
        ContentResourceManager.AddSpriteResource("CheopsPyramid", "Graphics\\CheopsPyramid", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("CheopsPyramidOriginal", "Graphics\\CheopsPyramidOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        // Teleporter Interior
        ContentResourceManager.AddSpriteResource("TeleportInteriorLower", "Graphics\\TeleportInteriorLower", 1020, 543);
        ContentResourceManager.AddSpriteResource("TeleportInteriorUpper", "Graphics\\TeleportInteriorUpper", 654, 460);
        ContentResourceManager.AddSpriteResource("TeleporterKeyOverlays", "Graphics\\TeleporterKeyOverlays", 23, 23);
        ContentResourceManager.AddSpriteResource("TeleporterPowerSupply", "Graphics\\TeleporterPowerSupply", 246, 402);

        // ElectricityGatesAndNodes
        ContentResourceManager.AddSpriteResource("ElectricityNodeElectricity1", "Graphics\\ElectricityAnim", 70, 64);
        ContentResourceManager.AddSpriteResource("ElectricityNodeElectricity2", "Graphics\\ElectricityAnim", 70, 64);
        ContentResourceManager.AddSpriteResource("NodeLights", "Graphics\\NodeLight", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        ContentResourceManager.AddSpriteResource("GateLights", "Graphics\\GateLight", 32, 32);
        ContentResourceManager.AddSpriteResource("ElectricityOriginal", "Graphics\\ElectricityOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        // Odd sized graphics
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

        // Status Panel
        ContentResourceManager.AddSpriteResource("StatusPanelGraphic", "Graphics\\StatusPanelNew", 1020, 181);
        ContentResourceManager.AddSpriteResource("EnerygyBar", "Graphics\\EnergyBar", 256, 30);
        ContentResourceManager.AddSpriteResource("JumpBar", "Graphics\\JumpBar", 240, 30);
        ContentResourceManager.AddSpriteResource("AmmoBar", "Graphics\\AmmoBar", 240, 30);
        ContentResourceManager.AddSpriteResource("PanelNumbers", "Graphics\\PanelNumbers", 29, 29);
        ContentResourceManager.AddSpriteResource("StatusPanelShadow", "Graphics\\StatusPanelShadow", 1020, 50);
        ContentResourceManager.AddSpriteResource("StatusPanelOverlay", "Graphics\\StatusPanelOverlay", 966, 135);

        ContentResourceManager.AddSpriteResource("ParticleSprite", "Graphics\\BouncingParticle", 32, 32);
        ContentResourceManager.AddSpriteResource("CollisionTile", "Graphics\\CollisionTile", TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight);

        // Add Particle Effects
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

        // Animation Tiles
        ContentResourceManager.AddAnimatedSpriteResource("BreakableBridges", "Graphics\\BreakableBridgeAnim", TileMetrics.TileWidth, 8, (int)ActionTiles.BreakableBridges, 4);
        ContentResourceManager.AddAnimatedSpriteResource("TeleporterPadsOccupied", "Graphics\\TeleporterPadAnimationOccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.ShuttlePlatform, 8);
        ContentResourceManager.AddAnimatedSpriteResource("TeleporterPadsUnoccupied", "Graphics\\TeleporterPadAnimationUnoccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.RemovedShuttlePlatform, 1);

        // Sounds
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

        // Background sounds
        ContentResourceManager.AddSoundResource("BackgroundSound1", "Sounds\\BackgroundSound1", SoundEffectInstanceType.Single, 0, false);
    }

    // Captures all status changes from game components
    private void ContentResourceManager_OnGameComponentStatusChanged(string message, int percentComplete, GameEngineStatus status)
    {
        if (status == GameEngineStatus.Started)
        {
            SqText = new ImageFont(this, ContentMgr);

            // Player (Blob). Built here rather than in Initialise because the three particle
            // effects he carries are resources, and those do not exist until content loads
            Player = new Blob(this);
            Player.OnScreenChanged += Player_OnScreenChanged;
            Player.OnBlobActionRaised += Player_OnBlobActionRaised;

            // Game Screens. Built here rather than in the constructor because the screen data and
            // the collision map it needs are resources, and those do not exist until content loads
            GameScreens =
                new GameScreenCollection(
                    this,
                    TileMetrics.TilesAcross,
                    TileMetrics.TilesDown,
                    0,
                    0,
                    TileMetrics.CollisionCellWidth,
                    TileMetrics.CollisionCellHeight);

            // Animation engine
            ColourAnimationEngine = new ColourAnimationEngine(this);

            // Add the colour animations to the colour animation engine
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

            ScreenXOffset = (GraphicsDeviceMgr.PreferredBackBufferWidth - GameScreens.ScreenWidth) / 2;
            ScreenYOffset = 10;

            // Conveyer nasties
            ConveyerNastiesInstance.Initialise(null);
            var converyerNastyGraphics = new ContentList(ContentMgr);
            converyerNastyGraphics.AddContent("conveyerNasty", "Graphics\\ConveryerNasty");
            ConveyerNastiesInstance.LoadContent(converyerNastyGraphics);

            // Initialise Breakable bridges
            BreakableBridgesEngine = new BreakableBridges(this);

            // Initialise the gamescreens
            GameScreens.Initialise(null);

            // Setup the presentation screens
            _optionsScreen = new OptionsScreen(this);
            _computerReportScreen = new ComputerReportScreen(this);
            _hiScoreScreen = new HiScoreScreen(this);
            _gameOverScreen = new GameOverScreen(this);
            _gameCompleteScreen = new GameCompleteScreen(this);
            _testScreen = new TestScreen(this);

            // Setup the game screens
            _mainScreen = new MainGameScreen(this);
            _teleportScreen = new TeleportScreen(this);
            _coreScreen = new PlanetCoreScreen(this, Elements);
            _accessScreen = new AccessBoothScreen(this);
            _cheopsScreen = new CheopsScreen(this);

            _screens.Add(GameScreenType.Options, (IGameScreen)_optionsScreen);
            _screens.Add(GameScreenType.ComputerReport, (IGameScreen)_computerReportScreen);
            _screens.Add(GameScreenType.HiScores, (IGameScreen)_hiScoreScreen);
            _screens.Add(GameScreenType.GameOver, (IGameScreen)_gameOverScreen);
            _screens.Add(GameScreenType.GameComplete, (IGameScreen)_gameCompleteScreen);
            _screens.Add(GameScreenType.TestScreen, (IGameScreen)_testScreen);

            _screens.Add(GameScreenType.MainGame, (IGameScreen)_mainScreen);
            _screens.Add(GameScreenType.Teleporter, (IGameScreen)_teleportScreen);
            _screens.Add(GameScreenType.PlanetCore, (IGameScreen)_coreScreen);
            _screens.Add(GameScreenType.AccessBooth, (IGameScreen)_accessScreen);
            _screens.Add(GameScreenType.Cheops, (IGameScreen)_cheopsScreen);

            // Load the content for the screens
            foreach (var screen in _screens)
            {
                ((GameComponentBase)screen.Value).LoadContent();
            }

            // Setup the screen engine
            ScreenEffectEngine = new ScreenEffectEngine(GameHost.ScreenWidth, GameHost.ScreenHeight, this);

            ScreenEffectEngine.RegisterViewPort(
                "GameArea",
                ScreenXOffset,
                ScreenYOffset + BlobStatusPanel.Height + 10,
                GameScreens.ScreenWidth,
                GameScreens.ScreenHeight);

            ScreenEffectEngine.RegisterViewPort(
                "StatusArea",
                ScreenXOffset + 2,
                ScreenYOffset,
                BlobStatusPanel.Width,
                BlobStatusPanel.Height);

            ScreenEffectEngine.RegisterViewPort(
                "EntireScreen",
                0,
                0,
                GameHost.ScreenWidth,
                GameHost.ScreenHeight);

            ContentResourceManager.Initialise();

            UpdateStatus("Content loaded", FullyLoadedPercent, GameEngineStatus.StartGame);
        }
        else
        {
            UpdateStatus(message, percentComplete, status);
        }
    }

    private void LoadContent()
    {
        ContentResourceManager.LoadContent();
        BlobKiller.LoadContent();
    }

    // Resets the game back to defaults - need to sort all of this out!
    public void Reset()
    {
        ((IGameScreen)_mainScreen).Reset(null);
    }

    public IGameScreen GetGameScreen(GameScreenType gameScreen)
    {
        _screens.TryGetValue(gameScreen, out var screenToGet);
        return screenToGet;
    }

    public void Update(GameTime gameTime)
    {
        if (ToggleKeys.GetPressedKey() == Keys.F9)
        {
            ScanlinesEnabled = !ScanlinesEnabled;
        }

        if (Status == GameEngineStatus.StartingGraphicsLoaded)
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
        else if (Status == GameEngineStatus.ContentLoaded)
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
        else if (Status == GameEngineStatus.StartGame)
        {
            if (CurrentGameScreen == GameScreenType.Cheops ||
                CurrentGameScreen == GameScreenType.MainGame ||
                CurrentGameScreen == GameScreenType.PlanetCore ||
                CurrentGameScreen == GameScreenType.Teleporter ||
                CurrentGameScreen == GameScreenType.AccessBooth)
            {
                if (CurrentGameScreen != GameScreenType.Teleporter)
                {
                    if (ToggleKeys.GetPressedKey() == PlayerKeys.Pause)
                    {
                        Paused = !Paused;

                        if (Paused)
                        {
                            ContentResourceManager.GetSoundFxEngine().KillSounds(true);
                        }
                    }
                }
            }

            // A toggle rather than IsKeyDown: held down, that flipped fullscreen once per
            // logic frame for as long as the key was held.
            if (ToggleKeys.GetPressedKey() == Keys.F12)
            {
                if (CurrentGameScreen == GameScreenType.Cheops ||
                    CurrentGameScreen == GameScreenType.MainGame ||
                    CurrentGameScreen == GameScreenType.PlanetCore ||
                    CurrentGameScreen == GameScreenType.AccessBooth)
                {
                    // Automatically pause the game after switching to or from fullscreen so the player doesn't die unexpectantly
                    if (!Paused)
                    {
                        Paused = true;
                    }
                }

                try
                {
                    ToggleFullScreen();
                }
                catch { }
            }

            if (ToggleKeys.GetPressedKey() == Keys.F10)
            {
                if (CurrentGameScreen == GameScreenType.MainGame)
                {
                    DrawBackgrounds = !DrawBackgrounds;
                }
            }

            // Switch between old and new graphics
            if (ToggleKeys.GetPressedKey() == Keys.F8)
            {
                UsingOldGraphics = !UsingOldGraphics;

                if (UsingOldGraphics)
                {
                    LowerScreenTilesName = "LowerScreenTilesOriginal";
                    UpperScreenTilesName = "UpperScreenTilesOriginal";
                    BlobSpritesName = "BlobSpritesOriginal";
                    PowerUpsName = "PowerUpsOriginal";
                    BlobsShipName = "BlobsShipOriginal";
                    AccessCodesFailedName = "AccessCodesFailedOriginal";
                    AccessCodesPassedName = "AccessCodesPassedOriginal";
                    AccessCodesGreyName = "AccessCodesGrayOriginal";
                    PickupsIlluminatedName = "PickupsIlluminatedOriginal";
                    PickupsName = "PickupsOriginal";

                    DrawBackgrounds = false;
                }
                else
                {
                    LowerScreenTilesName = "LowerScreenTiles";
                    UpperScreenTilesName = "UpperScreenTiles";
                    BlobSpritesName = "BlobSprites";
                    PowerUpsName = "PowerUps";
                    BlobsShipName = "BlobsShip";
                    AccessCodesFailedName = "AccessCodesFailed";
                    AccessCodesPassedName = "AccessCodesPassed";
                    AccessCodesGreyName = "AccessCodesGray";
                    PickupsIlluminatedName = "PickupsIlluminated";
                    PickupsName = "Pickups";

                    DrawBackgrounds = true;
                }

                if (IntroSequence.SequenceStage == IntroSequenceStage.Finished || IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
                {
                    ((MainGameScreen)GetGameScreen(GameScreenType.MainGame)).SpritesChanged();
                }

                ContentResourceManager.GetSoundFxEngine().PlaySound("AccessSuccess", 1f, false);
            }

            if (InTestMode)
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

                // Reset screen to it's original dimensions
                if (ToggleKeys.GetPressedKey() == Keys.F11)
                {
                    if (!GraphicsDeviceMgr.IsFullScreen)
                    {
                        /*
                        System.Windows.Forms.Form f = System.Windows.Forms.Form.FromHandle(parentWindow.Handle) as System.Windows.Forms.Form;

                        if (f != null)
                        {
                            f.ClientSize = new System.Drawing.Size(Starquake.ScreenWidth, Starquake.ScreenHeight);
                            GraphicsDeviceMgr.GraphicsDevice.PresentationParameters.BackBufferWidth = parentWindow.ClientBounds.Width;
                            GraphicsDeviceMgr.GraphicsDevice.PresentationParameters.BackBufferHeight = parentWindow.ClientBounds.Height;
                            GraphicsDeviceMgr.GraphicsDevice.Reset(GraphicsDeviceMgr.GraphicsDevice.PresentationParameters);
                            GraphicsDeviceMgr.ApplyChanges();
                        }
                        */
                    }
                }


                if (ToggleKeys.GetPressedKey() == Keys.F3)
                {
                    ScreenEffectEngine.ExecuteScreenEffect(ScreenEffectType.AdditiveFade, -1, 50, false);
                }
                /*
                if (ToggleKeys.GetPressedKey() == Keys.S)
                {
                    ScreenEffectEngine.ExecuteScreenEffect(enScreenEffectType.LeftTunnelEntry, 80, -1, false);
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D)
                {
                    ScreenEffectEngine.ExecuteScreenEffect(enScreenEffectType.RightTunnelEntry, 80, -1, false);
                }
                 */

                if (CurrentGameScreen == GameScreenType.MainGame)
                {
                    // Change screens
                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad4))
                    {
                        if (GameScreens.ScreenNumber > 0)
                        {
                            GameScreens.ScreenNumber--;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                            Thread.Sleep(TestModeScreenStepDelayMs);
                        }
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad6))
                    {
                        if (GameScreens.ScreenNumber < WorldMap.LastScreenNumber)
                        {
                            GameScreens.ScreenNumber++;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                            Thread.Sleep(TestModeScreenStepDelayMs);
                        }
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad8))
                    {
                        if (GameScreens.ScreenNumber - WorldMap.ScreensAcross >= 0)
                        {
                            GameScreens.ScreenNumber -= WorldMap.ScreensAcross;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                            Thread.Sleep(TestModeScreenStepDelayMs);
                        }
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad2))
                    {
                        if (GameScreens.ScreenNumber + WorldMap.ScreensAcross <= WorldMap.ScreenCount)
                        {
                            GameScreens.ScreenNumber += WorldMap.ScreensAcross;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(GameScreenType.MainGame, true);
                            Thread.Sleep(TestModeScreenStepDelayMs);
                        }
                    }

                    // Test colour animation engine
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
            }

            if (!Paused)
            {
                _screens.TryGetValue(CurrentGameScreen, out var screenToUpdate);

                // Status panel
                if (CurrentGameScreen == GameScreenType.Cheops ||
                    CurrentGameScreen == GameScreenType.MainGame ||
                    CurrentGameScreen == GameScreenType.PlanetCore ||
                    CurrentGameScreen == GameScreenType.Teleporter ||
                    CurrentGameScreen == GameScreenType.AccessBooth)
                {
                    BlobStatusPanel.Update(Player.BlobInventory);
                }

                if (screenToUpdate != null)
                {
                    screenToUpdate.Update(gameTime);

                    // Update realtime gamescreens
                    foreach (var igs in _screens.Values)
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

                if (!_particleTestTrigger)
                {
                    _particleTestTrigger = true;
                }
            }
        }
    }

    private void CalculatePresentationTransform()
    {
        // The back buffer, not GraphicsDevice.Viewport: the boot screen parks the viewport at
        // the composition size and the render viewports move it around as they please.
        var backBuffer = GraphicsDeviceMgr.GraphicsDevice.PresentationParameters;

        var scale = Math.Min(
            backBuffer.BackBufferWidth / (float)GameHost.ScreenWidth,
            backBuffer.BackBufferHeight / (float)GameHost.ScreenHeight);

        var borderX = (backBuffer.BackBufferWidth - GameHost.ScreenWidth * scale) / 2f;
        var borderY = (backBuffer.BackBufferHeight - GameHost.ScreenHeight * scale) / 2f;

        PresentationTransform =
            Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(borderX, borderY, 0f);
    }

    /// <summary>
    ///     Go fullscreen at the resolution the desktop is already running at, so the picture is
    ///     composed for the display it lands on instead of being stretched up from the windowed
    ///     size. The game itself never changes size: it always composes at
    ///     <see cref="GameHost.ScreenWidth" /> x <see cref="GameHost.ScreenHeight" /> and
    ///     PresentationTransform scales that onto whatever back buffer we end up with.
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
            if (CurrentGameScreen == GameScreenType.Cheops ||
                CurrentGameScreen == GameScreenType.MainGame ||
                CurrentGameScreen == GameScreenType.PlanetCore ||
                CurrentGameScreen == GameScreenType.Teleporter ||
                CurrentGameScreen == GameScreenType.AccessBooth)
            {
                ScreenEffectEngine.SelectRenderViewPort("StatusArea"); // StatusPanelArea;
                BlobStatusPanel.Draw(gameTime);
            }

            ScreenEffectEngine.SelectRenderViewPort("GameArea"); // GameArea;
            GraphicsDeviceMgr.GraphicsDevice.Clear(ScreenClearColour);

            _screens.TryGetValue(CurrentGameScreen, out var screenToDraw);
            screenToDraw?.Draw(gameTime);

            if (Paused)
            {
                Overlays.Paused(PausedOverlayXPos, PausedOverlayYPos, this);
            }
        }
        else
        {
            // We're starting up so draw some nice progress bars and graphics etc.
            // The viewport has to be the whole back buffer, not the composition size: the
            // boot screen is scaled up into it by PresentationTransform like everything else,
            // and a viewport any smaller would just clip the result.
            var backBuffer = GraphicsDeviceMgr.GraphicsDevice.PresentationParameters;

            var vp = new Viewport();
            vp.X = 0;
            vp.Y = 0;
            vp.Width = backBuffer.BackBufferWidth;
            vp.Height = backBuffer.BackBufferHeight;

            GraphicsDeviceMgr.GraphicsDevice.Viewport = vp;
            GraphicsDeviceMgr.GraphicsDevice.Clear(ScreenClearColour);


            if (Status == GameEngineStatus.Starting ||
                Status == GameEngineStatus.StartingGraphicsLoaded ||
                Status == GameEngineStatus.ContentLoaded)
            {
                _bootScreen.Begin(BlendState.Opaque, gameTime, PresentationTransform);
                _bootScreen.DrawSpriteUnscaled(0, 0, 0, _loadingScreenFade);
                _bootScreen.End();

                _bootProgressBackground.Begin(BlendState.AlphaBlend, gameTime, PresentationTransform);
                _bootProgressBackground.DrawSpriteUnscaled(0, BootProgressBackgroundXPos, BootProgressBackgroundYPos, _bootProgressBackgroundFade);
                _bootProgressBackground.End();

                // Draw the status percentage
                _bootProgress.Begin(BlendState.AlphaBlend, gameTime, PresentationTransform);
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
    }
    
    public void Dispose()
    {
        ContentResourceManager.GetSoundFxEngine().Dispose();
    }
}