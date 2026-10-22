// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace Starquake.Core;

/// <summary>
///     What the engine's frame loop needs of whichever screen is showing: step it, then draw it.
///     <para>
///         Nothing else belongs here. Handing over between screens is one screen naming another
///         statically, so putting those members here would only force empty or throwing
///         implementations onto screens that have no such thing.
///     </para>
/// </summary>
internal interface IGameScreen
{
    void Update(GameTime gameTime);
    void Draw();
}
