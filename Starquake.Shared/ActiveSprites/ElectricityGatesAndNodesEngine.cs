using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.GameComponents;
using Microsoft.Xna.Framework;
using StarquakeXNA.Structure;

namespace StarquakeXNA
{
    public class ElectricityGatesAndNodesEngine : GameComponentBase
    {
        Animation electricityAnimation;
        Animation electricityAnimation2;
        Animation electricityAnimationOriginal;

        ElectricityRandomiser[] electricityNodeControllers;
        public ElectricityRandomiser[] ElectricityNodeControllers
        {
            get { return electricityNodeControllers; }
        }

        List<SecondLayerTile> secondLayerTiles;
        ConveyerNasties conveyerNasties;

        #region IGameComponent Members

        public event EventHandler OnBlobStruckByElectricity;

        public ElectricityGatesAndNodesEngine(GameEngine GameEngineReference,
            ConveyerNasties ConvyrNasties) : base(GameEngineReference)
        {
            conveyerNasties = ConvyrNasties;
        }

        public override void Initialise(object data)
        {
            electricityAnimationOriginal = new Animation();
            electricityAnimationOriginal.FirstFrame = 0;
            electricityAnimationOriginal.CurrentFrame = 0;
            electricityAnimationOriginal.LastFrame = 3;
            electricityAnimationOriginal.Yoyo = false;
            electricityAnimationOriginal.Delay = 1;
            electricityAnimationOriginal.Repeat = true;
            electricityAnimationOriginal.AnimateForwards = true;

            electricityAnimation = new Animation();
            electricityAnimation.Repeat = true;
            electricityAnimation.Yoyo = true;
            electricityAnimation.FirstFrame = 0;
            electricityAnimation.LastFrame = 9;
            electricityAnimation.Delay = 1;
            electricityAnimation.AnimateForwards = true;

            electricityAnimation2 = new Animation();
            electricityAnimation2.Repeat = true;
            electricityAnimation2.Yoyo = false;
            electricityAnimation2.FirstFrame = 0;
            electricityAnimation2.LastFrame = 9;
            electricityAnimation2.Delay = 2;
            electricityAnimation2.AnimateForwards = false;
        }

        // Turn off electricity on all nodes and gates
        public void Reset()
        {
            if (secondLayerTiles != null)
            {
                int electricityNodeCounter = 0;

                for (int a = 0; a < secondLayerTiles.Count; a++)
                {
                    if (secondLayerTiles[a].TileType == enTileType.ElectricityGate ||
                        secondLayerTiles[a].TileType == enTileType.ElectricityNode)
                    {
                        if (electricityNodeControllers != null)
                        {
                            if (electricityNodeControllers.Length > electricityNodeCounter)
                            {
                                electricityNodeControllers[electricityNodeCounter].Reset();
                                electricityNodeCounter++;
                            }
                        }
                    }
                }
            }
        }

        public void Update()
        {
            if (electricityNodeControllers != null)
            {
                int electricityNodeCounter = 0;
                secondLayerTiles = gameEngine.GameScreens.GetListOfSecondLayerTiles();

                if (secondLayerTiles != null)
                {
                    for (int a = 0; a < secondLayerTiles.Count; a++)
                    {

                        if (secondLayerTiles[a].TileType == enTileType.ElectricityNode)
                        {
                            if (gameEngine.Player.BlobXPos + gameEngine.Player.BlobWidth > electricityNodeControllers[electricityNodeCounter].Location.X + 40 &&
                                gameEngine.Player.BlobXPos < electricityNodeControllers[electricityNodeCounter].Location.X + 88 &&
                                gameEngine.Player.BlobYPos + 64 > electricityNodeControllers[electricityNodeCounter].Location.Y &&
                                gameEngine.Player.BlobYPos < electricityNodeControllers[electricityNodeCounter].Location.Y + 32)
                            {
                                if (electricityNodeControllers[electricityNodeCounter].ElectricityOn)
                                {
                                    if (OnBlobStruckByElectricity != null)
                                    {
                                        OnBlobStruckByElectricity(this, EventArgs.Empty);
                                    }
                                }
                            }
                        }
                        else if (secondLayerTiles[a].TileType == enTileType.ElectricityGate)
                        {
                            if (gameEngine.Player.BlobXPos + gameEngine.Player.BlobWidth > electricityNodeControllers[electricityNodeCounter].Location.X + 32 &&
                                gameEngine.Player.BlobXPos < electricityNodeControllers[electricityNodeCounter].Location.X + 96 &&
                                gameEngine.Player.BlobYPos + 64 > electricityNodeControllers[electricityNodeCounter].Location.Y + 40 &&
                                gameEngine.Player.BlobYPos < electricityNodeControllers[electricityNodeCounter].Location.Y + 56)
                            {
                                if (electricityNodeControllers[electricityNodeCounter].ElectricityOn)
                                {
                                    if (OnBlobStruckByElectricity != null)
                                    {
                                        OnBlobStruckByElectricity(this, EventArgs.Empty);
                                    }
                                }
                            }
                        }

                        // Update the nodes
                        electricityNodeControllers[electricityNodeCounter].Update();

                        electricityNodeCounter++;
                    }
                }

                if (!gameEngine.UsingOldGraphics)
                {
                    electricityAnimation.GetNextFrame();
                    electricityAnimation2.GetNextFrame();
                }
                else
                {
                    electricityAnimationOriginal.GetNextFrame();
                }
            }
            else if (conveyerNasties !=null)
            {
                if (!gameEngine.UsingOldGraphics)
                {
                    electricityAnimation.GetNextFrame();
                    electricityAnimation2.GetNextFrame();
                }
            }
        }

