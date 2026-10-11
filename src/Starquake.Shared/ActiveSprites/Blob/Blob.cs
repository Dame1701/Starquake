// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Engines.ScreenEffects;
using Starquake.GameComponents;
using Starquake.Input;
using Starquake.Particles;
using Starquake.Scoring;
using Starquake.SharedLibs;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;
using Starquake.Tuning;
using Starquake.World;

namespace Starquake.ActiveSprites;

internal class Blob
{
    /// <summary>
    ///     How fast the tunnel transition plays, in pixels of play area a frame. A pace rather than a
    ///     literal step, because <see cref="TunnelEntryEffect" /> eases the motion: the squeeze lasts
    ///     as long as it would at this speed, and the dark and the arrival follow from that.
    /// </summary>
    private const int TunnelEntrySpeed = 64;

    /// <summary>
    ///     How far Blob must fall to break a breakable bridge. A shorter drop leaves it standing.
    /// </summary>
    private const int BridgeBreakingFallHeight = 128;

    /// <summary>
    ///     How high Blob jumps with no platforms left to lay. One collision cell, just enough to
    ///     clear a single step.
    /// </summary>
    private const int JumpHeightWithoutPlatforms = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     The alpha each copy of Blob is drawn at during the explosion flare. The sizes and the
    ///     count belong to <see cref="BlobOverload" />. This is a drawing colour, so it lives here.
    /// </summary>
    private const int OverloadFlareAlpha = 150;

    /// <summary>How fast Blob walks, in pixels a logic frame, horizontally and vertically.</summary>
    private const int WalkSpeed = 8;

    /// <summary>
    ///     How far apart a slow walk's footfalls are: as far as the full step covers while the
    ///     footfall sound, a tenth of a second long, plays once. See <see cref="PatterFeet" />.
    /// </summary>
    private static readonly int FootfallSpacing = WalkSpeed * LogicClock.Seconds(0.1);

    /// <summary>
    ///     Where the teleport beam and the shot's smoke trail are emitted from, measured in from the
    ///     top left of Blob and of the shot.
    /// </summary>
    private const int TeleportBeamXOffset = 30;

    /// <inheritdoc cref="TeleportBeamXOffset" />
    private const int TeleportBeamYOffset = 50;

    /// <summary>
    ///     Where Blob is put down after coming through a tunnel. Far enough inside the mouth on the
    ///     far side not to be standing in the tunnel just left.
    /// </summary>
    private const int TunnelExitLeftXOffset = 24;

    /// <inheritdoc cref="TunnelExitLeftXOffset" />
    private const int TunnelExitRightXOffset = 40;

    /// <inheritdoc cref="TunnelExitLeftXOffset" />
    private const int TunnelExitYOffset = 32;

    private readonly Animation _blobAnimLeft;
    private readonly Animation _blobAnimRight;


    private readonly JumpPlatforms _blobJumpPlatformEngine;
    private readonly BlobDeath _blobKiller;

    private readonly IStandardParticleEffect _blobTeleporterBeam;
    private readonly BreakableBridges _breakableBridges;
    private readonly BlobEntryPoint _entryPoint = new();
    private readonly EnergyDrain _drain = new();
    private readonly TeleportFade _teleport = new();
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


    private Guid _blobFallsSoundId;


    private Direction _blobsLastAnimDirection = Direction.None;

    /// <summary>How far Blob has walked since the walk cycle last moved on. See <see cref="Stride" />.</summary>
    private int _strideWalked;

    /// <summary>How much further a slow walk goes before the next footfall. See <see cref="PatterFeet" />.</summary>
    private int _distanceToFootfall;

    private Guid _blobTunnelLiftSoundId = Guid.Empty;

    private int _xPos;

    private int _yPos;

    private Direction _directionGoingThroughTunnel = Direction.None;

    private bool _inventoryAccessed;

    private bool _jumpPressed;
    private bool _resetBlobToScreenEntryPoint;

    /// <summary>
    ///     The last reading of Blob's surroundings. A field rather than a local, because a shuttle pad
    ///     is acted on at the start of the frame after the one that detected it. See
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
        BlobDeath blobKiller,
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
        Control = new CustomControl(session.PlayerKeys, input, session);
        State = BlobState.Normal;

