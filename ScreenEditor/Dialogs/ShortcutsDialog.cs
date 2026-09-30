// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Dialogs;

/// <summary>
///     The keyboard and pointer shortcuts, listed.
/// </summary>
/// <remarks>
///     Everything here is also on a tooltip, but tooltips only answer a question somebody already
///     knew to ask. The shortcuts that make this quick to use - holding Alt to pick a tile, dragging
///     the world map to scrub through screens - are the ones nobody would think to hover over.
/// </remarks>
public sealed class ShortcutsDialog : Window
{

    private static readonly (string Group, string Keys, string Meaning)[] Shortcuts =
    [
        ("Tools", "B / E / F / I", "Paint, erase, fill, pick"),
        ("Tools", "Right button", "Erase, whichever tool is selected"),
        ("Tools", "Alt and click", "Pick the tile under the pointer"),
        ("Tools", "Drag", "Paint or erase a trail of tiles"),

        ("View", "G / C / T / N", "Grid, collision map, tile numbers, neighbours"),
        ("View", "Ctrl and wheel", "Resize the tiles in the palette"),

        ("Moving about", "Arrow keys", "Go to the next screen in that direction"),
        ("Moving about", "Click the map", "Go to that screen; drag to scrub through them"),

        ("Map", "Ctrl+Z / Ctrl+Shift+Z", "Undo, redo"),
        ("Map", "Ctrl+C / Ctrl+V", "Copy and paste a whole screen"),
        ("Map", "Ctrl+S", "Save")
    ];

    public ShortcutsDialog()
    {
        Title = "Keyboard shortcuts";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(EditorColours.Panel);

        var rows = new StackPanel { Spacing = 2 };
        var lastGroup = string.Empty;

        foreach (var (group, keys, meaning) in Shortcuts)
        {
            if (group != lastGroup)
            {
                lastGroup = group;

                rows.Children.Add(new TextBlock
                {
                    Text = group.ToUpperInvariant(),
                    Classes = { "header" },
                    Margin = new Thickness(0, rows.Children.Count == 0 ? 0 : 16, 0, 4)
                });
            }

            rows.Children.Add(CreateRow(keys, meaning));
        }

        var close = new Button
        {
            Content = "Close",
            MinWidth = 90,
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Classes = { "primary" }
        };

        close.Click += (_, _) => Close();
        rows.Children.Add(close);

        Content = new ScrollViewer
        {
            Content = new Border
            {
                Padding = new Thickness(24),
                Child = rows
            }
        };
    }

    private static Control CreateRow(string keys, string meaning)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("170,*")
        };

        var keyText = new TextBlock
        {
            Text = keys,
            Foreground = EditorColours.AccentBrush,
            FontSize = 12
        };

        var meaningText = new TextBlock
        {
            Text = meaning,
            Foreground = EditorColours.TextBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };

        Grid.SetColumn(meaningText, 1);

        row.Children.Add(keyText);
        row.Children.Add(meaningText);

        return row;
    }
}
