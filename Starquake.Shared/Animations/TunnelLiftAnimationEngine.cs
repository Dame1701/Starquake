// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Animations;

public class TunnelLiftAnimationEngine : GameComponentBase
{
    private List<TunnelLiftAnimationColumn> _columns;

    public TunnelLiftAnimationEngine(GameEngine engine)
        : base(engine)
    {
        GameEngine = engine;
    }

    public void PerformPostChangeScreenActions()
    {
        Point[] tunnelLiftArrowLocations = null;

        if (!GameEngine.UsingOldGraphics)
        {
            tunnelLiftArrowLocations = GameEngine.GameScreens.GetCoordsOfTiles(new[]
            {
                396,
                407,
                408,
                409,
                412,
                413,
                414,
                415
            });
        }
        else
        {
            tunnelLiftArrowLocations = GameEngine.GameScreens.GetCoordsOfTiles(new[]
            {
                408,
                409,
                412,
                413
            });
        }

        _columns = null;
        var initialDelayTime = 0;

        if (tunnelLiftArrowLocations != null)
        {
            _columns = new List<TunnelLiftAnimationColumn>();

            for (var a = 0; a < tunnelLiftArrowLocations.Length; a++)
            {
                var existingColumn = _columns.Where(col => col.XPosition == tunnelLiftArrowLocations[a].X).FirstOrDefault();

                if (existingColumn == null)
                {
                    existingColumn = new TunnelLiftAnimationColumn(initialDelayTime);
                    initialDelayTime += 15;
                    _columns.Add(existingColumn);
                }

                existingColumn.AddSection(tunnelLiftArrowLocations[a]);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_columns != null && _columns.Count > 0)
        {
            for (var a = 0; a < _columns.Count; a++)
            {
                _columns[a].Update();
            }
        }
    }

    public override void Draw(GameTime gameTime, int screenfade)
    {
        if (_columns != null && _columns.Count > 0)
        {
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

            for (var a = 0; a < _columns.Count; a++)
            {
                _columns[a].Draw(GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName), screenfade);
            }

            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).End();
        }
    }

    public class TunnelLiftAnimationColumn
    {
        private readonly List<TunnelLifeAnimationSection> _sections;

        private int _delayTime;
        public TunnelLiftAnimationColumn(int initialDelayTime)
        {
            _sections = new List<TunnelLifeAnimationSection>();
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
            _sections.Add(new TunnelLifeAnimationSection(_delayTime)
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
                if (_sections[a].Intensity > 255)
                {
                    tunnelSprites.DrawSprite(13, _sections[a].X, _sections[a].Y, ColourHelper.GetXna4Colour(255, 255, 255, (byte)screenfade));
                }
                else
                {
                    tunnelSprites.DrawSprite(13, _sections[a].X, _sections[a].Y, ColourHelper.GetXna4Colour(255, 255, 255, _sections[a].Intensity * (byte)screenfade / 255));
                }
            }
        }
    }

    public class TunnelLifeAnimationSection
    {
        private readonly int _delayStart;
        private int _delayCounter;
        public int Intensity;

        private int _lightDirection;
        public int X;
        public int Y;

        public TunnelLifeAnimationSection(int delayStartTime)
        {
            _delayStart = delayStartTime;
            _delayCounter = delayStartTime;
        }

        public void Update()
        {
            if (_delayCounter > 0)
            {
                _delayCounter--;
                Intensity = 0;
            }
            else
            {
                if (_lightDirection == 0)
                {
                    if (Intensity < 255)
                    {
                        Intensity += 8;
                    }
                    else
                    {
                        _lightDirection = 1;
                    }
                }
                else if (_lightDirection == 1)
                {
                    if (Intensity > 0)
                    {
                        Intensity -= 8;
                    }
                    else
                    {
                        _lightDirection = 0;
                        //DelayCounter = DelayStart;
                    }
                }
            }
        }

        public override string ToString()
            => "X: " + X + ", Y:" + Y + ", D:" + _delayStart;
    }
}