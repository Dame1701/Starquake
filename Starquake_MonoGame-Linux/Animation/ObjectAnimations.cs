using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public class KeyScanner
    {
        int scanLineY;
        int scanLineFade;

        enDirection scanLineFadeDirection;
        int keyCodeFade;
        GameEngine gameEngine;
        bool matched;

        bool scanStarted;
        public bool ScanStarted
        {
            get { return scanStarted; }
        }

        bool flicker;

        bool scanned;
        public bool Scanned
        {
            get { return scanned; }
        }

        public KeyScanner(GameEngine GameEngineReference, bool Flicker)
        {
            flicker = Flicker;
            gameEngine = GameEngineReference;
            Reset(true);
        }

        public void Reset(bool Match)
        {
            scanLineFade = 255;
            scanLineY = 0;
            scanLineFadeDirection = enDirection.Down;
            matched = Match;
            scanned = false;
            scanStarted = false;
        }

        public void StartScan()
        {
            if (scanStarted == false)
            {
                scanStarted = true;
                gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("ScanCode", 0.3f, false);
            }
        }

        public void Update()
        {
            if (scanStarted)
            {
                if (scanLineFadeDirection == enDirection.Down)
                {
                    scanLineFade = scanLineFade - 16;

                    if (scanLineFade < 0)
                    {
                        scanLineFade = 0;
                        scanLineFadeDirection = enDirection.Up;
                    }
                }
                else if (scanLineFadeDirection == enDirection.Up)
                {
                    scanLineFade = scanLineFade + 16;

                    if (scanLineFade > 255)
                    {
                        scanLineFade = 255;
                        scanLineFadeDirection = enDirection.Down;
                    }
                }

                if (scanLineY < 68)
                {
                    scanLineY = scanLineY + 4;
                }
                else
                {
                    scanned = true;
                }

            }

            if (flicker)
            {
                keyCodeFade = gameEngine.RandomNumberGenerator.Next(100, 256);
            }
            else
            {
                keyCodeFade = 255;
            }
        }

        public void Draw(int CodeNumber, int XPos, int YPos, GameTime gameTime)
        {
            // Draw the code keys
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(CodeNumber, XPos + 2, YPos + 6,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)keyCodeFade));
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();

            int height = 0;
            if (scanLineY > 64)
            {
                height = 64;
            }
            else
            {
                height = scanLineY;
            }

            // If scan started......
            if (scanStarted)
            {
                if (matched)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).DrawSpriteSubSection(CodeNumber, XPos + 2, YPos + 6, new Microsoft.Xna.Framework.Rectangle(0, 0, 64, height),  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)keyCodeFade));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesPassedName).End();

                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).DrawSpriteSubSection(CodeNumber, XPos + 2, YPos + 6, new Microsoft.Xna.Framework.Rectangle(0, 0, 64, height),  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)keyCodeFade));
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.AccessCodesFailedName).End();
                }

                // Draw the scanlines
                gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").Begin(BlendState.Additive, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").DrawSprite(0, XPos - 4, scanLineY + YPos,  ColourHelper.GetXNA4Colour(255, 255, 255, (byte)scanLineFade));
                gameEngine.ContentResourceManager.GetSpriteCollection("ScanLine").End();
            }
        }
    }

    public class BlobAccessBoothAnim
    {
        int blobFrame;
        int startPoint;
        int endPoint;
        int blobX;

        enDirection blobDirection;
        Animation animLeft;
        Animation animRight;
        Animation animTurn;
        GameEngine gameEngine;

        public BlobAccessBoothAnim(GameEngine GameEngineReference, int StartPoint, int EndPoint)
        {
            gameEngine = GameEngineReference;
            startPoint = StartPoint;
            endPoint = EndPoint;

            animLeft = new Animation();
            animLeft.FirstFrame = 0;
            animLeft.CurrentFrame = 0;
            animLeft.LastFrame = 3;
            animLeft.Yoyo = false;
            animLeft.Delay = 2;
            animLeft.Repeat = true;
            animLeft.AnimateForwards = false;

            animRight = new Animation();
            animRight.FirstFrame = 7;
            animRight.CurrentFrame = 7;
            animRight.LastFrame = 10;
            animRight.Yoyo = false;
            animRight.Delay = 2;
            animRight.Repeat = true;
            animRight.AnimateForwards = true;

            animTurn = new Animation();
            animTurn.FirstFrame = 4;
            animTurn.CurrentFrame = 4;
            animTurn.LastFrame = 10;
            animTurn.Yoyo = false;
            animTurn.Delay = 2;
            animTurn.Repeat = false;
            animTurn.AnimateForwards = false;
            animTurn.Finished = false;
        }

        public void Reset(enDirection StartingDirection)
        {
            if (StartingDirection == enDirection.Right)
            {
                //blobX = 164;
                blobX = startPoint;
                blobFrame = 10;
            }
            else if (StartingDirection == enDirection.Left)
            {
                blobX = endPoint;
                blobFrame = 0;
            }

            blobDirection = StartingDirection;
        }

        public void Update()
        {
            if (blobDirection == enDirection.Right)
            {
                blobX = blobX + 8;

                if (!animTurn.Finished)
                {
                    animTurn.GetNextFrame();
                }
                else
                {
                    animRight.GetNextFrame();
                }

                if (animTurn.Finished)
                {
                    blobFrame = animRight.CurrentFrame;
                }
                else
                {
                    blobFrame = animTurn.CurrentFrame;
                }
            }
            else if (blobDirection == enDirection.Left)
            {
                blobX = blobX - 8;

                if (!animTurn.Finished)
                {
                    animTurn.GetNextFrame();
                }
                else
                {
                    animLeft.GetNextFrame();
                }

                if (animTurn.Finished)
                {
                    blobFrame = animLeft.CurrentFrame;
                }
                else
                {
                    blobFrame = animTurn.CurrentFrame;
                }
            }

            if (blobX > endPoint)
            {
                blobDirection = enDirection.Left;

                animTurn.FirstFrame = 0;
                animTurn.CurrentFrame = 6;
                animTurn.LastFrame = 6;
                animTurn.AnimateForwards = false;
                animTurn.Finished = false;
            }
            else if (blobX < startPoint)
            {
                blobDirection = enDirection.Right;

                animTurn.FirstFrame = 4;
                animTurn.CurrentFrame = 4;
                animTurn.LastFrame = 10;
                animTurn.AnimateForwards = true;
                animTurn.Finished = false;
            }
        }

        public void Draw(GameTime gameTime)
        {
            // Draw blob animation
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).DrawSprite(blobFrame, blobX, 192);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.BlobSpritesName).End();
        }
    }
}
