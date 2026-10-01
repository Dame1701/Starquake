// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Particles;

// A particle effect that can be initialised from multiple positions with their own durational lifetimes, an example are the post fire sparks
internal interface IPositionalTriggerParticleEffect : IParticleEffect
{
    void Start(int xPos, int yPos);
    void Update(GameTime gameTime);
}
