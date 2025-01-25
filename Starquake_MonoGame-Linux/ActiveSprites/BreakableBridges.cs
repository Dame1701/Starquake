using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake_MonoGame.Structure;
using StarquakeXNA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starquake_MonoGame.ActiveSprites
{
    public class BreakableBridges
    {
        List<BreakableBridge> bridges;
        GameEngine gameEngineReference;

        public BreakableBridges(GameEngine GameEngineReference)
        {
            gameEngineReference = GameEngineReference;
            bridges = new List<BreakableBridge>();
        }

        public void AddBridge(int TileLocationX, int TileLocationY)
        {
            bridges.Add(new BreakableBridge(TileLocationX, TileLocationY, gameEngineReference));
        }

        public void AddBridge(Point TileLocation)
        {
            bridges.Add(new BreakableBridge(TileLocation.X, TileLocation.Y, gameEngineReference));
        }

        public void Update()
        {
            if (bridges.Count > 0)
            {
                for (int a = 0; a < bridges.Count; a++)
                {
                    if (!bridges[a].Finished)
                    {
                        bridges[a].Update();
                    }
                    else
                    {
                        // Update the collision map to remove the bridge collision once we've finished dissolving
                        gameEngineReference.GameScreens.UpdateCollisionMap(false);

                        bridges.Remove(bridges[a]);
                        a--;
                    }
                }
            }
        }

        public void Reset()
        {
            bridges.Clear();
        }

        public void Draw(GameTime GameTime)
        {
            if (bridges.Count > 0)
            {
                for (int a = 0; a < bridges.Count; a++)
                {
                    bridges[a].Draw(GameTime);
                }
            }
        }
    }

    public class BreakableBridge
    {
        SpriteBatch textureDrawer;
        RenderTarget2D renderTarget;
        SpritePixelMatrix matrix;

        SpriteCollection breakableBridge;
        SpriteCollection breakableBridgeOriginal;

        int xPos;
        int yPos;
        GameEngine gameEngineReference;

        public bool Finished
        {
            get
            {
                return matrix.Finished;
            }
        }

        public BreakableBridge(int XPos, int YPos, GameEngine GameEngineReference)
        {
            gameEngineReference = GameEngineReference;

            breakableBridge = gameEngineReference.ContentResourceManager.GetSpriteCollection("BreakableBridge");
            breakableBridgeOriginal = gameEngineReference.ContentResourceManager.GetSpriteCollection("BreakableBridgeOriginal");

            textureDrawer = new SpriteBatch(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice);
            matrix = new SpritePixelMatrix(breakableBridge.SpriteWidth * breakableBridge.SpriteHeight);
            renderTarget = new RenderTarget2D(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice, breakableBridge.SpriteWidth, breakableBridge.SpriteHeight);

            this.xPos = XPos;
            this.yPos = YPos;
        }

        public void Update()
        {
            for (int a = 0; a < 200; a++)
            {
                matrix.SetRandomPixel(Color.Transparent);
            }
        }

        public void Draw(GameTime GameTime)
        {
            if (!Finished)
            {
                // The render target is a texture that we draw directly to
                gameEngineReference.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(renderTarget);
                gameEngineReference.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);

                if (gameEngineReference.UsingOldGraphics)
                {
                    breakableBridgeOriginal.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, GameTime);
                    breakableBridgeOriginal.DrawSprite(0, 0, 0, Color.White, matrix.Matrix, 4);
                    breakableBridgeOriginal.End();
                }
                else
                {
                    breakableBridge.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, GameTime);
                    breakableBridge.DrawSprite(0, 0, 0, Color.White, matrix.Matrix, 4);
                    breakableBridge.End();
                }

                // Set the render targer back to the screen
                gameEngineReference.ScreenEffctEngine.SelectRenderViewPort("GameArea", false);

                textureDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                textureDrawer.Draw(
                    renderTarget,
                    new Rectangle(xPos + (breakableBridge.SpriteWidth / 2), (yPos + breakableBridge.SpriteHeight / 2),
                    breakableBridge.SpriteWidth,
                    breakableBridge.SpriteHeight),
                    null,
                    Color.White,
                    0,
                    new Vector2(breakableBridge.SpriteWidth / 2, breakableBridge.SpriteHeight / 2),
                    SpriteEffects.None, 0);

                textureDrawer.End();
            }
        }
    }

}
