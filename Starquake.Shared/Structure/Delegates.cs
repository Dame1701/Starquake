// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public delegate void RaiseEngineComponentStatusChanged(string Message, int PercentComplete, GameEngineStatus Status);

public delegate void RaiseInteractionWithBlob(enNastyInterationType InteractionType, enNastyType NastyType);

public delegate void RaiseEffectComplete();