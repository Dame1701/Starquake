// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharedLibs;
using StarquakeXNA.AnimationHelpers;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class CoreElements : GameComponentBase
{
    public CoreElements(GameEngine GameEngineReference) : base(GameEngineReference)
    {
        Elements = new CoreElement[9];
        Reset();
    }
    public CoreElement[] Elements { get; }

    public event RaiseEffectComplete OnCoreElementReplaced;

    private void CoreElements_OnCoreElementReplaced()
    {
        for (var a = 0; a < 9; a++)
        {
            if (Elements[a].ElementState == CoreElementState.Replacing)
            {
                Elements[a].ElementState = CoreElementState.Replaced;
                if (OnCoreElementReplaced != null)
                {
                    OnCoreElementReplaced();
                    break;
                }
            }
            else if (Elements[a].ElementState == CoreElementState.Missing)
            {
                Elements[a].SetFadeToZero();
            }
        }
    }

    public void ReplaceElement(int ElementNoToReplace)
    {
        for (var a = 0; a < 9; a++)
        {
            if (Elements[a].CurrentElement == ElementNoToReplace)
            {
                // You get a healthy 10,000 points for each element replaced!
                gameEngine.BlobStatusPanel.PlayerScore = gameEngine.BlobStatusPanel.PlayerScore + 10000;

                Elements[a].ElementState = CoreElementState.Replacing;
                var test = Elements[a].CurrentElement;
                break;
            }
        }
    }

    public List<CoreElement> GetActiveElements()
    {
        var toReturn = new List<CoreElement>();

        for (var a = 0; a < 9; a++)
        {
            if (Elements[a].ElementState == CoreElementState.Missing)
            {
                toReturn.Add(Elements[a]);
            }
        }

        return toReturn;
    }

    // Resets the elements back to defaults
    public void Reset()
    {
        var counter = 0;
        bool fadeUp;

        // ensure we don't choose the same element twice....
        var elementsToChoose = new List<int>();
        for (var a = 0; a < 28; a++)
        {
            elementsToChoose.Add(a);
        }

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                if (counter % 2 == 0)
                {
                    fadeUp = true;
                }
                else
                {
                    fadeUp = false;
                }
                var randomChoice = gameEngine.RandomNumberGenerator.Next(0, elementsToChoose.Count);
                Elements[counter] = new CoreElement(gameEngine, elementsToChoose[randomChoice], counter, 6, fadeUp);
                Elements[counter].LoadContent();
                Elements[counter].OnCoreElementReplaced += CoreElements_OnCoreElementReplaced;
                elementsToChoose.RemoveAt(randomChoice);
                Elements[counter].ElementState = CoreElementState.Missing;
                counter++;
            }
        }
    }

    public int GetElementsReplacedCount()
    {
        var elementsReplaced = 0;

        for (var a = 0; a < 9; a++)
        {
            if (Elements[a].ElementState == CoreElementState.Replaced)
            {
                elementsReplaced++;
            }
        }

        return elementsReplaced;
    }

    public List<CollisionTile> GetCollisionMap()
    {
        var toReturn = new List<CollisionTile>();
        var coreX = 416;
        var coreY = 192;

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * 64, coreY + y * 64, CollisionTileType.NotBlob));
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * 64 + 32, coreY + y * 64, CollisionTileType.NotBlob));
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * 64, coreY + y * 64 + 32, CollisionTileType.NotBlob));
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * 64 + 32, coreY + y * 64 + 32, CollisionTileType.NotBlob));
            }
        }

        return toReturn;
    }

    public override void Update(GameTime gameTime)
    {
        for (var a = 0; a < Elements.Length; a++)
        {
            Elements[a].Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime, bool ReplacingElement)
    {
        var coreX = 416;
        var coreY = 192;
        var counter = 0;

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                Elements[counter].Draw(coreX + x * 64, coreY + y * 64, gameTime, ReplacingElement);
                counter++;
            }
        }

    }

    public void Draw(GameTime gameTime, int XPos, int YPos, int Fade)
    {

        var counter = 0;
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                Elements[counter].Draw(XPos + x * 64, YPos + y * 64, Fade, gameTime);
                counter++;
            }
        }

    }
}

public class CoreElement
{

    private readonly FloatSubtractor currentElementDownScaler;
    private readonly IntegerSubtractor elementFader;

    private readonly ElementFader fader1;
    private readonly ElementFader fader2;
    private readonly ElementFader fader3;

    private readonly FloatAdder finalElementUpScaler;

    private readonly GameEngine gameEngine;
    private readonly IntegerSubtractor screenFlashFader;
    private IStandardParticleEffect coreReplacementEffect;

    private bool elementReplacementStarted;

    private int xPos;
    private int yPos;

