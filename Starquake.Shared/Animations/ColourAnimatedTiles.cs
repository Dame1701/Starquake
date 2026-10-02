// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.Animations;

/// <summary>
///     Which tiles pulse, and how each one pulses. Six rows of level design, with nothing in them that
///     is not data.
/// </summary>
/// <remarks>
///     A fresh set every call, deliberately. <see cref="ColourFaderAnimation" /> is where a pulse keeps
///     its running state - whether it is going, how far through its loop it is, whether it is stopping -
///     so a shared static set would have the registered animations and the live ones be the same objects.
/// </remarks>
internal static class ColourAnimatedTiles
{
    /// <summary>
    ///     Every tile that pulses, ready to register. The booths each fade towards a colour that says what
    ///     they are - blue for a teleport, green and red for the two halves of an access tunnel - and the
    ///     three shuttle tags are the yellow, red and blue ones, which is the only place their colour is
    ///     written down.
    /// </summary>
    public static IEnumerable<ColourFaderAnimation> All()
        =>
        [
            new ColourFaderAnimation
            {
                TileNo = (int)ActionTiles.TeleportBooth,
                UpperTileToDraw = 7,
                Intensity = 4,
                TransitionColour = new Color(0, 0, ColourLevels.Full),
                Speed = 8
            },
            new ColourFaderAnimation
            {
                TileNo = (int)ActionTiles.AccessBoothLeft,
                UpperTileToDraw = 9,
                Intensity = 2,
                TransitionColour = new Color(0, ColourLevels.Full, 0),
                Speed = 32
            },
            new ColourFaderAnimation
            {
                TileNo = (int)ActionTiles.AccessBoothRight,
                UpperTileToDraw = 10,
                Intensity = 2,
                TransitionColour = new Color(ColourLevels.Full, 0, 0),
                Speed = 32
            },

            // The tags draw no upper tile of their own - they pulse the tile that is already there.
            new ColourFaderAnimation
            {
                TileNo = (int)ActionTiles.ShuttleTag1,
                UpperTileToDraw = NoUpperTile,
                Intensity = 2,
                TransitionColour = new Color(ColourLevels.Full, ColourLevels.Full, 0),
                Speed = 16
            },
            new ColourFaderAnimation
            {
                TileNo = (int)ActionTiles.ShuttleTag3,
                UpperTileToDraw = NoUpperTile,
                Intensity = 6,
                TransitionColour = new Color(ColourLevels.Full, 0, 0),
                Speed = 16
            },
            new ColourFaderAnimation
            {
                TileNo = (int)ActionTiles.ShuttleTag2,
                UpperTileToDraw = NoUpperTile,
                Intensity = 6,
                TransitionColour = new Color(0, TagBlueGreen, ColourLevels.Full),
                Speed = 16
            }
        ];

    /// <summary>
    ///     What <see cref="ColourFaderAnimation.UpperTileToDraw" /> holds for a pulse that has no second
    ///     tile laid over the first.
    /// </summary>
    private const int NoUpperTile = -1;

    /// <summary>
    ///     The green in the blue tag, which keeps it from reading as pure blue against the dark scenery.
    /// </summary>
    private const int TagBlueGreen = 100;
}
