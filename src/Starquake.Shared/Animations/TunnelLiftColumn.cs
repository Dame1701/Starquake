// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Sprites;

namespace Starquake.Animations;

internal class TunnelLiftColumn
{
    private readonly List<TunnelLiftSection> _sections;

    private int _delayTime;
    public TunnelLiftColumn(int initialDelayTime)
    {
        _sections = new List<TunnelLiftSection>();
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
        _sections.Add(new TunnelLiftSection(_delayTime)
        {
            X = coordinates.X,
            Y = coordinates.Y,
            Intensity = 0
        });
        _delayTime += 15;
    }

    public void Update()
    {
        foreach (TunnelLiftSection section in _sections)
        {
            section.Update();
        }
    }

    public void Draw(SpriteCollection tunnelSprites, int screenfade)
    {
        foreach (TunnelLiftSection section in _sections)
        {
            if (section.Intensity > ColourLevels.Full)
            {
                tunnelSprites.DrawSprite(13, section.X, section.Y, Colours.WhiteFadedTo((byte)screenfade));
            }
            else
            {
                tunnelSprites.DrawSprite(13, section.X, section.Y, Colours.WhiteFadedTo(section.Intensity * (byte)screenfade / ColourLevels.Full));
            }
        }
    }
}
