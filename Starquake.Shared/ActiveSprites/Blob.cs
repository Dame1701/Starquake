// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites.BlobParts;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.GameComponents;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Particles;
using Starquake.Scoring;
using Starquake.SharedLibs;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class Blob : GameComponentBase
{
    /// <summary>
    ///     How fast the tunnel transition plays, in pixels of play area a frame. It sets the pace of
    ///     the whole passage rather than a literal step, since
    ///     <see cref="TunnelEntryEffect" /> eases the motion - the squeeze takes as long as it would
    ///     have at this many pixels a frame, and the dark and the arrival follow from that.
    /// </summary>
    private const int TunnelEntrySpeed = 64;

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
    private readonly Animation _blobAnimFire1;
    private readonly Animation _blobAnimFire2;

    private readonly Animation _blobAnimLeft;
    private readonly Animation _blobAnimRight;

    private readonly BlobFire _blobFire;
    private readonly IPositionalTriggerParticleEffect _blobFireSparks;
    private readonly IStandardParticleEffect _blobFireTrail;

    private readonly JumpPlatforms _blobJumpPlatformEngine;
    private readonly BlobDeathAnimation _blobKiller;

    private readonly Animation _blobShuttleAnim;

    private readonly IStandardParticleEffect _blobTeleporterBeam;
    private readonly BreakableBridges _breakableBridges;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly ScreenEffectEngine _screenEffects;
    private readonly GameSession _session;
    private readonly PlayerStats _stats;
    private readonly List<ResetTile> _tilesToReset = [];
    private int _blobAlarmSoundCounter;

    private bool _blobCanFire = true;
    private int _blobDistanceFallen;

    private int _blobEnergyDrainCounter;
    private int _blobFallHeight;

    private int _blobFallSpeed = BlobInitialFallSpeed;
    private Guid _blobFallsSoundId;
    private int _blobFireFade;

    private int _blobFrameOnEntry;
    private bool _blobOnShuttle;
    private bool _blobOnShuttleCached;
    private bool _blobOnShuttleOnEntry;
    private bool _blobOnShuttlePad;
    private int _blobOverloadState;

    private float _blobScale = 1f;

    private Point _blobScreenEntryLocation;
    private Direction _blobsLastAnimDirection = Direction.None;
    private int _blobTotalDistanceFallen;
    private bool _blobTouched;

    private Guid _blobTunnelLiftSoundId = Guid.Empty;

    private int _blobXPos;

    private int _blobYPos;

    private Direction _directionBlobFired = Direction.None;
    private Direction _directionGoingThroughTunnel = Direction.None;
    private int _distanceInTunnelLift;
    private bool _inTunnelLift;
    private readonly Inventory _inventory;
    private readonly UnstickProbe _unstickProbe;

    private bool _inventoryAccessed;

    private bool _jumpPressed;
    private bool _resetBlobToScreenEntryPoint;
    private bool _startBlobTeleporting;

    private int _teleportFadeLevel;

    private TileDetected? _tileAboveBlob;
    private TileDetected? _tileBelowBlob;
    private TileDetected? _tileLeftBlob;
    private TileDetected? _tileRightBlob;

    public Blob(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsDeviceManager graphics,
        GraphicsSet graphicsSet,
        ScreenDirectory screenDirectory,
        ScreenEffectEngine screenEffects,
        PlayerStats stats,
        GameSession session,
        BlobDeathAnimation blobKiller,
        BreakableBridges breakableBridges,
        Random randomNumberGenerator,
        ToggleKeys input)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _screenDirectory = screenDirectory;
        _screenEffects = screenEffects;
        _stats = stats;
        _session = session;
        _blobKiller = blobKiller;
        _breakableBridges = breakableBridges;

        _blobJumpPlatformEngine = new JumpPlatforms(
            resources,
            gameScreens,
            graphics,
            graphicsSet,
            randomNumberGenerator,
            screenEffects);
        BlobsControl = new CustomControl(session.PlayerKeys, input);
        _blobTouched = false;
        _teleportFadeLevel = ColourLevels.Full;
        _startBlobTeleporting = false;
        BlobsState = BlobState.Normal;

        _blobTeleporterBeam =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("BeamMeUp_Small",
                ParticleEffectType.Triggered, null);
        _blobFireTrail =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("FireTrail", ParticleEffectType.Continual,
                null);

        _blobFireSparks =
            (IPositionalTriggerParticleEffect)_resources.GetParticleEffectInstance("FireSparks",
                ParticleEffectType.TriggeredMultiLocationTimed, null);

        _blobTeleporterBeam.ParticleEffectComplete += blobTeleporterBeam_ParticleEffectComplete;

        _blobFire = new BlobFire(new FireSurroundings(gameScreens, randomNumberGenerator, this));

        // Fire location starts same as Blob's screen location
        _blobFire.Launch(_blobXPos + FireOffset, _blobYPos + FireOffset);
        _blobFireFade = 0;

        // Every one of these loops forward with no yoyo except the left walk, which runs backwards
        // through the same frames, and the shuttle fire, which is slower
        _blobShuttleAnim = LoopingAnimation(0, ShuttleLastFrame, AnimationDelay, true);
        _blobAnimLeft = LoopingAnimation(BlobWalkLeftFirstFrame, BlobWalkLeftLastFrame, AnimationDelay, false);
        _blobAnimRight = LoopingAnimation(BlobWalkRightFirstFrame, BlobWalkRightLastFrame, AnimationDelay, true);
        _blobAnimFire1 = LoopingAnimation(0, FireAnimLastFrame, AnimationDelay, true);
        _blobAnimFire2 = LoopingAnimation(0, ShuttleFireAnimLastFrame, ShuttleFireAnimationDelay, true);

        _directionBlobFired = Direction.Right;

        _inventory = new Inventory();
        _unstickProbe = new UnstickProbe(gameScreens.Collisions);

        // Temporary test code to give Blob Access card and Key
        BlobInventory[1] = Collectables.Key;
        BlobInventory[0] = Collectables.AccessCard;
    }

    /// <summary>
    ///     What the blob is carrying. The queue itself is <see cref="Inventory" />'s; this is what
    ///     the status panel draws and the access booths read.
    /// </summary>
    public Collectables[] BlobInventory => _inventory.Items;

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

    public int BlobFireX => _blobFire.X;

    public int BlobFireY => _blobFire.Y;

    public bool BlobHasFired { get; private set; }

    public CustomControl BlobsControl { get; }

    public event RaiseScreenChanged? OnScreenChanged;

    public event RaiseBlobAction? OnBlobActionRaised;

    /// <summary>
    ///     Raised as the blob is put back together after a death, so anything holding hazard state
    ///     can clear it. The engine answers it by resetting the electricity nodes and gates.
    ///     <para>
    ///         An event rather than a call, because the electricity engine needs the blob to know
    ///         where it is standing and so cannot be built before it. That was the last cycle in the
    ///         graph, and it was this one line.
    ///     </para>
    /// </summary>
    public event Action? OnBlobDiedAndRespawned;

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
        bool changedScreens = false;

        switch (BlobsState)
        {
            case BlobState.Normal:
            {
                changedScreens = UpdateNormalState();
                break;
            }
            case BlobState.Overloading:
            {
                UpdateOverloadingState();
                break;
            }
            case BlobState.Dying:
            {
                UpdateDyingState();
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

    /// <summary>
    ///     One frame of blob being alive and in control. Returns true if the frame took him onto a
    ///     different screen, which is what tells <see cref="Update" /> to record a new entry point.
    /// </summary>
    private bool UpdateNormalState()
    {
        HandleShuttlePad();
        WalkLeft();
        WalkRight();

        if (_blobOnShuttle)
        {
            MoveOnShuttle();
        }
        else
        {
            LayJumpPlatformOrJump();
            AccessInventory();
            ReleaseHeldControls();
            ApplyGravity();
        }

        StartFiring();
        UpdateFire();
        StopFireEffects();

        bool changedScreens = ChangeScreenIfOffEdge();

        DetectSurroundingTiles();
        DetectShuttlePad();
        DetectTeleporter();
        DetectAccessBooth();
        changedScreens |= DetectLeftHandTunnel();
        changedScreens |= DetectRightHandTunnel();
        BreakBridgeUnderfoot();
        RideTunnelLift();
        CheckSpikes();
        CollectPowerUp();

        _blobJumpPlatformEngine.Update(_gameScreens);

        return changedScreens;
    }

    private void HandleShuttlePad()
    {
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
                            _gameScreens.ChangeShuttlePlatformState(true);
                            BlobHeight = BlobHeightOnShuttle;
                            _blobOnShuttle = true;
                        }

                        break;
                    }
                    case (int)ActionTiles.RemovedShuttlePlatform:
                    {
                        if (BlobsControl.MovedLeft() || BlobsControl.MovedRight())
                        {
                            _gameScreens.ChangeShuttlePlatformState(false);
                            BlobHeight = SpriteMetrics.SpriteHeight;
                            _blobOnShuttle = false;
                        }

                        break;
                    }
                }

                // Update the collision map for the current screen
                _gameScreens.UpdateCollisionMap(false);
            }
        }
    }

    private void WalkLeft()
    {
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

            int amountToMove = _gameScreens.CheckCollision(Direction.Left, _blobXPos, _blobYPos,
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
                _resources.GetSoundFxEngine()
                    .PlaySound("BlobRun", true);
            }
        }
    }

    private void WalkRight()
    {
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

            int amountToMove = _gameScreens.CheckCollision(Direction.Right, _blobXPos, _blobYPos,
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
                _resources.GetSoundFxEngine().PlaySound("BlobRun", true);
            }
        }
    }

    private void MoveOnShuttle()
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
            _blobYPos = _blobYPos - _gameScreens.CheckCollision(Direction.Up, _blobXPos,
                _blobYPos,
                BlobWidth, BlobHeight, BlobVSpeed, true);
            DirectionBlobMoved = Direction.Up;
        }

        if (BlobsControl.MovedDown())
        {
            _blobYPos = _blobYPos + _gameScreens.CheckCollision(Direction.Down, _blobXPos,
                _blobYPos, BlobWidth, BlobHeight, BlobVSpeed, true);
            DirectionBlobMoved = Direction.Down;
        }
    }

    private void LayJumpPlatformOrJump()
    {
        if (BlobsControl.MovedDown() &&
            !BlobsControl.MovedLeft() &&
            !BlobsControl.MovedRight())
        {
            if (!_jumpPressed)
            {
                _jumpPressed = true;

                if (_stats.Platforms > 0)
                {
                    if (!_blobOnShuttlePad)
                    {
                        int newYPos = _blobJumpPlatformEngine.GetNewJumpPlatform(_blobXPos, _blobYPos,
                            BlobWidth,
                            BlobHeight, true);

                        if (newYPos != JumpPlatforms.NoRoomForPlatform)
                        {
                            _blobYPos = newYPos;

                            _resources.GetSoundFxEngine()
                                .PlaySound("BlobJumps", 0.3f, false);

                            if (OnBlobActionRaised != null)
                            {
                                OnBlobActionRaised(BlobAction.LaidPlatform, null);
                            }
                        }
                    }
                }
                else // just jump aimlessly in the air
                {
                    // Check that we are on the ground first.... and above blob's head
                    if (_gameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                            BlobWidth, BlobHeight, 1, true) == 0 &&
                        _gameScreens.CheckCollision(Direction.Up, _blobXPos,
                            _blobYPos - JumpHeightWithoutPlatforms,
                            BlobWidth, BlobHeight, 1, true) != 0
                       )
                    {
                        _blobYPos = _blobYPos - JumpHeightWithoutPlatforms;
                        _resources.GetSoundFxEngine()
                            .PlaySound("BlobJumpsNoPlatforms", 1f, false);
                    }
                }
            }
        }
    }

    private void AccessInventory()
    {
        bool accessingCheops = false;
        if (_gameScreens.CurrentGameScreenData.CheopsPyramid != null)
        {
            if (CollisionDetection.Check(
                    new Rectangle(
                        _blobXPos,
                        _blobYPos,
                        SpriteMetrics.SpriteWidth,
                        SpriteMetrics.SpriteHeight),
                    new Rectangle(
                        _gameScreens.CurrentGameScreenData.CheopsPyramid.XLocation,
                        _gameScreens.CurrentGameScreenData.CheopsPyramid.YLocation,
                        SpriteMetrics.SpriteWidth,
                        SpriteMetrics.SpriteHeight)))
            {
                accessingCheops = true;
            }
        }

        // Ensure Blob isn't going through a force field
        bool inAccessForceField = false;
        TileDetected? forceFieldTile = _gameScreens.CheckTileByObject(Direction.None, _blobXPos,
            _blobYPos, BlobWidth, BlobHeight, 0);

        if (forceFieldTile is { TileNumber: (int)ActionTiles.AccessForceField })
        {
            inAccessForceField = true;
        }

        if (BlobsControl.MovedUp() &&
            !BlobsControl.MovedLeft() &&
            !BlobsControl.MovedRight())
        {
            if (!_inventoryAccessed &&
                !_inTunnelLift &&
                !inAccessForceField)
            {
                if (accessingCheops)
                {
                    // Do something with the pyramid.....
                    _screenDirectory.Get(GameScreenType.Cheops).Reset(null);
                    _screenDirectory.Current = GameScreenType.Cheops;
                }
                else
                {
                    _resources.GetSoundFxEngine()
                        .PlaySound("BlobPickUp", 0.3f, false);

                    _inventoryAccessed = true;

                    _inventory.SwapWithFloor(
                        new Rectangle(_blobXPos, BlobYPos, BlobWidth, BlobHeight),
                        _gameScreens.CurrentGameScreenData.PickUps);

                    _gameScreens.UpdateCollisionMap(false);
                }
            }
        }
    }

    /// <summary>
    ///     One of blob's animations: a run of frames played end to end and started over, which is what
    ///     all five of his are.
    /// </summary>
    /// <param name="firstFrame">Also the frame it starts on.</param>
    /// <param name="animateForwards">False to run the frames backwards, which is how the left walk works.</param>
    private static Animation LoopingAnimation(int firstFrame, int lastFrame, int delay, bool animateForwards)
        => new()
        {
            FirstFrame = firstFrame,
            CurrentFrame = firstFrame,
            LastFrame = lastFrame,
            Yoyo = false,
            Delay = delay,
            Repeat = true,
            AnimateForwards = animateForwards
        };

    /// <summary>
    ///     Un-latches the up and down controls so that holding either only acts once, and puts the
    ///     walk cycles back to looping after <see cref="MoveOnShuttle" /> has stopped them.
    /// </summary>
    private void ReleaseHeldControls()
    {
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
    }

    /// <summary>
    ///     Not real physics. The fall speed steps up the longer the fall goes on and resets the moment
    ///     he lands, which is all the game needs of it.
    /// </summary>
    private void ApplyGravity()
    {
        if (!_inTunnelLift) // Can't fall if we are in a tunnel lift
        {
            int falling = _gameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
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
            int fallDistance = _gameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                BlobWidth, BlobHeight, _blobFallSpeed, true);

            if (fallDistance > 0 && _blobTotalDistanceFallen > FallSoundDistance)
            {
                _blobFallsSoundId = _resources.GetSoundFxEngine()
                    .PlaySound("BlobFalls", true);
            }

            if (falling == 0)
            {
                _resources.GetSoundFxEngine()
                    .StopSound(_blobFallsSoundId, SoundFade.Fast);
            }

            // Check to see if Blob can potentially enter a teleport booth, tunnel etc.
            fallDistance = EdgeShuffleTowardOpening(fallDistance);

            _blobYPos = _blobYPos + fallDistance;
            _blobFallHeight = _blobFallHeight + fallDistance;
        }
    }

    /// <summary>
    ///     Cuts the fall short at the lip of an opening he is being walked into, so that holding a
    ///     direction while falling past a teleport booth or tunnel mouth lands him in it rather than
    ///     dropping him past it.
    /// </summary>
    private int EdgeShuffleTowardOpening(int fallDistance)
    {
        if (BlobsControl.MovedRight())
        {
            for (int a = 0; a < fallDistance; a++)
            {
                if (_gameScreens.CheckCollision(Direction.Down, _blobXPos + BlobWidth,
                        _blobYPos + a, BlobWidth + 1, BlobHeight, 1, true) == 0)
                {
                    if (_gameScreens.CheckCollision(Direction.Right, _blobXPos + BlobWidth,
                            _blobYPos + a, BlobWidth, BlobHeight, 1, true) > 0)
                    {
                        fallDistance = a;

                        if (fallDistance == 0)
                        {
                            fallDistance = 1;
                        }

                        break;
                    }
                }
            }
        }

        if (BlobsControl.MovedLeft())
        {
            for (int a = 0; a < fallDistance; a++)
            {
                if (_gameScreens.CheckCollision(Direction.Down, _blobXPos - BlobWidth,
                        _blobYPos + a, BlobWidth + 1, BlobHeight, 1, true) == 0)
                {
                    if (_gameScreens.CheckCollision(Direction.Right, _blobXPos - BlobWidth,
                            _blobYPos + a, BlobWidth, BlobHeight, 1, true) > 0)
                    {
                        fallDistance = a;

                        if (fallDistance == 0)
                        {
                            fallDistance = 1;
                        }

                        break;
                    }
                }
            }
        }

        return fallDistance;
    }

    private void StartFiring()
    {
        if (BlobsControl.HasFired())
        {
            if (_stats.FirePower > 0)
            {
                if (_blobCanFire)
                {
                    // Play sound
                    _resources.GetSoundFxEngine().PlaySound("BlobLazer");

                    _blobFire.Launch(_blobXPos + FireOffset, _blobYPos + FireOffset);
                    _blobFireFade = 0;

                    _blobCanFire = false;
                    BlobHasFired = true;

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
    }

    private void UpdateFire()
    {
        if (!BlobHasFired)
        {
            return;
        }

        _blobFireTrail?.Start();

        if (!_blobFire.Advance(_directionBlobFired, _blobOnShuttleCached, PointBlankReach()))
        {
            BlobHasFired = false;
            _blobCanFire = true;
        }

        _blobAnimFire1.GetNextFrame();
        _blobAnimFire2.GetNextFrame();
    }

    /// <summary>
    ///     How far a shot has to travel before the thing the blob is standing on is allowed to stop
    ///     it, or null if he is standing on nothing. Without this a shot fired while stood over a
    ///     collectable dies on the spot.
    /// </summary>
    private int? PointBlankReach()
        => CheckForBlobCollisionWithObject(new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out int offset)
            ? offset + BlobWidth / 2
            : null;

    private void StopFireEffects()
    {
        if (!BlobHasFired)
        {
            if (_blobFireTrail != null)
            {
                _blobFireTrail.Stop();
            }

            _blobOnShuttleCached = _blobOnShuttle;
        }
    }

    /// <summary>
    ///     Moves blob onto the next screen when he walks off an edge of this one, and says whether it
    ///     happened. Each edge is different enough to be its own method: the two vertical ones have to
    ///     decide where he lands.
    /// </summary>
    private bool ChangeScreenIfOffEdge()
    {
        bool changedScreens = ChangeScreenOffLeftEdge();

        changedScreens |= ChangeScreenOffRightEdge();
        changedScreens |= ChangeScreenOffTopEdge();
        changedScreens |= ChangeScreenOffBottomEdge();

        return changedScreens;
    }

    private bool ChangeScreenOffLeftEdge()
    {
        if (_blobXPos >= 0 || _gameScreens.ScreenNumber - 1 < 0)
        {
            return false;
        }

        _gameScreens.ScreenNumber--;
        _blobXPos = _gameScreens.ScreenWidth - _resources
            .GetSpriteCollection(_graphicsSet.Names.BlobSprites).SpriteWidth;

        RaiseScreenChangedToMainGame();

        return true;
    }

    private bool ChangeScreenOffRightEdge()
    {
        int blobRightEdge = _blobXPos + _resources
            .GetSpriteCollection(_graphicsSet.Names.BlobSprites).SpriteWidth;

        if (blobRightEdge <= _gameScreens.ScreenWidth ||
            _gameScreens.ScreenNumber + 1 > WorldMap.LastScreenNumber)
        {
            return false;
        }

        _gameScreens.ScreenNumber++;
        _blobXPos = 0;

        RaiseScreenChangedToMainGame();

        return true;
    }

    /// <summary>
    ///     Going up, blob has to be put down on something. On a shuttle he lands at the foot of the new
    ///     screen; on foot he is given a fresh jump platform to stand on, unless a tunnel lift is
    ///     carrying him, in which case it keeps carrying him.
    /// </summary>
    private bool ChangeScreenOffTopEdge()
    {
        if (_blobYPos >= 0 || _gameScreens.ScreenNumber - WorldMap.ScreensAcross < 0)
        {
            return false;
        }

        _gameScreens.ScreenNumber -= WorldMap.ScreensAcross;

        RaiseScreenChangedToMainGame();

        if (_blobOnShuttle || _inTunnelLift)
        {
            _blobYPos = _gameScreens.ScreenHeight - BlobHeight;
        }
        else
        {
            _blobYPos = _blobJumpPlatformEngine.GetNewJumpPlatform(
                _blobXPos,
                _gameScreens.ScreenHeight - _resources
                    .GetSpriteCollection(_graphicsSet.Names.BlobSprites).SpriteHeight,
                0,
                0,
                false);
        }

        return true;
    }

    private bool ChangeScreenOffBottomEdge()
    {
        if (_blobYPos + BlobHeight <= _gameScreens.ScreenHeight ||
            _gameScreens.ScreenNumber + WorldMap.ScreensAcross > WorldMap.LastScreenNumber)
        {
            return false;
        }

        _gameScreens.ScreenNumber += WorldMap.ScreensAcross;
        _blobYPos = 0;

        // He is falling onto the new screen, so the fall starts again from the top of it
        _blobDistanceFallen = 0;
        _blobFallSpeed = BlobInitialFallSpeed;

        RaiseScreenChangedToMainGame();

        return true;
    }

    private void RaiseScreenChangedToMainGame()
    {
        if (OnScreenChanged != null)
        {
            OnScreenChanged(GameScreenType.MainGame);
        }
    }

    private void DetectSurroundingTiles()
    {
        _tileAboveBlob = _gameScreens.CheckTileByObject(Direction.Up, _blobXPos, _blobYPos,
            BlobWidth,
            BlobHeight, BlobVSpeed);

        if (_blobOnShuttle)
        {
            _tileBelowBlob = _gameScreens.CheckTileByObject(Direction.Down, _blobXPos, _blobYPos,
                BlobWidth, BlobHeight - ShuttleHeight, BlobVSpeed); // Subtract the shuttle height off
        }
        else
        {
            _tileBelowBlob = _gameScreens.CheckTileByObject(Direction.Down, _blobXPos, _blobYPos,
                BlobWidth, BlobHeight, BlobVSpeed);
        }

        _tileLeftBlob = _gameScreens.CheckTileByObject(Direction.Left, _blobXPos, _blobYPos,
            BlobWidth,
            BlobHeight, BlobHSpeed);
        _tileRightBlob = _gameScreens.CheckTileByObject(Direction.Right, _blobXPos, _blobYPos,
            BlobWidth, BlobHeight, BlobHSpeed);
    }

    private void DetectShuttlePad()
    {
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
    }

    private void DetectTeleporter()
    {
        if (_tileRightBlob != null && BlobsControl.MovedRight())
        {
            if (_tileRightBlob.TileNumber == (int)ActionTiles.TeleportBooth)
            {
                if (_tileRightBlob.XPosition >= 24 &&
                    _tileRightBlob.YPosition == 32)
                {
                    _screenDirectory.Current = GameScreenType.Teleporter;

                    _screenDirectory.Get(GameScreenType.Teleporter).Reset(null);
                    _blobXPos = _tileRightBlob.MapXPosition * TileMetrics.TileWidth + TeleportBoothXOffset;
                    BlobFrame = BlobWalkRightFirstFrame;
                }
            }
        }
    }

    /// <summary>
    ///     Each side detects the booth on one side of blob and then reports the map position of the
    ///     tile on his other side, which is why both tiles have to be there for either to trigger.
    /// </summary>
    private void DetectAccessBooth()
    {
        if (_tileLeftBlob is { TileNumber: (int)ActionTiles.AccessBoothRight } &&
            _tileRightBlob != null)
        {
            if (_tileLeftBlob.XPosition <= 46 &&
                _tileLeftBlob.YPosition == 32)
            {
                _screenDirectory.Get(GameScreenType.AccessBooth).Reset(new object[]
                {
                    Direction.Left,
                    _tileRightBlob.MapXPosition
                });
                _screenDirectory.Current = GameScreenType.AccessBooth;
            }
        }

        if (_tileRightBlob is { TileNumber: (int)ActionTiles.AccessBoothLeft } &&
            _tileLeftBlob != null)
        {
            if (_tileRightBlob.XPosition >= 24 &&
                _tileRightBlob.YPosition == 32)
            {
                _screenDirectory.Get(GameScreenType.AccessBooth).Reset(new object[]
                {
                    Direction.Right,
                    _tileLeftBlob.MapXPosition
                });
                _screenDirectory.Current = GameScreenType.AccessBooth;
            }
        }
    }

    private bool DetectLeftHandTunnel()
    {
        bool changedScreens = false;

        if (_tileLeftBlob?.TileNumber is
            TunnelTiles.LeftMouth1 or
            TunnelTiles.LeftMouth2 or
            TunnelTiles.LeftMouth3 or
            TunnelTiles.LeftMouth4 or
            TunnelTiles.LeftMouth5 or
            TunnelTiles.LeftMouth6 or
            TunnelTiles.LeftMouth7 or
            TunnelTiles.LeftMouth8 or
            TunnelTiles.LeftMouth9 or
            TunnelTiles.LeftMouth10)
        {
            if (_tileLeftBlob.XPosition <= 32 && _tileLeftBlob.YPosition == 32)
            {
                _gameScreens.ScreenNumber--;
                _directionGoingThroughTunnel = Direction.Left;
                changedScreens = true;

                if (OnScreenChanged != null)
                {
                    _screenEffects.ExecuteScreenEffect(ScreenEffectType.LeftTunnelEntry, TunnelEntrySpeed,
                        -1, true);
                    _resources.GetSoundFxEngine()
                        .PlaySound("TunnelSwish1", 0.8f, false);
                    OnScreenChanged(GameScreenType.None);
                }
            }
        }

        return changedScreens;
    }

    private bool DetectRightHandTunnel()
    {
        bool changedScreens = false;

        if (_tileRightBlob?.TileNumber is
            TunnelTiles.RightMouth1 or
            TunnelTiles.RightMouth2 or
            TunnelTiles.RightMouth3 or
            TunnelTiles.RightMouth4 or
            TunnelTiles.RightMouth5 or
            TunnelTiles.RightMouth6 or
            TunnelTiles.RightMouth7 or
            TunnelTiles.RightMouth8 or
            TunnelTiles.RightMouth9 or
            TunnelTiles.RightMouth10)
        {
            if (_tileRightBlob.XPosition >= 32 && _tileRightBlob.YPosition == 32)
            {
                _gameScreens.ScreenNumber++;
                _directionGoingThroughTunnel = Direction.Right;
                changedScreens = true;

                if (OnScreenChanged != null)
                {
                    _screenEffects.ExecuteScreenEffect(ScreenEffectType.RightTunnelEntry,
                        TunnelEntrySpeed, -1, true);
                    _resources.GetSoundFxEngine()
                        .PlaySound("TunnelSwish2", 0.8f, false);
                    OnScreenChanged(GameScreenType.None);
                }
            }
        }

        return changedScreens;
    }

    private void BreakBridgeUnderfoot()
    {
        if (_tileBelowBlob is { TileNumber: (int)ActionTiles.BreakableBridges })
        {
            if (_blobFallHeight >= BridgeBreakingFallHeight)
            {
                // Play the sound
                _resources.GetSoundFxEngine().PlaySound("BridgeBreak");

                // Add the tile to the list of tiles to reset on screen change
                _tilesToReset.Add(new ResetTile(_tileBelowBlob, _gameScreens.ScreenNumber));

                // Remove the tile
                _gameScreens.CurrentGameScreenData
                    .ScreenTiles[_tileBelowBlob.MapXPosition, _tileBelowBlob.MapYPosition].TileImage = -1;

                // Add the tile to the Breakable bridges engine
                _breakableBridges.AddBridge(
                    _gameScreens.ConvertToScreenCoords(_tileBelowBlob.MapXPosition,
                        _tileBelowBlob.MapYPosition));
            }
        }
    }

    private void RideTunnelLift()
    {
        if (_tileAboveBlob != null)
        {
            switch (_tileAboveBlob.TileNumber)
            {
                case (int)ActionTiles.TunnelLiftShaft1 or
                    (int)ActionTiles.TunnelLiftShaft2 or
                    (int)ActionTiles.TunnelLiftTile or
                    (int)ActionTiles.TunnelLiftShaft3:
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
                case (int)ActionTiles.TunnelLiftTop1 or
                    (int)ActionTiles.TunnelLiftTop2 or
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
            bool moveBlob = true;
            if (_tileAboveBlob?.TileNumber is (int)ActionTiles.TunnelLiftShaft1 or
                (int)ActionTiles.TunnelLiftShaft2 or
                (int)ActionTiles.TunnelLiftShaft3)
            {
                if (_tileAboveBlob.YPosition == 96)
                {
                    if (BlobsControl.MovedLeft() && _gameScreens.CheckCollision(Direction.Left,
                            _blobXPos, _blobYPos, BlobWidth, BlobHeight, BlobHSpeed, true) > 0)
                    {
                        if (_distanceInTunnelLift > TunnelLiftMinimumRide)
                        {
                            _inTunnelLift = false;
                            moveBlob = false;
                        }
                    }
                    else if (BlobsControl.MovedRight() &&
                             _gameScreens.CheckCollision(Direction.Right, _blobXPos, _blobYPos,
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
                _blobTunnelLiftSoundId = _resources.GetSoundFxEngine()
                    .PlaySound("TunnelLift", true);
                _blobYPos -= TunnelLiftSpeed;
                _distanceInTunnelLift += TunnelLiftSpeed;
            }
            else
            {
                _resources.GetSoundFxEngine()
                    .StopSound(_blobTunnelLiftSoundId, SoundFade.None);
            }
        }
    }

    private void CheckSpikes()
    {
        TileDetected? tileIntersectingBlob =
            _gameScreens.CheckTileByObject(Direction.None, _blobXPos, _blobYPos, BlobWidth,
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
    }

    private void CollectPowerUp()
    {
        if (_gameScreens.CurrentGameScreenData.PowerUp != null)
        {
            if (CollisionDetection.Check(
                    _blobXPos,
                    BlobYPos,
                    BlobWidth,
                    BlobHeight,
                    _gameScreens.CurrentGameScreenData.PowerUp.XLocation,
                    _gameScreens.CurrentGameScreenData.PowerUp.YLocation,
                    TileMetrics.CollisionCellWidth,
                    TileMetrics.CollisionCellHeight))
            {
                if (OnBlobActionRaised != null)
                {
                    OnBlobActionRaised(BlobAction.GotPowerUp,
                        _gameScreens.CurrentGameScreenData.PowerUp.Type);
                }

                _gameScreens.CurrentGameScreenData.PowerUp = null;
                _gameScreens.UpdateCollisionMap(false);
            }
        }
    }

    /// <summary>
    ///     The alarm sounds for <see cref="OverloadAlarmFrames" /> frames, then the swell and shrink
    ///     sequence described on <see cref="OverloadSwelling" /> runs a step per frame until he blows.
    /// </summary>
    private void UpdateOverloadingState()
    {
        if (_blobAlarmSoundCounter == 1)
        {
            _resources.GetSoundFxEngine().PlaySound("BlobOverloadAlarm", 1.0f, false);
        }

        if (_blobOverloadState < OverloadExploded && _blobAlarmSoundCounter < OverloadAlarmFrames)
        {
            _blobAlarmSoundCounter++;
        }
        else
        {
            if (!_blobKiller.Triggered)
            {
                BlobHasFired = false;
                _blobCanFire = true;
                _blobFireTrail.Stop();

                _blobJumpPlatformEngine.ResetPlatforms();
                OnBlobDiedAndRespawned?.Invoke();

                if (_blobOverloadState == OverloadSwelling)
                {
                    _blobScale = _blobScale + OverloadScaleStep;

                    if (_blobScale > OverloadMaxScale)
                    {
                        _blobOverloadState = OverloadShrinking;
                        _blobScale = OverloadMaxScale;
                    }
                }
                else if (_blobOverloadState == OverloadShrinking)
                {
                    _blobScale = _blobScale - OverloadScaleStep;

                    if (_blobScale < OverloadMinScale)
                    {
                        _blobScale = OverloadMinScale;
                        _blobOverloadState = OverloadExploded;
                    }
                }

                if (_blobOverloadState == OverloadExploded)
                {
                    _resources.GetSoundFxEngine().PlaySound("BlobExplodes", false);
                    _blobKiller.Trigger(BlobXPos, BlobYPos, true);
                    BlobsState = BlobState.Dying;
                }
            }
        }
    }

    private void UpdateDyingState()
    {
        if (!_blobKiller.Triggered)
        {
            _blobOverloadState = OverloadSwelling;
            _blobScale = 1.0f;
            // If Blob is killed by a spiky tile, we reset his position to when he entered
            if (_resetBlobToScreenEntryPoint)
            {
                _blobYPos = _blobScreenEntryLocation.Y;
                _blobXPos = _blobScreenEntryLocation.X;
                _blobOnShuttle = _blobOnShuttleOnEntry;
                _gameScreens.ChangeShuttlePlatformState(_blobOnShuttle);
                _gameScreens.UpdateCollisionMap(false);
                BlobFrame = _blobFrameOnEntry;
                _inTunnelLift = false;
                _blobFallSpeed = BlobInitialFallSpeed;

                if (_blobOnShuttle)
                {
                    BlobHeight = BlobHeightOnShuttle;
                }
                else
                {
                    BlobHeight = SpriteMetrics.SpriteHeight;
                }
            }

            _resetBlobToScreenEntryPoint = false;
            BlobsState = BlobState.Dead;
            _blobKiller.Reset();
        }
    }

    /// <summary>
    ///     Pushes blob out of any scenery he has ended up inside, which happens when a tile appears
    ///     under him - a shuttle platform being taken, a screen changing beneath his feet.
    /// </summary>
    /// <remarks>
    ///     Each edge is tried in turn, and only if he is caught on that edge and free at the opposite
    ///     one: caught on both means there is nowhere to push him and he is left where he is. The four
    ///     edges were four copies of this, differing only in which thin strip is probed and which way
    ///     he is then walked.
    /// </remarks>
    public void UnstickBlobFromTiles()
    {
        Point freed = _unstickProbe.FreePositionFor(
            new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight));

        _blobXPos = freed.X;
        _blobYPos = freed.Y;
    }

    public void PerformPostChangeScreenActions()
    {
        // Reset blob's fire
        BlobHasFired = false;
        _blobCanFire = true;

        _blobFire.Launch(_blobXPos + FireOffset, _blobYPos + FireOffset);
        _blobFireFade = 0;

        _blobJumpPlatformEngine.ResetPlatforms();

        // Unstick Blob from the screenmap if he's got stuck due to a tile map change
        UnstickBlobFromTiles();

        if (_directionGoingThroughTunnel != Direction.None)
        {
            if (_directionGoingThroughTunnel == Direction.Left)
            {
                for (int a = TunnelTiles.RightMouth1; a <= TunnelTiles.LastMouth; a += TunnelTiles.MouthStride)
                {
                    List<Point> tiles = _gameScreens.GetCoordsOfTiles(a);

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
                for (int a = TunnelTiles.FirstMouth; a <= TunnelTiles.LastMouth + 1; a += TunnelTiles.MouthStride)
                {
                    List<Point> tiles = _gameScreens.GetCoordsOfTiles(a);

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

        foreach (ResetTile t in _tilesToReset)
        {
            ScreenTile[,]? screenTiles = _gameScreens.GetGameScreen(t.ScreenNumber).ScreenTiles;
            if (screenTiles != null)
            {
                screenTiles[
                    t.TileToReset.MapXPosition,
                    t.TileToReset.MapYPosition].TileImage = t.TileToReset.TileNumber;
            }
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
                DrawNormalState(screenFade);
                break;
            }
            case BlobState.Overloading when _blobAlarmSoundCounter < OverloadAlarmFrames:
            {
                DrawOverloadAlarmState(screenFade);
                break;
            }
            case BlobState.Overloading:
            {
                DrawOverloadFlare();
                break;
            }
        }
    }

    private void DrawNormalState(int screenFade)
    {
        _blobFireTrail?.Draw();

        FadeEnergyDrainTint();
        DrawBlobSprite(screenFade);
        DrawShuttle(screenFade);

        _blobJumpPlatformEngine.Draw();

        DrawFire(screenFade);
        DrawTeleportBeam();
        FadeInAfterTeleport();
    }

    /// <summary>
    ///     The red wash that shows energy being drained. It runs down while a nasty is touching him and
    ///     back up once it stops, so a glancing touch shows as a flash rather than a step change.
    /// </summary>
    private void FadeEnergyDrainTint()
    {
        if (BlobTouchedByNasty)
        {
            // Reset the counter
            if (!_blobTouched)
            {
                _blobEnergyDrainCounter = ColourLevels.Full;
                _blobTouched = true;
            }

            if (_blobEnergyDrainCounter > 0)
            {
                _blobEnergyDrainCounter -= EnergyDrainFadeStep;
            }

            if (_blobEnergyDrainCounter < 0)
            {
                _blobEnergyDrainCounter = 0;
            }
        }
        else
        {
            if (_blobEnergyDrainCounter < ColourLevels.Full)
            {
                _blobEnergyDrainCounter += EnergyDrainFadeStep;
            }

            if (_blobEnergyDrainCounter > ColourLevels.Full)
            {
                _blobEnergyDrainCounter = ColourLevels.Full;
            }

            _blobTouched = false;
        }
    }

    private void DrawBlobSprite(int screenFade)
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);
        blobSprites.Begin(BlendState.AlphaBlend);
        blobSprites.DrawSprite(BlobFrame,
            _blobXPos, _blobYPos,
            ColourHelper.GetXna4Colour(ColourLevels.Full, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                (byte)(_teleportFadeLevel * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
        blobSprites.End();
    }

    private void DrawShuttle(int screenFade)
    {
        if (_blobOnShuttle)
        {
            if (_graphicsSet.IsOriginal)
            {
                SpriteCollection blobShuttleOriginal = _resources.GetSpriteCollection("BlobShuttleOriginal");
                blobShuttleOriginal.Begin(BlendState.AlphaBlend);
                blobShuttleOriginal.DrawSprite(
                    _blobShuttleAnim.CurrentFrame, _blobXPos, _blobYPos + SpriteMetrics.SpriteHeight,
                    ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
                blobShuttleOriginal.End();
            }
            else
            {
                SpriteCollection blobShuttle = _resources.GetSpriteCollection("BlobShuttle");
                blobShuttle.Begin(BlendState.AlphaBlend);
                blobShuttle.DrawSprite(0, _blobXPos,
                    _blobYPos + SpriteMetrics.SpriteHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
                blobShuttle.End();
            }
        }
    }

    private void DrawFire(int screenFade)
    {
        if (BlobHasFired)
        {
            if (_blobFireFade < ColourLevels.Full)
            {
                _blobFireFade = _blobFireFade + FireFadeStep;
            }

            if (_blobFireFade > ColourLevels.Full)
            {
                _blobFireFade = ColourLevels.Full;
            }

            if (_graphicsSet.IsOriginal)
            {
                if (_blobOnShuttle)
                {
                    SpriteCollection blobFire2Original = _resources.GetSpriteCollection("BlobFire2Original");
                    blobFire2Original.Begin(BlendState.Additive);
                    blobFire2Original.DrawSprite(
                        _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                            (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), 1.0f, 0f, true,
                        _session.Paused);
                    blobFire2Original.End();
                }
                else
                {
                    SpriteCollection blobFire1 = _resources.GetSpriteCollection("BlobFire1");
                    blobFire1.Begin(BlendState.Additive);
                    blobFire1.DrawSprite(
                        _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY - FireOffset,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                            (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), 1.0f, 0f, true,
                        _session.Paused);
                    blobFire1.End();
                }
            }
            else
            {
                if (_blobOnShuttle)
                {
                    SpriteCollection blobFire2 = _resources.GetSpriteCollection("BlobFire2");
                    blobFire2.Begin(BlendState.Additive);
                    blobFire2.DrawSprite(
                        _blobAnimFire2.CurrentFrame, BlobFireX, BlobFireY,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                            (byte)(_blobFireFade * ((double)screenFade / ColourLevels.Full))), 1.0f, 10f,
                        true,
                        _session.Paused);
                    blobFire2.End();
                }
            }
        }
    }

    private void DrawTeleportBeam()
    {
        if (_startBlobTeleporting)
        {
            if (_blobTeleporterBeam != null)
            {
                _blobTeleporterBeam.Draw();
            }
        }
    }

    private void FadeInAfterTeleport()
    {
        if (_teleportFadeLevel < ColourLevels.Full)
        {
            _teleportFadeLevel += TeleportFadeStep;
        }

        if (_teleportFadeLevel > ColourLevels.Full)
        {
            _teleportFadeLevel = ColourLevels.Full;
        }
    }

    private void DrawOverloadAlarmState(int screenFade)
    {
        // Make blob flicker
        if (_blobAlarmSoundCounter % 2 == 0)
        {
            SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);
            blobSprites.Begin(BlendState.AlphaBlend);
            blobSprites.DrawSprite(
                BlobFrame, _blobXPos, _blobYPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                    (byte)(_teleportFadeLevel * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
            blobSprites.End();
        }

        if (_blobOnShuttle)
        {
            SpriteCollection blobShuttle = _resources.GetSpriteCollection("BlobShuttle");
            blobShuttle.Begin(BlendState.AlphaBlend);
            blobShuttle.DrawSprite(0, _blobXPos,
                _blobYPos + SpriteMetrics.SpriteHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
            blobShuttle.End();
        }
    }

    private void DrawOverloadFlare()
    {
        _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites)
            .Begin(BlendState.Additive);

        float temp = _blobScale - OverloadFlareFirstScale;
        for (int a = 0; a < OverloadFlareCopies; a++)
        {
            if (temp >= 0f)
            {
                _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites)
                    .DrawSprite(BlobFrame, _blobXPos, _blobYPos,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, OverloadFlareAlpha), temp, 0f);
            }

            temp += OverloadFlareScaleStep;
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites).End();
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
        _blobXPos = BlobStartXPos;
        _blobYPos = BlobStartYPos;

        // Remove Blob from shuttle pads
        _blobOnShuttle = false;
        BlobHeight = SpriteMetrics.SpriteHeight;
        _gameScreens.ChangeShuttlePlatformState(false);

        // Clear up any rogue collision tiles etc.
        PerformPostChangeScreenActions();
    }

    /// <summary>
    ///     Gets collision objects on the current screen which aren't tiles i.e Cheops Pyramids, CoreElements
    /// </summary>
    private List<InteractiveItem> GetCollisionObjects()
    {
        var temp = new List<InteractiveItem>();
        if (_gameScreens.CurrentGameScreenData.CheopsPyramid != null)
        {
            temp.Add(_gameScreens.CurrentGameScreenData.CheopsPyramid);
        }

        foreach (InteractiveItem t in _gameScreens.CurrentGameScreenData.PickUps)
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

        foreach (InteractiveItem t in GetCollisionObjects())
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

    /// <summary>
    ///     Joins a <see cref="BlobFire" /> to the game it is flying through. Everything the shot
    ///     asks about is answered from here, so the shot itself knows about nothing but its own
    ///     position - which is what lets its rules be tested without a screen or a graphics device.
    /// </summary>
    private sealed class FireSurroundings(GameScreenCollection gameScreens, Random randomNumberGenerator, Blob blob)
        : IFireSurroundings
    {
        public int ScreenWidth => gameScreens.ScreenWidth;

        public int ScreenHeight => gameScreens.ScreenHeight;

        /// <inheritdoc />
        /// <remarks>
        ///     The shot is not the blob, so it gets none of the leniency the collision map gives him.
        /// </remarks>
        public int DistanceClear(Direction direction, int x, int y, int width, int height, int distance)
            => gameScreens.CheckCollision(direction, x, y, width, height, distance, false);

        /// <inheritdoc />
        public FireDeflection ChooseDeflection()
            => (FireDeflection)randomNumberGenerator.Next(
                (int)FireDeflection.Left,
                (int)FireDeflection.Right + 1);

        /// <inheritdoc />
        public void ThrowSparks(int x, int y) => blob._blobFireSparks?.Start(x, y);
    }
}
