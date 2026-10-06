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
    ///     How high Blob jumps when he has no platforms left to lay. One collision cell, which is
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
    private readonly BlobEntryPoint _entryPoint = new();
    private readonly BlobFall _fall = new();
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Inventory _inventory;
    private readonly TunnelLift _lift;
    private readonly BlobOverload _overload = new();
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly ScreenEffectEngine _screenEffects;
    private readonly Shuttle _shuttle = new();
    private readonly PlayerStats _stats;
    private readonly List<ResetTile> _tilesToReset = [];
    private readonly UnstickProbe _unstickProbe;


    private int _blobEnergyDrainCounter;

    private Guid _blobFallsSoundId;


    private Direction _blobsLastAnimDirection = Direction.None;
    private bool _blobTouched;

    private Guid _blobTunnelLiftSoundId = Guid.Empty;

    private int _blobXPos;

    private int _blobYPos;

    private Direction _directionGoingThroughTunnel = Direction.None;

    private bool _inventoryAccessed;

    private bool _jumpPressed;
    private bool _resetBlobToScreenEntryPoint;
    private bool _startBlobTeleporting;

    /// <summary>
    ///     The last reading of what is around him. A field rather than a local because the shuttle pad
    ///     is acted on at the start of the frame after the one that detected it - see
    ///     <see cref="UpdateNormalState" />.
    /// </summary>
    private BlobSurroundings _surroundings;

    private int _teleportFadeLevel;

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
        ToggleKeys input,
        Action onExploded)
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

        _onExploded = onExploded;

        Weapon = new BlobWeapon(resources, gameScreens, graphics, graphicsSet, stats, randomNumberGenerator,
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

    private int BlobHSpeed { get; } = BlobWalkSpeed;

    private int BlobVSpeed { get; } = BlobWalkSpeed;

    /// <summary>
    ///     The lazer. A nasty asks it whether a shot is in flight and where it is, and tells it when it
    ///     has been hit.
    /// </summary>
    public BlobWeapon Weapon { get; }

    public CustomControl BlobsControl { get; }

    /// <summary>
    ///     Told when Blob comes apart, so the picture can be knocked about. A callback rather than
    ///     the thing that shakes it, which Blob has no business holding.
    /// </summary>
    private readonly Action _onExploded;

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

    /// <summary>
    ///     Starts Blob dissolving into a teleport beam. The booth he walked into has already decided
    ///     he is going somewhere; this is only how it looks.
    /// </summary>
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

            int amountToMove = _gameScreens.Collisions.CheckCollision(Direction.Left, _blobXPos, _blobYPos,
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

            int amountToMove = _gameScreens.Collisions.CheckCollision(Direction.Right, _blobXPos, _blobYPos,
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
            _blobYPos -= _gameScreens.Collisions.CheckCollision(Direction.Up, _blobXPos,
                _blobYPos,
                BlobWidth, BlobHeight, BlobVSpeed, true);
            DirectionBlobMoved = Direction.Up;
        }

        if (BlobsControl.MovedDown())
        {
            _blobYPos += _gameScreens.Collisions.CheckCollision(Direction.Down, _blobXPos,
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
                    if (CanJumpWithoutAPlatform())
                    {
                        _blobYPos -= JumpHeightWithoutPlatforms;
                        _resources.GetSoundFxEngine()
                            .PlaySound(Assets.Sounds.BlobJumpsNoPlatforms, 1f, false);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Whether Blob can manage the small jump he makes with no platform left to lay: he has to be
    ///     standing on something, and there has to be room over his head to rise into.
    /// </summary>
    /// <remarks>
    ///     Both probes ask how far he may travel, which is why they read as opposites. Nought going
    ///     down means he cannot descend a pixel and so is on the ground; something going up from one
    ///     jump above where he stands means that position still has slack over it.
    /// </remarks>
    private bool CanJumpWithoutAPlatform()
        => _gameScreens.Collisions.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
               BlobWidth, BlobHeight, 1, true) == 0 &&
           _gameScreens.Collisions.CheckCollision(Direction.Up, _blobXPos,
               _blobYPos - JumpHeightWithoutPlatforms,
               BlobWidth, BlobHeight, 1, true) != 0;

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

        if (BlobsControl.MovedUp() &&
            !BlobsControl.MovedLeft() &&
            !BlobsControl.MovedRight())
        {
            if (!_inventoryAccessed &&
                !_lift.GoingUp &&
                !InAnAccessForceField())
            {
                if (accessingCheops)
                {
                    EnterCheops();
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

    /// <summary>
    ///     Whether Blob is standing in an access tunnel's force field. He cannot reach his inventory
    ///     through one, which is what stops him swapping an item while he is inside a door.
    /// </summary>
    private bool InAnAccessForceField()
        => _gameScreens.Collisions.CheckTileByObject(Direction.None, _blobXPos, _blobYPos, BlobWidth, BlobHeight, 0)
            is { TileNumber: (int)ActionTiles.AccessForceField };

    /// <summary>
    ///     Hands over to the Cheops screen, where the pyramid offers Blob a swap.
    /// </summary>
    private void EnterCheops()
    {
        _screenDirectory.Cheops.Reset();
        _screenDirectory.Current = GameScreenType.Cheops;
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

        Fall fall = _fall.Step(
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

    /// <summary>
    ///     Breaks the bridge Blob has landed on, if he came down hard enough for it to give way. The
    ///     tile is taken off the map and handed to the bridges engine, which dissolves it where it
    ///     stood; it is noted down as well, so that leaving the screen puts it back.
    /// </summary>
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

        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BridgeBreak);

        _tilesToReset.Add(new ResetTile(below, _gameScreens.ScreenNumber));

        _gameScreens.CurrentGameScreenData
            .ScreenTiles[below.MapXPosition, below.MapYPosition].TileImage = -1;

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
            _gameScreens.Collisions.CheckTileByObject(Direction.None, _blobXPos, _blobYPos, BlobWidth,
                BlobHeight, 0);

        if (tileIntersectingBlob != null && SpikesHaveGotHim(tileIntersectingBlob))
        {
            KillBlob(true, false);
        }
    }

    /// <summary>
    ///     Whether Blob is far enough into a spiked tile for its spikes to reach him. Each of the
    ///     three is given the reach its own artwork suggests rather than the whole tile - see
    ///     <see cref="SpikeReachLeft" /> - so the three cases here are the three reaches, not three
    ///     copies of one test.
    /// </summary>
    private static bool SpikesHaveGotHim(TileDetected tile)
        => tile.TileNumber switch
        {
            (int)ActionTiles.Spike2 => tile.XPosition is > SpikeReachLeft and < SpikeReachRight &&
                                       tile.YPosition > CentralSpikeReachUp,
            (int)ActionTiles.Spike1 => tile.YPosition is < WallSpikeReachDown and > WallSpikeReachUp &&
                                       tile.XPosition > WallSpikeReachIn,
            (int)ActionTiles.Spike3 => tile.XPosition is > SpikeReachLeft and < SpikeReachRight &&
                                       tile.YPosition > BrokenGlassReachUp,
            _ => false
        };

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

    /// <summary>
    ///     Logic frames since the overload began, which is all the arcing on his skin needs to
    ///     know. Counted on the update and never in the draw: <c>GameHost</c> draws once per
    ///     vertical retrace, so a flicker counted in drawn frames crackles faster on a better
    ///     monitor.
    /// </summary>
    private int _overloadFrame;

    /// <summary>
    ///     Which frame of the electricity artwork the beams are drawn from. Stepped every logic
    ///     frame of the overload so the beams crawl as well as turn.
    /// </summary>
    private int _overloadArcFrame;

    /// <summary>
    ///     Logic frames since the game began, for the flicker on the drain. Counted on the update
    ///     and never in the draw, for the reason <see cref="ElectricSettings.FlickerAt" /> gives.
    /// </summary>
    private int _drainFrame;

    /// <summary>
    ///     The shader Blob burns with, pointed at the pose about to be drawn. The flicker is
    ///     <see cref="ElectricSettings.FlickerAt" />, shared with the node and gate arcs on
    ///     purpose - this is an electrical overload, and the two should crackle alike.
    /// </summary>
    private Effect OverloadShader(SpriteCollection sprites, string technique)
    {
        Effect overload = _resources.GetEffect(Assets.Shaders.Overload);

        Vector4 spriteRect = sprites.SpriteRectIn01(BlobFrame);

        overload.Parameters["SpriteRect"].SetValue(spriteRect);
        overload.Parameters["Charge"].SetValue(
            OverloadSettings.ChargeFrom(_blobEnergyDrainCounter));
        overload.Parameters["Flicker"].SetValue(ElectricSettings.FlickerAt(_overloadFrame));

        overload.Parameters["EmberColour"].SetValue(OverloadSettings.EmberColour);
        overload.Parameters["MidColour"].SetValue(OverloadSettings.MidColour);
        overload.Parameters["HotColour"].SetValue(OverloadSettings.HotColour);

        overload.Parameters["RimColour"].SetValue(OverloadSettings.RimColour);
        overload.Parameters["RimAmount"].SetValue(OverloadSettings.RimAmount);
        overload.Parameters["InteriorWash"].SetValue(OverloadSettings.InteriorWash);

        overload.Parameters["Split"].SetValue(_overload.Split);
        overload.Parameters["CrackColour"].SetValue(OverloadSettings.CrackColour);
        overload.Parameters["CrackBrightness"].SetValue(OverloadSettings.CrackBrightness);
        overload.Parameters["CrackWidth"].SetValue(OverloadSettings.CrackWidth);
        overload.Parameters["CrackDensity"].SetValue(OverloadSettings.CrackDensity);

        // The reach is given in the sprite's own pixels and the shader samples the whole strip, so
        // it is handed over already scaled into that.
        overload.Parameters["RimStep"].SetValue(new Vector2(
            OverloadSettings.RimRadius / sprites.SpriteWidth * spriteRect.Z,
            OverloadSettings.RimRadius / sprites.SpriteHeight * spriteRect.W));

        overload.CurrentTechnique = overload.Techniques[technique];

        return overload;
    }

    private void UpdateOverloadingState()
    {
        _overloadFrame++;
        _overloadArcFrame = _overloadFrame % OverloadSettings.BeamArcFrames;

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
            _onExploded();
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
            case BlobState.Overloading when _overload.Stage == OverloadStage.Cracking:
            {
                DrawOverloadSplitting(screenFade);
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
        _drainFrame++;

        if (BlobTouchedByNasty)
        {
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

    /// <summary>
    ///     Blob as he walks about, reddening while a nasty has hold of his energy.
    /// </summary>
    /// <remarks>
    ///     The tint is white now and the red is the shader's. It used to be done here, by holding
    ///     red at full and bringing green and blue down together - which is a healthy light of a
    ///     different colour rather than something being drained. The shader is skipped entirely
    ///     when nothing is draining him, which is nearly all the time.
    /// </remarks>
    private void DrawBlobSprite(int screenFade)
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

        bool draining = EnergyDrainSettings.IsDraining(_blobEnergyDrainCounter);

        blobSprites.Begin(BlendState.AlphaBlend, null, draining ? DrainShader(blobSprites) : null);
        blobSprites.DrawSprite(BlobFrame,
            _blobXPos, _blobYPos,
            ColourHelper.WhiteFadedTo(
                (byte)(_teleportFadeLevel * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
        blobSprites.End();
    }

    /// <summary>
    ///     The shader that reddens him while he is being drained, pointed at the pose about to be
    ///     drawn. The flicker is <see cref="ElectricSettings.FlickerAt" />, shared with the node
    ///     arcs and with his overload - it is all the same electricity.
    /// </summary>
    private Effect DrainShader(SpriteCollection sprites)
    {
        Effect drain = _resources.GetEffect(Assets.Shaders.Overload);

        Vector4 spriteRect = sprites.SpriteRectIn01(BlobFrame);

        drain.Parameters["SpriteRect"].SetValue(spriteRect);
        drain.Parameters["Drain"].SetValue(
            EnergyDrainSettings.DrainFrom(_blobEnergyDrainCounter));
        drain.Parameters["DrainColour"].SetValue(EnergyDrainSettings.DrainColour);
        drain.Parameters["RimAmount"].SetValue(EnergyDrainSettings.CoronaAmount);
        drain.Parameters["Flicker"].SetValue(ElectricSettings.FlickerAt(_drainFrame));

        drain.Parameters["RimStep"].SetValue(new Vector2(
            EnergyDrainSettings.CoronaRadius / sprites.SpriteWidth * spriteRect.Z,
            EnergyDrainSettings.CoronaRadius / sprites.SpriteHeight * spriteRect.W));

        drain.CurrentTechnique = drain.Techniques["Draining"];

        return drain;
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
        if (_overload.Visible)
        {
            SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

            // White rather than the red this used to tint him: the heat is the shader's now, and a
            // tint on top of it would only take the colour back out again.
            blobSprites.Begin(BlendState.AlphaBlend, null,
                OverloadShader(blobSprites, "Charging"));
            blobSprites.DrawSprite(
                BlobFrame, _blobXPos, _blobYPos,
                ColourHelper.WhiteFadedTo(
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

    /// <summary>
    ///     The beat before the burst: he swells and shakes while cracks open across him with light
    ///     coming out of them. The swell and the shrink that follow last eleven logic frames
    ///     between them, which is too quick to read as anything - this is the part there is time
    ///     to see.
    /// </summary>
    /// <remarks>
    ///     He is drawn at the overload's own scale, which grows over these frames in step with the
    ///     cracks. Holding him still and then bursting read as two things one after the other; the
    ///     cracks widening because he is pulling apart is what makes it one thing.
    /// </remarks>
    private void DrawOverloadSplitting(int screenFade)
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

        Vector2 shake = OverloadSettings.ShakeAt(_overloadFrame, _overload.Split);

        blobSprites.Begin(BlendState.AlphaBlend, null, OverloadShader(blobSprites, "Splitting"));
        blobSprites.DrawSprite(
            BlobFrame,
            _blobXPos + (int)shake.X,
            _blobYPos + (int)shake.Y,
            ColourHelper.WhiteFadedTo((byte)screenFade), _overload.Scale, 0f);
        blobSprites.End();

        DrawOverloadBeams(screenFade, shake);
    }

    /// <summary>
    ///     The beams of charge whipping around him while he comes apart, thrown further out as the
    ///     cracks widen.
    /// </summary>
    /// <remarks>
    ///     The node and gate arc artwork, turned about him - the game already has a drawing of
    ///     electricity and this is electricity, so it would be odd to draw a second kind. They go
    ///     through the same burning shader the nodes do, so they have the same white core and halo,
    ///     and they are drawn additively over him rather than under, because a beam passing in
    ///     front of him should light him rather than disappear behind him.
    /// </remarks>
    private void DrawOverloadBeams(int screenFade, Vector2 shake)
    {
        SpriteCollection arc = _resources.GetSpriteCollection(Assets.Graphics.ElectricityNodeElectricity1);

        int centreX = _blobXPos + SpriteMetrics.HalfSpriteWidth + (int)shake.X;
        int centreY = _blobYPos + SpriteMetrics.HalfSpriteHeight + (int)shake.Y;

        arc.Begin(BlendState.Additive, null, ElectricSettings.PointAt(
            _resources.GetEffect(Assets.Shaders.Electric),
            arc.SpriteRectIn01(_overloadArcFrame),
            arc.SpriteWidth,
            arc.SpriteHeight,
            _overloadFrame));

        for (int beam = 0; beam < OverloadSettings.BeamCount; beam++)
        {
            (Vector2 offset, float angle) = OverloadSettings.BeamAt(
                _overloadFrame, beam, _overload.Split);

            arc.DrawSprite(
                _overloadArcFrame,
                centreX + offset.X - arc.SpriteWidth / 2f,
                centreY + offset.Y - arc.SpriteHeight / 2f,
                ColourHelper.WhiteFadedTo((byte)screenFade),
                OverloadSettings.BeamScale,
                angle);
        }

        arc.End();
    }

    /// <summary>
    ///     The discharge. Five copies of him at rising scales, each drawn as a burning outline
    ///     rather than a ghost of him, and coloured by how far out it is - white-hot at the middle,
    ///     cooling to electric blue at the front. Five solid copies read as five Blobs; five
    ///     outlines read as a blast.
    /// </summary>
    private void DrawOverloadFlare()
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

        blobSprites.Begin(BlendState.Additive, null, OverloadShader(blobSprites, "Discharging"));

        int copy = 0;

        foreach (float scale in _overload.FlareScales())
        {
            // The colour of each shell rides in on the tint, which is per draw - so all five stay
            // in one batch rather than needing a shader setting apiece.
            Vector3 shell = OverloadSettings.ShellColour(copy, BlobOverload.FlareCopies);

            blobSprites.DrawSprite(BlobFrame, _blobXPos, _blobYPos,
                new Color(shell.X, shell.Y, shell.Z, OverloadFlareAlpha / (float)ColourLevels.Full),
                scale, 0f);

            copy++;
        }

        blobSprites.End();
    }

    public void DrawUpper(int screenFade)
    {
        Weapon.DrawSparks(screenFade);
    }

    /// <summary>
    ///     Blob at the start of a new game: standing still, at full health, back on the square the
    ///     ship came down on and off the shuttle.
    /// </summary>
    public void Reset()
    {
        _blobEnergyDrainCounter = 0;
        BlobFrame = BlobRestingFrame;
        BlobsState = BlobState.Normal;

        // Where he starts belongs to the map - the author marks the square the ship came down on -
        // so it is asked for rather than written down here.
        Point start = _gameScreens.BlobStartPosition;
        _blobXPos = start.X;
        _blobYPos = start.Y;

        TakeBlobOffTheShuttle();

        PerformPostChangeScreenActions();
    }

    /// <summary>
    ///     Puts the shuttle back on its pad and the pad back on every screen, so a new game does not
    ///     start with the shuttle wherever the last one left it.
    /// </summary>
    private void TakeBlobOffTheShuttle()
    {
        _shuttle.Reset();
        _gameScreens.ChangeShuttlePlatformState(false);
    }

    private void BlobTeleporterBeam_ParticleEffectComplete()
    {
        _startBlobTeleporting = false;
    }
}
