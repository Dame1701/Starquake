// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SharedLibs;
using Starquake_MonoGame.Structure;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class JumpPlatforms
{
    private readonly GameEngine gameEngine;
    private readonly List<JumpPlatform> platforms;

    public JumpPlatforms(GameEngine GameEngineRef)
    {
        gameEngine = GameEngineRef;
        platforms = new List<JumpPlatform>();
    }

    public int GetNewJumpPlatform(int XPos, int YPos, int Width, int Height, bool CheckCollision)
    {
        var position = JumpPlatform.GetJumpPlatformCoords(XPos, YPos, gameEngine.GameScreens);

        if (CheckCollision)
        {
            if (gameEngine.GameScreens.CheckRectangles(XPos, position.Y - 64, Width, Height, true) >= 0)
            {
                // If there is a collision, return a failure int i.e. -1000, needs to be a big number because we can have minus amounts when changing screen
                return -1000;
            }
        }

        JumpPlatform newPlatform = null;

        for (var a = 0; a < platforms.Count; a++)
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

        newPlatform.LeftCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.RightCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos + 32, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.PlatformActive = true;
        newPlatform.ReadyToAllocate = false;

        gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.LeftCollisionTile);
        gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.RightCollisionTile);

        // Returns blob's new YPosition
        return newPlatform.YPos - 64;
    }

    public void ResetPlatforms()
    {
        for (var a = 0; a < platforms.Count; a++)
        {
            gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(platforms[a].LeftCollisionTile);
            gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(platforms[a].RightCollisionTile);
            platforms[a].Reset();
        }
    }

    public void Update(GameScreenCollection GameScreensReference)
    {
        // Update jumpplatform lifetime
        if (platforms.Count > 0)
        {
            for (var a = 0; a < platforms.Count; a++)
            {
                if (platforms[a].LeftCollisionTile != null && platforms[a].RightCollisionTile != null)
                {
                    if (platforms[a].Finished)
                    {
                        GameScreensReference.CurrentGameScreenData.CollisionMap.Remove(platforms[a].LeftCollisionTile);
                        GameScreensReference.CurrentGameScreenData.CollisionMap.Remove(platforms[a].RightCollisionTile);
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
        for (var a = 0; a < platforms.Count; a++)
        {
            platforms[a].Draw(gameTime);
        }
    }

    internal class JumpPlatform
    {

        private readonly GameEngine gameEngine;

        private readonly SpriteCollection jumpPlatform;
        private readonly SpriteCollection jumpPlatformOriginal;
        private readonly SpriteBatch textureDrawer;

        private int dissolveTime;

        private SpritePixelMatrix matrix;

        private RenderTarget2D renderTarget;

        public JumpPlatform(GameEngine GameEngineRef, ContentManager Content, Point Position)
        {
            textureDrawer = new SpriteBatch(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice);
            gameEngine = GameEngineRef;

            jumpPlatform = GameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatforms").Clone(gameEngine);
            jumpPlatformOriginal = GameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatformsOriginal").Clone(gameEngine);

            XPos = Position.X;
            YPos = Position.Y;

            Reset();
        }

        public CollisionTile LeftCollisionTile { get; set; }

        public CollisionTile RightCollisionTile { get; set; }

        public int XPos { get; set; }

        public int YPos { get; set; }

        public bool PlatformActive { get; set; }

        public bool Finished => dissolveTime == 0;

        public bool ReadyToAllocate { get; set; }

        public static Point GetJumpPlatformCoords(int BlobXPos, int BlobYPos, GameScreenCollection GSC)
        {
            var ReturnValue = new Point();
            ReturnValue.X = BlobXPos;
            var PositionOnScreen = BlobYPos;
            ReturnValue.Y = (PositionOnScreen / 32 + 1) * 32;

            return ReturnValue;
        }

        public void Reset()
        {
            matrix = new SpritePixelMatrix(jumpPlatform.SpriteWidth * jumpPlatform.SpriteHeight);
            renderTarget = new RenderTarget2D(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice, jumpPlatform.SpriteWidth, jumpPlatform.SpriteHeight);
            PlatformActive = false;
            ReadyToAllocate = true;
            dissolveTime = 100;
        }

        public void Update()
        {
            if (PlatformActive)
            {
                // Only disintegrate after a while
                if (dissolveTime < 50)
                {
                    for (var a = 0; a < 50; a++)
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
            if (PlatformActive)
            {
                // The render target is a texture that we draw directly to
                gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(renderTarget);
                gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);

                if (gameEngine.UsingOldGraphics)
                {
                    jumpPlatformOriginal.Begin(BlendState.AlphaBlend, gameTime);
                    jumpPlatformOriginal.DrawSprite(0, 0, 0, Color.White, matrix.Matrix);
                    jumpPlatformOriginal.End();
                }
                else
                {
                    jumpPlatform.Begin(BlendState.AlphaBlend, gameTime);
                    jumpPlatform.DrawSprite(0, 0, 0, Color.White, matrix.Matrix);
                    jumpPlatform.End();
                }


                // Set the render targer back to the screen
                gameEngine.ScreenEffctEngine.SelectRenderViewPort("GameArea", false);

                if (dissolveTime > 0)
                {
                    textureDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

                    textureDrawer.Draw(
                        renderTarget,
                        new Rectangle(XPos + jumpPlatformOriginal.SpriteWidth / 2, YPos + jumpPlatformOriginal.SpriteHeight / 2,
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