        public void PerformPostChangeScreenActions()
        {
            // Reset Electricity Node Controller list
            electricityNodeControllers = null;
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime, int ScreenFade)
        {
            if (secondLayerTiles !=null && secondLayerTiles.Count > 0)
            {
                int electricityNodeCounter = 0;

                for (int a = 0; a < secondLayerTiles.Count; a++)
                {
                    if (secondLayerTiles[a].TileType == enTileType.ElectricityNode)
                    {
                        if (electricityNodeControllers != null)
                        {
                            if (electricityNodeControllers.Length > electricityNodeCounter)
                            {
                                if (electricityNodeControllers[electricityNodeCounter].ElectricityOn)
                                {
                                    if (!gameEngine.UsingOldGraphics)
                                    {
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(electricityAnimation.CurrentFrame, secondLayerTiles[a].TileLocation.X + 28, secondLayerTiles[a].TileLocation.Y - 18, Color.White);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").End();
                                    }
                                    else
                                    {
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").DrawSprite(electricityAnimationOriginal.CurrentFrame, secondLayerTiles[a].TileLocation.X + 32, secondLayerTiles[a].TileLocation.Y - 16, Color.White);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").End();
                                    }
                                }
                            }
                        }

                        electricityNodeCounter++;

                    }
                    else if (secondLayerTiles[a].TileType == enTileType.ElectricityGate)
                    {
                        if (electricityNodeControllers != null)
                        {
                            if (electricityNodeControllers.Length > electricityNodeCounter)
                            {
                                if (electricityNodeControllers[electricityNodeCounter].ElectricityOn)
                                {
                                    if (!gameEngine.UsingOldGraphics)
                                    {
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(electricityAnimation.CurrentFrame, secondLayerTiles[a].TileLocation.X + 28, secondLayerTiles[a].TileLocation.Y + 14, Color.White);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").End();
                                    }
                                    else
                                    {
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").DrawSprite(electricityAnimationOriginal.CurrentFrame, secondLayerTiles[a].TileLocation.X + 32, secondLayerTiles[a].TileLocation.Y + 16, Color.White);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").End();
                                    }
                                }
                            }
                        }

                        electricityNodeCounter++;
                    }
                }
            }
        }

        public void DrawUpperLayer(Microsoft.Xna.Framework.GameTime gameTime, int ScreenFade)
        {
            // Draw electricity nodes etc bottom layer
            List<SecondLayerTile> secondLayerTiles = gameEngine.GameScreens.GetListOfSecondLayerTiles();

            if (secondLayerTiles !=null && secondLayerTiles.Count > 0)
            {
                int electricityNodeCounter = 0;
                for (int a = 0; a < secondLayerTiles.Count; a++)
                {
                    if (secondLayerTiles[a].TileType == enTileType.ElectricityNode)
                    {
                        if (electricityNodeControllers != null)
                        {
                            if (electricityNodeControllers.Length > electricityNodeCounter)
                            {
                                if (electricityNodeControllers[electricityNodeCounter].ElectricityOn)
                                {
                                    if (!gameEngine.UsingOldGraphics)
                                    {
                                        // Left node
                                        gameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X - 12, secondLayerTiles[a].TileLocation.Y - 14, Color.Blue);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X - 12, secondLayerTiles[a].TileLocation.Y - 14, Color.Cyan);

                                        // Right node
                                        gameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 75, secondLayerTiles[a].TileLocation.Y - 14, Color.Blue);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 75, secondLayerTiles[a].TileLocation.Y - 14, Color.Cyan);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").End();

                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").DrawSprite(electricityAnimation2.CurrentFrame, secondLayerTiles[a].TileLocation.X + 28, secondLayerTiles[a].TileLocation.Y - 14, Color.White);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").End();
                                    }
                                }
                            }
                        }

                        electricityNodeCounter++;

                    }
                    else if (secondLayerTiles[a].TileType == enTileType.ElectricityGate)
                    {
                        if (electricityNodeControllers != null)
                        {
                            if (electricityNodeControllers.Length > electricityNodeCounter)
                            {
                                if (electricityNodeControllers[electricityNodeCounter].ElectricityOn)
                                {
                                    if (!gameEngine.UsingOldGraphics)
                                    {
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").DrawSprite(electricityAnimation2.CurrentFrame, secondLayerTiles[a].TileLocation.X + 28, secondLayerTiles[a].TileLocation.Y + 14, Color.White);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").End();
                                        
                                        // Left node
                                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 8, secondLayerTiles[a].TileLocation.Y + 27, Color.Blue);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 8, secondLayerTiles[a].TileLocation.Y + 27, Color.Cyan);

                                        // Right node
                                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 78, secondLayerTiles[a].TileLocation.Y + 27, Color.Blue);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 78, secondLayerTiles[a].TileLocation.Y + 27, Color.Cyan);
                                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").End();
                                    }
                                }
                            }
                        }

                        electricityNodeCounter++;
                    }
                }

                if (electricityNodeControllers == null)
                {
                    electricityNodeControllers = new ElectricityRandomiser[electricityNodeCounter];
                    for (int a = 0; a < electricityNodeCounter; a++)
                    {
                        electricityNodeControllers[a] = new ElectricityRandomiser(gameEngine);
                        electricityNodeControllers[a].Location = new Point(secondLayerTiles[a].TileLocation.X, secondLayerTiles[a].TileLocation.Y);
                    }
                }

            }
            if (conveyerNasties != null)
            {
                for (int a = 0; a < conveyerNasties.Nasties.Count; a++)
                {
                    if (conveyerNasties.Nasties[a].NastyActive)
                    {
                        // Left node
                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, (conveyerNasties.Nasties[a].XLocation - 6), conveyerNasties.Nasties[a].YLocation + 27, Color.Blue);
                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, (conveyerNasties.Nasties[a].XLocation - 6), conveyerNasties.Nasties[a].YLocation + 27, Color.Cyan);

                        // Right node
                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, conveyerNasties.Nasties[a].XLocation + 33, conveyerNasties.Nasties[a].YLocation + 27, Color.Blue);
                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, conveyerNasties.Nasties[a].XLocation + 33, conveyerNasties.Nasties[a].YLocation + 27, Color.Cyan);
                        gameEngine.ContentResourceManager.GetSpriteCollection("GateLights").End();

                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(electricityAnimation.CurrentFrame, conveyerNasties.Nasties[a].XLocation, conveyerNasties.Nasties[a].YLocation + 10, Color.White, 0.5f, 0);
                        gameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").End();
                    }
                }
            }
        }

        #endregion
    }

    public class ElectricityRandomiser
    {
        private int counter;
        private int counterTarget;
        private bool electricityOn;

        public bool ElectricityOn
        {
            get { return electricityOn; }
        }

        private Random randomGenerator;
        private bool electricitySoundPlayed;
        private GameEngine gameEngine;
        private Guid soundID;

        private Point location;
        public Point Location
        {
            get { return location; }
            set { location = value; }
        }

        public ElectricityRandomiser(GameEngine GameEngineReference)
        {
            gameEngine = GameEngineReference;
            electricitySoundPlayed = false;
            randomGenerator = GameEngineReference.RandomNumberGenerator;

            if (randomGenerator.Next(0, 2) == 0)
            {
                electricityOn = false;
            }
            else
            {
                electricityOn = true;
            }
            counter = 0;
            ResetCounter(false, true);
        }

        private void ResetCounter(bool NodeReset, bool ChangedScreens)
        {
            counter = 0;
            if (NodeReset)
            {
                // Longer interval if we have reset, probably becuase Blob has died so give the player chance to respond
                counterTarget = randomGenerator.Next(240, 300); // Random number between four seconds and a five seconds
            }
            else if (ChangedScreens)
            {
                // Fix the electricity time for new screen entry
                counterTarget = 60;
            }
            else
            {
                counterTarget = randomGenerator.Next(60, 300); // Random number between one second and five seconds
            }
        }

        public void Reset()
        {
            ResetCounter(true, false);
            electricityOn = false;
        }

        public void Update()
        {
            counter++;

            if (counter > counterTarget)
            {
                // flip the electricity
                electricityOn = !electricityOn;
                ResetCounter(false, false);
            }

            if (electricityOn)
            {
                if (electricitySoundPlayed == false)
                {
                    // Play sound
                    soundID = gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("ElectricityNodes", 0.3F);

                    electricitySoundPlayed = true;
                }
            }
            else
            {
                // Stop sound
                if (soundID != Guid.Empty)
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine().StopSound(soundID, SoundFade.None);
                }

                electricitySoundPlayed = false;
            }
        }
        
    }
}
