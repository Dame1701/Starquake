// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites.NastyBehaviour;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;
using Starquake.SharedLibs;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     How a nasty decides where to go. Fourteen behaviours that were 630 lines of a class which
///     also draws itself, so none of this could be reached without a graphics device.
/// </summary>
/// <remarks>
///     The payoff this split was for is <see cref="Step_AChasingNastyWalledIn_GivesUpChasing" />:
///     "nasties get stuck" is a thing that happens, which is why <c>ReleaseAttractionIfStuck</c> and
///     the stuck counter exist at all, and it is now reproducible in a test rather than only in a
///     game.
/// </remarks>
public class NastyBrainTests
{
    [Fact]
    public void Step_InOpenSpace_MovesTheNasty()
    {
        // Arrange
        NastyBrain brain = BrainOn(TestData.OpenScreen(), NastyType.Bird);
        brain.XPos = TestData.Middle;
        brain.YPos = TestData.Middle;

        // Act
        for (int frame = 0; frame < 60; frame++)
        {
            brain.Step(TestData.BlobX, TestData.BlobY);
        }

        // Assert
        Assert.True(brain.XPos != TestData.Middle || brain.YPos != TestData.Middle);
    }

    /// <summary>
    ///     A nasty attracted to Blob but unable to reach him would otherwise press against the
    ///     scenery for ever. After a quarter of a second of not moving it stops chasing, so that its
    ///     next frame of mind can take it somewhere else.
    /// </summary>
    [Fact]
    public void Step_AChasingNastyWalledIn_GivesUpChasing()
    {
        // Arrange - a nasty boxed in by solid tiles on every side, told Blob is beyond them
        GameScreenData screen = TestData.BoxedInScreen();
        NastyBrain brain = BrainOn(screen, NastyType.Bird);
        brain.XPos = TestData.BoxedInX;
        brain.YPos = TestData.BoxedInY;

        // Chase, so that being unable to move is something worth noticing
        brain.State = brain.State with
        {
            AttractedToBlob = true,
            MovementChangeCount = int.MaxValue,
            MovementChangeCounter = 0
        };

        // Act - long enough for the stuck counter to run out twice over
        for (int frame = 0; frame < 40; frame++)
        {
            brain.Step(TestData.BlobX, TestData.BlobY);
        }

        // Assert
        Assert.False(brain.State.AttractedToBlob);
    }

    [Fact]
    public void Step_ANastyWalledInButNotChasing_IsLeftAlone()
    {
        // Arrange
        GameScreenData screen = TestData.BoxedInScreen();
        NastyBrain brain = BrainOn(screen, NastyType.Bird);
        brain.XPos = TestData.BoxedInX;
        brain.YPos = TestData.BoxedInY;

        brain.State = brain.State with
        {
            AttractedToBlob = false,
            MovementChangeCount = int.MaxValue,
            MovementChangeCounter = 0
        };

        // Act
        for (int frame = 0; frame < 40; frame++)
        {
            brain.Step(TestData.BlobX, TestData.BlobY);
        }

        // Assert - it was not chasing, so there was nothing to release
        Assert.False(brain.State.AttractedToBlob);
        Assert.Equal(TestData.BoxedInX, brain.XPos);
        Assert.Equal(TestData.BoxedInY, brain.YPos);
    }

    [Fact]
    public void Step_ASpikyNastyWalledIn_KeepsChasingBecauseThatIsWhatSpikyNastiesDo()
    {
        // Arrange
        GameScreenData screen = TestData.BoxedInScreen();
        NastyBrain brain = BrainOn(screen, NastyType.Spiky);
        brain.XPos = TestData.BoxedInX;
        brain.YPos = TestData.BoxedInY;

        brain.State = brain.State with
        {
            AttractedToBlob = true,
            MovementChangeCount = int.MaxValue,
            MovementChangeCounter = 0
        };

        // Act
        for (int frame = 0; frame < 40; frame++)
        {
            brain.Step(TestData.BlobX, TestData.BlobY);
        }

        // Assert
        Assert.True(brain.State.AttractedToBlob);
    }

