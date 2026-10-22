// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Core;
using Starquake.Sprites;

namespace Starquake.ContentManagement;

internal class AnimTile
{
    private readonly GraphicsSet _graphicsSet;

    private readonly SpriteCollection _tileAnimationSpriteSet;

    public AnimTile(GraphicsSet graphicsSet, int screenTileRef, SpriteCollection tileAnimationSpriteSet, int delay)
    {
        _graphicsSet = graphicsSet;

        ScreenTileRef = screenTileRef;
        _tileAnimationSpriteSet = tileAnimationSpriteSet;

        TileAnimation = new Animation
        {
            FirstFrame = 0,
            LastFrame = _tileAnimationSpriteSet.SpriteCount - 1,
            AnimateForwards = true,
            Repeat = true,
            CurrentFrame = 0,
            Delay = delay,
            Yoyo = true
        };

    }
    public int ScreenTileRef { get; }

    private Animation TileAnimation { get; }

    public void Update()
    {
        TileAnimation.GetNextFrame();
    }

    public void Draw(int xPos, int yPos, Color colour)
    {
        if (_graphicsSet.IsRedrawn)
        {
            _tileAnimationSpriteSet.Begin(BlendState.AlphaBlend);
            _tileAnimationSpriteSet.DrawSprite(TileAnimation.CurrentFrame, xPos, yPos, colour);
            _tileAnimationSpriteSet.End();
        }
    }
}
