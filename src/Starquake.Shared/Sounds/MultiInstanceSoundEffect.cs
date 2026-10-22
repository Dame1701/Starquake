// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework.Audio;

namespace Starquake.Sounds;

internal sealed record MultiInstanceSoundEffect(SoundEffect Sound, int MaxInstances, bool KillOnScreenChange);
