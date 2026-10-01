// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using ProjectMercury;
using ProjectMercury.Emitters;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     That a copied particle effect can still have its emitters found by name.
/// </summary>
/// <remarks>
///     Every effect the game uses is a deep copy - <c>ParticleEngine.CopyEffect</c> makes one per
///     instance so that two callers sharing an effect name do not emit into the same particle pool -
///     and <see cref="Emitter.CopyBaseFields" /> used to rename each emitter to "Copy of" whatever it
///     had been called. Nothing could then be looked up by the name its content file gave it, which
///     crashed the game on the first screen with a key-locked door on it. These are cheap and they
///     cover a crash that only showed up in the one place the force field is drawn.
/// </remarks>
public class EmitterCopyTests
{
    [Fact]
    public void DeepCopy_AnEmitter_KeepsTheNameItWasGiven()
    {
        // Arrange
        Emitter emitter = TestData.NamedEmitter("Emitter01");

        // Act
        Emitter copy = emitter.DeepCopy();

        // Assert
        Assert.Equal("Emitter01", copy.Name);
    }

    /// <summary>
    ///     The crash itself: the force field asks its effect for "Emitter01" and "Emitter05", which are
    ///     the names <c>ForceField.xml</c> gives them.
    /// </summary>
    [Fact]
    public void DeepCopy_AnEffect_StillFindsItsEmittersByTheirContentFileNames()
    {
        // Arrange
        var effect = new ParticleEffect
        {
            TestData.NamedEmitter("Emitter01"),
            TestData.NamedEmitter("Emitter05")
        };

        // Act
        ParticleEffect copy = effect.DeepCopy();

        // Assert
        Assert.Equal("Emitter01", copy["Emitter01"].Name);
        Assert.Equal("Emitter05", copy["Emitter05"].Name);
    }

    /// <summary>
    ///     A copy of a copy, because an effect instance is copied from one that was itself loaded and
    ///     may be copied again. Repeated prefixing was what made the old behaviour impossible to work
    ///     around by writing the prefix out by hand.
    /// </summary>
    [Fact]
    public void DeepCopy_TwiceOver_StillLeavesTheNameAlone()
    {
        // Arrange
        Emitter emitter = TestData.NamedEmitter("Left Splat");

        // Act
        Emitter copy = emitter.DeepCopy().DeepCopy();

        // Assert
        Assert.Equal("Left Splat", copy.Name);
    }

    private static class TestData
    {
        /// <summary>
        ///     An emitter the engine will agree to copy. Every guarded property has to be set, not just
        ///     the name: a budget and a release quantity below one are both refused, and so is a term at
        ///     or below zero, so an emitter without them cannot be deep copied at all. One loaded from a
        ///     content file always has all of them.
        /// </summary>
        public static Emitter NamedEmitter(string name)
            => new()
            {
                Name = name,
                Budget = 10,
                Term = 1f,
                ReleaseQuantity = 1
            };
    }
}
