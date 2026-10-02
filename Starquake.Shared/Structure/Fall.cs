// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     What one frame of falling came to.
/// </summary>
/// <param name="Distance">How far down he goes this frame, in pixels.</param>
/// <param name="Audible">
///     Whether the fall has gone on long enough to be worth hearing. True on every frame of a long fall,
///     not just the first - the sound is asked for repeatedly and the sound engine keeps one instance.
/// </param>
/// <param name="OnTheGround">
///     Whether there was nothing under him to fall through at the start of the frame, which is what
///     stops the falling sound.
/// </param>
internal readonly record struct Fall(int Distance, bool Audible, bool OnTheGround);
