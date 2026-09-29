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
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

// This wraps multiple nasties
public class Nasties : GameComponentBase
{
    private readonly List<Nasty> nasties;
    private readonly int nastyCount = 1;
    private readonly IPositionalTriggerParticleEffect nastySmokes;
    private int previousScreenVisited;

    public Nasties(GameEngine GameEngineReference, int NastyCount)
        : base(GameEngineReference)
    {
        nastyCount = NastyCount;
        nasties = new List<Nasty>();
        nastySmokes =
            gameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                enParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

        // Create double the number needed for screen swap operations
        for (var a = 0; a < nastyCount * 2; a++)
        {
            nasties.Add(
                new Nasty(
                    GameEngineReference,
                    0,
                    0,
                    3));
        }
    }
    public event RaiseInteractionWithBlob OnNastyInteractionWithBlob;

    #region IGameComponent Members

    public void LoadContent(ContentList Content)
    {
        for (var a = 0; a < nastyCount * 2; a++)
        {
            nasties[a].LoadContent(Content);
        }
    }

    public void Initialise()
    {
        for (var a = 0; a < nastyCount * 2; a++)
        {
            nasties[a].Initialise();
        }

        // Maybe we should call this on start of the game for all involved....?
        PerformPostChangeScreenActions();
    }

    public void Reset()
    {
        for (var a = 0; a < nastyCount; a++)
        {
            nasties[a].NastyState = enNastyState.PreConception;
        }

        PerformPostChangeScreenActions();
    }

