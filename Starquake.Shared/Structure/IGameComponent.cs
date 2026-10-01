// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;

namespace Starquake.Structure;

internal interface IGameComponent
{
    event RaiseScreenChanged OnScreenChanged;
    void LoadContent(ContentList content);
    void Initialise();
    void PerformPostChangeScreenActions();
    void Draw(GameTime gameTime, int screenShakeXOffset, int screenShakeYOffset, int screenFade);
}
