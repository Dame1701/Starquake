// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using ProjectMercury;
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.Helpers;

public class NastyHelpers
{
    public static void ApplyNastyColours(NastyType nastyType, ParticleEffect effect)
    {
        if (nastyType == NastyType.FriedEgg)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(125, 62, 62).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(0, 182, 0).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Gonzo)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(4, 82, 184).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(182, 103, 10).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Cushion)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(59, 194, 42).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(227, 66, 19).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Helmet)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(202, 109, 17).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(137, 137, 137).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Shellfish)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(239, 238, 0).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(198, 99, 0).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Snail)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 124, 0).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(141, 141, 141).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(166, 198, 0).ToVector3();
        }
        else if (nastyType == NastyType.CurleyWurley)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(168, 163, 252).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(109, 109, 253).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Bird)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 0, 211).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(148, 148, 217).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(188, 62, 0).ToVector3();
        }
        else if (nastyType == NastyType.Ghost)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(0, 151, 43).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(88, 0, 88).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full).ToVector3();
        }
        else if (nastyType == NastyType.Scorpian)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(177, 8, 8).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(130, 2, 2).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(230, 230, 230).ToVector3();
        }
        else if (nastyType == NastyType.DragonFly)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(8, 150, 219).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(50, 111, 85).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(76, 191, 142).ToVector3();
        }
        else if (nastyType == NastyType.Helicopter)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(177, 8, 8).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(130, 2, 2).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(230, 230, 230).ToVector3();
        }
        else if (nastyType == NastyType.Hippo)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(229, 120, 252).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(137, 115, 251).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(145, 49, 50).ToVector3();
        }
        else if (nastyType == NastyType.Centipede)
        {
            effect["Copy of Left Splat"].ReleaseColour.Value = new Color(87, 165, 28).ToVector3();
            effect["Copy of Right Splat"].ReleaseColour.Value = new Color(110, 6, 124).ToVector3();
            effect["Copy of Splodges"].ReleaseColour.Value = new Color(221, 134, 38).ToVector3();
        }
    }
}