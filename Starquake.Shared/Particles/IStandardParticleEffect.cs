// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Particles;

// All standard triggered and continual with and without duration particle effects
public interface IStandardParticleEffect : IParticleEffect
{
    void Start();
    void Update(int xPos, int yPos, GameTime gameTime);
}