    [Fact]
    public void Step_TheCoreGuardian_BouncesDiagonallyAndNeverChases()
    {
        // Arrange
        NastyBrain brain = BrainOn(TestData.OpenScreen(), NastyType.CoreGuardian);
        brain.XPos = TestData.Middle;
        brain.YPos = TestData.Middle;

        // Act
        brain.Step(TestData.BlobX, TestData.BlobY);

        // Assert
        Assert.False(brain.State.AttractedToBlob);
        Assert.Equal(MoveType.DiagonalBouncing, brain.State.MovementType);
        Assert.Contains(
            brain.Direction,
            new[]
            {
                Direction.UpLeft,
                Direction.UpRight,
                Direction.DownLeft,
                Direction.DownRight
            });
    }

    [Fact]
    public void BecomeA_TheCoreGuardian_IsSetToBounce()
    {
        // Arrange
        NastyBrain brain = BrainOn(TestData.OpenScreen(), NastyType.Bird);

        // Act
        brain.BecomeA(NastyType.CoreGuardian);

        // Assert
        Assert.Equal(MoveType.DiagonalBouncing, brain.State.MovementType);
    }

    [Fact]
    public void BecomeA_ASpikyNasty_IsSetToLunge()
    {
        // Arrange
        NastyBrain brain = BrainOn(TestData.OpenScreen(), NastyType.Bird);

        // Act
        brain.BecomeA(NastyType.Spiky);

        // Assert
        Assert.Equal(MoveType.SpikeyMovement, brain.State.MovementType);
    }

    [Fact]
    public void BecomeA_AnyOtherNasty_NeverDrawsTheSpikyLunge()
    {
        // Arrange and act - every draw, many times over
        var brain = new NastyBrain(MapFor(TestData.OpenScreen()), new Random(9), TestData.MoveSpeed);

        // Assert
        for (int attempt = 0; attempt < 200; attempt++)
        {
            brain.BecomeA(NastyType.Bird);

            Assert.NotEqual(MoveType.SpikeyMovement, brain.State.MovementType);
        }
    }

    [Fact]
    public void PrepareForConception_ASpikyNasty_StartsPaused()
    {
        // Arrange
        NastyBrain brain = BrainOn(TestData.OpenScreen(), NastyType.Spiky);
        brain.XPos = TestData.Middle;
        brain.YPos = TestData.Middle;

        // Act
        brain.PrepareForConception();

        // Assert - it is set to lunge, and the first thing it does is stand still
        Assert.Equal(MoveType.SpikeyMovement, brain.State.MovementType);
    }

    /// <summary>
    ///     The core guardian is the one nasty whose direction nothing else ever sets. It takes its
    ///     own branch of <c>MoveNasty</c>, skipping the counter branch that hands every other
    ///     movement type a <see cref="Direction.None" /> to choose from, and <c>DiagonalBouncing</c>
    ///     picks a diagonal only when the direction is already None. Left at the enum's default of
    ///     Up it would set off upwards and never turn - <c>Reflected</c> has no case for a
    ///     non-diagonal, so nothing it hit would send it back either.
    /// </summary>
    [Fact]
    public void Step_ACoreGuardianBuiltTheWayTheCoreBuildsOne_FacesADiagonal()
    {
        // Arrange - BecomeA and nothing else, which is all PlanetCoreScreen does to one
        var brain = new NastyBrain(MapFor(TestData.OpenScreen()), new Random(4), TestData.MoveSpeed);
        brain.BecomeA(NastyType.CoreGuardian);
        brain.XPos = TestData.Middle;
        brain.YPos = TestData.Middle;

        // Act
        brain.Step(TestData.BlobX, TestData.BlobY);

        // Assert
        Assert.Contains(
            brain.Direction,
            new[]
            {
                Direction.UpLeft,
                Direction.UpRight,
                Direction.DownLeft,
                Direction.DownRight
            });
    }

    [Fact]
    public void Step_ANastyRunningIntoAWall_NotesWhichWayItWasStopped()
    {
        // Arrange - open floor with a wall in the next column
        GameScreenData screen = TestData.OpenScreen();
        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            screen.ScreenTiles[4, y] = new ScreenTile(TestData.SolidTile);
        }

