// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Core;

namespace Starquake.Sounds;

/// <summary>
///     One piece of music and the screen it belongs to.
///     <para>
///         Keyed by the screen rather than by a registered name, which is how every other asset is
///         reached. A song is wanted by exactly one screen and nothing asks for one by name, so the
///         screen is the only name it needs, and a <see cref="GameScreenType" /> cannot be mistyped.
///     </para>
/// </summary>
internal sealed record MusicDescription(GameScreenType Screen, string FilePath);
