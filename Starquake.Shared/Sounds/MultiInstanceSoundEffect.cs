// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;

namespace Starquake.Sounds;

public sealed record MultiInstanceSoundEffect(SoundEffect Sound, int MaxInstances, bool KillOnScreenChange);
