using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectMercury;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;

namespace StarquakeXNA
{
    // This wraps multiple nasties
    public class Nasties : GameComponentBase
    {
        int nastyCount = 1;
        int previousScreenVisited = 0;
        List<Nasty> nasties = null;
        IPositionalTriggerParticleEffect nastySmokes;
        public event RaiseInteractionWithBlob OnNastyInteractionWithBlob;

        public Nasties(GameEngine GameEngineReference, int NastyCount)
            : base(GameEngineReference)
        {
            nastyCount = NastyCount;
            nasties = new List<Nasty>();
            nastySmokes =
                gameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                    enParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

            // Create double the number needed for screen swap operations
            for (int a = 0; a < (nastyCount * 2); a++)
            {
                nasties.Add(
                    new Nasty(
                        GameEngineReference,
                        0,
                        0,
                        3));
            }
        }

        #region IGameComponent Members

        public void LoadContent(ContentList Content)
        {
            for (int a = 0; a < (nastyCount * 2); a++)
            {
                nasties[a].LoadContent(Content);
            }
        }

        public void Initialise()
        {
            for (int a = 0; a < (nastyCount * 2); a++)
            {
                nasties[a].Initialise();
            }

            // Maybe we should call this on start of the game for all involved....?
            PerformPostChangeScreenActions();
        }

        public void Reset()
        {
            for (int a = 0; a < nastyCount; a++)
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
                    for (int a = 0; a < nastyCount; a++)
                    {
                        nasties[a].NastyState = enNastyState.Dead;
                    }
                }

                // Flag that's set if a nasty is touching Blob already - stops all of them zapping his energy at once which to too quick and unfair
                bool nastyInteractingWithBlob = false;
                int nastySpawnedCounter = 0;

