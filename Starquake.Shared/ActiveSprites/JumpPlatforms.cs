// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Display;
using Starquake.Engines;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class JumpPlatforms
{
    private readonly GameEngine _gameEngine;
    private readonly List<JumpPlatform> _platforms;

    public JumpPlatforms(GameEngine gameEngineRef)
    {
        _gameEngine = gameEngineRef;
        _platforms = new List<JumpPlatform>();
    }

    public int GetNewJumpPlatform(int xPos, int yPos, int width, int height, bool checkCollision)
    {
        var position = JumpPlatform.GetJumpPlatformCoords(xPos, yPos, _gameEngine.GameScreens);

        if (checkCollision)
        {
            if (_gameEngine.GameScreens.CheckRectangles(xPos, position.Y - 64, width, height, true) >= 0)
            {
                // If there is a collision, return a failure int i.e. -1000, needs to be a big number because we can have minus amounts when changing screen
                return -1000;
            }
        }

        JumpPlatform newPlatform = null;

        for (var a = 0; a < _platforms.Count; a++)
        {
            if (_platforms[a].ReadyToAllocate)
            {
                newPlatform = _platforms[a];
                newPlatform.Reset();
                newPlatform.XPos = position.X;
                newPlatform.YPos = position.Y;
                break;
            }
        }

        if (newPlatform == null)
        {
            newPlatform = new JumpPlatform(_gameEngine, _gameEngine.ContentMgr, position);
            _platforms.Add(newPlatform);
        }

        newPlatform.LeftCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.RightCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos + 32, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.PlatformActive = true;
        newPlatform.ReadyToAllocate = false;

        _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.LeftCollisionTile);
        _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.RightCollisionTile);

        // Returns blob's new YPosition
        return newPlatform.YPos - 64;
    }

    public void ResetPlatforms()
    {
        for (var a = 0; a < _platforms.Count; a++)
        {
            _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].LeftCollisionTile);
            _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].RightCollisionTile);
            _platforms[a].Reset();
        }
    }

    public void Update(GameScreenCollection gameScreensReference)
    {
        // Update jumpplatform lifetime
        if (_platforms.Count > 0)
        {
            for (var a = 0; a < _platforms.Count; a++)
            {
                if (_platforms[a].LeftCollisionTile != null && _platforms[a].RightCollisionTile != null)
                {
                    if (_platforms[a].Finished)
                    {
                        gameScreensReference.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].LeftCollisionTile);
                        gameScreensReference.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].RightCollisionTile);
                        _platforms[a].Reset();
                    }
                    else
                    {
                        _platforms[a].Update();
                    }
                }
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        for (var a = 0; a < _platforms.Count; a++)
        {
            _platforms[a].Draw(gameTime);
        }
    }

    internal class JumpPlatform
    {

        private readonly GameEngine _gameEngine;

        private readonly SpriteCollection _jumpPlatform;
        private readonly SpriteCollection _jumpPlatformOriginal;
        private readonly SpriteBatch _textureDrawer;

        private int _dissolveTime;

        private SpritePixelMatrix _matrix;

        private RenderTarget2D _renderTarget;

        public JumpPlatform(GameEngine gameEngineRef, ContentManager content, Point position)
        {
            _textureDrawer = new SpriteBatch(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice);
            _gameEngine = gameEngineRef;

            _jumpPlatform = gameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatforms").Clone(_gameEngine);
            _jumpPlatformOriginal = gameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatformsOriginal").Clone(_gameEngine);

            XPos = position.X;
            YPos = position.Y;

            Reset();
        }

        public CollisionTile LeftCollisionTile { get; set; }

        public CollisionTile RightCollisionTile { get; set; }

        public int XPos { get; set; }

        public int YPos { get; set; }

        public bool PlatformActive { get; set; }

        public bool Finished => _dissolveTime == 0;

        public bool ReadyToAllocate { get; set; }

        public static Point GetJumpPlatformCoords(int blobXPos, int blobYPos, GameScreenCollection gsc)
        {
            var returnValue = new Point();
            returnValue.X = blobXPos;
            var positionOnScreen = blobYPos;
            returnValue.Y = (positionOnScreen / 32 + 1) * 32;

            return returnValue;
        }

        public void Reset()
        {
            _matrix = new SpritePixelMatrix(_jumpPlatform.SpriteWidth * _jumpPlatform.SpriteHeight);
            _renderTarget = new RenderTarget2D(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice, _jumpPlatform.SpriteWidth, _jumpPlatform.SpriteHeight);
            PlatformActive = false;
            ReadyToAllocate = true;
            _dissolveTime = 100;
        }

        public void Update()
        {
            if (PlatformActive)
            {
                // Only disintegrate after a while
                if (_dissolveTime < 50)
                {
                    for (var a = 0; a < 50; a++)
                    {
                        _matrix.SetRandomPixel(Color.Transparent);
                    }
                }

                if (_dissolveTime > 0)
                {
                    _dissolveTime--;
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (PlatformActive)
            {
                // The render target is a texture that we draw directly to
                _gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(_renderTarget);
                _gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);

                if (_gameEngine.UsingOldGraphics)
                {
                    _jumpPlatformOriginal.Begin(BlendState.AlphaBlend, gameTime);
                    _jumpPlatformOriginal.DrawSprite(0, 0, 0, Color.White, _matrix.Matrix);
                    _jumpPlatformOriginal.End();
                }
                else
                {
                    _jumpPlatform.Begin(BlendState.AlphaBlend, gameTime);
                    _jumpPlatform.DrawSprite(0, 0, 0, Color.White, _matrix.Matrix);
                    _jumpPlatform.End();
                }


                // Set the render targer back to the screen
                _gameEngine.ScreenEffctEngine.SelectRenderViewPort("GameArea", false);

                if (_dissolveTime > 0)
                {
                    _textureDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

                    _textureDrawer.Draw(
                        _renderTarget,
                        new Rectangle(XPos + _jumpPlatformOriginal.SpriteWidth / 2, YPos + _jumpPlatformOriginal.SpriteHeight / 2,
                            _jumpPlatformOriginal.SpriteWidth,
                            _jumpPlatformOriginal.SpriteHeight),
                        null,
                        Color.White,
                        0,
                        new Vector2(_jumpPlatformOriginal.SpriteWidth / 2, _jumpPlatformOriginal.SpriteHeight / 2),
                        SpriteEffects.None, 0);

                    _textureDrawer.End();
                }
            }
        }
    }
}