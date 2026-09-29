// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace StarquakeXNA.Structure;

public delegate void RaiseEngineComponentStatusChanged(string message, int percentComplete, GameEngineStatus status);

public delegate void RaiseInteractionWithBlob(NastyInterationType interactionType, NastyType nastyType);

public delegate void RaiseEffectComplete();