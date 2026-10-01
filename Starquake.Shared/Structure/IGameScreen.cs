// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Structure;

public interface IGameScreen
{
    int ScreenFade { get; set; }

    void Update(GameTime gameTime);
    void Reset(object? data);
    void Draw(GameTime gameTime);
    /// <summary>
    ///     Called on a screen once it has been handed control, with whatever the screen handing over
    ///     had to tell it.
    /// </summary>
    void PerformPostScreenChangeActions(GameScreenType triggeringScreen, ScreenChange change);
}
