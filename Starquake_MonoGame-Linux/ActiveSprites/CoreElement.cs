using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharedLibs;
using Microsoft.Xna.Framework;
using StarquakeXNA.Particles;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public class CoreElements : GameComponentBase
    {
        private CoreElement[] elements;
        public CoreElement[] Elements
        {
            get { return elements; }
        }

        public event RaiseEffectComplete OnCoreElementReplaced;
        public CoreElements(GameEngine GameEngineReference) : base(GameEngineReference)
        {
            elements = new CoreElement[9];
            Reset();
        }

        void CoreElements_OnCoreElementReplaced()
        {
            for (int a = 0; a < 9; a++)
            {
                if (elements[a].ElementState == enElementState.Replacing)
                {
                    elements[a].ElementState = enElementState.Replaced;
                    if (OnCoreElementReplaced != null)
                    {
                        OnCoreElementReplaced();
                        break;
                    }
                }
                else if (elements[a].ElementState == enElementState.Missing)
                {
                    elements[a].SetFadeToZero();
                }
            }
        }

        public void ReplaceElement(int ElementNoToReplace)
        {
            for (int a = 0; a < 9; a++)
            {
                if (elements[a].CurrentElement == ElementNoToReplace)
                {
                    // You get a healthy 10,000 points for each element replaced!
                    gameEngine.BlobStatusPanel.PlayerScore = gameEngine.BlobStatusPanel.PlayerScore + 10000;

                    elements[a].ElementState = enElementState.Replacing;
                    int test = elements[a].CurrentElement;
                    break;
                }
            }
        }

        public List<CoreElement> GetActiveElements()
        {
            List<CoreElement> toReturn = new List<CoreElement>();

            for (int a = 0; a < 9; a++)
            {
                if (elements[a].ElementState == enElementState.Missing)
                {
                    toReturn.Add(elements[a]);
                }
            }

            return toReturn;
        }

        // Resets the elements back to defaults
        public void Reset()
        {
            int counter = 0;
            bool fadeUp;

            // ensure we don't choose the same element twice....
            List<int> elementsToChoose = new List<int>();
            for (int a = 0; a < 28; a++)
            {
                elementsToChoose.Add(a);
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (counter % 2 == 0)
                    {
                        fadeUp = true;
                    }
                    else
                    {
                        fadeUp = false;
                    }
                    int randomChoice = gameEngine.RandomNumberGenerator.Next(0, elementsToChoose.Count);
                    elements[counter] = new CoreElement(gameEngine, elementsToChoose[randomChoice], counter, 6, fadeUp);
                    elements[counter].LoadContent();
                    elements[counter].OnCoreElementReplaced += new RaiseEffectComplete(CoreElements_OnCoreElementReplaced);
                    elementsToChoose.RemoveAt(randomChoice);
                    elements[counter].ElementState = enElementState.Missing;
                    counter++;
                }
            }
        }

        public int GetElementsReplacedCount()
        {
            int elementsReplaced = 0;

            for (int a = 0; a < 9; a++)
            {
                if (elements[a].ElementState == enElementState.Replaced)
                {
                    elementsReplaced++;
                }
            }

            return elementsReplaced;
        }

        public List<CollisionTile> GetCollisionMap()
        {
            List<CollisionTile> toReturn = new List<CollisionTile>();
            int coreX = 416;
            int coreY = 192;

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    toReturn.Add(new CollisionTile((int)enCollisionObjectType.CoreElement, coreX + (x * 64), coreY + (y * 64), enCollTileType.NotBlob));
                    toReturn.Add(new CollisionTile((int)enCollisionObjectType.CoreElement, coreX + (x * 64) + 32, coreY + (y * 64), enCollTileType.NotBlob));
                    toReturn.Add(new CollisionTile((int)enCollisionObjectType.CoreElement, coreX + (x * 64), coreY + (y * 64) + 32, enCollTileType.NotBlob));
                    toReturn.Add(new CollisionTile((int)enCollisionObjectType.CoreElement, coreX + (x * 64) + 32, coreY + (y * 64) + 32, enCollTileType.NotBlob));
                }
            }

            return toReturn;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            for (int a = 0; a < elements.Length; a++)
            {
                elements[a].Update(gameTime);
            }
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime, bool ReplacingElement)
        {
            int coreX = 416;
            int coreY = 192;
            int counter = 0;

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    elements[counter].Draw(coreX + (x * 64), coreY + (y * 64), gameTime, ReplacingElement);
                    counter++;
                }
            }

        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime, int XPos, int YPos, int Fade)
        {
            
            int counter = 0;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    elements[counter].Draw(XPos + (x * 64), YPos + (y * 64), Fade, gameTime);
                    counter++;
                }
            }

        }
    }

    public class CoreElement
    {
        internal class ElementFader
        {
            private int fadeLevel;
            public int FadeLevel
            {
                get { return fadeLevel; }
            }

            private int fadeSpeed;
            private enDirection fadeDirection;

            public ElementFader(GameEngine GameEngineInstance, enDirection FadeDirection)
            {
                fadeDirection = FadeDirection;
                fadeSpeed = GameEngineInstance.RandomNumberGenerator.Next(8, 16);
                fadeLevel = GameEngineInstance.RandomNumberGenerator.Next(50, 200);
            }

            public void SetFadeToZero()
            {
                fadeLevel = 0;
                fadeDirection = enDirection.Up;
            }

            public void Update()
            {
                if (fadeDirection == enDirection.Up)
                {
                    fadeLevel = fadeLevel + fadeSpeed;
                }
                else if (fadeDirection == enDirection.Down)
                {
                    fadeLevel = fadeLevel - fadeSpeed;
                }

                if (fadeLevel > 255)
                {
                    fadeLevel = 255;
                    fadeDirection = enDirection.Down;
                }
                else if (fadeLevel < 0)
                {
                    fadeLevel = 0;
                    fadeDirection = enDirection.Up;
                }

            }
        }

        private enElementState elementState;
        public enElementState ElementState
        {
            get { return elementState; }
            set { elementState = value; }
        }

        private int currentElement;
        public int CurrentElement
        {
            get { return currentElement; }
        }

        private int finalElement;
        public int FinalElement
        {
            get { return finalElement; }
        }

        private GameEngine gameEngine;

        private bool elementReplacementStarted = false;

        private ElementFader fader1;
        private ElementFader fader2;
        private ElementFader fader3;

        public event RaiseEffectComplete OnCoreElementReplaced;
        IStandardParticleEffect coreReplacementEffect;
        int xPos = 0;
        int yPos = 0;

        private AnimationHelpers.FloatSubtractor currentElementDownScaler;
        private AnimationHelpers.FloatAdder finalElementUpScaler;
        private AnimationHelpers.IntegerSubtractor elementFader;
        private AnimationHelpers.IntegerSubtractor screenFlashFader;

        public CoreElement(GameEngine GameEngineInstance, int CurrentElement, int FinalElement, int FadeSpeed, bool StartFadeUp)
        {
            currentElementDownScaler = new StarquakeXNA.AnimationHelpers.FloatSubtractor(3, 1, 0.1f, false);
            finalElementUpScaler = new StarquakeXNA.AnimationHelpers.FloatAdder(0, 1, 0.05f, false);
            elementFader = new StarquakeXNA.AnimationHelpers.IntegerSubtractor(255, 0, 8, false);
            screenFlashFader = new StarquakeXNA.AnimationHelpers.IntegerSubtractor(255, 0, 5, true);

            elementState = enElementState.Missing;
            currentElement = CurrentElement;
            finalElement = FinalElement;
            gameEngine = GameEngineInstance;

            if (StartFadeUp)
            {
                fader1 = new ElementFader(gameEngine, enDirection.Up);
                fader2 = new ElementFader(gameEngine, enDirection.Down);
                fader3 = new ElementFader(gameEngine, enDirection.Up);
            }
            else
            {
                fader1 = new ElementFader(gameEngine, enDirection.Down);
                fader2 = new ElementFader(gameEngine, enDirection.Up);
                fader3 = new ElementFader(gameEngine, enDirection.Down);
            }
            
        }

        public void SetFadeToZero()
        {
            fader1.SetFadeToZero();
            fader2.SetFadeToZero();
            fader3.SetFadeToZero();
        }

        public void LoadContent()
        {
            coreReplacementEffect = gameEngine.ContentResourceManager.GetParticleEffectInstance("CoreReplacementParticleEffect", enParticleEffectType.Continual, new TimeSpan(0, 0, 0, 0, 200)) as IStandardParticleEffect;
            coreReplacementEffect.ParticleEffectComplete += new RaiseEffectComplete(coreReplacementEffect_ParticleEffectComplete);
        }

        void coreReplacementEffect_ParticleEffectComplete()
        {
            // We're all done
            if (OnCoreElementReplaced != null)
            {
                OnCoreElementReplaced();
            }
        }

        public void Initialise()
        {
            
        }

        public void PerformPostChangeScreenActions()
        {
           
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if (elementState == enElementState.Missing)
            {
                fader1.Update();
                fader2.Update();
                fader3.Update();
            }
            else if (elementState == enElementState.Replacing)
            {
                if (elementReplacementStarted)
                {
                    // Scale current element down
                    currentElementDownScaler.Update();
                    finalElementUpScaler.Update();
                    if (finalElementUpScaler.Modifier > 0.95)
                    {
                        if (elementState == enElementState.Replacing)
                        {
                            if (!coreReplacementEffect.EffectActive)
                            {
                                coreReplacementEffect.Start();
                            }
                        }

                        // Flash the screen white then fade out
                        screenFlashFader.Trigger();
                    }
                    elementFader.Update();
                }
            }
            if (screenFlashFader.Triggered)
            {
                gameEngine.BackgroundColour =  ColourHelper.GetXNA4Colour(screenFlashFader.ByteModifier, screenFlashFader.ByteModifier, screenFlashFader.ByteModifier);
                screenFlashFader.Update();
            }

            if (elementReplacementStarted && elementState == enElementState.Replacing)
            {
                coreReplacementEffect.Update(xPos + 32, yPos + 32, gameTime);
            }
            
        }

        public void Draw(int XPos, int YPos, int Fade, Microsoft.Xna.Framework.GameTime gameTime)
        {
            if (elementState == enElementState.Replaced)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(finalElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)Fade));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }
            else if (elementState == enElementState.Missing)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").DrawSprite(currentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255,255,255, (byte)Fade));
                gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").End();
            }
        }

        public void Draw(int XPos, int YPos, Microsoft.Xna.Framework.GameTime gameTime, bool ReplacingElement)
        {
            xPos = XPos;
            yPos = YPos;

            if (elementState == enElementState.Missing)
            {
                if (ReplacingElement)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(currentElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, 50));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(currentElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fader1.FadeLevel));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();

                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).DrawSprite(currentElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fader2.FadeLevel));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).End();

                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).DrawSprite(currentElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fader3.FadeLevel));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).End();
                }
            }
            else if (elementState == enElementState.Replacing)
            {
                if (elementReplacementStarted == false)
                {
                    elementReplacementStarted = true;
                }

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsIlluminatedName).Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsIlluminatedName).DrawSprite(currentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, elementFader.ByteModifier), currentElementDownScaler.Modifier, 0);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsIlluminatedName).End();

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(finalElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)(255 - elementFader.ByteModifier)), finalElementUpScaler.Modifier, 0);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }
            else if (elementState == enElementState.Replaced)
            {
                if (ReplacingElement)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(finalElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, 50));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(finalElement, XPos, YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, 255));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
                }
            }

            if (elementReplacementStarted && elementState == enElementState.Replacing)
            {
                coreReplacementEffect.Draw();
            }
        }
    }
}
