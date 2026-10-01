// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The size the game composes its picture at. Nothing in the game draws to the back buffer
///     directly: everything is laid out against these two numbers and
///     <c>PresentationSettings.Transform</c> scales the result onto whatever the back buffer
///     happens to be, so the game looks the same in a small window as it does fullscreen on a 6K
///     display.
///     <para>
///         These lived on <c>GameHost</c>, which opens its window at this size and so has a claim
///         on them - but sixteen of the eighteen places that read them are in the game rather than
///         the host, and the game should not have to name its host to find out how big it is.
///     </para>
/// </summary>
internal static class CompositionSize
{
    /// <summary>
    ///     The width every screen, panel and overlay is positioned against.
    /// </summary>
    public const int Width = 1280;

    /// <inheritdoc cref="Width" />
    public const int Height = 800;
}
