// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

namespace Starquake.Animations;

/// <summary>
///     Everything outside itself one of Blob's parts has to ask about as it is thrown. Keeping
///     <see cref="BouncingParticle" /> to the stepping and bouncing rules is what lets them be
///     exercised without a screen, a content pipeline or a graphics device.
/// </summary>
internal interface IFragmentSurroundings
{
    int ScreenWidth { get; }

    int ScreenHeight { get; }

    /// <summary>
    ///     How far a box of <paramref name="width" /> by <paramref name="height" /> at
    ///     <paramref name="x" />, <paramref name="y" /> may travel in <paramref name="direction" />
    ///     before it hits scenery, up to <paramref name="distance" />.
    /// </summary>
    int DistanceClear(Direction direction, int x, int y, int width, int height, int distance);

    /// <summary>Throws the sparks a part strikes off whatever stopped it.</summary>
    void ThrowSparks(int x, int y);

    /// <summary>The sound of a part coming off scenery.</summary>
    void Clang();
}
