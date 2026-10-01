// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.WorldGeneration;

/// <summary>
///     Scatters the pick-ups, power-ups, pyramids and access codes over the 512 screens. Run once at
///     the start of a game, which is what makes every game of Starquake a different game on the same
///     map: the map is authored, what is lying on it is not.
/// </summary>
/// <remarks>
///     Everything here is decided by one <see cref="Random" />, handed in rather than made, so a world
///     is reproducible from the seed that made it. That is what makes this testable at all, and it is
///     why the one-line change that stopped <c>Reset</c> building its own generator from the clock
///     was worth its own commit in phase 0.
///     <para>
///         It works on the screen records and the collision map, and needs no content: the tile size
///         is a constant rather than something to ask a sprite sheet for.
///     </para>
/// </remarks>
public class WorldPopulator
{
    /// <summary>
    ///     How many of the 512 screens are given a pick-up.
    /// </summary>
    private const int PickUpCount = 100;

    /// <summary>
    ///     How many pyramids and core elements survive the thinning. Every screen with room gets one
    ///     during the scatter and the excess is taken away afterwards, rather than the placement
    ///     trying to be selective.
    /// </summary>
    private const int MaxCheops = 40;

    /// <inheritdoc cref="MaxCheops" />
    private const int MaxCoreElements = 18;

    /// <summary>
    ///     The range a Cheops code is drawn from - two digits - and an access booth code - three.
    ///     Both are redrawn until no digit repeats, which is what makes them readable on the scanner.
    /// </summary>
    private const int CheopsCodeLowest = 10;

    /// <inheritdoc cref="CheopsCodeLowest" />
    private const int CheopsCodeExclusiveUpperBound = 100;

    /// <inheritdoc cref="CheopsCodeLowest" />
    private const int AccessCodeLowest = 100;

    /// <inheritdoc cref="CheopsCodeLowest" />
    private const int AccessCodeExclusiveUpperBound = 1000;

    /// <summary>
    ///     How many kinds of pick-up the scatter draws from, and how many kinds of power-up. The
    ///     pick-up count stops one short of <see cref="Collectables.AccessCard" /> and
    ///     <see cref="Collectables.Key" />, which is why neither has ever appeared on the map - see
    ///     PLAN.md, "The key and the access card".
    /// </summary>
    private const int PickUpTypeCount = 28;

    /// <inheritdoc cref="PickUpTypeCount" />
    private const int PowerUpTypeCount = 8;

    /// <summary>
    ///     How many of the core elements lying about the map are set to ones the core actually wants,
    ///     so that a game is always winnable without being trivially so.
    /// </summary>
    private const int MinimumCoreMatches = 2;

    /// <inheritdoc cref="MinimumCoreMatches" />
    private const int MaximumCoreMatchesExclusive = 6;

    /// <summary>
    ///     The two tiles the map uses as plain scenery either side of a doorway. They are recognised
    ///     by number only so that an item is never placed on top of one.
    /// </summary>
    private const int SceneryTile1 = 120;

    /// <inheritdoc cref="SceneryTile1" />
    private const int SceneryTile2 = 121;

    /// <summary>
    ///     Where an item is drawn relative to the tile it stands on: one collision cell in from the
    ///     tile's left edge, and two cells above its top.
    /// </summary>
    private const int PlacementXInset = TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="PlacementXInset" />
    private const int PlacementYLift = TileMetrics.CollisionCellHeight * 2;

    /// <summary>
    ///     The tiles no pick-up, power-up or pyramid may be placed on, because each of them already
    ///     means something to the game - a shuttle, a spike, a bridge, a marker. This was a fifteen-term
    ///     chain of inequalities.
    /// </summary>
    private static readonly HashSet<int> TilesNothingMayBePlacedOn =
    [
        (int)ActionTiles.ShuttlePlatform,
        (int)ActionTiles.RemovedShuttlePlatform,
        (int)ActionTiles.ElectricityNode,
        (int)ActionTiles.ShuttleTag1,
        (int)ActionTiles.ShuttleTag2,
        (int)ActionTiles.ShuttleTag3,
        (int)ActionTiles.Spike1,
        (int)ActionTiles.Spike2,
        (int)ActionTiles.Spike3,
        (int)ActionTiles.ElectricityGate,
        (int)ActionTiles.BlankFiller,
        (int)ActionTiles.ConveyerNasty,
        (int)ActionTiles.BreakableBridges,
        SceneryTile1,
        SceneryTile2,
        (int)ActionTiles.PreferredCoreLocation
    ];

