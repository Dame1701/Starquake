using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StarquakeXNA.GameComponents;
using StarquakeXNA.Structure;

namespace StarquakeXNA
{
    public class RenderViewPort
    {
        private GameEngine gameEngine;

        private RenderTarget2D renderTarget;
        public RenderTarget2D RenderTarget
        {
            get { return renderTarget; }
        }

        private bool active;
        public bool Active
        {
            get { return active; }
            set { active = value; }
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

        private int masterXPos;
        public int MasterXPos
        {
            get { return masterXPos; }
        }

        private int masterYPos;
        public int MasterYPos
        {
            get { return masterYPos; }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        private int height;
        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public Rectangle GetBounds()
        {
            return new Rectangle(XPos, YPos, Width, Height);
        }

        public int MasterWidth
        {
            get { return renderTarget.Width; }
        }

        public int MasterHeight
        {
            get { return renderTarget.Height; }
        }

        private string name;
        public string Name
        {
            get { return name; }
        }

        public RenderViewPort(GameEngine GameEngineReference, string Name, int XPos, int YPos, int Width, int Height)
        {
            gameEngine = GameEngineReference;

            masterXPos = XPos;
            masterYPos = YPos;

            width = Width;
            height = Height;

            renderTarget = new RenderTarget2D(gameEngine.GraphicsDeviceMgr.GraphicsDevice, Width, Height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            

            xPos = XPos;
            yPos = YPos;
            name = Name;
            active = false;
        }
    }

    public class ScreenEffectEngine : GameComponentBase
    {
        List<RenderViewPort> renderViewPorts;
        List<ScreenEffectBase> screenEffects;


        bool haltBlobAndNasties;
        public bool HaltBlobAndNasties
        {
            get { return haltBlobAndNasties; }
        }

        ScreenEffectBase activeScreenEffect;
        public ScreenEffectBase ActiveScreenEffect
        {
            get { return activeScreenEffect; }
        }

        public enScreenEffectType CurrentScreenEffect
        {
            get
            {
                if (activeScreenEffect != null)
                {
                    return activeScreenEffect.ScreenEffectType;
                }

                return enScreenEffectType.None;
            }
        }

        SpriteBatch renderer;
        RenderViewPort activeViewPort;

        int screenWidth;
        int screenHeight;

        float horizontalScale;
        float verticalScale;

        public ScreenEffectEngine(
            int ScreenWidth,
            int ScreenHeight,
            GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            horizontalScale = 1f;
            verticalScale = 1f;

            screenWidth = ScreenWidth;
            screenHeight = ScreenHeight;

            renderViewPorts = new List<RenderViewPort>();
            screenEffects = new List<ScreenEffectBase>();

            // Add all of the screen effects we can find - do this eventually through reflection but for now - manually!
            screenEffects.Add(new LeftTunnelEntryEffect(gameEngine));
            screenEffects.Add(new RightTunnelEntryEffect(gameEngine));
            screenEffects.Add(new AddictiveFade(gameEngine));

            renderer = new SpriteBatch(gameEngine.GraphicsDeviceMgr.GraphicsDevice);
            haltBlobAndNasties = false;
        }

        /// <summary>
        /// Rather than adding a viewport this sets up a new Render2D target at the size of the viewport we want, we also store the location so we can draw correctly
        /// </summary>
        /// <param name="?"></param>
        public void RegisterViewPort(string Name,
            int XPosition,
            int YPosition,
            int Width,
            int Height)
        {
            if (GetRenderViewPort(Name) == null)
            {
                renderViewPorts.Add(new RenderViewPort(gameEngine, Name, XPosition, YPosition, Width, Height));
            }
            else
            {
                throw new Exception("A RenderViewPort with the same name already exists");
            }
        }

        public void SelectRenderViewPort(string Name, bool ClearScreen = true)
        {
            RenderViewPort selectedViewPort = GetRenderViewPort(Name);

            if (selectedViewPort != null)
            {
                selectedViewPort.Active = true;
                activeViewPort = selectedViewPort;
                gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(selectedViewPort.RenderTarget);

                if (ClearScreen)
                {
                    gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);
                }
            }
            else
            {
                throw new Exception("Could not find a RenderViewPort with the same name '" + Name + "'");
            }
        }

        public void MoveViewPort(string Name, int XOffset, int YOffset)
        {
            RenderViewPort viewPort = GetRenderViewPort(Name);
            viewPort.XPos = viewPort.MasterXPos + XOffset;
            viewPort.YPos = viewPort.MasterYPos + YOffset;
        }

        public RenderViewPort GetRenderViewPort(string Name)
        {
            for (int a = 0; a < renderViewPorts.Count; a++)
            {
                if (renderViewPorts[a].Name.ToLower() == Name.ToLower())
                {
                    return renderViewPorts[a];
                }
            }

            return null;
        }

        public void ExecuteScreenEffect(enScreenEffectType ScreenEffectToExecute, int EffectSpeed, int EffectDuration, bool HaltBlobAndNasties)
        {
            // Only allow one effect to execute at a time - may need to implement an overrride system....
            for (int a = 0; a < screenEffects.Count; a++)
            {
                screenEffects[a].Reset();
                activeScreenEffect = null;
            }

            for (int a = 0; a < screenEffects.Count; a++)
            {
                if (screenEffects[a].ScreenEffectType == ScreenEffectToExecute)
                {
                    activeScreenEffect = screenEffects[a];
                    activeScreenEffect.TriggerEffect(GetRenderViewPort("GameArea"), EffectSpeed, EffectDuration);
                    haltBlobAndNasties = HaltBlobAndNasties;
                    break;
                }
            }
        }

        public int GetParticleX(int XPos)
        {
            return XPos;
        }

        public int GetParticleY(int YPos)
        {
            return YPos;
        }

        public void Update()
        {
            if (activeScreenEffect != null)
            {
                activeScreenEffect.Update();

                if (activeScreenEffect.EffectFinished)
                {
                    haltBlobAndNasties = false;
                    activeScreenEffect = null;
                }
            }
        }

        public void CalculateScaling(int Width, int Height)
        {
            horizontalScale = (float)Width / (float)screenWidth;
            verticalScale = (float)Height / (float)screenHeight;
        }

        public void Draw()
        {
            // Select the back buffer
            gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(null);

            // Clear the screen
            gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);

            for (int a = 0; a < renderViewPorts.Count; a++)
            {
                if (renderViewPorts[a].Active)
                {
                    // Only apply effects to the game area - might change this so we can apply different ones to different ports...
                    if (renderViewPorts[a].Name.ToLower() == "gamearea")
                    {
                        // Run the alternative renderer if we need to
                        bool useDefaultRenderer = true;

                        if (activeScreenEffect != null)
                        {
                            useDefaultRenderer = activeScreenEffect.Draw(renderer, renderViewPorts[a]);
                        }

                        // We're using the default renderer so lets render
                        if (useDefaultRenderer)
                        {
                            renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque);

                            renderer.Draw(renderViewPorts[a].RenderTarget, new Rectangle(
                                    renderViewPorts[a].XPos,
                                    renderViewPorts[a].YPos,
                                    renderViewPorts[a].Width,
                                   renderViewPorts[a].Height), Color.White);

                            renderer.End();
                        }
                    }
                    else
                    {
                        renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque);

                        renderer.Draw(renderViewPorts[a].RenderTarget, new Rectangle(
                                    renderViewPorts[a].XPos,
                                    renderViewPorts[a].YPos,
                                    renderViewPorts[a].Width,
                                   renderViewPorts[a].Height), Color.White);

                        renderer.End();
                    }

                    renderViewPorts[a].Active = false;
                }
            }

        }
    }
}
