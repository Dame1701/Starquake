// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     Everything outside itself that a shot in flight has to ask about. <see cref="BlobFire" />
///     is the stepping and boundary rules and nothing else, which is what lets them be exercised
///     without a screen, a content pipeline or a graphics device.
/// </summary>
internal interface IFireSurroundings
{
    int ScreenWidth { get; }

    int ScreenHeight { get; }

    /// <summary>
    ///     How far a box of <paramref name="width" /> by <paramref name="height" /> at
    ///     <paramref name="x" />, <paramref name="y" /> may travel in <paramref name="direction" />
    ///     before it hits scenery, up to <paramref name="distance" />.
    /// </summary>
    int DistanceClear(Direction direction, int x, int y, int width, int height, int distance);

    /// <summary>
    ///     Which way a shot that has just hit scenery goes. Exactly one of these per deflection:
    ///     the sequence it draws from is shared with the rest of the game.
    /// </summary>
    FireDeflection ChooseDeflection();

    /// <summary>
    ///     Throws the impact sparks. Called where the shot came apart, not where it started.
    /// </summary>
    void ThrowSparks(int x, int y);
}
