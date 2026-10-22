// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Avalonia.Media;

namespace Starquake.ScreenEditor.Styles;

/// <summary>
///     The editor's palette, in one place.
/// </summary>
/// <remarks>
///     The editing surfaces are drawn by hand rather than built from controls, so their colours
///     cannot come from a style the way the window chrome's do. Both read them from here, the drawing
///     code directly and the XAML through <c>x:Static</c>, so the chrome and the canvas cannot drift
///     apart.
/// </remarks>
internal static class EditorColours
{

    /// <summary>
    ///     The empty space a screen is drawn on. Not black on purpose: the artwork has plenty of
    ///     black in it, and a dark tile should still read as a tile.
    /// </summary>
    public static readonly Color Void = Color.FromRgb(0x0B, 0x0D, 0x10);

    /// <summary>
    ///     The lighter square of the chequer filling an empty tile position, so an unpainted part of a
    ///     screen cannot be mistaken for dark scenery.
    /// </summary>
    private static readonly Color VoidChequer = Color.FromRgb(0x14, 0x17, 0x1C);

    public static readonly Color Background = Color.FromRgb(0x0E, 0x10, 0x14);

    public static readonly Color Panel = Color.FromRgb(0x17, 0x1A, 0x20);

    public static readonly Color PanelRaised = Color.FromRgb(0x1F, 0x23, 0x2A);

    public static readonly Color Border = Color.FromRgb(0x2B, 0x31, 0x3A);

    public static readonly Color Text = Color.FromRgb(0xE4, 0xE8, 0xEF);

    public static readonly Color Muted = Color.FromRgb(0x8A, 0x92, 0x9E);

    /// <summary>
    ///     The highlight colour. Amber appears nowhere in the tile artwork, so a selection stays
    ///     visible over every tile in the sheet.
    /// </summary>
    public static readonly Color Accent = Color.FromRgb(0xFF, 0xB4, 0x3C);

    public static readonly Color AccentDim = Color.FromRgb(0x6B, 0x4C, 0x18);

    /// <summary>
    ///     The editor's stand-in panels for tiles the game never draws. A cool grey-blue, which sits
    ///     back behind the artwork instead of competing with it like the sheet's flat magenta.
    /// </summary>
    public static readonly Color Marker = Color.FromRgb(0x7F, 0x9A, 0xC4);

    /// <summary>Solid parts of a tile, when the collision overlay is on.</summary>
    private static readonly Color Collision = Color.FromRgb(0xFF, 0x4D, 0x5E);

    public static readonly IBrush VoidBrush = new SolidColorBrush(Void);

    public static readonly IBrush VoidChequerBrush = new SolidColorBrush(VoidChequer);

    public static readonly IBrush TextBrush = new SolidColorBrush(Text);

    public static readonly IBrush MutedBrush = new SolidColorBrush(Muted);

    public static readonly IBrush AccentBrush = new SolidColorBrush(Accent);

    public static readonly IBrush CollisionBrush = new SolidColorBrush(Collision, 0.38);

    public static readonly IBrush HoverBrush = new SolidColorBrush(Colors.White, 0.14);

    /// <summary>Grid lines. Faint enough to read a screen through, strong enough to count by.</summary>
    public static readonly IPen GridPen = new Pen(new SolidColorBrush(Colors.White, 0.13));

    /// <summary>
    ///     The finer grid collision is tested on, shown only while that overlay is up.
    /// </summary>
    public static readonly IPen CollisionGridPen = new Pen(new SolidColorBrush(Collision, 0.25));

    public static readonly IPen AccentPen = new Pen(AccentBrush, 2);

    public static readonly IPen BorderPen = new Pen(new SolidColorBrush(Border));

    /// <summary>
    ///     The edge of the screen being edited, drawn over the dimmed neighbours so it is clear which
    ///     tiles on show can be painted on.
    /// </summary>
    public static readonly IPen ScreenEdgePen = new Pen(new SolidColorBrush(Colors.White, 0.55), 1.5);
}
