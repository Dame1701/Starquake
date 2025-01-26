using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarquakeXNA
{
    public enum enColourAnimationDirection
    {
        FadeUp = 1,
        FadeDown = 2,
    }
    
    public class ColourFaderAnimation
    {
        // Name
        public string Name;

        // The tile to affect
        public int TileNo;

        // The tile index of the upper layer to draw
        public int UpperTileToDraw;

        // Colour we're overlaying
        public Color TransitionColour;
        
        // We use addictive blending to build up a more severe shade oif destination colour
        public int Intensity;
        
        // How fast to do the colour fase
        public int Speed;

        // Is the animation running?
        public bool Running;

        // Is the animation stopping
        public bool Stopping;

        // Loop counter
        public int LoopCounter;

        // Number of times to loop before stopping
        public int LoopCount;

        // Animation direction
        public enColourAnimationDirection Direction;
    }
    
    public class ColourAnimationEngine : GameComponentBase
    {
        private List<ColourFaderAnimation> colourAnimationsRegistered;

        public ColourAnimationEngine(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            colourAnimationsRegistered = new List<ColourFaderAnimation>();
        }

        public void RegisterTile(ColourFaderAnimation FaderAnimation)
        {
            colourAnimationsRegistered.Add(FaderAnimation);
        }

        public void Start(int TileNo, int LoopCount)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].TileNo == TileNo)
                {
                    if (colourAnimationsRegistered[a].Running == false)
                    {
                        // Make colour initially invisible
                        colourAnimationsRegistered[a].TransitionColour.A = 0;
                        colourAnimationsRegistered[a].Running = true;
                        colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                        colourAnimationsRegistered[a].Stopping = false;
                        colourAnimationsRegistered[a].LoopCount = LoopCount;

                        // Reset the loop counter
                        colourAnimationsRegistered[a].LoopCounter = 0;
                    }
                }
            }
        }

        public void Start(int TileNo, int LoopCount, Color TransitionColour)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].TileNo == TileNo)
                {
                    if (colourAnimationsRegistered[a].Running == false)
                    {
                        // Make colour initially invisible
                        colourAnimationsRegistered[a].TransitionColour = TransitionColour;
                        colourAnimationsRegistered[a].TransitionColour.A = 0;
                        colourAnimationsRegistered[a].Running = true;
                        colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                        colourAnimationsRegistered[a].Stopping = false;
                        colourAnimationsRegistered[a].LoopCount = LoopCount;

                        // Reset the loop counter
                        colourAnimationsRegistered[a].LoopCounter = 0;
                    }
                }
            }
        }

        public void Start(int TileNo, int LoopCount, Color TransitionColour, int Intensity)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].TileNo == TileNo)
                {
                    if (colourAnimationsRegistered[a].Running == false)
                    {
                        // Make colour initially invisible
                        colourAnimationsRegistered[a].TransitionColour = TransitionColour;
                        colourAnimationsRegistered[a].TransitionColour.A = 0;
                        colourAnimationsRegistered[a].Intensity = Intensity;
                        colourAnimationsRegistered[a].Running = true;
                        colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                        colourAnimationsRegistered[a].Stopping = false;
                        colourAnimationsRegistered[a].LoopCount = LoopCount;

                        // Reset the loop counter
                        colourAnimationsRegistered[a].LoopCounter = 0;
                    }
                }
            }
        }

        public void Start(string Name, int LoopCount)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].Name == Name)
                {
                    if (colourAnimationsRegistered[a].Running == false)
                    {
                        // Make colour initially invisible
                        colourAnimationsRegistered[a].TransitionColour.A = 0;
                        colourAnimationsRegistered[a].Running = true;
                        colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                        colourAnimationsRegistered[a].Stopping = false;
                        colourAnimationsRegistered[a].LoopCount = LoopCount;

                        // Reset the loop counter
                        colourAnimationsRegistered[a].LoopCounter = 0;
                    }
                }
            }
        }

        public void Start(string Name, int LoopCount, Color TransitionColour)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].Name == Name)
                {
                    if (colourAnimationsRegistered[a].Running == false)
                    {
                        // Make colour initially invisible
                        colourAnimationsRegistered[a].TransitionColour = TransitionColour;
                        colourAnimationsRegistered[a].TransitionColour.A = 0;
                        colourAnimationsRegistered[a].Running = true;
                        colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                        colourAnimationsRegistered[a].Stopping = false;
                        colourAnimationsRegistered[a].LoopCount = LoopCount;

                        // Reset the loop counter
                        colourAnimationsRegistered[a].LoopCounter = 0;
                    }
                }
            }
        }

        public void Start(string Name, int LoopCount, Color TransitionColour, int Intensity)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].Name == Name)
                {
                    if (colourAnimationsRegistered[a].Running == false)
                    {
                        // Make colour initially invisible
                        colourAnimationsRegistered[a].TransitionColour = TransitionColour;
                        colourAnimationsRegistered[a].TransitionColour.A = 0;
                        colourAnimationsRegistered[a].Intensity = Intensity;
                        colourAnimationsRegistered[a].Running = true;
                        colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                        colourAnimationsRegistered[a].Stopping = false;
                        colourAnimationsRegistered[a].LoopCount = LoopCount;

                        // Reset the loop counter
                        colourAnimationsRegistered[a].LoopCounter = 0;
                    }
                }
            }
        }

        public void Stop(int TileNo)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].TileNo == TileNo)
                {
                    if (colourAnimationsRegistered[a].Running == true &&
                        colourAnimationsRegistered[a].Stopping == false)
                    {
                        // Schedule a stop
                        colourAnimationsRegistered[a].Stopping = true;
                    }
                }
            }
        }

        public void Stop(string Name)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].Name == Name)
                {
                    if (colourAnimationsRegistered[a].Running == true &&
                        colourAnimationsRegistered[a].Stopping == false)
                    {
                        // Schedule a stop
                        colourAnimationsRegistered[a].Stopping = true;
                    }
                }
            }
        }

        public void ChangeSpeed(int TileNo, int Speed)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].TileNo == TileNo)
                {
                    colourAnimationsRegistered[a].Speed = Speed;
                }
            }
        }

        public void ChangeSpeed(string Name, int Speed)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].Name == Name)
                {
                    colourAnimationsRegistered[a].Speed = Speed;
                }
            }
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            for (int a = 0; a < colourAnimationsRegistered.Count; a++)
            {
                if (colourAnimationsRegistered[a].Running)
                {
                    if (colourAnimationsRegistered[a].Direction == enColourAnimationDirection.FadeUp)
                    {
                        if (colourAnimationsRegistered[a].TransitionColour.A < 255)
                        {
                            int value = colourAnimationsRegistered[a].TransitionColour.A;
                            value = value + colourAnimationsRegistered[a].Speed;

                            if (value > 255)
                            {
                                value = 255;
                            }

                            colourAnimationsRegistered[a].TransitionColour.A = (byte)value;
                        }
                        else
                        {
                            colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeDown;
                        }
                    }
                    else if (colourAnimationsRegistered[a].Direction == enColourAnimationDirection.FadeDown)
                    {
                        if (colourAnimationsRegistered[a].TransitionColour.A > 0)
                        {
                            int value = colourAnimationsRegistered[a].TransitionColour.A;
                            value = value - colourAnimationsRegistered[a].Speed;

                            if (value < 0)
                            {
                                value = 0;
                            }

                            colourAnimationsRegistered[a].TransitionColour.A = (byte)value;
                        }
                        else
                        {
                            if (colourAnimationsRegistered[a].Stopping == false && 
                                colourAnimationsRegistered[a].LoopCounter < colourAnimationsRegistered[a].LoopCount - 1)
                            {
                                if (colourAnimationsRegistered[a].LoopCount >= 0)
                                {
                                    colourAnimationsRegistered[a].LoopCounter++;
                                }

                                colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                            }
                            else
                            {
                                if (colourAnimationsRegistered[a].Stopping ||
                                    colourAnimationsRegistered[a].LoopCount >=0)
                                {
                                    colourAnimationsRegistered[a].Running = false;
                                    colourAnimationsRegistered[a].LoopCounter = 0;
                                }
                                else
                                {
                                    colourAnimationsRegistered[a].Direction = enColourAnimationDirection.FadeUp;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Draw(GameTime gameTime, byte ScreenFade)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

            for (int y = 0; y < gameEngine.GameScreens.TilesDown; y++)
            {
                for (int x = 0; x < gameEngine.GameScreens.TilesAcross; x++)
                {
                    for (int a = 0; a < colourAnimationsRegistered.Count; a++)
                    {
                        if (colourAnimationsRegistered[a].Running)
                        {
                            if (gameEngine.GameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimationsRegistered[a].TileNo) // Teleport booths
                            {
                                // Layer up the tiles to make the intensity deeper
                                for (int b = 0; b < colourAnimationsRegistered[a].Intensity; b++)
                                {
                                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).DrawSprite(colourAnimationsRegistered[a].TileNo, (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                                                (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), colourAnimationsRegistered[a].TransitionColour);
                                }
                            }
                        }
                    }
                }
            }
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).End();
        }

        public void Draw(SpriteCollection Sprites, GameTime gameTime, byte ScreenFade)
        {
            Sprites.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

            for (int y = 0; y < gameEngine.GameScreens.TilesDown; y++)
            {
                for (int x = 0; x < gameEngine.GameScreens.TilesAcross; x++)
                {
                    for (int a = 0; a < colourAnimationsRegistered.Count; a++)
                    {
                        if (colourAnimationsRegistered[a].Running &&
                              colourAnimationsRegistered[a].TileNo >= 0)
                        {
                            if (gameEngine.GameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimationsRegistered[a].TileNo) // Teleport booths
                            {
                                // Layer up the tiles to make the intensity deeper
                                for (int b = 0; b < colourAnimationsRegistered[a].Intensity; b++)
                                {
                                    Sprites.DrawSprite(colourAnimationsRegistered[a].TileNo, (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                                                (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), colourAnimationsRegistered[a].TransitionColour);
                                }
                            }
                        }
                    }
                }
            }
            Sprites.End();
        }

        public void DrawUpperLayer(GameTime gameTime, byte ScreenFade)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

            for (int y = 0; y < gameEngine.GameScreens.TilesDown; y++)
            {
                for (int x = 0; x < gameEngine.GameScreens.TilesAcross; x++)
                {
                    for (int a = 0; a < colourAnimationsRegistered.Count; a++)
                    {
                        if (colourAnimationsRegistered[a].Running &&
                            colourAnimationsRegistered[a].UpperTileToDraw >=0)
                        {
                            if (gameEngine.GameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimationsRegistered[a].TileNo) // Teleport booths
                            {
                                // Layer up the tiles to make the intensity deeper
                                for (int b = 0; b < colourAnimationsRegistered[a].Intensity; b++)
                                {
                                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).DrawSprite(colourAnimationsRegistered[a].UpperTileToDraw, (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                                                (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).SpriteHeight), colourAnimationsRegistered[a].TransitionColour);
                                }
                            }
                        }
                    }
                }
            }
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).End();
        }
    }
}
