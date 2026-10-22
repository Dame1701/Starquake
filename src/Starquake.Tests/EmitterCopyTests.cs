// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using ProjectMercury;
using ProjectMercury.Emitters;
using Xunit;

namespace Starquake.Tests;

/// <summary>That a copied particle effect can still have its emitters found by name.</summary>
/// <remarks>
///     Every effect the game uses is a deep copy. <c>ParticleEngine.CopyEffect</c> makes one per
///     instance, so that two callers sharing an effect name do not emit into the same particle pool.
///     <para>
///         <see cref="Emitter.CopyBaseFields" /> must leave the emitter's name alone. Emitters are
///         looked up by the name their content file gave them, so a copy that renames them crashes
///         the first screen with a key-locked door on it.
///     </para>
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
    ///     The force field asks its effect for "Emitter01" and "Emitter05", which are the names
    ///     <c>ForceField.xml</c> gives them.
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
    ///     A copy of a copy, because an effect instance is copied from one that was itself loaded
    ///     and may be copied again. A name altered on each pass could not be predicted at all.
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
        ///     An emitter the engine will agree to copy. Every guarded property has to be set, not
        ///     just the name. A budget or a release quantity below one is refused, and so is a term
        ///     at or below zero, so an emitter missing any of them cannot be deep copied at all. One
        ///     loaded from a content file always has them.
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