        _blobTeleporterBeam =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BeamMeUpSmall,
                ParticleEffectType.Triggered, null);
        _blobTeleporterBeam.ParticleEffectComplete += TeleporterBeam_ParticleEffectComplete;

        _onExploded = onExploded;

        Weapon = new BlobWeapon(resources, gameScreens, graphics, graphicsSet, stats, randomNumberGenerator,
            () => OnActionRaised?.Invoke(BlobAction.FiredLazer, null));

        _lift = new TunnelLift(
            () => _blobTunnelLiftSoundId = _resources.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.TunnelLift, true),
            () => _resources.GetSoundFxEngine()
                .StopSound(_blobTunnelLiftSoundId, SoundFade.None));

        // The shot starts wherever Blob is, which here is the origin. Being put onto a screen
        // happens afterwards, through PerformPostChangeScreenActions.
        Weapon.ResetTo(_xPos, _yPos);

        // Both loop forward with no yoyo. The left walk is the exception: it runs backwards through
        // the right walk's frames.
        _blobAnimLeft = Animation.Looping(BlobWalkFrames.WalkLeftFirst, BlobWalkFrames.WalkLeftLast, BlobWalkFrames.Delay, false);
        _blobAnimRight = Animation.Looping(BlobWalkFrames.WalkRightFirst, BlobWalkFrames.WalkRightLast, BlobWalkFrames.Delay, true);

        _inventory = new Inventory();
        _unstickProbe = new UnstickProbe(gameScreens.Collisions);
    }

    /// <summary>
    ///     What Blob is carrying, as the status panel draws it and the access booths read it. The
    ///     queue itself belongs to <see cref="Inventory" />.
    /// </summary>
    public Collectables[] Carrying => _inventory.Items;

    private Direction DirectionBlobMoved { get; set; } = Direction.None;

    public int XPos
    {
        get => _xPos;
        set
        {
            _xPos = value;
            _entryPoint.XPos = value;
        }
    }

    public int YPos
    {
        get => _yPos;
        set
        {
            _yPos = value;
            _entryPoint.YPos = value;
        }
    }

    public BlobState State { get; set; }

    public bool TouchedByNasty { get; set; }

    public int Width => SpriteMetrics.SpriteWidth;

    /// <summary>
    ///     Blob's height, shuttle included. Owned by <see cref="Shuttle" />, because riding it is
    ///     what changes the height.
    /// </summary>
    public int Height => _shuttle.BlobHeight;

    public int Frame { get; set; } = BlobWalkFrames.Resting;

    private int HorizontalSpeed { get; } = WalkSpeed;

    private int VerticalSpeed { get; } = WalkSpeed;

    /// <summary>
    ///     The lazer. A nasty asks it whether a shot is in flight and where, and reports being hit.
    /// </summary>
    public BlobWeapon Weapon { get; }

    public CustomControl Control { get; }

    /// <summary>
    ///     Called when Blob comes apart, so the picture can be shaken. A callback rather than the
    ///     thing that does the shaking, which Blob has no business holding.
    /// </summary>
    private readonly Action _onExploded;

    public event RaiseScreenChanged? OnScreenChanged;

    public event RaiseBlobAction? OnActionRaised;

    /// <summary>
    ///     Raised as Blob is put back together after a death, so anything holding hazard state can
    ///     clear it. The engine answers by resetting the electricity nodes and gates.
    ///     <para>
    ///         An event rather than a call, because the electricity engine needs Blob to know where
    ///         it is standing and so cannot be built before Blob.
    ///     </para>
    /// </summary>
    public event Action? OnDiedAndRespawned;

    public void Kill(bool resetBlobToScreenEntryPoint, bool overloadFirst)
    {
        if (State != BlobState.Normal)
        {
            return;
        }

        _resetBlobToScreenEntryPoint = resetBlobToScreenEntryPoint;

        _overload.Begin(overloadFirst);

        State = BlobState.Overloading;
    }

    /// <summary>
    ///     Starts Blob dissolving into a teleport beam. The booth has already decided the
    ///     destination, so this is only the appearance.
    /// </summary>
    public void TriggerTeleportedAnimation()
    {
        _blobTeleporterBeam.Start();
        _teleport.Begin();
    }

    public void Update(GameTime gameTime)
    {
        bool changedScreens = false;

        switch (State)
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

        _blobTeleporterBeam.Update(_xPos + TeleportBeamXOffset, _yPos + TeleportBeamYOffset, gameTime);
        Weapon.UpdateEffects(gameTime);

        if (!changedScreens)
        {
            return;
        }

        _entryPoint.Record(XPos, YPos, Frame, _shuttle.Riding);
    }

    /// <summary>One frame of Blob being alive and under the player's control.</summary>
    /// <returns>
    ///     Whether the frame moved Blob onto a different screen, which tells <see cref="Update" /> to
    ///     record a new entry point.
    /// </returns>
    private bool UpdateNormalState()
    {
        // Last frame's reading on purpose. The shuttle noticed the pad from that same reading, so
        // its flag and the tile underneath are a matched pair. Probing again here would act on a
        // flag that no longer describes the tile given with it.
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
            Control.HasFired(),
            new Rectangle(_xPos, _yPos, Width, Height),
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

        _drain.Step(TouchedByNasty);
        _teleport.Step();

        return changedScreens;
    }

    /// <summary>
    ///     Acts on the shuttle pad Blob is standing on. Taking the shuttle or putting it back changes
    ///     the tile map on every screen, so the collision map is rebuilt afterwards. It is also
    ///     rebuilt when merely standing on a pad, which is what the original did.
    /// </summary>
    private void HandleShuttlePad(TileDetected? below)
    {
        switch (_shuttle.Step(below, Control.MovedUp(), Control.MovedLeft(), Control.MovedRight()))
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

    /// <summary>
    ///     Moves the walk cycle on by the distance stepped rather than once a frame, so a slow
    ///     analogue walk does not shuffle its feet on the spot. At the full step it moves on once a
    ///     frame, as it always has, whether or not a wall stops the step.
    /// </summary>
    private void Stride(Animation walk, int step)
    {
        _strideWalked += step;

        while (_strideWalked >= HorizontalSpeed)
        {
            walk.GetNextFrame();
            _strideWalked -= HorizontalSpeed;
        }
    }

    /// <summary>
    ///     The sound of Blob's feet. At the full step it is asked for every frame and starts again
    ///     whenever it finishes, as it always has. A slower analogue walk spaces the footfalls out by
    ///     the distance the full step covers between them instead, so the feet slow down with Blob.
    /// </summary>
    private void PatterFeet(int step)
    {
        if (_shuttle.Riding || _fall.Height != 0)
        {
            return;
        }

        if (step == HorizontalSpeed)
        {
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobRun, true);

            return;
        }

        if (_distanceToFootfall <= 0)
        {
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobRun, true);
            _distanceToFootfall += FootfallSpacing;
        }

        _distanceToFootfall -= step;
    }

    private void WalkLeft()
    {
        if (Control.MovedLeft())
        {
            int step = Control.WalkStep(HorizontalSpeed, _xPos, true);

            if (_blobsLastAnimDirection == Direction.Right)
            {
                _blobAnimLeft.CurrentFrame = BlobWalkFrames.TurningLeftFrom(Frame);
            }
            else
            {
                Stride(_blobAnimLeft, step);
            }

            int amountToMove = _gameScreens.Collisions.CheckCollision(Direction.Left, _xPos, _yPos,
                Width, Height, step, true);
            if (amountToMove > 0)
            {
                _xPos -= amountToMove;
                if (_shuttle.Riding)
                {
                    _shuttle.Advance();
                }
            }

            Frame = _blobAnimLeft.CurrentFrame;
            DirectionBlobMoved = Direction.Left;
            _blobsLastAnimDirection = Direction.Left;

            PatterFeet(step);
        }
    }

    private void WalkRight()
    {
        if (Control.MovedRight())
        {
            int step = Control.WalkStep(HorizontalSpeed, _xPos, false);

            if (_blobsLastAnimDirection == Direction.Left)
            {
                _blobAnimRight.CurrentFrame = BlobWalkFrames.TurningRightFrom(Frame);
            }
            else
            {
                Stride(_blobAnimRight, step);
            }

            int amountToMove = _gameScreens.Collisions.CheckCollision(Direction.Right, _xPos, _yPos,
                Width, Height, step, true);
            if (amountToMove > 0)
            {
                _xPos += amountToMove;
                if (_shuttle.Riding)
                {
                    _shuttle.Advance();
                }
            }

            Frame = _blobAnimRight.CurrentFrame;
            DirectionBlobMoved = Direction.Right;
            _blobsLastAnimDirection = Direction.Right;

            PatterFeet(step);
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

        if (Control.MovedUp())
        {
            _yPos -= _gameScreens.Collisions.CheckCollision(Direction.Up, _xPos,
                _yPos,
                Width, Height, VerticalSpeed, true);
            DirectionBlobMoved = Direction.Up;
        }

        if (Control.MovedDown())
        {
            _yPos += _gameScreens.Collisions.CheckCollision(Direction.Down, _xPos,
                _yPos, Width, Height, VerticalSpeed, true);
            DirectionBlobMoved = Direction.Down;
        }
    }

    private void LayJumpPlatformOrJump()
    {
        if (Control.LaysPlatform())
        {
            if (!_jumpPressed)
            {
                _jumpPressed = true;

                if (_stats.Platforms > 0)
                {
                    if (!_shuttle.OnThePad)
                    {
                        int newYPos = _blobJumpPlatformEngine.GetNewJumpPlatform(_xPos, _yPos,
                            Width,
                            Height, true);

                        if (newYPos != JumpPlatforms.NoRoomForPlatform)
                        {
                            _yPos = newYPos;

                            _resources.GetSoundFxEngine()
                                .PlaySound(Assets.Sounds.BlobJumps, 0.3f, false);

                            if (OnActionRaised != null)
                            {
                                OnActionRaised(BlobAction.LaidPlatform, null);
                            }
                        }
                    }
                }
                else // just jump aimlessly in the air
                {
                    if (CanJumpWithoutAPlatform())
                    {
                        _yPos -= JumpHeightWithoutPlatforms;
                        _resources.GetSoundFxEngine()
                            .PlaySound(Assets.Sounds.BlobJumpsNoPlatforms, 1f, false);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Whether Blob can manage the small jump made with no platform left to lay. Needs something
    ///     to stand on and room overhead to rise into.
    /// </summary>
    /// <remarks>
    ///     Both probes ask how far he may travel, which is why they read as opposites. Nought going
    ///     down means he cannot descend a pixel and so is on the ground; something going up from one
    ///     jump above where he stands means that position still has slack over it.
    /// </remarks>
    private bool CanJumpWithoutAPlatform()
        => _gameScreens.Collisions.CheckCollision(Direction.Down, _xPos, _yPos,
               Width, Height, 1, true) == 0 &&
           _gameScreens.Collisions.CheckCollision(Direction.Up, _xPos,
               _yPos - JumpHeightWithoutPlatforms,
               Width, Height, 1, true) != 0;

    private void AccessInventory()
    {
        bool accessingCheops = false;
        if (_gameScreens.CurrentGameScreenData.CheopsPyramid != null)
        {
            if (CollisionDetection.Check(
                    new Rectangle(
                        _xPos,
                        _yPos,
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

        if (Control.SwapsInventory())
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
                        new Rectangle(_xPos, YPos, Width, Height),
                        _gameScreens.CurrentGameScreenData.PickUps);

                    _gameScreens.UpdateCollisionMap(false);
                }
            }
        }
    }

    /// <summary>
    ///     Whether Blob is standing in an access tunnel's force field. The inventory cannot be
    ///     reached through one, which stops an item being swapped from inside a door.
    /// </summary>
    private bool InAnAccessForceField()
        => _gameScreens.Collisions.CheckTileByObject(Direction.None, _xPos, _yPos, Width, Height, 0)
            is { TileNumber: (int)ActionTiles.AccessForceField };

    /// <summary>Hands over to the Cheops screen, where the pyramid offers Blob a swap.</summary>
    private void EnterCheops()
    {
        _screenDirectory.Cheops.Reset();
        _screenDirectory.Current = GameScreenType.Cheops;
    }

    private void ReleaseHeldControls()
    {
        if (!Control.AskedForInventory())
        {
            _inventoryAccessed = false;
        }

        if (!Control.AskedForPlatform())
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
    ///     Not real physics. The fall speed steps up the longer a fall lasts and resets on landing,
    ///     which is all the game needs.
    /// </summary>
    private void ApplyGravity()
    {
        if (_lift.GoingUp) // Can't fall if we are in a tunnel lift
        {
            return;
        }

        Fall fall = _fall.Step(
            _gameScreens.Collisions,
            new Rectangle(_xPos, _yPos, Width, Height),
            Control.MovedLeft(),
            Control.MovedRight());

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

        _yPos += fall.Distance;
    }

    /// <summary>
    ///     Moves Blob onto the next screen after walking off an edge of this one.
    /// </summary>
    /// <remarks>
    ///     All four edges are tried in turn without short-circuiting, because more than one can
    ///     answer. A lift carrying Blob up while walking off the left edge moves a screen left and a
    ///     screen up, and the top crossing is asked about the position the left one produced.
    /// </remarks>
    /// <returns>Whether a screen change happened.</returns>
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
        if (ScreenEdges.OffLeft(_xPos, _yPos, Width, _gameScreens.ScreenWidth,
                _gameScreens.ScreenNumber) is not { } crossing)
        {
            return false;
        }

        _gameScreens.ScreenNumber = crossing.ScreenNumber;
        _xPos = crossing.XPos;

        RaiseScreenChangedToMainGame();

        return true;
    }

    /// <inheritdoc cref="ChangeScreenOffLeftEdge" />
    private bool ChangeScreenOffRightEdge()
    {
        if (ScreenEdges.OffRight(_xPos, _yPos, Width, _gameScreens.ScreenWidth,
                _gameScreens.ScreenNumber) is not { } crossing)
        {
            return false;
        }

        _gameScreens.ScreenNumber = crossing.ScreenNumber;
        _xPos = crossing.XPos;

        RaiseScreenChangedToMainGame();

        return true;
    }

    /// <summary>
    ///     Going up, Blob has to be put down on something. On the shuttle that is the foot of the new
    ///     screen; on foot it is a fresh jump platform, unless a tunnel lift is carrying Blob, which
    ///     keeps carrying. That lift case is the one arrival <see cref="ScreenEdges" /> cannot work
    ///     out, and the one it hands back unchanged.
    /// </summary>
    private bool ChangeScreenOffTopEdge()
    {
        if (ScreenEdges.OffTop(_xPos, _yPos, _gameScreens.ScreenNumber) is not { } crossing)
        {
            return false;
        }

        _gameScreens.ScreenNumber = crossing.ScreenNumber;

        RaiseScreenChangedToMainGame();

        if (_shuttle.Riding || _lift.GoingUp)
        {
            _yPos = _gameScreens.ScreenHeight - Height;
        }
        else
        {
            _yPos = _blobJumpPlatformEngine.GetNewJumpPlatform(
                _xPos,
                _gameScreens.ScreenHeight - _resources
                    .GetSpriteCollection(_graphicsSet.Names.BlobSprites).SpriteHeight,
                0,
                0,
                false);
        }

        return true;
    }

    /// <inheritdoc cref="ChangeScreenOffLeftEdge" />
    private bool ChangeScreenOffBottomEdge()
    {
        if (ScreenEdges.OffBottom(_xPos, _yPos, Height, _gameScreens.ScreenHeight,
                _gameScreens.ScreenNumber) is not { } crossing)
        {
            return false;
        }

        _gameScreens.ScreenNumber = crossing.ScreenNumber;
        _yPos = crossing.YPos;

        // Falling onto the new screen, so the fall starts again from its top.
        _fall.StartFallingAgain();

        RaiseScreenChangedToMainGame();

        return true;
    }

    private void RaiseScreenChangedToMainGame()
    {
        OnScreenChanged?.Invoke(GameScreenType.MainGame);
    }

    /// <summary>
    ///     Reads Blob's surroundings. Riding the shuttle adds height underneath rather than more
    ///     Blob, so the ground is looked for from Blob's own feet.
    /// </summary>
    private BlobSurroundings DetectSurroundingTiles()
        => BlobSurroundings.Around(
            _gameScreens.Collisions,
            _xPos,
            _yPos,
            Width,
            Height,
            // Blob's own height is the sprite's whether aboard the shuttle or not. The shuttle is
            // the whole of the difference between the two heights.
            SpriteMetrics.SpriteHeight,
            HorizontalSpeed,
            VerticalSpeed);

    private void DetectTeleporter(TileDetected? toTheRight)
    {
        if (toTheRight == null || !Control.MovedRight())
        {
            return;
        }

        // Only X is set below. The Y the booth tests for is the arrival offset, so Blob already
        // stands exactly where a booth puts him.
        if (!BoothEntry.IntoATeleporter(toTheRight))
        {
            return;
        }

        _screenDirectory.Current = GameScreenType.Teleporter;
        _screenDirectory.Teleporter.Reset();
        _xPos = toTheRight.MapXPosition * TileMetrics.TileWidth + BoothMetrics.BlobXOffset;
        Frame = BlobWalkFrames.WalkRightFirst;
    }

    /// <summary>
    ///     Each side detects the booth tile on one side of Blob and reports the map position of the
    ///     tile on the other side, so both tiles must be present for either to trigger.
    /// </summary>
    private void DetectAccessBooth(TileDetected? toTheLeft, TileDetected? toTheRight)
    {
        if (toTheRight != null && BoothEntry.IntoARightHandBooth(toTheLeft))
        {
            _screenDirectory.AccessBooth.Reset(Direction.Left, toTheRight.MapXPosition);
            _screenDirectory.Current = GameScreenType.AccessBooth;
        }

        if (toTheLeft == null || !BoothEntry.IntoALeftHandBooth(toTheRight))
        {
            return;
        }

        _screenDirectory.AccessBooth.Reset(Direction.Right, toTheLeft.MapXPosition);
        _screenDirectory.Current = GameScreenType.AccessBooth;
    }

    private bool DetectLeftHandTunnel(TileDetected? toTheLeft)
    {
        bool changedScreens = false;

        if (!TunnelMouths.IntoALeftMouth(toTheLeft))
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

        if (!TunnelMouths.IntoARightMouth(toTheRight))
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
    ///     Breaks the bridge Blob landed on, if the drop was hard enough. The tile is taken off the
    ///     map and handed to the bridges engine, which dissolves it where it stood. It is also noted
    ///     down, so leaving the screen puts it back.
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
        => _yPos -= _lift.Step(
            _gameScreens.Collisions,
            new Rectangle(_xPos, _yPos, Width, Height),
            above,
            Control.MovedLeft(),
            Control.MovedRight(),
            HorizontalSpeed);

    private void CheckSpikes()
    {
        TileDetected? tileIntersectingBlob =
            _gameScreens.Collisions.CheckTileByObject(Direction.None, _xPos, _yPos, Width,
                Height, 0);

        if (tileIntersectingBlob != null && Spikes.HaveGotHim(tileIntersectingBlob))
        {
            Kill(true, false);
        }
    }

    private void CollectPowerUp()
    {
        if (_gameScreens.CurrentGameScreenData.PowerUp == null)
        {
            return;
        }

        if (!CollisionDetection.Check(
                _xPos,
                YPos,
                Width,
                Height,
                _gameScreens.CurrentGameScreenData.PowerUp.XLocation,
                _gameScreens.CurrentGameScreenData.PowerUp.YLocation,
                TileMetrics.CollisionCellWidth,
                TileMetrics.CollisionCellHeight))
        {
            return;
        }

        OnActionRaised?.Invoke(BlobAction.GotPowerUp, _gameScreens.CurrentGameScreenData.PowerUp.Type);

        _gameScreens.CurrentGameScreenData.PowerUp = null;
        _gameScreens.UpdateCollisionMap(false);
    }




    /// <summary>
    ///     The shader Blob burns with, pointed at the pose about to be drawn. Flickers at
    ///     <see cref="ElectricSettings.FlickerAt" />, the same rate as the node and gate arcs, so the
    ///     overload crackles like the electricity it is.
    /// </summary>
    private Effect OverloadShader(SpriteCollection sprites, string technique)
    {
        Effect overload = _resources.GetEffect(Assets.Shaders.Overload);

        Vector4 spriteRect = sprites.SpriteRectIn01(Frame);

        overload.Parameters["SpriteRect"].SetValue(spriteRect);
        overload.Parameters["Charge"].SetValue(
            OverloadSettings.ChargeFrom(_drain.Level));
        overload.Parameters["Flicker"].SetValue(ElectricSettings.FlickerAt(_overload.Frame));

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

        // The reach is given in the sprite's own pixels but the shader samples the whole strip, so
        // it is scaled before being handed over.
        overload.Parameters["RimStep"].SetValue(new Vector2(
            OverloadSettings.RimRadius / sprites.SpriteWidth * spriteRect.Z,
            OverloadSettings.RimRadius / sprites.SpriteHeight * spriteRect.W));

        overload.CurrentTechnique = overload.Techniques[technique];

        return overload;
    }

    private void UpdateOverloadingState()
    {
        _overload.CountAFrame();

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
            OnDiedAndRespawned?.Invoke();

            if (_overload.Swell() != OverloadStage.Exploded)
            {
                return;
            }

            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobExplodes, false);
            _blobKiller.Trigger(XPos, YPos, true);
            _onExploded();
            State = BlobState.Dying;
        }
    }

    private void UpdateDyingState()
    {
        if (_blobKiller.Triggered)
        {
            return;
        }

        _overload.Reset();

        // A spike death puts Blob back where he entered the screen.
        if (_resetBlobToScreenEntryPoint)
        {
            _yPos = _entryPoint.YPos;
            _xPos = _entryPoint.XPos;
            _shuttle.PutBack(_entryPoint.OnShuttle);
            _gameScreens.ChangeShuttlePlatformState(_shuttle.Riding);
            _gameScreens.UpdateCollisionMap(false);
            Frame = _entryPoint.Frame;
            _lift.LetGo();
            _fall.StartFallingAgain();
        }

        _resetBlobToScreenEntryPoint = false;
        State = BlobState.Dead;
        _blobKiller.Reset();
    }

    /// <summary>
    ///     Pushes Blob out of scenery, which happens when a tile appears underneath: a shuttle
    ///     platform being taken, or a screen changing beneath Blob's feet.
    /// </summary>
    /// <remarks>
    ///     Each edge is tried in turn, and only when Blob is caught on that edge and free at the
    ///     opposite one. Caught on both means there is nowhere to push to, so Blob stays put.
    /// </remarks>
    public void UnstickFromTiles()
    {
        Point freed = _unstickProbe.FreePositionFor(
            new Rectangle(_xPos, _yPos, Width, Height));

        _xPos = freed.X;
        _yPos = freed.Y;
    }

    public void PerformPostChangeScreenActions()
    {
        Weapon.ResetTo(_xPos, _yPos);

        _blobJumpPlatformEngine.ResetPlatforms();

        UnstickFromTiles();

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

                        _xPos = tiles[0].X + TunnelExitRightXOffset;
                        _yPos = tiles[0].Y + TunnelExitYOffset;
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

                    _xPos = tiles[0].X + TunnelExitLeftXOffset;
                    _yPos = tiles[0].Y + TunnelExitYOffset;
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
        switch (State)
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
    ///     The red wash showing energy being drained. It runs down while a nasty is touching Blob and
    ///     back up once that stops, so a glancing touch shows as a flash rather than a step change.
    /// </summary>

    /// <summary>Blob walking about, reddening while a nasty drains his energy.</summary>
    /// <remarks>
    ///     The tint stays white and the shader supplies the red. The shader is skipped entirely when
    ///     nothing is draining Blob, which is nearly all the time.
    /// </remarks>
    private void DrawBlobSprite(int screenFade)
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

        bool draining = _drain.Draining;

        blobSprites.Begin(BlendState.AlphaBlend, null, draining ? DrainShader(blobSprites) : null);
        blobSprites.DrawSprite(Frame,
            _xPos, _yPos,
            Colours.WhiteFadedTo(
                (byte)(_teleport.Level * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
        blobSprites.End();
    }

    /// <summary>
    ///     The shader that reddens Blob while being drained, pointed at the pose about to be drawn.
    ///     Flickers at <see cref="ElectricSettings.FlickerAt" />, the same rate as the node arcs and
    ///     the overload, because it is all the same electricity.
    /// </summary>
    private Effect DrainShader(SpriteCollection sprites)
    {
        Effect drain = _resources.GetEffect(Assets.Shaders.Overload);

        Vector4 spriteRect = sprites.SpriteRectIn01(Frame);

        drain.Parameters["SpriteRect"].SetValue(spriteRect);
        drain.Parameters["Drain"].SetValue(
            EnergyDrainSettings.DrainFrom(_drain.Level));
        drain.Parameters["DrainColour"].SetValue(EnergyDrainSettings.DrainColour);
        drain.Parameters["RimAmount"].SetValue(EnergyDrainSettings.CoronaAmount);
        drain.Parameters["Flicker"].SetValue(ElectricSettings.FlickerAt(_drain.Frame));

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
                    _shuttle.Frame, _xPos, _yPos + SpriteMetrics.SpriteHeight,
                    Colours.WhiteFadedTo((byte)screenFade));
                blobShuttleOriginal.End();
            }
            else
            {
                SpriteCollection blobShuttle = _resources.GetSpriteCollection(Assets.Graphics.BlobShuttle);
                blobShuttle.Begin(BlendState.AlphaBlend);
                blobShuttle.DrawSprite(0, _xPos,
                    _yPos + SpriteMetrics.SpriteHeight, Colours.WhiteFadedTo((byte)screenFade));
                blobShuttle.End();
            }
        }
    }

    private void DrawTeleportBeam()
    {
        if (_teleport.BeamPlaying)
        {
            _blobTeleporterBeam.Draw();
        }
    }


    private void DrawOverloadAlarmState(int screenFade)
    {
        if (_overload.Visible)
        {
            SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

            // White, because the shader supplies the heat. A tint on top would take the colour back
            // out again.
            blobSprites.Begin(BlendState.AlphaBlend, null,
                OverloadShader(blobSprites, "Charging"));
            blobSprites.DrawSprite(
                Frame, _xPos, _yPos,
                Colours.WhiteFadedTo(
                    (byte)(_teleport.Level * ((double)screenFade / ColourLevels.Full))), 1f, 0f);
            blobSprites.End();
        }

        if (_shuttle.Riding)
        {
            SpriteCollection blobShuttle = _resources.GetSpriteCollection(Assets.Graphics.BlobShuttle);
            blobShuttle.Begin(BlendState.AlphaBlend);
            blobShuttle.DrawSprite(0, _xPos,
                _yPos + SpriteMetrics.SpriteHeight, Colours.WhiteFadedTo((byte)screenFade));
            blobShuttle.End();
        }
    }

    /// <summary>
    ///     The beat before the burst: Blob swells and shakes while cracks open with light coming out
    ///     of them. The swell and shrink that follow last eleven logic frames between them, too quick
    ///     to read, so this is the part there is time to see.
    /// </summary>
    /// <remarks>
    ///     Drawn at the overload's own scale, which grows over these frames in step with the cracks.
    ///     Holding Blob still and then bursting reads as two separate events; the cracks widening as
    ///     Blob pulls apart is what makes it one.
    /// </remarks>
    private void DrawOverloadSplitting(int screenFade)
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

        Vector2 shake = OverloadSettings.ShakeAt(_overload.Frame, _overload.Split);

        blobSprites.Begin(BlendState.AlphaBlend, null, OverloadShader(blobSprites, "Splitting"));
        blobSprites.DrawSprite(
            Frame,
            _xPos + (int)shake.X,
            _yPos + (int)shake.Y,
            Colours.WhiteFadedTo((byte)screenFade), _overload.Scale, 0f);
        blobSprites.End();

        DrawOverloadBeams(screenFade, shake);
    }

    /// <summary>
    ///     The beams of charge whipping around Blob while coming apart, thrown further out as the
    ///     cracks widen.
    /// </summary>
    /// <remarks>
    ///     The node and gate arc artwork rotated about Blob, because the game already has a drawing
    ///     of electricity and this is electricity. They go through the same burning shader as the
    ///     nodes, giving the same white core and halo, and are drawn additively over Blob so a beam
    ///     passing in front lights him rather than disappearing behind.
    /// </remarks>
    private void DrawOverloadBeams(int screenFade, Vector2 shake)
    {
        SpriteCollection arc = _resources.GetSpriteCollection(Assets.Graphics.ElectricityNodeElectricity1);

        int centreX = _xPos + SpriteMetrics.HalfSpriteWidth + (int)shake.X;
        int centreY = _yPos + SpriteMetrics.HalfSpriteHeight + (int)shake.Y;

        arc.Begin(BlendState.Additive, null, ElectricSettings.PointAt(
            _resources.GetEffect(Assets.Shaders.Electric),
            arc.SpriteRectIn01(_overload.ArcFrame),
            arc.SpriteWidth,
            arc.SpriteHeight,
            _overload.Frame));

        for (int beam = 0; beam < OverloadSettings.BeamCount; beam++)
        {
            (Vector2 offset, float angle) = OverloadSettings.BeamAt(
                _overload.Frame, beam, _overload.Split);

            arc.DrawSprite(
                _overload.ArcFrame,
                centreX + offset.X - arc.SpriteWidth / 2f,
                centreY + offset.Y - arc.SpriteHeight / 2f,
                Colours.WhiteFadedTo((byte)screenFade),
                OverloadSettings.BeamScale,
                angle);
        }

        arc.End();
    }

    /// <summary>
    ///     The discharge. Five copies of Blob at rising scales, each a burning outline rather than a
    ///     ghost, coloured by distance: white-hot in the middle, cooling to electric blue at the
    ///     front. Five solid copies would read as five Blobs; five outlines read as a blast.
    /// </summary>
    private void DrawOverloadFlare()
    {
        SpriteCollection blobSprites = _resources.GetSpriteCollection(_graphicsSet.Names.BlobSprites);

        blobSprites.Begin(BlendState.Additive, null, OverloadShader(blobSprites, "Discharging"));

        int copy = 0;

        foreach (float scale in _overload.FlareScales())
        {
            // Each shell's colour rides in on the tint, which is per draw, so all five stay in one
            // batch instead of needing a shader setting each.
            Vector3 shell = OverloadSettings.ShellColour(copy, BlobOverload.FlareCopies);

            blobSprites.DrawSprite(Frame, _xPos, _yPos,
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
    ///     Blob at the start of a new game: standing still at full health, off the shuttle, back on
    ///     the square the ship came down on.
    /// </summary>
    public void Reset()
    {
        _drain.Reset();
        Frame = BlobWalkFrames.Resting;
        State = BlobState.Normal;

        // The start square belongs to the map, marked by the author, so it is read rather than
        // written down here.
        Point start = _gameScreens.BlobStartPosition;
        _xPos = start.X;
        _yPos = start.Y;

        TakeBlobOffTheShuttle();

        PerformPostChangeScreenActions();
    }

    /// <summary>
    ///     Puts the shuttle back on its pad and the pad back on every screen, so a new game does not
    ///     start with the shuttle where the last game left it.
    /// </summary>
    private void TakeBlobOffTheShuttle()
    {
        _shuttle.Reset();
        _gameScreens.ChangeShuttlePlatformState(false);
    }

    private void TeleporterBeam_ParticleEffectComplete()
    {
        _teleport.BeamFinished();
    }
}
