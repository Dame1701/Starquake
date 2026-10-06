// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.Tests;

/// <summary>
///     The collision map as it was before it was indexed: one flat list of cells, scanned from end
///     to end for every question asked of it. Kept here, and nowhere else, as the oracle the real
///     map is measured against.
/// </summary>
/// <remarks>
///     This is a transcription rather than a wrapper. It shares no code with
///     <see cref="Starquake.Collisions.ScreenCollisionMap" /> - it walks a move one pixel at a time
///     rather than solving for it, and it answers from a list rather than a grid - so the two
///     agreeing is evidence and not a tautology. That is the same trick
///     <see cref="CollisionBroadPhaseTests" /> plays one level down, and for the same reason: the
///     rewrite was meant to be faster and nothing else, so what matters is that every answer
///     survived it.
///     <para>
///         It deliberately does not follow the real map's later corrections - it skips only tile
///         <c>-1</c> in <see cref="CheckTileByObject" /> rather than markers too, and it takes the
///         first tile in scan order rather than the nearest one. Those two are the changes that were
///         meant to happen, and <see cref="CheckTileByObjectTests" /> is where they are pinned.
///     </para>
/// </remarks>
internal sealed class ReferenceCollisionMap
{
    private const int GameObjectProbeSize = 32;
    private const int ItemCellsAcross = 2;
    private const int ItemCellSize = 32;
    private const int RightTunnelMouthInset = 96;
    private const int TunnelMouthCellsDown = 3;

    private readonly List<CollisionTile> _cells = [];
    private readonly List<IReferenceDynamicCells> _dynamicCellSources = [];
    private readonly GameScreenData[] _screens;
    private readonly int[,,] _tileCollisionData;

    public ReferenceCollisionMap(GameScreenData[] screens, int[,,] tileCollisionData)
    {
        _screens = screens;
        _tileCollisionData = tileCollisionData;
    }

    public int ScreenNumber { get; private set; }

    public IReadOnlyList<CollisionTile> Cells => _cells;

    private static int ScreenWidth => TileMetrics.TileWidth * TileMetrics.TilesAcross;

    private static int ScreenHeight => TileMetrics.TileHeight * TileMetrics.TilesDown;

    public void RegisterDynamicCells(IReferenceDynamicCells source)
        => _dynamicCellSources.Add(source);

