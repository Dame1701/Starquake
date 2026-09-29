// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Engines;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class CoreElements : GameComponentBase
{
    public CoreElements(GameEngine gameEngineReference) : base(gameEngineReference)
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

    public void ReplaceElement(int elementNoToReplace)
    {
        for (var a = 0; a < 9; a++)
        {
            if (Elements[a].CurrentElement == elementNoToReplace)
            {
                // You get a healthy 10,000 points for each element replaced!
                GameEngine.BlobStatusPanel.PlayerScore = GameEngine.BlobStatusPanel.PlayerScore + 10000;

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
                var randomChoice = GameEngine.RandomNumberGenerator.Next(0, elementsToChoose.Count);
                Elements[counter] = new CoreElement(GameEngine, elementsToChoose[randomChoice], counter, 6, fadeUp);
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

    public void Draw(GameTime gameTime, bool replacingElement)
    {
        var coreX = 416;
        var coreY = 192;
        var counter = 0;

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                Elements[counter].Draw(coreX + x * 64, coreY + y * 64, gameTime, replacingElement);
                counter++;
            }
        }

    }

    public void Draw(GameTime gameTime, int xPos, int yPos, int fade)
    {

        var counter = 0;
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                Elements[counter].Draw(xPos + x * 64, yPos + y * 64, fade, gameTime);
                counter++;
            }
        }

    }
}