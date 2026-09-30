// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Helpers;
using Starquake.Sprites;

namespace Starquake.Animations;

public class TunnelLiftAnimationColumn
{
    private readonly List<TunnelLiftAnimationSection> _sections;

    private int _delayTime;
    public TunnelLiftAnimationColumn(int initialDelayTime)
    {
        _sections = new List<TunnelLiftAnimationSection>();
        _delayTime = initialDelayTime;
    }

    public int XPosition
    {
        get
        {
            if (_sections.Count > 0)
            {
                return _sections[0].X;
            }

            return 0;
        }
    }

    public void AddSection(Point coordinates)
    {
        _sections.Add(new TunnelLiftAnimationSection(_delayTime)
        {
            X = coordinates.X,
            Y = coordinates.Y,
            Intensity = 0
        });
        _delayTime += 15;
    }

    public void Update()
    {
        foreach (var section in _sections)
        {
            section.Update();
        }
    }

    public void Draw(SpriteCollection tunnelSprites, int screenfade)
    {
        foreach (var section in _sections)
        {
            if (section.Intensity > ColourLevels.Full)
            {
                tunnelSprites.DrawSprite(13, section.X, section.Y, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenfade));
            }
            else
            {
                tunnelSprites.DrawSprite(13, section.X, section.Y, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, section.Intensity * (byte)screenfade / ColourLevels.Full));
            }
        }
    }
}