    public override void Update(GameTime gameTime)
    {
        if (gameEngine.Player.BlobsState == enBlobState.Normal)
        {
            // Cancel any nasty collision effect
            gameEngine.Player.BlobTouchedByNasty = false;

            if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                for (var a = 0; a < nastyCount; a++)
                {
                    nasties[a].NastyState = enNastyState.Dead;
                }
            }

            // Flag that's set if a nasty is touching Blob already - stops all of them zapping his energy at once which to too quick and unfair
            var nastyInteractingWithBlob = false;
            var nastySpawnedCounter = 0;

            for (var a = 0; a < nastyCount; a++)
            {
                if (nasties[a].NastyState == enNastyState.Normal)
                {
                    nastySpawnedCounter++;
                }

                // Setting the coordinates to a setting less than 0 will suspect all updates
                if (nasties[a].XPos > -1000 &&
                    nasties[a].YPos > -1000)
                {
                    nasties[a].Update(gameTime);

                    // Check for a collision between a nasty and Blob and raise an event
                    // Since we have a reference to Blob directly, we can send the call to animate contact directly
                    // We raise the event to affect the status panel
                    // Going to move the offsets etc. in a bit so it's not too precise and unfair!
                    if (CollisionDetection.Check(
                            gameEngine.Player.BlobXPos + 8,
                            gameEngine.Player.BlobYPos + 8,
                            gameEngine.Player.BlobWidth - 16,
                            gameEngine.Player.BlobHeight - 16,
                            nasties[a].XPos,
                            nasties[a].YPos,
                            nasties[a].NastyWidth,
                            nasties[a].NastyHeight))
                    {
                        if (nasties[a].NastyState == enNastyState.Normal)
                        {
                            // Raise the event
                            if (OnNastyInteractionWithBlob != null)
                            {
                                if (!nastyInteractingWithBlob)
                                {
                                    nastyInteractingWithBlob = true;
                                    OnNastyInteractionWithBlob(enNastyInterationType.HitBlob, nasties[a].NastyType);
                                }
                            }

                            // Trigger the blob animation
                            gameEngine.Player.BlobTouchedByNasty = true;
                        }
                    }

                    // Has blob shot a nasty
                    if (gameEngine.Player.BlobHasFired)
                    {
                        if (CollisionDetection.Check(
                                gameEngine.Player.BlobFireX,
                                gameEngine.Player.BlobFireY,
                                32,
                                32,
                                nasties[a].XPos,
                                nasties[a].YPos,
                                nasties[a].NastyWidth,
                                nasties[a].NastyHeight))
                        {
                            if (nasties[a].NastyState == enNastyState.Normal ||
                                nasties[a].NastyState == enNastyState.Birth)
                            {
                                gameEngine.Player.RegisterFireHit();
                                nasties[a].NastyState = enNastyState.Shot;

                                if (nasties[a].NastyType == enNastyType.Spiky)
                                {
                                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobExplodes");
                                }
                                else
                                {
                                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("NastyDeath");
                                }

                                if (OnNastyInteractionWithBlob != null)
                                {
                                    OnNastyInteractionWithBlob(enNastyInterationType.BlobShot,
                                        nasties[a].NastyType);
                                }
                            }
                        }
                    }

                    // Has the nasty hit an electricity gate...?
                    nasties[a].Smoking = false;

                    if (gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers != null)
                    {
                        for (var b = 0;
                             b < gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers.Length;
                             b++)
                        {
                            if (gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].ElectricityOn)
                            {
                                if (CollisionDetection.Check(
                                        gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .X,
                                        gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .Y,
                                        128,
                                        32,
                                        nasties[a].XPos,
                                        nasties[a].YPos,
                                        nasties[a].NastyWidth,
                                        nasties[a].NastyHeight))
                                {
                                    var NastyTravelDirection = CollisionDetection.CheckCollisionDirection(
                                        nasties[a].Speed,
                                        nasties[a].XPos,
                                        nasties[a].YPos,
                                        nasties[a].NastyWidth,
                                        nasties[a].NastyHeight,
                                        gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .X,
                                        gameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .Y,
                                        128,
                                        32);

                                    if (NastyTravelDirection != enDirection.None &&
                                        nasties[a].NastyState == enNastyState.Normal)
                                    {
                                        if (nastySmokes != null)
                                        {
                                            nasties[a].Smoking = true;
                                            nastySmokes.Start(
                                                SpriteHelpers.GetSpriteLocationOffset(nasties[a].Direction,
                                                    nasties[a].NastyWidth, nasties[a].NastyHeight).X +
                                                nasties[a].XPos,
                                                SpriteHelpers.GetSpriteLocationOffset(nasties[a].Direction,
                                                    nasties[a].NastyWidth, nasties[a].NastyHeight).Y +
                                                nasties[a].YPos);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (nastySpawnedCounter == nastyCount)
            {
                gameEngine.ContentResourceManager.GetSoundFXEngine().EnablePeriodicBackgroundSounds();
            }

            if (nastySmokes != null)
            {
                nastySmokes.Update(gameTime);
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
        if (gameEngine.GameScreens.ScreenNumber != previousScreenVisited)
        {
            // Save the nasties on the screen we have left into the previous screen array
            for (var a = 0; a < nastyCount; a++)
            {
                // Make sure they don't stay dead when we leave
                if (nasties[a].NastyState == enNastyState.Dead ||
                    nasties[a].NastyState == enNastyState.Shot)
                {
                    nasties[a].NastyState = enNastyState.PreConception;
                }

                nasties[a + nastyCount].CopyFromSource(nasties[a]);
            }
        }

        // Do we have nasties from the previous screen...?
        if (gameEngine.GameScreens.ScreenNumber == previousScreenVisited)
        {
            // Swap them all around
            for (var a = 0; a < nastyCount; a++)
            {
                Swap(nasties, a, a + nastyCount);
            }
        }

        // Nope so we are going to create a batch from scratch
        if (gameEngine.GameScreens.ScreenNumber != previousScreenVisited)
        {
            // Do we generate all nasties the same
            if (gameEngine.RandomNumberGenerator.Next(0, 4) == 0)
            {
                gameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame = true;
            }
            else
            {
                gameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame = false;
            }

            var nastyType = enNastyType.Ghost;
            if (gameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
            {
                nastyType = Nasty.GetRandomNastyType(gameEngine, true);
            }

            // Reset the nasties
            for (var a = 0; a < nastyCount; a++)
            {
                nasties[a].KeepNastyTheSame = gameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame;
                nasties[a].NastyState = enNastyState.PreConception;

                if (gameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
                {
                    nasties[a].KeepNastyTheSame = true;
                    nasties[a].NastyType = nastyType;
                }
            }
        }

        for (var a = 0; a < nastyCount; a++)
        {
            nasties[a].PerformPostChangeScreenActions();
        }

        // Deactivate some nasties if we have conveyer nasties on screen
        if (gameEngine.ConveyerNastiesInstance.NastiesActive > 0)
        {
            for (var a = 0; a < gameEngine.ConveyerNastiesInstance.NastiesActive; a++)
            {
                nasties[a].NastyState = enNastyState.Disabled;
            }
        }

        // Reset the nasty smoke effect
        if (nastySmokes != null)
        {
            nastySmokes.Reset();
        }

        previousScreenVisited = gameEngine.GameScreens.PreviousScreenNumber;
    }

    public override void Draw(GameTime gameTime, int ScreenFade)
    {
        if (gameEngine.Player.BlobsState == enBlobState.Normal)
        {
            for (var a = 0; a < nastyCount; a++)
            {
                if (nasties[a].XPos > -1000 &&
                    nasties[a].YPos > -1000)
                {
                    nasties[a].Draw(gameTime, ScreenFade);
                }
            }

            if (nastySmokes != null)
            {
                nastySmokes.Draw(ScreenFade);
            }
        }
    }

    #endregion

}

public class Nasty : GameComponentBase
{

    // The number of frames for the birth to appear (60 frames = 1 second)
    private const int birthPeriod = 60;

    // Animation
    private readonly Animation nastyAnim;

    private bool attractedToBlob; // They got through phases of being attracted to Blob
    private int birthColourLevel;

    // Birth stuff
    private int birthFadeLevel;
    private float birthRotateSpeed = 8.0f;

    private float birthScale;
    private int birthTimer;

    // Counters
    private int conceptionwaitCounter;

    private bool constantMovement; // Movement type that doesn't rely on a number of steps
    private bool deathAnticlockwise;

    // Death stuff
    private int deathFadeLevel = 255;
    private float deathScale = 1f;
    private bool firstMovement;

    private enDirection lastCollisionDirection;
    private int movementChangeCount;
    private int movementChangeCounter;

    // Behaviour

    private int nastyDeadPauseCounter;
    private IStandardParticleEffect nastyDies;

    private Animation nastyDiesAnimation;
    private IStandardParticleEffect nastyDiesType2;
    //bool attractedToBlobFlag;


    // When shot monsters usually dissappear for good but there is a small chance they'll regenerate
    // Tech stuff
    private SpriteCollection nastySprite;

    private SpriteCollection nastySpriteNew;
    private SpriteCollection nastySpriteOriginal;

    /// <summary>
    ///     If the nasty is attracted to blob but something is in the way it won't be able to move
    ///     So this records such an instance and releases it after a number of frames
    /// </summary>
    private int nastyStuckCounter;

    private enNastyType nastyType;

    private int prevXpos;
    private int prevYpos;

    private bool spikeyPause;
    private int stepsMovedCounter;
    private int stepsToMove;

    // Position

    public Nasty(
        GameEngine GameEngineReference,
        int Xposition,
        int Yposition,
        int MoveSpeed)
        : base(GameEngineReference)
    {
        XPos = Xposition;
        YPos = Yposition;
        moveSpeed = MoveSpeed;

        // Randomly assign some shit....
        movementChangeCount = 0;

        nastyAnim = new Animation();
        nastyAnim.FirstFrame = 0;
        nastyAnim.LastFrame = 3;
        nastyAnim.Repeat = true;
        nastyAnim.AnimateForwards = true;
        nastyAnim.Yoyo = true;
        nastyAnim.Delay = 4;
    }

    private MoveType movementType
    {
        get;
        set;
        //movementTypeInternal = MoveType.DiagonalBouncing;
    }

    public enDirection Direction { get; private set; }

    private int moveSpeed
    {
        get;
        set;
        //moveSpeedInternal = 40;
    }

    public int Speed => moveSpeed;

    public bool KeepNastyTheSame { get; set; }

    public int NastyWidth { get; set; } = 64;

    public enNastyState NastyState { get; set; }

    public int NastyHeight { get; set; } = 64;

    public bool Smoking { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    public enNastyType NastyType
    {
        get => nastyType;
        set
        {
            nastyType = value;
            if (nastyType == enNastyType.CoreGuardian)
            {
                movementType = MoveType.DiagonalBouncing;
            }
            else if (nastyType == enNastyType.Spiky)
            {
                movementType = MoveType.SpikeyMovement;
            }
            else
            {
                movementType =
                    (MoveType)gameEngine.RandomNumberGenerator.Next(0,
                        Enum.GetValues(typeof(MoveType)).Length - 1);
            }
        }
    }

    public void CopyFromSource(Nasty CopySource)
    {
        movementType = CopySource.movementType;
        attractedToBlob = CopySource.attractedToBlob;
        Direction = CopySource.Direction;
        stepsMovedCounter = CopySource.stepsMovedCounter;
        Direction = CopySource.Direction;
        stepsMovedCounter = CopySource.stepsMovedCounter;
        stepsToMove = CopySource.stepsToMove;
        constantMovement = CopySource.constantMovement;
        moveSpeed = CopySource.moveSpeed;
        lastCollisionDirection = CopySource.lastCollisionDirection;
        movementChangeCounter = CopySource.movementChangeCounter;
        movementChangeCount = CopySource.movementChangeCount;
        KeepNastyTheSame = CopySource.KeepNastyTheSame;
        nastyType = CopySource.nastyType;
        XPos = CopySource.XPos;
        YPos = CopySource.YPos;
        nastyDeadPauseCounter = CopySource.nastyDeadPauseCounter;

        // If the basty is dead reset it to conceptive state
        if (CopySource.NastyState == enNastyState.Dead)
        {
            NastyState = enNastyState.PreConception;
        }

        // Ensure that we don't carry on generating after moving screens
        if (CopySource.NastyState == enNastyState.Birth)
        {
            NastyState = enNastyState.Normal;
        }
        else
        {
            NastyState = CopySource.NastyState;
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
        var potentialLocations = gameEngine.GameScreens.GetNastyPotentialLocations();

        if (potentialLocations.Count > 0)
        {
            var nastyLocationIndex = gameEngine.RandomNumberGenerator.Next(0, potentialLocations.Count);

            // Return a location
            return new Point(potentialLocations[nastyLocationIndex].X + 32,
                potentialLocations[nastyLocationIndex].Y + 16);
        }

        // this should never happen
        return new Point(0, 0);
    }

    public void LoadContent(ContentList Content)
    {
        nastyDiesAnimation = new Animation();
        nastyDiesAnimation.FirstFrame = 0;
        nastyDiesAnimation.CurrentFrame = 0;
        nastyDiesAnimation.LastFrame = 3;
        nastyDiesAnimation.Yoyo = false;
        nastyDiesAnimation.Delay = 2;
        nastyDiesAnimation.Repeat = true;
        nastyDiesAnimation.AnimateForwards = true;

        nastySpriteOriginal = gameEngine.ContentResourceManager.GetSpriteCollection("NastiesOriginal")
            .Clone(gameEngine);
        nastySpriteNew = gameEngine.ContentResourceManager.GetSpriteCollection("Nasties").Clone(gameEngine);

        CheckSpritesInUse();

        nastyDies = gameEngine.ContentResourceManager.GetParticleEffectInstance("NastyDies",
            enParticleEffectType.Triggered, null) as IStandardParticleEffect;
        nastyDies.ParticleEffectComplete += nastyDies_ParticleEffectComplete;

        nastyDiesType2 =
            gameEngine.ContentResourceManager.GetParticleEffectInstance("NastySplat",
                enParticleEffectType.Triggered, null) as IStandardParticleEffect;
        nastyDiesType2.ParticleEffectComplete += nastyDies_ParticleEffectComplete;
    }

    private void CheckSpritesInUse()
    {
        if (gameEngine.UsingOldGraphics)
        {
            nastySprite = nastySpriteOriginal;
        }
        else
        {
            nastySprite = nastySpriteNew;
        }
    }

    private void nastyDies_ParticleEffectComplete()
    {
        if (NastyState == enNastyState.Dead)
        {
            // Extend this to pause for random time before bringing a nasty back to life
            nastyDeadPauseCounter = 0;
            NastyState = enNastyState.PreConception;
            conceptionwaitCounter = gameEngine.RandomNumberGenerator.Next(0, 500);
        }
    }

    public void Initialise()
    {
        stepsMovedCounter = 0;
        stepsToMove = 0;

        if (movementType == MoveType.DiagonalBouncing)
        {
            Direction = (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
        }
        else if (movementType == MoveType.RandomMovement ||
                 movementType == MoveType.RandomUpDownLeftRight)
        {
            Direction = (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // select from 1st four
        }
    }

    public static enNastyType GetRandomNastyType(GameEngine GameEngineReference, bool KeepNastiesTheSame)
    {
        //NastyType = (enNastyType)gameEngine.RandomNumberGenerator.Next(0, 15); // Pick from first 15 types

        var temp = 0;

        // Go to more than max nasty types to increase chance of four spiky nasties
        if (KeepNastiesTheSame)
        {
            // Add an extra 5 onto final nasty number when all anims done
            temp = GameEngineReference.RandomNumberGenerator.Next(0, 16);
        }
        else
        {
            temp = GameEngineReference.RandomNumberGenerator.Next(0, 16);
        }

        if (temp == 0)
        {
            return enNastyType.Bird;
        }
        if (temp == 1)
        {
            return enNastyType.Helmet;
        }
        if (temp == 2)
        {
            return enNastyType.FriedEgg;
        }
        if (temp == 3)
        {
            return enNastyType.CurleyWurley;
        }
        if (temp == 4)
        {
            return enNastyType.Shellfish;
        }
        if (temp == 5)
        {
            return enNastyType.Cushion;
        }
        if (temp == 6)
        {
            return enNastyType.Gonzo;
        }
        if (temp == 7)
        {
            return enNastyType.Snail;
        }
        if (temp == 8)
        {
            return enNastyType.Ghost;
        }
        if (temp == 9)
        {
            return enNastyType.Centipede;
        }
        if (temp == 10)
        {
            return enNastyType.Scorpian;
        }
        if (temp == 11)
        {
            return enNastyType.DragonFly;
        }
        if (temp == 12)
        {
            return enNastyType.Hippo;
        }
        if (temp == 13)
        {
            return enNastyType.Helicopter;
        }
        if (temp == 14)
        {
            return enNastyType.Bird;
        }
        return enNastyType.Spiky;
    }

    public override void Update(GameTime gameTime)
    {
        if (gameEngine.GameScreens.ScreenNumber == 198)
        {
            NastyState = enNastyState.Disabled;
        }

        if (gameEngine.GameScreens.ScreenNumber == 199)
        {
            nastyType = enNastyType.CoreGuardian;
            attractedToBlob = false;
        }

        // Reset conception variables
        if (NastyState == enNastyState.PreConception)
        {
            conceptionwaitCounter = gameEngine.RandomNumberGenerator.Next(0, 60);
            NastyState = enNastyState.Conception;

            if (!KeepNastyTheSame)
            {
                nastyType = GetRandomNastyType(gameEngine, false);
            }

            if (nastyType != enNastyType.Spiky)
            {
                movementType = MoveType.RandomMovement;
            }
            else
            {
                movementType = MoveType.SpikeyMovement;
                spikeyPause = true;
            }
        }
        else if (NastyState == enNastyState.Conception)
        {
            if (conceptionwaitCounter > 0)
            {
                conceptionwaitCounter--;
            }
            else
            {
                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("NastyAppear", 0.1F, false);
                movementChangeCounter = 0;
                movementChangeCount = 0;
                firstMovement = true;
                deathFadeLevel = 255;
                deathScale = 1f;
                birthFadeLevel = 0;
                birthScale = 0f;
                birthColourLevel = 0;
                birthRotateSpeed = 32.0f;
                birthTimer = 0;

                // Select a new location to spawn
                var NewPosition = GetNastySpawnLocation();
                if (NewPosition == new Point(0, 0))
                {
                    NastyState = enNastyState.Disabled;
                }
                else
                {
                    XPos = NewPosition.X;
                    YPos = NewPosition.Y;
                }

                NastyState = enNastyState.Birth;
            }
        }
        else if (NastyState == enNastyState.Birth)
        {
            if (birthScale < 1.0f)
            {
                birthScale = birthScale + 1f / birthPeriod;
            }

            if (birthScale > 1.0f)
            {
                birthScale = 1.0f;
            }


            // Fade the nasty in
            if (birthFadeLevel < 255)
            {
                birthFadeLevel =
                    birthFadeLevel +
                    (int)(255 /
                          ((double)birthPeriod /
                           2)); // We only have half the time to do the colour fade in 
            }


            // Once we're halfway through fade in the colours
            if (birthTimer > birthPeriod / 2)
            {
                if (birthColourLevel < 255)
                {
                    birthColourLevel =
                        birthColourLevel +
                        (int)(255 /
                              ((double)birthPeriod /
                               2)); // We only have half the time to do the colour fade in 
                }

                if (birthRotateSpeed > 16)
                {
                    birthRotateSpeed = birthRotateSpeed - 16f / ((float)birthPeriod / 2);
                }
            }

            if (birthFadeLevel > 255)
            {
                birthFadeLevel = 255;
            }

            if (birthColourLevel > 255)
            {
                birthColourLevel = 255;
            }

            if (birthTimer > birthPeriod)
            {
                if (nastySprite.SpriteRotation == 0)
                {
                    NastyState = enNastyState.Normal;
                    birthTimer = 0;
                }
            }

            birthTimer++;
        }
        else if (NastyState == enNastyState.Shot)
        {
            deathFadeLevel = 255;
            deathScale = 1.0f;

            if (nastyType == enNastyType.Spiky ||
                nastyType == enNastyType.Conveyer)
            {
                if (nastyDies != null)
                {
                    nastyDies.Start();
                }
            }
            else
            {
                if (nastyDiesType2 != null)
                {
                    NastyHelpers.ApplyNastyColours(nastyType, nastyDiesType2.Effect);
                    nastyDiesType2.Start();
                }
            }

            NastyState = enNastyState.Dead;

            if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
            {
                deathAnticlockwise = true;
            }
            else
            {
                deathAnticlockwise = false;
            }
        }
        else if (NastyState == enNastyState.Dead)
        {
            if (gameEngine.UsingOldGraphics)
            {
                nastyDiesAnimation.GetNextFrame();

                if (nastyDeadPauseCounter < 50)
                {
                    nastyDeadPauseCounter++;
                }
                else
                {
                    NastyState = enNastyState.PreConception;
                    conceptionwaitCounter = gameEngine.RandomNumberGenerator.Next(0, 500);
                    nastyDeadPauseCounter = 0;
                }
            }
        }

        if (nastyDies != null)
        {
            // Update the explosion
            nastyDies.Update(XPos + 32, YPos + 32, gameTime);
        }

        if (nastyDiesType2 != null)
        {
            // Update the splat
            nastyDiesType2.Update(XPos + 32, YPos + 32, gameTime);
        }

        var updateAnimation = false;

        if (NastyState != enNastyState.Disabled &&
            NastyState != enNastyState.PreConception &&
            NastyState != enNastyState.Conception &&
            NastyState != enNastyState.Birth)
        {
            updateAnimation = true;
        }

        if (NastyState == enNastyState.Birth &&
            birthTimer > birthPeriod / 4)
        {
            updateAnimation = true;
        }

        if (updateAnimation)
        {
            // Animate
            nastyAnim.GetNextFrame();

            if (nastyType != enNastyType.CoreGuardian)
            {
                if (attractedToBlob && nastyType != enNastyType.Spiky)
                {
                    if (prevXpos == XPos &&
                        prevYpos == YPos)
                    {
                        nastyStuckCounter++;
                    }
                    else
                    {
                        nastyStuckCounter = 0;
                    }

                    // Has nasty been stuck for 1/4 of a second...?
                    if (nastyStuckCounter > 15)
                    {
                        // Unattract blob
                        attractedToBlob = false;
                        nastyStuckCounter = 0;
                    }

                    prevXpos = XPos;
                    prevYpos = YPos;
                }
            }

            var amountToMoveX = 0;
            var amountToMoveY = 0;

            // Do the movement
            if (nastyType == enNastyType.CoreGuardian)
            {
                MoveNastyOneStep(ref amountToMoveX, ref amountToMoveY);
                DiagonalBouncing(0, 0);
            }
            else
            {
                if (stepsMovedCounter < stepsToMove)
                {
                    var moveOneStep = true;

                    if (movementType == MoveType.SpikeyMovement && spikeyPause)
                    {
                        moveOneStep = false;
                        stepsMovedCounter++;
                    }

                    if (moveOneStep)
                    {
                        MoveNastyOneStep(ref amountToMoveX, ref amountToMoveY);
                    }
                }
                else
                {
                    Direction = enDirection.None;
                    stepsMovedCounter = 0;

                    if (movementType == MoveType.RandomMovement)
                    {
                        constantMovement = false;
                        RandomMovement(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos);
                    }
                    else if (movementType == MoveType.RandomUpDownLeftRight)
                    {
                        constantMovement = false;
                        RandomUpDownLeftRight(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos);
                    }
                    else if (movementType == MoveType.DiagonalBouncing)
                    {
                        constantMovement = true;
                        if (gameEngine.Player != null)
                        {
                            DiagonalBouncing(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos);
                        }
                        else
                        {
                            DiagonalBouncing(0, 0);
                        }
                    }
                    else if (movementType == MoveType.DiagonalBouncingInSteps)
                    {
                        constantMovement = false;
                        DiagonalBouncingInSteps(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos);
                    }
                    else if (movementType == MoveType.RandomMovementsAllDirections)
                    {
                        constantMovement = false;
                        RandomMovementsAllDirections(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos);
                    }
                    else if (movementType == MoveType.SpikeyMovement)
                    {
                        spikeyPause = !spikeyPause;
                        constantMovement = false;
                        SpikeyMovement(gameEngine.Player.BlobXPos, gameEngine.Player.BlobYPos);
                    }
                }
            }

            // increment the movement change counter
            if (nastyType != enNastyType.CoreGuardian)
            {
                if (movementChangeCounter < movementChangeCount)
                {
                    movementChangeCounter++;
                }
                else
                {
                    var attractionFlag = gameEngine.RandomNumberGenerator.Next(0, 2);

                    if (attractionFlag == 0)
                    {
                        movementChangeCount = gameEngine.RandomNumberGenerator.Next(200, 500);
                        attractedToBlob = false;
                    }
                    else
                    {
                        movementChangeCount = gameEngine.RandomNumberGenerator.Next(400, 700);
                        attractedToBlob = true;
                    }

                    movementChangeCounter = 0;

                    if (nastyType == enNastyType.Spiky)
                    {
                        attractedToBlob = true; // Spiky nasties are always attracted to blob
                        movementType = MoveType.SpikeyMovement;
                    }
                    else
                    {
                        if (attractedToBlob)
                        {
                            // just choose from the attraction modes....
                            movementType = (MoveType)gameEngine.RandomNumberGenerator.Next(0, 2);
                        }
                        else
                        {
                            movementType = (MoveType)gameEngine.RandomNumberGenerator.Next(0,
                                Enum.GetValues(typeof(MoveType)).Length - 1);
                        }

                        if (movementType == MoveType.DiagonalBouncing)
                        {
                            Direction =
                                (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
                        }
                        else if (movementType == MoveType.RandomMovement ||
                                 movementType == MoveType.RandomUpDownLeftRight)
                        {
                            Direction =
                                (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // select from 1st four
                        }
                        else if (movementType == MoveType.DiagonalBouncingInSteps)
                        {
                            Direction =
                                (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
                            stepsToMove = gameEngine.RandomNumberGenerator.Next(8, 16);
                        }

                        // Create a random speed for the nasty to move
                        if (!attractedToBlob)
                        {
                            if (movementType == MoveType.DiagonalBouncing ||
                                movementType == MoveType.DiagonalBouncingInSteps ||
                                movementType == MoveType.RandomUpDownLeftRight)
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(6, 8);
                            }
                            else if (movementType == MoveType.RandomMovement ||
                                     movementType == MoveType.RandomMovementsAllDirections)
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(2, 4);
                            }
                            else
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(6, 8);
                            }
                        }
                        else
                        {
                            if (movementType == MoveType.RandomMovement ||
                                movementType == MoveType.RandomMovementsAllDirections)
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(4, 6);
                            }
                            else if (movementType == MoveType.RandomUpDownLeftRight ||
                                     movementType == MoveType.DiagonalBouncing ||
                                     movementType == MoveType.DiagonalBouncingInSteps)
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(8, 10);
                            }
                            else if (movementType == MoveType.SpikeyMovement)
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(4, 6);
                            }
                            else
                            {
                                moveSpeed = gameEngine.RandomNumberGenerator.Next(8, 10);
                            }
                        }

                        // Change movespeed to 1 if this is the first movement - allows the nasties to sppear moving slowly
                        if (firstMovement)
                        {
                            moveSpeed = 1;
                            movementChangeCount = 90;
                            firstMovement = false;
                        }
                    }
                }
            }
            else
            {
                attractedToBlob = false;
                constantMovement = true;
                movementType = MoveType.DiagonalBouncing;
                moveSpeed = 8;
            }
        }
    }

    private void DiagonalBouncingInSteps(int BlobsXPosition, int BlobsYPosition)
    {
        if (lastCollisionDirection == enDirection.None || Direction == enDirection.None)
        {
            Direction = (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // Select from last 4
        }

        if (Direction == enDirection.UpLeft)
        {
            if (lastCollisionDirection == enDirection.UpLeft)
            {
                Direction = enDirection.DownRight;
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                Direction = enDirection.UpRight;
            }
            else if (lastCollisionDirection == enDirection.Up)
            {
                Direction = enDirection.DownLeft;
            }
            else
            {
                Direction = enDirection.DownLeft; // Didn't hit anything, just change direction
            }
        }
        else if (Direction == enDirection.UpRight)
        {
            if (lastCollisionDirection == enDirection.UpRight)
            {
                Direction = enDirection.DownLeft;
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                Direction = enDirection.UpLeft;
            }
            else if (lastCollisionDirection == enDirection.Up)
            {
                Direction = enDirection.DownRight;
            }
            else
            {
                Direction = enDirection.DownRight; // Didn't hit anything, just change direction
            }
        }
        else if (Direction == enDirection.DownLeft)
        {
            if (lastCollisionDirection == enDirection.DownLeft)
            {
                Direction = enDirection.UpRight;
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                Direction = enDirection.DownRight;
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                Direction = enDirection.UpLeft;
            }
            else
            {
                Direction = enDirection.UpLeft; // Didn't hit anything, just change direction
            }
        }
        else if (Direction == enDirection.DownRight)
        {
            if (lastCollisionDirection == enDirection.DownRight)
            {
                Direction = enDirection.UpLeft;
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                Direction = enDirection.DownLeft;
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                Direction = enDirection.UpRight;
            }
            else
            {
                Direction = enDirection.UpRight; // Didn't hit anything, just change direction
            }
        }
    }

    private void DiagonalBouncing(int BlobsXPosition, int BlobsYPosition)
    {
        if (Direction == enDirection.None)
        {
            Direction = (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // Select from last 4
        }

        if (Direction == enDirection.UpLeft)
        {
            if (lastCollisionDirection == enDirection.UpLeft)
            {
                Direction = enDirection.DownRight;
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                Direction = enDirection.UpRight;
            }
            else if (lastCollisionDirection == enDirection.Up)
            {
                Direction = enDirection.DownLeft;
            }
        }
        else if (Direction == enDirection.UpRight)
        {
            if (lastCollisionDirection == enDirection.UpRight)
            {
                Direction = enDirection.DownLeft;
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                Direction = enDirection.UpLeft;
            }
            else if (lastCollisionDirection == enDirection.Up)
            {
                Direction = enDirection.DownRight;
            }
        }
        else if (Direction == enDirection.DownLeft)
        {
            if (lastCollisionDirection == enDirection.DownLeft)
            {
                Direction = enDirection.UpRight;
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                Direction = enDirection.DownRight;
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                Direction = enDirection.UpLeft;
            }
        }
        else if (Direction == enDirection.DownRight)
        {
            if (lastCollisionDirection == enDirection.DownRight)
            {
                Direction = enDirection.UpLeft;
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                Direction = enDirection.DownLeft;
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                Direction = enDirection.UpRight;
            }
        }

        stepsToMove = gameEngine.RandomNumberGenerator.Next(8, 40);
    }

    private void RandomUpDownLeftRight(int BlobsXPosition, int BlobsYPosition)
    {
        if (lastCollisionDirection == enDirection.None || Direction == enDirection.None)
        {
            Direction = (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // Only select fron first 4
        }

        if (lastCollisionDirection == enDirection.Up)
        {
            Direction = enDirection.Down;
        }
        else if (lastCollisionDirection == enDirection.Down)
        {
            Direction = enDirection.Up;
        }
        else if (lastCollisionDirection == enDirection.Left)
        {
            Direction = enDirection.Right;
        }
        else if (lastCollisionDirection == enDirection.Right)
        {
            Direction = enDirection.Left;
        }

        // If attracted to blob is turned on
        if (attractedToBlob)
        {
            if (XPos < BlobsXPosition)
            {
                if (lastCollisionDirection != enDirection.Right)
                {
                    Direction = enDirection.Right;
                }
            }
            else if (XPos > BlobsXPosition)
            {
                if (lastCollisionDirection != enDirection.Left)
                {
                    Direction = enDirection.Left;
                }
            }


            if (YPos < BlobsYPosition)
            {
                if (lastCollisionDirection != enDirection.Down)
                {
                    if (Direction == enDirection.Left)
                    {
                        Direction = enDirection.DownLeft;
                    }
                    else if (Direction == enDirection.Right)
                    {
                        Direction = enDirection.DownRight;
                    }
                    else
                    {
                        Direction = enDirection.Down;
                    }
                }
            }
            else if (YPos > BlobsYPosition)
            {
                if (lastCollisionDirection != enDirection.Up)
                {
                    if (Direction == enDirection.Left)
                    {
                        Direction = enDirection.UpLeft;
                    }
                    else if (Direction == enDirection.Right)
                    {
                        Direction = enDirection.UpRight;
                    }
                    else
                    {
                        Direction = enDirection.Up;
                    }
                }
            }
        }

        stepsToMove = gameEngine.RandomNumberGenerator.Next(8, 40);
    }

    private void RandomMovement(int BlobsXPosition, int BlobsYPosition)
    {
        if (lastCollisionDirection == enDirection.None || Direction == enDirection.None)
        {
            Direction = (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // Only select fron first 4
        }

        if (lastCollisionDirection == enDirection.Up)
        {
            Direction = enDirection.Down;
        }
        else if (lastCollisionDirection == enDirection.Down)
        {
            Direction = enDirection.Up;
        }
        else if (lastCollisionDirection == enDirection.Left)
        {
            Direction = enDirection.Right;
        }
        else if (lastCollisionDirection == enDirection.Right)
        {
            Direction = enDirection.Left;
        }

        // If attracted to blob is turned on
        if (attractedToBlob)
        {
            Direction = enDirection.None;

            if (XPos < BlobsXPosition)
            {
                if (lastCollisionDirection != enDirection.Right)
                {
                    Direction = enDirection.Right;
                }
            }
            else if (XPos > BlobsXPosition)
            {
                if (lastCollisionDirection != enDirection.Left)
                {
                    Direction = enDirection.Left;
                }
            }


            if (YPos < BlobsYPosition)
            {
                if (lastCollisionDirection != enDirection.Down)
                {
                    if (Direction == enDirection.Left)
                    {
                        Direction = enDirection.DownLeft;
                    }
                    else if (Direction == enDirection.Right)
                    {
                        Direction = enDirection.DownRight;
                    }
                    else
                    {
                        Direction = enDirection.Down;
                    }
                }
            }
            else if (YPos > BlobsYPosition)
            {
                if (lastCollisionDirection != enDirection.Up)
                {
                    if (Direction == enDirection.Left)
                    {
                        Direction = enDirection.UpLeft;
                    }
                    else if (Direction == enDirection.Right)
                    {
                        Direction = enDirection.UpRight;
                    }
                    else
                    {
                        Direction = enDirection.Up;
                    }
                }
            }
        }

        stepsToMove = gameEngine.RandomNumberGenerator.Next(1, 4);
    }

    private void RandomMovementsAllDirections(int BlobsXPosition, int BlobsYPosition)
    {
        if (lastCollisionDirection == enDirection.None || Direction == enDirection.None)
        {
            Direction =
                (enDirection)gameEngine.RandomNumberGenerator.Next(0, 8); // Select from all movement directions
        }

        if (!attractedToBlob)
        {
            if (lastCollisionDirection == enDirection.Up)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 2);

                if (newDirection == 0)
                {
                    Direction = enDirection.Down;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.DownLeft;
                }
                else
                {
                    Direction = enDirection.DownRight;
                }
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Up;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.UpLeft;
                }
                else
                {
                    Direction = enDirection.UpRight;
                }
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Right;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.DownRight;
                }
                else
                {
                    Direction = enDirection.UpRight;
                }
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Left;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.DownLeft;
                }
                else
                {
                    Direction = enDirection.UpLeft;
                }
            }
            else if (lastCollisionDirection == enDirection.UpLeft)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Down;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.DownLeft;
                }
                else
                {
                    Direction = enDirection.DownRight;
                }
            }
            else if (lastCollisionDirection == enDirection.UpRight)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Down;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.DownRight;
                }
                else
                {
                    Direction = enDirection.DownLeft;
                }
            }
            else if (lastCollisionDirection == enDirection.DownLeft)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Up;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.UpLeft;
                }
                else
                {
                    Direction = enDirection.UpRight;
                }
            }
            else if (lastCollisionDirection == enDirection.DownRight)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Up;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.UpRight;
                }
                else
                {
                    Direction = enDirection.UpLeft;
                }
            }
        }
        else // Attracted to Blob
        {
            if (XPos <= BlobsXPosition && YPos <= BlobsYPosition)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Right;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.Down;
                }
                else
                {
                    Direction = enDirection.DownRight;
                }
            }
            else if (XPos <= BlobsXPosition && YPos >= BlobsYPosition)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Right;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.Up;
                }
                else
                {
                    Direction = enDirection.UpRight;
                }
            }
            else if (XPos >= BlobsXPosition && YPos >= BlobsYPosition)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Left;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.Up;
                }
                else
                {
                    Direction = enDirection.UpLeft;
                }
            }
            else if (XPos >= BlobsXPosition && YPos <= BlobsYPosition)
            {
                var newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                if (newDirection == 0)
                {
                    Direction = enDirection.Left;
                }
                else if (newDirection == 1)
                {
                    Direction = enDirection.Down;
                }
                else
                {
                    Direction = enDirection.DownLeft;
                }
            }
        }

        stepsToMove = gameEngine.RandomNumberGenerator.Next(10, 40);
    }

    private void SpikeyMovement(int BlobsXPosition, int BlobsYPosition)
    {
        var stepsFromBlob = 0;

        if (!spikeyPause)
        {
            if (XPos <= BlobsXPosition && YPos <= BlobsYPosition)
            {
                var distanceX = BlobsXPosition - XPos;
                var distanceY = BlobsYPosition - YPos;

                if (distanceX > distanceY)
                {
                    Direction = enDirection.Right;
                    stepsFromBlob = BlobsXPosition - XPos;
                }
                else if (distanceY > distanceX)
                {
                    Direction = enDirection.Down;
                    stepsFromBlob = BlobsYPosition - YPos;
                }

                if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == enDirection.Down)
                    {
                        Direction = enDirection.DownRight;
                        stepsFromBlob = BlobsYPosition - YPos;
                    }
                    else if (Direction == enDirection.Right)
                    {
                        Direction = enDirection.DownRight;
                        stepsFromBlob = BlobsXPosition - XPos;
                    }
                }

                if (lastCollisionDirection == enDirection.Right)
                {
                    Direction = enDirection.Down;
                    stepsFromBlob = BlobsYPosition - YPos;
                }
                else if (lastCollisionDirection == enDirection.Down)
                {
                    Direction = enDirection.Right;
                    stepsFromBlob = BlobsXPosition - XPos;
                }
                else if (lastCollisionDirection == enDirection.DownRight)
                {
                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = enDirection.Right;
                        stepsFromBlob = BlobsXPosition - XPos;
                    }
                    else
                    {
                        Direction = enDirection.Down;
                        stepsFromBlob = BlobsYPosition - YPos;
                    }
                }
            }
            else if (XPos <= BlobsXPosition && YPos >= BlobsYPosition)
            {
                var distanceX = BlobsXPosition - XPos;
                var distanceY = YPos - BlobsYPosition;

                if (distanceX > distanceY)
                {
                    Direction = enDirection.Right;
                    stepsFromBlob = BlobsXPosition - XPos;
                }
                else if (distanceY > distanceX)
                {
                    Direction = enDirection.Up;
                    stepsFromBlob = YPos - BlobsYPosition;
                }

                if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == enDirection.Right)
                    {
                        Direction = enDirection.UpRight;
                        stepsFromBlob = BlobsXPosition - XPos;
                    }
                    else if (Direction == enDirection.Up)
                    {
                        Direction = enDirection.UpRight;
                        stepsFromBlob = YPos - BlobsYPosition;
                    }
                }

                if (lastCollisionDirection == enDirection.Up)
                {
                    Direction = enDirection.Right;
                    stepsFromBlob = BlobsXPosition - XPos;
                }
                else if (lastCollisionDirection == enDirection.Right)
                {
                    Direction = enDirection.Up;
                    stepsFromBlob = YPos - BlobsYPosition;
                }
                else if (lastCollisionDirection == enDirection.UpRight)
                {
                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = enDirection.Right;
                        stepsFromBlob = BlobsXPosition - XPos;
                    }
                    else
                    {
                        Direction = enDirection.Up;
                        stepsFromBlob = YPos - BlobsYPosition;
                    }
                }
            }
            else if (XPos >= BlobsXPosition && YPos >= BlobsYPosition)
            {
                var distanceX = XPos - BlobsXPosition;
                var distanceY = YPos - BlobsYPosition;

                if (distanceX > distanceY)
                {
                    Direction = enDirection.Left;
                    stepsFromBlob = XPos - BlobsXPosition;
                }
                else if (distanceY > distanceX)
                {
                    Direction = enDirection.Up;
                    stepsFromBlob = YPos - BlobsYPosition;
                }

                if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == enDirection.Left)
                    {
                        Direction = enDirection.UpLeft;
                        stepsFromBlob = XPos - BlobsXPosition;
                    }
                    else if (Direction == enDirection.Up)
                    {
                        Direction = enDirection.UpLeft;
                        stepsFromBlob = YPos - BlobsYPosition;
                    }
                }

                if (lastCollisionDirection == enDirection.Up)
                {
                    Direction = enDirection.Left;
                    stepsFromBlob = XPos - BlobsXPosition;
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    Direction = enDirection.Up;
                    stepsFromBlob = YPos - BlobsYPosition;
                }
                else if (lastCollisionDirection == enDirection.UpLeft)
                {
                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = enDirection.Left;
                        stepsFromBlob = XPos - BlobsXPosition;
                    }
                    else
                    {
                        Direction = enDirection.Up;
                        stepsFromBlob = YPos - BlobsYPosition;
                    }
                }
            }
            else if (XPos >= BlobsXPosition && YPos <= BlobsYPosition)
            {
                var distanceX = XPos - BlobsXPosition;
                var distanceY = BlobsYPosition - YPos;

                if (distanceX > distanceY)
                {
                    Direction = enDirection.Left;
                    stepsFromBlob = XPos - BlobsXPosition;
                }
                else if (distanceY > distanceX)
                {
                    Direction = enDirection.Down;
                    stepsFromBlob = BlobsYPosition - YPos;
                }

                if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    if (Direction == enDirection.Left)
                    {
                        Direction = enDirection.DownLeft;
                        stepsFromBlob = XPos - BlobsXPosition;
                    }
                    else if (Direction == enDirection.Down)
                    {
                        Direction = enDirection.DownLeft;
                        stepsFromBlob = BlobsYPosition - YPos;
                    }
                }

                if (lastCollisionDirection == enDirection.Down)
                {
                    Direction = enDirection.Left;
                    stepsFromBlob = XPos - BlobsXPosition;
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    Direction = enDirection.Down;
                    stepsFromBlob = BlobsYPosition - YPos;
                }
                else if (lastCollisionDirection == enDirection.DownLeft)
                {
                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        Direction = enDirection.Left;
                        stepsFromBlob = XPos - BlobsXPosition;
                    }
                    else
                    {
                        Direction = enDirection.Down;
                        stepsFromBlob = BlobsYPosition - YPos;
                    }
                }
            }
        }
        else
        {
            Direction = enDirection.None;
        }

        if (!spikeyPause)
        {
            stepsToMove = gameEngine.RandomNumberGenerator.Next(
                              (int)(stepsFromBlob / (double)moveSpeed / 2),
                              (int)(stepsFromBlob / (double)moveSpeed)) +
                          10; // An additional 10 to stop the spiky lads from getting stuck on scenary
        }
        else
        {
            stepsToMove = gameEngine.RandomNumberGenerator.Next(40, 80);
        }
    }

    private void MoveNastyOneStep(ref int amountToMoveX, ref int amountToMoveY)
    {
        lastCollisionDirection = enDirection.None;

        if (Direction == enDirection.Down)
        {
            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);

            if (amountToMoveY == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            YPos = YPos + amountToMoveY;
        }
        else if (Direction == enDirection.Up)
        {
            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);

            if (amountToMoveY == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            YPos = YPos - amountToMoveY;
        }
        else if (Direction == enDirection.Left)
        {
            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);

            if (amountToMoveX == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            XPos = XPos - amountToMoveX;
        }
        else if (Direction == enDirection.Right)
        {
            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);

            if (amountToMoveX == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            XPos = XPos + amountToMoveX;
        }
        else if (Direction == enDirection.UpLeft)
        {
            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);
            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, XPos, YPos - amountToMoveY,
                NastyWidth, NastyHeight, moveSpeed, false);

            if (amountToMoveY == 0)
            {
                stepsMovedCounter = stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            YPos = YPos - amountToMoveY;
            XPos = XPos - amountToMoveX;
        }
        else if (Direction == enDirection.DownLeft)
        {
            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);
            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, XPos, YPos + amountToMoveY,
                NastyWidth, NastyHeight, moveSpeed, false);

            if (amountToMoveY == 0)
            {
                stepsMovedCounter = stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            YPos = YPos + amountToMoveY;
            XPos = XPos - amountToMoveX;
        }
        else if (Direction == enDirection.UpRight)
        {
            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);
            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, XPos, YPos - amountToMoveY,
                NastyWidth, NastyHeight, moveSpeed, false);

            if (amountToMoveY == 0)
            {
                stepsMovedCounter = stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            YPos = YPos - amountToMoveY;
            XPos = XPos + amountToMoveX;
        }
        else if (Direction == enDirection.DownRight)
        {
            amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, XPos, YPos, NastyWidth,
                NastyHeight, moveSpeed, false);
            amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, XPos, YPos + amountToMoveY,
                NastyWidth, NastyHeight, moveSpeed, false);

            if (amountToMoveY == 0)
            {
                stepsMovedCounter = stepsToMove;
            }
            else if (amountToMoveX == 0)
            {
                stepsMovedCounter = stepsToMove;
            }

            YPos = YPos + amountToMoveY;
            XPos = XPos + amountToMoveX;
        }

        if (Direction == enDirection.Left)
        {
            if (amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.Left;
            }
        }
        else if (Direction == enDirection.Right)
        {
            if (amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.Right;
            }
        }
        else if (Direction == enDirection.Up)
        {
            if (amountToMoveY == 0)
            {
                lastCollisionDirection = enDirection.Up;
            }
        }
        else if (Direction == enDirection.Down)
        {
            if (amountToMoveY == 0)
            {
                lastCollisionDirection = enDirection.Down;
            }
        }
        else if (Direction == enDirection.UpLeft)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.UpLeft;
            }
            else if (amountToMoveY == 0)
            {
                lastCollisionDirection = enDirection.Up;
            }
            else if (amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.Left;
            }
        }
        else if (Direction == enDirection.UpRight)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.UpRight;
            }
            else if (amountToMoveY == 0)
            {
                lastCollisionDirection = enDirection.Up;
            }
            else if (amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.Right;
            }
        }
        else if (Direction == enDirection.DownLeft)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.DownLeft;
            }
            else if (amountToMoveY == 0)
            {
                lastCollisionDirection = enDirection.Down;
            }
            else if (amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.Left;
            }
        }
        else if (Direction == enDirection.DownRight)
        {
            if (amountToMoveY == 0 &&
                amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.DownRight;
            }
            else if (amountToMoveY == 0)
            {
                lastCollisionDirection = enDirection.Down;
            }
            else if (amountToMoveX == 0)
            {
                lastCollisionDirection = enDirection.Right;
            }
        }

        if (!constantMovement)
        {
            if (lastCollisionDirection != enDirection.None)
            {
                stepsMovedCounter = stepsToMove;
            }
            else
            {
                stepsMovedCounter++;
            }
        }
    }

    public void PerformPostChangeScreenActions()
    {
        // Stop the explosion effect leaking onto the next screen
        if (nastyDies != null)
        {
            nastyDies.Stop();
        }

        if (nastyDiesType2 != null)
        {
            nastyDiesType2.Stop();
        }

        if (NastyState == enNastyState.Disabled)
        {
            NastyState = enNastyState.Conception;
        }
    }

    public override void Draw(GameTime gameTime, int ScreenFade)
    {
        CheckSpritesInUse();

        var nastyImage = (int)nastyType * 4;

        if (nastyType == enNastyType.CoreGuardian)
        {
            nastySprite.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

            // Choose the image
            nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade), 1.0f, 4f, true, gameEngine.Paused);
            nastySprite.End();
        }
        else
        {
            if (NastyState == enNastyState.Birth)
            {
                nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

                // Choose the image
                nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                    ColourHelper.GetXNA4Colour((byte)birthColourLevel, (byte)birthColourLevel, 255,
                        (byte)(255 * ((double)ScreenFade / 255))), birthScale, birthRotateSpeed,
                    true, gameEngine.Paused);
                nastySprite.End();
            }
            else if (NastyState == enNastyState.Normal)
            {
                // Choose the image
                if (Smoking)
                {
                    nastySprite.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                    nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXNA4Colour(50, 50, 50, (byte)ScreenFade));
                    nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXNA4Colour(255, 0, 0, (byte)ScreenFade));
                    nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXNA4Colour(0, 100, 0, (byte)ScreenFade));
                }
                else
                {
                    nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                }

                nastySprite.End();
            }
            else if (NastyState == enNastyState.Dead)
            {
                // Draw the explosion
                if (nastyDies != null)
                {
                    nastyDies.Draw();
                }

                if (nastyDiesType2 != null)
                {
                    nastyDiesType2.Draw();
                }

                // Fade the nasty out as it explodes
                if (deathFadeLevel > 0)
                {
                    deathFadeLevel = deathFadeLevel - 20;
                }

                if (deathFadeLevel < 0)
                {
                    deathFadeLevel = 0;
                }

                // Shrink the nasty as it explodes
                if (deathScale > 0)
                {
                    deathScale = deathScale - 0.01f;
                }

                if (deathScale < 0)
                {
                    deathScale = 0;
                }

                if (deathFadeLevel > 0)
                {
                    nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXNA4Colour(255, 255, 255,
                            (byte)(deathFadeLevel * ((double)ScreenFade / 255))), deathScale, 16f,
                        deathAnticlockwise, gameEngine.Paused);
                    nastySprite.End();
                }

                if (gameEngine.UsingOldGraphics)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                        nastyDiesAnimation.CurrentFrame, XPos, YPos,
                        ColourHelper.GetXNA4Colour(255, 255, 255, ScreenFade), 1f, 0f);
                    gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                }
            }
        }
    }

    #endregion

}

