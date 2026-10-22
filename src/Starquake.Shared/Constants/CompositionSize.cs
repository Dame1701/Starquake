// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Constants;

/// <summary>
///     The size the game composes its picture at. Nothing draws to the back buffer directly:
///     everything is laid out against these two numbers and <c>PresentationSettings.Transform</c>
///     scales the result onto whatever the back buffer is, so the game looks the same in a small
///     window as it does fullscreen on a 6K display.
/// </summary>
internal static class CompositionSize
{
    /// <summary>The width every screen, panel and overlay is positioned against.</summary>
    public const int Width = 1280;

    /// <inheritdoc cref="Width" />
    public const int Height = 800;
}
