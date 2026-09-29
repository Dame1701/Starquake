// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectMercury;
using StarquakeXNA.Animations;
using StarquakeXNA.Collisions;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA.ActiveSprites;

// This wraps multiple nasties
public class Nasties : GameComponentBase
{
    private readonly List<Nasty> _nasties;
    private readonly int _nastyCount = 1;
    private readonly IPositionalTriggerParticleEffect _nastySmokes;
    private int _previousScreenVisited;

    public Nasties(GameEngine gameEngineReference, int nastyCount)
        : base(gameEngineReference)
    {
        _nastyCount = nastyCount;
        _nasties = new List<Nasty>();
        _nastySmokes =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

        // Create double the number needed for screen swap operations
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties.Add(
                new Nasty(
                    gameEngineReference,
                    0,
                    0,
                    3));
        }
    }
    public event RaiseInteractionWithBlob OnNastyInteractionWithBlob;

    #region IGameComponent Members

    public void LoadContent(ContentList content)
    {
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties[a].LoadContent(content);
        }
    }

    public void Initialise()
    {
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties[a].Initialise();
        }

        // Maybe we should call this on start of the game for all involved....?
        PerformPostChangeScreenActions();
    }

    public void Reset()
    {
        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].NastyState = NastyState.PreConception;
        }

        PerformPostChangeScreenActions();
    }

    public override void Update(GameTime gameTime)
    {
        if (GameEngine.Player.BlobsState == BlobState.Normal)
        {
            // Cancel any nasty collision effect
            GameEngine.Player.BlobTouchedByNasty = false;

            if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                for (var a = 0; a < _nastyCount; a++)
                {
                    _nasties[a].NastyState = NastyState.Dead;
                }
            }

            // Flag that's set if a nasty is touching Blob already - stops all of them zapping his energy at once which to too quick and unfair
            var nastyInteractingWithBlob = false;
            var nastySpawnedCounter = 0;

            for (var a = 0; a < _nastyCount; a++)
            {
                if (_nasties[a].NastyState == NastyState.Normal)
                {
                    nastySpawnedCounter++;
                }

                // Setting the coordinates to a setting less than 0 will suspect all updates
                if (_nasties[a].XPos > -1000 &&
                    _nasties[a].YPos > -1000)
                {
                    _nasties[a].Update(gameTime);

                    // Check for a collision between a nasty and Blob and raise an event
                    // Since we have a reference to Blob directly, we can send the call to animate contact directly
                    // We raise the event to affect the status panel
                    // Going to move the offsets etc. in a bit so it's not too precise and unfair!
                    if (CollisionDetection.Check(
                            GameEngine.Player.BlobXPos + 8,
                            GameEngine.Player.BlobYPos + 8,
                            GameEngine.Player.BlobWidth - 16,
                            GameEngine.Player.BlobHeight - 16,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight))
                    {
                        if (_nasties[a].NastyState == NastyState.Normal)
                        {
                            // Raise the event
                            if (OnNastyInteractionWithBlob != null)
                            {
                                if (!nastyInteractingWithBlob)
                                {
                                    nastyInteractingWithBlob = true;
                                    OnNastyInteractionWithBlob(NastyInterationType.HitBlob, _nasties[a].NastyType);
                                }
                            }

                            // Trigger the blob animation
                            GameEngine.Player.BlobTouchedByNasty = true;
                        }
                    }

                    // Has blob shot a nasty
                    if (GameEngine.Player.BlobHasFired)
                    {
                        if (CollisionDetection.Check(
                                GameEngine.Player.BlobFireX,
                                GameEngine.Player.BlobFireY,
                                32,
                                32,
                                _nasties[a].XPos,
                                _nasties[a].YPos,
                                _nasties[a].NastyWidth,
                                _nasties[a].NastyHeight))
                        {
                            if (_nasties[a].NastyState == NastyState.Normal ||
                                _nasties[a].NastyState == NastyState.Birth)
                            {
                                GameEngine.Player.RegisterFireHit();
                                _nasties[a].NastyState = NastyState.Shot;

                                if (_nasties[a].NastyType == NastyType.Spiky)
                                {
                                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobExplodes");
                                }
                                else
                                {
                                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("NastyDeath");
                                }

                                if (OnNastyInteractionWithBlob != null)
                                {
                                    OnNastyInteractionWithBlob(NastyInterationType.BlobShot,
                                        _nasties[a].NastyType);
                                }
                            }
                        }
                    }

                    // Has the nasty hit an electricity gate...?
                    _nasties[a].Smoking = false;

                    if (GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers != null)
                    {
                        for (var b = 0;
                             b < GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers.Length;
                             b++)
                        {
                            if (GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].ElectricityOn)
                            {
                                if (CollisionDetection.Check(
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .X,
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .Y,
                                        128,
                                        32,
                                        _nasties[a].XPos,
                                        _nasties[a].YPos,
                                        _nasties[a].NastyWidth,
                                        _nasties[a].NastyHeight))
                                {
                                    var nastyTravelDirection = CollisionDetection.CheckCollisionDirection(
                                        _nasties[a].Speed,
                                        _nasties[a].XPos,
                                        _nasties[a].YPos,
                                        _nasties[a].NastyWidth,
                                        _nasties[a].NastyHeight,
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .X,
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .Y,
                                        128,
                                        32);

                                    if (nastyTravelDirection != Direction.None &&
                                        _nasties[a].NastyState == NastyState.Normal)
                                    {
                                        if (_nastySmokes != null)
                                        {
                                            _nasties[a].Smoking = true;
                                            _nastySmokes.Start(
                                                SpriteHelpers.GetSpriteLocationOffset(_nasties[a].Direction,
                                                    _nasties[a].NastyWidth, _nasties[a].NastyHeight).X +
                                                _nasties[a].XPos,
                                                SpriteHelpers.GetSpriteLocationOffset(_nasties[a].Direction,
                                                    _nasties[a].NastyWidth, _nasties[a].NastyHeight).Y +
                                                _nasties[a].YPos);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (nastySpawnedCounter == _nastyCount)
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
            }

            if (_nastySmokes != null)
            {
                _nastySmokes.Update(gameTime);
            }
        }
        else
        {
            Reset();
        }
    }

    private void Swap(List<Nasty> list, int a, int b)
    {
        (list[a], list[b]) = (list[b], list[a]);
    }

    public void PerformPostChangeScreenActions()
    {
        if (GameEngine.GameScreens.ScreenNumber != _previousScreenVisited)
        {
            // Save the nasties on the screen we have left into the previous screen array
            for (var a = 0; a < _nastyCount; a++)
            {
                // Make sure they don't stay dead when we leave
                if (_nasties[a].NastyState == NastyState.Dead ||
                    _nasties[a].NastyState == NastyState.Shot)
                {
                    _nasties[a].NastyState = NastyState.PreConception;
                }

                _nasties[a + _nastyCount].CopyFromSource(_nasties[a]);
            }
        }

        // Do we have nasties from the previous screen...?
        if (GameEngine.GameScreens.ScreenNumber == _previousScreenVisited)
        {
            // Swap them all around
            for (var a = 0; a < _nastyCount; a++)
            {
                Swap(_nasties, a, a + _nastyCount);
            }
        }

        // Nope so we are going to create a batch from scratch
        if (GameEngine.GameScreens.ScreenNumber != _previousScreenVisited)
        {
            // Do we generate all nasties the same
            if (GameEngine.RandomNumberGenerator.Next(0, 4) == 0)
            {
                GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame = true;
            }
            else
            {
                GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame = false;
            }

            var nastyType = NastyType.Ghost;
            if (GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
            {
                nastyType = Nasty.GetRandomNastyType(GameEngine, true);
            }

            // Reset the nasties
            for (var a = 0; a < _nastyCount; a++)
            {
                _nasties[a].KeepNastyTheSame = GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame;
                _nasties[a].NastyState = NastyState.PreConception;

                if (GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
                {
                    _nasties[a].KeepNastyTheSame = true;
                    _nasties[a].NastyType = nastyType;
                }
            }
        }

        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].PerformPostChangeScreenActions();
        }

        // Deactivate some nasties if we have conveyer nasties on screen
        if (GameEngine.ConveyerNastiesInstance.NastiesActive > 0)
        {
            for (var a = 0; a < GameEngine.ConveyerNastiesInstance.NastiesActive; a++)
            {
                _nasties[a].NastyState = NastyState.Disabled;
            }
        }

        // Reset the nasty smoke effect
        if (_nastySmokes != null)
        {
            _nastySmokes.Reset();
        }

        _previousScreenVisited = GameEngine.GameScreens.PreviousScreenNumber;
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (GameEngine.Player.BlobsState == BlobState.Normal)
        {
            for (var a = 0; a < _nastyCount; a++)
            {
                if (_nasties[a].XPos > -1000 &&
                    _nasties[a].YPos > -1000)
                {
                    _nasties[a].Draw(gameTime, screenFade);
                }
            }

            if (_nastySmokes != null)
            {
                _nastySmokes.Draw(screenFade);
            }
        }
    }

    #endregion

}

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

public class NastyHelpers
{
    public static void ApplyNastyColours(NastyType nastyType, ParticleEffect effect)
    {
        if (nastyType == NastyType.FriedEgg)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(125, 62, 62).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(0, 182, 0).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Gonzo)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(4, 82, 184).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(182, 103, 10).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Cushion)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(59, 194, 42).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(227, 66, 19).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Helmet)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(202, 109, 17).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(137, 137, 137).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Shellfish)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(239, 238, 0).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(198, 99, 0).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Snail)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 124, 0).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(141, 141, 141).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(166, 198, 0).ToVector3();
        }
        else if (nastyType == NastyType.CurleyWurley)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(168, 163, 252).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(109, 109, 253).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Bird)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 0, 211).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(148, 148, 217).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(188, 62, 0).ToVector3();
        }
        else if (nastyType == NastyType.Ghost)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 151, 43).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(88, 0, 88).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (nastyType == NastyType.Scorpian)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(177, 8, 8).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(130, 2, 2).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(230, 230, 230).ToVector3();
        }
        else if (nastyType == NastyType.DragonFly)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(8, 150, 219).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(50, 111, 85).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(76, 191, 142).ToVector3();
        }
        else if (nastyType == NastyType.Helicopter)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(177, 8, 8).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(130, 2, 2).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(230, 230, 230).ToVector3();
        }
        else if (nastyType == NastyType.Hippo)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(229, 120, 252).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(137, 115, 251).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(145, 49, 50).ToVector3();
        }
        else if (nastyType == NastyType.Centipede)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(87, 165, 28).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(110, 6, 124).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(221, 134, 38).ToVector3();
        }
    }
}