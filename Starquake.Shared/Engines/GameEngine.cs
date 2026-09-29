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
using SharedLibs;
using Starquake_MonoGame.ActiveSprites;
using Starquake_XNA;
using Starquake_XNA.Animation;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Display;
using StarquakeXNA.GameComponents;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class GameEngine : StartableGameComponentBase, IDisposable
{

    private static GameEngine s_instance;
    public string AccessCodesFailedName = "AccessCodesFailed";
    public string AccessCodesGreyName = "AccessCodesGrey";
    public string AccessCodesPassedName = "AccessCodesPassed";
    private GameComponentBase AccessScreen;

    public Color BackgroundColour;

    public BlobDeathAnimation BlobKiller;
    public string BlobSpritesName = "BlobSprites";
    public string BlobsShipName = "BlobsShip";

    public StatusPanel BlobStatusPanel;
    private SpriteCollection bootProgress;
    private SpriteCollection bootProgressBackground;
    private int bootProgressBackgroundFade = 255;
    // Boot graphics
    private SpriteCollection bootScreen;

    // Animation tiles
    public SpriteCollection BreakableBridgeAnim;
    public BreakableBridges BreakableBridgesEngine;
    private GameComponentBase CheopsScreen;

    // Test
    public SpriteCollection CollisionTile;

    // Colour Animation Engine
    public ColourAnimationEngine ColourAnimationEngine;
    private GameComponentBase ComputerReportScreen;
    public ContentManager ContentMgr;

    public ResourceManager ContentResourceManager;
    public ConveyerNasties ConveyerNastiesInstance;
    private GameComponentBase CoreScreen;
    public enGameScreen CurrentGameScreen;

    public bool DrawBackgrounds = true;
    public ElectricityGatesAndNodesEngine ElectricityNodeGateEngine;

    // Central objects
    public CoreElements Elements;

    // Font 1
    public BitmapFont Font1;

    // Font 2
    public BitmapFont Font2;
    private GameComponentBase GameCompleteScreen;
    private GameComponentBase GameOverScreen;
    public GameScreenCollection GameScreens;

    //public MercuryParticleEngine ParticleEngine;
    public GraphicsDeviceManager GraphicsDeviceMgr;
    private GameComponentBase HiScoreScreen;

    public bool InTestMode = true;

    // Game Sequences
    public IntroSequence IntroSequence;

    // Loading screen
    private int loadingScreenFade;

    public string LowerScreenTilesName = "LowerScreenTiles";

    // Initialise Game Components
    private GameComponentBase MainScreen;
    public Nasties Nasties;

    private GameComponentBase OptionsScreen;

    private GameWindow parentWindow;
    private bool particleTestTrigger;

    private bool paused;
    public string PickupsIlluminatedName = "PickupsIlluminate";
    public string PickupsName = "Pickups";
    public Blob Player;

    private PlayerKeySet playerKeys;
    public string PowerUpsName = "PowerUps";

    private int PowerUpType;

    /// <summary>
    ///     The game is composed at a fixed <see cref="Starquake.ScreenWidth" /> by
    ///     <see cref="Starquake.ScreenHeight" /> and only scaled onto the back buffer as the very
    ///     last step, so nothing in the game has to know or care whether it is in a small window,
    ///     a dragged-out one, or fullscreen on a 6K display. The scale is uniform so the picture
    ///     is never stretched, and whatever is left over becomes an even border - top and bottom,
    ///     or left and right. Recalculated every frame rather than hung off a resize event, so a
    ///     fullscreen toggle, a window drag and a change of display all just work.
    /// </summary>
    public Matrix PresentationTransform = Matrix.Identity;

    public Random RandomNumberGenerator;
    public SpriteCollection Scanlines;
    public bool ScanlinesEnabled = true;

    public Color ScreenClearColour = Color.Black;

    // Screen Effect engine
    public ScreenEffectEngine ScreenEffctEngine;
    public Dictionary<enGameScreen, IGameScreen> Screens;
    public int ScreenXOffset;
    public int ScreenYOffset;

    // Test area
    public Shapes ShapeDrawer;

    // Fonts
    public ImageFont SQText;
    private GameComponentBase TeleportScreen;
    public CoreElement testElement1;
    private GameComponentBase TestScreen;

    // Text output
    public XNAFont TextWriter;

    // Tunnel lift animations
    public TunnelLiftAnimationEngine TunnelAnimations;
    public string UpperScreenTilesName = "UpperScreenTiles";

    public bool UsingOldGraphics;

    // Scaling
    public GameEngine(
        GameWindow ParentWindow,
        GraphicsDeviceManager Graphics,
        ContentManager Content)
    {
        s_instance = this;

        parentWindow = ParentWindow;

        Paused = false;

        // Setup graphics device and content management
        GraphicsDeviceMgr = Graphics;
        ContentMgr = Content;
        BackgroundColour = Color.Black;

        ContentResourceManager = new ResourceManager(this);
        ContentResourceManager.OnGameComponentStatusChanged += ContentResourceManager_OnGameComponentStatusChanged;

        RandomNumberGenerator = new Random((int)DateTime.Now.Ticks);
        Screens = new Dictionary<enGameScreen, IGameScreen>();

        // Starting screen will be the title screen later.....
        CurrentGameScreen = enGameScreen.Options;

        // Game Screens
        GameScreens =
            new GameScreenCollection(
                this,
                8,
                6,
                0,
                0,
                32,
                32);

        // Player keys
        playerKeys = new PlayerKeySet();
        playerKeys.Up = Keys.Up;
        playerKeys.Down = Keys.Down;
        playerKeys.Left = Keys.Left;
        playerKeys.Right = Keys.Right;
        playerKeys.Fire = Keys.LeftControl;
    }

    public PlayerKeySet PlayerKeys
    {
        get => playerKeys;
        set
        {
            playerKeys = value;
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
        get => paused;
        set
        {
            paused = value;
            if (paused)
            {
                if (ContentResourceManager != null)
                {
                    ContentResourceManager.GetSoundFXEngine().DisablePeriodicBackgroundSounds(true);
                }
            }
            else
            {
                if (ContentResourceManager != null)
                {
                    ContentResourceManager.GetSoundFXEngine().EnablePeriodicBackgroundSounds();
                }
            }
        }
    }

    public static GameEngine GetInstance()
        => s_instance;

    private void Nasties_OnNastyInteractionWithBlob(enNastyInterationType InteractionType, enNastyType NastyType)
    {
        if (InteractionType == enNastyInterationType.BlobShot)
        {
            BlobStatusPanel.PlayerScore = BlobStatusPanel.PlayerScore + 50;
        }
        else if (InteractionType == enNastyInterationType.HitBlob)
        {
            if (NastyType == enNastyType.Spiky)
            {
                Player.KillBlob(false, false);
            }
            else if (NastyType == enNastyType.Conveyer)
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

    private void Player_OnScreenChanged(enGameScreen TriggerScreen)
    {
        ((MainGameScreen)MainScreen).PerformPostScreenChangeActions(TriggerScreen, null);
    }

    private void Player_OnBlobActionRaised(enBlobAction Action, object Data)
    {
        // inform status panel that something has happened
        BlobStatusPanel.BlobHasPerformedAnAction(Action, Data);

        if (Action == enBlobAction.GotPowerUp)
        {
            PowerUpType = (int)Data;
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

        // Player (Blob)
        Player = new Blob(this);
        Player.OnScreenChanged += Player_OnScreenChanged;
        Player.OnBlobActionRaised += Player_OnBlobActionRaised;
        Player.Initialise();

        // Nasties
        Nasties = new Nasties(this, 4);
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
        bootScreen = new SpriteCollection(this, ContentMgr, @"Graphics\\BootScreen2", 1280, 800);
        bootProgressBackground = new SpriteCollection(this, ContentMgr, @"Graphics\\ProgressBarBackground", 1046, 32);
        bootProgress = new SpriteCollection(this, ContentMgr, @"Graphics\\BootProgress", 1, 24);
        Scanlines = new SpriteCollection(this, ContentMgr, @"Graphics\\Scanlines2", 1280, 800);

        status = GameEngineStatus.StartingGraphicsLoaded;

        // Font 1
        Font1 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraSmall"), new SpriteBatch(GraphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXNA4Colour(130, 130, 255), 12);

        // Font 2
        Font2 = new BitmapFont(ContentMgr.Load<SpriteFont>("Fonts\\elektoraMedium"), new SpriteBatch(GraphicsDeviceMgr.GraphicsDevice), Color.White, ColourHelper.GetXNA4Colour(130, 130, 255), 12);

        // Blob's death effect
        BlobKiller = new BlobDeathAnimation(this);

        // Backgrounds
        ContentResourceManager.AddSpriteResource("Background1", "Graphics\\Background1", 1024, 576);
        //ContentResourceManager.AddSpriteResource("Background2", "Graphics\\Background2", 1024, 576);

        // Proportional Numbers
        ContentResourceManager.AddSpriteResource("ProportionalNumbers", "Graphics\\ProportionalNumbers", 27, 26);

        // Game data, if these hit the same file they must be presented in order
        ContentResourceManager.AddDataResource("GameScreens", "Data/SQScreenData2.dat", typeof(GameScreenData[]));
        ContentResourceManager.AddDataResource("ScreenCollisionData", "Data/SQScreenData2.dat", typeof(int[,,]));

        // Option screen graphics
        ContentResourceManager.AddSpriteResource("OptionScreenBackground", "Graphics\\OptionsScreenBackground", 1280, 800);
        ContentResourceManager.AddSpriteResource("OptionScreenForeground", "Graphics\\OptionsScreenForeground", 821, 554);

        // Computer report screen graphics
        ContentResourceManager.AddSpriteResource("ComputerReportScreenBackground", "Graphics\\ComputerReportScreenBackground", 1280, 800);
        ContentResourceManager.AddSpriteResource("ComputerReportScreenForeground", "Graphics\\ComputerReportScreenForeground", 821, 554);

        // Gameover screen graphics
        ContentResourceManager.AddSpriteResource("GameOverScreenBackground", "Graphics\\GameOverScreenBackground", 1280, 800);
        ContentResourceManager.AddSpriteResource("GameOverScreenForeground", "Graphics\\GameOverScreenForeground", 821, 554);

        // HiScore screen graphics
        ContentResourceManager.AddSpriteResource("HiScoreScreenBackground", "Graphics\\HiScoreScreenBackground", 1280, 800);
        ContentResourceManager.AddSpriteResource("HiScoreScreenForeground", "Graphics\\HiScoreScreenForeground", 821, 554);

        // Game Complete screen graphics
        ContentResourceManager.AddSpriteResource("GameCompleteScreenBackground", "Graphics\\GameCompleteScreenBackground", 1280, 800);
        ContentResourceManager.AddSpriteResource("GameCompleteScreenForeground", "Graphics\\GameCompleteScreenForeground", 821, 554);

        // Game screen graphics
        ContentResourceManager.AddSpriteResource("UpperScreenTiles", "Graphics\\UpperScreenTiles", 128, 96);
        ContentResourceManager.AddSpriteResource("UpperScreenTilesOriginal", "Graphics\\UpperScreenTilesOriginal", 128, 96);

        ContentResourceManager.AddSpriteResource("LowerScreenTiles", "Graphics\\StartilesNew", 128, 96);
        ContentResourceManager.AddSpriteResource("LowerScreenTilesOriginal", "Graphics\\StartilesOriginal", 128, 96);

        ContentResourceManager.AddSpriteResource("LowerScreenTilesDark", "Graphics\\startilesNew", 128, 96);
        ContentResourceManager.AddSpriteResource("ExitShadows", "Graphics\\ExitShadows", 128, 96);

        // Blob's graphics
        //ContentResourceManager.AddSpriteResource(gameEngine.BlobSpritesName, "Graphics\\blobNew", 64, 64);
        //ContentResourceManager.AddSpriteResource(gameEngine.BlobSpritesName, "Graphics\\blobNewAlt", 64, 64);
        ContentResourceManager.AddSpriteResource("BlobSprites", "Graphics\\blobNewAlt2", 64, 64);
        ContentResourceManager.AddSpriteResource("BlobSpritesOriginal", "Graphics\\BlobOriginal", 64, 64);

        ContentResourceManager.AddSpriteResource("BlobParts", "Graphics\\blobParts", 32, 32);
        ContentResourceManager.AddSpriteResource("BlobShuttle", "Graphics\\BlobShuttle", 64, 32);
        ContentResourceManager.AddSpriteResource("BlobShuttleOriginal", "Graphics\\BlobShuttleOriginal", 64, 32);

        ContentResourceManager.AddSpriteResource("BlobFire1", "Graphics\\BlobFire1", 64, 64);
        ContentResourceManager.AddSpriteResource("BlobFire2", "Graphics\\BlobFireNew2", 32, 32);
        ContentResourceManager.AddSpriteResource("BlobFire2Original", "Graphics\\BlobFire2Original", 64, 64);

        ContentResourceManager.AddSpriteResource("BlobFire2Overlay", "Graphics\\BlobFire2Overlay", 48, 48);
        ContentResourceManager.AddSpriteResource("JumpPlatforms", "Graphics\\BlobJumpPlatform", 64, 32);
        ContentResourceManager.AddSpriteResource("JumpPlatformsOriginal", "Graphics\\BlobJumpPlatformOriginal", 64, 32);

        // PickUps
        ContentResourceManager.AddSpriteResource("PowerUps", "Graphics\\PowerUps", 64, 64);
        ContentResourceManager.AddSpriteResource("PowerUpsOriginal", "Graphics\\PowerUpsOriginal", 64, 64);
        ContentResourceManager.AddSpriteResource("Pickups", "Graphics\\Pickups", 64, 64);
        ContentResourceManager.AddSpriteResource("PickupsOriginal", "Graphics\\PickupsOriginal", 64, 64);

        // Nasties
        ContentResourceManager.AddSpriteResource("Nasties", "Graphics\\Nasties", 64, 64);
        ContentResourceManager.AddSpriteResource("NastiesOriginal", "Graphics\\NastiesOriginal", 64, 64);

        // Access booth graphics
        ContentResourceManager.AddSpriteResource("ScanLine", "Graphics\\ScanLine", 74, 9);

        ContentResourceManager.AddSpriteResource("AccessCodesFailed", "Graphics\\AccessCodesFailed", 64, 64);
        ContentResourceManager.AddSpriteResource("AccessCodesPassed", "Graphics\\AccessCodesPassed", 64, 64);
        ContentResourceManager.AddSpriteResource("AccessCodesGray", "Graphics\\AccessCodesGray", 64, 64);
        ContentResourceManager.AddSpriteResource("PickupsIlluminated", "Graphics\\PickupsIlluminated", 64, 64);

        ContentResourceManager.AddSpriteResource("AccessCodesFailedOriginal", "Graphics\\AccessCodesFailedOriginal", 64, 64);
        ContentResourceManager.AddSpriteResource("AccessCodesPassedOriginal", "Graphics\\AccessCodesPassedOriginal", 64, 64);
        ContentResourceManager.AddSpriteResource("AccessCodesGrayOriginal", "Graphics\\AccessCodesGrayOriginal", 64, 64);
        ContentResourceManager.AddSpriteResource("PickupsIlluminatedOriginal", "Graphics\\PickupsIlluminatedOriginal", 64, 64);

        // Cheops pyramid
        ContentResourceManager.AddSpriteResource("CheopsPyramid", "Graphics\\CheopsPyramid", 64, 64);
        ContentResourceManager.AddSpriteResource("CheopsPyramidOriginal", "Graphics\\CheopsPyramidOriginal", 64, 64);

        // Teleporter Interior
        ContentResourceManager.AddSpriteResource("TeleportInteriorLower", "Graphics\\TeleportInteriorLower", 1020, 543);
        ContentResourceManager.AddSpriteResource("TeleportInteriorUpper", "Graphics\\TeleportInteriorUpper", 654, 460);
        ContentResourceManager.AddSpriteResource("TeleporterKeyOverlays", "Graphics\\TeleporterKeyOverlays", 23, 23);
        ContentResourceManager.AddSpriteResource("TeleporterPowerSupply", "Graphics\\TeleporterPowerSupply", 246, 402);

        // ElectricityGatesAndNodes
        ContentResourceManager.AddSpriteResource("ElectricityNodeElectricity1", "Graphics\\ElectricityAnim", 70, 64);
        ContentResourceManager.AddSpriteResource("ElectricityNodeElectricity2", "Graphics\\ElectricityAnim", 70, 64);
        ContentResourceManager.AddSpriteResource("NodeLights", "Graphics\\NodeLight", 64, 64);
        ContentResourceManager.AddSpriteResource("GateLights", "Graphics\\GateLight", 32, 32);
        ContentResourceManager.AddSpriteResource("ElectricityOriginal", "Graphics\\ElectricityOriginal", 64, 64);

        // Odd sized graphics
        ContentResourceManager.AddSpriteResource("OllyBackground", "Graphics\\OllyBackground", 256, 192);
        ContentResourceManager.AddSpriteResource("OllyLower", "Graphics\\OllyLower", 256, 192);
        ContentResourceManager.AddSpriteResource("OllyUpper", "Graphics\\OllyUpper", 125, 120);
        ContentResourceManager.AddSpriteResource("OllyEyes", "Graphics\\OllyEyes", 44, 8);
        ContentResourceManager.AddSpriteResource("OllyOriginal", "Graphics\\OllyOriginal", 256, 192);

        ContentResourceManager.AddSpriteResource("BlobsShipOriginal", "Graphics\\BlobsSpaceShipOriginal", 128, 96);
        ContentResourceManager.AddSpriteResource("BlobsShip", "Graphics\\BlobsSpaceShip", 156, 128);
        ContentResourceManager.AddSpriteResource("BlobsShipHazardLights", "Graphics\\BlobsSpaceShipHazardLights", 156, 128);
        ContentResourceManager.AddSpriteResource("BlobsShipHeadLights", "Graphics\\BlobsSpaceShipHeadLights", 156, 128);


        ContentResourceManager.AddSpriteResource("BlankingTile", "Graphics\\BlankingTile", 128, 96);

        ContentResourceManager.AddSpriteResource("BreakableBridge", "Graphics\\BreakableBridge", 128, 96);
        ContentResourceManager.AddSpriteResource("BreakableBridgeOriginal", "Graphics\\BreakableBridgeOriginal", 128, 96);

        // Status Panel
        ContentResourceManager.AddSpriteResource("StatusPanelGraphic", "Graphics\\StatusPanelNew", 1020, 181);
        ContentResourceManager.AddSpriteResource("EnerygyBar", "Graphics\\EnergyBar", 256, 30);
        ContentResourceManager.AddSpriteResource("JumpBar", "Graphics\\JumpBar", 240, 30);
        ContentResourceManager.AddSpriteResource("AmmoBar", "Graphics\\AmmoBar", 240, 30);
        ContentResourceManager.AddSpriteResource("PanelNumbers", "Graphics\\PanelNumbers", 29, 29);
        ContentResourceManager.AddSpriteResource("StatusPanelShadow", "Graphics\\StatusPanelShadow", 1020, 50);
        ContentResourceManager.AddSpriteResource("StatusPanelOverlay", "Graphics\\StatusPanelOverlay", 966, 135);

        ContentResourceManager.AddSpriteResource("ParticleSprite", "Graphics\\BouncingParticle", 32, 32);
        ContentResourceManager.AddSpriteResource("CollisionTile", "Graphics\\CollisionTile", 32, 32);

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
        ContentResourceManager.AddAnimatedSpriteResource("BreakableBridges", "Graphics\\BreakableBridgeAnim", 128, 8, 410, 4);
        ContentResourceManager.AddAnimatedSpriteResource("TeleporterPadsOccupied", "Graphics\\TeleporterPadAnimationOccupied", 128, 96, 395, 8);
        ContentResourceManager.AddAnimatedSpriteResource("TeleporterPadsUnoccupied", "Graphics\\TeleporterPadAnimationUnoccupied", 128, 96, 398, 1);

        // Sounds
        ContentResourceManager.AddSoundResource("ScanCode", "Sounds\\ScanCode", enSoundEffectInstanceType.Multi, 4, false);
        ContentResourceManager.AddSoundResource("AccessSuccess", "Sounds\\AccessSuccess", enSoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("AccessFailed", "Sounds\\AccessFailed", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("MachineryBackground", "Sounds\\MachineryBackground", enSoundEffectInstanceType.Single, 0, true);

        ContentResourceManager.AddSoundResource("Tunnel", "Sounds\\Tunnel", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("TunnelSwish1", "Sounds\\TunnelSwish1", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("TunnelSwish2", "Sounds\\TunnelSwish2", enSoundEffectInstanceType.Single, 0, false);

        ContentResourceManager.AddSoundResource("NastyDeath", "Sounds\\NastyDeath", enSoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobLazer", "Sounds\\BlobLazer", enSoundEffectInstanceType.Multi, 2, true);
        ContentResourceManager.AddSoundResource("ElectricityNodes", "Sounds\\ElectricityNodes", enSoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BridgeBreak", "Sounds\\BridgeBreak", enSoundEffectInstanceType.Single, 0, true);
        ContentResourceManager.AddSoundResource("BlobRun", "Sounds\\BlobRun", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobFalls", "Sounds\\BlobFalls", enSoundEffectInstanceType.Single, 0, true);
        ContentResourceManager.AddSoundResource("TunnelLift", "Sounds\\TunnelLift", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("NastyAppear", "Sounds\\NastyAppear", enSoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("Rumble", "Sounds\\Rumble", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobJumps", "Sounds\\BlobJump", enSoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobJumpsNoPlatforms", "Sounds\\BlobJumpNoPlatforms", enSoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobPickUp", "Sounds\\BlobPickUp", enSoundEffectInstanceType.Multi, 4, true);
        ContentResourceManager.AddSoundResource("BlobOverloadAlarm", "Sounds\\BlobOverloadAlarm", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobShipFall", "Sounds\\BlobShipFall", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("BlobShipExplodes", "Sounds\\BlobShipExplodes", enSoundEffectInstanceType.Single, 0, false);

        ContentResourceManager.AddSoundResource("BlobExplodes", "Sounds\\BlobExplodes", enSoundEffectInstanceType.Multi, 4, false);
        ContentResourceManager.AddSoundResource("BlobPartClang", "Sounds\\BlobPartClang", enSoundEffectInstanceType.Multi, 8, false);

        ContentResourceManager.AddSoundResource("NastyAppears", "Sounds\\BlobExplodes", enSoundEffectInstanceType.Multi, 4, false);
        ContentResourceManager.AddSoundResource("TeleportKeyClick", "Sounds\\TeleportKeyClick", enSoundEffectInstanceType.Multi, 5, false);
        ContentResourceManager.AddSoundResource("TeleportInvalidCode", "Sounds\\TeleportInvalidCode", enSoundEffectInstanceType.Single, 0, false);
        ContentResourceManager.AddSoundResource("Teleport", "Sounds\\Teleport", enSoundEffectInstanceType.Multi, 3, false);

        ContentResourceManager.AddSoundResource("PowerUp1", "Sounds\\PowerUp1", enSoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("PowerUp2", "Sounds\\PowerUp2", enSoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("PowerUp3", "Sounds\\PowerUp3", enSoundEffectInstanceType.Multi, 2, false);
        ContentResourceManager.AddSoundResource("LifePowerUp", "Sounds\\LifePowerUp", enSoundEffectInstanceType.Multi, 2, false);

        // Background sounds
        ContentResourceManager.AddSoundResource("BackgroundSound1", "Sounds\\BackgroundSound1", enSoundEffectInstanceType.Single, 0, false);
    }

    // Captures all status changes from game components
    private void ContentResourceManager_OnGameComponentStatusChanged(string Message, int PercentComplete, GameEngineStatus Status)
    {
        if (Status == GameEngineStatus.Started)
        {
            SQText = new ImageFont(this, ContentMgr);

            var blobGraphics = new ContentList(ContentMgr);
            Player.LoadContent(blobGraphics);

            GameScreens.LoadContent();

            // Animation engine
            ColourAnimationEngine = new ColourAnimationEngine(this);

            // Add the colour animations to the colour animation engine
            ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
            {
                TileNo = 397,
                UpperTileToDraw = 7,
                Intensity = 4,
                TransitionColour = new Color(0, 0, 255),
                Speed = 8
            }); // Teleport booths
            ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
            {
                TileNo = 399,
                UpperTileToDraw = 9,
                Intensity = 2,
                TransitionColour = new Color(0, 255, 0),
                Speed = 32
            }); // Access tunnel left
            ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
            {
                TileNo = 400,
                UpperTileToDraw = 10,
                Intensity = 2,
                TransitionColour = new Color(255, 0, 0),
                Speed = 32
            }); // Access tunnel right
            ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
            {
                TileNo = 401,
                UpperTileToDraw = -1,
                Intensity = 2,
                TransitionColour = new Color(255, 255, 0),
                Speed = 16
            }); // Yellow teleport tag
            ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
            {
                TileNo = 417,
                UpperTileToDraw = -1,
                Intensity = 6,
                TransitionColour = new Color(255, 0, 0),
                Speed = 16
            }); // Red teleport tag
            ColourAnimationEngine.RegisterTile(new ColourFaderAnimation
            {
                TileNo = 418,
                UpperTileToDraw = -1,
                Intensity = 6,
                TransitionColour = new Color(0, 100, 255),
                Speed = 16
            }); // Blue teleport tag

            ScreenXOffset = (GraphicsDeviceMgr.PreferredBackBufferWidth - GameScreens.ScreenWidth) / 2;
            ScreenYOffset = 10;

            // Conveyer nasties
            ConveyerNastiesInstance.Initialise(null);
            var converyerNastyGraphics = new ContentList(ContentMgr);
            converyerNastyGraphics.AddContent("conveyerNasty", "Graphics\\ConveryerNasty");
            ConveyerNastiesInstance.LoadContent(converyerNastyGraphics);

            // Initialise Electricity nodes and gates
            ElectricityNodeGateEngine.Initialise(null);

            // Initialise Breakable bridges
            BreakableBridgesEngine = new BreakableBridges(this);

            // Initialise the gamescreens
            GameScreens.Initialise(null);

            // Setup the presentation screens
            OptionsScreen = new OptionsScreen(this);
            ComputerReportScreen = new ComputerReportScreen(this);
            HiScoreScreen = new HiScoreScreen(this);
            GameOverScreen = new GameOverScreen(this);
            GameCompleteScreen = new GameCompleteScreen(this);
            TestScreen = new TestScreen(this);

            // Setup the game screens
            MainScreen = new MainGameScreen(this);
            TeleportScreen = new TeleportScreen(this);
            CoreScreen = new PlanetCoreScreen(this);
            AccessScreen = new AccessBoothScreen(this);
            CheopsScreen = new CheopsScreen(this);

            CoreScreen.Initialise(Elements);

            Screens.Add(enGameScreen.Options, (IGameScreen)OptionsScreen);
            Screens.Add(enGameScreen.ComputerReport, (IGameScreen)ComputerReportScreen);
            Screens.Add(enGameScreen.HiScores, (IGameScreen)HiScoreScreen);
            Screens.Add(enGameScreen.GameOver, (IGameScreen)GameOverScreen);
            Screens.Add(enGameScreen.GameComplete, (IGameScreen)GameCompleteScreen);
            Screens.Add(enGameScreen.TestScreen, (IGameScreen)TestScreen);

            Screens.Add(enGameScreen.MainGame, (IGameScreen)MainScreen);
            Screens.Add(enGameScreen.Teleporter, (IGameScreen)TeleportScreen);
            Screens.Add(enGameScreen.PlanetCore, (IGameScreen)CoreScreen);
            Screens.Add(enGameScreen.AccessBooth, (IGameScreen)AccessScreen);
            Screens.Add(enGameScreen.Cheops, (IGameScreen)CheopsScreen);

            // Load the content for the screens
            foreach (var screen in Screens)
            {
                ((GameComponentBase)screen.Value).LoadContent();
            }

            // Setup the screen engine
            ScreenEffctEngine = new ScreenEffectEngine(Starquake.ScreenWidth, Starquake.ScreenHeight, this);

            ScreenEffctEngine.RegisterViewPort(
                "GameArea",
                ScreenXOffset,
                ScreenYOffset + BlobStatusPanel.Height + 10,
                GameScreens.ScreenWidth,
                GameScreens.ScreenHeight);

            ScreenEffctEngine.RegisterViewPort(
                "StatusArea",
                ScreenXOffset + 2,
                ScreenYOffset,
                BlobStatusPanel.Width,
                BlobStatusPanel.Height);

            ScreenEffctEngine.RegisterViewPort(
                "EntireScreen",
                0,
                0,
                1280,
                800);

            ContentResourceManager.Initialise();

            UpdateStatus("Content loaded", 100, GameEngineStatus.StartGame);
        }
        else
        {
            UpdateStatus(Message, PercentComplete, Status);
        }
    }

    public void LoadContent()
    {
        ContentResourceManager.LoadContent();
        BlobKiller.LoadContent();
    }

    // Resets the game back to defaults - need to sort all of this out!
    public void Reset()
    {
        ((IGameScreen)MainScreen).Reset(null);
    }

    public IGameScreen GetGameScreen(enGameScreen GameScreen)
    {
        IGameScreen screenToGet = null;
        Screens.TryGetValue(GameScreen, out screenToGet);
        return screenToGet;
    }

    public void Update(GameTime gameTime)
    {
        if (ToggleKeys.GetPressedKey() == Keys.F9)
        {
            ScanlinesEnabled = !ScanlinesEnabled;
        }

        if (status == GameEngineStatus.StartingGraphicsLoaded)
        {
            loadingScreenFade += 4;

            if (loadingScreenFade > 255)
            {
                loadingScreenFade = 255;
            }

            if (loadingScreenFade == 255)
            {
                status = GameEngineStatus.Starting;
                LoadContent();
            }
        }
        else if (status == GameEngineStatus.ContentLoaded)
        {
            // Fade out the progress bar background
            if (bootProgressBackgroundFade > 0)
            {
                bootProgressBackgroundFade -= 4;
            }

            if (bootProgressBackgroundFade < 0)
            {
                bootProgressBackgroundFade = 0;
            }

            // Wait for a keypress before leaving the loading screen
            if (ToggleKeys.GetPressedKey() != Keys.None)
            {
                ContentResourceManager_OnGameComponentStatusChanged("Content loaded", 100, GameEngineStatus.Started);
            }
        }
        else if (status == GameEngineStatus.StartGame)
        {
            if (CurrentGameScreen == enGameScreen.Cheops ||
                CurrentGameScreen == enGameScreen.MainGame ||
                CurrentGameScreen == enGameScreen.PlanetCore ||
                CurrentGameScreen == enGameScreen.Teleporter ||
                CurrentGameScreen == enGameScreen.AccessBooth)
            {
                if (CurrentGameScreen != enGameScreen.Teleporter)
                {
                    if (ToggleKeys.GetPressedKey() == PlayerKeys.Pause)
                    {
                        Paused = !Paused;

                        if (Paused)
                        {
                            ContentResourceManager.GetSoundFXEngine().KillSounds(true);
                        }
                    }
                }
            }

            IGameScreen screenToUpdate = null;

            // A toggle rather than IsKeyDown: held down, that flipped fullscreen once per
            // logic frame for as long as the key was held.
            if (ToggleKeys.GetPressedKey() == Keys.F12)
            {
                if (CurrentGameScreen == enGameScreen.Cheops ||
                    CurrentGameScreen == enGameScreen.MainGame ||
                    CurrentGameScreen == enGameScreen.PlanetCore ||
                    CurrentGameScreen == enGameScreen.AccessBooth)
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
                if (CurrentGameScreen == enGameScreen.MainGame)
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
                    AccessCodesGreyName = "AccessCodesGreyOriginal";
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
                    AccessCodesGreyName = "AccessCodesGrey";
                    PickupsIlluminatedName = "PickupsIlluminated";
                    PickupsName = "Pickups";

                    DrawBackgrounds = true;
                }

                if (IntroSequence.SequenceStage == enIntroSequenceStage.Finished || IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
                {
                    ((MainGameScreen)GetGameScreen(enGameScreen.MainGame)).SpritesChanged();
                }

                ContentResourceManager.GetSoundFXEngine().PlaySound("AccessSuccess", 1f, false);
            }

            if (InTestMode)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.F1))
                {
                    if (Player.BlobsState == enBlobState.Normal)
                    {
                        Player.KillBlob(false, false);
                    }
                }

                // Give us some lives back (Testing)
                if (Keyboard.GetState().IsKeyDown(Keys.F2))
                {
                    BlobStatusPanel.BlobLives = 5;
                    Player.BlobsState = enBlobState.Dead;
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
                    ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.AddictiveFade, -1, 50, false);
                }
                /*
                if (ToggleKeys.GetPressedKey() == Keys.S)
                {
                    ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.LeftTunnelEntry, 80, -1, false);
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D)
                {
                    ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.RightTunnelEntry, 80, -1, false);
                }
                 */

                if (CurrentGameScreen == enGameScreen.MainGame)
                {
                    // Change screens
                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad4))
                    {
                        if (GameScreens.ScreenNumber > 0)
                        {
                            GameScreens.ScreenNumber--;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(enGameScreen.MainGame, true);
                            Thread.Sleep(100);
                        }
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad6))
                    {
                        if (GameScreens.ScreenNumber < 511)
                        {
                            GameScreens.ScreenNumber++;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(enGameScreen.MainGame, true);
                            Thread.Sleep(100);
                        }
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad8))
                    {
                        if (GameScreens.ScreenNumber - 16 >= 0)
                        {
                            GameScreens.ScreenNumber -= 16;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(enGameScreen.MainGame, true);
                            Thread.Sleep(100);
                        }
                    }

                    if (Keyboard.GetState().IsKeyDown(Keys.NumPad2))
                    {
                        if (GameScreens.ScreenNumber + 16 <= 512)
                        {
                            GameScreens.ScreenNumber += 16;
                            GetGameScreen(CurrentGameScreen).PerformPostScreenChangeActions(enGameScreen.MainGame, true);
                            Thread.Sleep(100);
                        }
                    }

                    // Test colour animation engine
                    if (ToggleKeys.GetPressedKey() == Keys.K)
                    {
                        ColourAnimationEngine.Start(401, -1);
                        ColourAnimationEngine.Start(417, -1);
                        ColourAnimationEngine.Start(418, -1);
                    }

                    if (ToggleKeys.GetPressedKey() == Keys.L)
                    {
                        ColourAnimationEngine.Stop(401);
                        ColourAnimationEngine.Stop(417);
                        ColourAnimationEngine.Stop(418);
                    }
                }
            }

            if (!Paused)
            {
                Screens.TryGetValue(CurrentGameScreen, out screenToUpdate);

                // Status panel
                if (CurrentGameScreen == enGameScreen.Cheops ||
                    CurrentGameScreen == enGameScreen.MainGame ||
                    CurrentGameScreen == enGameScreen.PlanetCore ||
                    CurrentGameScreen == enGameScreen.Teleporter ||
                    CurrentGameScreen == enGameScreen.AccessBooth)
                {
                    BlobStatusPanel.Update(Player.BlobInventory);
                }

                if (screenToUpdate != null)
                {
                    screenToUpdate.Update(gameTime);

                    // Update realtime gamescreens
                    foreach (var igs in Screens.Values)
                    {
                        if (igs is IRealTimeGameScreen && igs != screenToUpdate)
                        {
                            if (GameScreens.ScreenNumber == 198)
                            {
                                ((IRealTimeGameScreen)igs).UpdateOffScreen(gameTime);
                            }

                            if (GameScreens.ScreenNumber == 197)
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

                if (!particleTestTrigger)
                {
                    particleTestTrigger = true;
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
            backBuffer.BackBufferWidth / (float)Starquake.ScreenWidth,
            backBuffer.BackBufferHeight / (float)Starquake.ScreenHeight);

        var borderX = (backBuffer.BackBufferWidth - Starquake.ScreenWidth * scale) / 2f;
        var borderY = (backBuffer.BackBufferHeight - Starquake.ScreenHeight * scale) / 2f;

        PresentationTransform =
            Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(borderX, borderY, 0f);
    }

    /// <summary>
    ///     Go fullscreen at the resolution the desktop is already running at, so the picture is
    ///     composed for the display it lands on instead of being stretched up from the windowed
    ///     size. The game itself never changes size: it always composes at
    ///     <see cref="Starquake.ScreenWidth" /> x <see cref="Starquake.ScreenHeight" /> and
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
            GraphicsDeviceMgr.PreferredBackBufferWidth = Starquake.ScreenWidth;
            GraphicsDeviceMgr.PreferredBackBufferHeight = Starquake.ScreenHeight;
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

        if (status == GameEngineStatus.StartGame)
        {
            // Status panel
            if (CurrentGameScreen == enGameScreen.Cheops ||
                CurrentGameScreen == enGameScreen.MainGame ||
                CurrentGameScreen == enGameScreen.PlanetCore ||
                CurrentGameScreen == enGameScreen.Teleporter ||
                CurrentGameScreen == enGameScreen.AccessBooth)
            {
                ScreenEffctEngine.SelectRenderViewPort("StatusArea"); // StatusPanelArea;
                BlobStatusPanel.Draw(gameTime);
            }

            ScreenEffctEngine.SelectRenderViewPort("GameArea"); // GameArea;
            GraphicsDeviceMgr.GraphicsDevice.Clear(ScreenClearColour);

            Screens.TryGetValue(CurrentGameScreen, out var screenToDraw);
            screenToDraw?.Draw(gameTime);

            if (Paused)
            {
                Overlays.Paused(400, 200, this);
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


            if (status == GameEngineStatus.Starting ||
                status == GameEngineStatus.StartingGraphicsLoaded ||
                status == GameEngineStatus.ContentLoaded)
            {
                bootScreen.Begin(BlendState.Opaque, gameTime, PresentationTransform);
                bootScreen.DrawSpriteUnscaled(0, 0, 0, loadingScreenFade);
                bootScreen.End();

                bootProgressBackground.Begin(BlendState.AlphaBlend, gameTime, PresentationTransform);
                bootProgressBackground.DrawSpriteUnscaled(0, 116, 657, bootProgressBackgroundFade);
                bootProgressBackground.End();

                // Draw the status percentage
                bootProgress.Begin(BlendState.AlphaBlend, gameTime, PresentationTransform);
                var PercentBarWidth = (int)(519 / (double)100 * PercentComplete);

                for (var a = 0; a < PercentBarWidth; a++)
                {
                    bootProgress.DrawSpriteUnscaled(0, 122 + a * 2, 661, bootProgressBackgroundFade);
                }

                bootProgress.End();
                //Vector2 statusMessageSize = Font1.MeasureString(statusMessage);
                //Font1.Begin(BlendState.AlphaBlend);
                //Font1.WriteText((int)(1280 - statusMessageSize.X) / 2, 620, statusMessage, Color.White, 255);
                //Font1.End();

            }
        }
    }

    #region IDisposable Members

    private void FreeMem(IDisposable ObjToFree)
    {
        if (ObjToFree != null)
        {
            ObjToFree.Dispose();
            ObjToFree = null;
        }
    }

    public void Dispose()
    {
        ContentResourceManager.GetSoundFXEngine().Dispose();
        //FreeMem(BlobSprites);
        //FreeMem(BlobShuttle);
        //FreeMem(JumpPlatforms);
        //FreeMem(BlobFire1);
        //FreeMem(BlobFire2);
        //FreeMem(BlobFire2Overlay);
        //FreeMem(UpperScreenTiles);
        //FreeMem(LowerScreenTiles);
        //FreeMem(PickUps);
        //FreeMem(SQText);
    }

    #endregion

}