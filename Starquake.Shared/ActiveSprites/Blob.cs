// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sounds;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class Blob : GameComponentBase
{

    /// <summary>
    ///     How far the blob's shot travels each logic frame.
    /// </summary>
    private const int BlobFireSpeed = 16;

    /// <summary>
    ///     How many things the blob can carry. Picking something up pushes the whole inventory along
    ///     one and drops whatever falls off the end.
    /// </summary>
    private const int BlobMaxItemsHeld = 4;

    /// <summary>
    ///     The shuttle the blob rides sits directly beneath it and makes the pair this much taller,
    ///     which is what the collision code has to allow for while the blob is aboard.
    /// </summary>
    private const int ShuttleHeight = 32;

    /// <inheritdoc cref="ShuttleHeight" />
    private const int BlobHeightOnShuttle = SpriteMetrics.SpriteHeight + ShuttleHeight;

    /// <summary>
    ///     The frame the blob stands still on - the one it is drawn in on a fresh screen and after a
    ///     death - and the frames its walk cycles run between. Left and right are separate runs of
    ///     frames in the one sheet rather than one run flipped, so turning round means jumping
    ///     between them.
    /// </summary>
    private const int BlobRestingFrame = 5;

    /// <inheritdoc cref="BlobRestingFrame" />
    private const int BlobWalkLeftFirstFrame = 0;

    /// <inheritdoc cref="BlobRestingFrame" />
    private const int BlobWalkLeftLastFrame = 3;

    /// <inheritdoc cref="BlobRestingFrame" />
    private const int BlobWalkRightFirstFrame = 7;

    /// <inheritdoc cref="BlobRestingFrame" />
    private const int BlobWalkRightLastFrame = 10;

    /// <summary>
    ///     Where a walk cycle is picked up when the blob turns round mid-stride, so the new direction
    ///     starts from a matching pose instead of snapping back to the first frame.
    /// </summary>
    private const int BlobTurnToLeftFrame = 6;

    /// <inheritdoc cref="BlobTurnToLeftFrame" />
    private const int BlobTurnToRightFrame = 4;

    /// <summary>
    ///     The shuttle's own animation, and the two the blob's gun muzzle uses. Frames are advanced
    ///     one per <see cref="AnimationDelay" /> logic frames.
    /// </summary>
    private const int ShuttleLastFrame = 3;

    /// <inheritdoc cref="ShuttleLastFrame" />
    private const int FireAnimLastFrame = 3;

    /// <inheritdoc cref="ShuttleLastFrame" />
    private const int ShuttleFireAnimLastFrame = 23;

    /// <inheritdoc cref="ShuttleLastFrame" />
    private const int AnimationDelay = 2;

    /// <inheritdoc cref="ShuttleLastFrame" />
    private const int ShuttleFireAnimationDelay = 1;

    /// <summary>
    ///     Gravity. The blob starts falling at <see cref="BlobInitialFallSpeed" /> pixels a frame and
    ///     gains one every <see cref="FallSpeedIncreaseDistance" /> pixels until it reaches
    ///     <see cref="BlobMaxFallSpeed" />, which is what makes a long drop feel like one.
    /// </summary>
    private const int BlobInitialFallSpeed = 4;

    /// <inheritdoc cref="BlobInitialFallSpeed" />
    private const int BlobMaxFallSpeed = 12;

    /// <inheritdoc cref="BlobInitialFallSpeed" />
    private const int FallSpeedIncreaseDistance = 12;

    /// <summary>
    ///     How far the blob has to have fallen before the falling sound starts, so a step down off a
    ///     platform is silent.
    /// </summary>
    private const int FallSoundDistance = SpriteMetrics.SpriteHeight;

    /// <summary>
    ///     How far the blob has to land from to break a breakable bridge. Anything shorter and the
    ///     bridge holds.
    /// </summary>
    private const int BridgeBreakingFallHeight = 128;

    /// <summary>
    ///     How high the blob hops when it has no platforms left to lay. One collision cell, which is
    ///     just enough to clear a single step.
    /// </summary>
    private const int JumpHeightWithoutPlatforms = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     How far up the blob is carried each frame by a tunnel lift, and how far it has to have
    ///     risen before it is allowed to step out sideways at the top.
    /// </summary>
    private const int TunnelLiftSpeed = 8;

    /// <inheritdoc cref="TunnelLiftSpeed" />
    private const int TunnelLiftMinimumRide = 8;

    /// <summary>
    ///     Where a shot leaves the blob, measured in from its top left corner.
    /// </summary>
    private const int FireOffset = 16;

    /// <summary>
    ///     The shot itself - a thin sliver <see cref="FireWidth" /> across and
    ///     <see cref="FireLength" /> along, whichever way it happens to be travelling. The long
    ///     dimension doubles as the allowance the shot is given at the screen edges, so a shot is
    ///     spent when its far end leaves the playfield rather than when its origin does.
    /// </summary>
    private const int FireWidth = 2;

    /// <inheritdoc cref="FireWidth" />
    private const int FireLength = 32;

    /// <summary>
    ///     Where the sparks are thrown from when a shot hits something, measured down from the shot.
    /// </summary>
    private const int FireSparksYOffset = 24;

    /// <summary>
    ///     What a shot fired from the shuttle is doing. It leaves straight up or straight down, and
    ///     the first thing it hits sends it off diagonally one way or the other rather than stopping
    ///     it, so this says which of the three it is currently on.
    /// </summary>
    private const int FireTravellingStraight = 0;

    /// <inheritdoc cref="FireTravellingStraight" />
    private const int FireDeflectedLeft = 1;

    /// <inheritdoc cref="FireTravellingStraight" />
    private const int FireDeflectedRight = 2;

    /// <summary>
    ///     How the blob comes apart when it dies. It swells to <see cref="OverloadMaxScale" />, then
    ///     shrinks through zero to <see cref="OverloadMinScale" /> - drawing at a negative scale is
    ///     what turns it inside out - and then explodes.
    /// </summary>
    private const int OverloadSwelling = 0;

    /// <inheritdoc cref="OverloadSwelling" />
    private const int OverloadShrinking = 1;

    /// <inheritdoc cref="OverloadSwelling" />
    private const int OverloadExploded = 3;

    /// <inheritdoc cref="OverloadSwelling" />
    private const float OverloadScaleStep = 0.30f;

    /// <inheritdoc cref="OverloadSwelling" />
    private const float OverloadMaxScale = 1.6f;

    /// <inheritdoc cref="OverloadSwelling" />
    private const float OverloadMinScale = -1f;

    /// <summary>
    ///     How long the alarm sounds before the blob comes apart, and how many copies of it are drawn
    ///     at once during the explosion to give the flare its depth.
    /// </summary>
    private const int OverloadAlarmFrames = 60;

    /// <inheritdoc cref="OverloadAlarmFrames" />
    private const int OverloadFlareCopies = 5;

    /// <inheritdoc cref="OverloadAlarmFrames" />
    private const float OverloadFlareFirstScale = 0.4f;

    /// <inheritdoc cref="OverloadAlarmFrames" />
    private const float OverloadFlareScaleStep = 0.2f;

    /// <inheritdoc cref="OverloadAlarmFrames" />
    private const int OverloadFlareAlpha = 150;

    /// <summary>
    ///     How fast the blob reddens when a nasty is touching it and recovers when one is not, and
    ///     how fast it fades back in after a teleport. Both are counted in colour channel levels a
    ///     frame.
    /// </summary>
    private const int EnergyDrainFadeStep = 16;

    /// <inheritdoc cref="EnergyDrainFadeStep" />
    private const int FireFadeStep = 16;

    /// <inheritdoc cref="EnergyDrainFadeStep" />
    private const int TeleportFadeStep = 8;

    /// <summary>
    ///     How fast the blob walks, in pixels a logic frame, horizontally and vertically.
    /// </summary>
    private const int BlobWalkSpeed = 8;

    /// <summary>
    ///     How far inside a spiked tile the blob has to be before the spikes get it. The three spike
    ///     tiles are drawn differently - one standing in the middle of its tile, one mounted on the
    ///     right hand wall, one a field of broken glass on the floor - so each is given the reach its
    ///     artwork suggests rather than the whole tile. The numbers are the blob's position relative
    ///     to the tile, so a negative one means the tile starts before the blob does.
    /// </summary>
    private const int SpikeReachLeft = -16;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int SpikeReachRight = 64;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int CentralSpikeReachUp = -8;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int WallSpikeReachDown = 64;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int WallSpikeReachUp = -32;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int WallSpikeReachIn = 16;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int BrokenGlassReachUp = -16;

    /// <summary>
    ///     How thick a slice is tested when working out which way a stuck blob has to be nudged to
    ///     get free after a tile map change.
    /// </summary>
    private const int UnstickProbeThickness = 8;

    /// <summary>
    ///     Where the blob starts, and where it reappears after losing a life.
    /// </summary>
    private const int BlobStartXPos = 416;

    /// <inheritdoc cref="BlobStartXPos" />
    private const int BlobStartYPos = 414;

    /// <summary>
    ///     Where the teleport beam and the shot's smoke trail are emitted from, measured in from the
    ///     top left of the blob and of the shot respectively.
    /// </summary>
    private const int TeleportBeamXOffset = 30;

    /// <inheritdoc cref="TeleportBeamXOffset" />
    private const int TeleportBeamYOffset = 50;

    /// <inheritdoc cref="TeleportBeamXOffset" />
    private const int FireTrailOffset = 14;

    /// <summary>
    ///     How far into the booth's tile the blob is placed when it steps into a teleporter, so it is
    ///     standing in the doorway rather than on its edge.
    /// </summary>
    private const int TeleportBoothXOffset = 20;

    /// <summary>
    ///     Where the blob is put down after coming through a tunnel - just far enough inside the
    ///     mouth on the far side that it is not standing in the tunnel it just left.
    /// </summary>
    private const int TunnelExitLeftXOffset = 24;

    /// <inheritdoc cref="TunnelExitLeftXOffset" />
    private const int TunnelExitRightXOffset = 40;

    /// <inheritdoc cref="TunnelExitLeftXOffset" />
    private const int TunnelExitYOffset = 32;

    private readonly JumpPlatforms _blobJumpPlatformEngine;
    private int _blobAlarmSoundCounter;
    private readonly Animation _blobAnimFire1;
    private readonly Animation _blobAnimFire2;

    private readonly Animation _blobAnimLeft;
    private readonly Animation _blobAnimRight;

    private bool _blobCanFire = true;
    private int _blobDistanceFallen;

    private int _blobEnergyDrainCounter;
    private int _blobFallHeight;

    private int _blobFallSpeed = BlobInitialFallSpeed;
    private Guid _blobFallsSoundId;
    private int _blobFireCollisionDirection;
    private int _blobFireDistance;

    private int _blobFireFade;
    private readonly IPositionalTriggerParticleEffect _blobFireSparks;
    private readonly IStandardParticleEffect _blobFireTrail;

    private int _blobFrameOnEntry;
    private bool _blobOnShuttle;
    private bool _blobOnShuttleCached;
    private bool _blobOnShuttleOnEntry;
    private bool _blobOnShuttlePad;
    private int _blobOverloadState;

    private float _blobScale = 1f;

    private Point _blobScreenEntryLocation;

    private readonly Animation _blobShuttleAnim;
    private Direction _blobsLastAnimDirection = Direction.None;

    private readonly IStandardParticleEffect _blobTeleporterBeam;
    private int _blobTotalDistanceFallen;
    private bool _blobTouched;

    private Guid _blobTunnelLiftSoundId = Guid.Empty;

    private int _blobXPos;

    private int _blobYPos;

    private Direction _directionBlobFired = Direction.None;
    private Direction _directionGoingThroughTunnel = Direction.None;
    private int _distanceInTunnelLift;
    private bool _inTunnelLift;
    private bool _inventoryAccessed;

    private bool _jumpPressed;
    private bool _resetBlobToScreenEntryPoint;
    private bool _startBlobTeleporting;

    private int _teleportFadeLevel;

    private TileDetected? _tileAboveBlob;
    private TileDetected? _tileBelowBlob;
    private TileDetected? _tileLeftBlob;
    private TileDetected? _tileRightBlob;
    private readonly List<ResetTile> _tilesToReset = [];

    public Blob(GameEngine gameEngineReference) : base(gameEngineReference)
    {
        _blobJumpPlatformEngine = new JumpPlatforms(GameEngine);
        BlobsControl = new CustomControl(gameEngineReference.PlayerKeys, GameEngine, true);
        _blobTouched = false;
        _teleportFadeLevel = ColourLevels.Full;
        _startBlobTeleporting = false;
        BlobsState = BlobState.Normal;

        _blobTeleporterBeam =
            (IStandardParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("BeamMeUp_Small",
                ParticleEffectType.Triggered, null);
        _blobFireTrail =
            (IStandardParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("FireTrail", ParticleEffectType.Continual,
                null);

        _blobFireSparks =
            (IPositionalTriggerParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks",
                ParticleEffectType.TriggeredMultiLocationTimed, null);

        _blobTeleporterBeam.ParticleEffectComplete += blobTeleporterBeam_ParticleEffectComplete;

        // Fire location starts same as Blob's screen location
        BlobFireX = _blobXPos + FireOffset;
        BlobFireY = _blobYPos + FireOffset;
        _blobFireFade = 0;

        _blobShuttleAnim = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = ShuttleLastFrame,
            Yoyo = false,
            Delay = AnimationDelay,
            Repeat = true,
            AnimateForwards = true
        };

        _blobAnimLeft = new Animation
        {
            FirstFrame = BlobWalkLeftFirstFrame,
            CurrentFrame = BlobWalkLeftFirstFrame,
            LastFrame = BlobWalkLeftLastFrame,
            Yoyo = false,
            Delay = AnimationDelay,
            Repeat = true,
            AnimateForwards = false
        };

        _blobAnimRight = new Animation
        {
            FirstFrame = BlobWalkRightFirstFrame,
            CurrentFrame = BlobWalkRightFirstFrame,
            LastFrame = BlobWalkRightLastFrame,
            Yoyo = false,
            Delay = AnimationDelay,
            Repeat = true,
            AnimateForwards = true
        };

        _blobAnimFire1 = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = FireAnimLastFrame,
            Yoyo = false,
            Delay = AnimationDelay,
            Repeat = true,
            AnimateForwards = true
        };

        _blobAnimFire2 = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = ShuttleFireAnimLastFrame,
            Yoyo = false,
            Delay = ShuttleFireAnimationDelay,
            Repeat = true,
            AnimateForwards = true
        };
        _directionBlobFired = Direction.Right;

        BlobInventory = new Collectables[BlobMaxItemsHeld];

        // Clear inventory items
        for (var a = 0; a < BlobInventory.Length; a++)
        {
            BlobInventory[a] = Collectables.None;
        }

        // Temporary test code to give Blob Access card and Key
        BlobInventory[1] = Collectables.Key;
        BlobInventory[0] = Collectables.AccessCard;

    }

    public Collectables[] BlobInventory { get; }

    private Direction DirectionBlobMoved { get; set; } = Direction.None;

    public int BlobXPos
    {
        get => _blobXPos;
        set
        {
            _blobXPos = value;
            _blobScreenEntryLocation.X = value;
        }
    }

    public int BlobYPos
    {
        get => _blobYPos;
        set
        {
            _blobYPos = value;
            _blobScreenEntryLocation.Y = value;
        }
    }

    public BlobState BlobsState { get; set; }

    public bool BlobTouchedByNasty { get; set; }

    public int BlobWidth => SpriteMetrics.SpriteWidth;

    public int BlobHeight { get; private set; } = SpriteMetrics.SpriteHeight;

    public int BlobFrame { get; set; } = BlobRestingFrame;

    public int BlobHSpeed { get; set; } = BlobWalkSpeed;

    public int BlobVSpeed { get; set; } = BlobWalkSpeed;

    public int BlobFireX { get; private set; }

    public int BlobFireY { get; private set; }

    public bool BlobHasFired { get; private set; }

    public CustomControl BlobsControl { get; }

    public event RaiseScreenChanged? OnScreenChanged;

    public event RaiseBlobAction? OnBlobActionRaised;

    public void KillBlob(bool resetBlobToScreenEntryPoint, bool overloadFirst)
    {
        if (BlobsState != BlobState.Normal)
        {
            return;
        }

        _resetBlobToScreenEntryPoint = resetBlobToScreenEntryPoint;

        _blobOverloadState = overloadFirst ? OverloadSwelling : OverloadExploded;

        BlobsState = BlobState.Overloading;
        _blobAlarmSoundCounter = 0;
    }

    // Blob has hit something
    public void RegisterFireHit()
    {
        BlobHasFired = false;
        _blobCanFire = true;
    }

    public void TriggerBlobTeleportedAnimation()
    {
        _blobTeleporterBeam.Start();
        _startBlobTeleporting = true;
        _teleportFadeLevel = 0;
    }

    public override void Update(GameTime gameTime)
    {
        var changedScreens = false;
        switch (BlobsState)
        {
            case BlobState.Normal:
            {
                // Do something about being on something etc.
                if (_blobOnShuttlePad)
                {
                    if (_tileBelowBlob != null)
                    {
                        switch (_tileBelowBlob.TileNumber)
                        {
                            // Change the map to reflect shuttle has been taken
                            case (int)ActionTiles.ShuttlePlatform:
                            {
                                if (BlobsControl.MovedUp())
                                {
                                    GameEngine.GameScreens.ChangeShuttlePlatformState(true);
                                    BlobHeight = BlobHeightOnShuttle;
                                    _blobOnShuttle = true;
                                }

                                break;
                            }
                            case (int)ActionTiles.RemovedShuttlePlatform:
                            {
                                if (BlobsControl.MovedLeft() || BlobsControl.MovedRight())
                                {
                                    GameEngine.GameScreens.ChangeShuttlePlatformState(false);
                                    BlobHeight = SpriteMetrics.SpriteHeight;
                                    _blobOnShuttle = false;
                                }

                                break;
                            }
                        }

                        // Update the collision map for the current screen
                        GameEngine.GameScreens.UpdateCollisionMap(false);
                    }
                }

                if (BlobsControl.MovedLeft())
                {
                    if (_blobsLastAnimDirection == Direction.Right)
                    {
                        _blobAnimLeft.CurrentFrame = BlobFrame > BlobTurnToLeftFrame ? BlobTurnToLeftFrame : BlobFrame;
                    }
                    else
                    {
                        _blobAnimLeft.GetNextFrame();
                    }

                    var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Left, _blobXPos, _blobYPos,
                        BlobWidth, BlobHeight, BlobHSpeed, true);
                    if (amountToMove > 0)
                    {
                        _blobXPos = _blobXPos - amountToMove;
                        if (_blobOnShuttle)
                        {
                            _blobShuttleAnim.GetNextFrame();
                        }
                    }

                    BlobFrame = _blobAnimLeft.CurrentFrame;
                    DirectionBlobMoved = Direction.Left;
                    _blobsLastAnimDirection = Direction.Left;

                    if (!_blobOnShuttle && _blobFallHeight == 0)
                    {
                        GameEngine.ContentResourceManager.GetSoundFxEngine()
                            .PlaySound("BlobRun", true);
                    }
                }

                if (BlobsControl.MovedRight())
                {
                    if (_blobsLastAnimDirection == Direction.Left)
                    {
                        _blobAnimRight.CurrentFrame = BlobFrame < BlobTurnToRightFrame ? BlobTurnToRightFrame : BlobFrame;
                    }
                    else
                    {
                        _blobAnimRight.GetNextFrame();
                    }

                    var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Right, _blobXPos, _blobYPos,
                        BlobWidth, BlobHeight, BlobHSpeed, true);
                    if (amountToMove > 0)
                    {
                        _blobXPos = _blobXPos + amountToMove;
                        if (_blobOnShuttle)
                        {
                            _blobShuttleAnim.GetNextFrame();
                        }
                    }

                    BlobFrame = _blobAnimRight.CurrentFrame;
                    DirectionBlobMoved = Direction.Right;
                    _blobsLastAnimDirection = Direction.Right;

                    if (!_blobOnShuttle && _blobFallHeight == 0)
                    {
                        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobRun", true);
                    }
                }

                if (_blobOnShuttle)
                {
                    if (_blobAnimRight.Repeat)
                    {
                        _blobAnimRight.Repeat = false;
                    }

                    if (_blobAnimLeft.Repeat)
                    {
                        _blobAnimLeft.Repeat = false;
                    }

                    if (BlobsControl.MovedUp())
                    {
                        _blobYPos = _blobYPos - GameEngine.GameScreens.CheckCollision(Direction.Up, _blobXPos,
                            _blobYPos,
                            BlobWidth, BlobHeight, BlobVSpeed, true);
                        DirectionBlobMoved = Direction.Up;
                    }

                    if (BlobsControl.MovedDown())
                    {
                        _blobYPos = _blobYPos + GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos,
                            _blobYPos, BlobWidth, BlobHeight, BlobVSpeed, true);
                        DirectionBlobMoved = Direction.Down;
                    }
                }
                else
                {
                    // Create a standing platform
                    if (BlobsControl.MovedDown() &&
                        !BlobsControl.MovedLeft() &&
                        !BlobsControl.MovedRight())
                    {
                        if (!_jumpPressed)
                        {
                            _jumpPressed = true;

                            if (GameEngine.BlobStatusPanel.Platforms > 0)
                            {
                                if (!_blobOnShuttlePad)
                                {
                                    var temp = _blobJumpPlatformEngine.GetNewJumpPlatform(_blobXPos, _blobYPos,
                                        BlobWidth,
                                        BlobHeight, true);

                                    if (temp == -1)
                                    {
                                    }

                                    // If -1 is returned from this routine, the collision check failed
                                    if (temp != -1000)
                                    {
                                        _blobYPos = temp;

                                        GameEngine.ContentResourceManager.GetSoundFxEngine()
                                            .PlaySound("BlobJumps", 0.3f, false);

                                        if (OnBlobActionRaised != null)
                                            OnBlobActionRaised(BlobAction.LaidPlatform, null);
                                    }
                                }
                            }
                            else // just jump aimlessly in the air
                            {
                                // Check that we are on the ground first.... and above blob's head
                                if (GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                                        BlobWidth, BlobHeight, 1, true) == 0 &&
                                    GameEngine.GameScreens.CheckCollision(Direction.Up, _blobXPos,
                                        _blobYPos - JumpHeightWithoutPlatforms,
                                        BlobWidth, BlobHeight, 1, true) != 0
                                   )
                                {
                                    _blobYPos = _blobYPos - JumpHeightWithoutPlatforms;
                                    GameEngine.ContentResourceManager.GetSoundFxEngine()
                                        .PlaySound("BlobJumpsNoPlatforms", 1f, false);
                                }
                            }
                        }
                    }

                    var accessingCheops = false;
                    if (GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid != null)
                    {
                        if (CollisionDetection.Check(
                                new Rectangle(
                                    _blobXPos,
                                    _blobYPos,
                                    SpriteMetrics.SpriteWidth,
                                    SpriteMetrics.SpriteHeight),
                                new Rectangle(
                                    GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid.XLocation,
                                    GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid.YLocation,
                                    SpriteMetrics.SpriteWidth,
                                    SpriteMetrics.SpriteHeight)))
                        {
                            accessingCheops = true;
                        }
                    }

                    // Ensure Blob isn't going through a force field
                    var inAccessForceField = false;
                    var forceFieldTile = GameEngine.GameScreens.CheckTileByObject(Direction.None, _blobXPos,
                        _blobYPos, BlobWidth, BlobHeight, 0);

                    if (forceFieldTile is { TileNumber: (int)ActionTiles.AccessForceField })
                    {
                        inAccessForceField = true;
                    }

                    // Pick up an object or drop one perhaps.....
                    if (BlobsControl.MovedUp() &&
                        !BlobsControl.MovedLeft() &&
                        !BlobsControl.MovedRight())
                    {
                        if (!_inventoryAccessed &&
                            !_inTunnelLift &&
                            !inAccessForceField) // Ensure Blob isn't going through a force field
                        {
                            if (accessingCheops)
                            {
                                // Do something with the pyramid.....
                                GameEngine.GetGameScreen(GameScreenType.Cheops).Reset(null);
                                GameEngine.CurrentGameScreen = GameScreenType.Cheops;
                            }
                            else
                            {
                                GameEngine.ContentResourceManager.GetSoundFxEngine()
                                    .PlaySound("BlobPickUp", 0.3f, false);

                                _inventoryAccessed = true;

                                // Collect powerups
                                InteractiveItem? interactiveItemDropped = null;

                                // Pull out the last item and put it on the screen
                                if (BlobInventory[^1] != Collectables.None)
                                {
                                    //gameEngine.GameScreens.C
                                    var itemXPos = _blobXPos / GameEngine.GameScreens.CollAreaWidth *
                                                   GameEngine.GameScreens.CollAreaWidth;
                                    var itemYPos = _blobYPos / GameEngine.GameScreens.CollAreaHeight *
                                                   GameEngine.GameScreens.CollAreaHeight;

                                    interactiveItemDropped = new InteractiveItem(itemXPos, itemYPos,
                                        (int)BlobInventory[^1]);

                                    GameEngine.GameScreens.CurrentGameScreenData.PickUps.Add(interactiveItemDropped);

                                    // Nothing held here now
                                    BlobInventory[^1] = Collectables.None;
                                }

                                // Move everything along one in Blob's inventory
                                for (var b = BlobInventory.Length - 1; b > 0; b--)
                                {
                                    BlobInventory[b] = BlobInventory[b - 1];
                                    BlobInventory[b - 1] = Collectables.None;
                                }

                                // Pick up an item if we can....
                                if (GameEngine.GameScreens.CurrentGameScreenData.PickUps != null)
                                {
                                    for (var a = 0; a < GameEngine.GameScreens.CurrentGameScreenData.PickUps.Count; a++)
                                    {
                                        if (!CollisionDetection.Check(
                                                _blobXPos,
                                                BlobYPos,
                                                BlobWidth,
                                                BlobHeight,
                                                GameEngine.GameScreens.CurrentGameScreenData.PickUps[a].XLocation + SpriteMetrics.SpriteWidth / 4,
                                                GameEngine.GameScreens.CurrentGameScreenData.PickUps[a].YLocation + TileMetrics.CollisionCellHeight,
                                                TileMetrics.CollisionCellWidth,
                                                TileMetrics.CollisionCellHeight))
                                        {
                                            continue;
                                        }

                                        // Make sure it's not what we just dropped....
                                        var pickupItem = !(interactiveItemDropped != null &&
                                                           interactiveItemDropped ==
                                                           GameEngine.GameScreens.CurrentGameScreenData.PickUps[a]);

                                        if (!pickupItem)
                                        {
                                            continue;
                                        }

                                        // Add the item to Blob's inventory
                                        BlobInventory[0] = (Collectables)GameEngine.GameScreens
                                            .CurrentGameScreenData.PickUps[a].Type;

                                        // Remove it from the screen
                                        GameEngine.GameScreens.CurrentGameScreenData.PickUps.Remove(
                                            GameEngine.GameScreens.CurrentGameScreenData.PickUps[a]);
                                        break;
                                    }
                                }

                                GameEngine.GameScreens.UpdateCollisionMap(false);
                            }
                        }
                    }

                    if (!BlobsControl.MovedUp())
                    {
                        _inventoryAccessed = false;
                    }

                    if (!BlobsControl.MovedDown())
                    {
                        _jumpPressed = false;
                    }

                    if (!_blobAnimRight.Repeat)
                    {
                        _blobAnimRight.Repeat = true;
                    }

                    if (!_blobAnimLeft.Repeat)
                    {
                        _blobAnimLeft.Repeat = true;
                    }

                    // Crappy fake gravity routine :p
                    if (!_inTunnelLift) // Can't fall if we are in a tunnel lift
                    {
                        var falling = GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                            BlobWidth, BlobHeight, _blobFallSpeed, true);

                        if (falling > 0)
                        {
                            _blobDistanceFallen = _blobDistanceFallen + falling;
                            _blobTotalDistanceFallen = _blobTotalDistanceFallen + falling;

                            if (_blobDistanceFallen > FallSpeedIncreaseDistance && _blobFallSpeed < BlobMaxFallSpeed)
                            {
                                _blobFallSpeed++;
                                _blobDistanceFallen = 0;
                            }
                        }
                        else
                        {
                            _blobTotalDistanceFallen = 0;
                            _blobDistanceFallen = 0;
                            _blobFallSpeed = BlobInitialFallSpeed;
                            _blobFallHeight = 0;
                        }

                        // Fall - arrrgh!!!
                        var fallDistance = GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                            BlobWidth, BlobHeight, _blobFallSpeed, true);

                        if (fallDistance > 0 && _blobTotalDistanceFallen > FallSoundDistance)
                        {
                            _blobFallsSoundId = GameEngine.ContentResourceManager.GetSoundFxEngine()
                                .PlaySound("BlobFalls", true);
                        }

                        if (falling == 0)
                        {
                            GameEngine.ContentResourceManager.GetSoundFxEngine()
                                .StopSound(_blobFallsSoundId, SoundFade.Fast);
                        }

                        // Check to see if Blob can potentially enter a teleport booth, tunnel etc.
                        if (BlobsControl.MovedRight())
                        {
                            for (var a = 0; a < fallDistance; a++)
                            {
                                if (GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos + BlobWidth,
                                        _blobYPos + a, BlobWidth + 1, BlobHeight, 1, true) == 0)
                                {
                                    if (GameEngine.GameScreens.CheckCollision(Direction.Right, _blobXPos + BlobWidth,
                                            _blobYPos + a, BlobWidth, BlobHeight, 1, true) > 0)
                                    {
                                        fallDistance = a;

                                        if (fallDistance == 0)
                                            fallDistance = 1;

                                        break;
                                    }
                                }
                            }
                        }

                        if (BlobsControl.MovedLeft())
                        {
                            for (var a = 0; a < fallDistance; a++)
                            {
                                if (GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos - BlobWidth,
                                        _blobYPos + a, BlobWidth + 1, BlobHeight, 1, true) == 0)
                                {
                                    if (GameEngine.GameScreens.CheckCollision(Direction.Right, _blobXPos - BlobWidth,
                                            _blobYPos + a, BlobWidth, BlobHeight, 1, true) > 0)
                                    {
                                        fallDistance = a;

                                        if (fallDistance == 0)
                                            fallDistance = 1;

                                        break;
                                    }
                                }
                            }
                        }

                        _blobYPos = _blobYPos + fallDistance;
                        _blobFallHeight = _blobFallHeight + fallDistance;
                    }
                }

                // Fire not on a shuttle
                if (BlobsControl.HasFired())
                {
                    if (GameEngine.BlobStatusPanel.FirePower > 0)
                    {
                        if (_blobCanFire)
                        {
                            // Play sound
                            GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobLazer");
                            _blobFireDistance = 0;

                            BlobFireX = _blobXPos + FireOffset;
                            BlobFireY = _blobYPos + FireOffset;
                            _blobFireFade = 0;

                            _blobCanFire = false;
                            BlobHasFired = true;
                            _blobFireCollisionDirection = FireTravellingStraight;

                            if (OnBlobActionRaised != null)
                            {
                                OnBlobActionRaised(BlobAction.FiredLazer, null);
                            }

                            if (_blobOnShuttle)
                            {
                                _directionBlobFired = DirectionBlobMoved;
                            }
                            else
                            {
                                if (DirectionBlobMoved == Direction.Left)
                                {
                                    _directionBlobFired = Direction.Left;
                                }
                                else if (DirectionBlobMoved == Direction.Right)
                                {
                                    _directionBlobFired = Direction.Right;
                                }
                            }
                        }
                    }
                }

                // Fire routine
                if (BlobHasFired)
                {
                    var hitScreenBoundary = false;

                    if (_blobFireTrail != null)
                    {
                        _blobFireTrail.Start();
                    }

                    if (_blobOnShuttleCached)
                    {
                        if (_directionBlobFired == Direction.Up)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedLeft)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedRight)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }

                            // have we hit game scenary
                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                if (amountToMoveY == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == FireTravellingStraight)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(FireDeflectedLeft, FireDeflectedRight + 1);

                                        if (_blobFireSparks != null)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (BlobFireY < 0)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (_blobFireCollisionDirection == FireTravellingStraight)
                                {
                                    if (amountToMoveY > 0)
                                    {
                                        BlobFireY = BlobFireY - amountToMoveY;
                                    }
                                    else
                                    {
                                        BlobHasFired = false;
                                        _blobCanFire = true;

                                        if (_blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                        }
                                    }
                                }
                            }
                            else if (_blobFireCollisionDirection > 0)
                            {
                                if (BlobFireY + FireLength > GameEngine.GameScreens.ScreenHeight)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                if (_blobFireCollisionDirection == FireDeflectedLeft)
                                {
                                    if (BlobFireX < 0)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == FireDeflectedRight)
                                {
                                    if (BlobFireX + FireLength > GameEngine.GameScreens.ScreenWidth)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireY = BlobFireY + amountToMoveY;

                                    if (_blobFireCollisionDirection == FireDeflectedLeft)
                                    {
                                        BlobFireX = BlobFireX - amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                    else if (_blobFireCollisionDirection == FireDeflectedRight)
                                    {
                                        BlobFireX = BlobFireX + amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                }
                                else
                                {
                                    BlobHasFired = false;
                                    _blobCanFire = true;

                                    if (_blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                    }
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Down)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedLeft)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedRight)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }

                            // have we hit game scenary
                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                if (amountToMoveY == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == FireTravellingStraight)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(FireDeflectedLeft, FireDeflectedRight + 1);

                                        if (_blobFireSparks != null)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (BlobFireY + FireLength > GameEngine.GameScreens.ScreenHeight)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (_blobFireCollisionDirection == FireTravellingStraight)
                                {
                                    if (amountToMoveY > 0)
                                    {
                                        BlobFireY = BlobFireY + amountToMoveY;
                                    }
                                    else
                                    {
                                        BlobHasFired = false;
                                        _blobCanFire = true;

                                        if (_blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                        }
                                    }
                                }
                            }
                            else if (_blobFireCollisionDirection > 0)
                            {
                                if (BlobFireY < 0)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                if (_blobFireCollisionDirection == FireDeflectedLeft)
                                {
                                    if (BlobFireX < 0)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == FireDeflectedRight)
                                {
                                    if (BlobFireY + FireLength > GameEngine.GameScreens.ScreenWidth)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireY = BlobFireY - amountToMoveY;

                                    if (_blobFireCollisionDirection == FireDeflectedLeft)
                                    {
                                        BlobFireX = BlobFireX - amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                    else if (_blobFireCollisionDirection == FireDeflectedRight)
                                    {
                                        BlobFireX = BlobFireX + amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                }
                                else
                                {
                                    BlobHasFired = false;
                                    _blobCanFire = true;

                                    if (_blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                    }
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Left)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedLeft)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedRight)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }

                            if (CheckForBlobCollisionWithObject(
                                    new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                            {
                                if (_blobFireDistance < offset + BlobWidth / 2)
                                {
                                    amountToMoveX = BlobFireSpeed;
                                    amountToMoveY = BlobFireSpeed;
                                }
                            }

                            // have we hit game scenary
                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                if (amountToMoveX == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == FireTravellingStraight)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(FireDeflectedLeft, FireDeflectedRight + 1);

                                        _blobFireSparks?.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (BlobFireX < 0)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (_blobFireCollisionDirection == FireTravellingStraight)
                                {
                                    if (amountToMoveX > 0)
                                    {
                                        BlobFireX -= amountToMoveX;
                                        _blobFireDistance += amountToMoveX;
                                    }
                                    else
                                    {
                                        BlobHasFired = false;
                                        _blobCanFire = true;

                                        if (_blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                        }
                                    }
                                }
                            }
                            else if (_blobFireCollisionDirection > 0)
                            {
                                if (BlobFireX + FireLength > GameEngine.GameScreens.ScreenWidth)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                if (_blobFireCollisionDirection == FireDeflectedLeft)
                                {
                                    if (BlobFireY < 0)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == FireDeflectedRight)
                                {
                                    if (BlobFireY + FireLength > GameEngine.GameScreens.ScreenHeight)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireX = BlobFireX + amountToMoveX;
                                    _blobFireDistance = _blobFireDistance + amountToMoveX;

                                    if (_blobFireCollisionDirection == FireDeflectedLeft)
                                    {
                                        BlobFireY = BlobFireY - amountToMoveY;
                                    }
                                    else if (_blobFireCollisionDirection == FireDeflectedRight)
                                    {
                                        BlobFireY = BlobFireY + amountToMoveY;
                                    }
                                }
                                else
                                {
                                    BlobHasFired = false;
                                    _blobCanFire = true;

                                    if (_blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                    }
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Right)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedLeft)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == FireDeflectedRight)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);
                            }

                            if (CheckForBlobCollisionWithObject(
                                    new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                            {
                                if (_blobFireDistance < offset + BlobWidth / 2)
                                {
                                    amountToMoveX = BlobFireSpeed;
                                    amountToMoveY = BlobFireSpeed;
                                }
                            }

                            //if (CheckForBlobFireCollisionWithObject(new Rectangle(blobFireX, blobFireY, 32, 32)))
                            //{
                            //    amountToMoveX = blobFireSpeed;
                            //    amountToMoveY = blobFireSpeed;
                            //}

                            // have we hit game scenary
                            if (_blobFireCollisionDirection == FireTravellingStraight)
                            {
                                if (amountToMoveX == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == FireTravellingStraight)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(FireDeflectedLeft, FireDeflectedRight + 1);

                                        _blobFireSparks?.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (BlobFireX + FireLength > GameEngine.GameScreens.ScreenWidth)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (_blobFireCollisionDirection == FireTravellingStraight)
                                {
                                    if (amountToMoveX > 0)
                                    {
                                        BlobFireX = BlobFireX + amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                    else
                                    {
                                        BlobHasFired = false;
                                        _blobCanFire = true;

                                        if (_blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                        }
                                    }
                                }
                            }
                            else if (_blobFireCollisionDirection > 0)
                            {
                                if (BlobFireX < 0)
                                {
                                    amountToMoveX = 0;
                                }

                                if (_blobFireCollisionDirection == FireDeflectedLeft)
                                {
                                    if (BlobFireY < 0)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == FireDeflectedRight)
                                {
                                    if (BlobFireY + FireLength > GameEngine.GameScreens.ScreenHeight)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireX -= amountToMoveX;
                                    _blobFireDistance += amountToMoveX;

                                    if (_blobFireCollisionDirection == FireDeflectedLeft)
                                    {
                                        BlobFireY -= amountToMoveY;
                                    }
                                    else if (_blobFireCollisionDirection == FireDeflectedRight)
                                    {
                                        BlobFireY += amountToMoveY;
                                    }
                                }
                                else
                                {
                                    BlobHasFired = false;
                                    _blobCanFire = true;

                                    if (_blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_directionBlobFired == Direction.Left)
                        {
                            var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);

                            if (CheckForBlobCollisionWithObject(
                                    new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                            {
                                if (_blobFireDistance < offset + BlobWidth / 2)
                                {
                                    amountToMove = BlobFireSpeed;
                                }
                            }

                            // Have we crossed the screen boundary
                            if (BlobFireX < 0)
                            {
                                amountToMove = 0;
                                hitScreenBoundary = true;
                            }

                            // Have we hit a nastie
                            if (amountToMove > 0)
                            {
                                BlobFireX -= amountToMove;
                                _blobFireDistance += amountToMove;
                            }
                            else
                            {
                                BlobHasFired = false;
                                _blobCanFire = true;

                                if (_blobFireSparks != null && !hitScreenBoundary)
                                {
                                    _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Right)
                        {
                            var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                BlobFireY, FireWidth, FireLength, BlobFireSpeed, false);

                            if (CheckForBlobCollisionWithObject(
                                    new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                            {
                                if (_blobFireDistance < offset + BlobWidth / 2)
                                {
                                    amountToMove = BlobFireSpeed;
                                }
                            }

                            // Have we crossed the screen boundary
                            if (BlobFireX + FireLength > GameEngine.GameScreens.ScreenWidth)
                            {
                                amountToMove = 0;
                                hitScreenBoundary = true;
                            }

                            // Have we hit a nastie
                            if (amountToMove > 0)
                            {
                                BlobFireX = BlobFireX + amountToMove;
                                _blobFireDistance = _blobFireDistance + amountToMove;
                            }
                            else
                            {
                                BlobHasFired = false;
                                _blobCanFire = true;

                                if (_blobFireSparks != null && !hitScreenBoundary)
                                {
                                    _blobFireSparks.Start(BlobFireX, BlobFireY + FireSparksYOffset);
                                }
                            }
                        }
                        else
                        {
                            // Blob has fired in an invalid direction, probably since he steped off a shuttle pad so kill the fire
                            BlobHasFired = false;
                            _blobCanFire = true;
                        }
                    }

                    _blobAnimFire1.GetNextFrame();
                    _blobAnimFire2.GetNextFrame();
                }

                // Stop the particle effect since the fire has stopped
                if (!BlobHasFired)
                {
                    if (_blobFireTrail != null)
                    {
                        _blobFireTrail.Stop();
                    }

                    _blobOnShuttleCached = _blobOnShuttle;
                }

                // Change screens...?
                if (_blobXPos < 0)
                {
                    if (GameEngine.GameScreens.ScreenNumber - 1 >= 0)
                    {
                        GameEngine.GameScreens.ScreenNumber--;
                        _blobXPos = GameEngine.GameScreens.ScreenWidth - GameEngine.ContentResourceManager
                            .GetSpriteCollection(GameEngine.BlobSpritesName).SpriteWidth;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(GameScreenType.MainGame);
                        }
                    }
                }

                if (_blobXPos + GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName)
                        .SpriteWidth > GameEngine.GameScreens.ScreenWidth)
                {
                    if (GameEngine.GameScreens.ScreenNumber + 1 <= WorldMap.ScreenCount)
                    {
                        GameEngine.GameScreens.ScreenNumber++;
                        _blobXPos = 0;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(GameScreenType.MainGame);
                        }
                    }
                }

                if (_blobYPos < 0)
                {
                    if (GameEngine.GameScreens.ScreenNumber - WorldMap.ScreensAcross >= 0)
                    {
                        GameEngine.GameScreens.ScreenNumber -= WorldMap.ScreensAcross;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(GameScreenType.MainGame);
                        }

                        if (_blobOnShuttle)
                        {
                            _blobYPos = GameEngine.GameScreens.ScreenHeight - BlobHeight;
                        }
                        else // Must be on a jumpplatform unless we're in a tunnel lift.....
                        {
                            if (!_inTunnelLift)
                            {
                                _blobYPos = _blobJumpPlatformEngine.GetNewJumpPlatform(_blobXPos,
                                    GameEngine.GameScreens.ScreenHeight - GameEngine.ContentResourceManager
                                        .GetSpriteCollection(GameEngine.BlobSpritesName).SpriteHeight, 0, 0, false);
                            }
                            else
                            {
                                _blobYPos = GameEngine.GameScreens.ScreenHeight - BlobHeight;
                            }
                        }
                    }
                }

                if (_blobYPos + BlobHeight > GameEngine.GameScreens.ScreenHeight)
                {
                    if (GameEngine.GameScreens.ScreenNumber + WorldMap.ScreensAcross <= WorldMap.ScreenCount)
                    {
                        GameEngine.GameScreens.ScreenNumber += WorldMap.ScreensAcross;
                        _blobYPos = 0;
                        _blobDistanceFallen = 0;
                        _blobFallSpeed = BlobInitialFallSpeed;

                        changedScreens = true;
                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(GameScreenType.MainGame);
                        }
                    }
                }

                _tileAboveBlob = GameEngine.GameScreens.CheckTileByObject(Direction.Up, _blobXPos, _blobYPos,
                    BlobWidth,
                    BlobHeight, BlobVSpeed);

                if (_blobOnShuttle)
                {
                    _tileBelowBlob = GameEngine.GameScreens.CheckTileByObject(Direction.Down, _blobXPos, _blobYPos,
                        BlobWidth, BlobHeight - ShuttleHeight, BlobVSpeed); // Subtract the shuttle height off
                }
                else
                {
                    _tileBelowBlob = GameEngine.GameScreens.CheckTileByObject(Direction.Down, _blobXPos, _blobYPos,
                        BlobWidth, BlobHeight, BlobVSpeed);
                }

                _tileLeftBlob = GameEngine.GameScreens.CheckTileByObject(Direction.Left, _blobXPos, _blobYPos,
                    BlobWidth,
                    BlobHeight, BlobHSpeed);
                _tileRightBlob = GameEngine.GameScreens.CheckTileByObject(Direction.Right, _blobXPos, _blobYPos,
                    BlobWidth, BlobHeight, BlobHSpeed);

                // Detect Blob on a shuttle pad
                _blobOnShuttlePad = false;
                if (_tileBelowBlob != null)
                {
                    if ((ActionTiles)_tileBelowBlob.TileNumber == ActionTiles.ShuttlePlatform ||
                        (ActionTiles)_tileBelowBlob.TileNumber == ActionTiles.RemovedShuttlePlatform)
                    {
                        if (_tileBelowBlob.XPosition == 32 && _tileBelowBlob.YPosition == 0)
                        {
                            _blobOnShuttlePad = true;
                        }
                    }
                }

                // Detect a teleporter
                if (_tileRightBlob != null && BlobsControl.MovedRight())
                {
                    if (_tileRightBlob.TileNumber == (int)ActionTiles.TeleportBooth)
                    {
                        if (_tileRightBlob.XPosition >= 24 &&
                            _tileRightBlob.YPosition == 32)
                        {
                            GameEngine.CurrentGameScreen = GameScreenType.Teleporter;

                            GameEngine.GetGameScreen(GameScreenType.Teleporter).Reset(null);
                            _blobXPos = _tileRightBlob.MapXPosition * TileMetrics.TileWidth + TeleportBoothXOffset;
                            BlobFrame = BlobWalkRightFirstFrame;
                        }
                    }
                }

                // Detect an access booth. Note that each side detects the booth on one side of
                // blob and then reports the map position of the tile on his other side, which is
                // why both tiles have to be there for either to trigger
                if (_tileLeftBlob is { TileNumber: (int)ActionTiles.AccessBoothRight } &&
                    _tileRightBlob != null)
                {
                    if (_tileLeftBlob.XPosition <= 46 &&
                        _tileLeftBlob.YPosition == 32)
                    {
                        GameEngine.GetGameScreen(GameScreenType.AccessBooth).Reset(new object[]
                        {
                            Direction.Left,
                            _tileRightBlob.MapXPosition
                        });
                        GameEngine.CurrentGameScreen = GameScreenType.AccessBooth;
                    }
                }

                if (_tileRightBlob is { TileNumber: (int)ActionTiles.AccessBoothLeft } &&
                    _tileLeftBlob != null)
                {
                    if (_tileRightBlob.XPosition >= 24 &&
                        _tileRightBlob.YPosition == 32)
                    {
                        GameEngine.GetGameScreen(GameScreenType.AccessBooth).Reset(new object[]
                        {
                            Direction.Right,
                            _tileLeftBlob.MapXPosition
                        });
                        GameEngine.CurrentGameScreen = GameScreenType.AccessBooth;
                    }
                }

                // Detect a lefthand tunnel
                if (_tileLeftBlob?.TileNumber is
                    TunnelTiles.LeftMouth1 or TunnelTiles.LeftMouth2 or TunnelTiles.LeftMouth3 or
                    TunnelTiles.LeftMouth4 or TunnelTiles.LeftMouth5 or TunnelTiles.LeftMouth6 or
                    TunnelTiles.LeftMouth7 or TunnelTiles.LeftMouth8 or TunnelTiles.LeftMouth9 or
                    TunnelTiles.LeftMouth10)
                {
                    if (_tileLeftBlob.XPosition <= 32 && _tileLeftBlob.YPosition == 32)
                    {
                        GameEngine.GameScreens.ScreenNumber--;
                        _directionGoingThroughTunnel = Direction.Left;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            GameEngine.ScreenEffectEngine.ExecuteScreenEffect(ScreenEffectType.LeftTunnelEntry, 80,
                                -1, true);
                            GameEngine.ContentResourceManager.GetSoundFxEngine()
                                .PlaySound("TunnelSwish1", 0.8f, false);
                            OnScreenChanged(GameScreenType.None);
                        }
                    }
                }

                // Detect a righthand tunnel
                if (_tileRightBlob?.TileNumber is
                    TunnelTiles.RightMouth1 or TunnelTiles.RightMouth2 or TunnelTiles.RightMouth3 or
                    TunnelTiles.RightMouth4 or TunnelTiles.RightMouth5 or TunnelTiles.RightMouth6 or
                    TunnelTiles.RightMouth7 or TunnelTiles.RightMouth8 or TunnelTiles.RightMouth9 or
                    TunnelTiles.RightMouth10)
                {
                    if (_tileRightBlob.XPosition >= 32 && _tileRightBlob.YPosition == 32)
                    {
                        GameEngine.GameScreens.ScreenNumber++;
                        _directionGoingThroughTunnel = Direction.Right;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            GameEngine.ScreenEffectEngine.ExecuteScreenEffect(ScreenEffectType.RightTunnelEntry,
                                80, -1, true);
                            GameEngine.ContentResourceManager.GetSoundFxEngine()
                                .PlaySound("TunnelSwish2", 0.8f, false);
                            OnScreenChanged(GameScreenType.None);
                        }
                    }
                }

                // Breakable bridges
                if (_tileBelowBlob is { TileNumber: (int)ActionTiles.BreakableBridges })
                {
                    if (_blobFallHeight >= BridgeBreakingFallHeight)
                    {
                        // Play the sound
                        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BridgeBreak");

                        // Add the tile to the list of tiles to reset on screen change
                        _tilesToReset.Add(new ResetTile(_tileBelowBlob, GameEngine.GameScreens.ScreenNumber));

                        // Remove the tile
                        GameEngine.GameScreens.CurrentGameScreenData
                            .ScreenTiles[_tileBelowBlob.MapXPosition, _tileBelowBlob.MapYPosition].TileImage = -1;

                        // Add the tile to the Breakable bridges engine
                        GameEngine.BreakableBridgesEngine.AddBridge(
                            GameEngine.GameScreens.ConvertToScreenCoords(_tileBelowBlob.MapXPosition,
                                _tileBelowBlob.MapYPosition));
                    }
                }

                // Tunnel lifts
                if (_tileAboveBlob != null)
                {
                    switch (_tileAboveBlob.TileNumber)
                    {
                        case (int)ActionTiles.TunnelLiftShaft1 or (int)ActionTiles.TunnelLiftShaft2 or
                            (int)ActionTiles.TunnelLiftTile or (int)ActionTiles.TunnelLiftShaft3:
                        {
                            if (_tileAboveBlob.XPosition == 32)
                            {
                                if (!_inTunnelLift)
                                {
                                    _distanceInTunnelLift = 0;
                                }

                                _inTunnelLift = true;
                            }

                            break;
                        }
                        case (int)ActionTiles.TunnelLiftTop1 or (int)ActionTiles.TunnelLiftTop2 or
                            (int)ActionTiles.TunnelLiftTop3:
                        {
                            if (_tileAboveBlob.YPosition == 96)
                            {
                                _inTunnelLift = false;
                            }

                            break;
                        }
                    }
                }

                if (_inTunnelLift)
                {
                    var moveBlob = true;
                    if (_tileAboveBlob?.TileNumber is (int)ActionTiles.TunnelLiftShaft1 or
                        (int)ActionTiles.TunnelLiftShaft2 or (int)ActionTiles.TunnelLiftShaft3)
                    {
                        if (_tileAboveBlob.YPosition == 96)
                        {
                            if (BlobsControl.MovedLeft() && GameEngine.GameScreens.CheckCollision(Direction.Left,
                                    _blobXPos, _blobYPos, BlobWidth, BlobHeight, BlobHSpeed, true) > 0)
                            {
                                if (_distanceInTunnelLift > TunnelLiftMinimumRide)
                                {
                                    _inTunnelLift = false;
                                    moveBlob = false;
                                }
                            }
                            else if (BlobsControl.MovedRight() &&
                                     GameEngine.GameScreens.CheckCollision(Direction.Right, _blobXPos, _blobYPos,
                                         BlobWidth, BlobHeight, BlobHSpeed, true) > 0)
                            {
                                if (_distanceInTunnelLift > TunnelLiftMinimumRide)
                                {
                                    _inTunnelLift = false;
                                    moveBlob = false;
                                }
                            }
                        }
                    }

                    if (moveBlob)
                    {
                        _blobTunnelLiftSoundId = GameEngine.ContentResourceManager.GetSoundFxEngine()
                            .PlaySound("TunnelLift", true);
                        _blobYPos -= TunnelLiftSpeed;
                        _distanceInTunnelLift += TunnelLiftSpeed;
                    }
                    else
                    {
                        GameEngine.ContentResourceManager.GetSoundFxEngine()
                            .StopSound(_blobTunnelLiftSoundId, SoundFade.None);
                    }
                }

                // Spiky tiles
                var tileIntersectingBlob =
                    GameEngine.GameScreens.CheckTileByObject(Direction.None, _blobXPos, _blobYPos, BlobWidth,
                        BlobHeight, 0);

                if (tileIntersectingBlob != null)
                {
                    // Central spiky tiles
                    if (tileIntersectingBlob.TileNumber == (int)ActionTiles.Spike2)
                    {
                        if (tileIntersectingBlob.XPosition is > SpikeReachLeft and < SpikeReachRight &&
                            tileIntersectingBlob.YPosition > CentralSpikeReachUp)
                        {
                            KillBlob(true, false);
                        }
                    }

                    // Right side mounted spikey tiles
                    if (tileIntersectingBlob.TileNumber == (int)ActionTiles.Spike1)
                    {
                        if (tileIntersectingBlob.YPosition is < WallSpikeReachDown and > WallSpikeReachUp &&
                            tileIntersectingBlob.XPosition > WallSpikeReachIn)
                        {
                            KillBlob(true, false);
                        }
                    }

                    // Broken bottle spiky tiles
                    if (tileIntersectingBlob.TileNumber == (int)ActionTiles.Spike3)
                    {
                        if (tileIntersectingBlob.XPosition is > SpikeReachLeft and < SpikeReachRight &&
                            tileIntersectingBlob.YPosition > BrokenGlassReachUp)
                        {
                            KillBlob(true, false);
                        }
                    }
                }

                // Collect powerups
                if (GameEngine.GameScreens.CurrentGameScreenData.PowerUp != null)
                {
                    if (CollisionDetection.Check(
                            _blobXPos,
                            BlobYPos,
                            BlobWidth,
                            BlobHeight,
                            GameEngine.GameScreens.CurrentGameScreenData.PowerUp.XLocation,
                            GameEngine.GameScreens.CurrentGameScreenData.PowerUp.YLocation,
                            TileMetrics.CollisionCellWidth,
                            TileMetrics.CollisionCellHeight))
                    {
                        if (OnBlobActionRaised != null)
                        {
                            OnBlobActionRaised(BlobAction.GotPowerUp,
                                GameEngine.GameScreens.CurrentGameScreenData.PowerUp.Type);
                        }

                        GameEngine.GameScreens.CurrentGameScreenData.PowerUp = null;
                        GameEngine.GameScreens.UpdateCollisionMap(false);
                    }
                }

                _blobJumpPlatformEngine.Update(GameEngine.GameScreens);
                break;
            }
            case BlobState.Overloading:
            {
                // Play the alarm sound for a bit
                if (_blobAlarmSoundCounter == 1)
                {
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobOverloadAlarm", 1.0f, false);
                }

                if (_blobOverloadState < 3 && _blobAlarmSoundCounter < 60)
                {
                    _blobAlarmSoundCounter++;
                }
                else
                {
                    if (!GameEngine.BlobKiller.Triggered)
                    {
                        BlobHasFired = false;
                        _blobCanFire = true;
                        _blobFireTrail.Stop();

                        _blobJumpPlatformEngine.ResetPlatforms();
                        GameEngine.ElectricityNodeGateEngine.Reset();

                        if (_blobOverloadState == 0)
                        {
                            _blobScale = _blobScale + 0.30f;

                            if (_blobScale > 1.6f)
                            {
                                _blobOverloadState = 1;
                                _blobScale = 1.6f;
                            }
                        }
                        else if (_blobOverloadState == 1)
                        {
                            _blobScale = _blobScale - 0.30f;

                            if (_blobScale < -1f)
                            {
                                _blobScale = -1f;
                                _blobOverloadState = 3;
                            }
                        }

                        if (_blobOverloadState == 3)
                        {
                            GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobExplodes", false);
                            GameEngine.BlobKiller.Trigger(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos, true);
                            BlobsState = BlobState.Dying;
                        }
                    }
                }

                break;
            }
            case BlobState.Dying:
            {
                if (!GameEngine.BlobKiller.Triggered)
                {
                    _blobOverloadState = 0;
                    _blobScale = 1.0f;
                    // If Blob is killed by a spiky tile, we reset his position to when he entered
                    if (_resetBlobToScreenEntryPoint)
                    {
                        _blobYPos = _blobScreenEntryLocation.Y;
                        _blobXPos = _blobScreenEntryLocation.X;
                        _blobOnShuttle = _blobOnShuttleOnEntry;
                        GameEngine.GameScreens.ChangeShuttlePlatformState(_blobOnShuttle);
                        GameEngine.GameScreens.UpdateCollisionMap(false);
                        BlobFrame = _blobFrameOnEntry;
                        _inTunnelLift = false;
                        _blobFallSpeed = BlobInitialFallSpeed;

                        if (_blobOnShuttle)
                            BlobHeight = BlobHeightOnShuttle;
                        else
                            BlobHeight = SpriteMetrics.SpriteHeight;
                    }

                    _resetBlobToScreenEntryPoint = false;
                    BlobsState = BlobState.Dead;
                    GameEngine.BlobKiller.Reset();
                }

                break;
            }
        }

        // Update the teleport beam effect - probably should have a boolean to stop drawing when not needed.....
        _blobFireSparks?.Update(gameTime);
        _blobTeleporterBeam?.Update(_blobXPos + TeleportBeamXOffset, _blobYPos + TeleportBeamYOffset, gameTime);
        _blobFireTrail?.Update(BlobFireX + FireTrailOffset, BlobFireY + FireTrailOffset, gameTime);

        if (!changedScreens)
        {
            return;
        }

        _blobScreenEntryLocation = new Point(BlobXPos, BlobYPos);
        _blobOnShuttleOnEntry = _blobOnShuttle;
        _blobFrameOnEntry = BlobFrame;
    }

    public void UnstickBlobFromTiles()
    {
        // Is blob stuck at the top?
        if (GameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, BlobWidth, UnstickProbeThickness, true) >= 0)
        {
            // And not stuck at the bottom?
            if (GameEngine.GameScreens.CheckRectangles(
                    _blobXPos,
                    _blobYPos + (BlobHeight - UnstickProbeThickness),
                    BlobWidth,
                    UnstickProbeThickness,
                    true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                var distanceToMoveY = 0;
                while (GameEngine.GameScreens.CheckRectangles(
                           _blobXPos,
                           _blobYPos + distanceToMoveY,
                           BlobWidth,
                           UnstickProbeThickness,
                           true) >= 0)
                {
                    distanceToMoveY++;
                }

                // Shift Blob to a safe position
                _blobYPos += distanceToMoveY;
            }
        }

        if (GameEngine.GameScreens.CheckRectangles(
                _blobXPos,
                _blobYPos + (BlobHeight - UnstickProbeThickness),
                BlobWidth,
                UnstickProbeThickness, true) >= 0) // Is Blob stuck at the bottom
        {
            // And not stuck at the top?
            if (GameEngine.GameScreens.CheckRectangles(
                    _blobXPos,
                    _blobYPos,
                    BlobWidth,
                    UnstickProbeThickness,
                    true) < 0)
            {
                // He's stuck at the bottom so lets figure out how far we need to go to release him
                var distanceToMoveY = 0;
                while (GameEngine.GameScreens.CheckRectangles(
                           _blobXPos,
                           _blobYPos + (BlobHeight - UnstickProbeThickness) - distanceToMoveY, BlobWidth, UnstickProbeThickness, true) >= 0)
                {
                    distanceToMoveY++;
                }

                // Shift Blob to a safe position
                _blobYPos -= distanceToMoveY;
            }
        }

        if (GameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, UnstickProbeThickness, BlobHeight, true) >=
            0) // Is Blob stuck at the left
        {
            // And not stuck at the right?
            if (GameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - UnstickProbeThickness), _blobYPos, UnstickProbeThickness, BlobHeight, true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                var distanceToMoveX = 0;
                while (GameEngine.GameScreens.CheckRectangles(_blobXPos + distanceToMoveX, _blobYPos, UnstickProbeThickness, BlobHeight,
                           true) >= 0)
                {
                    distanceToMoveX++;
                }

                // Shift Blob to a safe position
                _blobXPos += distanceToMoveX;
            }
        }

        if (GameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - UnstickProbeThickness), _blobYPos, UnstickProbeThickness, BlobHeight, true) >=
            0) // Is Blob stuck at the right
        {
            // And not stuck at the left?
            if (GameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, UnstickProbeThickness, BlobHeight, true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                var distanceToMoveX = 0;
                while (GameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - UnstickProbeThickness) - distanceToMoveX,
                           _blobYPos, UnstickProbeThickness, BlobHeight, true) >= 0)
                {
                    distanceToMoveX++;
                }

                // Shift Blob to a safe position
                _blobXPos -= distanceToMoveX;
            }
        }
    }

    public void PerformPostChangeScreenActions()
    {
        // Reset blob's fire
        BlobHasFired = false;
        _blobCanFire = true;
        _blobFireCollisionDirection = FireTravellingStraight;

        BlobFireX = _blobXPos + FireOffset;
        BlobFireY = _blobYPos + FireOffset;
        _blobFireFade = 0;

        _blobJumpPlatformEngine.ResetPlatforms();

        // Unstick Blob from the screenmap if he's got stuck due to a tile map change
        UnstickBlobFromTiles();

        if (_directionGoingThroughTunnel != Direction.None)
        {
            if (_directionGoingThroughTunnel == Direction.Left)
            {
                for (var a = TunnelTiles.RightMouth1; a <= TunnelTiles.LastMouth; a += TunnelTiles.MouthStride)
                {
                    var tiles = GameEngine.GameScreens.GetCoordsOfTiles(a);

                    if (tiles is not { Count: 1 })
                    {
                        continue;
                    }

                    _blobXPos = tiles[0].X + TunnelExitLeftXOffset;
                    _blobYPos = tiles[0].Y + TunnelExitYOffset;
                    break;
                }
            }
            else if (_directionGoingThroughTunnel == Direction.Right)
            {
                for (var a = TunnelTiles.FirstMouth; a <= TunnelTiles.LastMouth + 1; a += TunnelTiles.MouthStride)
                {
                    var tiles = GameEngine.GameScreens.GetCoordsOfTiles(a);

                    if (tiles is not { Count: 1 })
                    {
                        continue;
                    }

                    _blobXPos = tiles[0].X + TunnelExitRightXOffset;
                    _blobYPos = tiles[0].Y + TunnelExitYOffset;
                    break;
                }
            }

            _directionGoingThroughTunnel = Direction.None;
        }

        foreach (var t in _tilesToReset)
        {
            var screenTiles = GameEngine.GameScreens.GetGameScreen(t.ScreenNumber).ScreenTiles;
            if (screenTiles != null)
                screenTiles[
                    t.TileToReset.MapXPosition,
                    t.TileToReset.MapYPosition].TileImage = t.TileToReset.TileNumber;
        }

        _tilesToReset.Clear();
        _blobFireSparks?.Reset();
        _blobFireTrail?.Reset();
    }

    public override void Draw(
        GameTime gameTime,
        int screenFade)
    {
        switch (BlobsState)
        {
            case BlobState.Normal:
            {
                // Draw the fire trail particles
                _blobFireTrail?.Draw();

                // Draw blob
                if (BlobTouchedByNasty)
                {
                    // Reset the counter
                    if (!_blobTouched)
                    {
                        _blobEnergyDrainCounter = ColourLevels.Full;
                        _blobTouched = true;
                    }

                    if (_blobEnergyDrainCounter > 0)
                        _blobEnergyDrainCounter -= EnergyDrainFadeStep;

                    if (_blobEnergyDrainCounter < 0)
                        _blobEnergyDrainCounter = 0;
                }
                else
                {
                    if (_blobEnergyDrainCounter < ColourLevels.Full)
                        _blobEnergyDrainCounter += EnergyDrainFadeStep;

                    if (_blobEnergyDrainCounter > ColourLevels.Full)
                    {
                        _blobEnergyDrainCounter = ColourLevels.Full;
                    }

                    _blobTouched = false;
                }

                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName)
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).DrawSprite(BlobFrame,
                    _blobXPos, _blobYPos,
                    ColourHelper.GetXna4Colour(ColourLevels.Full, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                        (byte)(_teleportFadeLevel * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).End();

                // Draw other bits
                if (_blobOnShuttle)
                {
                    if (GameEngine.UsingOldGraphics)
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal")
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").DrawSprite(
                            _blobShuttleAnim.CurrentFrame, _blobXPos, _blobYPos + SpriteMetrics.SpriteHeight,
                            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").End();
                    }
                    else
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle")
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, _blobXPos,
                            _blobYPos + SpriteMetrics.SpriteHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
                    }
                }

                _blobJumpPlatformEngine.Draw(gameTime);

                if (BlobHasFired)
                {
                    if (_blobFireFade < ColourLevels.Full)
                    {
                        _blobFireFade = _blobFireFade + 16;
                    }

                    if (_blobFireFade > ColourLevels.Full)
                    {
                        _blobFireFade = ColourLevels.Full;
                    }

                    if (GameEngine.UsingOldGraphics)
                    {
                        if (_blobOnShuttle)
                        {
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                                .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                                _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY,
                                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                                    (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), 1.0f, 0f, true,
                                GameEngine.Paused);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                        }
                        else
                        {
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1")
                                .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").DrawSprite(
                                _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY - FireOffset,
                                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                                    (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), 1.0f, 0f, true,
                                GameEngine.Paused);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").End();
                        }
                    }
                    else
                    {
                        if (_blobOnShuttle)
                        {
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2")
                                .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").DrawSprite(
                                _blobAnimFire2.CurrentFrame, BlobFireX, BlobFireY,
                                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                                    (byte)(_blobFireFade * ((double)screenFade / ColourLevels.Full))), 1.0f, 10f,
                                true,
                                GameEngine.Paused);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").End();
                        }
                    }
                }

                // Draw teleport beam effect
                if (_startBlobTeleporting)
                {
                    if (_blobTeleporterBeam != null)
                    {
                        _blobTeleporterBeam.Draw();
                    }
                }

                if (_teleportFadeLevel < ColourLevels.Full)
                {
                    _teleportFadeLevel += TeleportFadeStep;
                }

                if (_teleportFadeLevel > ColourLevels.Full)
                {
                    _teleportFadeLevel = ColourLevels.Full;
                }

                break;
            }
            case BlobState.Overloading when _blobAlarmSoundCounter < 60:
            {
                // Make blob flicker
                if (_blobAlarmSoundCounter % 2 == 0)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName)
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).DrawSprite(
                        BlobFrame, _blobXPos, _blobYPos,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                            (byte)(_teleportFadeLevel * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).End();
                }

                // Draw other bits
                if (_blobOnShuttle)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, _blobXPos,
                        _blobYPos + SpriteMetrics.SpriteHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
                }

                break;
            }
            case BlobState.Overloading:
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName)
                    .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

                var temp = _blobScale - 0.4f;
                for (var a = 0; a < 5; a++)
                {
                    if (temp >= 0f)
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName)
                            .DrawSprite(BlobFrame, _blobXPos, _blobYPos,
                                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, 150), temp, 0f);
                    }

                    temp += 0.2f;
                }

                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).End();
                break;
            }
        }
    }

    public void DrawUpper(int screenFade)
    {
        _blobFireSparks?.Draw(screenFade);
    }

    public void Reset()
    {
        // Reset blob's energy drain counter
        _blobEnergyDrainCounter = 0;

        // Reset Blob's animation frame
        BlobFrame = 5;

        // Reset Blob's state
        BlobsState = BlobState.Normal;

        // Reset Blob's position
        _blobXPos = 416;
        _blobYPos = 414;

        // Remove Blob from shuttle pads
        _blobOnShuttle = false;
        BlobHeight = SpriteMetrics.SpriteHeight;
        GameEngine.GameScreens.ChangeShuttlePlatformState(false);

        // Clear up any rogue collision tiles etc.
        PerformPostChangeScreenActions();
    }

    /// <summary>
    ///     Gets collision objects on the current screen which aren't tiles i.e Cheops Pyramids, CoreElements
    /// </summary>
    private List<InteractiveItem> GetCollisionObjects()
    {
        var temp = new List<InteractiveItem>();
        if (GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid != null)
        {
            temp.Add(GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid);
        }

        foreach (var t in GameEngine.GameScreens.CurrentGameScreenData.PickUps)
        {
            temp.Add(t);
        }

        return temp;
    }

    private void blobTeleporterBeam_ParticleEffectComplete()
    {
        _startBlobTeleporting = false;
    }

    private bool CheckForBlobCollisionWithObject(Rectangle blobsPosition, out int offset)
    {
        offset = 0;

        foreach (var t in GetCollisionObjects())
        {
            if (!CollisionDetection.Check(blobsPosition, new Rectangle(t.XLocation, t.YLocation, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight)))
            {
                continue;
            }

            offset = blobsPosition.X - t.XLocation;
            if (offset < 0)
            {
                offset = -offset;
            }

            return true;
        }

        return false;
    }
}