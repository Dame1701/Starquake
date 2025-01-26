using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using StarquakeXNA.GameComponents;
using Microsoft.Xna.Framework;
using StarquakeXNA.ContentManagement;
using SharedLibs;
using StarquakeXNA.Particles;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public class Blob : GameComponentBase
    {
        int blobXPos;
        public int BlobXPos
        {
            get { return blobXPos; }
            set 
            { 
                blobXPos = value;
                blobScreenEntryLocation.X = value;
            }
        }

        int blobYPos;
        public int BlobYPos
        {
            get { return blobYPos; }
            set 
            { 
                blobYPos = value;
                blobScreenEntryLocation.Y = value;
            }
        }

        bool blobCanMove = true;
        public bool BlobCanMove
        {
            get { return blobCanMove; }
            set { blobCanMove = value; }
        }

        bool blobTouchedByNasty = false;
        public bool BlobTouchedByNasty
        {
            get { return blobTouchedByNasty; }
            set { blobTouchedByNasty = value; }
        }

        int blobWidth = 64;
        public int BlobWidth
        {
            get { return blobWidth; }
            set { blobWidth = value; }
        }

        int blobHeight = 64;
        public int BlobHeight
        {
            get { return blobHeight; }
            set { blobHeight = value; }
        }

        int blobFrame = 5;
        public int BlobFrame
        {
            get { return blobFrame; }
            set { blobFrame = value; }
        }

        private Guid blobFallsSoundID;

        int blobHSpeed = 8;
        public int BlobHSpeed
        {
            get { return blobHSpeed; }
            set { blobHSpeed = value; }
        }

        int blobVSpeed = 8;
        public int BlobVSpeed
        {
            get { return blobVSpeed; }
            set { blobVSpeed = value; }
        }

        int blobFallSpeed = 4;
        int blobDistanceFallen = 0;
        int blobTotalDistanceFallen = 0;
        int blobFallHeight = 0;
        int blobOverloadState = 0;
        int blobAlarmSoundCounter = 0;

        float blobScale = 1f;

        int blobFireX = 0;
        public int BlobFireX
        {
            get { return blobFireX; }
            set { blobFireX = value; }
        }

        int blobFireY = 0;
        public int BlobFireY
        {
            get { return blobFireY; }
            set { blobFireY = value; }
        }

        int blobFireFade = 0;
        int blobFireSpeed = 16;
        int blobFireCollisionDirection = 0;
        int blobFireDistance = 0;

        const int blobFireDistanceBeforeColDetection = 128;
        
        bool jumpPressed = false;
        enDirection directionGoingThroughTunnel = enDirection.None;
        bool inTunnelLift = false;
        int distanceInTunnelLift = 0;

        bool blobHasFired = false;
        public bool BlobHasFired
        {
            get { return blobHasFired; }
        }

        bool blobCanFire = true;
        bool blobOnShuttlePad = false;
        bool blobOnShuttleCached = false;
        bool blobOnShuttle = false;

        Point blobScreenEntryLocation;
        bool blobOnShuttleOnEntry = false;
        bool blobInTunnelLiftOnEntry = false;
        int blobFrameOnEntry = 0;
        bool resetBlobToScreenEntryPoint = false;
 
        int blobEnergyDrainCounter = 0;
        bool blobTouched = false;

        enBlobState blobsState;
        public enBlobState BlobsState
        {
            get { return blobsState; }
            set { blobsState = value; }
        }

        int blobMaxItemsHeld = 4;
        bool inventoryAccessed = false;
        enCollectables[] blobInventory;
        public enCollectables[] BlobInventory
        {
            get { return blobInventory; }
            set { blobInventory = value; }
        }

        TileDetected tileAboveBlob = null;
        TileDetected tileBelowBlob = null;
        TileDetected tileLeftBlob = null;
        TileDetected tileRightBlob = null;

        enDirection directionBlobMoved = enDirection.None;
        public enDirection DirectionBlobMoved
        {
            get { return directionBlobMoved; }
        }

        enDirection directionBlobFired = enDirection.None;
        enDirection blobsLastAnimDirection = enDirection.None;

        Animation blobAnimLeft;
        Animation blobAnimRight;
        Animation blobAnimFire2;
        Animation blobAnimFire1;

        Animation blobShuttleAnim;
        List<ResetTile> tilesToReset;

        CustomControl blobsControl;
        public CustomControl BlobsControl
        {
            get { return blobsControl; }
        }

        IStandardParticleEffect blobTeleporterBeam;
        IStandardParticleEffect blobFireTrail;
        IPositionalTriggerParticleEffect blobFireSparks;

        JumpPlatforms blobJumpPlatformEngine;
        public JumpPlatforms BlobJumpPlatformEngine
        {
            get { return blobJumpPlatformEngine; }
        }

        Guid blobRunSoundID = Guid.Empty;
        Guid blobTunnelLiftSoundID = Guid.Empty;

        public event RaiseScreenChanged OnScreenChanged;
        public delegate void RaiseBlobAction(enBlobAction Action, object Data);
        public event RaiseBlobAction OnBlobActionRaised;

        public Blob(GameEngine GameEngineReference) : base(GameEngineReference)
        {
            blobJumpPlatformEngine = new JumpPlatforms(gameEngine);
            blobsControl = new CustomControl(GameEngineReference.PlayerKeys, gameEngine, true);
            blobTouched = false;
            teleportFadeLevel = 255;
            startBlobTeleporting = false;
            blobCanMove = true;
            blobsState = enBlobState.Normal;
        }

        public void KillBlob(bool ResetBlobToScreenEntryPoint, bool OverloadFirst)
        {
            if (BlobsState == enBlobState.Normal)
            {
                resetBlobToScreenEntryPoint = ResetBlobToScreenEntryPoint;

                if (OverloadFirst)
                {
                    blobOverloadState = 0;
                }
                else
                {
                    blobOverloadState = 3;
                }

                BlobsState = enBlobState.Overloading;
                blobAlarmSoundCounter = 0;
            }
        }

        public void LoadContent(ContentList Content)
        {
            blobTeleporterBeam = gameEngine.ContentResourceManager.GetParticleEffectInstance("BeamMeUp_Small", enParticleEffectType.Triggered, null) as IStandardParticleEffect;
            blobFireTrail = gameEngine.ContentResourceManager.GetParticleEffectInstance("FireTrail", enParticleEffectType.Continual, null) as IStandardParticleEffect;
            //blobFireSparks = gameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks", enParticleEffectType.Triggered, null);
            blobFireSparks = gameEngine.ContentResourceManager.GetParticleEffectInstance("FireSparks", enParticleEffectType.TriggeredMultiLocationTimed, null) as IPositionalTriggerParticleEffect;

            if (blobTeleporterBeam !=null)
            {
                blobTeleporterBeam.ParticleEffectComplete += new RaiseEffectComplete(blobTeleporterBeam_ParticleEffectComplete);
            }
        }

        void blobTeleporterBeam_ParticleEffectComplete()
        {
            //blobCanMove = true;
            startBlobTeleporting = false;
        }

        public void Initialise()
        {
            // Fire location starts same as Blob's screen location
            blobFireX = blobXPos + 16;
            blobFireY = blobYPos + 16;
            blobFireFade = 0;

            blobShuttleAnim = new Animation();
            blobShuttleAnim.FirstFrame = 0;
            blobShuttleAnim.CurrentFrame = 0;
            blobShuttleAnim.LastFrame = 3;
            blobShuttleAnim.Yoyo = false;
            blobShuttleAnim.Delay = 2;
            blobShuttleAnim.Repeat = true;
            blobShuttleAnim.AnimateForwards = true;

            blobAnimLeft = new Animation();
            blobAnimLeft.FirstFrame = 0;
            blobAnimLeft.CurrentFrame = 0;
            blobAnimLeft.LastFrame = 3;
            blobAnimLeft.Yoyo = false;
            blobAnimLeft.Delay = 2;
            blobAnimLeft.Repeat = true;
            blobAnimLeft.AnimateForwards = false;

            blobAnimRight = new Animation();
            blobAnimRight.FirstFrame = 7;
            blobAnimRight.CurrentFrame = 7;
            blobAnimRight.LastFrame = 10;
            blobAnimRight.Yoyo = false;
            blobAnimRight.Delay = 2;
            blobAnimRight.Repeat = true;
            blobAnimRight.AnimateForwards = true;

            blobAnimFire1 = new Animation();
            blobAnimFire1.FirstFrame = 0;
            blobAnimFire1.CurrentFrame = 0;
            blobAnimFire1.LastFrame = 3;
            blobAnimFire1.Yoyo = false;
            blobAnimFire1.Delay = 2;
            blobAnimFire1.Repeat = true;
            blobAnimFire1.AnimateForwards = true;

            blobAnimFire2 = new Animation();
            blobAnimFire2.FirstFrame = 0;
            blobAnimFire2.CurrentFrame = 0;
            blobAnimFire2.LastFrame = 23;
            blobAnimFire2.Yoyo = false;
            blobAnimFire2.Delay = 1;
            blobAnimFire2.Repeat = true;
            blobAnimFire2.AnimateForwards = true;
            directionBlobFired = enDirection.Right;

            blobInventory = new enCollectables[blobMaxItemsHeld];
            
            // Clear inventory items
            for (int a = 0; a < blobInventory.Length; a++)
            {
                blobInventory[a] = enCollectables.None;
            }

            // Tempory test code to give Blob Accesscard and Key
            blobInventory[1] = enCollectables.Key;
            blobInventory[0] = enCollectables.AccessCard;

            tilesToReset = new List<ResetTile>();
        }

        // Blob has hit something
        public void RegisterFireHit()
        {
            blobHasFired = false;
            blobCanFire = true;
        }

        int teleportFadeLevel = 255;
        bool startBlobTeleporting = false;

        public void TriggerBlobTeleportedAnimation()
        {
            if (blobTeleporterBeam != null)
            {
                blobTeleporterBeam.Start();
            }
            startBlobTeleporting = true;
            teleportFadeLevel = 0;
        }

        public override void Update(GameTime gameTime)
        {
            bool changedScreens = false;

            if (blobCanMove && blobsState == enBlobState.Normal)
            {
                // Do something about being on something etc.
                if (blobOnShuttlePad)
                {
                    if (tileBelowBlob != null)
                    {
                        // Change the map to reflect shuttle has been taken
                        if (tileBelowBlob.TileNumber == (int)enActionTiles.ShuttlePlatform)
                        {
                            if (blobsControl.MovedUp())
                            {
                                gameEngine.GameScreens.ChangeShuttlePlatformState(true);
                                blobHeight = 96;
                                blobOnShuttle = true;
                            }
                        }
                        else if (tileBelowBlob.TileNumber == (int)enActionTiles.RemovedShuttlePlatform)
                        {
                            if (blobsControl.MovedLeft() || blobsControl.MovedRight())
                            {
                                gameEngine.GameScreens.ChangeShuttlePlatformState(false);
                                blobHeight = 64;
                                blobOnShuttle = false;
                            }
                        }

                        // Update the collision map for the current screen
                        gameEngine.GameScreens.UpdateCollisionMap(false);
                    }

                }

                if (blobsControl.MovedLeft())
                {
                    int CurrentFrame = blobAnimLeft.CurrentFrame;

                    if (blobsLastAnimDirection == enDirection.Right)
                    {
                        if (blobFrame > 6)
                        {
                            blobAnimLeft.CurrentFrame = 6;
                        }
                        else
                        {
                            blobAnimLeft.CurrentFrame = blobFrame;
                        }
                    }
                    else
                    {
                        blobAnimLeft.GetNextFrame();
                    }

                    int AmountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobXPos, blobYPos, blobWidth, blobHeight, blobHSpeed, true);
                    if (AmountToMove > 0)
                    {
                        blobXPos = blobXPos - AmountToMove;
                        if (blobOnShuttle)
                        {
                            blobShuttleAnim.GetNextFrame();
                        }
                    }

                    blobFrame = blobAnimLeft.CurrentFrame;
                    directionBlobMoved = enDirection.Left;
                    blobsLastAnimDirection = enDirection.Left;

                    if (!blobOnShuttle && blobFallHeight == 0)
                    {
                        blobRunSoundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobRun", true);
                    }
                }

                if (blobsControl.MovedRight())
                {
                    int CurrentFrame = blobAnimRight.CurrentFrame;

                    if (blobsLastAnimDirection == enDirection.Left)
                    {
                        if (blobFrame < 4)
                        {
                            blobAnimRight.CurrentFrame = 4;
                        }
                        else
                        {
                            blobAnimRight.CurrentFrame = blobFrame;
                        }
                    }
                    else
                    {
                        blobAnimRight.GetNextFrame();
                    }

                    int AmountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobXPos, blobYPos, blobWidth, blobHeight, blobHSpeed, true);
                    if (AmountToMove > 0)
                    {
                        blobXPos = blobXPos + AmountToMove;
                        if (blobOnShuttle)
                        {
                            blobShuttleAnim.GetNextFrame();
                        }
                    }

                    blobFrame = blobAnimRight.CurrentFrame;
                    directionBlobMoved = enDirection.Right;
                    blobsLastAnimDirection = enDirection.Right;

                    if (!blobOnShuttle && blobFallHeight == 0)
                    {
                        blobRunSoundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobRun", true);
                    }
                }


                if (blobOnShuttle)
                {
                    if (blobAnimRight.Repeat)
                    {
                        blobAnimRight.Repeat = false;
                    }

                    if (blobAnimLeft.Repeat)
                    {
                        blobAnimLeft.Repeat = false;
                    }

                    if (blobsControl.MovedUp())
                    {
                        blobYPos = blobYPos - gameEngine.GameScreens.CheckCollision(enDirection.Up, blobXPos, blobYPos, blobWidth, blobHeight, blobVSpeed, true);
                        directionBlobMoved = enDirection.Up;
                    }

                    if (blobsControl.MovedDown())
                    {
                        blobYPos = blobYPos + gameEngine.GameScreens.CheckCollision(enDirection.Down, blobXPos, blobYPos, blobWidth, blobHeight, blobVSpeed, true);
                        directionBlobMoved = enDirection.Down;
                    }
                }
                else
                {
                    // Create a standing platform
                    if (blobsControl.MovedDown() &&
                        !blobsControl.MovedLeft() &&
                        !blobsControl.MovedRight())
                    {
                        if (jumpPressed == false)
                        {
                            jumpPressed = true;

                            if (gameEngine.BlobStatusPanel.Platforms > 0)
                            {
                                if (blobOnShuttlePad == false)
                                {
                                    int temp = blobJumpPlatformEngine.GetNewJumpPlatform(blobXPos, blobYPos, blobWidth, blobHeight, true);

                                    if (temp == -1)
                                    {
                                    }
                                    // If -1 is returned from this routine, the collision check failed
                                    if (temp != -1000)
                                    {
                                        blobYPos = temp;

                                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobJumps", 0.3f, false);

                                        if (OnBlobActionRaised != null)
                                            OnBlobActionRaised(enBlobAction.LaidPlatform, null);
                                    }
                                }
                            }
                            else // just jump aimlessly in the air
                            {
                                // Check that we are on the ground first.... and above blob's head
                                if (gameEngine.GameScreens.CheckCollision(enDirection.Down, blobXPos, blobYPos, blobWidth, blobHeight, 1, true) == 0 &&
                                    gameEngine.GameScreens.CheckCollision(enDirection.Up, blobXPos, blobYPos - 32, blobWidth, blobHeight, 1, true) != 0
                                    )
                                {
                                    blobYPos = blobYPos - 32;
                                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobJumpsNoPlatforms", 1f, false);
                                }
                            }
                        }
                    }

                    bool accessingCheops = false;

                    if (gameEngine.GameScreens.CurrentGameScreen.CheopsPyramid != null)
                    {
                        if (CollisionDetection.Check(
                            new Rectangle(
                                blobXPos,
                                blobYPos,
                                64,
                                64),
                            new Rectangle(
                                gameEngine.GameScreens.CurrentGameScreen.CheopsPyramid.XLocation,
                                gameEngine.GameScreens.CurrentGameScreen.CheopsPyramid.YLocation,
                                64,
                                64)))
                        {
                            accessingCheops = true;
                        }
                    }

                    // Ensure Blob isn't going through a force field
                    bool inAccessForceField = false;
                    TileDetected forceFieldTile = gameEngine.GameScreens.CheckTileByObject(enDirection.None,blobXPos, blobYPos, BlobWidth, blobHeight, 0);

                    if (forceFieldTile != null && forceFieldTile.TileNumber == 419)
                    {
                        inAccessForceField = true;
                    }

                    // Pick up an object or drop one perhaps.....
                    if (blobsControl.MovedUp() &&
                         !blobsControl.MovedLeft() &&
                        !blobsControl.MovedRight())
                    {
                        if (inventoryAccessed == false &&
                            !inTunnelLift &&
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
                                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.3f, false); 

                                inventoryAccessed = true;

                                // Collect powerups
                                Item itemDropped = null;

                                // Pull out the last item and put it on the screen
                                if (blobInventory[blobInventory.Length - 1] != enCollectables.None)
                                {
                                    //gameEngine.GameScreens.C
                                    int itemXPos = (blobXPos / gameEngine.GameScreens.CollAreaWidth) * gameEngine.GameScreens.CollAreaWidth;
                                    int itemYPos = (blobYPos / gameEngine.GameScreens.CollAreaHeight) * gameEngine.GameScreens.CollAreaHeight;
                                    
                                    itemDropped = new Item(itemXPos, itemYPos, (int)blobInventory[blobInventory.Length - 1]);

                                    gameEngine.GameScreens.CurrentGameScreen.PickUps.Add(itemDropped);

                                    // Nothing held here now
                                    blobInventory[blobInventory.Length - 1] = enCollectables.None;
                                }

                                // Move everything along one in Blob's inventory
                                for (int b = blobInventory.Length - 1; b > 0; b--)
                                {
                                    blobInventory[b] = blobInventory[b - 1];
                                    blobInventory[b - 1] = enCollectables.None;
                                }

                                // Pick up an item if we can....
                                if (gameEngine.GameScreens.CurrentGameScreen.PickUps != null)
                                {
                                    for (int a = 0; a < gameEngine.GameScreens.CurrentGameScreen.PickUps.Count; a++)
                                    {
                                        if (CollisionDetection.Check(
                                            blobXPos,
                                            BlobYPos,
                                            blobWidth,
                                            blobHeight,
                                            gameEngine.GameScreens.CurrentGameScreen.PickUps[a].XLocation + 16,
                                            gameEngine.GameScreens.CurrentGameScreen.PickUps[a].YLocation + 32,
                                            32,
                                            32))
                                        {
                                            // Make sure it's not what we just dropped....
                                            bool pickupItem = true;

                                            if (itemDropped != null &&
                                                itemDropped == gameEngine.GameScreens.CurrentGameScreen.PickUps[a])
                                            {
                                                pickupItem = false;
                                            }

                                            if (pickupItem)
                                            {
                                                // Add the item to Blob's inventory
                                                blobInventory[0] = (enCollectables)gameEngine.GameScreens.CurrentGameScreen.PickUps[a].Type;

                                                // Remove it from the screen
                                                gameEngine.GameScreens.CurrentGameScreen.PickUps.Remove(gameEngine.GameScreens.CurrentGameScreen.PickUps[a]);
                                                break;
                                            }
                                        }
                                    }
                                }
                                gameEngine.GameScreens.UpdateCollisionMap(false);
                            }
                        }
                    }

                    if (!blobsControl.MovedUp())
                    {
                        inventoryAccessed = false;
                    }

                    if (!blobsControl.MovedDown())
                    {
                        jumpPressed = false;
                    }

                    if (!blobAnimRight.Repeat)
                    {
                        blobAnimRight.Repeat = true;
                    }

                    if (!blobAnimLeft.Repeat)
                    {
                        blobAnimLeft.Repeat = true;
                    }

                    // Crappy fake gravity routine :p
                    if (!inTunnelLift) // Can't fall if we are in a tunnel lift
                    {
                        int falling = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobXPos, blobYPos, blobWidth, blobHeight, blobFallSpeed, true);

                        if (falling > 0)
                        {
                            blobDistanceFallen = blobDistanceFallen + falling;
                            blobTotalDistanceFallen = blobTotalDistanceFallen + falling;

                            if (blobDistanceFallen > 12 && blobFallSpeed < 12)
                            {
                                blobFallSpeed++;
                                blobDistanceFallen = 0;
                            }
                        }
                        else
                        {
                            blobTotalDistanceFallen = 0;
                            blobDistanceFallen = 0;
                            blobFallSpeed = 4;
                            blobFallHeight = 0;
                        }

                        // Fall - arrrgh!!!
                        int fallDistance = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobXPos, blobYPos, blobWidth, blobHeight, blobFallSpeed, true);

                        if (fallDistance > 0 && blobTotalDistanceFallen > 64)
                        {
                            blobFallsSoundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobFalls", true);
                        }
                        
                        if (falling == 0)
                        {
                            gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(blobFallsSoundID, enSoundFade.Fast);
                        }

                        // Check to see if Blob can potentially enter a teleport booth, tunnel etc.
                        if (blobsControl.MovedRight())
                        {
                            for (int a = 0; a < fallDistance; a++)
                            {
                                if (gameEngine.GameScreens.CheckCollision(enDirection.Down, blobXPos + blobWidth, blobYPos + a, blobWidth + 1, blobHeight, 1, true) == 0)
                                {
                                    if (gameEngine.GameScreens.CheckCollision(enDirection.Right, blobXPos + blobWidth, blobYPos + a, blobWidth, blobHeight, 1, true) > 0)
                                    {
                                        fallDistance = a;

                                        if (fallDistance == 0)
                                            fallDistance = 1;

                                        break;
                                    }
                                }
                            }
                        }

                        if (blobsControl.MovedLeft())
                        {
                            for (int a = 0; a < fallDistance; a++)
                            {
                                if (gameEngine.GameScreens.CheckCollision(enDirection.Down, blobXPos - blobWidth, blobYPos + a, blobWidth + 1, blobHeight, 1, true) == 0)
                                {
                                    if (gameEngine.GameScreens.CheckCollision(enDirection.Right, blobXPos - blobWidth, blobYPos + a, blobWidth, blobHeight, 1, true) > 0)
                                    {
                                        fallDistance = a;

                                        if (fallDistance == 0)
                                            fallDistance = 1;

                                        break;
                                    }
                                }
                            }
                        }
                        blobYPos = blobYPos + fallDistance;
                        blobFallHeight = blobFallHeight + fallDistance;
                    }
                }

                //
                /// Fire Routine
                //

                // Fire not on a shuttle
                if (blobsControl.HasFired())
                {
                    if (gameEngine.BlobStatusPanel.FirePower > 0)
                    {
                        if (blobCanFire)
                        {
                            // Play sound
                            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobLazer");
                            blobFireDistance = 0;

                            blobFireX = blobXPos + 16;
                            blobFireY = blobYPos + 16;
                            blobFireFade = 0;

                            blobCanFire = false;
                            blobHasFired = true;
                            blobFireCollisionDirection = 0;

                            if (OnBlobActionRaised != null)
                            {
                                OnBlobActionRaised(enBlobAction.FiredLazer, null);
                            }

                            if (blobOnShuttle)
                            {
                                directionBlobFired = directionBlobMoved;
                            }
                            else
                            {
                                if (directionBlobMoved == enDirection.Left)
                                {
                                    directionBlobFired = enDirection.Left;
                                }
                                else if (directionBlobMoved == enDirection.Right)
                                {
                                    directionBlobFired = enDirection.Right;
                                }
                            }

                        }
                    }
                }

                // Fire routine
                if (blobHasFired)
                {
                    bool hitScreenBoundary = false;

                    if (blobFireTrail != null)
                    {
                        blobFireTrail.Start();
                    }

                    if (blobOnShuttleCached)
                    {
                        if (directionBlobFired == enDirection.Up)
                        {
                            int amountToMoveY = 0;
                            int amountToMoveX = 0;

                            if (blobFireCollisionDirection == 0)
                            {
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }

                            // have we hit game scenary
                            if (blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveY == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);
                                       
                                        if (blobFireSparks != null)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (blobFireY < 0)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (blobFireCollisionDirection == 0)
                                {
                                    if (amountToMoveY > 0)
                                    {
                                        blobFireY = blobFireY - amountToMoveY;
                                    }
                                    else
                                    {
                                        blobHasFired = false;
                                        blobCanFire = true;

                                        if (blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }
                                }
                            }
                            else if (blobFireCollisionDirection > 0)
                            {
                                if (blobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                if (blobFireCollisionDirection == 1)
                                {
                                    if (blobFireX < 0)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (blobFireCollisionDirection == 2)
                                {
                                    if (blobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    blobFireY = blobFireY + amountToMoveY;

                                    if (blobFireCollisionDirection == 1)
                                    {
                                        blobFireX = blobFireX - amountToMoveX;
                                        blobFireDistance = blobFireDistance + amountToMoveX;
                                    }
                                    else if (blobFireCollisionDirection == 2)
                                    {
                                        blobFireX = blobFireX + amountToMoveX;
                                        blobFireDistance = blobFireDistance + amountToMoveX;
                                    }
                                }
                                else
                                {
                                    blobHasFired = false;
                                    blobCanFire = true;

                                    if (blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        blobFireSparks.Start(blobFireX, blobFireY + 24);
                                    }
                                }
                            }
                        }
                        else if (directionBlobFired == enDirection.Down)
                        {
                            int amountToMoveY = 0;
                            int amountToMoveX = 0;

                            if (blobFireCollisionDirection == 0)
                            {
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }

                            // have we hit game scenary
                            if (blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveY == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

                                        if (blobFireSparks != null)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (blobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (blobFireCollisionDirection == 0)
                                {
                                    if (amountToMoveY > 0)
                                    {
                                        blobFireY = blobFireY + amountToMoveY;
                                    }
                                    else
                                    {
                                        blobHasFired = false;
                                        blobCanFire = true;

                                        if (blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }

                                }
                            }
                            else if (blobFireCollisionDirection > 0)
                            {
                                if (blobFireY < 0)
                                {
                                    amountToMoveY = 0;
                                    hitScreenBoundary = true;
                                }

                                if (blobFireCollisionDirection == 1)
                                {
                                    if (blobFireX < 0)
                                    {
                                        amountToMoveX = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (blobFireCollisionDirection == 2)
                                {
                                    if (blobFireY + 32 > gameEngine.GameScreens.ScreenWidth)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    blobFireY = blobFireY - amountToMoveY;

                                    if (blobFireCollisionDirection == 1)
                                    {
                                        blobFireX = blobFireX - amountToMoveX;
                                        blobFireDistance = blobFireDistance + amountToMoveX;
                                    }
                                    else if (blobFireCollisionDirection == 2)
                                    {
                                        blobFireX = blobFireX + amountToMoveX;
                                        blobFireDistance = blobFireDistance + amountToMoveX;
                                    }
                                }
                                else
                                {
                                    blobHasFired = false;
                                    blobCanFire = true;

                                    if (blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        blobFireSparks.Start(blobFireX, blobFireY + 24);
                                    }
                                }
                            }
                        }
                        else if (directionBlobFired == enDirection.Left)
                        {
                            int amountToMoveY = 0;
                            int amountToMoveX = 0;

                            if (blobFireCollisionDirection == 0)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }

                            int offset = 0;
                            if (CheckForBlobCollisionWithObject(new Rectangle(blobXPos, blobYPos, blobWidth, blobHeight), out offset))
                            {
                                if (blobFireDistance < offset + (blobWidth / 2))
                                {
                                    amountToMoveX = blobFireSpeed;
                                    amountToMoveY = blobFireSpeed;
                                }
                            }

                            // have we hit game scenary
                            if (blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveX == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

                                        if (blobFireSparks != null)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (blobFireX < 0)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (blobFireCollisionDirection == 0)
                                {
                                    if (amountToMoveX > 0)
                                    {
                                        blobFireX = blobFireX - amountToMoveX;
                                        blobFireDistance = blobFireDistance + amountToMoveX;
                                    }
                                    else
                                    {
                                        blobHasFired = false;
                                        blobCanFire = true;

                                        if (blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }

                                }
                            }
                            else if (blobFireCollisionDirection > 0)
                            {
                                if (blobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                if (blobFireCollisionDirection == 1)
                                {
                                    if (blobFireY < 0)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (blobFireCollisionDirection == 2)
                                {
                                    if (blobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    blobFireX = blobFireX + amountToMoveX;
                                    blobFireDistance = blobFireDistance + amountToMoveX;

                                    if (blobFireCollisionDirection == 1)
                                    {
                                        blobFireY = blobFireY - amountToMoveY;
                                    }
                                    else if (blobFireCollisionDirection == 2)
                                    {
                                        blobFireY = blobFireY + amountToMoveY;
                                    }
                                }
                                else
                                {
                                    blobHasFired = false;
                                    blobCanFire = true;

                                    if (blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        blobFireSparks.Start(blobFireX, blobFireY + 24);
                                    }
                                }
                            }
                        }
                        else if (directionBlobFired == enDirection.Right)
                        {
                           
                            int amountToMoveY = 0;
                            int amountToMoveX = 0;

                            if (blobFireCollisionDirection == 0)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 1)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Up, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }
                            else if (blobFireCollisionDirection == 2)
                            {
                                amountToMoveX = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                                amountToMoveY = gameEngine.GameScreens.CheckCollision(enDirection.Down, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);
                            }

                            int offset = 0;
                            if (CheckForBlobCollisionWithObject(new Rectangle(blobXPos, blobYPos, blobWidth, blobHeight), out offset))
                            {
                                if (blobFireDistance < offset + (blobWidth / 2))
                                {
                                    amountToMoveX = blobFireSpeed;
                                    amountToMoveY = blobFireSpeed;
                                }
                            }

                            //if (CheckForBlobFireCollisionWithObject(new Rectangle(blobFireX, blobFireY, 32, 32)))
                            //{
                            //    amountToMoveX = blobFireSpeed;
                            //    amountToMoveY = blobFireSpeed;
                            //}

                            // have we hit game scenary
                            if (blobFireCollisionDirection == 0)
                            {
                                if (amountToMoveX == 0)
                                {
                                    // Lets change fire direction, we can go diagonal 
                                    if (blobFireCollisionDirection == 0)
                                    {
                                        // Randomly select the new direction
                                        blobFireCollisionDirection = gameEngine.RandomNumberGenerator.Next(1, 3);

                                        if (blobFireSparks != null)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }
                                }

                                // Have we crossed the screen boundary
                                if (blobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
                                {
                                    amountToMoveX = 0;
                                    hitScreenBoundary = true;
                                }

                                // Have we hit a nastie
                                if (blobFireCollisionDirection == 0)
                                {
                                    if (amountToMoveX > 0)
                                    {
                                        blobFireX = blobFireX + amountToMoveX;
                                        blobFireDistance = blobFireDistance + amountToMoveX;
                                    }
                                    else
                                    {
                                        blobHasFired = false;
                                        blobCanFire = true;

                                        if (blobFireSparks != null && !hitScreenBoundary)
                                        {
                                            blobFireSparks.Start(blobFireX, blobFireY + 24);
                                        }
                                    }

                                }
                            }
                            else if (blobFireCollisionDirection > 0)
                            {
                                if (blobFireX < 0)
                                {
                                    amountToMoveX = 0;
                                }

                                if (blobFireCollisionDirection == 1)
                                {
                                    if (blobFireY < 0)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }
                                else if (blobFireCollisionDirection == 2)
                                {
                                    if (blobFireY + 32 > gameEngine.GameScreens.ScreenHeight)
                                    {
                                        amountToMoveY = 0;
                                        hitScreenBoundary = true;
                                    }
                                }

                                if (amountToMoveX > 0 && amountToMoveY > 0)
                                {
                                    blobFireX = blobFireX - amountToMoveX;
                                    blobFireDistance = blobFireDistance + amountToMoveX;

                                    if (blobFireCollisionDirection == 1)
                                    {
                                        blobFireY = blobFireY - amountToMoveY;
                                    }
                                    else if (blobFireCollisionDirection == 2)
                                    {
                                        blobFireY = blobFireY + amountToMoveY;
                                    }
                                }
                                else
                                {
                                    blobHasFired = false;
                                    blobCanFire = true;

                                    if (blobFireSparks != null && !hitScreenBoundary)
                                    {
                                        blobFireSparks.Start(blobFireX, blobFireY + 24);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (directionBlobFired == enDirection.Left)
                        {
                            int amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Left, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);

                            int offset = 0;
                            if (CheckForBlobCollisionWithObject(new Rectangle(blobXPos, blobYPos, blobWidth, blobHeight), out offset))
                            {
                                if (blobFireDistance < offset + (blobWidth / 2))
                                {
                                    amountToMove = blobFireSpeed;
                                }
                            }

                            // Have we crossed the screen boundary
                            if (blobFireX < 0)
                            {
                                amountToMove = 0;
                                hitScreenBoundary = true;
                            }

                            // Have we hit a nastie
                            if (amountToMove > 0)
                            {
                                blobFireX = blobFireX - amountToMove;
                                blobFireDistance = blobFireDistance + amountToMove;
                            }
                            else
                            {
                                blobHasFired = false;
                                blobCanFire = true;

                                if (blobFireSparks != null && !hitScreenBoundary)
                                {
                                    blobFireSparks.Start(blobFireX, blobFireY + 24);
                                }
                            }
                        }
                        else if (directionBlobFired == enDirection.Right)
                        {
                            int amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Right, blobFireX, blobFireY, 2, 32, blobFireSpeed, false);

                            int offset = 0;
                            if (CheckForBlobCollisionWithObject(new Rectangle(blobXPos, blobYPos, blobWidth, blobHeight), out offset))
                            {
                                if (blobFireDistance < offset + (blobWidth / 2))
                                {
                                    amountToMove = blobFireSpeed;
                                }
                            }

                            // Have we crossed the screen boundary
                            if (blobFireX + 32 > gameEngine.GameScreens.ScreenWidth)
                            {
                                amountToMove = 0;
                                hitScreenBoundary = true;
                            }

                            // Have we hit a nastie
                            if (amountToMove > 0)
                            {
                                blobFireX = blobFireX + amountToMove;
                                blobFireDistance = blobFireDistance + amountToMove;
                            }
                            else
                            {
                                blobHasFired = false;
                                blobCanFire = true;

                                if (blobFireSparks != null && !hitScreenBoundary)
                                {
                                    blobFireSparks.Start(blobFireX, blobFireY + 24);
                                }
                            }
                        }
                        else
                        {
                            // Blob has fired in an invalid direction, probably since he steped off a shuttle pad so kill the fire
                            blobHasFired = false;
                            blobCanFire = true;
                        }
                    }

                    blobAnimFire1.GetNextFrame();
                    blobAnimFire2.GetNextFrame();
                }

                // Stop the particle effect since the fire has stopped
                if (blobHasFired == false)
                {
                    if (blobFireTrail != null)
                    {
                        blobFireTrail.Stop();
                    }

                    blobOnShuttleCached = blobOnShuttle;
                }

                // Change screens...?
                if (blobXPos < 0)
                {
                    if (gameEngine.GameScreens.ScreenNumber - 1 >= 0)
                    {
                        gameEngine.GameScreens.ScreenNumber--;
                        blobXPos = gameEngine.GameScreens.ScreenWidth - gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).SpriteWidth;
                        changedScreens = true;
 
                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(enGameScreen.MainGame);
                        }
                    }
                }

                if (blobXPos + gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).SpriteWidth > gameEngine.GameScreens.ScreenWidth)
                {
                    if (gameEngine.GameScreens.ScreenNumber + 1 <= 512)
                    {
                        gameEngine.GameScreens.ScreenNumber++;
                        blobXPos = 0;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(enGameScreen.MainGame);
                        }
                    }
                }

                if (blobYPos < 0)
                {
                    if (gameEngine.GameScreens.ScreenNumber - 16 >= 0)
                    {
                        gameEngine.GameScreens.ScreenNumber -= 16;
                        changedScreens = true;

                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(enGameScreen.MainGame);
                        }

                        if (blobOnShuttle)
                        {
                            blobYPos = gameEngine.GameScreens.ScreenHeight - blobHeight;
                        }
                        else // Must be on a jumpplatform unless we're in a tunnel lift.....
                        {
                            if (!inTunnelLift)
                            {
                                
                                blobYPos = blobJumpPlatformEngine.GetNewJumpPlatform(blobXPos, gameEngine.GameScreens.ScreenHeight - gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).SpriteHeight, 0, 0, false);
                                
                            }
                            else
                            {
                                blobYPos = gameEngine.GameScreens.ScreenHeight - blobHeight;
                            }
                        }
                    }
                }

                if (blobYPos + blobHeight > gameEngine.GameScreens.ScreenHeight)
                {
                    if (gameEngine.GameScreens.ScreenNumber + 16 <= 512)
                    {
                        gameEngine.GameScreens.ScreenNumber += 16;
                        blobYPos = 0;
                        blobDistanceFallen = 0;
                        blobFallSpeed = 4;
 
                        changedScreens = true;
                        if (OnScreenChanged != null)
                        {
                            OnScreenChanged(enGameScreen.MainGame);
                        }
                    }
                }

                tileAboveBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Up, blobXPos, blobYPos, blobWidth, blobHeight, blobVSpeed);

                if (blobOnShuttle)
                {
                    tileBelowBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Down, blobXPos, blobYPos, blobWidth, blobHeight - 32, blobVSpeed); // Subtract the shuttle height off
                }
                else
                {
                    tileBelowBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Down, blobXPos, blobYPos, blobWidth, blobHeight, blobVSpeed);
                }

                tileLeftBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Left, blobXPos, blobYPos, blobWidth, blobHeight, blobHSpeed);
                tileRightBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.Right, blobXPos, blobYPos, blobWidth, blobHeight, blobHSpeed);

                // Detect Blob on a shuttle pad
                blobOnShuttlePad = false;
                if (tileBelowBlob != null)
                {
                    if ((enActionTiles)tileBelowBlob.TileNumber == enActionTiles.ShuttlePlatform ||
                        (enActionTiles)tileBelowBlob.TileNumber == enActionTiles.RemovedShuttlePlatform)
                    {
                        if (tileBelowBlob.XPosition == 32 && tileBelowBlob.YPosition == 0)
                        {
                            blobOnShuttlePad = true;
                        }
                    }
                }

                // Detect a teleporter
                if (tileRightBlob != null && blobsControl.MovedRight())
                {
                    if (tileRightBlob.TileNumber == 397)
                    {
                        if (tileRightBlob.XPosition >= 24 &&
                            tileRightBlob.YPosition == 32)
                        {
                            gameEngine.CurrentGameScreen = enGameScreen.Teleporter;

                            gameEngine.GetGameScreen(enGameScreen.Teleporter).Reset(null);
                            blobXPos = (tileRightBlob.MapXPosition * 128) + 20;
                            blobFrame = 7;
                        }
                    }
                }

                // Detect an access booth
                if (tileLeftBlob !=null && tileLeftBlob.TileNumber == 400)
                {
                    if (tileLeftBlob.XPosition <= 46 &&
                        tileLeftBlob.YPosition == 32)
                    {
                        gameEngine.GetGameScreen(enGameScreen.AccessBooth).Reset(new object[] { enDirection.Left, tileRightBlob.MapXPosition });
                        gameEngine.CurrentGameScreen = enGameScreen.AccessBooth;
                    }
                }

                if (tileRightBlob !=null && tileRightBlob.TileNumber == 399)
                {
                    if (tileRightBlob.XPosition >= 24 &&
                            tileRightBlob.YPosition == 32)
                    {
                        gameEngine.GetGameScreen(enGameScreen.AccessBooth).Reset(new object[] { enDirection.Right, tileLeftBlob.MapXPosition });
                        gameEngine.CurrentGameScreen = enGameScreen.AccessBooth;
                    }
                }

                // Detect a lefthand tunnel
                if (tileLeftBlob != null)
                {
                    if (tileLeftBlob.TileNumber == 420 ||
                        tileLeftBlob.TileNumber == 422 ||
                        tileLeftBlob.TileNumber == 424 ||
                        tileLeftBlob.TileNumber == 426 ||
                        tileLeftBlob.TileNumber == 428 ||
                        tileLeftBlob.TileNumber == 430 ||
                        tileLeftBlob.TileNumber == 432 ||
                        tileLeftBlob.TileNumber == 434 ||
                        tileLeftBlob.TileNumber == 436 ||
                        tileLeftBlob.TileNumber == 438
                        )
                    {
                        if (tileLeftBlob.XPosition <= 32 && tileLeftBlob.YPosition == 32)
                        {
                            gameEngine.GameScreens.ScreenNumber--;
                            directionGoingThroughTunnel = enDirection.Left;
                            changedScreens = true;

                            if (OnScreenChanged != null)
                            {
                                gameEngine.ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.LeftTunnelEntry, 80, -1, true);
                                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("TunnelSwish1", 0.8f, false);
                                OnScreenChanged(enGameScreen.None);
                            }
                        }
                    }
                }

                // Detect a righthand tunnel
                if (tileRightBlob != null)
                {
                    if (tileRightBlob.TileNumber == 421 ||
                        tileRightBlob.TileNumber == 423 ||
                        tileRightBlob.TileNumber == 425 ||
                        tileRightBlob.TileNumber == 427 ||
                        tileRightBlob.TileNumber == 429 ||
                        tileRightBlob.TileNumber == 431 ||
                        tileRightBlob.TileNumber == 433 ||
                        tileRightBlob.TileNumber == 435 ||
                        tileRightBlob.TileNumber == 437 ||
                        tileRightBlob.TileNumber == 439
                        )
                    {
                        if (tileRightBlob.XPosition >= 32 && tileRightBlob.YPosition == 32)
                        {
                            gameEngine.GameScreens.ScreenNumber++;
                            directionGoingThroughTunnel = enDirection.Right;
                            changedScreens = true;
   
                            if (OnScreenChanged != null)
                            {
                                gameEngine.ScreenEffctEngine.ExecuteScreenEffect(enScreenEffectType.RightTunnelEntry, 80, -1, true);
                                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("TunnelSwish2", 0.8f, false);
                                OnScreenChanged(enGameScreen.None);
                            }
                        }
                    }
                }

                // Breakable bridges
                if (tileBelowBlob != null)
                {
                    if (tileBelowBlob.TileNumber == 410)
                    {
                        if (blobFallHeight >= 128)
                        {
                            // Play the sound
                            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BridgeBreak");

                            // Add the tile to the list of tiles to reset on screen change
                            tilesToReset.Add(new ResetTile(tileBelowBlob, gameEngine.GameScreens.ScreenNumber));

                            // Remove the tile
                            gameEngine.GameScreens.CurrentGameScreen.ScreenTiles[tileBelowBlob.MapXPosition, tileBelowBlob.MapYPosition].TileImage = -1;

                            // Add the tile to the Breakable bridges engine
                            gameEngine.BreakableBridgesEngine.AddBridge(gameEngine.GameScreens.ConvertToScreenCoords(tileBelowBlob.MapXPosition, tileBelowBlob.MapYPosition));
                        }
                    }
                }

                // Tunnel lifts
                if (tileAboveBlob != null)
                {
                    if (tileAboveBlob.TileNumber == 409 ||
                        tileAboveBlob.TileNumber == 412 ||
                        tileAboveBlob.TileNumber == 408 ||
                        tileAboveBlob.TileNumber == 413)
                    {
                        if (tileAboveBlob.XPosition == 32)
                        {
                            if (inTunnelLift == false)
                            {
                                distanceInTunnelLift = 0;
                            }

                            inTunnelLift = true;
                        }
                    }
                    else if (tileAboveBlob.TileNumber == 407 ||
                        tileAboveBlob.TileNumber == 414 ||
                        tileAboveBlob.TileNumber == 415)
                    {
                        if (tileAboveBlob.YPosition == 96)
                        {
                            inTunnelLift = false;
                        }
                    }

                }


                if (inTunnelLift)
                {
                    bool MoveBlob = true;
                    if (tileAboveBlob != null)
                    {
                        if (tileAboveBlob.TileNumber == 409 ||
                             tileAboveBlob.TileNumber == 412 ||
                            tileAboveBlob.TileNumber == 413)
                        {
                            if (tileAboveBlob.YPosition == 96)
                            {
                                if (blobsControl.MovedLeft() && gameEngine.GameScreens.CheckCollision(enDirection.Left, blobXPos, blobYPos, blobWidth, blobHeight, blobHSpeed, true) > 0)
                                {
                                    if (distanceInTunnelLift > 8)
                                    {
                                        inTunnelLift = false;
                                        MoveBlob = false;
                                    }
                                }
                                else if (blobsControl.MovedRight() && gameEngine.GameScreens.CheckCollision(enDirection.Right, blobXPos, blobYPos, blobWidth, blobHeight, blobHSpeed, true) > 0)
                                {
                                    if (distanceInTunnelLift > 8)
                                    {
                                        inTunnelLift = false;
                                        MoveBlob = false;
                                    }
                                }
                            }
                        }
                    }

                    if (MoveBlob)
                    {
                        blobTunnelLiftSoundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("TunnelLift", true);
                        blobYPos = blobYPos - 8;// gameEngine.GameScreens.CheckCollision(enDirection.Up, blobXPos, blobYPos, blobWidth, blobHeight, 1);
                        distanceInTunnelLift = distanceInTunnelLift + 8;
                    }
                    else
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(blobTunnelLiftSoundID, enSoundFade.None);
                    }
                }

                // Spiky tiles
                TileDetected tileIntersectingBlob = gameEngine.GameScreens.CheckTileByObject(enDirection.None, blobXPos, blobYPos, blobWidth, blobHeight, 0);

                if (tileIntersectingBlob != null)
                {
                    // Central spiky tiles
                    if (tileIntersectingBlob.TileNumber == 405)
                    {
                        if (tileIntersectingBlob.XPosition > -16 &&
                            tileIntersectingBlob.XPosition < 64 &&
                            tileIntersectingBlob.YPosition > -8)
                        {
                            KillBlob(true, false);
                        }
                    }

                    // Right side mounted spikey tiles
                    if (tileIntersectingBlob.TileNumber == 404)
                    {
                        if (tileIntersectingBlob.YPosition < 64 &&
                            tileIntersectingBlob.YPosition > -32 &&
                            tileIntersectingBlob.XPosition > 16)
                        {
                            KillBlob(true, false);
                        }
                    }

                    // Broken bottle spiky tiles
                    if (tileIntersectingBlob.TileNumber == 416)
                    {
                        if (tileIntersectingBlob.XPosition > -16 &&
                            tileIntersectingBlob.XPosition < 64 &&
                            tileIntersectingBlob.YPosition > -16)
                        {
                            KillBlob(true, false);
                        }
                    }
                }

                // Collect powerups
                if (gameEngine.GameScreens.CurrentGameScreen.PowerUp != null)
                {
                    if (CollisionDetection.Check(
                        blobXPos,
                        BlobYPos,
                        blobWidth,
                        blobHeight,
                        gameEngine.GameScreens.CurrentGameScreen.PowerUp.XLocation,
                        gameEngine.GameScreens.CurrentGameScreen.PowerUp.YLocation,
                        32,
                        32))
                    {
                        if (OnBlobActionRaised != null)
                        {
                            OnBlobActionRaised(enBlobAction.GotPowerUp, gameEngine.GameScreens.CurrentGameScreen.PowerUp.Type);
                        }
                        gameEngine.GameScreens.CurrentGameScreen.PowerUp = null;
                        gameEngine.GameScreens.UpdateCollisionMap(false);
                    }
                }

                blobJumpPlatformEngine.Update(gameEngine.GameScreens);
            }
            else if (BlobsState == enBlobState.Overloading)
            {
                // Play the alarm sound for a bit
                if (blobAlarmSoundCounter == 1)
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobOverloadAlarm", 1.0f, false);
                }

                if (blobOverloadState < 3 && blobAlarmSoundCounter < 60)
                {
                    blobAlarmSoundCounter++;
                }
                else
                {
                    if (gameEngine.BlobKiller.Triggered == false)
                    {
                        blobHasFired = false;
                        blobCanFire = true;
                        blobFireTrail.Stop();

                        blobJumpPlatformEngine.ResetPlatforms();
                        gameEngine.ElectricityNodeGateEngine.Reset();

                        if (blobOverloadState == 0)
                        {
                            blobScale = blobScale + 0.30f;

                            if (blobScale > 1.6f)
                            {
                                blobOverloadState = 1;
                                blobScale = 1.6f;
                            }
                        }
                        else if (blobOverloadState == 1)
                        {
                            blobScale = blobScale - 0.30f;

                            if (blobScale < -1f)
                            {
                                blobScale = -1f;
                                blobOverloadState = 3;
                            }
                        }

                        if (blobOverloadState == 3)
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
                    blobOverloadState = 0;
                    blobScale = 1.0f;
                    // If Blob is killed by a spiky tile, we reset his position to when he entered
                    if (resetBlobToScreenEntryPoint)
                    {
                        blobYPos = blobScreenEntryLocation.Y;
                        blobXPos = blobScreenEntryLocation.X;
                        blobOnShuttle = blobOnShuttleOnEntry;
                        gameEngine.GameScreens.ChangeShuttlePlatformState(blobOnShuttle);
                        gameEngine.GameScreens.UpdateCollisionMap(false);
                        blobFrame = blobFrameOnEntry;
                        inTunnelLift = blobInTunnelLiftOnEntry;
                        blobFallSpeed = 4;

                        if (blobOnShuttle)
                            blobHeight = 96;
                        else
                            blobHeight = 64;
                    }

                    resetBlobToScreenEntryPoint = false;
                    BlobsState = enBlobState.Dead;
                    gameEngine.BlobKiller.Reset();
                }
            }
            
            // Update the teleport beam effect - probably should have a boolean to stop drawing when not needed.....
            blobFireSparks?.Update(gameTime);
            blobTeleporterBeam?.Update(blobXPos + 30, blobYPos + 50, gameTime);
            blobFireTrail?.Update(blobFireX + 14, blobFireY + 14, gameTime);

            if (changedScreens)
            {
                changedScreens = false;

                blobScreenEntryLocation = new Point(BlobXPos, BlobYPos);
                blobOnShuttleOnEntry = blobOnShuttle;
                blobFrameOnEntry = blobFrame;
            }
        }

        public void UnstickBlobFromTiles()
        {
            // Is blob stuck at the top?
            if (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos, blobWidth, 8, true) >= 0)
            {
                // And not stuck at the bottom?
                if (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos + (blobHeight - 8), blobWidth, 8, true) < 0)
                {
                    // He's stuck at the top so lets figure out how far we need to go to release him
                    int distanceToMoveY = 0;
                    while (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos + distanceToMoveY, blobWidth, 8, true) >= 0)
                    {
                        distanceToMoveY++;
                    }

                    // Shift Blob to a safe position
                    blobYPos = blobYPos + distanceToMoveY;
                }
            }
            
            if (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos + (blobHeight - 8), blobWidth, 8, true) >= 0) // Is Blob stuck at the bottom
            {
                // And not stuck at the top?
                if (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos, blobWidth, 8, true) < 0)
                {
                    // He's stuck at the bottom so lets figure out how far we need to go to release him
                    int distanceToMoveY = 0;
                    while (gameEngine.GameScreens.CheckRectangles(blobXPos, (blobYPos + (blobHeight - 8)) - distanceToMoveY, blobWidth, 8, true) >= 0)
                    {
                        distanceToMoveY++;
                    }

                    // Shift Blob to a safe position
                    blobYPos = blobYPos - distanceToMoveY;
                }
            }
            
            if (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos, 8, blobHeight, true) >= 0) // Is Blob stuck at the left
            {
                // And not stuck at the right?
                if (gameEngine.GameScreens.CheckRectangles(blobXPos + (blobWidth - 8), blobYPos, 8, blobHeight, true) < 0)
                {
                    // He's stuck at the top so lets figure out how far we need to go to release him
                    int distanceToMoveX = 0;
                    while (gameEngine.GameScreens.CheckRectangles(blobXPos + distanceToMoveX, blobYPos, 8, blobHeight, true) >= 0)
                    {
                        distanceToMoveX++;
                    }

                    // Shift Blob to a safe position
                    blobXPos = blobXPos + distanceToMoveX;
                }
            }
            
            if (gameEngine.GameScreens.CheckRectangles(blobXPos + (blobWidth - 8), blobYPos, 8, blobHeight, true) >= 0) // Is Blob stuck at the right
            {
                // And not stuck at the left?
                if (gameEngine.GameScreens.CheckRectangles(blobXPos, blobYPos, 8, blobHeight, true) < 0)
                {
                    // He's stuck at the top so lets figure out how far we need to go to release him
                    int distanceToMoveX = 0;
                    while (gameEngine.GameScreens.CheckRectangles((blobXPos + (blobWidth - 8)) - distanceToMoveX, blobYPos, 8, blobHeight, true) >= 0)
                    {
                        distanceToMoveX++;
                    }

                    // Shift Blob to a safe position
                    blobXPos = blobXPos - distanceToMoveX;
                }
            }
        }

        public void PerformPostChangeScreenActions()
        {
            // Reset blob's fire
            blobHasFired = false;
            blobCanFire = true;
            blobFireCollisionDirection = 0;

            blobFireX = blobXPos + 16;
            blobFireY = blobYPos + 16;
            blobFireFade = 0;

            blobJumpPlatformEngine.ResetPlatforms();

            // Unstick Blob from the screenmap if he's got stuck due to a tile map change
            UnstickBlobFromTiles();

            if (directionGoingThroughTunnel != enDirection.None)
            {
                if (directionGoingThroughTunnel == enDirection.Left)
                {
                    for (int a = 421; a < 441; a = a + 2)
                    {
                        List<Point> tiles = gameEngine.GameScreens.GetCoordsOfTiles(a);

                        if (tiles != null && tiles.Count == 1)
                        {
                            blobXPos = tiles[0].X + 24;
                            blobYPos = tiles[0].Y + 32;
                            break;
                        }
                    }
                }
                else if (directionGoingThroughTunnel == enDirection.Right) 
                {
                    for (int a = 420; a < 442; a = a + 2)
                    {
                        List<Point> tiles = gameEngine.GameScreens.GetCoordsOfTiles(a);

                        if (tiles != null && tiles.Count == 1)
                        {
                            blobXPos = tiles[0].X + 40;
                            blobYPos = tiles[0].Y + 32;
                            break;
                        }
                    }
                }

                directionGoingThroughTunnel = enDirection.None;
            }

            for (int a = 0; a < tilesToReset.Count; a++)
            {
                gameEngine.GameScreens.GetGameScreen(tilesToReset[a].ScreenNumber).
                    ScreenTiles[
                    tilesToReset[a].TileToReset.MapXPosition,
                    tilesToReset[a].TileToReset.MapYPosition].TileImage = tilesToReset[a].TileToReset.TileNumber;
            }

            tilesToReset.Clear();

            if (blobFireSparks != null)
            {
                blobFireSparks.Reset();
            }

            if (blobFireTrail != null)
            {
                blobFireTrail.Reset();
            }
        }

        public override void Draw(
            GameTime gameTime,
            int ScreenFade)
        {
            if (BlobsState == enBlobState.Normal)
            {
                // Draw the fire trail particles
                if (blobFireTrail != null)
                {
                    blobFireTrail.Draw();
                }

                // Draw blob
                if (blobTouchedByNasty)
                {
                    // Reset the counter
                    if (!blobTouched)
                    {
                        blobEnergyDrainCounter = 255;
                        blobTouched = true;
                    }

                    if (blobEnergyDrainCounter > 0)
                        blobEnergyDrainCounter = blobEnergyDrainCounter - 16;

                    if (blobEnergyDrainCounter < 0)
                        blobEnergyDrainCounter = 0;
                }
                else
                {
                    if (blobEnergyDrainCounter < 255)
                        blobEnergyDrainCounter = blobEnergyDrainCounter + 16;

                    if (blobEnergyDrainCounter > 255)
                    {
                        blobEnergyDrainCounter = 255;
                    }

                    blobTouched = false;
                }

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).DrawSprite(blobFrame, blobXPos, blobYPos, ColourHelper.GetXNA4Colour(255, (byte)blobEnergyDrainCounter, (byte)blobEnergyDrainCounter, (byte)((double)teleportFadeLevel * (double)((double)ScreenFade / 255))), 1f, 0f);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();

                // Draw other bits
                if (blobOnShuttle)
                {
                    if (gameEngine.UsingOldGraphics)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").DrawSprite(blobShuttleAnim.CurrentFrame, blobXPos, blobYPos + 64, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttleOriginal").End();
                    }
                    else
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, blobXPos, blobYPos + 64, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
                    }
                }
                blobJumpPlatformEngine.Draw(gameTime);

                if (blobHasFired)
                {
                    if (blobFireFade < 255)
                    {
                        blobFireFade = blobFireFade + 16;
                    }
                    if (blobFireFade > 255)
                    {
                        blobFireFade = 255;
                    }

                    if (gameEngine.UsingOldGraphics)
                    {
                        if (blobOnShuttle)
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").DrawSprite(blobAnimFire1.CurrentFrame, blobFireX, blobFireY, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)((double)255 * (double)((double)ScreenFade / 255))), 1.0f, 0f, true, gameEngine.Paused);
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2Original").End();
                        }
                        else
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").DrawSprite(blobAnimFire1.CurrentFrame, blobFireX, blobFireY - 16, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)((double)255 * (double)((double)ScreenFade / 255))), 1.0f, 0f, true, gameEngine.Paused);
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire1").End();
                        }
                    }
                    else
                    {
                        if (blobOnShuttle)
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").DrawSprite(blobAnimFire2.CurrentFrame, blobFireX, blobFireY, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)((double)blobFireFade * (double)((double)ScreenFade / 255))), 1.0f, 10f, true, gameEngine.Paused);
                            gameEngine.ContentResourceManager.GetSpriteCollection("BlobFire2").End();
                        }
                    }
               
                }

                // Draw teleport beam effect
                if (startBlobTeleporting)
                {
                    if (blobTeleporterBeam != null)
                    {
                        blobTeleporterBeam.Draw();
                    }
                }

                if (teleportFadeLevel < 255)
                {
                    teleportFadeLevel = teleportFadeLevel + 8;
                }

                if (teleportFadeLevel > 255)
                {
                    teleportFadeLevel = 255;
                }
            }
            else if (blobsState == enBlobState.Overloading)
            {
                if (blobAlarmSoundCounter < 60)
                {
                    // Make blob flicker
                    if (blobAlarmSoundCounter % 2 == 0)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).DrawSprite(blobFrame, blobXPos, blobYPos, ColourHelper.GetXNA4Colour(255, (byte)blobEnergyDrainCounter, (byte)blobEnergyDrainCounter, (byte)((double)teleportFadeLevel * (double)((double)ScreenFade / 255))), 1f, 0f);
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();
                    }

                    // Draw other bits
                    if (blobOnShuttle)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").DrawSprite(0, blobXPos, blobYPos + 64, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlobShuttle").End();
                    }
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

                    float temp = blobScale - 0.4f;

                    for (int a = 0; a < 5; a++)
                    {
                        if (temp >= 0f)
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).DrawSprite(blobFrame, blobXPos, blobYPos, ColourHelper.GetXNA4Colour(255, 255, 255, 150), temp, 0f);
                        }

                        temp = temp + 0.2f;
                    }

                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();
                }
            }
        }

        public void DrawUpper(int screenFade)
        {
            if (blobFireSparks != null)
            {
                blobFireSparks.Draw(screenFade);
            }
        }

        /// <summary>
        /// Gets collision objects on the current screen which aren't tiles i.e Cheops Pyramids, CoreElements 
        /// </summary>
        public List<Item> GetCollisionObjects()
        {
            List<Item> temp = new List<Item>();

            if (gameEngine.GameScreens.CurrentGameScreen.CheopsPyramid != null)
            {
                temp.Add(gameEngine.GameScreens.CurrentGameScreen.CheopsPyramid);
            }

            if (gameEngine.GameScreens.CurrentGameScreen.PickUps != null)
            {
                for (int a = 0; a < gameEngine.GameScreens.CurrentGameScreen.PickUps.Count; a++)
                {
                    temp.Add(gameEngine.GameScreens.CurrentGameScreen.PickUps[a]);
                }
            }

            if (temp.Count > 0)
            {
                return temp;
            }

            return null;
        }

        /// <summary>
        /// Routine to allow us to detect when Blob is near a Cheops pyrramid or core element. His fire power should not be blocked in these cases
        /// </summary>
        /// <param name="AmountToMove"></param>
        /// <param name="FireDirection"></param>
        /// <returns></returns>
        public bool CheckForBlobCollisionWithObject(Rectangle BlobsPosition, out int Offset)
        {
            Offset = 0;

            List<Item> screenItems = GetCollisionObjects();

            if (screenItems != null)
            {
                for (int a = 0; a < screenItems.Count; a++)
                {
                    if (CollisionDetection.Check(BlobsPosition, new Rectangle(screenItems[a].XLocation, screenItems[a].YLocation, 64, 64)))
                    {
                        Offset = BlobsPosition.X - screenItems[a].XLocation;

                        if (Offset < 0)
                        {
                            Offset = -Offset;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public void Reset()
        {
            // Reset blob's energy drain counter
            blobEnergyDrainCounter = 0;

            // Reset Blob's animation frame
            blobFrame = 5;

            // Reset Blob's state
            blobsState = enBlobState.Normal;

            // Reset Blob's position
            blobXPos = 416;
            blobYPos = 414;
            
            // Remove Blob from shuttle pads
            blobOnShuttle = false;
            blobHeight = 64;
            gameEngine.GameScreens.ChangeShuttlePlatformState(false);

            // Clear up any rogue collision tiles etc.
            PerformPostChangeScreenActions();
        }
    }
}
