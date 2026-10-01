// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using ProjectMercury;
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.Helpers;

internal static class NastyHelpers
{

    private static readonly Color White =
        new(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full);

    /// <summary>
    ///     What each kind of nasty splatters. Half of them throw white splodges and half throw a colour
    ///     of their own, which is the only pattern in it - the rest is a painter's choice per nasty, and
    ///     so it is a table rather than a rule.
    /// </summary>
    /// <remarks>
    ///     Scorpian and Helicopter share a set. That is how it was written, and whether it is deliberate
    ///     or a copy-and-paste is not something this refactor can tell.
    /// </remarks>
    private static readonly Dictionary<NastyType, SplatColours> SplatColoursByNasty = new()
    {
        [NastyType.FriedEgg] = new SplatColours(new Color(125, 62, 62), new Color(0, 182, 0), White),
        [NastyType.Gonzo] = new SplatColours(new Color(4, 82, 184), new Color(182, 103, 10), White),
        [NastyType.Cushion] = new SplatColours(new Color(59, 194, 42), new Color(227, 66, 19), White),
        [NastyType.Helmet] = new SplatColours(new Color(202, 109, 17), new Color(137, 137, 137), White),
        [NastyType.Shellfish] = new SplatColours(new Color(239, 238, 0), new Color(198, 99, 0), White),
        [NastyType.Snail] = new SplatColours(new Color(0, 124, 0), new Color(141, 141, 141), new Color(166, 198, 0)),
        [NastyType.CurleyWurley] = new SplatColours(new Color(168, 163, 252), new Color(109, 109, 253), White),
        [NastyType.Bird] = new SplatColours(new Color(0, 0, 211), new Color(148, 148, 217), new Color(188, 62, 0)),
        [NastyType.Ghost] = new SplatColours(new Color(0, 151, 43), new Color(88, 0, 88), White),
        [NastyType.Scorpian] = new SplatColours(new Color(177, 8, 8), new Color(130, 2, 2), new Color(230, 230, 230)),
        [NastyType.DragonFly] = new SplatColours(new Color(8, 150, 219), new Color(50, 111, 85), new Color(76, 191, 142)),
        [NastyType.Helicopter] = new SplatColours(new Color(177, 8, 8), new Color(130, 2, 2), new Color(230, 230, 230)),
        [NastyType.Hippo] = new SplatColours(new Color(229, 120, 252), new Color(137, 115, 251), new Color(145, 49, 50)),
        [NastyType.Centipede] = new SplatColours(new Color(87, 165, 28), new Color(110, 6, 124), new Color(221, 134, 38))
    };

    public static void ApplyNastyColours(NastyType nastyType, ParticleEffect effect)
    {
        if (!SplatColoursByNasty.TryGetValue(nastyType, out SplatColours colours))
        {
            return;
        }

        // The names the effect's content file gives these, now that a deep copy keeps them rather
        // than prefixing every one with "Copy of".
        effect["Left Splat"].ReleaseColour.Value = colours.LeftSplat.ToVector3();
        effect["Right Splat"].ReleaseColour.Value = colours.RightSplat.ToVector3();
        effect["Splodges"].ReleaseColour.Value = colours.Splodges.ToVector3();
    }

    /// <summary>
    ///     The colours a nasty splatters when it dies: one for each of the two side splats and one for
    ///     the shower of splodges between them.
    /// </summary>
    private readonly record struct SplatColours(Color LeftSplat, Color RightSplat, Color Splodges);
}