public class NastyHelpers
{
    public static void ApplyNastyColours(enNastyType NastyType, ParticleEffect Effect)
    {
        if (NastyType == enNastyType.FriedEgg)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(125, 62, 62).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(0, 182, 0).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Gonzo)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(4, 82, 184).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(182, 103, 10).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Cushion)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(59, 194, 42).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(227, 66, 19).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Helmet)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(202, 109, 17).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(137, 137, 137).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Shellfish)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(239, 238, 0).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(198, 99, 0).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Snail)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 124, 0).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(141, 141, 141).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(166, 198, 0).ToVector3();
        }
        else if (NastyType == enNastyType.CurleyWurley)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(168, 163, 252).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(109, 109, 253).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Bird)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 0, 211).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(148, 148, 217).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(188, 62, 0).ToVector3();
        }
        else if (NastyType == enNastyType.Ghost)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 151, 43).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(88, 0, 88).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(255, 255, 255).ToVector3();
        }
        else if (NastyType == enNastyType.Scorpian)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(177, 8, 8).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(130, 2, 2).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(230, 230, 230).ToVector3();
        }
        else if (NastyType == enNastyType.DragonFly)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(8, 150, 219).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(50, 111, 85).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(76, 191, 142).ToVector3();
        }
        else if (NastyType == enNastyType.Helicopter)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(177, 8, 8).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(130, 2, 2).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(230, 230, 230).ToVector3();
        }
        else if (NastyType == enNastyType.Hippo)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(229, 120, 252).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(137, 115, 251).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(145, 49, 50).ToVector3();
        }
        else if (NastyType == enNastyType.Centipede)
        {
            Effect["Copy of Left Splat"].ReleaseColour.Value = new Color(87, 165, 28).ToVector3();
            Effect["Copy of Right Splat"].ReleaseColour.Value = new Color(110, 6, 124).ToVector3();
            Effect["Copy of Splodges"].ReleaseColour.Value = new Color(221, 134, 38).ToVector3();
        }
    }
}