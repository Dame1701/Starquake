// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Structure;

/// <summary>
///     The frame a game component is driven through: content load, initialise, update, draw. Every
///     method is a no-op here, so a component overrides only the parts of the cycle it takes part
///     in.
///     <para>
///         This used to hold a <c>GameEngine</c> it never called a member on, purely so its
///         subclasses could reach one. That made the god object reachable from 22 classes without
///         any of them naming the dependency, and meant none of them could be narrowed alone -
///         dropping one subclass's reference meant changing the base constructor, which touched
///         every sibling at once. The components now hold their own, so what each one needs is
///         visible in its own constructor and can be narrowed on its own.
///     </para>
/// </summary>
internal abstract class GameComponentBase
{
    public virtual void LoadContent()
    {
    }

    public virtual void Initialise(object? data)
    {
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(GameTime gameTime)
    {
    }

    public virtual void Draw(GameTime gameTime, int screenFade, object? parameters)
    {
    }

    public virtual void Draw(GameTime gameTime, int screenFade)
    {
    }
}
