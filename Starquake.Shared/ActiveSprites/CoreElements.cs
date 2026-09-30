// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class CoreElements : GameComponentBase
{
    private readonly GameEngine GameEngine;

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

    public CoreElements(GameEngine gameEngineReference)
    {
        GameEngine = gameEngineReference;

        Elements = new CoreElement[9];
        Reset();
    }
    public CoreElement[] Elements { get; }

    public event RaiseEffectComplete? OnCoreElementReplaced;

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

    public void ReplaceElement(int elementNoToReplace)
    {
        for (var a = 0; a < 9; a++)
        {
            if (Elements[a].CurrentElement == elementNoToReplace)
            {
                // You get a healthy 10,000 points for each element replaced!
                GameEngine.BlobStatusPanel.PlayerScore = GameEngine.BlobStatusPanel.PlayerScore + ScoreForCompletingCore;

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

        for (var y = 0; y < CoreGridDown; y++)
        {
            for (var x = 0; x < CoreGridAcross; x++)
            {
                if (counter % 2 == 0)
                {
                    fadeUp = true;
                }
                else
                {
                    fadeUp = false;
                }
                var randomChoice = GameEngine.RandomNumberGenerator.Next(0, elementsToChoose.Count);
                Elements[counter] = new CoreElement(GameEngine, elementsToChoose[randomChoice], counter, 6, fadeUp);
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
        var coreX = CoreXPos;
        var coreY = CoreYPos;

        for (var y = 0; y < CoreGridDown; y++)
        {
            for (var x = 0; x < CoreGridAcross; x++)
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
        foreach (var element in Elements)
        {
            element.Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime, bool replacingElement)
    {
        var coreX = CoreXPos;
        var coreY = CoreYPos;
        var counter = 0;

        for (var y = 0; y < CoreGridDown; y++)
        {
            for (var x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Draw(coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, gameTime, replacingElement);
                counter++;
            }
        }

    }

    public void Draw(GameTime gameTime, int xPos, int yPos, int fade)
    {

        var counter = 0;
        for (var y = 0; y < CoreGridDown; y++)
        {
            for (var x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Draw(xPos + x * SpriteMetrics.SpriteWidth, yPos + y * SpriteMetrics.SpriteHeight, fade, gameTime);
                counter++;
            }
        }

    }
}