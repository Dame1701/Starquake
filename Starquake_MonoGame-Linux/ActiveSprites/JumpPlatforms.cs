using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharedLibs;
using Starquake_MonoGame.Structure;

namespace StarquakeXNA
{
    public class JumpPlatforms
    {
        private List<JumpPlatform> platforms;
        private GameEngine gameEngine;

        public JumpPlatforms(GameEngine GameEngineRef)
        {
            gameEngine = GameEngineRef;
            platforms = new List<JumpPlatform>();
        }

        public int GetNewJumpPlatform(int XPos, int YPos, int Width, int Height, bool CheckCollision)
        {
            Point position = JumpPlatform.GetJumpPlatformCoords(XPos, YPos, gameEngine.GameScreens);

            if (CheckCollision)
            {
                if ((gameEngine.GameScreens.CheckRectangles(XPos, position.Y - 64, Width, Height, true) >= 0))
                {
                    // If there is a collision, return a failure int i.e. -1000, needs to be a big number because we can have minus amounts when changing screen
                    return -1000;
                }
            }

            JumpPlatform newPlatform = null;

            for (int a = 0; a < platforms.Count; a++)
            {
                if (platforms[a].ReadyToAllocate)
                {
                    newPlatform = platforms[a];
                    newPlatform.Reset();
                    newPlatform.XPos = position.X;
                    newPlatform.YPos = position.Y;
                    break;
                }
            }

            if (newPlatform == null)
            {
                newPlatform = new JumpPlatform(gameEngine, gameEngine.ContentMgr, position);
                platforms.Add(newPlatform);
            }

            newPlatform.LeftCollisionTile = new CollisionTile((int)enCollisionObjectType.JumpPlatform, newPlatform.XPos, newPlatform.YPos, enCollTileType.EveryoneHits);
            newPlatform.RightCollisionTile = new CollisionTile((int)enCollisionObjectType.JumpPlatform, newPlatform.XPos + 32, newPlatform.YPos, enCollTileType.EveryoneHits);
            newPlatform.PlatformActive = true;
            newPlatform.ReadyToAllocate = false;

            gameEngine.GameScreens.CurrentGameScreen.CollisionMap.Add(newPlatform.LeftCollisionTile);
            gameEngine.GameScreens.CurrentGameScreen.CollisionMap.Add(newPlatform.RightCollisionTile);

            // Returns blob's new YPosition
            return newPlatform.YPos - 64;
        }

        public void ResetPlatforms()
        {
            for (int a = 0; a < platforms.Count; a++)
            {
                gameEngine.GameScreens.CurrentGameScreen.CollisionMap.Remove(platforms[a].LeftCollisionTile);
                gameEngine.GameScreens.CurrentGameScreen.CollisionMap.Remove(platforms[a].RightCollisionTile);
                platforms[a].Reset();
            }
        }

