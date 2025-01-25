using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StarquakeXNA.ContentManagement;
using SharedLibs;
using StarquakeXNA.Particles;
using StarquakeXNA.Helpers;

namespace StarquakeXNA.GameComponents
{
    public class PlanetCoreScreen : GameComponentBase, IRealTimeGameScreen
    {
        GameScreenCollection gameScreens;
        public GameScreenCollection GameScreens
        {
            get { return gameScreens; }
            set { gameScreens = value; }
        }

        //private MercuryParticleEngine.MercuryParticleEffect coreVortex;
        private IStandardParticleEffect coreSparks;
        private IStandardParticleEffect coreComputationEffect;

        private List<Nasty> coreAliens;
        private Random randomNumberGenerator;
        int coreAlienCount = 4;

        Queue<int> elementsToReplace;

        private CoreElements elements;

        bool justEnteredCore = true;
        int entryTime;

        bool replacingElements = false;
        int blankingTilesOpacity;

        public bool Success
        {
            get { return true;}
        }


        public PlanetCoreScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {

        }

        public override void LoadContent()
        {
            elements.LoadContent();
            coreAliens = new List<Nasty>();
            ContentList nastyGraphics = new ContentList(gameEngine.ContentMgr);
            nastyGraphics.AddContent("nastySprite", "Graphics\\nastiesNew");
            randomNumberGenerator = new Random((int)System.DateTime.Now.Ticks);

            for (int a = 0; a < coreAlienCount; a++)
            {
                Nasty coreAlien = new Nasty(
                    gameEngine,
                    0,
                    0,
                    9);

                coreAlien.LoadContent(nastyGraphics);
                coreAlien.NastyType = enNastyType.CoreGuardian;
                coreAlien.Initialise(null);
                coreAliens.Add(coreAlien);
            }
            coreComputationEffect = gameEngine.ContentResourceManager.GetParticleEffectInstance("BinaryEffect", enParticleEffectType.Continual, null) as IStandardParticleEffect;
            coreSparks = gameEngine.ContentResourceManager.GetParticleEffectInstance("CoreSparks", enParticleEffectType.Continual, null) as IStandardParticleEffect;
        }

        public override void Initialise(object Data)
        {
            if (Data != null)
            {
                elements = Data as CoreElements;
                elements.OnCoreElementReplaced += new RaiseEffectComplete(elements_OnCoreElementReplaced);
            }
            else
            {
                gameEngine.GameScreens.ScreenNumber = 199;

                // Add the collision map for the core elements
                gameEngine.GameScreens.CurrentGameScreen.CollisionMap.AddRange(elements.GetCollisionMap());

                List<Point> potentialLocations = gameEngine.GameScreens.GetNastyPotentialLocations();
                if (potentialLocations.Count < coreAlienCount)
                {
                    coreAlienCount = potentialLocations.Count;
                }

                for (int a = 0; a < coreAlienCount; a++)
                {
                    coreAliens[a].XPos = potentialLocations[a].X;
                    coreAliens[a].YPos = potentialLocations[a].Y;
                    coreAliens[a].NastyType = enNastyType.CoreGuardian;
                    coreAliens[a].NastyState = enNastyState.Normal;
                }
            }
        }

        void elements_OnCoreElementReplaced()
        {
            if (elementsToReplace != null &&
                elementsToReplace.Count > 0)
            {
                elements.ReplaceElement(elementsToReplace.Dequeue());
            }
            else
            {
                entryTime = 0;
                replacingElements = false;
                CalculateCoreAlienCount();
            }
        }

        private void CalculateCoreAlienCount()
        {
            // Figure out how many core aliens to display
            int elementsLeftToReplace = elements.GetActiveElements().Count;
            if (elementsLeftToReplace > 8)
            {
                coreAlienCount = 0;
            }
            else if (elementsLeftToReplace <= 8 && elementsLeftToReplace > 6)
            {
                coreAlienCount = 1;
            }
            else if (elementsLeftToReplace <= 6 && elementsLeftToReplace > 4)
            {
                coreAlienCount = 2;
            }
            else if (elementsLeftToReplace <= 4 && elementsLeftToReplace > 2)
            {
                coreAlienCount = 3;
            }
            else if (elementsLeftToReplace < 2)
            {
                coreAlienCount = 4;
            }
        }

