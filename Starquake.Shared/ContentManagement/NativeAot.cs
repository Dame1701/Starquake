// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Content;
using ProjectMercury;
using ProjectMercury.Emitters;
using ProjectMercury.Modifiers;

namespace Starquake.ContentManagement;

/// <summary>
///     What a NativeAOT build has to be told at compile time because there is no JIT at runtime to
///     work it out. Everything here is a no-op on a normal build: the readers registered below are
///     the same ones reflection would have found, so the game takes one code path either way.
/// </summary>
/// <remarks>
///     MonoGame reads the name of each asset's reader out of the <c>.xnb</c> and asks for it back. A
///     JIT compiles whatever comes back; ILC can only emit code it saw at build time, so a generic the
///     compiler never meets in source - <c>ReflectiveReader&lt;VariableFloat&gt;</c>, say - simply does
///     not exist in the binary, and the load fails with "missing native code or metadata".
///     <para>
///         The screen data has the same problem and answers it elsewhere: the formatters MessagePack
///         would otherwise work out at runtime are named in <c>ScreenDataSerializer</c>, next to the
///         types they are for.
///     </para>
///     <para>
///         Naming every one of them here is what puts them in. The two heads' .csproj files also root
///         <c>MonoGame.Framework</c> and <c>ProjectMercury</c> against trimming - see
///         <c>Starquake.Shared.props</c> - because <see cref="ReflectiveReader{T}" /> reads an
///         emitter's properties reflectively and the trimmer cannot see that coming either.
///     </para>
/// </remarks>
internal static class NativeAot
{
    /// <summary>
    ///     Call before any content is loaded. <see cref="GameHost" />'s constructor is the place; the
    ///     first <c>Content.Load</c> happens in <c>GameEngine</c>'s, one step later.
    /// </summary>
    internal static void Register() => RegisterParticleEffectReaders();

    /// <summary>
    ///     The particle effects, and only those. Every other asset the game loads - textures, fonts,
    ///     sounds, shaders - is read by a non-generic reader that MonoGame itself names in source, so
    ///     ILC emits those without being asked.
    /// </summary>
    /// <remarks>
    ///     Mercury's effects are compiled by <c>IntermediateSerializer</c> rather than by a content
    ///     writer of their own, so each <c>.xnb</c> asks for a <see cref="ReflectiveReader{T}" /> over
    ///     one of its types. That is the whole list: a type missing from it throws on the effect that
    ///     wants it, naming itself in the message.
    /// </remarks>
    private static void RegisterParticleEffectReaders()
    {
        Reflective<ParticleEffect>();
        Reflective<EmitterCollection>();
        Reflective<Emitter>();
        Reflective<CircleEmitter>();
        Reflective<ConeEmitter>();
        Reflective<LineEmitter>();
        Reflective<RectEmitter>();
        Reflective<VariableFloat>();
        Reflective<VariableFloat3>();
        Reflective<ModifierCollection>();
        Reflective<Modifier>();
        Reflective<ColourInterpolatorModifier>();
        Reflective<ColourModifier>();
        Reflective<DampingModifier>();
        Reflective<LinearGravityModifier>();
        Reflective<OpacityFastFadeModifier>();
        Reflective<OpacityInterpolatorModifier>();
        Reflective<OpacityModifier>();
        Reflective<OpacityOscillator>();
        Reflective<RotationModifier>();
        Reflective<RotationRateModifier>();
        Reflective<ScaleInterpolatorModifier>();
        Reflective<ScaleModifier>();
        Reflective<ScaleOscillator>();
        Reflective<VelocityClampModifier>();

        // The three MonoGame generics an effect reaches for over a Mercury type. The enum one is
        // needed for the same reason as the reflective readers - EmitterBlendMode is a value type,
        // so there is no shared implementation to fall back on - and the two lists are named here
        // alongside it rather than left to chance.
        Register($"Microsoft.Xna.Framework.Content.EnumReader`1[[{typeof(EmitterBlendMode).AssemblyQualifiedName}]]",
            static () => new EnumReader<EmitterBlendMode>());
        Register($"Microsoft.Xna.Framework.Content.ListReader`1[[{typeof(Emitter).AssemblyQualifiedName}]]",
            static () => new ListReader<Emitter>());
        Register($"Microsoft.Xna.Framework.Content.ListReader`1[[{typeof(Modifier).AssemblyQualifiedName}]]",
            static () => new ListReader<Modifier>());
    }

    /// <summary>
    ///     One reflectively-read type. The key is the string the content pipeline wrote into the
    ///     <c>.xnb</c>, which is the reader's name around the asset type's assembly-qualified one -
    ///     taken from the type here rather than spelled out, so a version bump cannot silently stop
    ///     matching.
    /// </summary>
    private static void Reflective<T>() =>
        Register($"Microsoft.Xna.Framework.Content.ReflectiveReader`1[[{typeof(T).AssemblyQualifiedName}]]",
            static () => new ReflectiveReader<T>());

    private static void Register(string readerTypeName, Func<ContentTypeReader> creator) =>
        ContentTypeReaderManager.AddTypeCreator(readerTypeName, creator);
}