    private readonly ScreenCollisionMap _collisions;
    private readonly Random _randomNumberGenerator;
    private readonly GameScreenData[] _screens;

    public WorldPopulator(GameScreenData[] screens, ScreenCollisionMap collisions, Random randomNumberGenerator)
    {
        _screens = screens;
        _collisions = collisions;
        _randomNumberGenerator = randomNumberGenerator;
    }

    /// <summary>
    ///     Fills the whole world.
    /// </summary>
    /// <param name="coreElementsWanted">
    ///     The element numbers the core is asking for this game. Some of the core elements scattered
    ///     over the map are set to these so that the game can be finished; the list is passed in
    ///     rather than read off <c>CoreElements</c>, which would drag the graphics in behind it.
    /// </param>
    public void Populate(IReadOnlyList<int> coreElementsWanted)
    {
        List<int> powerUpScreens = ChoosePowerUpScreens();
        PlaceScreenObjects(powerUpScreens);

        var screensContainingCheopsPyramid = new List<GameScreenData>();
        var screensContainingCoreElements = new List<GameScreenData>();
        CollectScreensWithCheopsAndCoreElements(screensContainingCheopsPyramid, screensContainingCoreElements);

        TrimTo(screensContainingCheopsPyramid, MaxCheops, screen => screen.CheopsPyramid = null);
        TrimTo(screensContainingCoreElements, MaxCoreElements,
            screen => screen.PickUps = new List<InteractiveItem>());

        MatchSomeCoreElementsToTheCore(screensContainingCoreElements, coreElementsWanted);
    }

