// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ContentManagement;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Animations;

public class AnimTile : GameComponentBase
{
    private readonly GraphicsSet _graphicsSet;

    private readonly SpriteCollection _tileAnimationSpriteSet;

    public AnimTile(GraphicsSet graphicsSet, int screenTileRef, SpriteCollection tileAnimationSpriteSet, int delay)
    {
        _graphicsSet = graphicsSet;

        this.ScreenTileRef = screenTileRef;
        _tileAnimationSpriteSet = tileAnimationSpriteSet;

        TileAnimation = new Animation();
        TileAnimation.FirstFrame = 0;

        TileAnimation.LastFrame = _tileAnimationSpriteSet.SpriteCount - 1;
        TileAnimation.AnimateForwards = true;
        TileAnimation.Repeat = true;
        TileAnimation.CurrentFrame = 0;
        TileAnimation.Delay = delay;
        TileAnimation.Yoyo = true;
    }
    public int ScreenTileRef { get; }

    private Animation TileAnimation { get; }

    public override void Update(GameTime gameTime)
    {
        TileAnimation.GetNextFrame();
    }

    public void Draw(int xPos, int yPos, Color colour, GameTime gameTime)
    {
        if (_graphicsSet.IsRedrawn)
        {
            _tileAnimationSpriteSet.Begin(BlendState.AlphaBlend, gameTime);
            _tileAnimationSpriteSet.DrawSprite(TileAnimation.CurrentFrame, xPos, yPos, colour);
            _tileAnimationSpriteSet.End();
        }
    }
}