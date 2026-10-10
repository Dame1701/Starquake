// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Starquake.SharedLibs;

/// <summary>
///     How the screen data is serialised. The game, the editor and the tests all pass
///     <see cref="Options" /> when reading or writing <c>SQScreenData.dat</c>, so all three agree.
/// </summary>
/// <remarks>
///     MessagePack's defaults pull in <c>StandardResolver</c>, which builds formatters by emitting
///     IL at runtime. A NativeAOT build cannot do that, so <see cref="Options" /> names every
///     formatter the screen data needs instead.
///     <para>
///         Adding a type to the screen data means adding a line here. Miss it and the first
///         serialisation throws, naming the type it could not resolve.
///     </para>
/// </remarks>
public static class ScreenDataSerializer
{
    /// <summary>
    ///     The options every call site passes. Built from an explicit resolver, not from
    ///     <see cref="MessagePackSerializerOptions.Standard" />, which brings the dynamic resolvers
    ///     with it.
    /// </summary>
    public static MessagePackSerializerOptions Options { get; } = new(
        CompositeResolver.Create(
            [
                // The generic instantiations the data uses, named rather than worked out at runtime.
                new ArrayFormatter<GameScreenData>(),
                new TwoDimensionalArrayFormatter<ScreenTile>(),
                new ThreeDimensionalArrayFormatter<int>()
            ],
            [
                // GameScreenData, ScreenTile and CorePlacement, whose formatters the MessagePack
                // source generator writes at compile time from their [MessagePackObject] attributes.
                GeneratedMessagePackResolver.Instance,

                // string and int?, the only two the generated formatters go on to ask for.
                BuiltinResolver.Instance
            ]));
}
