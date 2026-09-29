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
        for (var a = 0; a < _sections.Count; a++)
        {
            _sections[a].Update();
        }
    }

    public void Draw(SpriteCollection tunnelSprites, int screenfade)
    {
        for (var a = 0; a < _sections.Count; a++)
        {
            if (_sections[a].Intensity > ColourLevels.Full)
            {
                tunnelSprites.DrawSprite(13, _sections[a].X, _sections[a].Y, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenfade));
            }
            else
            {
                tunnelSprites.DrawSprite(13, _sections[a].X, _sections[a].Y, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, _sections[a].Intensity * (byte)screenfade / ColourLevels.Full));
            }
        }
    }
}
