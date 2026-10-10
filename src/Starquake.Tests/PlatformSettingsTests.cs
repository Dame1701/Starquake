// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     How far through coming apart a jump platform is. The burning edges can only be judged by
///     looking at them. This is the one number driving them.
/// </summary>
public class PlatformSettingsTests
{
    /// <summary>The point in a platform's life the dissolve begins, as JumpPlatform sets it.</summary>
    private const int DissolveStartsAt = 50;

    /// <summary>
    ///     It stands whole for the first half of its life. A platform that glowed before it began
    ///     to go would be lying about its state.
    /// </summary>
    [Theory]
    [InlineData(100)]
    [InlineData(70)]
    [InlineData(DissolveStartsAt)]
    public void DecayFrom_BeforeTheDissolveBegins_IsNothingAtAll(int dissolveTime)
        => Assert.Equal(0f, PlatformSettings.DecayFrom(dissolveTime, DissolveStartsAt));

    [Fact]
    public void DecayFrom_AcrossTheDissolve_RunsFromWholeToGone()
    {
        // Assert
        Assert.Equal(0f, PlatformSettings.DecayFrom(DissolveStartsAt, DissolveStartsAt));
        Assert.Equal(0.5f, PlatformSettings.DecayFrom(DissolveStartsAt / 2, DissolveStartsAt), 2);
        Assert.Equal(1f, PlatformSettings.DecayFrom(0, DissolveStartsAt));
    }

    [Fact]
    public void DecayFrom_AsTheDissolveRuns_OnlyEverGrows()
    {
        // Act / Assert
        for (int dissolveTime = DissolveStartsAt; dissolveTime > 0; dissolveTime--)
        {
            Assert.True(
                PlatformSettings.DecayFrom(dissolveTime - 1, DissolveStartsAt)
                >= PlatformSettings.DecayFrom(dissolveTime, DissolveStartsAt),
                $"the platform came back together at {dissolveTime}");
        }
    }

    /// <summary>
    ///     The countdown is not stopped the instant it reaches zero, so the decay has to hold at
    ///     gone. Running past it would light the slab brighter than it is ever meant to go.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-200)]
    public void DecayFrom_PastTheEnd_IsHeldAtGone(int dissolveTime)
        => Assert.Equal(1f, PlatformSettings.DecayFrom(dissolveTime, DissolveStartsAt));
}