    public void Rebuild(int screenNumber)
    {
        ScreenNumber = screenNumber;
        _cells.Clear();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (TileImageAt(x, y) <= -1 || MarkerTiles.Contains(TileImageAt(x, y)))
                {
                    continue;
                }

                AddTileCollisionCells(x, y);
                AddInteriorCollisionCells(x, y);
                AddCentralSpikeCollisionCells(x, y);
                AddWallSpikeCollisionCells(x, y);
                AddSecretPassageCollisionCells(x, y);
            }
        }

        AddScreenObjectCollisionTiles();

        foreach (IReferenceDynamicCells source in _dynamicCellSources)
        {
            foreach (CollisionTile cell in source.CellsOnScreen(ScreenNumber))
            {
                _cells.Add(cell);
            }
        }
    }

    public void Add(CollisionTile cell)
        => _cells.Add(cell);

    public void Remove(CollisionTile cell)
        => _cells.Remove(cell);

    public int CheckRectangles(int playerXPos, int playerYPos, int playerWidth, int playerHeight, bool isBlob)
    {
        foreach (CollisionTile collisionTile in _cells)
        {
            bool collide = Overlaps(
                new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                new Rectangle(collisionTile.XPos, collisionTile.YPos,
                    TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight));

            if (collide && !(isBlob && collisionTile.CollisionTileType == CollisionTileType.NotBlob))
            {
                return collisionTile.TileImage;
            }
        }

        return -1;
    }

    public int CheckCollision(Direction playerDirection, int playerXPos, int playerYPos,
        int playerWidth, int playerHeight, int velocity, bool isBlob)
    {
        int checkMovementAmount = velocity;

        foreach (CollisionTile colTile in _cells)
        {
            int allowed = StepAllowedBy(
                colTile, playerDirection, playerXPos, playerYPos, playerWidth, playerHeight, velocity, isBlob);

            if (allowed <= checkMovementAmount)
            {
                checkMovementAmount = allowed;
            }
        }

        if (!isBlob)
        {
            checkMovementAmount = ClampToScreenBounds(
                playerDirection, playerXPos, playerYPos, playerWidth, playerHeight, checkMovementAmount);
        }

        return checkMovementAmount;
    }

    public TileDetected? CheckTileByObject(Direction directionToCheck, int playerXPos, int playerYPos,
        int playerWidth, int playerHeight, int velocity)
    {
        if (ProbeRectangleFor(directionToCheck, playerXPos, playerYPos, playerWidth, playerHeight, velocity)
            is not { } probe)
        {
            return null;
        }

        bool measureFromFoot = directionToCheck is Direction.Up or Direction.Down;

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (TileImageAt(x, y) <= -1)
                {
                    continue;
                }

                var tileRect = new Rectangle(
                    x * TileMetrics.TileWidth, y * TileMetrics.TileHeight,
                    TileMetrics.TileWidth, TileMetrics.TileHeight);

                if (!Overlaps(probe, tileRect))
                {
                    continue;
                }

                int xpos = playerXPos - x * TileMetrics.TileWidth;
                int ypos = (measureFromFoot ? playerYPos + playerHeight : playerYPos) - y * TileMetrics.TileHeight;

                return new TileDetected(TileImageAt(x, y), xpos, ypos, x, y);
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether any cell sits exactly here and lets Blob through - the question
    ///     <c>GameScreenCollection.NastiesMayStandOn</c> asks, which used to mean scanning the whole
    ///     list once per cell of the tile it was asking about.
    /// </summary>
    public bool HasNastyOnlyCellAt(int cellX, int cellY)
    {
        foreach (CollisionTile cell in _cells)
        {
            if (cell.XPos == cellX && cell.YPos == cellY &&
                cell.CollisionTileType == CollisionTileType.NotBlob)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanStandOnTopOf(int tileImage)
        => _tileCollisionData[tileImage, 1, 0] == 1 && _tileCollisionData[tileImage, 2, 0] == 1;

    public bool IsSolid(int tileImage, int cellAcross, int cellDown)
        => tileImage >= 0 &&
           tileImage < _tileCollisionData.GetLength(0) &&
           _tileCollisionData[tileImage, cellAcross, cellDown] == 1;

    /// <summary>
    ///     Two boxes overlapping, by the distance between their centres - the game's own test,
    ///     written out again so the oracle leans on nothing that is being changed.
    /// </summary>
    private static bool Overlaps(Rectangle one, Rectangle two)
        => Math.Abs(one.X + one.Width / 2 - (two.X + two.Width / 2)) < one.Width / 2 + two.Width / 2 &&
           Math.Abs(one.Y + one.Height / 2 - (two.Y + two.Height / 2)) < one.Height / 2 + two.Height / 2;

    /// <summary>
    ///     The move walked a pixel at a time, which is how far a thing may travel before it meets
    ///     another - the shape the real one now solves for in one step.
    /// </summary>
    private static int WalkedStep(Direction direction, int velocity,
        int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        (int stepX, int stepY) = direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };

        if (stepX == 0 && stepY == 0)
        {
            return 0;
        }

        int moved = 0;

        for (int step = 1; step <= velocity; step++)
        {
            if (Overlaps(
                    new Rectangle(obj1X + stepX * step, obj1Y + stepY * step, obj1W, obj1H),
                    new Rectangle(obj2X, obj2Y, obj2W, obj2H)))
            {
                break;
            }

            moved++;
        }

        return moved;
    }

    private static Rectangle? ProbeRectangleFor(Direction directionToCheck,
        int playerXPos, int playerYPos, int playerWidth, int playerHeight, int velocity)
        => directionToCheck switch
        {
            Direction.Up => new Rectangle(playerXPos, playerYPos - velocity, playerWidth, playerHeight),
            Direction.Down => new Rectangle(playerXPos, playerYPos + playerHeight, playerWidth, velocity),
            Direction.Left => new Rectangle(playerXPos - velocity, playerYPos, playerWidth, playerHeight),
            Direction.Right => new Rectangle(playerXPos, playerYPos, playerWidth + velocity, playerHeight),
            Direction.None => new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
            _ => null
        };

    private static int StepAllowedBy(CollisionTile colTile, Direction playerDirection,
        int playerXPos, int playerYPos, int playerWidth, int playerHeight, int velocity, bool isBlob)
    {
        if (colTile.CollisionTileType == CollisionTileType.NotBlob && isBlob)
        {
            return velocity;
        }

        if (colTile.TileImage > 999 &&
            !isBlob &&
            (CollisionObjectType)colTile.TileImage is CollisionObjectType.JumpPlatform or
                CollisionObjectType.CoreElement or
                CollisionObjectType.GameObject &&
            Overlaps(
                new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                new Rectangle(colTile.XPos, colTile.YPos, GameObjectProbeSize, GameObjectProbeSize)))
        {
            return velocity;
        }

        return WalkedStep(playerDirection, velocity,
            playerXPos, playerYPos, playerWidth, playerHeight,
            colTile.XPos, colTile.YPos,
            TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight);
    }

    private static int ClampToScreenBounds(Direction playerDirection,
        int playerXPos, int playerYPos, int playerWidth, int playerHeight, int checkMovementAmount)
    {
        (int position, int size, int limit) = playerDirection switch
        {
            Direction.Up or Direction.Down => (playerYPos, playerHeight, ScreenHeight),
            Direction.Left or Direction.Right => (playerXPos, playerWidth, ScreenWidth),
            _ => (0, 0, 0)
        };

        if (playerDirection is Direction.Up or Direction.Left)
        {
            int overshoot = position - checkMovementAmount;

            return overshoot < 0 ? checkMovementAmount + overshoot : checkMovementAmount;
        }

        if (playerDirection is Direction.Down or Direction.Right)
        {
            int overshoot = position + size + checkMovementAmount - limit;

            return overshoot > 0 ? checkMovementAmount - overshoot : checkMovementAmount;
        }

        return checkMovementAmount;
    }

    private int TileImageAt(int x, int y)
        => _screens[ScreenNumber].ScreenTiles[x, y].TileImage;

    private void AddTileCollisionCells(int x, int y)
    {
        for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
        {
            for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
            {
                if (_tileCollisionData[TileImageAt(x, y), cellAcross, cellDown] != 1)
                {
                    continue;
                }

                _cells.Add(new CollisionTile(
                    TileImageAt(x, y),
                    x * TileMetrics.TileWidth + cellAcross * TileMetrics.CollisionCellWidth,
                    y * TileMetrics.TileHeight + cellDown * TileMetrics.CollisionCellHeight,
                    CollisionTileType.EveryoneHits));
            }
        }
    }

    private void AddInteriorCollisionCells(int x, int y)
    {
        if ((ActionTiles)TileImageAt(x, y) is not (ActionTiles.TeleportBooth or
            ActionTiles.AccessBoothLeft or
            ActionTiles.AccessBoothRight or
            ActionTiles.TunnelLiftTop1 or
            ActionTiles.TunnelLiftTop2 or
            ActionTiles.TunnelLiftTop3 or
            ActionTiles.TunnelLiftShaft1 or
            ActionTiles.TunnelLiftShaft2 or
            ActionTiles.TunnelLiftShaft3))
        {
            return;
        }

        for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
        {
            for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
            {
                AddNastyOnlyCell(
                    cellAcross * TileMetrics.CollisionCellWidth + x * TileMetrics.TileWidth,
                    cellDown * TileMetrics.CollisionCellHeight + y * TileMetrics.TileHeight);
            }
        }
    }

    private void AddCentralSpikeCollisionCells(int x, int y)
    {
        if ((ActionTiles)TileImageAt(x, y) is not (ActionTiles.Spike2 or ActionTiles.Spike3))
        {
            return;
        }

        for (int cellDown = 1; cellDown <= 2; cellDown++)
        {
            for (int cellAcross = 1; cellAcross <= 2; cellAcross++)
            {
                AddNastyOnlyCell(
                    cellAcross * TileMetrics.CollisionCellWidth + x * TileMetrics.TileWidth,
                    cellDown * TileMetrics.CollisionCellHeight + y * TileMetrics.TileHeight);
            }
        }
    }

    private void AddWallSpikeCollisionCells(int x, int y)
    {
        if ((ActionTiles)TileImageAt(x, y) != ActionTiles.Spike1)
        {
            return;
        }

        for (int cellDown = 0; cellDown <= 1; cellDown++)
        {
            for (int cellAcross = 2; cellAcross <= 3; cellAcross++)
            {
                AddNastyOnlyCell(
                    cellAcross * TileMetrics.CollisionCellWidth + x * TileMetrics.TileWidth,
                    cellDown * TileMetrics.CollisionCellHeight + y * TileMetrics.TileHeight);
            }
        }
    }

    private void AddSecretPassageCollisionCells(int x, int y)
    {
        for (int mouth = 0; mouth < TunnelTiles.MouthPairs; mouth++)
        {
            int tunnelTile = mouth + TunnelTiles.FirstMouth;

            if (TileImageAt(x, y) != tunnelTile)
            {
                continue;
            }

            int tileX = x * TileMetrics.TileWidth;
            int columnX = tunnelTile % 2 == 0 ? tileX : tileX + RightTunnelMouthInset;

            for (int cellDown = 0; cellDown < TunnelMouthCellsDown; cellDown++)
            {
                AddNastyOnlyCell(
                    columnX,
                    cellDown * TileMetrics.CollisionCellHeight + y * TileMetrics.TileHeight);
            }
        }
    }

    private void AddNastyOnlyCell(int xPos, int yPos)
        => _cells.Add(new CollisionTile(
            (int)CollisionObjectType.GameObject, xPos, yPos, CollisionTileType.NotBlob));

    private void AddScreenObjectCollisionTiles()
    {
        InteractiveItem? powerUp = _screens[ScreenNumber].PowerUp;
        if (powerUp != null)
        {
            AddObjectCollisionTiles(powerUp);
        }

        foreach (InteractiveItem pickUp in _screens[ScreenNumber].PickUps)
        {
            AddObjectCollisionTiles(pickUp);
        }

        InteractiveItem? cheopsPyramid = _screens[ScreenNumber].CheopsPyramid;
        if (cheopsPyramid != null)
        {
            AddObjectCollisionTiles(cheopsPyramid);
        }
    }

    private void AddObjectCollisionTiles(InteractiveItem item)
    {
        for (int y = 0; y < ItemCellsAcross; y++)
        {
            for (int x = 0; x < ItemCellsAcross; x++)
            {
                _cells.Add(new CollisionTile(
                    (int)CollisionObjectType.GameObject,
                    item.XLocation + x * ItemCellSize,
                    item.YLocation + y * ItemCellSize,
                    CollisionTileType.NotBlob));
            }
        }
    }
}

/// <summary>
///     The oracle's own copy of <c>IDynamicCollisionCells</c>, so a test can hand the same cells to
///     both maps without either interface being the other's.
/// </summary>
internal interface IReferenceDynamicCells
{
    IEnumerable<CollisionTile> CellsOnScreen(int screenNumber);
}
