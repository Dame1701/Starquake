// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Dialogs;

/// <summary>What somebody chose when asked about unsaved work.</summary>
internal enum ConfirmChoice
{
    Save,
    Discard,
    Cancel
}

/// <summary>
///     The editor's prompts: "you have unsaved changes", and the warnings it gives on the way to
///     saving.
/// </summary>
/// <remarks>
///     Avalonia has no message box, so this is built here rather than taking a dependency for one.
///     The unsaved-work form offers three choices, because losing an afternoon of map work to a
///     stray Cmd+W is the worst thing this tool could do. A warning has nothing to discard, so it
///     offers two.
/// </remarks>
internal sealed class ConfirmDialog : Window
{

    private ConfirmChoice _choice = ConfirmChoice.Cancel;

    private ConfirmDialog(string message, bool offerDiscard, string confirmText)
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

        if (offerDiscard)
        {
            buttons.Children.Add(CreateButton("Discard", ConfirmChoice.Discard));
        }

        Button save = CreateButton(confirmText, ConfirmChoice.Save);
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

    /// <summary>Asks what to do about work that has not been saved.</summary>
    public static Task<ConfirmChoice> AskAsync(Window owner, string message)
    {
        var dialog = new ConfirmDialog(message, true, "Save");

        return dialog.ShowDialog<ConfirmChoice>(owner);
    }

    /// <summary>
    ///     Warns about something and asks whether to go ahead. The choice is between saving as things
    ///     are and going back to fix them, so it is the same window with one button fewer.
    /// </summary>
    public static async Task<bool> WarnAsync(Window owner, string message)
    {
        var dialog = new ConfirmDialog(message, false, "Save anyway");

        return await dialog.ShowDialog<ConfirmChoice>(owner) == ConfirmChoice.Save;
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
