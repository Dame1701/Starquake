// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class CoreElements : GameComponentBase
{

    /// <summary>
    ///     The core is a three by three block of elements sitting in the middle of the core room.
    ///     Each element is drawn a sprite apart, and the whole block starts here.
    /// </summary>
    private const int CoreGridAcross = 3;

    /// <inheritdoc cref="CoreGridAcross" />
    private const int CoreGridDown = 3;

    /// <inheritdoc cref="CoreGridAcross" />
    private const int CoreXPos = 416;

    /// <inheritdoc cref="CoreGridAcross" />
    private const int CoreYPos = 192;

    /// <summary>
    ///     What finishing the core is worth.
    /// </summary>
    private const int ScoreForCompletingCore = 10000;
    private readonly GraphicsSet _graphicsSet;
    private readonly PresentationSettings _presentation;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly StatusPanel _statusPanel;

    public CoreElements(
        StatusPanel statusPanel,
        ResourceManager resources,
        GraphicsSet graphicsSet,
        PresentationSettings presentation,
        Random randomNumberGenerator)
    {
        _statusPanel = statusPanel;
        _resources = resources;
        _graphicsSet = graphicsSet;
        _presentation = presentation;
        _randomNumberGenerator = randomNumberGenerator;

        Elements = new CoreElement[9];
        Reset();
    }
    public CoreElement[] Elements { get; }

    public event RaiseEffectComplete? OnCoreElementReplaced;

    private void CoreElements_OnCoreElementReplaced()
    {
        for (int a = 0; a < 9; a++)
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

    public void ReplaceElement(int elementNoToReplace)
    {
        for (int a = 0; a < 9; a++)
        {
            if (Elements[a].CurrentElement == elementNoToReplace)
            {
                // You get a healthy 10,000 points for each element replaced!
                _statusPanel.PlayerScore = _statusPanel.PlayerScore + ScoreForCompletingCore;

                Elements[a].ElementState = CoreElementState.Replacing;
                break;
            }
        }
    }

    public List<CoreElement> GetActiveElements()
    {
        var toReturn = new List<CoreElement>();

        for (int a = 0; a < 9; a++)
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
        int counter = 0;
        bool fadeUp;

        // ensure we don't choose the same element twice....
        var elementsToChoose = new List<int>();
        for (int a = 0; a < 28; a++)
        {
            elementsToChoose.Add(a);
        }

        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                if (counter % 2 == 0)
                {
                    fadeUp = true;
                }
                else
                {
                    fadeUp = false;
                }
                int randomChoice = _randomNumberGenerator.Next(0, elementsToChoose.Count);
                Elements[counter] = new CoreElement(_resources, _graphicsSet, _presentation, _randomNumberGenerator, elementsToChoose[randomChoice], counter, 6, fadeUp);
                Elements[counter].OnCoreElementReplaced += CoreElements_OnCoreElementReplaced;
                elementsToChoose.RemoveAt(randomChoice);
                Elements[counter].ElementState = CoreElementState.Missing;
                counter++;
            }
        }
    }

    public int GetElementsReplacedCount()
    {
        int elementsReplaced = 0;

        for (int a = 0; a < 9; a++)
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
        int coreX = CoreXPos;
        int coreY = CoreYPos;

        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, CollisionTileType.NotBlob));
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth + SpriteMetrics.HalfSpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, CollisionTileType.NotBlob));
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight + SpriteMetrics.HalfSpriteHeight, CollisionTileType.NotBlob));
                toReturn.Add(new CollisionTile((int)CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth + SpriteMetrics.HalfSpriteWidth, coreY + y * SpriteMetrics.SpriteHeight + SpriteMetrics.HalfSpriteHeight, CollisionTileType.NotBlob));
            }
        }

        return toReturn;
    }

    public override void Update(GameTime gameTime)
    {
        foreach (CoreElement element in Elements)
        {
            element.Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime, bool replacingElement)
    {
        int coreX = CoreXPos;
        int coreY = CoreYPos;
        int counter = 0;

        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Draw(coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, gameTime, replacingElement);
                counter++;
            }
        }

    }

    public void Draw(GameTime gameTime, int xPos, int yPos, int fade)
    {

        int counter = 0;
        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Draw(xPos + x * SpriteMetrics.SpriteWidth, yPos + y * SpriteMetrics.SpriteHeight, fade, gameTime);
                counter++;
            }
        }

    }
}