        public void Reset(object Data)
        {
        }

        /// <summary>
        /// Some screens need to keep events moving even when they are not showing
        /// </summary>
        /// <param name="gameTime"></param>
        public void UpdateOffScreen(GameTime gameTime)
        {
            for (int a = 0; a < coreAlienCount; a++)
            {
                coreAliens[a].Update(gameTime);
            }

            elements.Update(gameTime);
  
            if (coreSparks != null)
            {
                coreSparks.Start();
                coreSparks.Update(512, 288, gameTime);
            }

            if (coreComputationEffect != null)
            {
                coreComputationEffect.Start();
                coreComputationEffect.Update(512, 288, gameTime);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (justEnteredCore)
            {
                blankingTilesOpacity = 255;
                replacingElements = false;
                Initialise(null);
                justEnteredCore = false;

                CalculateCoreAlienCount();

                elementsToReplace = new Queue<int>();

                // Check to see if blob is carrying any elements to replace
                List<CoreElement> activeElements = elements.GetActiveElements();

                for (int a = 0; a < gameEngine.Player.BlobInventory.Length; a++)
                {
                    for (int b = 0; b < activeElements.Count; b++)
                    {
                        if ((int)gameEngine.Player.BlobInventory[a] == activeElements[b].CurrentElement)
                        {
                            gameEngine.Player.BlobInventory[a] = enCollectables.None;
                            elementsToReplace.Enqueue(activeElements[b].CurrentElement);
                            replacingElements = true;
                            
                        }
                    }
                }

                if (!replacingElements)
                {
                    entryTime = 0;
                }
                else
                {
                    elements_OnCoreElementReplaced();
                }
            }

            if (!replacingElements)
            {
                if (blankingTilesOpacity > 0)
                {
                    blankingTilesOpacity = blankingTilesOpacity - 16;
                }

                if (blankingTilesOpacity < 0)
                {
                    blankingTilesOpacity = 0;
                }
            }

            // Leave the core room
            if (!replacingElements && entryTime > 240)
            {
                gameEngine.GameScreens.ScreenNumber = 198;
                gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                gameEngine.Player.BlobXPos = gameEngine.GameScreens.ScreenWidth - gameEngine.Player.BlobWidth;
                gameEngine.GetGameScreen(enGameScreen.MainGame).PerformPostScreenChangeActions(enGameScreen.PlanetCore, null);

                justEnteredCore = true;
                gameEngine.BackgroundColour = Color.Black;
            }
            else if (!replacingElements && gameEngine.Elements.GetElementsReplacedCount() == 9)
            {
                // Finished the game!
                gameEngine.CurrentGameScreen = enGameScreen.GameComplete;
                gameEngine.GetGameScreen(enGameScreen.GameComplete).PerformPostScreenChangeActions(enGameScreen.MainGame, null);
            }
            else
            {
                UpdateOffScreen(gameTime);
            }

            entryTime++;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!replacingElements)
            {
                if (coreComputationEffect != null)
                {
                    coreComputationEffect.Draw();
                }

                if (coreSparks != null)
                {
                    coreSparks.Draw();
                }
            }

            if (!replacingElements)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").Begin(BlendState.AlphaBlend, gameTime);
                for (int a = 0; a < 6; a++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").DrawSprite(0, (b * 128) + 128, (a * 96),  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)blankingTilesOpacity));
                    }
                }
                gameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").End();
            }

            gameEngine.GameScreens.Draw(Color.White, gameTime);

            // Draw the core aliens
            if (!replacingElements)
            {
                for (int a = 0; a < coreAlienCount; a++)
                {
                    coreAliens[a].Draw(gameTime, (byte)(255 - blankingTilesOpacity));
                }
            }

            elements.Draw(gameTime, replacingElements);
        }

        #region IGameScreen Members

        public int ScreenFade
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IGameScreen Members


        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
