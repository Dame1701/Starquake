// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class Nasty : GameComponentBase
{

    // The number of frames for the birth to appear (60 frames = 1 second)
    private const int BirthPeriod = 60;

    // Animation
    private readonly Animation _nastyAnim;

    private bool _attractedToBlob; // They got through phases of being attracted to Blob
    private int _birthColourLevel;

    // Birth stuff
    private int _birthFadeLevel;
    private float _birthRotateSpeed = 8.0f;

    private float _birthScale;
    private int _birthTimer;

    // Counters
    private int _conceptionwaitCounter;

    private bool _constantMovement; // Movement type that doesn't rely on a number of steps
    private bool _deathAnticlockwise;

    // Death stuff
    private int _deathFadeLevel = 255;
    private float _deathScale = 1f;
    private bool _firstMovement;

    private Direction _lastCollisionDirection;
    private int _movementChangeCount;
    private int _movementChangeCounter;

    // Behaviour

    private int _nastyDeadPauseCounter;
    private IStandardParticleEffect _nastyDies;

    private Animation _nastyDiesAnimation;
    private IStandardParticleEffect _nastyDiesType2;
    //bool attractedToBlobFlag;


    // When shot monsters usually dissappear for good but there is a small chance they'll regenerate
    // Tech stuff
    private SpriteCollection _nastySprite;

    private SpriteCollection _nastySpriteNew;
    private SpriteCollection _nastySpriteOriginal;

    /// <summary>
    ///     If the nasty is attracted to blob but something is in the way it won't be able to move
    ///     So this records such an instance and releases it after a number of frames
    /// </summary>
    private int _nastyStuckCounter;

    private NastyType _nastyType;

    private int _prevXpos;
    private int _prevYpos;

    private bool _spikeyPause;
    private int _stepsMovedCounter;
    private int _stepsToMove;

    // Position

    public Nasty(
        GameEngine gameEngineReference,
        int xposition,
        int yposition,
        int moveSpeed)
        : base(gameEngineReference)
    {
        XPos = xposition;
        YPos = yposition;
        MoveSpeed = moveSpeed;

        // Randomly assign some shit....
        _movementChangeCount = 0;

        _nastyAnim = new Animation();
        _nastyAnim.FirstFrame = 0;
        _nastyAnim.LastFrame = 3;
        _nastyAnim.Repeat = true;
        _nastyAnim.AnimateForwards = true;
        _nastyAnim.Yoyo = true;
        _nastyAnim.Delay = 4;
    }

    private MoveType MovementType
    {
        get;
        set;
        //movementTypeInternal = MoveType.DiagonalBouncing;
    }

    public Direction Direction { get; private set; }

    private int MoveSpeed
    {
        get;
        set;
        //moveSpeedInternal = 40;
    }

    public int Speed => MoveSpeed;

    public bool KeepNastyTheSame { get; set; }

    public int NastyWidth { get; set; } = 64;

    public NastyState NastyState { get; set; }

    public int NastyHeight { get; set; } = 64;

    public bool Smoking { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    public NastyType NastyType
    {
        get => _nastyType;
        set
        {
            _nastyType = value;
            if (_nastyType == NastyType.CoreGuardian)
            {
                MovementType = MoveType.DiagonalBouncing;
            }
            else if (_nastyType == NastyType.Spiky)
            {
                MovementType = MoveType.SpikeyMovement;
            }
            else
            {
                MovementType =
                    (MoveType)GameEngine.RandomNumberGenerator.Next(0,
                        Enum.GetValues(typeof(MoveType)).Length - 1);
            }
        }
    }

    public void CopyFromSource(Nasty copySource)
    {
        MovementType = copySource.MovementType;
        _attractedToBlob = copySource._attractedToBlob;
        Direction = copySource.Direction;
        _stepsMovedCounter = copySource._stepsMovedCounter;
        Direction = copySource.Direction;
        _stepsMovedCounter = copySource._stepsMovedCounter;
        _stepsToMove = copySource._stepsToMove;
        _constantMovement = copySource._constantMovement;
        MoveSpeed = copySource.MoveSpeed;
        _lastCollisionDirection = copySource._lastCollisionDirection;
        _movementChangeCounter = copySource._movementChangeCounter;
        _movementChangeCount = copySource._movementChangeCount;
        KeepNastyTheSame = copySource.KeepNastyTheSame;
        _nastyType = copySource._nastyType;
        XPos = copySource.XPos;
        YPos = copySource.YPos;
        _nastyDeadPauseCounter = copySource._nastyDeadPauseCounter;

        // If the basty is dead reset it to conceptive state
        if (copySource.NastyState == NastyState.Dead)
        {
            NastyState = NastyState.PreConception;
        }

        // Ensure that we don't carry on generating after moving screens
        if (copySource.NastyState == NastyState.Birth)
        {
            NastyState = NastyState.Normal;
        }
        else
        {
            NastyState = copySource.NastyState;
        }
    }

    private enum MoveType
    {
        RandomMovement,
        RandomUpDownLeftRight,
        DiagonalBouncing,
        DiagonalBouncingInSteps,
        RandomMovementsAllDirections,
        SpikeyMovement
    }

    #region IGameComponent Members

    private Point GetNastySpawnLocation()
    {
        // Get all locations where we can have nasties from the current game screen
        var potentialLocations = GameEngine.GameScreens.GetNastyPotentialLocations();

        if (potentialLocations.Count > 0)
        {
            var nastyLocationIndex = GameEngine.RandomNumberGenerator.Next(0, potentialLocations.Count);

            // Return a location
            return new Point(potentialLocations[nastyLocationIndex].X + 32,
                potentialLocations[nastyLocationIndex].Y + 16);
        }

        // this should never happen
        return new Point(0, 0);
    }

    public void LoadContent(ContentList content)
    {
        _nastyDiesAnimation = new Animation();
        _nastyDiesAnimation.FirstFrame = 0;
        _nastyDiesAnimation.CurrentFrame = 0;
        _nastyDiesAnimation.LastFrame = 3;
        _nastyDiesAnimation.Yoyo = false;
        _nastyDiesAnimation.Delay = 2;
        _nastyDiesAnimation.Repeat = true;
        _nastyDiesAnimation.AnimateForwards = true;

        _nastySpriteOriginal = GameEngine.ContentResourceManager.GetSpriteCollection("NastiesOriginal")
            .Clone(GameEngine);
        _nastySpriteNew = GameEngine.ContentResourceManager.GetSpriteCollection("Nasties").Clone(GameEngine);

        CheckSpritesInUse();

        _nastyDies = GameEngine.ContentResourceManager.GetParticleEffectInstance("NastyDies",
            ParticleEffectType.Triggered, null) as IStandardParticleEffect;
        _nastyDies.ParticleEffectComplete += nastyDies_ParticleEffectComplete;

        _nastyDiesType2 =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("NastySplat",
                ParticleEffectType.Triggered, null) as IStandardParticleEffect;
        _nastyDiesType2.ParticleEffectComplete += nastyDies_ParticleEffectComplete;
    }

    private void CheckSpritesInUse()
    {
        if (GameEngine.UsingOldGraphics)
        {
            _nastySprite = _nastySpriteOriginal;
        }
        else
        {
            _nastySprite = _nastySpriteNew;
        }
    }

    private void nastyDies_ParticleEffectComplete()
    {
        if (NastyState == NastyState.Dead)
        {
            // Extend this to pause for random time before bringing a nasty back to life
            _nastyDeadPauseCounter = 0;
            NastyState = NastyState.PreConception;
            _conceptionwaitCounter = GameEngine.RandomNumberGenerator.Next(0, 500);
        }
    }

    public void Initialise()
    {
        _stepsMovedCounter = 0;
        _stepsToMove = 0;

        if (MovementType == MoveType.DiagonalBouncing)
        {
            Direction = (Direction)GameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
        }
        else if (MovementType == MoveType.RandomMovement ||
                 MovementType == MoveType.RandomUpDownLeftRight)
        {
            Direction = (Direction)GameEngine.RandomNumberGenerator.Next(0, 4); // select from 1st four
        }
    }

    public static NastyType GetRandomNastyType(GameEngine gameEngineReference, bool keepNastiesTheSame)
    {
        //NastyType = (enNastyType)gameEngine.RandomNumberGenerator.Next(0, 15); // Pick from first 15 types

        var temp = 0;

        // Go to more than max nasty types to increase chance of four spiky nasties
        if (keepNastiesTheSame)
        {
            // Add an extra 5 onto final nasty number when all anims done
            temp = gameEngineReference.RandomNumberGenerator.Next(0, 16);
        }
        else
        {
            temp = gameEngineReference.RandomNumberGenerator.Next(0, 16);
        }

        if (temp == 0)
        {
            return NastyType.Bird;
        }
        if (temp == 1)
        {
            return NastyType.Helmet;
        }
        if (temp == 2)
        {
            return NastyType.FriedEgg;
        }
        if (temp == 3)
        {
            return NastyType.CurleyWurley;
        }
        if (temp == 4)
        {
            return NastyType.Shellfish;
        }
        if (temp == 5)
        {
            return NastyType.Cushion;
        }
        if (temp == 6)
        {
            return NastyType.Gonzo;
        }
        if (temp == 7)
        {
            return NastyType.Snail;
        }
        if (temp == 8)
        {
            return NastyType.Ghost;
        }
        if (temp == 9)
        {
            return NastyType.Centipede;
        }
        if (temp == 10)
        {
            return NastyType.Scorpian;
        }
        if (temp == 11)
        {
            return NastyType.DragonFly;
        }
        if (temp == 12)
        {
            return NastyType.Hippo;
        }
        if (temp == 13)
        {
            return NastyType.Helicopter;
        }
        if (temp == 14)
        {
            return NastyType.Bird;
        }
        return NastyType.Spiky;
    }

    public override void Update(GameTime gameTime)
    {
        if (GameEngine.GameScreens.ScreenNumber == 198)
        {
            NastyState = NastyState.Disabled;
        }

        if (GameEngine.GameScreens.ScreenNumber == 199)
        {
            _nastyType = NastyType.CoreGuardian;
            _attractedToBlob = false;
        }

        // Reset conception variables
        if (NastyState == NastyState.PreConception)
        {
            _conceptionwaitCounter = GameEngine.RandomNumberGenerator.Next(0, 60);
            NastyState = NastyState.Conception;

            if (!KeepNastyTheSame)
            {
                _nastyType = GetRandomNastyType(GameEngine, false);
            }

            if (_nastyType != NastyType.Spiky)
            {
                MovementType = MoveType.RandomMovement;
            }
            else
            {
                MovementType = MoveType.SpikeyMovement;
                _spikeyPause = true;
            }
        }
        else if (NastyState == NastyState.Conception)
        {
            if (_conceptionwaitCounter > 0)
            {
                _conceptionwaitCounter--;
            }
            else
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("NastyAppear", 0.1F, false);
                _movementChangeCounter = 0;
                _movementChangeCount = 0;
                _firstMovement = true;
                _deathFadeLevel = 255;
                _deathScale = 1f;
                _birthFadeLevel = 0;
                _birthScale = 0f;
                _birthColourLevel = 0;
                _birthRotateSpeed = 32.0f;
                _birthTimer = 0;

                // Select a new location to spawn
                var newPosition = GetNastySpawnLocation();
                if (newPosition == new Point(0, 0))
                {
                    NastyState = NastyState.Disabled;
                }
                else
                {
                    XPos = newPosition.X;
                    YPos = newPosition.Y;
                }

                NastyState = NastyState.Birth;
            }
        }
        else if (NastyState == NastyState.Birth)
        {
            if (_birthScale < 1.0f)
            {
                _birthScale = _birthScale + 1f / BirthPeriod;
            }

            if (_birthScale > 1.0f)
            {
                _birthScale = 1.0f;
            }


            // Fade the nasty in
            if (_birthFadeLevel < 255)
            {
                _birthFadeLevel =
                    _birthFadeLevel +
                    (int)(255 /
                          ((double)BirthPeriod /
                           2)); // We only have half the time to do the colour fade in 
            }


            // Once we're halfway through fade in the colours
            if (_birthTimer > BirthPeriod / 2)
            {
                if (_birthColourLevel < 255)
                {
                    _birthColourLevel =
                        _birthColourLevel +
                        (int)(255 /
                              ((double)BirthPeriod /
                               2)); // We only have half the time to do the colour fade in 
                }

                if (_birthRotateSpeed > 16)
                {
                    _birthRotateSpeed = _birthRotateSpeed - 16f / ((float)BirthPeriod / 2);
                }
            }

            if (_birthFadeLevel > 255)
            {
                _birthFadeLevel = 255;
            }

            if (_birthColourLevel > 255)
            {
                _birthColourLevel = 255;
            }

            if (_birthTimer > BirthPeriod)
            {
                if (_nastySprite.SpriteRotation == 0)
                {
                    NastyState = NastyState.Normal;
                    _birthTimer = 0;
                }
            }

            _birthTimer++;
        }
        else if (NastyState == NastyState.Shot)
        {
            _deathFadeLevel = 255;
            _deathScale = 1.0f;

            if (_nastyType == NastyType.Spiky ||
                _nastyType == NastyType.Conveyer)
            {
                if (_nastyDies != null)
                {
                    _nastyDies.Start();
                }
            }
            else
            {
                if (_nastyDiesType2 != null)
                {
                    NastyHelpers.ApplyNastyColours(_nastyType, _nastyDiesType2.Effect);
                    _nastyDiesType2.Start();
                }
            }

            NastyState = NastyState.Dead;

            if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
            {
                _deathAnticlockwise = true;
            }
            else
            {
                _deathAnticlockwise = false;
            }
        }
        else if (NastyState == NastyState.Dead)
        {
            if (GameEngine.UsingOldGraphics)
            {
                _nastyDiesAnimation.GetNextFrame();

                if (_nastyDeadPauseCounter < 50)
                {
                    _nastyDeadPauseCounter++;
                }
                else
                {
                    NastyState = NastyState.PreConception;
                    _conceptionwaitCounter = GameEngine.RandomNumberGenerator.Next(0, 500);
                    _nastyDeadPauseCounter = 0;
                }
            }
        }

        if (_nastyDies != null)
        {
            // Update the explosion
            _nastyDies.Update(XPos + 32, YPos + 32, gameTime);
        }

        if (_nastyDiesType2 != null)
        {
            // Update the splat
            _nastyDiesType2.Update(XPos + 32, YPos + 32, gameTime);
        }

        var updateAnimation = false;

        if (NastyState != NastyState.Disabled &&
            NastyState != NastyState.PreConception &&
            NastyState != NastyState.Conception &&
            NastyState != NastyState.Birth)
        {
            updateAnimation = true;
        }

        if (NastyState == NastyState.Birth &&
            _birthTimer > BirthPeriod / 4)
        {
            updateAnimation = true;
        }

        if (updateAnimation)
        {
            // Animate
            _nastyAnim.GetNextFrame();

            if (_nastyType != NastyType.CoreGuardian)
            {
                if (_attractedToBlob && _nastyType != NastyType.Spiky)
                {
                    if (_prevXpos == XPos &&
                        _prevYpos == YPos)
                    {
                        _nastyStuckCounter++;
                    }
                    else
                    {
                        _nastyStuckCounter = 0;
                    }

                    // Has nasty been stuck for 1/4 of a second...?
                    if (_nastyStuckCounter > 15)
                    {
                        // Unattract blob
                        _attractedToBlob = false;
                        _nastyStuckCounter = 0;
                    }

                    _prevXpos = XPos;
                    _prevYpos = YPos;
                }
            }

            var amountToMoveX = 0;
            var amountToMoveY = 0;

            // Do the movement
            if (_nastyType == NastyType.CoreGuardian)
            {
                MoveNastyOneStep(ref amountToMoveX, ref amountToMoveY);
                DiagonalBouncing(0, 0);
            }
            else
            {
                if (_stepsMovedCounter < _stepsToMove)
                {
                    var moveOneStep = true;

                    if (MovementType == MoveType.SpikeyMovement && _spikeyPause)
                    {
                        moveOneStep = false;
                        _stepsMovedCounter++;
                    }

                    if (moveOneStep)
                    {
                        MoveNastyOneStep(ref amountToMoveX, ref amountToMoveY);
                    }
                }
                else
                {
                    Direction = Direction.None;
                    _stepsMovedCounter = 0;

                    if (MovementType == MoveType.RandomMovement)
                    {
                        _constantMovement = false;
                        RandomMovement(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos);
                    }
                    else if (MovementType == MoveType.RandomUpDownLeftRight)
                    {
                        _constantMovement = false;
                        RandomUpDownLeftRight(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos);
                    }
                    else if (MovementType == MoveType.DiagonalBouncing)
                    {
                        _constantMovement = true;
                        if (GameEngine.Player != null)
                        {
                            DiagonalBouncing(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos);
                        }
                        else
                        {
                            DiagonalBouncing(0, 0);
                        }
                    }
                    else if (MovementType == MoveType.DiagonalBouncingInSteps)
                    {
                        _constantMovement = false;
                        DiagonalBouncingInSteps(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos);
                    }
                    else if (MovementType == MoveType.RandomMovementsAllDirections)
                    {
                        _constantMovement = false;
                        RandomMovementsAllDirections(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos);
                    }
                    else if (MovementType == MoveType.SpikeyMovement)
                    {
                        _spikeyPause = !_spikeyPause;
                        _constantMovement = false;
                        SpikeyMovement(GameEngine.Player.BlobXPos, GameEngine.Player.BlobYPos);
                    }
                }
            }

            // increment the movement change counter
            if (_nastyType != NastyType.CoreGuardian)
            {
                if (_movementChangeCounter < _movementChangeCount)
                {
                    _movementChangeCounter++;
                }
                else
                {
                    var attractionFlag = GameEngine.RandomNumberGenerator.Next(0, 2);

                    if (attractionFlag == 0)
                    {
                        _movementChangeCount = GameEngine.RandomNumberGenerator.Next(200, 500);
                        _attractedToBlob = false;
                    }
                    else
                    {
                        _movementChangeCount = GameEngine.RandomNumberGenerator.Next(400, 700);
                        _attractedToBlob = true;
                    }

                    _movementChangeCounter = 0;

                    if (_nastyType == NastyType.Spiky)
                    {
                        _attractedToBlob = true; // Spiky nasties are always attracted to blob
                        MovementType = MoveType.SpikeyMovement;
                    }
                    else
                    {
                        if (_attractedToBlob)
                        {
                            // just choose from the attraction modes....
                            MovementType = (MoveType)GameEngine.RandomNumberGenerator.Next(0, 2);
                        }
                        else
                        {
                            MovementType = (MoveType)GameEngine.RandomNumberGenerator.Next(0,
                                Enum.GetValues(typeof(MoveType)).Length - 1);
                        }

                        if (MovementType == MoveType.DiagonalBouncing)
                        {
                            Direction =
                                (Direction)GameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
                        }
                        else if (MovementType == MoveType.RandomMovement ||
                                 MovementType == MoveType.RandomUpDownLeftRight)
                        {
                            Direction =
                                (Direction)GameEngine.RandomNumberGenerator.Next(0, 4); // select from 1st four
                        }
                        else if (MovementType == MoveType.DiagonalBouncingInSteps)
                        {
                            Direction =
                                (Direction)GameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
                            _stepsToMove = GameEngine.RandomNumberGenerator.Next(8, 16);
                        }

                        // Create a random speed for the nasty to move
                        if (!_attractedToBlob)
                        {
                            if (MovementType == MoveType.DiagonalBouncing ||
                                MovementType == MoveType.DiagonalBouncingInSteps ||
                                MovementType == MoveType.RandomUpDownLeftRight)
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(6, 8);
                            }
                            else if (MovementType == MoveType.RandomMovement ||
                                     MovementType == MoveType.RandomMovementsAllDirections)
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(2, 4);
                            }
                            else
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(6, 8);
                            }
                        }
                        else
                        {
                            if (MovementType == MoveType.RandomMovement ||
                                MovementType == MoveType.RandomMovementsAllDirections)
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(4, 6);
                            }
                            else if (MovementType == MoveType.RandomUpDownLeftRight ||
                                     MovementType == MoveType.DiagonalBouncing ||
                                     MovementType == MoveType.DiagonalBouncingInSteps)
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(8, 10);
                            }
                            else if (MovementType == MoveType.SpikeyMovement)
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(4, 6);
                            }
                            else
                            {
                                MoveSpeed = GameEngine.RandomNumberGenerator.Next(8, 10);
                            }
                        }

                        // Change movespeed to 1 if this is the first movement - allows the nasties to sppear moving slowly
                        if (_firstMovement)
                        {
                            MoveSpeed = 1;
                            _movementChangeCount = 90;
                            _firstMovement = false;
                        }
                    }
                }
            }
            else
            {
                _attractedToBlob = false;
                _constantMovement = true;
                MovementType = MoveType.DiagonalBouncing;
                MoveSpeed = 8;
            }
        }
    }

    private void DiagonalBouncingInSteps(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = (Direction)GameEngine.RandomNumberGenerator.Next(4, 8); // Select from last 4
        }

        if (Direction == Direction.UpLeft)
        {
            if (_lastCollisionDirection == Direction.UpLeft)
            {
                Direction = Direction.DownRight;
            }
            else if (_lastCollisionDirection == Direction.Left)
            {
                Direction = Direction.UpRight;
            }
            else if (_lastCollisionDirection == Direction.Up)
            {
                Direction = Direction.DownLeft;
            }
            else
            {
                Direction = Direction.DownLeft; // Didn't hit anything, just change direction
            }
        }
        else if (Direction == Direction.UpRight)
        {
            if (_lastCollisionDirection == Direction.UpRight)
            {
                Direction = Direction.DownLeft;
            }
            else if (_lastCollisionDirection == Direction.Right)
            {
                Direction = Direction.UpLeft;
            }
            else if (_lastCollisionDirection == Direction.Up)
            {
                Direction = Direction.DownRight;
            }
            else
            {
                Direction = Direction.DownRight; // Didn't hit anything, just change direction
            }
        }
        else if (Direction == Direction.DownLeft)
        {
            if (_lastCollisionDirection == Direction.DownLeft)
            {
                Direction = Direction.UpRight;
            }
            else if (_lastCollisionDirection == Direction.Left)
            {
                Direction = Direction.DownRight;
            }
            else if (_lastCollisionDirection == Direction.Down)
            {
                Direction = Direction.UpLeft;
            }
            else
            {
                Direction = Direction.UpLeft; // Didn't hit anything, just change direction
            }
        }
        else if (Direction == Direction.DownRight)
        {
            if (_lastCollisionDirection == Direction.DownRight)
            {
                Direction = Direction.UpLeft;
            }
            else if (_lastCollisionDirection == Direction.Right)
            {
                Direction = Direction.DownLeft;
            }
            else if (_lastCollisionDirection == Direction.Down)
            {
                Direction = Direction.UpRight;
            }
            else
            {
                Direction = Direction.UpRight; // Didn't hit anything, just change direction
            }
        }
    }

    private void DiagonalBouncing(int blobsXPosition, int blobsYPosition)
    {
        if (Direction == Direction.None)
        {
            Direction = (Direction)GameEngine.RandomNumberGenerator.Next(4, 8); // Select from last 4
        }

        if (Direction == Direction.UpLeft)
        {
            if (_lastCollisionDirection == Direction.UpLeft)
            {
                Direction = Direction.DownRight;
            }
            else if (_lastCollisionDirection == Direction.Left)
            {
                Direction = Direction.UpRight;
            }
            else if (_lastCollisionDirection == Direction.Up)
            {
                Direction = Direction.DownLeft;
            }
        }
        else if (Direction == Direction.UpRight)
        {
            if (_lastCollisionDirection == Direction.UpRight)
            {
                Direction = Direction.DownLeft;
            }
            else if (_lastCollisionDirection == Direction.Right)
            {
                Direction = Direction.UpLeft;
            }
            else if (_lastCollisionDirection == Direction.Up)
            {
                Direction = Direction.DownRight;
            }
        }
        else if (Direction == Direction.DownLeft)
        {
            if (_lastCollisionDirection == Direction.DownLeft)
            {
                Direction = Direction.UpRight;
            }
            else if (_lastCollisionDirection == Direction.Left)
            {
                Direction = Direction.DownRight;
            }
            else if (_lastCollisionDirection == Direction.Down)
            {
                Direction = Direction.UpLeft;
            }
        }
        else if (Direction == Direction.DownRight)
        {
            if (_lastCollisionDirection == Direction.DownRight)
            {
                Direction = Direction.UpLeft;
            }
            else if (_lastCollisionDirection == Direction.Right)
            {
                Direction = Direction.DownLeft;
            }
            else if (_lastCollisionDirection == Direction.Down)
            {
                Direction = Direction.UpRight;
            }
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(8, 40);
    }

    private void RandomUpDownLeftRight(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = (Direction)GameEngine.RandomNumberGenerator.Next(0, 4); // Only select fron first 4
        }

        if (_lastCollisionDirection == Direction.Up)
        {
            Direction = Direction.Down;
        }
        else if (_lastCollisionDirection == Direction.Down)
        {
            Direction = Direction.Up;
        }
        else if (_lastCollisionDirection == Direction.Left)
        {
            Direction = Direction.Right;
        }
        else if (_lastCollisionDirection == Direction.Right)
        {
            Direction = Direction.Left;
        }

        // If attracted to blob is turned on
        if (_attractedToBlob)
        {
            if (XPos < blobsXPosition)
            {
                if (_lastCollisionDirection != Direction.Right)
                {
                    Direction = Direction.Right;
                }
            }
            else if (XPos > blobsXPosition)
            {
                if (_lastCollisionDirection != Direction.Left)
                {
                    Direction = Direction.Left;
                }
            }


            if (YPos < blobsYPosition)
            {
                if (_lastCollisionDirection != Direction.Down)
                {
                    if (Direction == Direction.Left)
                    {
                        Direction = Direction.DownLeft;
                    }
                    else if (Direction == Direction.Right)
                    {
                        Direction = Direction.DownRight;
                    }
                    else
                    {
                        Direction = Direction.Down;
                    }
                }
            }
            else if (YPos > blobsYPosition)
            {
                if (_lastCollisionDirection != Direction.Up)
                {
                    if (Direction == Direction.Left)
                    {
                        Direction = Direction.UpLeft;
                    }
                    else if (Direction == Direction.Right)
                    {
                        Direction = Direction.UpRight;
                    }
                    else
                    {
                        Direction = Direction.Up;
                    }
                }
            }
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(8, 40);
    }

    private void RandomMovement(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = (Direction)GameEngine.RandomNumberGenerator.Next(0, 4); // Only select fron first 4
        }

        if (_lastCollisionDirection == Direction.Up)
        {
            Direction = Direction.Down;
        }
        else if (_lastCollisionDirection == Direction.Down)
        {
            Direction = Direction.Up;
        }
        else if (_lastCollisionDirection == Direction.Left)
        {
            Direction = Direction.Right;
        }
        else if (_lastCollisionDirection == Direction.Right)
        {
            Direction = Direction.Left;
        }

        // If attracted to blob is turned on
        if (_attractedToBlob)
        {
            Direction = Direction.None;

            if (XPos < blobsXPosition)
            {
                if (_lastCollisionDirection != Direction.Right)
                {
                    Direction = Direction.Right;
                }
            }
            else if (XPos > blobsXPosition)
            {
                if (_lastCollisionDirection != Direction.Left)
                {
                    Direction = Direction.Left;
                }
            }


            if (YPos < blobsYPosition)
            {
                if (_lastCollisionDirection != Direction.Down)
                {
                    if (Direction == Direction.Left)
                    {
                        Direction = Direction.DownLeft;
                    }
                    else if (Direction == Direction.Right)
                    {
                        Direction = Direction.DownRight;
                    }
                    else
                    {
                        Direction = Direction.Down;
                    }
                }
            }
            else if (YPos > blobsYPosition)
            {
                if (_lastCollisionDirection != Direction.Up)
                {
                    if (Direction == Direction.Left)
                    {
                        Direction = Direction.UpLeft;
                    }
                    else if (Direction == Direction.Right)
                    {
                        Direction = Direction.UpRight;
                    }
                    else
                    {
                        Direction = Direction.Up;
                    }
                }
            }
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(1, 4);
    }

    private void RandomMovementsAllDirections(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction =
                (Direction)GameEngine.RandomNumberGenerator.Next(0, 8); // Select from all movement directions
        }

        if (!_attractedToBlob)
        {
            if (_lastCollisionDirection == Direction.Up)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 2);

                if (newDirection == 0)
                {
                    Direction = Direction.Down;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.DownLeft;
                }
                else
                {
                    Direction = Direction.DownRight;
                }
            }
            else if (_lastCollisionDirection == Direction.Down)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Up;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.UpLeft;
                }
                else
                {
                    Direction = Direction.UpRight;
                }
            }
            else if (_lastCollisionDirection == Direction.Left)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Right;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.DownRight;
                }
                else
                {
                    Direction = Direction.UpRight;
                }
            }
            else if (_lastCollisionDirection == Direction.Right)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Left;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.DownLeft;
                }
                else
                {
                    Direction = Direction.UpLeft;
                }
            }
            else if (_lastCollisionDirection == Direction.UpLeft)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Down;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.DownLeft;
                }
                else
                {
                    Direction = Direction.DownRight;
                }
            }
            else if (_lastCollisionDirection == Direction.UpRight)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Down;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.DownRight;
                }
                else
                {
                    Direction = Direction.DownLeft;
                }
            }
            else if (_lastCollisionDirection == Direction.DownLeft)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Up;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.UpLeft;
                }
                else
                {
                    Direction = Direction.UpRight;
                }
            }
            else if (_lastCollisionDirection == Direction.DownRight)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Up;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.UpRight;
                }
                else
                {
                    Direction = Direction.UpLeft;
                }
            }
        }
        else // Attracted to Blob
        {
            if (XPos <= blobsXPosition && YPos <= blobsYPosition)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Right;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.Down;
                }
                else
                {
                    Direction = Direction.DownRight;
                }
            }
            else if (XPos <= blobsXPosition && YPos >= blobsYPosition)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Right;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.Up;
                }
                else
                {
                    Direction = Direction.UpRight;
                }
            }
            else if (XPos >= blobsXPosition && YPos >= blobsYPosition)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Left;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.Up;
                }
                else
                {
                    Direction = Direction.UpLeft;
                }
            }
            else if (XPos >= blobsXPosition && YPos <= blobsYPosition)
            {
                var newDirection = GameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = Direction.Left;
                }
                else if (newDirection == 1)
                {
                    Direction = Direction.Down;
                }
                else
                {
                    Direction = Direction.DownLeft;
                }
            }
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(10, 40);
    }

    private void SpikeyMovement(int blobsXPosition, int blobsYPosition)
    {
        var stepsFromBlob = 0;

        if (!_spikeyPause)
        {
            if (XPos <= blobsXPosition && YPos <= blobsYPosition)
            {
                var distanceX = blobsXPosition - XPos;
                var distanceY = blobsYPosition - YPos;

                if (distanceX > distanceY)
                {
                    Direction = Direction.Right;
                    stepsFromBlob = blobsXPosition - XPos;
                }
                else if (distanceY > distanceX)
                {
                    Direction = Direction.Down;
                    stepsFromBlob = blobsYPosition - YPos;
                }

                if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == Direction.Down)
                    {
                        Direction = Direction.DownRight;
                        stepsFromBlob = blobsYPosition - YPos;
                    }
                    else if (Direction == Direction.Right)
                    {
                        Direction = Direction.DownRight;
                        stepsFromBlob = blobsXPosition - XPos;
                    }
                }

                if (_lastCollisionDirection == Direction.Right)
                {
                    Direction = Direction.Down;
                    stepsFromBlob = blobsYPosition - YPos;
                }
                else if (_lastCollisionDirection == Direction.Down)
                {
                    Direction = Direction.Right;
                    stepsFromBlob = blobsXPosition - XPos;
                }
                else if (_lastCollisionDirection == Direction.DownRight)
                {
                    if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = Direction.Right;
                        stepsFromBlob = blobsXPosition - XPos;
                    }
                    else
                    {
                        Direction = Direction.Down;
                        stepsFromBlob = blobsYPosition - YPos;
                    }
                }
            }
            else if (XPos <= blobsXPosition && YPos >= blobsYPosition)
            {
                var distanceX = blobsXPosition - XPos;
                var distanceY = YPos - blobsYPosition;

                if (distanceX > distanceY)
                {
                    Direction = Direction.Right;
                    stepsFromBlob = blobsXPosition - XPos;
                }
                else if (distanceY > distanceX)
                {
                    Direction = Direction.Up;
                    stepsFromBlob = YPos - blobsYPosition;
                }

                if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == Direction.Right)
                    {
                        Direction = Direction.UpRight;
                        stepsFromBlob = blobsXPosition - XPos;
                    }
                    else if (Direction == Direction.Up)
                    {
                        Direction = Direction.UpRight;
                        stepsFromBlob = YPos - blobsYPosition;
                    }
                }

                if (_lastCollisionDirection == Direction.Up)
                {
                    Direction = Direction.Right;
                    stepsFromBlob = blobsXPosition - XPos;
                }
                else if (_lastCollisionDirection == Direction.Right)
                {
                    Direction = Direction.Up;
                    stepsFromBlob = YPos - blobsYPosition;
                }
                else if (_lastCollisionDirection == Direction.UpRight)
                {
                    if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = Direction.Right;
                        stepsFromBlob = blobsXPosition - XPos;
                    }
                    else
                    {
                        Direction = Direction.Up;
                        stepsFromBlob = YPos - blobsYPosition;
                    }
                }
            }
            else if (XPos >= blobsXPosition && YPos >= blobsYPosition)
            {
                var distanceX = XPos - blobsXPosition;
                var distanceY = YPos - blobsYPosition;

                if (distanceX > distanceY)
                {
                    Direction = Direction.Left;
                    stepsFromBlob = XPos - blobsXPosition;
                }
                else if (distanceY > distanceX)
                {
                    Direction = Direction.Up;
                    stepsFromBlob = YPos - blobsYPosition;
                }

                if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == Direction.Left)
                    {
                        Direction = Direction.UpLeft;
                        stepsFromBlob = XPos - blobsXPosition;
                    }
                    else if (Direction == Direction.Up)
                    {
                        Direction = Direction.UpLeft;
                        stepsFromBlob = YPos - blobsYPosition;
                    }
                }

                if (_lastCollisionDirection == Direction.Up)
                {
                    Direction = Direction.Left;
                    stepsFromBlob = XPos - blobsXPosition;
                }
                else if (_lastCollisionDirection == Direction.Left)
                {
                    Direction = Direction.Up;
                    stepsFromBlob = YPos - blobsYPosition;
                }
                else if (_lastCollisionDirection == Direction.UpLeft)
                {
                    if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = Direction.Left;
                        stepsFromBlob = XPos - blobsXPosition;
                    }
                    else
                    {
                        Direction = Direction.Up;
                        stepsFromBlob = YPos - blobsYPosition;
                    }
                }
            }
            else if (XPos >= blobsXPosition && YPos <= blobsYPosition)
            {
                var distanceX = XPos - blobsXPosition;
                var distanceY = blobsYPosition - YPos;

                if (distanceX > distanceY)
                {
                    Direction = Direction.Left;
                    stepsFromBlob = XPos - blobsXPosition;
                }
                else if (distanceY > distanceX)
                {
                    Direction = Direction.Down;
                    stepsFromBlob = blobsYPosition - YPos;
                }

                if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == Direction.Left)
                    {
                        Direction = Direction.DownLeft;
                        stepsFromBlob = XPos - blobsXPosition;
                    }
                    else if (Direction == Direction.Down)
                    {
                        Direction = Direction.DownLeft;
                        stepsFromBlob = blobsYPosition - YPos;
                    }
                }

                if (_lastCollisionDirection == Direction.Down)
                {
                    Direction = Direction.Left;
                    stepsFromBlob = XPos - blobsXPosition;
                }
                else if (_lastCollisionDirection == Direction.Left)
                {
                    Direction = Direction.Down;
                    stepsFromBlob = blobsYPosition - YPos;
                }
                else if (_lastCollisionDirection == Direction.DownLeft)
                {
                    if (GameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = Direction.Left;
                        stepsFromBlob = XPos - blobsXPosition;
                    }
                    else
                    {
                        Direction = Direction.Down;
                        stepsFromBlob = blobsYPosition - YPos;
                    }
                }
            }
        }
        else
        {
            Direction = Direction.None;
        }

        if (!_spikeyPause)
        {
            _stepsToMove = GameEngine.RandomNumberGenerator.Next(
                              (int)(stepsFromBlob / (double)MoveSpeed / 2),
                              (int)(stepsFromBlob / (double)MoveSpeed)) +
                          10; // An additional 10 to stop the spiky lads from getting stuck on scenary
        }
        else
        {
            _stepsToMove = GameEngine.RandomNumberGenerator.Next(40, 80);
        }
    }

    private void MoveNastyOneStep(ref int amountToMoveX, ref int amountToMoveY)
    {
        _lastCollisionDirection = Direction.None;

        if (Direction == Direction.Down)
        {
            amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);

            if (amountToMoveY == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            YPos = YPos + amountToMoveY;
        }
        else if (Direction == Direction.Up)
        {
            amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);

            if (amountToMoveY == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            YPos = YPos - amountToMoveY;
        }
        else if (Direction == Direction.Left)
        {
            amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);

            if (amountToMoveX == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            XPos = XPos - amountToMoveX;
        }
        else if (Direction == Direction.Right)
        {
            amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);

            if (amountToMoveX == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            XPos = XPos + amountToMoveX;
        }
        else if (Direction == Direction.UpLeft)
        {
            amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);
            amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, XPos, YPos - amountToMoveY,
                NastyWidth, NastyHeight, MoveSpeed, false);

            if (amountToMoveY == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            YPos = YPos - amountToMoveY;
            XPos = XPos - amountToMoveX;
        }
        else if (Direction == Direction.DownLeft)
        {
            amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);
            amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Left, XPos, YPos + amountToMoveY,
                NastyWidth, NastyHeight, MoveSpeed, false);

            if (amountToMoveY == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            YPos = YPos + amountToMoveY;
            XPos = XPos - amountToMoveX;
        }
        else if (Direction == Direction.UpRight)
        {
            amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Up, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);
            amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, XPos, YPos - amountToMoveY,
                NastyWidth, NastyHeight, MoveSpeed, false);

            if (amountToMoveY == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            YPos = YPos - amountToMoveY;
            XPos = XPos + amountToMoveX;
        }
        else if (Direction == Direction.DownRight)
        {
            amountToMoveY = GameEngine.GameScreens.CheckCollision(Direction.Down, XPos, YPos, NastyWidth,
                NastyHeight, MoveSpeed, false);
            amountToMoveX = GameEngine.GameScreens.CheckCollision(Direction.Right, XPos, YPos + amountToMoveY,
                NastyWidth, NastyHeight, MoveSpeed, false);

            if (amountToMoveY == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                _stepsMovedCounter = _stepsToMove;
            }

            YPos = YPos + amountToMoveY;
            XPos = XPos + amountToMoveX;
        }

        if (Direction == Direction.Left)
        {
            if (amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.Left;
            }
        }
        else if (Direction == Direction.Right)
        {
            if (amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.Right;
            }
        }
        else if (Direction == Direction.Up)
        {
            if (amountToMoveY == 0)
            {
                _lastCollisionDirection = Direction.Up;
            }
        }
        else if (Direction == Direction.Down)
        {
            if (amountToMoveY == 0)
            {
                _lastCollisionDirection = Direction.Down;
            }
        }
        else if (Direction == Direction.UpLeft)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.UpLeft;
            }
            else if (amountToMoveY == 0)
            {
                _lastCollisionDirection = Direction.Up;
            }
            else if (amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.Left;
            }
        }
        else if (Direction == Direction.UpRight)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.UpRight;
            }
            else if (amountToMoveY == 0)
            {
                _lastCollisionDirection = Direction.Up;
            }
            else if (amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.Right;
            }
        }
        else if (Direction == Direction.DownLeft)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.DownLeft;
            }
            else if (amountToMoveY == 0)
            {
                _lastCollisionDirection = Direction.Down;
            }
            else if (amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.Left;
            }
        }
        else if (Direction == Direction.DownRight)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.DownRight;
            }
            else if (amountToMoveY == 0)
            {
                _lastCollisionDirection = Direction.Down;
            }
            else if (amountToMoveX == 0)
            {
                _lastCollisionDirection = Direction.Right;
            }
        }

        if (!_constantMovement)
        {
            if (_lastCollisionDirection != Direction.None)
            {
                _stepsMovedCounter = _stepsToMove;
            }
            else
            {
                _stepsMovedCounter++;
            }
        }
    }

    public void PerformPostChangeScreenActions()
    {
        // Stop the explosion effect leaking onto the next screen
        if (_nastyDies != null)
        {
            _nastyDies.Stop();
        }

        if (_nastyDiesType2 != null)
        {
            _nastyDiesType2.Stop();
        }

        if (NastyState == NastyState.Disabled)
        {
            NastyState = NastyState.Conception;
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        CheckSpritesInUse();

        var nastyImage = (int)_nastyType * 4;

        if (_nastyType == NastyType.CoreGuardian)
        {
            _nastySprite.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

            // Choose the image
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(255, 255, 255, (byte)screenFade), 1.0f, 4f, true, GameEngine.Paused);
            _nastySprite.End();
        }
        else
        {
            if (NastyState == NastyState.Birth)
            {
                _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

                // Choose the image
                _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                    ColourHelper.GetXna4Colour((byte)_birthColourLevel, (byte)_birthColourLevel, 255,
                        (byte)(255 * ((double)screenFade / 255))), _birthScale, _birthRotateSpeed,
                    true, GameEngine.Paused);
                _nastySprite.End();
            }
            else if (NastyState == NastyState.Normal)
            {
                // Choose the image
                if (Smoking)
                {
                    _nastySprite.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(50, 50, 50, (byte)screenFade));
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(255, 0, 0, (byte)screenFade));
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(0, 100, 0, (byte)screenFade));
                }
                else
                {
                    _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(255, 255, 255, (byte)screenFade));
                }

                _nastySprite.End();
            }
            else if (NastyState == NastyState.Dead)
            {
                // Draw the explosion
                if (_nastyDies != null)
                {
                    _nastyDies.Draw();
                }

                if (_nastyDiesType2 != null)
                {
                    _nastyDiesType2.Draw();
                }

                // Fade the nasty out as it explodes
                if (_deathFadeLevel > 0)
                {
                    _deathFadeLevel = _deathFadeLevel - 20;
                }

                if (_deathFadeLevel < 0)
                {
                    _deathFadeLevel = 0;
                }

                // Shrink the nasty as it explodes
                if (_deathScale > 0)
                {
                    _deathScale = _deathScale - 0.01f;
                }

                if (_deathScale < 0)
                {
                    _deathScale = 0;
                }

                if (_deathFadeLevel > 0)
                {
                    _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(255, 255, 255,
                            (byte)(_deathFadeLevel * ((double)screenFade / 255))), _deathScale, 16f,
                        _deathAnticlockwise, GameEngine.Paused);
                    _nastySprite.End();
                }

                if (GameEngine.UsingOldGraphics)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                        _nastyDiesAnimation.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(255, 255, 255, screenFade), 1f, 0f);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                }
            }
        }
    }

    #endregion

}