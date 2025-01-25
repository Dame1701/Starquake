using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StarquakeXNA.Helpers;
using Starquake_MonoGame.Structure;

namespace StarquakeXNA
{
    public enum enSpriteEffect
    {
        None,
        Distortion,
        Dissolve,
        DrawAsSolidColour,
    }

    public class EffectSprite
    {
        public SpriteCollection Sprite;
        public Point Location;
    }

    [Serializable]
    public class SpriteCollection
    {
        private int[] spriteTexturePosX;
        private int[] spriteTexturePosY;
        private int noImages;

        private int effectSpeed;
        public int EffectSpeed
        {
            get { return effectSpeed; }
            set { effectSpeed = value; }
        }

        private float spriteRotation = 0f;
        public float SpriteRotation
        {
            get { return spriteRotation; }
            set { spriteRotation = value; }
        }

        private bool clockWise = true;
        private float rotationSpeed = 1f;

        public static Texture2D SpriteEffectsTexture;

        [NonSerialized]
        private SpriteBatch sprites;

        [NonSerialized]
        private Texture2D spriteImages;

        private int spriteWidth;
        public int SpriteWidth
        {
            get { return spriteWidth; }
        }

        private int spriteHeight;
        public int SpriteHeight
        {
            get { return spriteHeight; }
        }

        private byte fade;

        private bool effectEnded;
        public bool EffectEnded
        {
            get { return effectEnded; }
        }

        public int SpriteCount
        {
            get { return noImages; }
        }

        private string imageFilePath;

        [NonSerialized]
        private Effect spriteEffect;

        private enSpriteEffect currentSpriteEffect;
        public enSpriteEffect CurrentSpriteEffect
        {
            get { return currentSpriteEffect; }
            set
            {
                currentSpriteEffect = value;
                SetSpriteEffect();
            }
        }

        [NonSerialized]
        private GameEngine gameEngineReference;

        public SpriteCollection(GameEngine GameEngineReference, ContentManager Content, string ImageFilePath, int SpriteWidth, int SpriteHeight)
        {
            imageFilePath = ImageFilePath;
            gameEngineReference = GameEngineReference;

            effectSpeed = 4;

            spriteWidth = SpriteWidth;
            spriteHeight = SpriteHeight;

            sprites = new SpriteBatch(gameEngineReference.GraphicsDeviceMgr.GraphicsDevice);
            spriteImages = Content.Load<Texture2D>(ImageFilePath);

            int NoHorizontalImages = spriteImages.Width / SpriteWidth;
            int NoVerticleImages = spriteImages.Height / SpriteHeight;

            noImages = NoHorizontalImages * NoVerticleImages;

            spriteTexturePosX = new int[noImages];
            spriteTexturePosY = new int[noImages];

            int Counter = 0;

            fade = 255;

            CurrentSpriteEffect = enSpriteEffect.None;

            // Generate Coordinates for the textures of the tiles found
            for (int y = 0; y < NoVerticleImages; y++)
            {
                for (int x = 0; x < NoHorizontalImages; x++)
                {
                    spriteTexturePosX[Counter] = x * spriteWidth;
                    spriteTexturePosY[Counter] = y * spriteHeight;
                    Counter++;
                }
            }
        }

        private void SetSpriteEffect()
        {
            if (currentSpriteEffect == enSpriteEffect.Distortion)
            {
                //spriteEffect = gameEngineReference.ContentMgr.Load<Effect>("Shaders\\refraction");
            }
            else if (currentSpriteEffect == enSpriteEffect.Dissolve)
            {
                //spriteEffect = gameEngineReference.ContentMgr.Load<Effect>("Shaders\\disappear");
            }
            else if (currentSpriteEffect == enSpriteEffect.DrawAsSolidColour)
            {
                //spriteEffect = gameEngineReference.ContentMgr.Load<Effect>("Shaders\\drawaswhite");
            }

            effectEnded = false;
            fade = 255;
        }

        public void ResetEffect()
        {
            fade = 0;
            effectEnded = true;
        }