    /// <summary>
    ///     Whether a code reads unambiguously - no digit appearing twice.
    /// </summary>
    private static bool CheckAccessCodeValidity(string accessCode)
    {
        for (int a = 0; a < accessCode.Length; a++)
        {
            for (int b = 0; b < accessCode.Length; b++)
            {
                if (a != b && accessCode[a] == accessCode[b])
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Picks the screens a power-up goes on. Only screens that can hold more than one object qualify,
    ///     because a power-up has to share with whatever is already there.
    /// </summary>
    private List<int> ChoosePowerUpScreens()
    {
        var powerUpScreens = new List<int>();

        for (int a = 0; a < PickUpCount; a++)
        {
            // Only place on screens where we can have more than 1 object
            int screenChosen = -1;
            while (screenChosen < 0)
            {
                int temp = _randomNumberGenerator.Next(0, WorldMap.LastScreenNumber);

                // Ensure more than one location, and that we haven't chosen this screen already
                if (GetPotentialLocations(temp).Count > 1 && !powerUpScreens.Contains(temp))
                {
                    screenChosen = temp;
                }
            }

            powerUpScreens.Add(screenChosen);
        }

        return powerUpScreens;
    }

    /// <summary>
    ///     Fills in every screen but the core: its pick-ups, its power-up if it drew one, its pyramid and
    ///     its access code.
    /// </summary>
    private void PlaceScreenObjects(List<int> powerUpScreens)
    {
        for (int a = 0; a < _screens.Length; a++)
        {
            // The core screen is seeded differently and is left out of the scatter
            if (a == WorldMap.CoreScreenNumber)
            {
                continue;
            }

            _screens[a].PickUps = new List<InteractiveItem>();

            List<InteractiveItem> potentialLocations = GetPotentialLocations(a);
            bool placePickup = powerUpScreens.Contains(a);

            if (potentialLocations.Count > 0)
            {
                PlacePowerUpAndPickUp(a, potentialLocations, placePickup);
            }

            if (potentialLocations.Count > 0)
            {
                PlaceCheopsPyramid(a, potentialLocations);
            }

            AssignAccessBoothCode(a);
        }
    }

    /// <summary>
    ///     The power-up first, then a pick-up on a different tile if this screen drew one. Each takes its
    ///     location out of the list so the two cannot land on top of each other.
    /// </summary>
    private void PlacePowerUpAndPickUp(
        int screenNumber,
        List<InteractiveItem> potentialLocations,
        bool placePickup)
    {
        int powerUpToSelect = _randomNumberGenerator.Next(potentialLocations.Count);
        _screens[screenNumber].PowerUp = new InteractiveItem(
            potentialLocations[powerUpToSelect].XLocation,
            potentialLocations[powerUpToSelect].YLocation,
            _randomNumberGenerator.Next(PowerUpTypeCount));

        // Take the powerup location out of the list
        potentialLocations.Remove(potentialLocations[powerUpToSelect]);

        if (!placePickup)
        {
            return;
        }

        // Now select another location for our pickup
        int pickUpToSelect = _randomNumberGenerator.Next(potentialLocations.Count);

        // A screen that names where a core element would best go gets it there instead
        InteractiveItem? preferredLocation = GetPreferredLocation(screenNumber);

        if (preferredLocation != null)
        {
            preferredLocation.Type = _randomNumberGenerator.Next(PickUpTypeCount);
            _screens[screenNumber].PickUps.Add(preferredLocation);
        }
        else
        {
            _screens[screenNumber].PickUps.Add(new InteractiveItem(
                potentialLocations[pickUpToSelect].XLocation,
                potentialLocations[pickUpToSelect].YLocation,
                _randomNumberGenerator.Next(PickUpTypeCount)));
        }

        potentialLocations.Remove(potentialLocations[pickUpToSelect]);
    }

    /// <summary>
    ///     Every screen with room gets one at this stage. <see cref="Populate" /> thins them down to
    ///     <see cref="MaxCheops" /> afterwards, which is why there is no attempt to be selective here.
    /// </summary>
    private void PlaceCheopsPyramid(
        int screenNumber,
        List<InteractiveItem> potentialLocations)
    {
        int cheopsLocationToSelect = _randomNumberGenerator.Next(potentialLocations.Count);
        _screens[screenNumber].CheopsPyramid = new InteractiveItem(
            potentialLocations[cheopsLocationToSelect].XLocation,
            potentialLocations[cheopsLocationToSelect].YLocation, 0);

        _screens[screenNumber].CheopsAccessCode =
            DrawUniqueCode(CheopsCodeLowest, CheopsCodeExclusiveUpperBound);
    }

    /// <summary>
    ///     A screen with a booth on either side gets one code, which is what the booth on the far side of
    ///     the door asks for.
    /// </summary>
    private void AssignAccessBoothCode(int screenNumber)
    {
        bool hasBooth =
            HasTile(screenNumber, (int)ActionTiles.AccessBoothLeft) ||
            HasTile(screenNumber, (int)ActionTiles.AccessBoothRight);

        if (hasBooth)
        {
            _screens[screenNumber].AccessCode =
                DrawUniqueCode(AccessCodeLowest, AccessCodeExclusiveUpperBound);
        }
    }

    /// <summary>
    ///     A code from the range, redrawn until no digit in it repeats.
    /// </summary>
    private string DrawUniqueCode(int lowest, int exclusiveUpperBound)
    {
        string code;

        do
        {
            code = _randomNumberGenerator.Next(lowest, exclusiveUpperBound).ToString();
        } while (!CheckAccessCodeValidity(code));

        return code;
    }

    private bool HasTile(int screenNumber, int tileType)
    {
        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (_screens[screenNumber].ScreenTiles[x, y].TileImage == tileType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Every tile on a screen that an item could sit on top of: solid enough to stand on, with a
    ///     clear space above it, and not one of the tiles that already mean something.
    /// </summary>
    private List<InteractiveItem> GetPotentialLocations(int screenNumber)
    {
        var toReturn = new List<InteractiveItem>();

        // Start at row 1: nothing above that row is reachable
        for (int y = 1; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tileImage = _screens[screenNumber].ScreenTiles[x, y].TileImage;

                if (tileImage < 0 || TilesNothingMayBePlacedOn.Contains(tileImage))
                {
                    continue;
                }

                // Anything above it....?
                if (_screens[screenNumber].ScreenTiles[x, y - 1].TileImage != -1)
                {
                    continue;
                }

                // Only place on tiles with collision blocks below the powerup
                if (!_collisions.CanStandOnTopOf(tileImage))
                {
                    continue;
                }

                toReturn.Add(new InteractiveItem(
                    x * TileMetrics.TileWidth + PlacementXInset,
                    y * TileMetrics.TileHeight - PlacementYLift,
                    0));
            }
        }

        return toReturn;
    }

    /// <summary>
    ///     Where this screen's author said a core element would be best placed, if they said so at all.
    /// </summary>
    private InteractiveItem? GetPreferredLocation(int screenNumber)
    {
        for (int y = 1; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (_screens[screenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.PreferredCoreLocation)
                {
                    return new InteractiveItem(
                        x * TileMetrics.TileWidth + TileMetrics.CollisionCellWidth,
                        y * TileMetrics.TileHeight + TileMetrics.CollisionCellHeight,
                        0);
                }
            }
        }

        return null;
    }

    private void CollectScreensWithCheopsAndCoreElements(
        List<GameScreenData> screensContainingCheopsPyramid,
        List<GameScreenData> screensContainingCoreElements)
    {
        foreach (GameScreenData screen in _screens)
        {
            if (screen.CheopsPyramid != null)
            {
                screensContainingCheopsPyramid.Add(screen);
            }

            // A screen counts as holding a core element if it holds any pick-up that is not one of
            // the two items the core does not want - a guard written in anticipation of the key and
            // the access card being placed, which they never were.
            foreach (InteractiveItem pickUp in screen.PickUps)
            {
                if ((Collectables)pickUp.Type != Collectables.AccessCard &&
                    (Collectables)pickUp.Type != Collectables.Key)
                {
                    screensContainingCoreElements.Add(screen);
                    break;
                }
            }
        }
    }

    /// <summary>
    ///     Thins a list of screens down to at most <paramref name="maximum" /> of them by picking one at
    ///     random and stripping what it was listed for. The pyramids and the core elements were two copies
    ///     of this loop differing only in what they cleared.
    /// </summary>
    private void TrimTo(
        List<GameScreenData> screens,
        int maximum,
        Action<GameScreenData> removeFrom)
    {
        while (screens.Count > maximum)
        {
            GameScreenData screen = screens[_randomNumberGenerator.Next(0, screens.Count)];

            removeFrom(screen);
            screens.Remove(screen);
        }
    }

    /// <summary>
    ///     Between two and five of the core elements lying about the map are set to ones the core actually
    ///     wants, so that a game is always winnable without being trivially so.
    /// </summary>
    private void MatchSomeCoreElementsToTheCore(
        List<GameScreenData> screensContainingCoreElements,
        IReadOnlyList<int> coreElementsWanted)
    {
        var elementsLeftToPlace = new List<int>(coreElementsWanted);

        // Neither list is a fixed length - one is however many screens the generator gave a pick-up
        // to, the other however many elements the core still wants - so the draw is clamped to what
        // is actually there rather than trusted to fit.
        int matchesToMake = Math.Min(
            _randomNumberGenerator.Next(MinimumCoreMatches, MaximumCoreMatchesExclusive),
            Math.Min(screensContainingCoreElements.Count, elementsLeftToPlace.Count));

        for (int a = 0; a < matchesToMake; a++)
        {
            int elementIndex = _randomNumberGenerator.Next(0, elementsLeftToPlace.Count);

            screensContainingCoreElements[a].PickUps[0].Type = elementsLeftToPlace[elementIndex];
            elementsLeftToPlace.RemoveAt(elementIndex);
        }
    }
}
