// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Starquake.SharedLibs;

/// <summary>
///     How the screen data is serialised. Everything that reads or writes <c>SQScreenData.dat</c> -
///     the game, the editor and the tests - passes <see cref="Options" /> rather than letting
///     MessagePack fall back to its defaults, so all three agree by construction.
/// </summary>
/// <remarks>
///     The default would work. What it would also do is drag in <c>StandardResolver</c>, which ends
///     in the resolvers that build a formatter by emitting IL at runtime - the thing a NativeAOT
///     build has no way to do. Naming every formatter this file's types need is what keeps that
///     machinery out of reach: the resolver below knows the three generic instantiations the data
///     uses, defers to the source generator for the annotated types, and to MessagePack's built-ins
///     for <c>string</c> and <c>int?</c>. Nothing else is reachable, which is the point.
///     <para>
///         A type added to the screen data that is not one of those needs a line here. It says so
///         loudly - <c>GetFormatterWithVerify</c> throws naming the type it could not resolve - and
///         it says so on the first serialisation rather than at some later point.
///     </para>
/// </remarks>
public static class ScreenDataSerializer
{
    /// <summary>
    ///     The options every call site passes. Built from a resolver rather than from
    ///     <see cref="MessagePackSerializerOptions.Standard" />, because <c>Standard</c> is the
    ///     property that carries the dynamic resolvers in with it.
    /// </summary>
    public static MessagePackSerializerOptions Options { get; } = new(
        CompositeResolver.Create(
            [
                // The generic instantiations the data is made of. A resolver that worked these out
                // from the type at runtime is exactly what this file exists to avoid.
                new ArrayFormatter<GameScreenData>(),
                new TwoDimensionalArrayFormatter<ScreenTile>(),
                new ThreeDimensionalArrayFormatter<int>()
            ],
            [
                // GameScreenData, ScreenTile and CorePlacement, whose formatters the MessagePack
                // source generator wrote at compile time from their [MessagePackObject] attributes.
                GeneratedMessagePackResolver.Instance,

                // string and int?, which are the only two the generated formatters go on to ask for.
                BuiltinResolver.Instance
            ]));
}
