// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

using Starquake.Core;
namespace Starquake.Sounds;

internal sealed record SoundDescription(
    string ResourceName,
    string FilePath,
    SoundEffectInstanceType InstanceType,
    int MaxInstances,
    bool KillOnScreenChange);