    public CoreElement(GameEngine GameEngineInstance, int CurrentElement, int FinalElement, int FadeSpeed, bool StartFadeUp)
    {
        currentElementDownScaler = new FloatSubtractor(3, 1, 0.1f, false);
        finalElementUpScaler = new FloatAdder(0, 1, 0.05f, false);
        elementFader = new IntegerSubtractor(255, 0, 8, false);
        screenFlashFader = new IntegerSubtractor(255, 0, 5, true);

        ElementState = CoreElementState.Missing;
        this.CurrentElement = CurrentElement;
        this.FinalElement = FinalElement;
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

    public CoreElementState ElementState { get; set; }

    public int CurrentElement { get; }

    public int FinalElement { get; }

    public event RaiseEffectComplete OnCoreElementReplaced;

    public void SetFadeToZero()
    {
        fader1.SetFadeToZero();
        fader2.SetFadeToZero();
        fader3.SetFadeToZero();
    }

    public void LoadContent()
    {
        coreReplacementEffect = gameEngine.ContentResourceManager.GetParticleEffectInstance("CoreReplacementParticleEffect", enParticleEffectType.Continual, new TimeSpan(0, 0, 0, 0, 200)) as IStandardParticleEffect;
        coreReplacementEffect.ParticleEffectComplete += coreReplacementEffect_ParticleEffectComplete;
    }

    private void coreReplacementEffect_ParticleEffectComplete()
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

    public void Update(GameTime gameTime)
    {
        if (ElementState == CoreElementState.Missing)
        {
            fader1.Update();
            fader2.Update();
            fader3.Update();
        }
        else if (ElementState == CoreElementState.Replacing)
        {
            if (elementReplacementStarted)
            {
                // Scale current element down
                currentElementDownScaler.Update();
                finalElementUpScaler.Update();
                if (finalElementUpScaler.Modifier > 0.95)
                {
                    if (ElementState == CoreElementState.Replacing)
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
            gameEngine.BackgroundColour = ColourHelper.GetXNA4Colour(screenFlashFader.ByteModifier, screenFlashFader.ByteModifier, screenFlashFader.ByteModifier);
            screenFlashFader.Update();
        }

        if (elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            coreReplacementEffect.Update(xPos + 32, yPos + 32, gameTime);
        }

    }

    public void Draw(int XPos, int YPos, int Fade, GameTime gameTime)
    {
        if (ElementState == CoreElementState.Replaced)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(FinalElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)Fade));
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
        }
        else if (ElementState == CoreElementState.Missing)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").DrawSprite(CurrentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)Fade));
            gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").End();
        }
    }

    public void Draw(int XPos, int YPos, GameTime gameTime, bool ReplacingElement)
    {
        xPos = XPos;
        yPos = YPos;

        if (ElementState == CoreElementState.Missing)
        {
            if (ReplacingElement)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(CurrentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, 50));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }
            else
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(CurrentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fader1.FadeLevel));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).DrawSprite(CurrentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fader2.FadeLevel));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).End();

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).DrawSprite(CurrentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)fader3.FadeLevel));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).End();
            }
        }
        else if (ElementState == CoreElementState.Replacing)
        {
            if (!elementReplacementStarted)
            {
                elementReplacementStarted = true;
            }

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsIlluminatedName).Begin(BlendState.Additive, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsIlluminatedName).DrawSprite(CurrentElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, elementFader.ByteModifier), currentElementDownScaler.Modifier, 0);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsIlluminatedName).End();

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(FinalElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)(255 - elementFader.ByteModifier)), finalElementUpScaler.Modifier, 0);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
        }
        else if (ElementState == CoreElementState.Replaced)
        {
            if (ReplacingElement)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(FinalElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, 50));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }
            else
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(FinalElement, XPos, YPos, ColourHelper.GetXNA4Colour(255, 255, 255, 255));
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }
        }

        if (elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            coreReplacementEffect.Draw();
        }
    }

    internal class ElementFader
    {

        private readonly int fadeSpeed;
        private enDirection fadeDirection;

        public ElementFader(GameEngine GameEngineInstance, enDirection FadeDirection)
        {
            fadeDirection = FadeDirection;
            fadeSpeed = GameEngineInstance.RandomNumberGenerator.Next(8, 16);
            FadeLevel = GameEngineInstance.RandomNumberGenerator.Next(50, 200);
        }
        public int FadeLevel { get; private set; }

        public void SetFadeToZero()
        {
            FadeLevel = 0;
            fadeDirection = enDirection.Up;
        }

        public void Update()
        {
            if (fadeDirection == enDirection.Up)
            {
                FadeLevel = FadeLevel + fadeSpeed;
            }
            else if (fadeDirection == enDirection.Down)
            {
                FadeLevel = FadeLevel - fadeSpeed;
            }

            if (FadeLevel > 255)
            {
                FadeLevel = 255;
                fadeDirection = enDirection.Down;
            }
            else if (FadeLevel < 0)
            {
                FadeLevel = 0;
                fadeDirection = enDirection.Up;
            }

        }
    }
}