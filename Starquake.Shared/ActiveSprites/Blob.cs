// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharedLibs;
using StarquakeXNA.Animations;
using StarquakeXNA.Collisions;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Display;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;
using StarquakeXNA.Input;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA.ActiveSprites;

public class Blob : GameComponentBase
{

    public delegate void RaiseBlobAction(BlobAction action, object data);

    private const int BlobFireSpeed = 16;

    private const int BlobMaxItemsHeld = 4;

    private readonly JumpPlatforms _blobJumpPlatformEngine;
    private int _blobAlarmSoundCounter;
    private Animation _blobAnimFire1;
    private Animation _blobAnimFire2;

    private Animation _blobAnimLeft;
    private Animation _blobAnimRight;

    private bool _blobCanFire = true;
    private int _blobDistanceFallen;

    private int _blobEnergyDrainCounter;
    private int _blobFallHeight;

    private int _blobFallSpeed = 4;
    private Guid _blobFallsSoundId;
    private int _blobFireCollisionDirection;
    private int _blobFireDistance;

    private int _blobFireFade;
    private IPositionalTriggerParticleEffect _blobFireSparks;
    private IStandardParticleEffect _blobFireTrail;

    private int _blobFrameOnEntry;
    private bool _blobOnShuttle;
    private bool _blobOnShuttleCached;
    private bool _blobOnShuttleOnEntry;
    private bool _blobOnShuttlePad;
    private int _blobOverloadState;

    private float _blobScale = 1f;

    private Point _blobScreenEntryLocation;

    private Animation _blobShuttleAnim;
    private Direction _blobsLastAnimDirection = Direction.None;

    private IStandardParticleEffect _blobTeleporterBeam;
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

    private TileDetected _tileAboveBlob;
    private TileDetected _tileBelowBlob;
    private TileDetected _tileLeftBlob;
    private TileDetected _tileRightBlob;
    private List<ResetTile> _tilesToReset;

    public Blob(GameEngine gameEngineReference) : base(gameEngineReference)
    {
        _blobJumpPlatformEngine = new JumpPlatforms(GameEngine);
        BlobsControl = new CustomControl(gameEngineReference.PlayerKeys, GameEngine, true);
        _blobTouched = false;
        _teleportFadeLevel = 255;
        _startBlobTeleporting = false;
        BlobsState = BlobState.Normal;
    }

    public Collectables[] BlobInventory { get; private set; }

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

    public int BlobWidth => 64;

    public int BlobHeight { get; private set; } = 64;

    public int BlobFrame { get; set; } = 5;

    public int BlobHSpeed { get; set; } = 8;

    public int BlobVSpeed { get; set; } = 8;

    public int BlobFireX { get; private set; }

    public int BlobFireY { get; private set; }

    public bool BlobHasFired { get; private set; }

    public CustomControl BlobsControl { get; }

    public event RaiseScreenChanged OnScreenChanged;

    public event RaiseBlobAction OnBlobActionRaised;

    public void KillBlob(bool resetBlobToScreenEntryPoint, bool overloadFirst)
    {
        if (BlobsState != BlobState.Normal)
        {
            return;
        }

        _resetBlobToScreenEntryPoint = resetBlobToScreenEntryPoint;

        _blobOverloadState = overloadFirst ? 0 : 3;

        BlobsState = BlobState.Overloading;
        _blobAlarmSoundCounter = 0;
    }

    public void LoadContent(ContentList content)
    {
        _blobTeleporterBeam =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("BeamMeUp_Small",
                ParticleEffectType.Triggered, null) as IStandardParticleEffect;
        _blobFireTrail =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("FireTrail", ParticleEffectType.Continual,
                null) as IStandardParticleEffect;

        _blobFireSparks =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks",
                ParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

        if (_blobTeleporterBeam != null)
        {
            _blobTeleporterBeam.ParticleEffectComplete += blobTeleporterBeam_ParticleEffectComplete;
        }
    }

    public void Initialise()
    {
        // Fire location starts same as Blob's screen location
        BlobFireX = _blobXPos + 16;
        BlobFireY = _blobYPos + 16;
        _blobFireFade = 0;

        _blobShuttleAnim = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 3,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = true
        };

