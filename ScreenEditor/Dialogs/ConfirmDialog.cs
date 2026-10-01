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
///     What somebody chose when asked about unsaved work.
/// </summary>
internal enum ConfirmChoice
{
    Save,
    Discard,
    Cancel
}

/// <summary>
///     The "you have unsaved changes" prompt.
/// </summary>
/// <remarks>
///     Avalonia has no message box of its own, and this is the only prompt the editor needs, so it
///     is built here rather than by taking on a dependency for it. It is deliberately a three way
///     choice: losing an afternoon of map work to a stray Cmd+W would be the single worst thing this
///     tool could do.
/// </remarks>
internal sealed class ConfirmDialog : Window
{

    private ConfirmChoice _choice = ConfirmChoice.Cancel;

    private ConfirmDialog(string message)
    {
        Title = "Starquake Screen Editor";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(EditorColours.Panel);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 20, 0, 0)
        };

        buttons.Children.Add(CreateButton("Cancel", ConfirmChoice.Cancel));
        buttons.Children.Add(CreateButton("Discard", ConfirmChoice.Discard));

        Button save = CreateButton("Save", ConfirmChoice.Save);
        save.Classes.Add("primary");
        save.IsDefault = true;
        buttons.Children.Add(save);

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = EditorColours.TextBrush
                },
                buttons
            }
        };
    }

    public static Task<ConfirmChoice> AskAsync(Window owner, string message)
    {
        var dialog = new ConfirmDialog(message);

        return dialog.ShowDialog<ConfirmChoice>(owner);
    }

    private Button CreateButton(string text, ConfirmChoice choice)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 90,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        button.Click += (_, _) =>
        {
            _choice = choice;
            Close(_choice);
        };

        return button;
    }
}
