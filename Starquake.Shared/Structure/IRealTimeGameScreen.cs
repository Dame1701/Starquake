// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Structure;

/// <summary>
///     Game screens that need to carry on updating even when they're not active
/// </summary>
public interface IRealTimeGameScreen : IGameScreen
{
    void UpdateOffScreen(GameTime gameTime);
}