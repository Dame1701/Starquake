// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
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

    /// <summary>
    ///     The nasty's walk cycle. It plays back and forth rather than looping, which is what gives
    ///     the legs their scuttle.
    /// </summary>
    private const int NastyLastFrame = 3;

    /// <inheritdoc cref="NastyLastFrame" />
    private const int NastyAnimationDelay = 4;

    /// <summary>
    ///     How fast the nasty spins as it materialises, and how fast it is still spinning by the time
    ///     it has finished. It slows from the one to the other over the second half of the birth.
    /// </summary>
    private const float BirthStartRotateSpeed = 8.0f;

    /// <inheritdoc cref="BirthStartRotateSpeed" />
    private const float BirthRespawnRotateSpeed = 32.0f;

    /// <inheritdoc cref="BirthStartRotateSpeed" />
    private const float BirthEndRotateSpeed = 16f;

    /// <summary>
    ///     The colours only fade in over the back half of the birth, so both fades count up at twice
    ///     the rate the birth itself runs at.
    /// </summary>
    private const int BirthColourFadeHalf = 2;

    /// <summary>
    ///     How long a nasty lies dead before it is allowed to come back, and the longest it then
    ///     waits before doing so. A nasty is never gone for good - it is only ever biding its time.
    /// </summary>
    private const int DeadPauseFrames = 50;

    /// <inheritdoc cref="DeadPauseFrames" />
    private const int LongestConceptionWait = 500;

    /// <inheritdoc cref="DeadPauseFrames" />
    private const int ShortestConceptionWaitOnCoreScreen = 60;

    /// <summary>
    ///     How long a nasty keeps to one frame of mind before picking another. It chases the blob for
    ///     longer than it wanders, so the two ranges are different.
    /// </summary>
    private const int WanderingShortest = 200;

    /// <inheritdoc cref="WanderingShortest" />
    private const int WanderingLongest = 500;

    /// <inheritdoc cref="WanderingShortest" />
    private const int ChasingShortest = 400;

    /// <inheritdoc cref="WanderingShortest" />
    private const int ChasingLongest = 700;

    /// <summary>
    ///     A newly born nasty crawls at one pixel a frame for this long before it picks up speed, so
    ///     it can be seen coming.
    /// </summary>
    private const int FirstMovementDuration = 90;

    /// <summary>
    ///     How far a nasty travels before choosing a new direction. The ranges differ by movement
    ///     type: a bouncing nasty commits to longer runs than a wandering one.
    /// </summary>
    private const int ShortestRun = 8;

    /// <inheritdoc cref="ShortestRun" />
    private const int ShortestChasingRun = 10;

    /// <inheritdoc cref="ShortestRun" />
    private const int LongestRun = 40;

    /// <inheritdoc cref="ShortestRun" />
    private const int ShortestBouncingRun = 40;

    /// <inheritdoc cref="ShortestRun" />
    private const int LongestBouncingRun = 80;

    /// <summary>
    ///     The shortest runs of the lot, which is what makes a nasty on
    ///     <see cref="MoveType.RandomMovement" /> scuttle rather than stride.
    /// </summary>
    private const int ShortestScuttle = 1;

    /// <inheritdoc cref="ShortestScuttle" />
    private const int LongestScuttle = 4;

    /// <summary>
    ///     Added to the run a spiky nasty sets itself so that it goes a little past the blob. Without
    ///     it they get stuck on the scenery.
    /// </summary>
    private const int SpikeyOvershoot = 10;

    /// <summary>
    ///     How quickly a dead nasty shrinks away, and how fast the corpse spins as it goes.
    /// </summary>
    private const float DeathShrinkStep = 0.01f;

    /// <inheritdoc cref="DeathShrinkStep" />
    private const float DeathRotateSpeed = 16f;

    /// <summary>
    ///     How fast a live nasty rocks from side to side as it walks.
    /// </summary>
    private const float NastyRockSpeed = 4f;

    /// <summary>
    ///     The colours the different nasties are tinted: grey for one that has been shot, red for a
    ///     spiky one, green for a conveyer one.
    /// </summary>
    private const int ShotNastyGrey = 50;

    /// <inheritdoc cref="ShotNastyGrey" />
    private const int ConveyerNastyGreen = 100;

    // Animation
    private readonly Animation _nastyAnim;

    private bool _attractedToBlob; // They got through phases of being attracted to Blob
    private int _birthColourLevel;

    // Birth stuff
    private int _birthFadeLevel;
    private float _birthRotateSpeed = BirthStartRotateSpeed;

    private float _birthScale;
    private int _birthTimer;

    // Counters
    private int _conceptionwaitCounter;

    private bool _constantMovement; // Movement type that doesn't rely on a number of steps
    private bool _deathAnticlockwise;

    // Death stuff
    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;
    private bool _firstMovement;

    private Direction _lastCollisionDirection;
    private int _movementChangeCount;
    private int _movementChangeCounter;

    // Behaviour

    private int _nastyDeadPauseCounter;
    private readonly IStandardParticleEffect _nastyDies;

    private readonly Animation _nastyDiesAnimation;
    private readonly IStandardParticleEffect _nastyDiesType2;
    //bool attractedToBlobFlag;


    // When shot monsters usually dissappear for good but there is a small chance they'll regenerate
    // Tech stuff
    private SpriteCollection _nastySprite;

    private readonly SpriteCollection _nastySpriteNew;
    private readonly SpriteCollection _nastySpriteOriginal;

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
        _nastyAnim.LastFrame = NastyLastFrame;
        _nastyAnim.Repeat = true;
        _nastyAnim.AnimateForwards = true;
        _nastyAnim.Yoyo = true;
        _nastyAnim.Delay = NastyAnimationDelay;

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

        _nastyDies = (IStandardParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("NastyDies",
            ParticleEffectType.Triggered, null);
        _nastyDies.ParticleEffectComplete += nastyDies_ParticleEffectComplete;

        _nastyDiesType2 =
            (IStandardParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("NastySplat",
                ParticleEffectType.Triggered, null);
        _nastyDiesType2.ParticleEffectComplete += nastyDies_ParticleEffectComplete;
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

    public int NastyWidth { get; } = SpriteMetrics.SpriteWidth;

    public NastyState NastyState { get; set; }

    public int NastyHeight { get; } = SpriteMetrics.SpriteHeight;

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
            return new Point(potentialLocations[nastyLocationIndex].X + SpriteMetrics.HalfSpriteWidth,
                potentialLocations[nastyLocationIndex].Y + 16);
        }

        // this should never happen
        return new Point(0, 0);
    }

    [MemberNotNull(nameof(_nastySprite))]
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
            _conceptionwaitCounter = GameEngine.RandomNumberGenerator.Next(0, LongestConceptionWait);
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
        if (GameEngine.GameScreens.ScreenNumber == WorldMap.CoreApproachScreenNumber)
        {
            NastyState = NastyState.Disabled;
        }

        if (GameEngine.GameScreens.ScreenNumber == WorldMap.CoreScreenNumber)
        {
            _nastyType = NastyType.CoreGuardian;
            _attractedToBlob = false;
        }

        if (NastyState == NastyState.PreConception)
        {
            BeginConception();
        }
        else if (NastyState == NastyState.Conception)
        {
            WaitToBeBorn();
        }
        else if (NastyState == NastyState.Birth)
        {
            UpdateBirth();
        }
        else if (NastyState == NastyState.Shot)
        {
            StartDying();
        }
        else if (NastyState == NastyState.Dead)
        {
            UpdateDeath();
        }

        UpdateDeathEffects(gameTime);

        if (!ShouldAnimate())
        {
            return;
        }

        _nastyAnim.GetNextFrame();

        ReleaseAttractionIfStuck();
        MoveNasty();
        UpdateFrameOfMind();
    }

    /// <summary>
    ///     Picks how long this nasty waits to be conceived, and what kind of nasty it is going to be
    ///     when it gets there.
    /// </summary>
    private void BeginConception()
    {
        _conceptionwaitCounter = GameEngine.RandomNumberGenerator.Next(0, ShortestConceptionWaitOnCoreScreen);
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

    private void WaitToBeBorn()
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
            _deathFadeLevel = ColourLevels.Full;
            _deathScale = 1f;
            _birthFadeLevel = 0;
            _birthScale = 0f;
            _birthColourLevel = 0;
            _birthRotateSpeed = BirthRespawnRotateSpeed;
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

    private void UpdateBirth()
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
        if (_birthFadeLevel < ColourLevels.Full)
        {
            _birthFadeLevel =
                _birthFadeLevel +
                (int)(ColourLevels.Full /
                      ((double)BirthPeriod /
                       BirthColourFadeHalf)); // We only have half the time to do the colour fade in 
        }


        // Once we're halfway through fade in the colours
        if (_birthTimer > BirthPeriod / BirthColourFadeHalf)
        {
            if (_birthColourLevel < ColourLevels.Full)
            {
                _birthColourLevel =
                    _birthColourLevel +
                    (int)(ColourLevels.Full /
                          ((double)BirthPeriod /
                           BirthColourFadeHalf)); // We only have half the time to do the colour fade in 
            }

            if (_birthRotateSpeed > BirthEndRotateSpeed)
            {
                _birthRotateSpeed = _birthRotateSpeed - BirthEndRotateSpeed / ((float)BirthPeriod / BirthColourFadeHalf);
            }
        }

        if (_birthFadeLevel > ColourLevels.Full)
        {
            _birthFadeLevel = ColourLevels.Full;
        }

        if (_birthColourLevel > ColourLevels.Full)
        {
            _birthColourLevel = ColourLevels.Full;
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

    private void StartDying()
    {
        _deathFadeLevel = ColourLevels.Full;
        _deathScale = 1.0f;

        if (_nastyType == NastyType.Spiky ||
            _nastyType == NastyType.Conveyer)
        {
            _nastyDies.Start();
        }
        else
        {
            NastyHelpers.ApplyNastyColours(_nastyType, _nastyDiesType2.Effect);
            _nastyDiesType2.Start();
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

    private void UpdateDeath()
    {
        if (GameEngine.UsingOldGraphics)
        {
            _nastyDiesAnimation.GetNextFrame();

            if (_nastyDeadPauseCounter < DeadPauseFrames)
            {
                _nastyDeadPauseCounter++;
            }
            else
            {
                NastyState = NastyState.PreConception;
                _conceptionwaitCounter = GameEngine.RandomNumberGenerator.Next(0, LongestConceptionWait);
                _nastyDeadPauseCounter = 0;
            }
        }
    }

    private void UpdateDeathEffects(GameTime gameTime)
    {
        if (_nastyDies != null)
        {
            // Update the explosion
            _nastyDies.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        }

        if (_nastyDiesType2 != null)
        {
            // Update the splat
            _nastyDiesType2.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        }
    }

    /// <summary>
    ///     A nasty is only animated once it is fully real - or a quarter of the way through being born,
    ///     which is where its legs start showing.
    /// </summary>
    private bool ShouldAnimate()
    {
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

        return updateAnimation;
    }

    /// <summary>
    ///     A nasty chasing the blob with something in the way cannot move at all, so it gives up on the
    ///     chase after a while rather than grinding against the scenery for ever.
    /// </summary>
    private void ReleaseAttractionIfStuck()
    {
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
    }

    private void MoveNasty()
    {
        var amountToMoveX = 0;
        var amountToMoveY = 0;

        if (_nastyType == NastyType.CoreGuardian)
        {
            MoveNastyOneStep(ref amountToMoveX, ref amountToMoveY);
            DiagonalBouncing();
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
                    DiagonalBouncing();
                }
                else if (MovementType == MoveType.DiagonalBouncingInSteps)
                {
                    _constantMovement = false;
                    DiagonalBouncingInSteps();
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
    }

    /// <summary>
    ///     How long a nasty keeps to one frame of mind before picking another, and what it picks: whether
    ///     it chases the blob or wanders, how it moves, which way it sets off and how fast.
    /// </summary>
    private void UpdateFrameOfMind()
    {
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
                    _movementChangeCount = GameEngine.RandomNumberGenerator.Next(WanderingShortest, WanderingLongest);
                    _attractedToBlob = false;
                }
                else
                {
                    _movementChangeCount = GameEngine.RandomNumberGenerator.Next(ChasingShortest, ChasingLongest);
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
                        _movementChangeCount = FirstMovementDuration;
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

    private void DiagonalBouncingInSteps()
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomDiagonal();
        }

        // Hitting nothing still turns it away vertically, which is what keeps a nasty set going in
        // steps from running in a straight line for ever.
        Direction = Reflected(Direction, _lastCollisionDirection) ?? TurnedAwayVertically(Direction);
    }

    private void DiagonalBouncing()
    {
        if (Direction == Direction.None)
        {
            Direction = RandomDiagonal();
        }

        if (Reflected(Direction, _lastCollisionDirection) is { } reflected)
        {
            Direction = reflected;
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(ShortestRun, LongestRun);
    }

    /// <summary>
    ///     Which diagonal a nasty comes off on, given the one it was travelling on and what it just
    ///     hit, or null if what it hit does not turn it. Walking into the corner sends it straight
    ///     back the way it came; walking into the wall on one side flips only that side.
    /// </summary>
    private static Direction? Reflected(Direction travelling, Direction hit)
        => (travelling, hit) switch
        {
            (Direction.UpLeft, Direction.UpLeft) => Direction.DownRight,
            (Direction.UpLeft, Direction.Left) => Direction.UpRight,
            (Direction.UpLeft, Direction.Up) => Direction.DownLeft,
            (Direction.UpRight, Direction.UpRight) => Direction.DownLeft,
            (Direction.UpRight, Direction.Right) => Direction.UpLeft,
            (Direction.UpRight, Direction.Up) => Direction.DownRight,
            (Direction.DownLeft, Direction.DownLeft) => Direction.UpRight,
            (Direction.DownLeft, Direction.Left) => Direction.DownRight,
            (Direction.DownLeft, Direction.Down) => Direction.UpLeft,
            (Direction.DownRight, Direction.DownRight) => Direction.UpLeft,
            (Direction.DownRight, Direction.Right) => Direction.DownLeft,
            (Direction.DownRight, Direction.Down) => Direction.UpRight,
            _ => null
        };

    /// <summary>
    ///     The same diagonal with its up or down half flipped. Anything that is not a diagonal is
    ///     left alone, which is what the four bouncing ladders used to do by falling off the end.
    /// </summary>
    private static Direction TurnedAwayVertically(Direction direction)
        => direction switch
        {
            Direction.UpLeft => Direction.DownLeft,
            Direction.UpRight => Direction.DownRight,
            Direction.DownLeft => Direction.UpLeft,
            Direction.DownRight => Direction.UpRight,
            _ => direction
        };

    private void RandomUpDownLeftRight(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomStraightDirection();
        }

        BounceStraightBack();

        if (_attractedToBlob)
        {
            HeadTowardsBlob(blobsXPosition, blobsYPosition);
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(ShortestRun, LongestRun);
    }

    /// <summary>
    ///     The same as <see cref="RandomUpDownLeftRight" /> but in much shorter runs, and it forgets
    ///     which way it was going before it starts chasing. That second difference matters: with no
    ///     direction to carry in, a nasty level with the blob heads straight at him rather than
    ///     turning the vertical move into a diagonal.
    /// </summary>
    private void RandomMovement(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomStraightDirection();
        }

        BounceStraightBack();

        if (_attractedToBlob)
        {
            Direction = Direction.None;
            HeadTowardsBlob(blobsXPosition, blobsYPosition);
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(ShortestScuttle, LongestScuttle);
    }

    /// <summary>
    ///     Turns a nasty that has walked straight into something back the way it came, and leaves one
    ///     that has hit nothing or hit a corner alone.
    /// </summary>
    private void BounceStraightBack()
        => Direction = _lastCollisionDirection switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction
        };

    /// <summary>
    ///     Points a nasty at the blob - diagonally when he is off in both axes - while refusing to
    ///     turn into whatever it has just walked into.
    /// </summary>
    private void HeadTowardsBlob(int blobsXPosition, int blobsYPosition)
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
                Direction = Direction switch
                {
                    Direction.Left => Direction.DownLeft,
                    Direction.Right => Direction.DownRight,
                    _ => Direction.Down
                };
            }
        }
        else if (YPos > blobsYPosition)
        {
            if (_lastCollisionDirection != Direction.Up)
            {
                Direction = Direction switch
                {
                    Direction.Left => Direction.UpLeft,
                    Direction.Right => Direction.UpRight,
                    _ => Direction.Up
                };
            }
        }
    }

    private void RandomMovementsAllDirections(int blobsXPosition, int blobsYPosition)
    {
        if (_lastCollisionDirection == Direction.None || Direction == Direction.None)
        {
            Direction = RandomAnyDirection();
        }

        var choices = _attractedToBlob
            ? WaysTowardsBlob(blobsXPosition, blobsYPosition)
            : WaysAwayFrom(_lastCollisionDirection);

        if (choices is not null)
        {
            Direction = choices[GameEngine.RandomNumberGenerator.Next(0, choices.Length)];
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(ShortestChasingRun, LongestRun);
    }

    /// <summary>
    ///     The three ways out of having walked into something from each of the eight directions, or
    ///     null when nothing was hit. Each one keeps whichever half of the direction was not blocked
    ///     and reverses the other, so the three are the straight reverse and the two diagonals
    ///     either side of it.
    /// </summary>
    private static Direction[]? WaysAwayFrom(Direction hit)
        => hit switch
        {
            Direction.Up => [Direction.Down, Direction.DownLeft, Direction.DownRight],
            Direction.Down => [Direction.Up, Direction.UpLeft, Direction.UpRight],
            Direction.Left => [Direction.Right, Direction.DownRight, Direction.UpRight],
            Direction.Right => [Direction.Left, Direction.DownLeft, Direction.UpLeft],
            Direction.UpLeft => [Direction.Down, Direction.DownLeft, Direction.DownRight],
            Direction.UpRight => [Direction.Down, Direction.DownRight, Direction.DownLeft],
            Direction.DownLeft => [Direction.Up, Direction.UpLeft, Direction.UpRight],
            Direction.DownRight => [Direction.Up, Direction.UpRight, Direction.UpLeft],
            _ => null
        };

    /// <summary>
    ///     The three ways towards the blob from whichever quadrant he is in: straight across,
    ///     straight up or down, and the diagonal between them. The tests are deliberately inclusive
    ///     on both sides so that a nasty level with the blob in one axis still has a quadrant, which
    ///     is why this never has to answer "nowhere".
    /// </summary>
    private Direction[] WaysTowardsBlob(int blobsXPosition, int blobsYPosition)
    {
        if (XPos <= blobsXPosition && YPos <= blobsYPosition)
        {
            return [Direction.Right, Direction.Down, Direction.DownRight];
        }

        if (XPos <= blobsXPosition && YPos >= blobsYPosition)
        {
            return [Direction.Right, Direction.Up, Direction.UpRight];
        }

        if (XPos >= blobsXPosition && YPos >= blobsYPosition)
        {
            return [Direction.Left, Direction.Up, Direction.UpLeft];
        }

        return [Direction.Left, Direction.Down, Direction.DownLeft];
    }

    /// <summary>One of the four diagonals - the second half of <see cref="Direction" />.</summary>
    private Direction RandomDiagonal() => (Direction)GameEngine.RandomNumberGenerator.Next(4, 8);

    /// <summary>One of the four straight directions - the first half of <see cref="Direction" />.</summary>
    private Direction RandomStraightDirection() => (Direction)GameEngine.RandomNumberGenerator.Next(0, 4);

    /// <summary>Any of the eight, but never <see cref="Direction.None" />.</summary>
    private Direction RandomAnyDirection() => (Direction)GameEngine.RandomNumberGenerator.Next(0, 8);

    /// <summary>
    ///     A spiky nasty goes at the blob along whichever axis he is further away on, sometimes cuts
    ///     the corner diagonally instead, and if it has just walked into something it takes the other
    ///     axis. Then it rests for a while, which is the other half of the
    ///     <see cref="_spikeyPause" /> cycle and the reason it is beatable.
    /// </summary>
    /// <remarks>
    ///     This was four blocks of the same 55 lines, one per quadrant the blob can be in, with the
    ///     directions and the subtractions swapped round. What is left is the two directions that
    ///     point at him and the diagonal between them.
    /// </remarks>
    private void SpikeyMovement(int blobsXPosition, int blobsYPosition)
    {
        if (_spikeyPause)
        {
            Direction = Direction.None;
            _stepsToMove = GameEngine.RandomNumberGenerator.Next(ShortestBouncingRun, LongestBouncingRun);
            return;
        }

        var acrossToBlob = XPos <= blobsXPosition ? Direction.Right : Direction.Left;
        var alongToBlob = VerticalDirectionTowardsBlob(blobsXPosition, blobsYPosition);
        var cornerToBlob = Diagonal(acrossToBlob, alongToBlob);

        var distanceX = Math.Abs(blobsXPosition - XPos);
        var distanceY = Math.Abs(blobsYPosition - YPos);
        var stepsFromBlob = 0;

        if (distanceX > distanceY)
        {
            Direction = acrossToBlob;
            stepsFromBlob = distanceX;
        }
        else if (distanceY > distanceX)
        {
            Direction = alongToBlob;
            stepsFromBlob = distanceY;
        }

        // Half the time it cuts the corner rather than closing one axis at a time.
        if (CoinFlip())
        {
            if (Direction == acrossToBlob)
            {
                Direction = cornerToBlob;
                stepsFromBlob = distanceX;
            }
            else if (Direction == alongToBlob)
            {
                Direction = cornerToBlob;
                stepsFromBlob = distanceY;
            }
        }

        // It walked into something last step, so try a way it has not just been stopped.
        if (_lastCollisionDirection == acrossToBlob)
        {
            Direction = alongToBlob;
            stepsFromBlob = distanceY;
        }
        else if (_lastCollisionDirection == alongToBlob)
        {
            Direction = acrossToBlob;
            stepsFromBlob = distanceX;
        }
        else if (_lastCollisionDirection == cornerToBlob)
        {
            if (CoinFlip())
            {
                Direction = acrossToBlob;
                stepsFromBlob = distanceX;
            }
            else
            {
                Direction = alongToBlob;
                stepsFromBlob = distanceY;
            }
        }

        _stepsToMove = GameEngine.RandomNumberGenerator.Next(
                           (int)(stepsFromBlob / (double)MoveSpeed / 2),
                           (int)(stepsFromBlob / (double)MoveSpeed)) +
                       SpikeyOvershoot;
    }

    /// <summary>
    ///     Which of up and down points at the blob.
    /// </summary>
    /// <remarks>
    ///     The four quadrant tests this replaced were an if/else chain with inclusive bounds on both
    ///     sides, so a nasty exactly level with the blob fell into whichever arm matched first: down
    ///     if it was to his left, up if it was to his right. That is an artefact of the order they
    ///     were written in rather than anything intended, but tidying it changes what the game does,
    ///     so it is reproduced here instead.
    /// </remarks>
    private Direction VerticalDirectionTowardsBlob(int blobsXPosition, int blobsYPosition)
    {
        if (XPos <= blobsXPosition)
        {
            return YPos <= blobsYPosition ? Direction.Down : Direction.Up;
        }

        return YPos < blobsYPosition ? Direction.Down : Direction.Up;
    }

    /// <summary>The diagonal made of one horizontal and one vertical direction.</summary>
    private static Direction Diagonal(Direction across, Direction along)
        => (across, along) switch
        {
            (Direction.Right, Direction.Down) => Direction.DownRight,
            (Direction.Right, _) => Direction.UpRight,
            (_, Direction.Down) => Direction.DownLeft,
            _ => Direction.UpLeft
        };

    /// <summary>A one-in-two chance, which is how most of a nasty's mind gets made up.</summary>
    private bool CoinFlip() => GameEngine.RandomNumberGenerator.Next(0, 2) == 0;

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
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade), 1.0f, NastyRockSpeed, true, GameEngine.Paused);
            _nastySprite.End();
        }
        else
        {
            if (NastyState == NastyState.Birth)
            {
                _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

                // Choose the image
                _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                    ColourHelper.GetXna4Colour((byte)_birthColourLevel, (byte)_birthColourLevel, ColourLevels.Full,
                        (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), _birthScale, _birthRotateSpeed,
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
                        ColourHelper.GetXna4Colour(ShotNastyGrey, ShotNastyGrey, ShotNastyGrey, (byte)screenFade));
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, 0, 0, (byte)screenFade));
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(0, ConveyerNastyGreen, 0, (byte)screenFade));
                }
                else
                {
                    _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
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
                    _deathScale = _deathScale - DeathShrinkStep;
                }

                if (_deathScale < 0)
                {
                    _deathScale = 0;
                }

                if (_deathFadeLevel > 0)
                {
                    _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                            (byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))), _deathScale, DeathRotateSpeed,
                        _deathAnticlockwise, GameEngine.Paused);
                    _nastySprite.End();
                }

                if (GameEngine.UsingOldGraphics)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                        _nastyDiesAnimation.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, screenFade), 1f, 0f);
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                }
            }
        }
    }

    #endregion

}