        public void Begin(BlendState blendState, GameTime gameTime)
        {
            if (currentSpriteEffect == enSpriteEffect.None)
            {
                sprites.Begin(SpriteSortMode.Deferred, blendState);
            }
            else
            {
                sprites.Begin(SpriteSortMode.Immediate, blendState);
            }


            if (currentSpriteEffect == enSpriteEffect.Distortion)
            {
                spriteEffect.Parameters["DisplacementScroll"].SetValue(
                                                           MoveInCircle(gameTime, 0.1f));
                /*
                if (fade > 0)
                {
                    int fade2 = fade;
                    fade2 = fade2 - effectSpeed;

                    if (fade2 > 0)
                    {
                        fade = (byte)fade2;
                    }
                    else
                    {
                        fade = 0;
                    }
                }
                */
            }
            else if (currentSpriteEffect == enSpriteEffect.Dissolve)
            {
                Vector2 moveInCircle = MoveInCircle(gameTime, 0.8f) * 0.25f;
                spriteEffect.Parameters["OverlayScroll"].SetValue(moveInCircle);


                if (fade > 0)
                {
                    int fade2 = fade;
                    fade2 = fade2 - effectSpeed;

                    if (fade2 > 0)
                    {
                        fade = (byte)fade2;
                    }
                    else
                    {
                        fade = 0;
                    }
                }

            }

            // Begin the custom effect.
            if (currentSpriteEffect != enSpriteEffect.None)
            {
                spriteEffect.CurrentTechnique.Passes[0].Apply();
            }
        }

        public void Begin(BlendState blendState, SpriteSortMode sortMode, GameTime gameTime)
        {
            if (currentSpriteEffect == enSpriteEffect.None)
            {
                sprites.Begin(sortMode, blendState);

            }
            else
            {
                gameEngineReference.GraphicsDeviceMgr.GraphicsDevice.Textures[1] = SpriteEffectsTexture;
                sprites.Begin(SpriteSortMode.Immediate,
                              blendState);
            }

            if (spriteEffect != null)
            {
                if (currentSpriteEffect == enSpriteEffect.Distortion)
                {
                    spriteEffect.Parameters["DisplacementScroll"].SetValue(
                                                               MoveInCircle(gameTime, 0.1f));
                }
                else if (currentSpriteEffect == enSpriteEffect.Dissolve)
                {
                    if (fade > 0)
                    {
                        int fade2 = fade;
                        fade2 = fade2 - 4;

                        if (fade2 > 0)
                        {
                            fade = (byte)fade2;
                        }
                        else
                        {
                            fade = 0;
                        }
                    }
                }

                // Begin the custom effect.
                if (currentSpriteEffect != enSpriteEffect.None)
                {
                    spriteEffect.CurrentTechnique.Passes[0].Apply();
                }
            }
        }

        public void DrawSpriteUnscaled(int SpriteNumber, int XPosition, int YPosition)
        {
            sprites.Draw(
                spriteImages,
                new Vector2(XPosition, YPosition),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(255, 255, 255, fade),
                0f,
                new Vector2(0, 0),
                new Vector2(1, 1),
                SpriteEffects.None,
                0);
        }