        _blobAnimLeft = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 3,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = false
        };

        _blobAnimRight = new Animation
        {
            FirstFrame = 7,
            CurrentFrame = 7,
            LastFrame = 10,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = true
        };

        _blobAnimFire1 = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 3,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = true
        };

        _blobAnimFire2 = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 23,
            Yoyo = false,
            Delay = 1,
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

        _tilesToReset = [];
    }

    // Blob has hit something
    public void RegisterFireHit()
    {
        BlobHasFired = false;
        _blobCanFire = true;
    }

    public void TriggerBlobTeleportedAnimation()
    {
        _blobTeleporterBeam?.Start();
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
                                    BlobHeight = 96;
                                    _blobOnShuttle = true;
                                }

                                break;
                            }
                            case (int)ActionTiles.RemovedShuttlePlatform:
                            {
                                if (BlobsControl.MovedLeft() || BlobsControl.MovedRight())
                                {
                                    GameEngine.GameScreens.ChangeShuttlePlatformState(false);
                                    BlobHeight = 64;
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
                        _blobAnimLeft.CurrentFrame = BlobFrame > 6 ? 6 : BlobFrame;
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
                        _blobAnimRight.CurrentFrame = BlobFrame < 4 ? 4 : BlobFrame;
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
                                    GameEngine.GameScreens.CheckCollision(Direction.Up, _blobXPos, _blobYPos - 32,
                                        BlobWidth, BlobHeight, 1, true) != 0
                                   )
                                {
                                    _blobYPos = _blobYPos - 32;
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
                                    64,
                                    64),
                                new Rectangle(
                                    GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid.XLocation,
                                    GameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid.YLocation,
                                    64,
                                    64)))
                        {
                            accessingCheops = true;
                        }
                    }

                    // Ensure Blob isn't going through a force field
                    var inAccessForceField = false;
                    var forceFieldTile = GameEngine.GameScreens.CheckTileByObject(Direction.None, _blobXPos,
                        _blobYPos, BlobWidth, BlobHeight, 0);

                    if (forceFieldTile is { TileNumber: 419 })
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
                                InteractiveItem interactiveItemDropped = null;

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
                                                GameEngine.GameScreens.CurrentGameScreenData.PickUps[a].XLocation + 16,
                                                GameEngine.GameScreens.CurrentGameScreenData.PickUps[a].YLocation + 32,
                                                32,
                                                32))
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

                            if (_blobDistanceFallen > 12 && _blobFallSpeed < 12)
                            {
                                _blobFallSpeed++;
                                _blobDistanceFallen = 0;
                            }
                        }
                        else
                        {
                            _blobTotalDistanceFallen = 0;
                            _blobDistanceFallen = 0;
                            _blobFallSpeed = 4;
                            _blobFallHeight = 0;
                        }

                        // Fall - arrrgh!!!
                        var fallDistance = GameEngine.GameScreens.CheckCollision(Direction.Down, _blobXPos, _blobYPos,
                            BlobWidth, BlobHeight, _blobFallSpeed, true);

                        if (fallDistance > 0 && _blobTotalDistanceFallen > 64)
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

                            BlobFireX = _blobXPos + 16;
                            BlobFireY = _blobYPos + 16;
                            _blobFireFade = 0;

                            _blobCanFire = false;
                            BlobHasFired = true;
                            _blobFireCollisionDirection = 0;

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

                            if (_blobFireCollisionDirection == 0)
                            {
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }

                            // have we hit game scenary
                            if (_blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveY == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(1, 3);

                                        if (_blobFireSparks != null)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
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
                                if (_blobFireCollisionDirection == 0)
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
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                        }
                                    }
                                }
                            }
                            else if (_blobFireCollisionDirection > 0)
                            {
                                if (BlobFireY + 32 > GameEngine.GameScreens.ScreenHeight)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                if (_blobFireCollisionDirection == 1)
                                {
                                    if (BlobFireX < 0)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == 2)
                                {
                                    if (BlobFireX + 32 > GameEngine.GameScreens.ScreenWidth)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireY = BlobFireY + amountToMoveY;

                                    if (_blobFireCollisionDirection == 1)
                                    {
                                        BlobFireX = BlobFireX - amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                    else if (_blobFireCollisionDirection == 2)
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
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Down)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == 0)
                            {
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }

                            // have we hit game scenary
                            if (_blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveY == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(1, 3);

                                        if (_blobFireSparks != null)
                                        {
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (BlobFireY + 32 > GameEngine.GameScreens.ScreenHeight)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (_blobFireCollisionDirection == 0)
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
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
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

                                if (_blobFireCollisionDirection == 1)
                                {
                                    if (BlobFireX < 0)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == 2)
                                {
                                    if (BlobFireY + 32 > GameEngine.GameScreens.ScreenWidth)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireY = BlobFireY - amountToMoveY;

                                    if (_blobFireCollisionDirection == 1)
                                    {
                                        BlobFireX = BlobFireX - amountToMoveX;
                                        _blobFireDistance = _blobFireDistance + amountToMoveX;
                                    }
                                    else if (_blobFireCollisionDirection == 2)
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
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Left)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == 0)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
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
                            if (_blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveX == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(1, 3);

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
                                if (_blobFireCollisionDirection == 0)
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
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                        }
                                    }
                                }
                            }
                            else if (_blobFireCollisionDirection > 0)
                            {
                                if (BlobFireX + 32 > GameEngine.GameScreens.ScreenWidth)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                if (_blobFireCollisionDirection == 1)
                                {
                                    if (BlobFireY < 0)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == 2)
                                {
                                    if (BlobFireY + 32 > GameEngine.GameScreens.ScreenHeight)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireX = BlobFireX + amountToMoveX;
                                    _blobFireDistance = _blobFireDistance + amountToMoveX;

                                    if (_blobFireCollisionDirection == 1)
                                    {
                                        BlobFireY = BlobFireY - amountToMoveY;
                                    }
                                    else if (_blobFireCollisionDirection == 2)
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
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Right)
                        {
                            var amountToMoveY = 0;
                            var amountToMoveX = 0;

                            if (_blobFireCollisionDirection == 0)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                            }
                            else if (_blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
                                amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, BlobFireX,
                                    BlobFireY, 2, 32, BlobFireSpeed, false);
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
                            if (_blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveX == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (_blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        _blobFireCollisionDirection = GameEngine.RandomNumberGenerator.Next(1, 3);

                                        _blobFireSparks?.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (BlobFireX + 32 > GameEngine.GameScreens.ScreenWidth)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (_blobFireCollisionDirection == 0)
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
                                            _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
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

                                if (_blobFireCollisionDirection == 1)
                                {
                                    if (BlobFireY < 0)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (_blobFireCollisionDirection == 2)
                                {
                                    if (BlobFireY + 32 > GameEngine.GameScreens.ScreenHeight)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    BlobFireX -= amountToMoveX;
                                    _blobFireDistance += amountToMoveX;

                                    if (_blobFireCollisionDirection == 1)
                                    {
                                        BlobFireY -= amountToMoveY;
                                    }
                                    else if (_blobFireCollisionDirection == 2)
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
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
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
                                BlobFireY, 2, 32, BlobFireSpeed, false);

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
                                    _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                }
                            }
                        }
                        else if (_directionBlobFired == Direction.Right)
                        {
                            var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Right, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);

                            if (CheckForBlobCollisionWithObject(
                                    new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                            {
                                if (_blobFireDistance < offset + BlobWidth / 2)
                                {
                                    amountToMove = BlobFireSpeed;
                                }
                            }

                            // Have we crossed the screen boundary
                            if (BlobFireX + 32 > GameEngine.GameScreens.ScreenWidth)
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
                                    _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
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
                    if (GameEngine.GameScreens.ScreenNumber + 1 <= 512)
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
                    if (GameEngine.GameScreens.ScreenNumber - 16 >= 0)
                    {
                        GameEngine.GameScreens.ScreenNumber -= 16;
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
                    if (GameEngine.GameScreens.ScreenNumber + 16 <= 512)
                    {
                        GameEngine.GameScreens.ScreenNumber += 16;
                        _blobYPos = 0;
                        _blobDistanceFallen = 0;
                        _blobFallSpeed = 4;

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
                        BlobWidth, BlobHeight - 32, BlobVSpeed); // Subtract the shuttle height off
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
                    if (_tileRightBlob.TileNumber == 397)
                    {
                        if (_tileRightBlob.XPosition >= 24 &&
                            _tileRightBlob.YPosition == 32)
                        {
                            GameEngine.CurrentGameScreen = GameScreenType.Teleporter;

                            GameEngine.GetGameScreen(GameScreenType.Teleporter).Reset(null);
                            _blobXPos = _tileRightBlob.MapXPosition * 128 + 20;
                            BlobFrame = 7;
                        }
                    }
                }

                // Detect an access booth
                if (_tileLeftBlob is { TileNumber: 400 })
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

                if (_tileRightBlob is { TileNumber: 399 })
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
                if (_tileLeftBlob?.TileNumber is 420 or 422 or 424 or 426 or 428 or 430 or 432 or 434 or 436 or 438)
                {
                    if (_tileLeftBlob.XPosition <= 32 && _tileLeftBlob.YPosition == 32)
                    {
                        GameEngine.GameScreens.ScreenNumber--;
                        _directionGoingThroughTunnel = Direction.Left;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            GameEngine.ScreenEffctEngine.ExecuteScreenEffect(ScreenEffectType.LeftTunnelEntry, 80,
                                -1, true);
                            GameEngine.ContentResourceManager.GetSoundFxEngine()
                                .PlaySound("TunnelSwish1", 0.8f, false);
                            OnScreenChanged(GameScreenType.None);
                        }
                    }
                }

                // Detect a righthand tunnel
                if (_tileRightBlob?.TileNumber is 421 or 423 or 425 or 427 or 429 or 431 or 433 or 435 or 437 or 439)
                {
                    if (_tileRightBlob.XPosition >= 32 && _tileRightBlob.YPosition == 32)
                    {
                        GameEngine.GameScreens.ScreenNumber++;
                        _directionGoingThroughTunnel = Direction.Right;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            GameEngine.ScreenEffctEngine.ExecuteScreenEffect(ScreenEffectType.RightTunnelEntry,
                                80, -1, true);
                            GameEngine.ContentResourceManager.GetSoundFxEngine()
                                .PlaySound("TunnelSwish2", 0.8f, false);
                            OnScreenChanged(GameScreenType.None);
                        }
                    }
                }

                // Breakable bridges
                if (_tileBelowBlob is { TileNumber: 410 })
                {
                    if (_blobFallHeight >= 128)
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
                        case 409 or 412 or 408 or 413:
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
                        case 407 or 414 or 415:
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
                    if (_tileAboveBlob?.TileNumber is 409 or 412 or 413)
                    {
                        if (_tileAboveBlob.YPosition == 96)
                        {
                            if (BlobsControl.MovedLeft() && GameEngine.GameScreens.CheckCollision(Direction.Left,
                                    _blobXPos, _blobYPos, BlobWidth, BlobHeight, BlobHSpeed, true) > 0)
                            {
                                if (_distanceInTunnelLift > 8)
                                {
                                    _inTunnelLift = false;
                                    moveBlob = false;
                                }
                            }
                            else if (BlobsControl.MovedRight() &&
                                     GameEngine.GameScreens.CheckCollision(Direction.Right, _blobXPos, _blobYPos,
                                         BlobWidth, BlobHeight, BlobHSpeed, true) > 0)
                            {
                                if (_distanceInTunnelLift > 8)
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
                        _blobYPos -= 8;
                        _distanceInTunnelLift += 8;
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
                    if (tileIntersectingBlob.TileNumber == 405)
                    {
                        if (tileIntersectingBlob.XPosition is > -16 and < 64 &&
                            tileIntersectingBlob.YPosition > -8)
                        {
                            KillBlob(true, false);
                        }
                    }

                    // Right side mounted spikey tiles
                    if (tileIntersectingBlob.TileNumber == 404)
                    {
                        if (tileIntersectingBlob.YPosition is < 64 and > -32 &&
                            tileIntersectingBlob.XPosition > 16)
                        {
                            KillBlob(true, false);
                        }
                    }

                    // Broken bottle spiky tiles
                    if (tileIntersectingBlob.TileNumber == 416)
                    {
                        if (tileIntersectingBlob.XPosition is > -16 and < 64 &&
                            tileIntersectingBlob.YPosition > -16)
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
                            32,
                            32))
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
                        _blobFallSpeed = 4;

                        if (_blobOnShuttle)
                            BlobHeight = 96;
                        else
                            BlobHeight = 64;
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
        _blobTeleporterBeam?.Update(_blobXPos + 30, _blobYPos + 50, gameTime);
        _blobFireTrail?.Update(BlobFireX + 14, BlobFireY + 14, gameTime);

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
        if (GameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, BlobWidth, 8, true) >= 0)
        {
            // And not stuck at the bottom?
            if (GameEngine.GameScreens.CheckRectangles(
                    _blobXPos,
                    _blobYPos + (BlobHeight - 8),
                    BlobWidth,
                    8,
                    true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                var distanceToMoveY = 0;
                while (GameEngine.GameScreens.CheckRectangles(
                           _blobXPos,
                           _blobYPos + distanceToMoveY,
                           BlobWidth,
                           8,
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
                _blobYPos + (BlobHeight - 8),
                BlobWidth,
                8, true) >= 0) // Is Blob stuck at the bottom
        {
            // And not stuck at the top?
            if (GameEngine.GameScreens.CheckRectangles(
                    _blobXPos,
                    _blobYPos,
                    BlobWidth,
                    8,
                    true) < 0)
            {
                // He's stuck at the bottom so lets figure out how far we need to go to release him
                var distanceToMoveY = 0;
                while (GameEngine.GameScreens.CheckRectangles(
                           _blobXPos,
                           _blobYPos + (BlobHeight - 8) - distanceToMoveY, BlobWidth, 8, true) >= 0)
                {
                    distanceToMoveY++;
                }

                // Shift Blob to a safe position
                _blobYPos -= distanceToMoveY;
            }
        }

        if (GameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, 8, BlobHeight, true) >=
            0) // Is Blob stuck at the left
        {
            // And not stuck at the right?
            if (GameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - 8), _blobYPos, 8, BlobHeight, true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                var distanceToMoveX = 0;
                while (GameEngine.GameScreens.CheckRectangles(_blobXPos + distanceToMoveX, _blobYPos, 8, BlobHeight,
                           true) >= 0)
                {
                    distanceToMoveX++;
                }

                // Shift Blob to a safe position
                _blobXPos += distanceToMoveX;
            }
        }

        if (GameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - 8), _blobYPos, 8, BlobHeight, true) >=
            0) // Is Blob stuck at the right
        {
            // And not stuck at the left?
            if (GameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, 8, BlobHeight, true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                var distanceToMoveX = 0;
                while (GameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - 8) - distanceToMoveX,
                           _blobYPos, 8, BlobHeight, true) >= 0)
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
        _blobFireCollisionDirection = 0;

        BlobFireX = _blobXPos + 16;
        BlobFireY = _blobYPos + 16;
        _blobFireFade = 0;

        _blobJumpPlatformEngine.ResetPlatforms();

        // Unstick Blob from the screenmap if he's got stuck due to a tile map change
        UnstickBlobFromTiles();

        if (_directionGoingThroughTunnel != Direction.None)
        {
            if (_directionGoingThroughTunnel == Direction.Left)
            {
                for (var a = 421; a < 441; a += 2)
                {
                    var tiles = GameEngine.GameScreens.GetCoordsOfTiles(a);

                    if (tiles is not { Count: 1 })
                    {
                        continue;
                    }

                    _blobXPos = tiles[0].X + 24;
                    _blobYPos = tiles[0].Y + 32;
                    break;
                }
            }
            else if (_directionGoingThroughTunnel == Direction.Right)
            {
                for (var a = 420; a < 442; a += 2)
                {
                    var tiles = GameEngine.GameScreens.GetCoordsOfTiles(a);

                    if (tiles is not { Count: 1 })
                    {
                        continue;
                    }

                    _blobXPos = tiles[0].X + 40;
                    _blobYPos = tiles[0].Y + 32;
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
                        _blobEnergyDrainCounter = 255;
                        _blobTouched = true;
                    }

                    if (_blobEnergyDrainCounter > 0)
                        _blobEnergyDrainCounter -= 16;

                    if (_blobEnergyDrainCounter < 0)
                        _blobEnergyDrainCounter = 0;
                }
                else
                {
                    if (_blobEnergyDrainCounter < 255)
                        _blobEnergyDrainCounter += 16;

                    if (_blobEnergyDrainCounter > 255)
                    {
                        _blobEnergyDrainCounter = 255;
                    }

                    _blobTouched = false;
                }

                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName)
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).DrawSprite(BlobFrame,
                    _blobXPos, _blobYPos,
                    ColourHelper.GetXna4Colour(255, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                        (byte)(_teleportFadeLevel * ((double)screenFade / 255))), 1f, 0f);
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).End();

                // Draw other bits
                if (_blobOnShuttle)
                {
                    if (GameEngine.UsingOldGraphics)
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal")
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").DrawSprite(
                            _blobShuttleAnim.CurrentFrame, _blobXPos, _blobYPos + 64,
                            ColourHelper.GetXna4Colour(255, 255, 255, (byte)screenFade));
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").End();
                    }
                    else
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle")
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, _blobXPos,
                            _blobYPos + 64, ColourHelper.GetXna4Colour(255, 255, 255, (byte)screenFade));
                        GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
                    }
                }

                _blobJumpPlatformEngine.Draw(gameTime);

                if (BlobHasFired)
                {
                    if (_blobFireFade < 255)
                    {
                        _blobFireFade = _blobFireFade + 16;
                    }

                    if (_blobFireFade > 255)
                    {
                        _blobFireFade = 255;
                    }

                    if (GameEngine.UsingOldGraphics)
                    {
                        if (_blobOnShuttle)
                        {
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                                .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                                _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY,
                                ColourHelper.GetXna4Colour(255, 255, 255,
                                    (byte)(255 * ((double)screenFade / 255))), 1.0f, 0f, true,
                                GameEngine.Paused);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                        }
                        else
                        {
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1")
                                .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").DrawSprite(
                                _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY - 16,
                                ColourHelper.GetXna4Colour(255, 255, 255,
                                    (byte)(255 * ((double)screenFade / 255))), 1.0f, 0f, true,
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
                                ColourHelper.GetXna4Colour(255, 255, 255,
                                    (byte)(_blobFireFade * ((double)screenFade / 255))), 1.0f, 10f,
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

                if (_teleportFadeLevel < 255)
                {
                    _teleportFadeLevel += 8;
                }

                if (_teleportFadeLevel > 255)
                {
                    _teleportFadeLevel = 255;
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
                        ColourHelper.GetXna4Colour(255, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                            (byte)(_teleportFadeLevel * ((double)screenFade / 255))), 1f, 0f);
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.BlobSpritesName).End();
                }

                // Draw other bits
                if (_blobOnShuttle)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, _blobXPos,
                        _blobYPos + 64, ColourHelper.GetXna4Colour(255, 255, 255, (byte)screenFade));
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
                                ColourHelper.GetXna4Colour(255, 255, 255, 150), temp, 0f);
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
        BlobHeight = 64;
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

        return temp.Count > 0 ? temp : null;
    }

    private void blobTeleporterBeam_ParticleEffectComplete()
    {
        _startBlobTeleporting = false;
    }

    private bool CheckForBlobCollisionWithObject(Rectangle blobsPosition, out int offset)
    {
        offset = 0;

        var screenItems = GetCollisionObjects();
        if (screenItems == null)
        {
            return false;
        }

        foreach (var t in screenItems)
        {
            if (!CollisionDetection.Check(blobsPosition, new Rectangle(t.XLocation, t.YLocation, 64, 64)))
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