// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

internal class Blob
{
    /// <summary>
    ///     How fast the tunnel transition plays, in pixels of play area a frame. It sets the pace of
    ///     the whole passage rather than a literal step, since
    ///     <see cref="TunnelEntryEffect" /> eases the motion - the squeeze takes as long as it would
    ///     have at this many pixels a frame, and the dark and the arrival follow from that.
    /// </summary>
    private const int TunnelEntrySpeed = 64;

    /// <summary>
    ///     The frame Blob stands still on - the one he is drawn in on a fresh screen and after a
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
    ///     Where a walk cycle is picked up when Blob turns round mid-stride, so the new direction
    ///     starts from a matching pose instead of snapping back to the first frame.
    /// </summary>
    private const int BlobTurnToLeftFrame = 6;

    /// <inheritdoc cref="BlobTurnToLeftFrame" />
    private const int BlobTurnToRightFrame = 4;

    /// <summary>
    ///     How many logic frames each frame of a walk cycle is held for.
    /// </summary>
    private const int AnimationDelay = 2;

    /// <summary>
    ///     How far Blob has to land from to break a breakable bridge. Anything shorter and the
    ///     bridge holds.
    /// </summary>
    private const int BridgeBreakingFallHeight = 128;

    /// <summary>
    ///     How high Blob hops when he has no platforms left to lay. One collision cell, which is
    ///     just enough to clear a single step.
    /// </summary>
    private const int JumpHeightWithoutPlatforms = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     The alpha each copy of Blob is drawn at during the explosion flare. The sizes and how many
    ///     there are belong to <see cref="BlobOverload" />; this is a drawing colour, so it stays here.
    /// </summary>
    private const int OverloadFlareAlpha = 150;

    /// <summary>
    ///     How fast Blob reddens when a nasty is touching him and recovers when one is not, and
    ///     how fast it fades back in after a teleport. Both are counted in colour channel levels a
    ///     frame.
    /// </summary>
    private const int EnergyDrainFadeStep = 16;

    /// <inheritdoc cref="EnergyDrainFadeStep" />
    private const int TeleportFadeStep = 8;

    /// <summary>
    ///     How fast Blob walks, in pixels a logic frame, horizontally and vertically.
    /// </summary>
    private const int BlobWalkSpeed = 8;

    /// <summary>
    ///     How far inside a spiked tile Blob has to be before the spikes get him. The three spike
    ///     tiles are drawn differently - one standing in the middle of its tile, one mounted on the
    ///     right hand wall, one a field of broken glass on the floor - so each is given the reach its
    ///     artwork suggests rather than the whole tile. The numbers are Blob's position relative
    ///     to the tile, so a negative one means the tile starts before Blob does.
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
    ///     Where the teleport beam and the shot's smoke trail are emitted from, measured in from the
    ///     top left of Blob and of the shot respectively.
    /// </summary>
    private const int TeleportBeamXOffset = 30;

    /// <inheritdoc cref="TeleportBeamXOffset" />
    private const int TeleportBeamYOffset = 50;

    /// <summary>
    ///     How far into a booth's tile Blob has to have walked for it to count as having stepped in.
    ///     A little past <see cref="BoothMetrics.BlobXOffset" />, so Blob settles back onto the
    ///     doorway as the booth takes over rather than having to be walked into exactly.
    /// </summary>
    private const int TeleportBoothEntryReach = 24;

    /// <summary>
    ///     Where Blob is put down after coming through a tunnel - just far enough inside the
    ///     mouth on the far side that it is not standing in the tunnel it just left.
    /// </summary>
    private const int TunnelExitLeftXOffset = 24;

    /// <inheritdoc cref="TunnelExitLeftXOffset" />
    private const int TunnelExitRightXOffset = 40;

    /// <inheritdoc cref="TunnelExitLeftXOffset" />
    private const int TunnelExitYOffset = 32;

    private readonly Animation _blobAnimLeft;
    private readonly Animation _blobAnimRight;


    private readonly JumpPlatforms _blobJumpPlatformEngine;
    private readonly BlobDeathAnimation _blobKiller;