        public void Update(GameScreenCollection GameScreensReference)
        {
            // Update jumpplatform lifetime
            if (platforms.Count > 0)
            {
                for (int a = 0; a < platforms.Count; a++)
                {
                    if (platforms[a].LeftCollisionTile != null && platforms[a].RightCollisionTile != null)
                    {
                        if (platforms[a].Finished)
                        {
                            GameScreensReference.CurrentGameScreen.CollisionMap.Remove(platforms[a].LeftCollisionTile);
                            GameScreensReference.CurrentGameScreen.CollisionMap.Remove(platforms[a].RightCollisionTile);
                            platforms[a].Reset();
                        }
                        else
                        {
                            platforms[a].Update();
                        }
                    }
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            for (int a = 0; a < platforms.Count; a++)
            {
                platforms[a].Draw(gameTime);
            }
        }

        internal class JumpPlatform
        {
            SpriteBatch textureDrawer;
            RenderTarget2D renderTarget;
            SpritePixelMatrix matrix;

            public static Point GetJumpPlatformCoords(int BlobXPos, int BlobYPos, GameScreenCollection GSC)
            {
                Point ReturnValue = new Point();
                ReturnValue.X = BlobXPos;
                int PositionOnScreen = BlobYPos;
                ReturnValue.Y = (((PositionOnScreen / 32) + 1) * 32);

                return ReturnValue;
            }

            private CollisionTile leftCollisionTile;
            public CollisionTile LeftCollisionTile
            {
                get { return leftCollisionTile; }
                set { leftCollisionTile = value; }
            }

            private CollisionTile rightCollisionTile;
            public CollisionTile RightCollisionTile
            {
                get { return rightCollisionTile; }
                set { rightCollisionTile = value; }
            }

            private int xPos;
            public int XPos
            {
                get { return xPos; }
                set { xPos = value; }
            }

            private int yPos;
            public int YPos
            {
                get { return yPos; }
                set { yPos = value; }
            }

            int dissolveTime = 0;
            bool platformActive = false;
            public bool PlatformActive
            {
                get { return platformActive; }
                set { platformActive = value; }
            }

            public bool Finished
            {
                get { return dissolveTime == 0; }
            }

            private bool readyToAllocate;
            public bool ReadyToAllocate
            {
                set { readyToAllocate = value; }
                get { return readyToAllocate; }
            }

            private SpriteCollection jumpPlatform;
            private SpriteCollection jumpPlatformOriginal;

            private GameEngine gameEngine;

            public JumpPlatform(GameEngine GameEngineRef, ContentManager Content, Point Position)
            {
                textureDrawer = new SpriteBatch(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice);
                gameEngine = GameEngineRef;

                jumpPlatform = GameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatforms").Clone(gameEngine); 
                jumpPlatformOriginal = GameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatformsOriginal").Clone(gameEngine);

                xPos = Position.X;
                yPos = Position.Y;

                Reset();
            }

            public void Reset()
            {
                matrix = new SpritePixelMatrix(jumpPlatform.SpriteWidth * jumpPlatform.SpriteHeight);
                renderTarget = new RenderTarget2D(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice, jumpPlatform.SpriteWidth, jumpPlatform.SpriteHeight);
                platformActive = false;
                readyToAllocate = true;
                dissolveTime = 100;
            }

            public void Update()
            {
                if (platformActive)
                {
                    // Only disintegrate after a while
                    if (dissolveTime < 50)
                    {
                        for (int a = 0; a < 50; a++)
                        {
                            matrix.SetRandomPixel(Color.Transparent);
                        }
                    }

                    if (dissolveTime > 0)
                    {
                        dissolveTime--;
                    }
                }
            }

            public void Draw(GameTime gameTime)
            {
                if (platformActive)
                {
                    // The render target is a texture that we draw directly to
                    gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(renderTarget);
                    gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);

                    if (gameEngine.UsingOldGraphics)
                    {
                        jumpPlatformOriginal.Begin(BlendState.AlphaBlend, gameTime);
                        jumpPlatformOriginal.DrawSprite(0, 0, 0, Color.White, matrix.Matrix, 4);
                        jumpPlatformOriginal.End();
                    }
                    else
                    {
                        jumpPlatform.Begin(BlendState.AlphaBlend, gameTime);
                        jumpPlatform.DrawSprite(0, 0, 0, Color.White, matrix.Matrix, 4);
                        jumpPlatform.End();
                    }


                    // Set the render targer back to the screen
                    gameEngine.ScreenEffctEngine.SelectRenderViewPort("GameArea", false);

                    if (dissolveTime > 0)
                    {
                        textureDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

                        textureDrawer.Draw(
                            renderTarget,
                            new Rectangle(xPos + (jumpPlatformOriginal.SpriteWidth / 2), (yPos + jumpPlatformOriginal.SpriteHeight / 2),
                            jumpPlatformOriginal.SpriteWidth,
                            jumpPlatformOriginal.SpriteHeight),
                            null,
                            Color.White,
                            0,
                            new Vector2(jumpPlatformOriginal.SpriteWidth / 2, jumpPlatformOriginal.SpriteHeight / 2),
                            SpriteEffects.None, 0);

                        textureDrawer.End();
                    }
                }
            }
        }
    }
}
