using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharedLibs;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class Blob : GameComponentBase
{
    private int _blobXPos;
    public int BlobXPos
    {
        get => _blobXPos;
        set
        {
            _blobXPos = value;
            _blobScreenEntryLocation.X = value;
        }
    }

    private int _blobYPos;
    public int BlobYPos
    {
        get => _blobYPos;
        set
        {
            _blobYPos = value;
            _blobScreenEntryLocation.Y = value;
        }
    }

    public bool BlobTouchedByNasty { get; set; }

    public int BlobWidth => 64;

    public int BlobHeight { get; private set; } = 64;

    public int BlobFrame { get; set; } = 5;

    private Guid _blobFallsSoundId;

    public int BlobHSpeed { get; set; } = 8;

    public int BlobVSpeed { get; set; } = 8;

    private int _blobFallSpeed = 4;
    private int _blobDistanceFallen;
    private int _blobTotalDistanceFallen;
    private int _blobFallHeight;
    private int _blobOverloadState;
    private int _blobAlarmSoundCounter;

    private float _blobScale = 1f;

    public int BlobFireX { get; set; }

    public int BlobFireY { get; set; }

    private int _blobFireFade;
    private const int BlobFireSpeed = 16;
    private int _blobFireCollisionDirection;
    private int _blobFireDistance;

    private bool _jumpPressed;
    private enDirection _directionGoingThroughTunnel = enDirection.None;
    private bool _inTunnelLift;
    private int _distanceInTunnelLift;

    public bool BlobHasFired { get; private set; }

    private bool _blobCanFire = true;
    private bool _blobOnShuttlePad;
    private bool _blobOnShuttleCached;
    private bool _blobOnShuttle;

    private Point _blobScreenEntryLocation;
    private bool _blobOnShuttleOnEntry;

    private int _blobFrameOnEntry;
    private bool _resetBlobToScreenEntryPoint;

    private int _blobEnergyDrainCounter;
    private bool _blobTouched;

    public enBlobState BlobsState { get; set; }

    private const int BlobMaxItemsHeld = 4;
    private bool _inventoryAccessed;

    public enCollectables[] BlobInventory { get; private set; }

    private TileDetected _tileAboveBlob;
    private TileDetected _tileBelowBlob;
    private TileDetected _tileLeftBlob;
    private TileDetected _tileRightBlob;

    private enDirection DirectionBlobMoved { get; set; } = enDirection.None;

    private enDirection _directionBlobFired = enDirection.None;
    private enDirection _blobsLastAnimDirection = enDirection.None;

    private Animation _blobAnimLeft;
    private Animation _blobAnimRight;
    private Animation _blobAnimFire2;
    private Animation _blobAnimFire1;

    private Animation _blobShuttleAnim;
    private List<ResetTile> _tilesToReset;

    public CustomControl BlobsControl { get; }

    private IStandardParticleEffect _blobTeleporterBeam;
    private IStandardParticleEffect _blobFireTrail;
    private IPositionalTriggerParticleEffect _blobFireSparks;

    private readonly JumpPlatforms _blobJumpPlatformEngine;

    private Guid _blobTunnelLiftSoundId = Guid.Empty;

    public event RaiseScreenChanged OnScreenChanged;

    public delegate void RaiseBlobAction(enBlobAction Action, object Data);

    public event RaiseBlobAction OnBlobActionRaised;

    public Blob(GameEngine GameEngineReference) : base(GameEngineReference)
    {
        _blobJumpPlatformEngine = new JumpPlatforms(gameEngine);
        BlobsControl = new CustomControl(GameEngineReference.PlayerKeys, gameEngine, true);
        _blobTouched = false;
        _teleportFadeLevel = 255;
        _startBlobTeleporting = false;
        BlobsState = enBlobState.Normal;
    }

    public void KillBlob(bool ResetBlobToScreenEntryPoint, bool OverloadFirst)
    {
        if (BlobsState == enBlobState.Normal)
        {
            _resetBlobToScreenEntryPoint = ResetBlobToScreenEntryPoint;

            if (OverloadFirst)
            {
                _blobOverloadState = 0;
            }
            else
            {
                _blobOverloadState = 3;
            }

            BlobsState = enBlobState.Overloading;
            _blobAlarmSoundCounter = 0;
        }
    }

    public void LoadContent(ContentList Content)
    {
        _blobTeleporterBeam =
            gameEngine.ContentResourceManager.GetParticleEffectInstance("BeamMeUp_Small",
                enParticleEffectType.Triggered, null) as IStandardParticleEffect;
        _blobFireTrail =
            gameEngine.ContentResourceManager.GetParticleEffectInstance("FireTrail", enParticleEffectType.Continual,
                null) as IStandardParticleEffect;
        //blobFireSparks = gameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks", enParticleEffectType.Triggered, null);
        _blobFireSparks =
            gameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks",
                enParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

        if (_blobTeleporterBeam != null)
        {
            _blobTeleporterBeam.ParticleEffectComplete += blobTeleporterBeam_ParticleEffectComplete;
        }
    }

    void blobTeleporterBeam_ParticleEffectComplete()
    {
        //blobCanMove = true;
        _startBlobTeleporting = false;
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
        _directionBlobFired = enDirection.Right;

        BlobInventory = new enCollectables[BlobMaxItemsHeld];

        // Clear inventory items
        for (var a = 0; a < BlobInventory.Length; a++)
        {
            BlobInventory[a] = enCollectables.None;
        }

        // Temporary test code to give Blob Accesscard and Key
        BlobInventory[1] = enCollectables.Key;
        BlobInventory[0] = enCollectables.AccessCard;

        _tilesToReset = [];
    }

    // Blob has hit something
    public void RegisterFireHit()
    {
        BlobHasFired = false;
        _blobCanFire = true;
    }

    private int _teleportFadeLevel;
    private bool _startBlobTeleporting;

    public void TriggerBlobTeleportedAnimation()
    {
        if (_blobTeleporterBeam != null)
        {
            _blobTeleporterBeam.Start();
        }

        _startBlobTeleporting = true;
        _teleportFadeLevel = 0;
    }

    public override void Update(GameTime gameTime)
    {
        var changedScreens = false;

        if (BlobsState == enBlobState.Normal)
        {
            // Do something about being on something etc.
            if (_blobOnShuttlePad)
            {
                if (_tileBelowBlob != null)
                {
                    // Change the map to reflect shuttle has been taken
                    if (_tileBelowBlob.TileNumber == (int)enActionTiles.ShuttlePlatform)
                    {
                        if (BlobsControl.MovedUp())
                        {
                            gameEngine.GameScreens.ChangeShuttlePlatformState(true);
                            BlobHeight = 96;
                            _blobOnShuttle = true;
                        }
                    }
                    else if (_tileBelowBlob.TileNumber == (int)enActionTiles.RemovedShuttlePlatform)
                    {
                        if (BlobsControl.MovedLeft() || BlobsControl.MovedRight())
                        {
                            gameEngine.GameScreens.ChangeShuttlePlatformState(false);
                            BlobHeight = 64;
                            _blobOnShuttle = false;
                        }
                    }

                    // Update the collision map for the current screen
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                }
            }

            if (BlobsControl.MovedLeft())
            {
                if (_blobsLastAnimDirection == enDirection.Right)
                {
                    if (BlobFrame > 6)
                    {
                        _blobAnimLeft.CurrentFrame = 6;
                    }
                    else
                    {
                        _blobAnimLeft.CurrentFrame = BlobFrame;
                    }
                }
                else
                {
                    _blobAnimLeft.GetNextFrame();
                }

                var amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Left, _blobXPos, _blobYPos,
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
                DirectionBlobMoved = enDirection.Left;
                _blobsLastAnimDirection = enDirection.Left;

                if (!_blobOnShuttle && _blobFallHeight == 0)
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine()
                        .PlaySound("BlobRun", true);
                }
            }

            if (BlobsControl.MovedRight())
            {
                if (_blobsLastAnimDirection == enDirection.Left)
                {
                    if (BlobFrame < 4)
                    {
                        _blobAnimRight.CurrentFrame = 4;
                    }
                    else
                    {
                        _blobAnimRight.CurrentFrame = BlobFrame;
                    }
                }
                else
                {
                    _blobAnimRight.GetNextFrame();
                }

                var amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Right, _blobXPos, _blobYPos,
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
                DirectionBlobMoved = enDirection.Right;
                _blobsLastAnimDirection = enDirection.Right;

                if (!_blobOnShuttle && _blobFallHeight == 0)
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobRun", true);
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
                    _blobYPos = _blobYPos - gameEngine.GameScreens.CheckCollision(enDirection.Up, _blobXPos,
                        _blobYPos,
                        BlobWidth, BlobHeight, BlobVSpeed, true);
                    DirectionBlobMoved = enDirection.Up;
                }

                if (BlobsControl.MovedDown())
                {
                    _blobYPos = _blobYPos + gameEngine.GameScreens.CheckCollision(enDirection.Down, _blobXPos,
                        _blobYPos, BlobWidth, BlobHeight, BlobVSpeed, true);
                    DirectionBlobMoved = enDirection.Down;
                }
            }
            else
            {
                // Create a standing platform
                if (BlobsControl.MovedDown() &&
                    !BlobsControl.MovedLeft() &&
                    !BlobsControl.MovedRight())
                {
                    if (_jumpPressed == false)
                    {
                        _jumpPressed = true;

                        if (gameEngine.BlobStatusPanel.Platforms > 0)
                        {
                            if (_blobOnShuttlePad == false)
                            {
                                int temp = _blobJumpPlatformEngine.GetNewJumpPlatform(_blobXPos, _blobYPos,
                                    BlobWidth,
                                    BlobHeight, true);

                                if (temp == -1)
                                {
                                }

                                // If -1 is returned from this routine, the collision check failed
                                if (temp != -1000)
                                {
                                    _blobYPos = temp;

                                    gameEngine.ContentResourceManager.GetSoundFXEngine()
                                        .PlaySound("BlobJumps", 0.3f, false);

                                    if (OnBlobActionRaised != null)
                                        OnBlobActionRaised(enBlobAction.LaidPlatform, null);
                                }
                            }
                        }
                        else // just jump aimlessly in the air
                        {
                            // Check that we are on the ground first.... and above blob's head
                            if (gameEngine.GameScreens.CheckCollision(enDirection.Down, _blobXPos, _blobYPos,
                                    BlobWidth, BlobHeight, 1, true) == 0 &&
                                gameEngine.GameScreens.CheckCollision(enDirection.Up, _blobXPos, _blobYPos - 32,
                                    BlobWidth, BlobHeight, 1, true) != 0
                               )
                            {
                                _blobYPos = _blobYPos - 32;
                                gameEngine.ContentResourceManager.GetSoundFXEngine()
                                    .PlaySound("BlobJumpsNoPlatforms", 1f, false);
                            }
                        }
                    }
                }

                bool accessingCheops = false;

                if (gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid != null)
                {
                    if (CollisionDetection.Check(
                            new Rectangle(
                                _blobXPos,
                                _blobYPos,
                                64,
                                64),
                            new Rectangle(
                                gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid.XLocation,
                                gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid.YLocation,
                                64,
                                64)))
                    {
                        accessingCheops = true;
                    }
                }

                // Ensure Blob isn't going through a force field
                bool inAccessForceField = false;
                TileDetected forceFieldTile = gameEngine.GameScreens.CheckTileByObject(enDirection.None, _blobXPos,
                    _blobYPos, BlobWidth, BlobHeight, 0);

                if (forceFieldTile != null && forceFieldTile.TileNumber == 419)
                {
                    inAccessForceField = true;
                }

                // Pick up an object or drop one perhaps.....
                if (BlobsControl.MovedUp() &&
                    !BlobsControl.MovedLeft() &&
                    !BlobsControl.MovedRight())
                {
                    if (_inventoryAccessed == false &&
                        !_inTunnelLift &&
                        !inAccessForceField) // Ensure Blob isn't going through a force field
                    {
                        if (accessingCheops)
                        {
                            // Do something with the pyramid.....
                            gameEngine.GetGameScreen(enGameScreen.Cheops).Reset(null);
                            gameEngine.CurrentGameScreen = enGameScreen.Cheops;
                        }
                        else
                        {
                            gameEngine.ContentResourceManager.GetSoundFXEngine()
                                .PlaySound("BlobPickUp", 0.3f, false);

                            _inventoryAccessed = true;

                            // Collect powerups
                            InteractiveItem interactiveItemDropped = null;

                            // Pull out the last item and put it on the screen
                            if (BlobInventory[BlobInventory.Length - 1] != enCollectables.None)
                            {
                                //gameEngine.GameScreens.C
                                int itemXPos = (_blobXPos / gameEngine.GameScreens.CollAreaWidth) *
                                               gameEngine.GameScreens.CollAreaWidth;
                                int itemYPos = (_blobYPos / gameEngine.GameScreens.CollAreaHeight) *
                                               gameEngine.GameScreens.CollAreaHeight;

                                interactiveItemDropped = new InteractiveItem(itemXPos, itemYPos,
                                    (int)BlobInventory[BlobInventory.Length - 1]);

                                gameEngine.GameScreens.CurrentGameScreenData.PickUps.Add(interactiveItemDropped);

                                // Nothing held here now
                                BlobInventory[BlobInventory.Length - 1] = enCollectables.None;
                            }

                            // Move everything along one in Blob's inventory
                            for (int b = BlobInventory.Length - 1; b > 0; b--)
                            {
                                BlobInventory[b] = BlobInventory[b - 1];
                                BlobInventory[b - 1] = enCollectables.None;
                            }

                            // Pick up an item if we can....
                            if (gameEngine.GameScreens.CurrentGameScreenData.PickUps != null)
                            {
                                for (int a = 0; a < gameEngine.GameScreens.CurrentGameScreenData.PickUps.Count; a++)
                                {
                                    if (CollisionDetection.Check(
                                            _blobXPos,
                                            BlobYPos,
                                            BlobWidth,
                                            BlobHeight,
                                            gameEngine.GameScreens.CurrentGameScreenData.PickUps[a].XLocation + 16,
                                            gameEngine.GameScreens.CurrentGameScreenData.PickUps[a].YLocation + 32,
                                            32,
                                            32))
                                    {
                                        // Make sure it's not what we just dropped....
                                        bool pickupItem = true;

                                        if (interactiveItemDropped != null &&
                                            interactiveItemDropped ==
                                            gameEngine.GameScreens.CurrentGameScreenData.PickUps[a])
                                        {
                                            pickupItem = false;
                                        }

                                        if (pickupItem)
                                        {
                                            // Add the item to Blob's inventory
                                            BlobInventory[0] = (enCollectables)gameEngine.GameScreens
                                                .CurrentGameScreenData.PickUps[a].Type;

                                            // Remove it from the screen
                                            gameEngine.GameScreens.CurrentGameScreenData.PickUps.Remove(
                                                gameEngine.GameScreens.CurrentGameScreenData.PickUps[a]);
                                            break;
                                        }
                                    }
                                }
                            }

                            gameEngine.GameScreens.UpdateCollisionMap(false);
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
                    int falling = gameEngine.GameScreens.CheckCollision(enDirection.Down, _blobXPos, _blobYPos,
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
                    int fallDistance = gameEngine.GameScreens.CheckCollision(enDirection.Down, _blobXPos, _blobYPos,
                        BlobWidth, BlobHeight, _blobFallSpeed, true);

                    if (fallDistance > 0 && _blobTotalDistanceFallen > 64)
                    {
                        _blobFallsSoundId = gameEngine.ContentResourceManager.GetSoundFXEngine()
                            .PlaySound("BlobFalls", true);
                    }

                    if (falling == 0)
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine()
                            .StopSound(_blobFallsSoundId, SoundFade.Fast);
                    }

                    // Check to see if Blob can potentially enter a teleport booth, tunnel etc.
                    if (BlobsControl.MovedRight())
                    {
                        for (int a = 0; a < fallDistance; a++)
                        {
                            if (gameEngine.GameScreens.CheckCollision(enDirection.Down, _blobXPos + BlobWidth,
                                    _blobYPos + a, BlobWidth + 1, BlobHeight, 1, true) == 0)
                            {
                                if (gameEngine.GameScreens.CheckCollision(enDirection.Right, _blobXPos + BlobWidth,
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
                        for (int a = 0; a < fallDistance; a++)
                        {
                            if (gameEngine.GameScreens.CheckCollision(enDirection.Down, _blobXPos - BlobWidth,
                                    _blobYPos + a, BlobWidth + 1, BlobHeight, 1, true) == 0)
                            {
                                if (gameEngine.GameScreens.CheckCollision(enDirection.Right, _blobXPos - BlobWidth,
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
                if (gameEngine.BlobStatusPanel.FirePower > 0)
                {
                    if (_blobCanFire)
                    {
                        // Play sound
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobLazer");
                        _blobFireDistance = 0;

                        BlobFireX = _blobXPos + 16;
                        BlobFireY = _blobYPos + 16;
                        _blobFireFade = 0;

                        _blobCanFire = false;
                        BlobHasFired = true;
                        _blobFireCollisionDirection = 0;

                        if (OnBlobActionRaised != null)
                        {
                            OnBlobActionRaised(enBlobAction.FiredLazer, null);
                        }

                        if (_blobOnShuttle)
                        {
                            _directionBlobFired = DirectionBlobMoved;
                        }
                        else
                        {
                            if (DirectionBlobMoved == enDirection.Left)
                            {
                                _directionBlobFired = enDirection.Left;
                            }
                            else if (DirectionBlobMoved == enDirection.Right)
                            {
                                _directionBlobFired = enDirection.Right;
                            }
                        }
                    }
                }
            }

            // Fire routine
            if (BlobHasFired)
            {
                bool hitScreenBoundary = false;

                if (_blobFireTrail != null)
                {
                    _blobFireTrail.Start();
                }

                if (_blobOnShuttleCached)
                {
                    if (_directionBlobFired == enDirection.Up)
                    {
                        int amountToMoveY = 0;
                        int amountToMoveX = 0;

                        if (_blobFireCollisionDirection == 0)
                        {
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 1)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 2)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, BlobFireX,
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
                                    _blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

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
                            if (BlobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
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
                                if (BlobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
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
                    else if (_directionBlobFired == enDirection.Down)
                    {
                        int amountToMoveY = 0;
                        int amountToMoveX = 0;

                        if (_blobFireCollisionDirection == 0)
                        {
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 1)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 2)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, BlobFireX,
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
                                    _blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

                                    if (_blobFireSparks != null)
                                    {
                                        _blobFireSparks.Start(BlobFireX, BlobFireY + 24);
                                    }
                                }
                            }

                            // Have we crossed the screen boundary
                            if (BlobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
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
                                if (BlobFireY + 32 > gameEngine.GameScreens.ScreenWidth)
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
                    else if (_directionBlobFired == enDirection.Left)
                    {
                        int amountToMoveY = 0;
                        int amountToMoveX = 0;

                        if (_blobFireCollisionDirection == 0)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 1)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 2)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }

                        if (CheckForBlobCollisionWithObject(
                                new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                        {
                            if (_blobFireDistance < offset + (BlobWidth / 2))
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
                                    _blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

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
                            if (BlobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
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
                                if (BlobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
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
                    else if (_directionBlobFired == enDirection.Right)
                    {
                        int amountToMoveY = 0;
                        int amountToMoveX = 0;

                        if (_blobFireCollisionDirection == 0)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 1)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }
                        else if (_blobFireCollisionDirection == 2)
                        {
                            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, BlobFireX,
                                BlobFireY, 2, 32, BlobFireSpeed, false);
                        }

                        if (CheckForBlobCollisionWithObject(
                                new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                        {
                            if (_blobFireDistance < offset + (BlobWidth / 2))
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
                                    _blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

                                    _blobFireSparks?.Start(BlobFireX, BlobFireY + 24);
                                }
                            }

                            // Have we crossed the screen boundary
                            if (BlobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
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
                                if (BlobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
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
                    if (_directionBlobFired == enDirection.Left)
                    {
                        var amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Left, BlobFireX,
                            BlobFireY, 2, 32, BlobFireSpeed, false);

                        if (CheckForBlobCollisionWithObject(
                                new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                        {
                            if (_blobFireDistance < offset + (BlobWidth / 2))
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
                    else if (_directionBlobFired == enDirection.Right)
                    {
                        var amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Right, BlobFireX,
                            BlobFireY, 2, 32, BlobFireSpeed, false);

                        if (CheckForBlobCollisionWithObject(
                                new Rectangle(_blobXPos, _blobYPos, BlobWidth, BlobHeight), out var offset))
                        {
                            if (_blobFireDistance < offset + (BlobWidth / 2))
                            {
                                amountToMove = BlobFireSpeed;
                            }
                        }

                        // Have we crossed the screen boundary
                        if (BlobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
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
            if (BlobHasFired == false)
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
                if (gameEngine.GameScreens.ScreenNumber - 1 >= 0)
                {
                    gameEngine.GameScreens.ScreenNumber--;
                    _blobXPos = gameEngine.GameScreens.ScreenWidth - gameEngine.ContentResourceManager
                        .GetSpriteCollection(gameEngine.BlobSpritesName).SpriteWidth;
                    changedScreens = true;

                    if (OnScreenChanged != null)
                    {
                        OnScreenChanged(enGameScreen.MainGame);
                    }
                }
            }

            if (_blobXPos + gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName)
                    .SpriteWidth > gameEngine.GameScreens.ScreenWidth)
            {
                if (gameEngine.GameScreens.ScreenNumber + 1 <= 512)
                {
                    gameEngine.GameScreens.ScreenNumber++;
                    _blobXPos = 0;
                    changedScreens = true;

                    if (OnScreenChanged != null)
                    {
                        OnScreenChanged(enGameScreen.MainGame);
                    }
                }
            }

            if (_blobYPos < 0)
            {
                if (gameEngine.GameScreens.ScreenNumber - 16 >= 0)
                {
                    gameEngine.GameScreens.ScreenNumber -= 16;
                    changedScreens = true;

                    if (OnScreenChanged != null)
                    {
                        OnScreenChanged(enGameScreen.MainGame);
                    }

                    if (_blobOnShuttle)
                    {
                        _blobYPos = gameEngine.GameScreens.ScreenHeight - BlobHeight;
                    }
                    else // Must be on a jumpplatform unless we're in a tunnel lift.....
                    {
                        if (!_inTunnelLift)
                        {
                            _blobYPos = _blobJumpPlatformEngine.GetNewJumpPlatform(_blobXPos,
                                gameEngine.GameScreens.ScreenHeight - gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.BlobSpritesName).SpriteHeight, 0, 0, false);
                        }
                        else
                        {
                            _blobYPos = gameEngine.GameScreens.ScreenHeight - BlobHeight;
                        }
                    }
                }
            }

            if (_blobYPos + BlobHeight > gameEngine.GameScreens.ScreenHeight)
            {
                if (gameEngine.GameScreens.ScreenNumber + 16 <= 512)
                {
                    gameEngine.GameScreens.ScreenNumber += 16;
                    _blobYPos = 0;
                    _blobDistanceFallen = 0;
                    _blobFallSpeed = 4;

                    changedScreens = true;
                    if (OnScreenChanged != null)
                    {
                        OnScreenChanged(enGameScreen.MainGame);
                    }
                }
            }

            _tileAboveBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Up, _blobXPos, _blobYPos,
                BlobWidth,
                BlobHeight, BlobVSpeed);

            if (_blobOnShuttle)
            {
                _tileBelowBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Down, _blobXPos, _blobYPos,
                    BlobWidth, BlobHeight - 32, BlobVSpeed); // Subtract the shuttle height off
            }
            else
            {
                _tileBelowBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Down, _blobXPos, _blobYPos,
                    BlobWidth, BlobHeight, BlobVSpeed);
            }

            _tileLeftBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Left, _blobXPos, _blobYPos,
                BlobWidth,
                BlobHeight, BlobHSpeed);
            _tileRightBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Right, _blobXPos, _blobYPos,
                BlobWidth, BlobHeight, BlobHSpeed);

            // Detect Blob on a shuttle pad
            _blobOnShuttlePad = false;
            if (_tileBelowBlob != null)
            {
                if ((enActionTiles)_tileBelowBlob.TileNumber == enActionTiles.ShuttlePlatform ||
                    (enActionTiles)_tileBelowBlob.TileNumber == enActionTiles.RemovedShuttlePlatform)
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
                        gameEngine.CurrentGameScreen = enGameScreen.Teleporter;

                        gameEngine.GetGameScreen(enGameScreen.Teleporter).Reset(null);
                        _blobXPos = (_tileRightBlob.MapXPosition * 128) + 20;
                        BlobFrame = 7;
                    }
                }
            }

            // Detect an access booth
            if (_tileLeftBlob != null && _tileLeftBlob.TileNumber == 400)
            {
                if (_tileLeftBlob.XPosition <= 46 &&
                    _tileLeftBlob.YPosition == 32)
                {
                    gameEngine.GetGameScreen(enGameScreen.AccessBooth).Reset(new object[]
                        { enDirection.Left, _tileRightBlob.MapXPosition });
                    gameEngine.CurrentGameScreen = enGameScreen.AccessBooth;
                }
            }

            if (_tileRightBlob != null && _tileRightBlob.TileNumber == 399)
            {
                if (_tileRightBlob.XPosition >= 24 &&
                    _tileRightBlob.YPosition == 32)
                {
                    gameEngine.GetGameScreen(enGameScreen.AccessBooth).Reset(new object[]
                        { enDirection.Right, _tileLeftBlob.MapXPosition });
                    gameEngine.CurrentGameScreen = enGameScreen.AccessBooth;
                }
            }

            // Detect a lefthand tunnel
            if (_tileLeftBlob != null)
            {
                if (_tileLeftBlob.TileNumber == 420 ||
                    _tileLeftBlob.TileNumber == 422 ||
                    _tileLeftBlob.TileNumber == 424 ||
                    _tileLeftBlob.TileNumber == 426 ||
                    _tileLeftBlob.TileNumber == 428 ||
                    _tileLeftBlob.TileNumber == 430 ||
                    _tileLeftBlob.TileNumber == 432 ||
                    _tileLeftBlob.TileNumber == 434 ||
                    _tileLeftBlob.TileNumber == 436 ||
                    _tileLeftBlob.TileNumber == 438
                   )
                {
                    if (_tileLeftBlob.XPosition <= 32 && _tileLeftBlob.YPosition == 32)
                    {
                        gameEngine.GameScreens.ScreenNumber--;
                        _directionGoingThroughTunnel = enDirection.Left;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            gameEngine.ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.LeftTunnelEntry, 80,
                                -1, true);
                            gameEngine.ContentResourceManager.GetSoundFXEngine()
                                .PlaySound("TunnelSwish1", 0.8f, false);
                            OnScreenChanged(enGameScreen.None);
                        }
                    }
                }
            }

            // Detect a righthand tunnel
            if (_tileRightBlob != null)
            {
                if (_tileRightBlob.TileNumber == 421 ||
                    _tileRightBlob.TileNumber == 423 ||
                    _tileRightBlob.TileNumber == 425 ||
                    _tileRightBlob.TileNumber == 427 ||
                    _tileRightBlob.TileNumber == 429 ||
                    _tileRightBlob.TileNumber == 431 ||
                    _tileRightBlob.TileNumber == 433 ||
                    _tileRightBlob.TileNumber == 435 ||
                    _tileRightBlob.TileNumber == 437 ||
                    _tileRightBlob.TileNumber == 439
                   )
                {
                    if (_tileRightBlob.XPosition >= 32 && _tileRightBlob.YPosition == 32)
                    {
                        gameEngine.GameScreens.ScreenNumber++;
                        _directionGoingThroughTunnel = enDirection.Right;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            gameEngine.ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.RightTunnelEntry,
                                80, -1, true);
                            gameEngine.ContentResourceManager.GetSoundFXEngine()
                                .PlaySound("TunnelSwish2", 0.8f, false);
                            OnScreenChanged(enGameScreen.None);
                        }
                    }
                }
            }

            // Breakable bridges
            if (_tileBelowBlob != null)
            {
                if (_tileBelowBlob.TileNumber == 410)
                {
                    if (_blobFallHeight >= 128)
                    {
                        // Play the sound
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BridgeBreak");

                        // Add the tile to the list of tiles to reset on screen change
                        _tilesToReset.Add(new ResetTile(_tileBelowBlob, gameEngine.GameScreens.ScreenNumber));

                        // Remove the tile
                        gameEngine.GameScreens.CurrentGameScreenData
                            .ScreenTiles[_tileBelowBlob.MapXPosition, _tileBelowBlob.MapYPosition].TileImage = -1;

                        // Add the tile to the Breakable bridges engine
                        gameEngine.BreakableBridgesEngine.AddBridge(
                            gameEngine.GameScreens.ConvertToScreenCoords(_tileBelowBlob.MapXPosition,
                                _tileBelowBlob.MapYPosition));
                    }
                }
            }

            // Tunnel lifts
            if (_tileAboveBlob != null)
            {
                if (_tileAboveBlob.TileNumber is 409 or 412 or 408 or 413)
                {
                    if (_tileAboveBlob.XPosition == 32)
                    {
                        if (_inTunnelLift == false)
                        {
                            _distanceInTunnelLift = 0;
                        }

                        _inTunnelLift = true;
                    }
                }
                else if (_tileAboveBlob.TileNumber is 407 or 414 or 415)
                {
                    if (_tileAboveBlob.YPosition == 96)
                    {
                        _inTunnelLift = false;
                    }
                }
            }

            if (_inTunnelLift)
            {
                var moveBlob = true;
                if (_tileAboveBlob != null)
                {
                    if (_tileAboveBlob.TileNumber == 409 ||
                        _tileAboveBlob.TileNumber == 412 ||
                        _tileAboveBlob.TileNumber == 413)
                    {
                        if (_tileAboveBlob.YPosition == 96)
                        {
                            if (BlobsControl.MovedLeft() && gameEngine.GameScreens.CheckCollision(enDirection.Left,
                                    _blobXPos, _blobYPos, BlobWidth, BlobHeight, BlobHSpeed, true) > 0)
                            {
                                if (_distanceInTunnelLift > 8)
                                {
                                    _inTunnelLift = false;
                                    moveBlob = false;
                                }
                            }
                            else if (BlobsControl.MovedRight() &&
                                     gameEngine.GameScreens.CheckCollision(enDirection.Right, _blobXPos, _blobYPos,
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
                }

                if (moveBlob)
                {
                    _blobTunnelLiftSoundId = gameEngine.ContentResourceManager.GetSoundFXEngine()
                        .PlaySound("TunnelLift", true);
                    _blobYPos -= 8;
                    _distanceInTunnelLift += 8;
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine()
                        .StopSound(_blobTunnelLiftSoundId, SoundFade.None);
                }
            }

            // Spiky tiles
            TileDetected tileIntersectingBlob =
                gameEngine.GameScreens.CheckTileByObject(enDirection.None, _blobXPos, _blobYPos, BlobWidth,
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
            if (gameEngine.GameScreens.CurrentGameScreenData.PowerUp != null)
            {
                if (CollisionDetection.Check(
                        _blobXPos,
                        BlobYPos,
                        BlobWidth,
                        BlobHeight,
                        gameEngine.GameScreens.CurrentGameScreenData.PowerUp.XLocation,
                        gameEngine.GameScreens.CurrentGameScreenData.PowerUp.YLocation,
                        32,
                        32))
                {
                    if (OnBlobActionRaised != null)
                    {
                        OnBlobActionRaised(enBlobAction.GotPowerUp,
                            gameEngine.GameScreens.CurrentGameScreenData.PowerUp.Type);
                    }

                    gameEngine.GameScreens.CurrentGameScreenData.PowerUp = null;
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                }
            }

            _blobJumpPlatformEngine.Update(gameEngine.GameScreens);
        }
        else if (BlobsState == enBlobState.Overloading)
        {
            // Play the alarm sound for a bit
            if (_blobAlarmSoundCounter == 1)
            {
                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobOverloadAlarm", 1.0f, false);
            }

            if (_blobOverloadState < 3 && _blobAlarmSoundCounter < 60)
            {
                _blobAlarmSoundCounter++;
            }
            else
            {
                if (gameEngine.BlobKiller.Triggered == false)
                {
                    BlobHasFired = false;
                    _blobCanFire = true;
                    _blobFireTrail.Stop();

                    _blobJumpPlatformEngine.ResetPlatforms();
                    gameEngine.ElectricityNodeGateEngine.Reset();

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
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobExplodes", false);
                        gameEngine.BlobKiller.Trigger(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos, true);
                        BlobsState = enBlobState.Dying;
                    }
                }
            }
        }
        else if (BlobsState == enBlobState.Dying)
        {
            if (gameEngine.BlobKiller.Triggered == false)
            {
                _blobOverloadState = 0;
                _blobScale = 1.0f;
                // If Blob is killed by a spiky tile, we reset his position to when he entered
                if (_resetBlobToScreenEntryPoint)
                {
                    _blobYPos = _blobScreenEntryLocation.Y;
                    _blobXPos = _blobScreenEntryLocation.X;
                    _blobOnShuttle = _blobOnShuttleOnEntry;
                    gameEngine.GameScreens.ChangeShuttlePlatformState(_blobOnShuttle);
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                    BlobFrame = _blobFrameOnEntry;
                    _inTunnelLift = false;
                    _blobFallSpeed = 4;

                    if (_blobOnShuttle)
                        BlobHeight = 96;
                    else
                        BlobHeight = 64;
                }

                _resetBlobToScreenEntryPoint = false;
                BlobsState = enBlobState.Dead;
                gameEngine.BlobKiller.Reset();
            }
        }

        // Update the teleport beam effect - probably should have a boolean to stop drawing when not needed.....
        _blobFireSparks?.Update(gameTime);
        _blobTeleporterBeam?.Update(_blobXPos + 30, _blobYPos + 50, gameTime);
        _blobFireTrail?.Update(BlobFireX + 14, BlobFireY + 14, gameTime);

        if (changedScreens)
        {
            _blobScreenEntryLocation = new Point(BlobXPos, BlobYPos);
            _blobOnShuttleOnEntry = _blobOnShuttle;
            _blobFrameOnEntry = BlobFrame;
        }
    }

    public void UnstickBlobFromTiles()
    {
        // Is blob stuck at the top?
        if (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, BlobWidth, 8, true) >= 0)
        {
            // And not stuck at the bottom?
            if (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos + (BlobHeight - 8), BlobWidth, 8,
                    true) <
                0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                int distanceToMoveY = 0;
                while (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos + distanceToMoveY, BlobWidth, 8,
                           true) >= 0)
                {
                    distanceToMoveY++;
                }

                // Shift Blob to a safe position
                _blobYPos = _blobYPos + distanceToMoveY;
            }
        }

        if (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos + (BlobHeight - 8), BlobWidth, 8, true) >=
            0) // Is Blob stuck at the bottom
        {
            // And not stuck at the top?
            if (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, BlobWidth, 8, true) < 0)
            {
                // He's stuck at the bottom so lets figure out how far we need to go to release him
                int distanceToMoveY = 0;
                while (gameEngine.GameScreens.CheckRectangles(_blobXPos,
                           (_blobYPos + (BlobHeight - 8)) - distanceToMoveY, BlobWidth, 8, true) >= 0)
                {
                    distanceToMoveY++;
                }

                // Shift Blob to a safe position
                _blobYPos = _blobYPos - distanceToMoveY;
            }
        }

        if (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, 8, BlobHeight, true) >=
            0) // Is Blob stuck at the left
        {
            // And not stuck at the right?
            if (gameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - 8), _blobYPos, 8, BlobHeight,
                    true) <
                0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                int distanceToMoveX = 0;
                while (gameEngine.GameScreens.CheckRectangles(_blobXPos + distanceToMoveX, _blobYPos, 8, BlobHeight,
                           true) >= 0)
                {
                    distanceToMoveX++;
                }

                // Shift Blob to a safe position
                _blobXPos = _blobXPos + distanceToMoveX;
            }
        }

        if (gameEngine.GameScreens.CheckRectangles(_blobXPos + (BlobWidth - 8), _blobYPos, 8, BlobHeight, true) >=
            0) // Is Blob stuck at the right
        {
            // And not stuck at the left?
            if (gameEngine.GameScreens.CheckRectangles(_blobXPos, _blobYPos, 8, BlobHeight, true) < 0)
            {
                // He's stuck at the top so lets figure out how far we need to go to release him
                int distanceToMoveX = 0;
                while (gameEngine.GameScreens.CheckRectangles((_blobXPos + (BlobWidth - 8)) - distanceToMoveX,
                           _blobYPos, 8, BlobHeight, true) >= 0)
                {
                    distanceToMoveX++;
                }

                // Shift Blob to a safe position
                _blobXPos = _blobXPos - distanceToMoveX;
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

        if (_directionGoingThroughTunnel != enDirection.None)
        {
            if (_directionGoingThroughTunnel == enDirection.Left)
            {
                for (int a = 421; a < 441; a = a + 2)
                {
                    List<Point> tiles = gameEngine.GameScreens.GetCoordsOfTiles(a);

                    if (tiles != null && tiles.Count == 1)
                    {
                        _blobXPos = tiles[0].X + 24;
                        _blobYPos = tiles[0].Y + 32;
                        break;
                    }
                }
            }
            else if (_directionGoingThroughTunnel == enDirection.Right)
            {
                for (int a = 420; a < 442; a = a + 2)
                {
                    List<Point> tiles = gameEngine.GameScreens.GetCoordsOfTiles(a);

                    if (tiles != null && tiles.Count == 1)
                    {
                        _blobXPos = tiles[0].X + 40;
                        _blobYPos = tiles[0].Y + 32;
                        break;
                    }
                }
            }

            _directionGoingThroughTunnel = enDirection.None;
        }

        foreach (var t in _tilesToReset)
        {
            var screenTiles = gameEngine.GameScreens.GetGameScreen(t.ScreenNumber).ScreenTiles;
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
        int ScreenFade)
    {
        if (BlobsState == enBlobState.Normal)
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

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName)
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).DrawSprite(BlobFrame,
                _blobXPos, _blobYPos,
                ColourHelper.GetXNA4Colour(255, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                    (byte)(_teleportFadeLevel * ((double)ScreenFade / 255))), 1f, 0f);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();

            // Draw other bits
            if (_blobOnShuttle)
            {
                if (gameEngine.UsingOldGraphics)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").DrawSprite(
                        _blobShuttleAnim.CurrentFrame, _blobXPos, _blobYPos + 64,
                        ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").End();
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, _blobXPos,
                        _blobYPos + 64, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
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

                if (gameEngine.UsingOldGraphics)
                {
                    if (_blobOnShuttle)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                            .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                            _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY,
                            ColourHelper.GetXNA4Colour(255, 255, 255,
                                (byte)(255 * ((double)ScreenFade / 255))), 1.0f, 0f, true,
                            gameEngine.Paused);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                    }
                    else
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1")
                            .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").DrawSprite(
                            _blobAnimFire1.CurrentFrame, BlobFireX, BlobFireY - 16,
                            ColourHelper.GetXNA4Colour(255, 255, 255,
                                (byte)(255 * ((double)ScreenFade / 255))), 1.0f, 0f, true,
                            gameEngine.Paused);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").End();
                    }
                }
                else
                {
                    if (_blobOnShuttle)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2")
                            .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").DrawSprite(
                            _blobAnimFire2.CurrentFrame, BlobFireX, BlobFireY,
                            ColourHelper.GetXNA4Colour(255, 255, 255,
                                (byte)(_blobFireFade * ((double)ScreenFade / 255))), 1.0f, 10f,
                            true,
                            gameEngine.Paused);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").End();
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
        }
        else if (BlobsState == enBlobState.Overloading)
        {
            if (_blobAlarmSoundCounter < 60)
            {
                // Make blob flicker
                if (_blobAlarmSoundCounter % 2 == 0)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName)
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).DrawSprite(
                        BlobFrame, _blobXPos, _blobYPos,
                        ColourHelper.GetXNA4Colour(255, (byte)_blobEnergyDrainCounter, (byte)_blobEnergyDrainCounter,
                            (byte)(_teleportFadeLevel * ((double)ScreenFade / 255))), 1f, 0f);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();
                }

                // Draw other bits
                if (_blobOnShuttle)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, _blobXPos,
                        _blobYPos + 64, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
                }
            }
            else
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName)
                    .Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

                var temp = _blobScale - 0.4f;
                for (var a = 0; a < 5; a++)
                {
                    if (temp >= 0f)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName)
                            .DrawSprite(BlobFrame, _blobXPos, _blobYPos,
                                ColourHelper.GetXNA4Colour(255, 255, 255, 150), temp, 0f);
                    }

                    temp += 0.2f;
                }

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();
            }
        }
    }

    public void DrawUpper(int screenFade)
    {
        _blobFireSparks?.Draw(screenFade);
    }

    /// <summary>
    /// Gets collision objects on the current screen which aren't tiles i.e Cheops Pyramids, CoreElements 
    /// </summary>
    private List<InteractiveItem> GetCollisionObjects()
    {
        var temp = new List<InteractiveItem>();
        if (gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid != null)
        {
            temp.Add(gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid);
        }

        foreach (var t in gameEngine.GameScreens.CurrentGameScreenData.PickUps)
        {
            temp.Add(t);
        }

        return temp.Count > 0 ? temp : null;
    }

    private bool CheckForBlobCollisionWithObject(Rectangle BlobsPosition, out int Offset)
    {
        Offset = 0;

        var screenItems = GetCollisionObjects();
        if (screenItems == null)
        {
            return false;
        }

        foreach (var t in screenItems)
        {
            if (!CollisionDetection.Check(BlobsPosition, new Rectangle(t.XLocation, t.YLocation, 64, 64)))
            {
                continue;
            }

            Offset = BlobsPosition.X - t.XLocation;
            if (Offset < 0)
            {
                Offset = -Offset;
            }

            return true;
        }

        return false;
    }

    public void Reset()
    {
        // Reset blob's energy drain counter
        _blobEnergyDrainCounter = 0;

        // Reset Blob's animation frame
        BlobFrame = 5;

        // Reset Blob's state
        BlobsState = enBlobState.Normal;

        // Reset Blob's position
        _blobXPos = 416;
        _blobYPos = 414;

        // Remove Blob from shuttle pads
        _blobOnShuttle = false;
        BlobHeight = 64;
        gameEngine.GameScreens.ChangeShuttlePlatformState(false);

        // Clear up any rogue collision tiles etc.
        PerformPostChangeScreenActions();
    }
}