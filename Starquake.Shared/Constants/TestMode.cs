// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     Whether the debug keys are live. F1 kills the blob, F2 refills its lives, and the rest of
///     the F-keys around them jump the game about for testing.
///     <para>
///         Compiled in rather than switched at runtime, and on. This was a
///         <c>public readonly bool</c> on the engine assigned <c>true</c> at its declaration and
///         nowhere else in the solution - a constant wearing a field's clothing, which is the last
///         of the group stage 1 was about.
///     </para>
/// </summary>
public static class TestMode
{
    /// <summary>
    ///     Whether <c>HandleTestModeKeys</c> is called each frame.
    /// </summary>
    public const bool Enabled = true;
}