    private readonly IStandardParticleEffect _blobTeleporterBeam;
    private readonly BreakableBridges _breakableBridges;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly ScreenEffectEngine _screenEffects;
    private readonly PlayerStats _stats;
    private readonly List<ResetTile> _tilesToReset = [];


    private int _blobEnergyDrainCounter;

    private Guid _blobFallsSoundId;


    private Direction _blobsLastAnimDirection = Direction.None;
    private bool _blobTouched;

    private Guid _blobTunnelLiftSoundId = Guid.Empty;

    private int _blobXPos;

    private int _blobYPos;

    private Direction _directionGoingThroughTunnel = Direction.None;
    private readonly BlobFall _fall = new();
    private readonly BlobEntryPoint _entryPoint = new();
    private readonly BlobOverload _overload = new();
    private readonly Shuttle _shuttle = new();
    private readonly TunnelLift _lift;
    private readonly Inventory _inventory;
    private readonly UnstickProbe _unstickProbe;

    private bool _inventoryAccessed;

    private bool _jumpPressed;
    private bool _resetBlobToScreenEntryPoint;
    private bool _startBlobTeleporting;

    private int _teleportFadeLevel;

    /// <summary>
    ///     The last reading of what is around him. A field rather than a local because the shuttle pad
    ///     is acted on at the start of the frame after the one that detected it - see
    ///     <see cref="UpdateNormalState" />.
    /// </summary>
    private BlobSurroundings _surroundings;

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
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BeamMeUpSmall,
                ParticleEffectType.Triggered, null);
        _blobTeleporterBeam.ParticleEffectComplete += BlobTeleporterBeam_ParticleEffectComplete;

        Weapon = new BlobWeapon(resources, gameScreens, graphicsSet, stats, randomNumberGenerator,
            () => OnBlobActionRaised?.Invoke(BlobAction.FiredLazer, null));

        _lift = new TunnelLift(
            () => _blobTunnelLiftSoundId = _resources.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.TunnelLift, true),
            () => _resources.GetSoundFxEngine()
                .StopSound(_blobTunnelLiftSoundId, SoundFade.None));

        // The shot starts where Blob is, which at this point is the origin - he is put onto a screen
        // afterwards, and that goes through PerformPostChangeScreenActions.
        Weapon.ResetTo(_blobXPos, _blobYPos);

        // Both of these loop forward with no yoyo except the left walk, which runs backwards through the
        // right walk's frames
        _blobAnimLeft = Animation.Looping(BlobWalkLeftFirstFrame, BlobWalkLeftLastFrame, AnimationDelay, false);
        _blobAnimRight = Animation.Looping(BlobWalkRightFirstFrame, BlobWalkRightLastFrame, AnimationDelay, true);

        _inventory = new Inventory();
        _unstickProbe = new UnstickProbe(gameScreens.Collisions);
    }

    /// <summary>
    ///     What Blob is carrying. The queue itself is <see cref="Inventory" />'s; this is what
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
            _entryPoint.XPos = value;
        }
    }

    public int BlobYPos
    {
        get => _blobYPos;
        set
        {
            _blobYPos = value;
            _entryPoint.YPos = value;
        }
    }

    public BlobState BlobsState { get; set; }

    public bool BlobTouchedByNasty { get; set; }

    public int BlobWidth => SpriteMetrics.SpriteWidth;

    /// <summary>
    ///     How tall he is, shuttle included. Owned by <see cref="Shuttle" /> because the two facts
    ///     are one: riding it is what makes him taller.
    /// </summary>
    public int BlobHeight => _shuttle.BlobHeight;

    public int BlobFrame { get; set; } = BlobRestingFrame;

    private int BlobHSpeed { get; set; } = BlobWalkSpeed;

    private int BlobVSpeed { get; set; } = BlobWalkSpeed;

    /// <summary>
    ///     The lazer. A nasty asks it whether a shot is in flight and where it is, and tells it when it
    ///     has been hit.
    /// </summary>
    public BlobWeapon Weapon { get; }

    public CustomControl BlobsControl { get; }

    public event RaiseScreenChanged? OnScreenChanged;

    public event RaiseBlobAction? OnBlobActionRaised;

    /// <summary>
    ///     Raised as Blob is put back together after a death, so anything holding hazard state
    ///     can clear it. The engine answers it by resetting the electricity nodes and gates.
    ///     <para>
    ///         An event rather than a call, because the electricity engine needs Blob to know
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

        _overload.Begin(overloadFirst);

        BlobsState = BlobState.Overloading;
    }

    // Blob has hit something
    public void TriggerBlobTeleportedAnimation()
    {
        _blobTeleporterBeam.Start();
        _startBlobTeleporting = true;
        _teleportFadeLevel = 0;
    }

    public void Update(GameTime gameTime)
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
        _blobTeleporterBeam.Update(_blobXPos + TeleportBeamXOffset, _blobYPos + TeleportBeamYOffset, gameTime);
        Weapon.UpdateEffects(gameTime);

        if (!changedScreens)
        {
            return;
        }

        _entryPoint.Record(BlobXPos, BlobYPos, BlobFrame, _shuttle.Riding);
    }

    /// <summary>
    ///     One frame of Blob being alive and in control. Returns true if the frame took him onto a
    ///     different screen, which is what tells <see cref="Update" /> to record a new entry point.
    /// </summary>
    private bool UpdateNormalState()
    {
        // Deliberately last frame's reading. The shuttle noticed the pad from the same reading at
        // the end of that frame, so its flag and the tile under it are a matched pair; probing again
        // here would act on a flag that no longer describes the tile it is given.
        HandleShuttlePad(_surroundings.Below);
        WalkLeft();
        WalkRight();

        if (_shuttle.Riding)
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

        Weapon.Step(
            BlobsControl.HasFired(),
            new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight),
            DirectionBlobMoved,
            _shuttle.Riding);

        bool changedScreens = ChangeScreenIfOffEdge();

        _surroundings = DetectSurroundingTiles();
        _shuttle.NoticeThePadUnderfoot(_surroundings.Below);
        DetectTeleporter(_surroundings.Right);
        DetectAccessBooth(_surroundings.Left, _surroundings.Right);
        changedScreens |= DetectLeftHandTunnel(_surroundings.Left);
        changedScreens |= DetectRightHandTunnel(_surroundings.Right);
        BreakBridgeUnderfoot(_surroundings.Below);
        GoUpTheShaft(_surroundings.Above);
        CheckSpikes();
        CollectPowerUp();

        _blobJumpPlatformEngine.Update();

        FadeEnergyDrainTint();
        FadeInAfterTeleport();

        return changedScreens;
    }

    /// <summary>
    ///     Acts on the pad he is standing on. Taking the shuttle or putting it back changes the tile
    ///     map on every screen, so the collision map is rebuilt afterwards - and it is rebuilt when he
    ///     is merely standing on a pad too, which is what the original did.
    /// </summary>
    private void HandleShuttlePad(TileDetected? below)
    {
        switch (_shuttle.Step(below, BlobsControl.MovedUp(), BlobsControl.MovedLeft(), BlobsControl.MovedRight()))
        {
            case ShuttleBoarding.NotOnThePad:
            {
                return;
            }
            case ShuttleBoarding.Boarded:
            {
                _gameScreens.ChangeShuttlePlatformState(true);
                break;
            }
            case ShuttleBoarding.SteppedOff:
            {
                _gameScreens.ChangeShuttlePlatformState(false);
                break;
            }
        }

        _gameScreens.UpdateCollisionMap(false);
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
                _blobXPos -= amountToMove;
                if (_shuttle.Riding)
                {
                    _shuttle.Advance();
                }
            }

            BlobFrame = _blobAnimLeft.CurrentFrame;
            DirectionBlobMoved = Direction.Left;
            _blobsLastAnimDirection = Direction.Left;

            if (!_shuttle.Riding && _fall.Height == 0)
            {
                _resources.GetSoundFxEngine()
                    .PlaySound(Assets.Sounds.BlobRun, true);
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
                _blobXPos += amountToMove;
                if (_shuttle.Riding)
                {
                    _shuttle.Advance();
                }
            }

            BlobFrame = _blobAnimRight.CurrentFrame;
            DirectionBlobMoved = Direction.Right;
            _blobsLastAnimDirection = Direction.Right;

            if (!_shuttle.Riding && _fall.Height == 0)
            {
                _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobRun, true);
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
            _blobYPos -= _gameScreens.CheckCollision(Direction.Up, _blobXPos,
                _blobYPos,
                BlobWidth, BlobHeight, BlobVSpeed, true);
            DirectionBlobMoved = Direction.Up;
        }

        if (BlobsControl.MovedDown())
        {
            _blobYPos += _gameScreens.CheckCollision(Direction.Down, _blobXPos,
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
                    if (!_shuttle.OnThePad)
                    {
                        int newYPos = _blobJumpPlatformEngine.GetNewJumpPlatform(_blobXPos, _blobYPos,
                            BlobWidth,
                            BlobHeight, true);

                        if (newYPos != JumpPlatforms.NoRoomForPlatform)
                        {
                            _blobYPos = newYPos;

                            _resources.GetSoundFxEngine()
                                .PlaySound(Assets.Sounds.BlobJumps, 0.3f, false);

                            if (OnBlobActionRaised != null)
                            {
                                OnBlobActionRaised(BlobAction.LaidPlatform, null);
                            }
                        }
                    }
                }
                else // just jump aimlessly in the air
                {
                    // Check that we are on the ground first.... and above Blob's head
                    if (_gameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                            BlobWidth, BlobHeight, 1, true) == 0 &&
                        _gameScreens.CheckCollision(Direction.Up, _blobXPos,
                            _blobYPos - JumpHeightWithoutPlatforms,
                            BlobWidth, BlobHeight, 1, true) != 0
                       )
                    {
                        _blobYPos -= JumpHeightWithoutPlatforms;
                        _resources.GetSoundFxEngine()
                            .PlaySound(Assets.Sounds.BlobJumpsNoPlatforms, 1f, false);
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
                !_lift.GoingUp &&
                !inAccessForceField)
            {
                if (accessingCheops)
                {
                    // Do something with the pyramid.....
                    _screenDirectory.Cheops.Reset();
                    _screenDirectory.Current = GameScreenType.Cheops;
                }
                else
                {
                    _resources.GetSoundFxEngine()
                        .PlaySound(Assets.Sounds.BlobPickUp, 0.3f, false);

                    _inventoryAccessed = true;

                    _inventory.SwapWithFloor(
                        new Rectangle(_blobXPos, BlobYPos, BlobWidth, BlobHeight),
                        _gameScreens.CurrentGameScreenData.PickUps);

                    _gameScreens.UpdateCollisionMap(false);
                }
            }
        }
    }

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
        if (_lift.GoingUp) // Can't fall if we are in a tunnel lift
        {
            return;
        }

        var fall = _fall.Step(
            _gameScreens.Collisions,
            new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight),
            BlobsControl.MovedLeft(),
            BlobsControl.MovedRight());

        if (fall.Audible)
        {
            _blobFallsSoundId = _resources.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.BlobFalls, true);
        }

        if (fall.OnTheGround)
        {
            _resources.GetSoundFxEngine()
                .StopSound(_blobFallsSoundId, SoundFade.Fast);
        }

        // Fall - arrrgh!!!
        _blobYPos += fall.Distance;
    }

    /// <summary>
    ///     Moves Blob onto the next screen when he walks off an edge of this one, and says whether it
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
    ///     Going up, Blob has to be put down on something. On a shuttle he lands at the foot of the new
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

        if (_shuttle.Riding || _lift.GoingUp)
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
        _fall.StartFallingAgain();

        RaiseScreenChangedToMainGame();

        return true;
    }

    private void RaiseScreenChangedToMainGame()
    {
        OnScreenChanged?.Invoke(GameScreenType.MainGame);
    }

    /// <summary>
    ///     Looks around him. Riding the shuttle makes him taller, and the extra height is the shuttle
    ///     underneath him rather than more of Blob, so the ground is looked for from his own feet.
    /// </summary>
    private BlobSurroundings DetectSurroundingTiles()
        => BlobSurroundings.Around(
            _gameScreens.Collisions,
            _blobXPos,
            _blobYPos,
            BlobWidth,
            BlobHeight,
            // The ground is looked for from his own feet rather than the shuttle's, and his own
            // height is the sprite's whether he is aboard or not - the shuttle is all of the
            // difference between the two.
            SpriteMetrics.SpriteHeight,
            BlobHSpeed,
            BlobVSpeed);

    private void DetectTeleporter(TileDetected? toTheRight)
    {
        if (toTheRight == null || !BlobsControl.MovedRight())
        {
            return;
        }

        if (toTheRight.TileNumber != (int)ActionTiles.TeleportBooth)
        {
            return;
        }

        // Far enough in to be in the doorway, and standing on the booth's floor rather than
        // passing it in mid air. Only the X is set from here: the Y test is the arrival
        // offset, so Blob is already standing exactly where a booth puts him.
        if (toTheRight.XPosition < TeleportBoothEntryReach ||
            toTheRight.YPosition != BoothMetrics.BlobYOffset)
        {
            return;
        }

        _screenDirectory.Current = GameScreenType.Teleporter;
        _screenDirectory.Teleporter.Reset();
        _blobXPos = toTheRight.MapXPosition * TileMetrics.TileWidth + BoothMetrics.BlobXOffset;
        BlobFrame = BlobWalkRightFirstFrame;
    }

    /// <summary>
    ///     Each side detects the booth on one side of Blob and then reports the map position of the
    ///     tile on his other side, which is why both tiles have to be there for either to trigger.
    /// </summary>
    private void DetectAccessBooth(TileDetected? toTheLeft, TileDetected? toTheRight)
    {
        if (toTheLeft is { TileNumber: (int)ActionTiles.AccessBoothRight } && toTheRight != null)
        {
            if (toTheLeft is { XPosition: <= 46, YPosition: 32 })
            {
                _screenDirectory.AccessBooth.Reset(Direction.Left, toTheRight.MapXPosition);
                _screenDirectory.Current = GameScreenType.AccessBooth;
            }
        }

        if (toTheRight is not { TileNumber: (int)ActionTiles.AccessBoothLeft } || toTheLeft == null)
        {
            return;
        }

        if (toTheRight.XPosition < 24 || toTheRight.YPosition != 32)
        {
            return;
        }
        _screenDirectory.AccessBooth.Reset(Direction.Right, toTheLeft.MapXPosition);
        _screenDirectory.Current = GameScreenType.AccessBooth;
    }

    private bool DetectLeftHandTunnel(TileDetected? toTheLeft)
    {
        bool changedScreens = false;

        if (toTheLeft is null ||
            !TunnelTiles.IsLeftMouth(toTheLeft.TileNumber) ||
            toTheLeft.XPosition > 32 ||
            toTheLeft.YPosition != 32)
        {
            return changedScreens;
        }

        _gameScreens.ScreenNumber--;
        _directionGoingThroughTunnel = Direction.Left;
        changedScreens = true;

        if (OnScreenChanged == null)
        {
            return changedScreens;
        }

        _screenEffects.ExecuteScreenEffect(ScreenEffectType.LeftTunnelEntry, TunnelEntrySpeed, -1, true);
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.TunnelSwish1, 0.8f, false);
        OnScreenChanged(GameScreenType.None);

        return changedScreens;
    }

    private bool DetectRightHandTunnel(TileDetected? toTheRight)
    {
        bool changedScreens = false;

        if (toTheRight is null ||
            !TunnelTiles.IsRightMouth(toTheRight.TileNumber) ||
            toTheRight.XPosition < 32 ||
            toTheRight.YPosition != 32)
        {
            return changedScreens;
        }

        _gameScreens.ScreenNumber++;
        _directionGoingThroughTunnel = Direction.Right;
        changedScreens = true;

        if (OnScreenChanged == null)
        {
            return changedScreens;
        }

        _screenEffects.ExecuteScreenEffect(ScreenEffectType.RightTunnelEntry, TunnelEntrySpeed, -1, true);
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.TunnelSwish2, 0.8f, false);
        OnScreenChanged(GameScreenType.None);

        return changedScreens;
    }

    private void BreakBridgeUnderfoot(TileDetected? below)
    {
        if (below is not { TileNumber: (int)ActionTiles.BreakableBridges })
        {
            return;
        }

        if (_fall.Height < BridgeBreakingFallHeight)
        {
            return;
        }

        // Play the sound
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BridgeBreak);

        // Add the tile to the list of tiles to reset on screen change
        _tilesToReset.Add(new ResetTile(below, _gameScreens.ScreenNumber));

        // Remove the tile
        _gameScreens.CurrentGameScreenData
            .ScreenTiles[below.MapXPosition, below.MapYPosition].TileImage = -1;

        // Add the tile to the Breakable bridges engine
        _breakableBridges.AddBridge(
            _gameScreens.ConvertToScreenCoords(below.MapXPosition,
                below.MapYPosition));
    }

    private void GoUpTheShaft(TileDetected? above)
        => _blobYPos -= _lift.Step(
            _gameScreens.Collisions,
            new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight),
            above,
            BlobsControl.MovedLeft(),
            BlobsControl.MovedRight(),
            BlobHSpeed);

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
        if (_gameScreens.CurrentGameScreenData.PowerUp == null)
        {
            return;
        }

        if (!CollisionDetection.Check(
                _blobXPos,
                BlobYPos,
                BlobWidth,
                BlobHeight,
                _gameScreens.CurrentGameScreenData.PowerUp.XLocation,
                _gameScreens.CurrentGameScreenData.PowerUp.YLocation,
                TileMetrics.CollisionCellWidth,
                TileMetrics.CollisionCellHeight))
        {
            return;
        }

        OnBlobActionRaised?.Invoke(BlobAction.GotPowerUp, _gameScreens.CurrentGameScreenData.PowerUp.Type);

        _gameScreens.CurrentGameScreenData.PowerUp = null;
        _gameScreens.UpdateCollisionMap(false);
    }

    private void UpdateOverloadingState()
    {
        if (_overload.AlarmJustStarted)
        {
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobOverloadAlarm, 1.0f, false);
        }

        if (_overload.AlarmSounding)
        {
            _overload.CountAnAlarmFrame();
        }
        else
        {
            if (_blobKiller.Triggered)
            {
                return;
            }
            Weapon.Disarm();

            _blobJumpPlatformEngine.ResetPlatforms();
            OnBlobDiedAndRespawned?.Invoke();

            if (_overload.Swell() != OverloadStage.Exploded)
            {
                return;
            }

            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobExplodes, false);
            _blobKiller.Trigger(BlobXPos, BlobYPos, true);
            BlobsState = BlobState.Dying;
        }
    }

    private void UpdateDyingState()
    {
        if (_blobKiller.Triggered)
        {
            return;
        }

        _overload.Reset();

        // If Blob is killed by a spiky tile, we reset his position to when he entered
        if (_resetBlobToScreenEntryPoint)
        {
            _blobYPos = _entryPoint.YPos;
            _blobXPos = _entryPoint.XPos;
            _shuttle.PutBack(_entryPoint.OnShuttle);
            _gameScreens.ChangeShuttlePlatformState(_shuttle.Riding);
            _gameScreens.UpdateCollisionMap(false);
            BlobFrame = _entryPoint.Frame;
            _lift.LetGo();
            _fall.StartFallingAgain();
        }

        _resetBlobToScreenEntryPoint = false;
        BlobsState = BlobState.Dead;
        _blobKiller.Reset();
    }

    /// <summary>
    ///     Pushes Blob out of any scenery he has ended up inside, which happens when a tile appears
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
        Weapon.ResetTo(_blobXPos, _blobYPos);

        _blobJumpPlatformEngine.ResetPlatforms();

        // Unstick Blob from the screenmap if he's got stuck due to a tile map change
        UnstickBlobFromTiles();

        if (_directionGoingThroughTunnel != Direction.None)
        {
            if (_directionGoingThroughTunnel != Direction.Left)
            {
                if (_directionGoingThroughTunnel == Direction.Right)
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
            }
            else
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

            _directionGoingThroughTunnel = Direction.None;
        }

        foreach (ResetTile t in _tilesToReset)
        {
            ScreenTile[,] screenTiles = _gameScreens.GetGameScreen(t.ScreenNumber).ScreenTiles;

            screenTiles[
                t.TileToReset.MapXPosition,
                t.TileToReset.MapYPosition].TileImage = t.TileToReset.TileNumber;
        }

        _tilesToReset.Clear();
        Weapon.ResetEffects();
    }

    public void Draw(int screenFade)
    {
        switch (BlobsState)
        {
            case BlobState.Normal:
            {
                DrawNormalState(screenFade);
                break;
            }
            case BlobState.Overloading when _overload.ShowingTheAlarm:
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
        Weapon.DrawTrail();

        DrawBlobSprite(screenFade);
        DrawShuttle(screenFade);

        _blobJumpPlatformEngine.Draw();

        Weapon.Draw(screenFade, _shuttle.Riding);
        DrawTeleportBeam();
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
        if (_shuttle.Riding)
        {
            if (_graphicsSet.IsOriginal)
            {
                SpriteCollection blobShuttleOriginal = _resources.GetSpriteCollection(Assets.Graphics.BlobShuttleOriginal);
                blobShuttleOriginal.Begin(BlendState.AlphaBlend);
                blobShuttleOriginal.DrawSprite(
                    _shuttle.Frame, _blobXPos, _blobYPos + SpriteMetrics.SpriteHeight,
                    ColourHelper.WhiteFadedTo((byte)screenFade));
                blobShuttleOriginal.End();
            }
            else
            {
                SpriteCollection blobShuttle = _resources.GetSpriteCollection(Assets.Graphics.BlobShuttle);
                blobShuttle.Begin(BlendState.AlphaBlend);
                blobShuttle.DrawSprite(0, _blobXPos,
                    _blobYPos + SpriteMetrics.SpriteHeight, ColourHelper.WhiteFadedTo((byte)screenFade));
                blobShuttle.End();
            }
        }
    }

    private void DrawTeleportBeam()
    {
        if (_startBlobTeleporting)
        {
            _blobTeleporterBeam.Draw();
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
        // Make Blob flicker
        if (_overload.Visible)
        {
            SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);
            blobSprites.Begin(BlendState.AlphaBlend);
            blobSprites.DrawSprite(
                BlobFrame, _blobXPos, _blobYPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                    (byte)(_teleportFadeLevel * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
            blobSprites.End();
        }

        if (_shuttle.Riding)
        {
            SpriteCollection blobShuttle = _resources.GetSpriteCollection(Assets.Graphics.BlobShuttle);
            blobShuttle.Begin(BlendState.AlphaBlend);
            blobShuttle.DrawSprite(0, _blobXPos,
                _blobYPos + SpriteMetrics.SpriteHeight, ColourHelper.WhiteFadedTo((byte)screenFade));
            blobShuttle.End();
        }
    }

    private void DrawOverloadFlare()
    {
        _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites)
            .Begin(BlendState.Additive);

        foreach (float scale in _overload.FlareScales())
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites)
                .DrawSprite(BlobFrame, _blobXPos, _blobYPos,
                    ColourHelper.WhiteFadedTo(OverloadFlareAlpha), scale, 0f);
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites).End();
    }

    public void DrawUpper(int screenFade)
    {
        Weapon.DrawSparks(screenFade);
    }

    public void Reset()
    {
        // Reset Blob's energy drain counter
        _blobEnergyDrainCounter = 0;

        // Reset Blob's animation frame
        BlobFrame = 5;

        // Reset Blob's state
        BlobsState = BlobState.Normal;

        // Reset Blob's position. Where that is belongs to the map - the author marks the square the
        // ship came down on - so it is asked for rather than written down here.
        Point start = _gameScreens.BlobStartPosition;
        _blobXPos = start.X;
        _blobYPos = start.Y;

        // Remove Blob from shuttle pads
        _shuttle.Reset();
        _gameScreens.ChangeShuttlePlatformState(false);

        // Clear up any rogue collision tiles etc.
        PerformPostChangeScreenActions();
    }

    private void BlobTeleporterBeam_ParticleEffectComplete()
    {
        _startBlobTeleporting = false;
    }
}
