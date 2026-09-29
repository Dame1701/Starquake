// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Engines;

namespace Starquake.Structure;

public abstract class GameComponentBase
{
    protected GameEngine GameEngine;

    public GameComponentBase(GameEngine gameEngineReference)
    {
        GameEngine = gameEngineReference;
    }

    public virtual void LoadContent()
    {
    }

    public virtual void Initialise(object data)
    {
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(GameTime gameTime)
    {
    }

    public virtual void Draw(GameTime gameTime, int screenFade, object parameters)
    {
    }

    public virtual void Draw(GameTime gameTime, int screenFade)
    {
    }
}