// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Structure;

namespace Starquake.Animations;

/// <summary>Which tiles pulse, and how each one pulses.</summary>
/// <remarks>
///     A fresh set on every call, deliberately. <see cref="ColourFader" /> holds a pulse's running
///     state, so a shared static set would make the registered animations and the live ones the same
///     objects.
/// </remarks>
internal static class ColourAnimatedTiles
{

    /// <summary>
    ///     What <see cref="ColourFader.UpperTileToDraw" /> holds for a pulse with no second tile laid
    ///     over the first.
    /// </summary>
    private const int NoUpperTile = -1;

    /// <summary>
    ///     The green in the blue tag, which stops it reading as pure blue against dark scenery.
    /// </summary>
    private const int TagBlueGreen = 100;
    /// <summary>
    ///     Every tile that pulses, ready to register. Each booth fades towards a colour that says
    ///     what it is: blue for a teleport, green and red for the two halves of an access tunnel.
    ///     This is also the only place the three shuttle tags' colours are written down.
    /// </summary>
    public static IEnumerable<ColourFader> All()
        =>
        [
            new()
            {
                TileNo = (int)ActionTiles.TeleportBooth,
                UpperTileToDraw = 7,
                Intensity = 4,
                TransitionColour = new Color(0, 0, ColourLevels.Full),
                Speed = 8
            },
            new()
            {
                TileNo = (int)ActionTiles.AccessBoothLeft,
                UpperTileToDraw = 9,
                Intensity = 2,
                TransitionColour = new Color(0, ColourLevels.Full, 0),
                Speed = 32
            },
            new()
            {
                TileNo = (int)ActionTiles.AccessBoothRight,
                UpperTileToDraw = 10,
                Intensity = 2,
                TransitionColour = new Color(ColourLevels.Full, 0, 0),
                Speed = 32
            },

            // The tags draw no upper tile of their own. They pulse the tile already there and throw
            // a halo from each light on it. The positions are measured off the artwork: each light
            // is 28 by 36 at one end of a 128 by 96 cell.
            new()
            {
                TileNo = (int)ActionTiles.ShuttleTag1,
                UpperTileToDraw = NoUpperTile,
                HaloLights = [new Point(108, 76)],
                Intensity = 2,
                TransitionColour = new Color(ColourLevels.Full, ColourLevels.Full, 0),
                Speed = 16
            },
            new()
            {
                TileNo = (int)ActionTiles.ShuttleTag3,
                UpperTileToDraw = NoUpperTile,
                HaloLights = [new Point(16, 76), new Point(110, 76)],
                Intensity = 6,
                TransitionColour = new Color(ColourLevels.Full, 0, 0),
                Speed = 16
            },
            new()
            {
                TileNo = (int)ActionTiles.ShuttleTag2,
                UpperTileToDraw = NoUpperTile,
                HaloLights = [new Point(16, 76)],
                Intensity = 6,
                TransitionColour = new Color(0, TagBlueGreen, ColourLevels.Full),
                Speed = 16
            }
        ];
}
