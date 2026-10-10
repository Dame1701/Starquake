// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Particles;

// A particle effect that can be triggered at several positions at once, each with its own lifetime.
// The sparks a shot throws on impact are one.
internal interface IPositionalTriggerParticleEffect : IParticleEffect
{
    void Start(int xPos, int yPos);
    void Update(GameTime gameTime);
}
