// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using ProjectMercury;
using Starquake.Structure;

namespace Starquake.Particles;

public interface IParticleEffect
{
    ParticleEffect Effect { get; }
    bool EffectActive { get; }
    event RaiseEffectComplete? ParticleEffectComplete;
    void Stop();
    void Reset();
    void Draw();
    void Draw(int screenFade);
}