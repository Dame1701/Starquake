using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using StarquakeXNA.GameComponents;
using Microsoft.Xna.Framework;
using StarquakeXNA.ContentManagement;

namespace StarquakeXNA
{
    public delegate void RaiseScreenChanged(enGameScreen TriggerScreen);
    public interface IGameComponent
    {
        event RaiseScreenChanged OnScreenChanged;
        void LoadContent(ContentList Content);
        void Initialise();
        void PerformPostChangeScreenActions();
        void Draw(GameTime gameTime, int ScreenShakeXOffset, int ScreenShakeYOffset, int ScreenFade);
    }

    public interface IGameScreen
    {
        int ScreenFade
        {
            get;
            set;
        }

        bool Success
        {
            get;
        }

        void Update(GameTime gameTime);
        void Reset(object Data);
        void Draw(GameTime gameTime);
        void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param);
    }

    /// <summary>
    /// Game screens that need to carry on updating even when they're not active
    /// </summary>
    public interface IRealTimeGameScreen : IGameScreen
    {
        void UpdateOffScreen(GameTime gameTime);
    }
}
