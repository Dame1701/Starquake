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
    void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param);
}