                for (int a = 0; a < nastyCount; a++)
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
                            for (int b = 0;
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
                                        enDirection NastyTravelDirection = CollisionDetection.CheckCollisionDirection(
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

        void Swap(List<Nasty> list, int a, int b)
        {
            Nasty temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        public void PerformPostChangeScreenActions()
        {
            if (gameEngine.GameScreens.ScreenNumber != previousScreenVisited)
            {
                // Save the nasties on the screen we have left into the previous screen array
                for (int a = 0; a < nastyCount; a++)
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
                for (int a = 0; a < nastyCount; a++)
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

                enNastyType nastyType = enNastyType.Ghost;
                if (gameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
                {
                    nastyType = Nasty.GetRandomNastyType(gameEngine, true);
                }

                // Reset the nasties
                for (int a = 0; a < nastyCount; a++)
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

            for (int a = 0; a < nastyCount; a++)
            {
                nasties[a].PerformPostChangeScreenActions();
            }

            // Deactivate some nasties if we have conveyer nasties on screen
            if (gameEngine.ConveyerNastiesInstance.NastiesActive > 0)
            {
                for (int a = 0; a < gameEngine.ConveyerNastiesInstance.NastiesActive; a++)
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
                for (int a = 0; a < nastyCount; a++)
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
        private enum MoveType
        {
            RandomMovement,
            RandomUpDownLeftRight,
            DiagonalBouncing,
            DiagonalBouncingInSteps,
            RandomMovementsAllDirections,
            SpikeyMovement,
        }


        // When shot monsters usually dissappear for good but there is a small chance they'll regenerate
        // Tech stuff
        SpriteCollection nastySprite;

        SpriteCollection nastySpriteNew;
        SpriteCollection nastySpriteOriginal;

        Animation nastyDiesAnimation;

        // Behaviour
        MoveType movementTypeInternal;

        MoveType movementType
        {
            get { return movementTypeInternal; }
            set
            {
                movementTypeInternal = value;
                //movementTypeInternal = MoveType.DiagonalBouncing;
            }
        }

        bool attractedToBlob; // They got through phases of being attracted to Blob
        //bool attractedToBlobFlag;

        enDirection nastyDirection;

        public enDirection Direction
        {
            get { return nastyDirection; }
        }

        bool constantMovement; // Movement type that doesn't rely on a number of steps

        int moveSpeedInternal;

        int moveSpeed
        {
            get { return moveSpeedInternal; }
            set
            {
                moveSpeedInternal = value;
                //moveSpeedInternal = 40;
            }
        }

        public int Speed
        {
            get { return moveSpeed; }
        }

        enDirection lastCollisionDirection;

        // Birth stuff
        int birthFadeLevel = 0;
        int birthColourLevel = 0;
        int birthTimer = 0;

        // The number of frames for the birth to appear (60 frames = 1 second)
        const int birthPeriod = 60;

        float birthScale = 0f;
        float birthRotateSpeed = 8.0f;

        // Counters
        int conceptionwaitCounter;
        int movementChangeCounter;
        int movementChangeCount;
        int stepsMovedCounter;
        int stepsToMove;
        bool spikeyPause = false;
        bool firstMovement = false;

        bool keepNastyTheSame = false;

        public bool KeepNastyTheSame
        {
            get { return keepNastyTheSame; }
            set { keepNastyTheSame = value; }
        }

        // Death stuff
        int deathFadeLevel = 255;
        float deathScale = 1f;
        bool deathAnticlockwise = false;

        int nastyWidth = 64;

        public int NastyWidth
        {
            get { return nastyWidth; }
            set { nastyWidth = value; }
        }

        enNastyState nastyState;

        public enNastyState NastyState
        {
            get { return nastyState; }
            set { nastyState = value; }
        }

        int nastyHeight = 64;

        public int NastyHeight
        {
            get { return nastyHeight; }
            set { nastyHeight = value; }
        }

        bool smoking;

        public bool Smoking
        {
            get { return smoking; }
            set { smoking = value; }
        }

        // Position
        private int xPos;

        public int XPos
        {
            get { return xPos; }
            set { xPos = value; }
        }

        private int yPos;

        public int YPos
        {
            get { return yPos; }
            set { yPos = value; }
        }

        /// <summary>
        /// If the nasty is attracted to blob but something is in the way it won't be able to move
        /// So this records such an instance and releases it after a number of frames
        /// </summary>
        private int nastyStuckCounter;

        private int prevXpos;
        private int prevYpos;
        private int nastyDeadPauseCounter;

        private enNastyType nastyType;

        public enNastyType NastyType
        {
            get { return nastyType; }
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
                            (Enum.GetValues(typeof(MoveType)).Length - 1));
                }
            }
        }

        // Animation
        Animation nastyAnim;
        IStandardParticleEffect nastyDies;
        IStandardParticleEffect nastyDiesType2;

        public Nasty(
            GameEngine GameEngineReference,
            int Xposition,
            int Yposition,
            int MoveSpeed)
            : base(GameEngineReference)
        {
            xPos = Xposition;
            yPos = Yposition;
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

        public void CopyFromSource(Nasty CopySource)
        {
            this.movementType = CopySource.movementType;
            this.attractedToBlob = CopySource.attractedToBlob;
            this.nastyDirection = CopySource.nastyDirection;
            this.stepsMovedCounter = CopySource.stepsMovedCounter;
            this.nastyDirection = CopySource.nastyDirection;
            this.stepsMovedCounter = CopySource.stepsMovedCounter;
            this.stepsToMove = CopySource.stepsToMove;
            this.constantMovement = CopySource.constantMovement;
            this.moveSpeed = CopySource.moveSpeed;
            this.lastCollisionDirection = CopySource.lastCollisionDirection;
            this.movementChangeCounter = CopySource.movementChangeCounter;
            this.movementChangeCount = CopySource.movementChangeCount;
            this.keepNastyTheSame = CopySource.keepNastyTheSame;
            this.nastyType = CopySource.nastyType;
            this.xPos = CopySource.xPos;
            this.yPos = CopySource.yPos;
            this.nastyDeadPauseCounter = CopySource.nastyDeadPauseCounter;

            // If the basty is dead reset it to conceptive state
            if (CopySource.nastyState == enNastyState.Dead)
            {
                this.nastyState = enNastyState.PreConception;
            }

            // Ensure that we don't carry on generating after moving screens
            if (CopySource.nastyState == enNastyState.Birth)
            {
                this.nastyState = enNastyState.Normal;
            }
            else
            {
                this.nastyState = CopySource.nastyState;
            }
        }

        #region IGameComponent Members

        private Point GetNastySpawnLocation()
        {
            // Get all locations where we can have nasties from the current game screen
            List<Point> potentialLocations = gameEngine.GameScreens.GetNastyPotentialLocations();

            if (potentialLocations.Count > 0)
            {
                int nastyLocationIndex = gameEngine.RandomNumberGenerator.Next(0, potentialLocations.Count);

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
            nastyDies.ParticleEffectComplete += new RaiseEffectComplete(nastyDies_ParticleEffectComplete);

            nastyDiesType2 =
                gameEngine.ContentResourceManager.GetParticleEffectInstance("NastySplat",
                    enParticleEffectType.Triggered, null) as IStandardParticleEffect;
            nastyDiesType2.ParticleEffectComplete += new RaiseEffectComplete(nastyDies_ParticleEffectComplete);
        }

        void CheckSpritesInUse()
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

        void nastyDies_ParticleEffectComplete()
        {
            if (nastyState == enNastyState.Dead)
            {
                // Extend this to pause for random time before bringing a nasty back to life
                nastyDeadPauseCounter = 0;
                nastyState = enNastyState.PreConception;
                conceptionwaitCounter = gameEngine.RandomNumberGenerator.Next(0, 500);
            }
        }

        public void Initialise()
        {
            stepsMovedCounter = 0;
            stepsToMove = 0;

            if (movementType == MoveType.DiagonalBouncing)
            {
                nastyDirection = (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
            }
            else if (movementType == MoveType.RandomMovement ||
                     movementType == MoveType.RandomUpDownLeftRight)
            {
                nastyDirection = (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // select from 1st four
            }
        }

        public static enNastyType GetRandomNastyType(GameEngine GameEngineReference, bool KeepNastiesTheSame)
        {
            //NastyType = (enNastyType)gameEngine.RandomNumberGenerator.Next(0, 15); // Pick from first 15 types

            int temp = 0;

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
            else if (temp == 1)
            {
                return enNastyType.Helmet;
            }
            else if (temp == 2)
            {
                return enNastyType.FriedEgg;
            }
            else if (temp == 3)
            {
                return enNastyType.CurleyWurley;
            }
            else if (temp == 4)
            {
                return enNastyType.Shellfish;
            }
            else if (temp == 5)
            {
                return enNastyType.Cushion;
            }
            else if (temp == 6)
            {
                return enNastyType.Gonzo;
            }
            else if (temp == 7)
            {
                return enNastyType.Snail;
            }
            else if (temp == 8)
            {
                return enNastyType.Ghost;
            }
            else if (temp == 9)
            {
                return enNastyType.Centipede;
            }
            else if (temp == 10)
            {
                return enNastyType.Scorpian;
            }
            else if (temp == 11)
            {
                return enNastyType.DragonFly;
            }
            else if (temp == 12)
            {
                return enNastyType.Hippo;
            }
            else if (temp == 13)
            {
                return enNastyType.Helicopter;
            }
            else if (temp == 14)
            {
                return enNastyType.Bird;
            }
            else
            {
                return enNastyType.Spiky;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (gameEngine.GameScreens.ScreenNumber == 198)
            {
                nastyState = enNastyState.Disabled;
            }

            if (gameEngine.GameScreens.ScreenNumber == 199)
            {
                nastyType = enNastyType.CoreGuardian;
                attractedToBlob = false;
            }

            // Reset conception variables
            if (nastyState == enNastyState.PreConception)
            {
                conceptionwaitCounter = gameEngine.RandomNumberGenerator.Next(0, 60);
                nastyState = enNastyState.Conception;

                if (!keepNastyTheSame)
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
            else if (nastyState == enNastyState.Conception)
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
                    Point NewPosition = GetNastySpawnLocation();
                    if (NewPosition == new Point(0, 0))
                    {
                        nastyState = enNastyState.Disabled;
                    }
                    else
                    {
                        xPos = NewPosition.X;
                        yPos = NewPosition.Y;
                    }

                    nastyState = enNastyState.Birth;
                }
            }
            else if (nastyState == enNastyState.Birth)
            {
                if (birthScale < 1.0f)
                {
                    birthScale = birthScale + (1f / (float)birthPeriod);
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
                        (int)((double)255 /
                              (double)((double)birthPeriod /
                                       2)); // We only have half the time to do the colour fade in 
                }


                // Once we're halfway through fade in the colours
                if (birthTimer > birthPeriod / 2)
                {
                    if (birthColourLevel < 255)
                    {
                        birthColourLevel =
                            birthColourLevel +
                            (int)((double)255 /
                                  (double)((double)birthPeriod /
                                           2)); // We only have half the time to do the colour fade in 
                    }

                    if (birthRotateSpeed > 16)
                    {
                        birthRotateSpeed = birthRotateSpeed - (16f / ((float)birthPeriod / 2));
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
                        nastyState = enNastyState.Normal;
                        birthTimer = 0;
                    }
                }

                birthTimer++;
            }
            else if (nastyState == enNastyState.Shot)
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

                nastyState = enNastyState.Dead;

                if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                {
                    deathAnticlockwise = true;
                }
                else
                {
                    deathAnticlockwise = false;
                }
            }
            else if (nastyState == enNastyState.Dead)
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
                        nastyState = enNastyState.PreConception;
                        conceptionwaitCounter = gameEngine.RandomNumberGenerator.Next(0, 500);
                        nastyDeadPauseCounter = 0;
                    }
                }
            }

            if (nastyDies != null)
            {
                // Update the explosion
                nastyDies.Update(xPos + 32, yPos + 32, gameTime);
            }

            if (nastyDiesType2 != null)
            {
                // Update the splat
                nastyDiesType2.Update(xPos + 32, yPos + 32, gameTime);
            }

            bool updateAnimation = false;

            if (nastyState != enNastyState.Disabled &&
                nastyState != enNastyState.PreConception &&
                nastyState != enNastyState.Conception &&
                nastyState != enNastyState.Birth)
            {
                updateAnimation = true;
            }

            if (nastyState == enNastyState.Birth &&
                birthTimer > (birthPeriod / 4))
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
                        if (prevXpos == xPos &&
                            prevYpos == yPos)
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

                        prevXpos = xPos;
                        prevYpos = yPos;
                    }
                }

                int amountToMoveX = 0;
                int amountToMoveY = 0;

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
                        bool moveOneStep = true;

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
                        nastyDirection = enDirection.None;
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
                        int attractionFlag = gameEngine.RandomNumberGenerator.Next(0, 2);

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
                                    (Enum.GetValues(typeof(MoveType)).Length - 1));
                            }

                            if (movementType == MoveType.DiagonalBouncing)
                            {
                                nastyDirection =
                                    (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // select from 2nd four
                            }
                            else if (movementType == MoveType.RandomMovement ||
                                     movementType == MoveType.RandomUpDownLeftRight)
                            {
                                nastyDirection =
                                    (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // select from 1st four
                            }
                            else if (movementType == MoveType.DiagonalBouncingInSteps)
                            {
                                nastyDirection =
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
            if (lastCollisionDirection == enDirection.None || nastyDirection == enDirection.None)
            {
                nastyDirection = (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // Select from last 4
            }

            if (nastyDirection == enDirection.UpLeft)
            {
                if (lastCollisionDirection == enDirection.UpLeft)
                {
                    nastyDirection = enDirection.DownRight;
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    nastyDirection = enDirection.UpRight;
                }
                else if (lastCollisionDirection == enDirection.Up)
                {
                    nastyDirection = enDirection.DownLeft;
                }
                else
                {
                    nastyDirection = enDirection.DownLeft; // Didn't hit anything, just change direction
                }
            }
            else if (nastyDirection == enDirection.UpRight)
            {
                if (lastCollisionDirection == enDirection.UpRight)
                {
                    nastyDirection = enDirection.DownLeft;
                }
                else if (lastCollisionDirection == enDirection.Right)
                {
                    nastyDirection = enDirection.UpLeft;
                }
                else if (lastCollisionDirection == enDirection.Up)
                {
                    nastyDirection = enDirection.DownRight;
                }
                else
                {
                    nastyDirection = enDirection.DownRight; // Didn't hit anything, just change direction
                }
            }
            else if (nastyDirection == enDirection.DownLeft)
            {
                if (lastCollisionDirection == enDirection.DownLeft)
                {
                    nastyDirection = enDirection.UpRight;
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    nastyDirection = enDirection.DownRight;
                }
                else if (lastCollisionDirection == enDirection.Down)
                {
                    nastyDirection = enDirection.UpLeft;
                }
                else
                {
                    nastyDirection = enDirection.UpLeft; // Didn't hit anything, just change direction
                }
            }
            else if (nastyDirection == enDirection.DownRight)
            {
                if (lastCollisionDirection == enDirection.DownRight)
                {
                    nastyDirection = enDirection.UpLeft;
                }
                else if (lastCollisionDirection == enDirection.Right)
                {
                    nastyDirection = enDirection.DownLeft;
                }
                else if (lastCollisionDirection == enDirection.Down)
                {
                    nastyDirection = enDirection.UpRight;
                }
                else
                {
                    nastyDirection = enDirection.UpRight; // Didn't hit anything, just change direction
                }
            }
        }

        private void DiagonalBouncing(int BlobsXPosition, int BlobsYPosition)
        {
            if (nastyDirection == enDirection.None)
            {
                nastyDirection = (enDirection)gameEngine.RandomNumberGenerator.Next(4, 8); // Select from last 4
            }

            if (nastyDirection == enDirection.UpLeft)
            {
                if (lastCollisionDirection == enDirection.UpLeft)
                {
                    nastyDirection = enDirection.DownRight;
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    nastyDirection = enDirection.UpRight;
                }
                else if (lastCollisionDirection == enDirection.Up)
                {
                    nastyDirection = enDirection.DownLeft;
                }
            }
            else if (nastyDirection == enDirection.UpRight)
            {
                if (lastCollisionDirection == enDirection.UpRight)
                {
                    nastyDirection = enDirection.DownLeft;
                }
                else if (lastCollisionDirection == enDirection.Right)
                {
                    nastyDirection = enDirection.UpLeft;
                }
                else if (lastCollisionDirection == enDirection.Up)
                {
                    nastyDirection = enDirection.DownRight;
                }
            }
            else if (nastyDirection == enDirection.DownLeft)
            {
                if (lastCollisionDirection == enDirection.DownLeft)
                {
                    nastyDirection = enDirection.UpRight;
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    nastyDirection = enDirection.DownRight;
                }
                else if (lastCollisionDirection == enDirection.Down)
                {
                    nastyDirection = enDirection.UpLeft;
                }
            }
            else if (nastyDirection == enDirection.DownRight)
            {
                if (lastCollisionDirection == enDirection.DownRight)
                {
                    nastyDirection = enDirection.UpLeft;
                }
                else if (lastCollisionDirection == enDirection.Right)
                {
                    nastyDirection = enDirection.DownLeft;
                }
                else if (lastCollisionDirection == enDirection.Down)
                {
                    nastyDirection = enDirection.UpRight;
                }
            }

            stepsToMove = gameEngine.RandomNumberGenerator.Next(8, 40);
        }

        private void RandomUpDownLeftRight(int BlobsXPosition, int BlobsYPosition)
        {
            if (lastCollisionDirection == enDirection.None || nastyDirection == enDirection.None)
            {
                nastyDirection = (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // Only select fron first 4
            }

            if (lastCollisionDirection == enDirection.Up)
            {
                nastyDirection = enDirection.Down;
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                nastyDirection = enDirection.Up;
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                nastyDirection = enDirection.Right;
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                nastyDirection = enDirection.Left;
            }

            // If attracted to blob is turned on
            if (attractedToBlob)
            {
                if (xPos < BlobsXPosition)
                {
                    if (lastCollisionDirection != enDirection.Right)
                    {
                        nastyDirection = enDirection.Right;
                    }
                }
                else if (xPos > BlobsXPosition)
                {
                    if (lastCollisionDirection != enDirection.Left)
                    {
                        nastyDirection = enDirection.Left;
                    }
                }


                if (yPos < BlobsYPosition)
                {
                    if (lastCollisionDirection != enDirection.Down)
                    {
                        if (nastyDirection == enDirection.Left)
                        {
                            nastyDirection = enDirection.DownLeft;
                        }
                        else if (nastyDirection == enDirection.Right)
                        {
                            nastyDirection = enDirection.DownRight;
                        }
                        else
                        {
                            nastyDirection = enDirection.Down;
                        }
                    }
                }
                else if (yPos > BlobsYPosition)
                {
                    if (lastCollisionDirection != enDirection.Up)
                    {
                        if (nastyDirection == enDirection.Left)
                        {
                            nastyDirection = enDirection.UpLeft;
                        }
                        else if (nastyDirection == enDirection.Right)
                        {
                            nastyDirection = enDirection.UpRight;
                        }
                        else
                        {
                            nastyDirection = enDirection.Up;
                        }
                    }
                }
            }

            stepsToMove = gameEngine.RandomNumberGenerator.Next(8, 40);
        }

        private void RandomMovement(int BlobsXPosition, int BlobsYPosition)
        {
            if (lastCollisionDirection == enDirection.None || nastyDirection == enDirection.None)
            {
                nastyDirection = (enDirection)gameEngine.RandomNumberGenerator.Next(0, 4); // Only select fron first 4
            }

            if (lastCollisionDirection == enDirection.Up)
            {
                nastyDirection = enDirection.Down;
            }
            else if (lastCollisionDirection == enDirection.Down)
            {
                nastyDirection = enDirection.Up;
            }
            else if (lastCollisionDirection == enDirection.Left)
            {
                nastyDirection = enDirection.Right;
            }
            else if (lastCollisionDirection == enDirection.Right)
            {
                nastyDirection = enDirection.Left;
            }

            // If attracted to blob is turned on
            if (attractedToBlob)
            {
                nastyDirection = enDirection.None;

                if (xPos < BlobsXPosition)
                {
                    if (lastCollisionDirection != enDirection.Right)
                    {
                        nastyDirection = enDirection.Right;
                    }
                }
                else if (xPos > BlobsXPosition)
                {
                    if (lastCollisionDirection != enDirection.Left)
                    {
                        nastyDirection = enDirection.Left;
                    }
                }


                if (yPos < BlobsYPosition)
                {
                    if (lastCollisionDirection != enDirection.Down)
                    {
                        if (nastyDirection == enDirection.Left)
                        {
                            nastyDirection = enDirection.DownLeft;
                        }
                        else if (nastyDirection == enDirection.Right)
                        {
                            nastyDirection = enDirection.DownRight;
                        }
                        else
                        {
                            nastyDirection = enDirection.Down;
                        }
                    }
                }
                else if (yPos > BlobsYPosition)
                {
                    if (lastCollisionDirection != enDirection.Up)
                    {
                        if (nastyDirection == enDirection.Left)
                        {
                            nastyDirection = enDirection.UpLeft;
                        }
                        else if (nastyDirection == enDirection.Right)
                        {
                            nastyDirection = enDirection.UpRight;
                        }
                        else
                        {
                            nastyDirection = enDirection.Up;
                        }
                    }
                }
            }

            stepsToMove = gameEngine.RandomNumberGenerator.Next(1, 4);
        }

        private void RandomMovementsAllDirections(int BlobsXPosition, int BlobsYPosition)
        {
            if (lastCollisionDirection == enDirection.None || nastyDirection == enDirection.None)
            {
                nastyDirection =
                    (enDirection)gameEngine.RandomNumberGenerator.Next(0, 8); // Select from all movement directions
            }

            if (!attractedToBlob)
            {
                if (lastCollisionDirection == enDirection.Up)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 2);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Down;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.DownLeft;
                    }
                    else
                    {
                        nastyDirection = enDirection.DownRight;
                    }
                }
                else if (lastCollisionDirection == enDirection.Down)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Up;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.UpLeft;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpRight;
                    }
                }
                else if (lastCollisionDirection == enDirection.Left)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Right;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.DownRight;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpRight;
                    }
                }
                else if (lastCollisionDirection == enDirection.Right)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Left;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.DownLeft;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpLeft;
                    }
                }
                else if (lastCollisionDirection == enDirection.UpLeft)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Down;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.DownLeft;
                    }
                    else
                    {
                        nastyDirection = enDirection.DownRight;
                    }
                }
                else if (lastCollisionDirection == enDirection.UpRight)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Down;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.DownRight;
                    }
                    else
                    {
                        nastyDirection = enDirection.DownLeft;
                    }
                }
                else if (lastCollisionDirection == enDirection.DownLeft)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Up;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.UpLeft;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpRight;
                    }
                }
                else if (lastCollisionDirection == enDirection.DownRight)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Up;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.UpRight;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpLeft;
                    }
                }
            }
            else // Attracted to Blob
            {
                if (xPos <= BlobsXPosition && yPos <= BlobsYPosition)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Right;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.Down;
                    }
                    else
                    {
                        nastyDirection = enDirection.DownRight;
                    }
                }
                else if (xPos <= BlobsXPosition && yPos >= BlobsYPosition)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Right;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.Up;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpRight;
                    }
                }
                else if (xPos >= BlobsXPosition && yPos >= BlobsYPosition)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Left;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.Up;
                    }
                    else
                    {
                        nastyDirection = enDirection.UpLeft;
                    }
                }
                else if (xPos >= BlobsXPosition && yPos <= BlobsYPosition)
                {
                    int newDirection = gameEngine.RandomNumberGenerator.Next(0, 3);

                    if (newDirection == 0)
                    {
                        nastyDirection = enDirection.Left;
                    }
                    else if (newDirection == 1)
                    {
                        nastyDirection = enDirection.Down;
                    }
                    else
                    {
                        nastyDirection = enDirection.DownLeft;
                    }
                }
            }

            stepsToMove = gameEngine.RandomNumberGenerator.Next(10, 40);
        }

        private void SpikeyMovement(int BlobsXPosition, int BlobsYPosition)
        {
            int stepsFromBlob = 0;

            if (!spikeyPause)
            {
                if (xPos <= BlobsXPosition && yPos <= BlobsYPosition)
                {
                    int distanceX = BlobsXPosition - xPos;
                    int distanceY = BlobsYPosition - yPos;

                    if (distanceX > distanceY)
                    {
                        nastyDirection = enDirection.Right;
                        stepsFromBlob = BlobsXPosition - xPos;
                    }
                    else if (distanceY > distanceX)
                    {
                        nastyDirection = enDirection.Down;
                        stepsFromBlob = BlobsYPosition - yPos;
                    }

                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        if (nastyDirection == enDirection.Down)
                        {
                            nastyDirection = enDirection.DownRight;
                            stepsFromBlob = BlobsYPosition - yPos;
                        }
                        else if (nastyDirection == enDirection.Right)
                        {
                            nastyDirection = enDirection.DownRight;
                            stepsFromBlob = BlobsXPosition - xPos;
                        }
                    }

                    if (lastCollisionDirection == enDirection.Right)
                    {
                        nastyDirection = enDirection.Down;
                        stepsFromBlob = BlobsYPosition - yPos;
                    }
                    else if (lastCollisionDirection == enDirection.Down)
                    {
                        nastyDirection = enDirection.Right;
                        stepsFromBlob = BlobsXPosition - xPos;
                    }
                    else if (lastCollisionDirection == enDirection.DownRight)
                    {
                        if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                        {
                            nastyDirection = enDirection.Right;
                            stepsFromBlob = BlobsXPosition - xPos;
                        }
                        else
                        {
                            nastyDirection = enDirection.Down;
                            stepsFromBlob = BlobsYPosition - yPos;
                        }
                    }
                }
                else if (xPos <= BlobsXPosition && yPos >= BlobsYPosition)
                {
                    int distanceX = BlobsXPosition - xPos;
                    int distanceY = yPos - BlobsYPosition;

                    if (distanceX > distanceY)
                    {
                        nastyDirection = enDirection.Right;
                        stepsFromBlob = BlobsXPosition - xPos;
                    }
                    else if (distanceY > distanceX)
                    {
                        nastyDirection = enDirection.Up;
                        stepsFromBlob = yPos - BlobsYPosition;
                    }

                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        if (nastyDirection == enDirection.Right)
                        {
                            nastyDirection = enDirection.UpRight;
                            stepsFromBlob = BlobsXPosition - xPos;
                        }
                        else if (nastyDirection == enDirection.Up)
                        {
                            nastyDirection = enDirection.UpRight;
                            stepsFromBlob = yPos - BlobsYPosition;
                        }
                    }

                    if (lastCollisionDirection == enDirection.Up)
                    {
                        nastyDirection = enDirection.Right;
                        stepsFromBlob = BlobsXPosition - xPos;
                    }
                    else if (lastCollisionDirection == enDirection.Right)
                    {
                        nastyDirection = enDirection.Up;
                        stepsFromBlob = yPos - BlobsYPosition;
                    }
                    else if (lastCollisionDirection == enDirection.UpRight)
                    {
                        if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                        {
                            nastyDirection = enDirection.Right;
                            stepsFromBlob = BlobsXPosition - xPos;
                        }
                        else
                        {
                            nastyDirection = enDirection.Up;
                            stepsFromBlob = yPos - BlobsYPosition;
                        }
                    }
                }
                else if (xPos >= BlobsXPosition && yPos >= BlobsYPosition)
                {
                    int distanceX = xPos - BlobsXPosition;
                    int distanceY = yPos - BlobsYPosition;

                    if (distanceX > distanceY)
                    {
                        nastyDirection = enDirection.Left;
                        stepsFromBlob = xPos - BlobsXPosition;
                    }
                    else if (distanceY > distanceX)
                    {
                        nastyDirection = enDirection.Up;
                        stepsFromBlob = yPos - BlobsYPosition;
                    }

                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        if (nastyDirection == enDirection.Left)
                        {
                            nastyDirection = enDirection.UpLeft;
                            stepsFromBlob = xPos - BlobsXPosition;
                        }
                        else if (nastyDirection == enDirection.Up)
                        {
                            nastyDirection = enDirection.UpLeft;
                            stepsFromBlob = yPos - BlobsYPosition;
                        }
                    }

                    if (lastCollisionDirection == enDirection.Up)
                    {
                        nastyDirection = enDirection.Left;
                        stepsFromBlob = xPos - BlobsXPosition;
                    }
                    else if (lastCollisionDirection == enDirection.Left)
                    {
                        nastyDirection = enDirection.Up;
                        stepsFromBlob = yPos - BlobsYPosition;
                    }
                    else if (lastCollisionDirection == enDirection.UpLeft)
                    {
                        if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                        {
                            nastyDirection = enDirection.Left;
                            stepsFromBlob = xPos - BlobsXPosition;
                        }
                        else
                        {
                            nastyDirection = enDirection.Up;
                            stepsFromBlob = yPos - BlobsYPosition;
                        }
                    }
                }
                else if (xPos >= BlobsXPosition && yPos <= BlobsYPosition)
                {
                    int distanceX = xPos - BlobsXPosition;
                    int distanceY = BlobsYPosition - yPos;

                    if (distanceX > distanceY)
                    {
                        nastyDirection = enDirection.Left;
                        stepsFromBlob = xPos - BlobsXPosition;
                    }
                    else if (distanceY > distanceX)
                    {
                        nastyDirection = enDirection.Down;
                        stepsFromBlob = BlobsYPosition - yPos;
                    }

                    if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                    {
                        if (nastyDirection == enDirection.Left)
                        {
                            nastyDirection = enDirection.DownLeft;
                            stepsFromBlob = xPos - BlobsXPosition;
                        }
                        else if (nastyDirection == enDirection.Down)
                        {
                            nastyDirection = enDirection.DownLeft;
                            stepsFromBlob = BlobsYPosition - yPos;
                        }
                    }

                    if (lastCollisionDirection == enDirection.Down)
                    {
                        nastyDirection = enDirection.Left;
                        stepsFromBlob = xPos - BlobsXPosition;
                    }
                    else if (lastCollisionDirection == enDirection.Left)
                    {
                        nastyDirection = enDirection.Down;
                        stepsFromBlob = BlobsYPosition - yPos;
                    }
                    else if (lastCollisionDirection == enDirection.DownLeft)
                    {
                        if (gameEngine.RandomNumberGenerator.Next(0, 2) == 0)
                        {
                            nastyDirection = enDirection.Left;
                            stepsFromBlob = xPos - BlobsXPosition;
                        }
                        else
                        {
                            nastyDirection = enDirection.Down;
                            stepsFromBlob = BlobsYPosition - yPos;
                        }
                    }
                }
            }
            else
            {
                nastyDirection = enDirection.None;
            }

            if (!spikeyPause)
            {
                stepsToMove = gameEngine.RandomNumberGenerator.Next(
                                  (int)(((double)stepsFromBlob / (double)moveSpeed) / 2),
                                  (int)((double)stepsFromBlob / (double)moveSpeed)) +
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

            if (nastyDirection == enDirection.Down)
            {
                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);

                if (amountToMoveY == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                yPos = yPos + amountToMoveY;
            }
            else if (nastyDirection == enDirection.Up)
            {
                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);

                if (amountToMoveY == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                yPos = yPos - amountToMoveY;
            }
            else if (nastyDirection == enDirection.Left)
            {
                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);

                if (amountToMoveX == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                xPos = xPos - amountToMoveX;
            }
            else if (nastyDirection == enDirection.Right)
            {
                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);

                if (amountToMoveX == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                xPos = xPos + amountToMoveX;
            }
            else if (nastyDirection == enDirection.UpLeft)
            {
                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);
                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, xPos, yPos - amountToMoveY,
                    nastyWidth, nastyHeight, moveSpeed, false);

                if (amountToMoveY == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }
                else if (amountToMoveX == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                yPos = yPos - amountToMoveY;
                xPos = xPos - amountToMoveX;
            }
            else if (nastyDirection == enDirection.DownLeft)
            {
                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);
                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, xPos, yPos + amountToMoveY,
                    nastyWidth, nastyHeight, moveSpeed, false);

                if (amountToMoveY == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }
                else if (amountToMoveX == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                yPos = yPos + amountToMoveY;
                xPos = xPos - amountToMoveX;
            }
            else if (nastyDirection == enDirection.UpRight)
            {
                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);
                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, xPos, yPos - amountToMoveY,
                    nastyWidth, nastyHeight, moveSpeed, false);

                if (amountToMoveY == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }
                else if (amountToMoveX == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                yPos = yPos - amountToMoveY;
                xPos = xPos + amountToMoveX;
            }
            else if (nastyDirection == enDirection.DownRight)
            {
                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, xPos, yPos, nastyWidth,
                    nastyHeight, moveSpeed, false);
                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, xPos, yPos + amountToMoveY,
                    nastyWidth, nastyHeight, moveSpeed, false);

                if (amountToMoveY == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }
                else if (amountToMoveX == 0)
                {
                    stepsMovedCounter = stepsToMove;
                }

                yPos = yPos + amountToMoveY;
                xPos = xPos + amountToMoveX;
            }

            if (nastyDirection == enDirection.Left)
            {
                if (amountToMoveX == 0)
                {
                    lastCollisionDirection = enDirection.Left;
                }
            }
            else if (nastyDirection == enDirection.Right)
            {
                if (amountToMoveX == 0)
                {
                    lastCollisionDirection = enDirection.Right;
                }
            }
            else if (nastyDirection == enDirection.Up)
            {
                if (amountToMoveY == 0)
                {
                    lastCollisionDirection = enDirection.Up;
                }
            }
            else if (nastyDirection == enDirection.Down)
            {
                if (amountToMoveY == 0)
                {
                    lastCollisionDirection = enDirection.Down;
                }
            }
            else if (nastyDirection == enDirection.UpLeft)
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
            else if (nastyDirection == enDirection.UpRight)
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
            else if (nastyDirection == enDirection.DownLeft)
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
            else if (nastyDirection == enDirection.DownRight)
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

            if (nastyState == enNastyState.Disabled)
            {
                nastyState = enNastyState.Conception;
            }
        }

        public override void Draw(GameTime gameTime, int ScreenFade)
        {
            CheckSpritesInUse();

            int nastyImage = (((int)nastyType) * 4);

            if (nastyType == enNastyType.CoreGuardian)
            {
                nastySprite.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

                // Choose the image
                nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                    ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade), 1.0f, 4f, true, gameEngine.Paused);
                nastySprite.End();
            }
            else
            {
                if (nastyState == enNastyState.Birth)
                {
                    nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

                    // Choose the image
                    nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                        ColourHelper.GetXNA4Colour((byte)birthColourLevel, (byte)birthColourLevel, 255,
                            (byte)((double)255 * (double)((double)ScreenFade / 255))), birthScale, birthRotateSpeed,
                        true, gameEngine.Paused);
                    nastySprite.End();
                }
                else if (nastyState == enNastyState.Normal)
                {
                    // Choose the image
                    if (smoking)
                    {
                        nastySprite.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                        nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                            ColourHelper.GetXNA4Colour(50, 50, 50, (byte)ScreenFade));
                        nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                            ColourHelper.GetXNA4Colour(255, 0, 0, (byte)ScreenFade));
                        nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                            ColourHelper.GetXNA4Colour(0, 100, 0, (byte)ScreenFade));
                    }
                    else
                    {
                        nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                            ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                    }

                    nastySprite.End();
                }
                else if (nastyState == enNastyState.Dead)
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
                        nastySprite.DrawSprite(nastyImage + nastyAnim.CurrentFrame, xPos, yPos,
                            ColourHelper.GetXNA4Colour(255, 255, 255,
                                (byte)((double)deathFadeLevel * (double)((double)ScreenFade / 255))), deathScale, 16f,
                            deathAnticlockwise, gameEngine.Paused);
                        nastySprite.End();
                    }

                    if (gameEngine.UsingOldGraphics)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original")
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(
                            nastyDiesAnimation.CurrentFrame, xPos, yPos,
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
}