        NastyBrain brain = BrainOn(screen, NastyType.Bird);
        brain.XPos = 4 * TileMetrics.TileWidth - SpriteMetrics.SpriteWidth;
        brain.YPos = TileMetrics.TileHeight;
        brain.Direction = Direction.Right;
        brain.State = brain.State with
        {
            MovementChangeCount = int.MaxValue,
            ConstantMovement = true,
            StepsToMove = int.MaxValue
        };

        // Act
        brain.Step(TestData.BlobX, TestData.BlobY);

        // Assert
        Assert.Equal(Direction.Right, brain.State.LastCollisionDirection);
    }

    /// <summary>
    ///     What a screen swap does: the spare set of nasties takes over the live set's state. The
    ///     record struct is what makes "all of it" mean all of it.
    /// </summary>
    [Fact]
    public void State_CopiedToAnotherNasty_CarriesEveryFieldOfIt()
    {
        // Arrange
        NastyBrain source = BrainOn(TestData.OpenScreen(), NastyType.Bird);
        NastyBrain destination = BrainOn(TestData.OpenScreen(), NastyType.Bird);

        var wanted = new NastyMovementState
        {
            MovementType = MoveType.DiagonalBouncingInSteps,
            AttractedToBlob = true,
            Direction = Direction.UpLeft,
            StepsMovedCounter = 3,
            StepsToMove = 17,
            ConstantMovement = true,
            MoveSpeed = 6,
            LastCollisionDirection = Direction.Down,
            MovementChangeCounter = 11,
            MovementChangeCount = 250
        };
        source.State = wanted;

        // Act
        destination.State = source.State;

        // Assert
        Assert.Equal(wanted, destination.State);
    }

    private static NastyBrain BrainOn(GameScreenData screen, NastyType nastyType)
    {
        var brain = new NastyBrain(MapFor(screen), new Random(4), TestData.MoveSpeed);
        brain.BecomeA(nastyType);

        return brain;
    }

    private static ScreenCollisionMap MapFor(GameScreenData screen)
    {
        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        return map;
    }

    private static class TestData
    {
        public const int SolidTile = 1;

        /// <summary>
        ///     A tile solid in every collision cell but the one at (1, 1), which is left open.
        /// </summary>
        public const int PocketTile = 4;

        public const int MoveSpeed = 4;

        /// <summary>Somewhere in the middle of an empty screen, clear of every edge.</summary>
        public const int Middle = 300;

        /// <summary>Where Blob is told to be - far away, so a chasing nasty has somewhere to go.</summary>
        public const int BlobX = 900;

        /// <inheritdoc cref="BlobX" />
        public const int BlobY = 500;

        /// <summary>
        ///     The one free collision cell of <see cref="BoxedInScreen" />, which is exactly a nasty
        ///     wide and a nasty high. A tile is four cells by three, so leaving a whole tile free
        ///     would still leave room to shuffle about inside it.
        /// </summary>
        public const int BoxedInX = TileMetrics.TileWidth + TileMetrics.CollisionCellWidth;

        /// <inheritdoc cref="BoxedInX" />
        public const int BoxedInY = TileMetrics.TileHeight + TileMetrics.CollisionCellHeight;

        /// <summary>A screen of nothing at all, so a nasty can go wherever it likes.</summary>
        public static GameScreenData OpenScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        /// <summary>
        ///     A screen of tiles that are solid in every cell but one, so the free cells form a grid
        ///     of nasty-sized pockets with no way between them. A nasty in one cannot move at all,
        ///     which is the condition the stuck counter exists for.
        /// </summary>
        public static GameScreenData BoxedInScreen()
        {
            var screen = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);

            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    screen.ScreenTiles[x, y] = new ScreenTile(PocketTile);
                }
            }

            return screen;
        }

        public static int[,,] TileCollisionData()
        {
            int[,,] data = new int[
                512,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];

            for (int across = 0; across < TileMetrics.CollisionCellsAcrossTile; across++)
            {
                for (int down = 0; down < TileMetrics.CollisionCellsDownTile; down++)
                {
                    data[SolidTile, across, down] = 1;
                    data[PocketTile, across, down] = across == 1 && down == 1 ? 0 : 1;
                }
            }

            return data;
        }
    }
}
