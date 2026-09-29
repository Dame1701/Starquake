// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace Starquake_XNA;

public class TunnelLiftAnimationEngine : GameComponentBase
{
    private List<TunnelLiftAnimationColumn> Columns;

    public TunnelLiftAnimationEngine(GameEngine Engine)
        : base(Engine)
    {
        gameEngine = Engine;
    }

    public void PerformPostChangeScreenActions()
    {
        Point[] TunnelLiftArrowLocations = null;

        if (!gameEngine.UsingOldGraphics)
        {
            TunnelLiftArrowLocations = gameEngine.GameScreens.GetCoordsOfTiles(new[]
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
            TunnelLiftArrowLocations = gameEngine.GameScreens.GetCoordsOfTiles(new[]
            {
                408,
                409,
                412,
                413
            });
        }

        Columns = null;
        var InitialDelayTime = 0;

        if (TunnelLiftArrowLocations != null)
        {
            Columns = new List<TunnelLiftAnimationColumn>();

            for (var a = 0; a < TunnelLiftArrowLocations.Length; a++)
            {
                var ExistingColumn = Columns.Where(col => col.XPosition == TunnelLiftArrowLocations[a].X).FirstOrDefault();

                if (ExistingColumn == null)
                {
                    ExistingColumn = new TunnelLiftAnimationColumn(InitialDelayTime);
                    InitialDelayTime += 15;
                    Columns.Add(ExistingColumn);
                }

                ExistingColumn.AddSection(TunnelLiftArrowLocations[a]);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (Columns != null && Columns.Count > 0)
        {
            for (var a = 0; a < Columns.Count; a++)
            {
                Columns[a].Update();
            }
        }
    }

    public override void Draw(GameTime gameTime, int Screenfade)
    {
        if (Columns != null && Columns.Count > 0)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

            for (var a = 0; a < Columns.Count; a++)
            {
                Columns[a].Draw(gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName), Screenfade);
            }

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).End();
        }
    }

    public class TunnelLiftAnimationColumn
    {
        private readonly List<TunnelLifeAnimationSection> sections;

        private int DelayTime;
        public TunnelLiftAnimationColumn(int InitialDelayTime)
        {
            sections = new List<TunnelLifeAnimationSection>();
            DelayTime = InitialDelayTime;
        }

        public int XPosition
        {
            get
            {
                if (sections.Count > 0)
                {
                    return sections[0].X;
                }

                return 0;
            }
        }

        public void AddSection(Point Coordinates)
        {
            sections.Add(new TunnelLifeAnimationSection(DelayTime)
            {
                X = Coordinates.X,
                Y = Coordinates.Y,
                Intensity = 0
            });
            DelayTime += 15;
        }

        public void Update()
        {
            for (var a = 0; a < sections.Count; a++)
            {
                sections[a].Update();
            }
        }

        public void Draw(SpriteCollection TunnelSprites, int Screenfade)
        {
            for (var a = 0; a < sections.Count; a++)
            {
                if (sections[a].Intensity > 255)
                {
                    TunnelSprites.DrawSprite(13, sections[a].X, sections[a].Y, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)Screenfade));
                }
                else
                {
                    TunnelSprites.DrawSprite(13, sections[a].X, sections[a].Y, ColourHelper.GetXNA4Colour(255, 255, 255, sections[a].Intensity * (byte)Screenfade / 255));
                }
            }
        }
    }

    public class TunnelLifeAnimationSection
    {
        private readonly int DelayStart;
        private int DelayCounter;
        public int Intensity;

        private int LightDirection;
        public int X;
        public int Y;

        public TunnelLifeAnimationSection(int DelayStartTime)
        {
            DelayStart = DelayStartTime;
            DelayCounter = DelayStartTime;
        }

        public void Update()
        {
            if (DelayCounter > 0)
            {
                DelayCounter--;
                Intensity = 0;
            }
            else
            {
                if (LightDirection == 0)
                {
                    if (Intensity < 255)
                    {
                        Intensity += 8;
                    }
                    else
                    {
                        LightDirection = 1;
                    }
                }
                else if (LightDirection == 1)
                {
                    if (Intensity > 0)
                    {
                        Intensity -= 8;
                    }
                    else
                    {
                        LightDirection = 0;
                        //DelayCounter = DelayStart;
                    }
                }
            }
        }

        public override string ToString()
            => "X: " + X + ", Y:" + Y + ", D:" + DelayStart;
    }
}