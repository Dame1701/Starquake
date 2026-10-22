// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace Starquake.Particles;

// Every particle effect triggered from one position, continuous or not, with or without a duration.
internal interface IStandardParticleEffect : IParticleEffect
{
    void Start();
    void Update(int xPos, int yPos, GameTime gameTime);
}
