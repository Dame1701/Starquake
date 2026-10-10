// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Scoring;
using Starquake.Sounds;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Every scoring and power-up rule in the game. <see cref="PlayerStats" /> holds them without
///     the HUD that draws them, so none of this needs a graphics device.
/// </summary>
/// <remarks>
///     The sound engine is real, and deliberately so. It is handed a <c>ContentManager</c> with no
///     content, and a sound it has never been given is a no-op that returns <c>Guid.Empty</c>. That
///     is cheaper than an interface introduced only to be faked.
/// </remarks>
public class PlayerStatsTests
{
    [Fact]
    public void Reset_StartsAGame_FullOfEverything()
    {
        // Arrange
        PlayerStats stats = NewStats();

        // Act
        stats.Reset();

        // Assert
        Assert.Equal(TestData.StartingLives, stats.BlobLives);
        Assert.Equal(0, stats.PlayerScore);
        Assert.Equal(TestData.MaxEnergy, stats.EnergyLevel);
        Assert.Equal(TestData.MaxFirePower, stats.FirePower);
        Assert.Equal(TestData.StartingPlatforms, stats.Platforms);
    }

    // Internal rather than public, here and in every theory below that takes a game type. The
    // game's types are internal and this project only sees them through InternalsVisibleTo, which
    // is not enough to let a public signature name one. It costs nothing: xunit requires the test
    // class to be public, not its test methods.
    [Theory]
    [InlineData(PowerUp.EnergySmall, TestData.SmallTopUp)]
    [InlineData(PowerUp.EnergyMedium, TestData.MediumTopUp)]
    [InlineData(PowerUp.EnergyFull, TestData.FullTopUp)]
    internal void GotPowerUp_AnEnergyPowerUp_AddsItsOwnAmount(PowerUp powerUp, int expectedTopUp)
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.EnergyLevel = 0;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)powerUp);

        // Assert
        Assert.Equal(expectedTopUp, stats.EnergyLevel);
    }

    [Fact]
    public void GotPowerUp_ASupplyAlreadyNearlyFull_IsCappedRatherThanOverfilled()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.EnergyLevel = TestData.MaxEnergy - 1;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)PowerUp.EnergyFull);

        // Assert
        Assert.Equal(TestData.MaxEnergy, stats.EnergyLevel);
    }

    [Fact]
    public void GotPowerUp_AnExtraLife_AddsOne()
    {
        // Arrange
        PlayerStats stats = NewStats();
        int livesBefore = stats.BlobLives;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)PowerUp.ExtraLife);

        // Assert
        Assert.Equal(livesBefore + 1, stats.BlobLives);
    }

    /// <summary>
    ///     The wildcard's three branches. Platforms are weighed at twice their count, so the supply
    ///     that looks lowest by number is not always the one that wins.
    /// </summary>
    [Fact]
    public void GotPowerUp_TheWildcard_TopsUpEnergyWhenEnergyIsLowest()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.EnergyLevel = 1;
        stats.FirePower = TestData.MaxFirePower;
        stats.Platforms = TestData.MaxPlatforms;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)PowerUp.WhateverIsLowest);

        // Assert
        Assert.Equal(1 + TestData.LargeTopUp, stats.EnergyLevel);
    }

    [Fact]
    public void GotPowerUp_TheWildcard_TopsUpFirePowerWhenFirePowerIsLowest()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.EnergyLevel = TestData.MaxEnergy;
        stats.FirePower = 1;
        stats.Platforms = TestData.MaxPlatforms;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)PowerUp.WhateverIsLowest);

        // Assert
        Assert.Equal(1 + TestData.MediumTopUp, stats.FirePower);
    }

    [Fact]
    public void GotPowerUp_TheWildcard_WeighsAPlatformAtTwiceItsCount()
    {
        // Arrange - fewer platforms than fire power by count, but not once doubled
        PlayerStats stats = NewStats();
        stats.EnergyLevel = TestData.MaxEnergy;
        stats.FirePower = 20;
        stats.Platforms = 15;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)PowerUp.WhateverIsLowest);

        // Assert - 15 platforms are worth 30, so fire power at 20 is what is short
        Assert.Equal(20 + TestData.MediumTopUp, stats.FirePower);
        Assert.Equal(15, stats.Platforms);
    }

    [Fact]
    public void GotPowerUp_TheWildcardWithNoLivesLeft_GivesALifeRatherThanASupply()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.BlobLives = 0;
        stats.EnergyLevel = 1;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.GotPowerUp, (int)PowerUp.WhateverIsLowest);

        // Assert
        Assert.Equal(1, stats.BlobLives);
        Assert.Equal(1, stats.EnergyLevel);
    }

    [Fact]
    public void FiredLazer_SpendsOneShot()
    {
        // Arrange
        PlayerStats stats = NewStats();
        double firePowerBefore = stats.FirePower;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.FiredLazer, null);

        // Assert
        Assert.Equal(firePowerBefore - 1, stats.FirePower);
    }

    [Fact]
    public void FiredLazer_WithNothingLeft_DoesNotGoNegative()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.FirePower = 0;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.FiredLazer, null);

        // Assert
        Assert.Equal(0, stats.FirePower);
    }

    [Fact]
    public void LaidPlatform_SpendsOnePlatform()
    {
        // Arrange
        PlayerStats stats = NewStats();
        double platformsBefore = stats.Platforms;

        // Act
        stats.BlobHasPerformedAnAction(BlobAction.LaidPlatform, null);

        // Assert
        Assert.Equal(platformsBefore - 1, stats.Platforms);
    }

    [Fact]
    public void DrainEnergy_SomewhereSafe_DrainsNothingHoweverLongHeStandsThere()
    {
        // Arrange
        PlayerStats stats = NewStats();

        // Act
        for (int frame = 0; frame < TestData.EnergyDrainFrames * 3; frame++)
        {
            stats.DrainEnergy(false);
        }

        // Assert
        Assert.Equal(TestData.MaxEnergy, stats.EnergyLevel);
    }

    [Fact]
    public void DrainEnergy_OnThePlanet_LosesABarEveryFourSeconds()
    {
        // Arrange
        PlayerStats stats = NewStats();

        // Act - two full drain periods, plus the frame each one starts on
        for (int frame = 0; frame < 2 * (TestData.EnergyDrainFrames + 2); frame++)
        {
            stats.DrainEnergy(true);
        }

        // Assert
        Assert.Equal(TestData.MaxEnergy - 2, stats.EnergyLevel);
    }

    [Fact]
    public void DrainEnergy_PartwayThroughAPeriod_HasTakenNothingYet()
    {
        // Arrange
        PlayerStats stats = NewStats();

        // Act
        for (int frame = 0; frame < TestData.EnergyDrainFrames; frame++)
        {
            bool ranOut = stats.DrainEnergy(true);

            // Assert
            Assert.False(ranOut);
        }

        Assert.Equal(TestData.MaxEnergy, stats.EnergyLevel);
    }

    [Fact]
    public void DrainEnergy_TheDrainTakingTheLastBar_SaysTheBlobShouldDie()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.EnergyLevel = 1;

        // Act
        bool ranOut = false;
        for (int frame = 0; frame < TestData.EnergyDrainFrames + 2 && !ranOut; frame++)
        {
            ranOut = stats.DrainEnergy(true);
        }

        // Assert
        Assert.True(ranOut);
    }

    [Fact]
    public void TakeALife_WithLivesToSpare_RevivesHimOnFullEnergy()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.EnergyLevel = 0;

        // Act
        bool gameOver = stats.TakeALife();

        // Assert
        Assert.False(gameOver);
        Assert.Equal(TestData.StartingLives - 1, stats.BlobLives);
        Assert.Equal(TestData.MaxEnergy, stats.EnergyLevel);
    }

    [Fact]
    public void TakeALife_HavingRunOutOfPlatforms_GivesEnoughBackToClimbOut()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.Platforms = 0;

        // Act
        stats.TakeALife();

        // Assert
        Assert.Equal(TestData.PlatformsOnRevival, stats.Platforms);
    }

    [Fact]
    public void TakeALife_HavingPlentyOfPlatforms_LeavesThemAlone()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.Platforms = TestData.MaxPlatforms;

        // Act
        stats.TakeALife();

        // Assert
        Assert.Equal(TestData.MaxPlatforms, stats.Platforms);
    }

    [Fact]
    public void TakeALife_TheLastOne_IsGameOverAndLeavesNoNegativeLives()
    {
        // Arrange
        PlayerStats stats = NewStats();
        stats.BlobLives = 0;

        // Act
        bool gameOver = stats.TakeALife();

        // Assert
        Assert.True(gameOver);
        Assert.Equal(0, stats.BlobLives);
    }

    /// <summary>
    ///     A sound engine with no content in it. Every <c>PlaySound</c> finds nothing registered under
    ///     the name and returns <c>Guid.Empty</c> without touching an audio device.
    /// </summary>
    private static PlayerStats NewStats()
        => new(new SoundEffectEngine(new ContentManager(new GameServiceContainer())));

    private static class TestData
    {
        public const int StartingLives = 4;

        public const int StartingPlatforms = 32;

        public const int MaxEnergy = 128;

        public const int MaxFirePower = 128;

        public const int MaxPlatforms = 64;

        public const int SmallTopUp = 32;

        public const int MediumTopUp = 64;

        public const int LargeTopUp = 96;

        public const int FullTopUp = 128;

        public const int PlatformsOnRevival = 8;

        /// <summary>Kept in step with <see cref="PlayerStats" />' own constant.</summary>
        public const int EnergyDrainFrames = 4 * LogicClock.FramesPerSecond;
    }
}
