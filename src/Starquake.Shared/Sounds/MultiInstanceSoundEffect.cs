// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;

namespace Starquake.Sounds;

internal sealed record MultiInstanceSoundEffect(SoundEffect Sound, int MaxInstances, bool KillOnScreenChange);
