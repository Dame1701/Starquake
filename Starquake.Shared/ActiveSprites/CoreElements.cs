// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Scoring;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

internal class CoreElements
{

    /// <summary>
    ///     The core is a three by three block of elements sitting in the middle of the core room.
    ///     Each element is drawn a sprite apart, and the whole block starts here.
    /// </summary>
    /// <summary>
    ///     How many elements the core is across and down. Read from <see cref="CoreSite" /> rather than
    ///     said again here, because the screen editor outlines the same area and the two must agree.
    ///     Where the core sits is no longer here at all: it is on the map, and is passed in.
    /// </summary>
    private const int CoreGridAcross = CoreSite.SpritesAcross;

    /// <inheritdoc cref="CoreGridAcross" />
    private const int CoreGridDown = CoreSite.SpritesDown;

    /// <summary>
    ///     What finishing the core is worth.
    /// </summary>
    private const int ScoreForCompletingCore = 10000;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly PlayerStats _stats;

    public CoreElements(
        PlayerStats stats,
        ResourceManager resources,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator)
    {
        _stats = stats;
        _resources = resources;
        _graphicsSet = graphicsSet;
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
                _stats.PlayerScore += ScoreForCompletingCore;

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

    /// <summary>
    ///     A fresh set of nine elements for the core to be missing, drawn without repeats so no two
    ///     of them ask for the same thing.
    /// </summary>
    public void Reset()
    {
        int counter = 0;
        bool fadeUp;

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
                Elements[counter] = new CoreElement(_resources, _graphicsSet, _randomNumberGenerator, elementsToChoose[randomChoice], counter, fadeUp);
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

    public List<CollisionTile> GetCollisionMap(int coreX, int coreY)
    {
        var toReturn = new List<CollisionTile>();

        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                toReturn.Add(CollisionTile.ForObject(CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, CollisionTileType.NotBlob));
                toReturn.Add(CollisionTile.ForObject(CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth + SpriteMetrics.HalfSpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, CollisionTileType.NotBlob));
                toReturn.Add(CollisionTile.ForObject(CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight + SpriteMetrics.HalfSpriteHeight, CollisionTileType.NotBlob));
                toReturn.Add(CollisionTile.ForObject(CollisionObjectType.CoreElement, coreX + x * SpriteMetrics.SpriteWidth + SpriteMetrics.HalfSpriteWidth, coreY + y * SpriteMetrics.SpriteHeight + SpriteMetrics.HalfSpriteHeight, CollisionTileType.NotBlob));
            }
        }

        return toReturn;
    }

    public void Update(GameTime gameTime, Point corePosition)
    {
        int counter = 0;

        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Update(
                    gameTime,
                    corePosition.X + x * SpriteMetrics.SpriteWidth,
                    corePosition.Y + y * SpriteMetrics.SpriteHeight);
                counter++;
            }
        }
    }

    public void Draw(int coreX, int coreY, bool replacingElement)
    {
        int counter = 0;

        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Draw(coreX + x * SpriteMetrics.SpriteWidth, coreY + y * SpriteMetrics.SpriteHeight, replacingElement);
                counter++;
            }
        }

    }

    public void Draw(int xPos, int yPos, int fade)
    {

        int counter = 0;
        for (int y = 0; y < CoreGridDown; y++)
        {
            for (int x = 0; x < CoreGridAcross; x++)
            {
                Elements[counter].Draw(xPos + x * SpriteMetrics.SpriteWidth, yPos + y * SpriteMetrics.SpriteHeight, fade);
                counter++;
            }
        }

    }
}
