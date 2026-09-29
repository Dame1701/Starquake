// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;

namespace Starquake.Structure;

public delegate void RaiseScreenChanged(GameScreenType triggerScreen);

public interface IGameComponent
{
    event RaiseScreenChanged OnScreenChanged;
    void LoadContent(ContentList content);
    void Initialise();
    void PerformPostChangeScreenActions();
    void Draw(GameTime gameTime, int screenShakeXOffset, int screenShakeYOffset, int screenFade);
}

public interface IGameScreen
{
    int ScreenFade { get; set; }

    bool Success { get; }

    void Update(GameTime gameTime);
    void Reset(object data);
    void Draw(GameTime gameTime);
    void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param);
}

/// <summary>
///     Game screens that need to carry on updating even when they're not active
/// </summary>
public interface IRealTimeGameScreen : IGameScreen
{
    void UpdateOffScreen(GameTime gameTime);
}