        public void DrawSpriteUnscaled(int SpriteNumber, int XPosition, int YPosition, int Fade)
        {
            sprites.Draw(
                spriteImages,
                new Vector2(XPosition, YPosition),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(255, 255, 255, Fade),
                0f,
                new Vector2(0, 0),
                new Vector2(1, 1),
                SpriteEffects.None,
                0);
        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            sprites.Draw(
                spriteImages,
                new Vector2((float)XPosition * HScaling, (float)YPosition * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(255, 255, 255, fade),
                0f,
                new Vector2(0, 0),
                new Vector2(HScaling, VScaling),
                SpriteEffects.None,
                0);
        }

        public void DrawSprite(int SpriteNumber, float XPosition, float YPosition)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            sprites.Draw(
                spriteImages,
                new Vector2((float)XPosition * HScaling, (float)YPosition * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(255, 255, 255, fade));
        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, SpritePixel[] Matrix, int BlockSize = 4, SpriteEffects SpriteEffect = SpriteEffects.None)
        {
            if (Matrix.Length == spriteWidth * spriteHeight)
            {
                float HScaling = 1f;
                float VScaling = 1f;

                byte Alpha = SpriteColour.A;
                if (Alpha == 255)
                {
                    Alpha = fade;
                }

                int x = 0;
                int y = 0;
                int counter = 0;

                int BlockMultiplier = 1;
                if (BlockSize > 1)
                {
                    BlockMultiplier = 2;
                }

                for (int a = 0; a < Matrix.Length; a += (BlockSize * BlockMultiplier))
                {
                    counter++;
                    if (Matrix[a].IsOn)
                    {
                        sprites.Draw(
                            spriteImages,
                            new Vector2((float)(XPosition + x) * HScaling, (float)(YPosition + y) * VScaling),
                            new Rectangle(spriteTexturePosX[SpriteNumber] + x, spriteTexturePosY[SpriteNumber] + y, BlockSize, BlockSize),
                            Matrix[a].Colour,
                            0f,
                            new Vector2(0, 0),
                            new Vector2(HScaling, VScaling),
                            SpriteEffect,
                            0f);
                    }
                    if (x > spriteWidth)
                    {
                        x = 0;
                        y = y + BlockSize;
                    }
                    else
                    {
                        x = x + BlockSize;
                    }
                }
            }
        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, int Width, int Height, Color SpriteColour)
        {
            float HScaling = 1f;
            float VScaling = 1f;


            sprites.Draw(
                spriteImages,
                new Vector2((float)XPosition * HScaling, (float)YPosition * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], Width, Height),
                SpriteColour,
                0f,
                new Vector2(0, 0),
                new Vector2(HScaling, VScaling),
                SpriteEffects.None,
                0);

        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            byte Alpha = SpriteColour.A;
            if (Alpha == 255)
            {
                Alpha = fade;
            }

            sprites.Draw(
                spriteImages,
                new Vector2((float)XPosition * HScaling, (float)YPosition * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
                0f,
                new Vector2(0, 0),
                new Vector2(HScaling, VScaling),
                SpriteEffects.None,
                0f);
        }

        public void DrawSpriteSubSection(int SpriteNumber, int XPosition, int YPosition, Rectangle SubSpriteArea, Color SpriteColour)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            if (spriteWidth >= SubSpriteArea.Width &&
                spriteHeight >= SubSpriteArea.Height &&
                spriteWidth >= SubSpriteArea.Width + SubSpriteArea.X &&
                spriteHeight >= SubSpriteArea.Height + SubSpriteArea.Y)
            {
                sprites.Draw(
                spriteImages,
                new Vector2((float)XPosition * HScaling, (float)YPosition * VScaling),
                new Rectangle(
                    spriteTexturePosX[SpriteNumber] + SubSpriteArea.X,
                    spriteTexturePosY[SpriteNumber] + SubSpriteArea.Y,
                    SubSpriteArea.Width,
                    SubSpriteArea.Height),
                SpriteColour,
                0f,
                new Vector2(0, 0),
                new Vector2(HScaling, VScaling),
                SpriteEffects.None,
                0);
            }

        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, float Scale, float Rotation)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            sprites.Draw(spriteImages,
               new Vector2((float)(XPosition + spriteWidth / 2) * HScaling, (float)(YPosition + spriteHeight / 2) * VScaling),
               new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
               Rotation,
               new Vector2(spriteWidth / 2, spriteHeight / 2),
               new Vector2(HScaling * Scale, VScaling * Scale),
               SpriteEffects.None,
               0);
        }

