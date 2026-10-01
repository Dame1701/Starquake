// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Structure;

/// <summary>
///     What the engine's frame loop needs of whichever screen is showing: step it, then draw it.
///     <para>
///         Nothing else belongs here. This used to carry <c>Reset</c>, <c>ScreenFade</c> and
///         <c>PerformPostScreenChangeActions</c> as well, none of which the frame loop calls - they
///         are a screen handing over to one other screen it names. Every one of those call sites
///         names its target statically, so the only thing putting them on the interface achieved was
///         forcing eleven members onto screens that have no such thing: six empty <c>Reset</c>
///         bodies, two empty and two throwing <c>PerformPostScreenChangeActions</c>, and a
///         <c>ScreenFade</c> on the planet core that threw from both its getter and its setter.
///     </para>
/// </summary>
internal interface IGameScreen
{
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}
