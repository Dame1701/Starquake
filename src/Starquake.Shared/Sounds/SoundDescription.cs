// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

namespace Starquake.Sounds;

internal sealed record SoundDescription(
    string ResourceName,
    string FilePath,
    SoundEffectInstanceType InstanceType,
    int MaxInstances,
    bool KillOnScreenChange);