        public void DrawSprite(int SpriteNumber, float XPosition, float YPosition, Color SpriteColour, float Scale, float Rotation)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            sprites.Draw(spriteImages,
               new Vector2((float)(XPosition + spriteWidth / 2) * HScaling, (float)(YPosition + spriteHeight / 2) * VScaling),
               new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
               Rotation,
               new Vector2(spriteWidth / 2, spriteHeight / 2),
               new Vector2(HScaling * Scale, VScaling * Scale),
               SpriteEffects.None,
               0);
        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, float Scale, float Rotation, int OriginX, int OriginY)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            sprites.Draw(spriteImages,
                new Vector2((float)(XPosition + OriginX) * HScaling, (float)(YPosition + OriginY) * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
                Rotation,
                new Vector2(OriginX, OriginY),
                new Vector2(HScaling * Scale, VScaling * Scale),
                SpriteEffects.None, 0);
        }


        public void DrawSprite(int SpriteNumber, float XPosition, float YPosition, Color SpriteColour, float Scale, float Rotation, int OriginX, int OriginY)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            sprites.Draw(spriteImages,
                new Vector2((float)(XPosition + OriginX) * HScaling, (float)(YPosition + OriginY) * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
                Rotation,
                new Vector2(OriginX, OriginY),
                new Vector2(HScaling * Scale, VScaling * Scale),
                SpriteEffects.None, 0);
        }

        public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, float Scale, float RotationSpeed, bool Clockwise, bool Paused)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            clockWise = Clockwise;

            if (Paused)
            {
                rotationSpeed = 0;
            }
            else
            {
                rotationSpeed = RotationSpeed;
            }

            sprites.Draw(spriteImages,
                new Vector2((float)(XPosition + spriteWidth / 2) * HScaling, (float)(YPosition + spriteHeight / 2) * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
                MathHelper.ToRadians(spriteRotation),
                new Vector2(spriteWidth / 2, spriteHeight / 2),
                new Vector2(HScaling * Scale, VScaling * Scale),
                SpriteEffects.None, 0);
        }

        public void DrawSprite(int SpriteNumber, float XPosition, float YPosition, Color SpriteColour, float Scale, float RotationSpeed, bool Clockwise, bool Paused)
        {
            float HScaling = 1f;
            float VScaling = 1f;

            clockWise = Clockwise;

            if (Paused)
            {
                rotationSpeed = 0;
            }
            else
            {
                rotationSpeed = RotationSpeed;
            }

            sprites.Draw(spriteImages,
                new Vector2((float)(XPosition + spriteWidth / 2) * HScaling, (float)(YPosition + spriteHeight / 2) * VScaling),
                new Rectangle(spriteTexturePosX[SpriteNumber], spriteTexturePosY[SpriteNumber], spriteWidth, spriteHeight),
                 ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
                MathHelper.ToRadians(spriteRotation),
                new Vector2(spriteWidth / 2, spriteHeight / 2),
                new Vector2(HScaling * Scale, VScaling * Scale),
                SpriteEffects.None, 0);
        }

        public void End()
        {
            sprites.End();

            if (fade == 0)
            {
                CurrentSpriteEffect = enSpriteEffect.None;
                effectEnded = true;
            }
            Update();
        }

        // Updates rotational sprites
        private void Update()
        {
            if (clockWise)
            {
                if (spriteRotation < 360f)
                {
                    spriteRotation = spriteRotation + rotationSpeed;
                }
                if (spriteRotation >= 360f)
                {
                    spriteRotation = 0f;
                }
            }
            else
            {
                if (spriteRotation > 0f)
                {
                    spriteRotation = spriteRotation - rotationSpeed;
                }
                if (spriteRotation <= 0f)
                {
                    spriteRotation = 360f;
                }
            }
        }

        private Vector2 MoveInCircle(GameTime gameTime, float speed)
        {
            double time = gameTime.TotalGameTime.TotalSeconds * speed;

            float x = (float)Math.Cos(time);
            float y = (float)Math.Sin(time);

            return new Vector2(x, y);
        }

        private float Pulsate(double counter, float speed, float min, float max)
        {
            return min + ((float)Math.Sin(counter) + 1) / 2 * (max - min);
        }

        public override string ToString()
        {
            return "SpriteCollection, Effect Ended: " + effectEnded.ToString();
        }

        public SpriteCollection Clone(GameEngine GameEngineReference)
        {
            SpriteCollection ClonedSprites = SharedLibs.BinarySerialiser.SerialiseCloneBinary(this);

            ClonedSprites.gameEngineReference = GameEngineReference;
            ClonedSprites.sprites = new SpriteBatch(gameEngineReference.GraphicsDeviceMgr.GraphicsDevice);
            ClonedSprites.spriteImages = gameEngineReference.ContentMgr.Load<Texture2D>(ClonedSprites.imageFilePath);

            return ClonedSprites;
        }
    }
}
