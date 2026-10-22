// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Starquake.ScreenEditor.Styles;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Dialogs;

/// <summary>
///     What somebody chose for a screen's teleport code. A null <see cref="Code" /> means the booth
///     was left with no code. Cancelling the dialog comes back as no choice at all.
/// </summary>
internal sealed record TeleportCodeChoice(string? Code);

/// <summary>
///     Sets the destination code of one screen's teleport booth.
/// </summary>
/// <remarks>
///     While a code is being typed, two things are worth reporting: whether a booth could spell it at
///     all, and whether another screen already answers to it. Only the session holds the map, so it
///     is passed in as a function and this reports whatever comes back.
/// </remarks>
internal sealed class TeleportCodeDialog : Window
{

    private readonly Func<string, string?> _describeProblem;

    private readonly TextBox _entry;

    private readonly TextBlock _problem;

    private readonly Button _set;

    private TeleportCodeDialog(int screen, string? existing, Func<string, string?> describeProblem)
    {
        _describeProblem = describeProblem;

        Title = "Teleport code";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(EditorColours.Panel);

        _entry = new TextBox
        {
            Text = existing ?? string.Empty,
            MaxLength = TeleportCodes.CodeLength,
            Watermark = "FIVE LETTERS",
            FontSize = 15,
            Margin = new Thickness(0, 14, 0, 0)
        };

        _entry.TextChanged += (_, _) => KeepUpperCase();

        _problem = new TextBlock
        {
            Foreground = EditorColours.AccentBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
            // Kept in place whether or not there is a problem to report. The window sizes itself to
            // its content, so a line that appears and disappears would move the buttons while
            // somebody is typing.
            MinHeight = 18
        };

        _set = new Button
        {
            Content = "Set code",
            MinWidth = 90,
            IsDefault = true,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Classes =
            {
                "primary"
            }
        };

        _set.Click += (_, _) => Close(new TeleportCodeChoice(_entry.Text));

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 20, 0, 0)
        };

        buttons.Children.Add(CreateButton("Cancel", null));

        // Only offered when there is something to remove, so the destructive button is never the
        // obvious thing to press on a booth that has no code yet.
        if (existing is not null)
        {
            buttons.Children.Add(CreateButton("Remove", new TeleportCodeChoice(null)));
        }

        buttons.Children.Add(_set);

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Children =
            {
                new TextBlock
                {
                    Text = "Typed into any teleport booth, this code sends the blob to screen " + screen + ".",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = EditorColours.TextBrush
                },
                _entry,
                _problem,
                buttons
            }
        };

        KeepUpperCase();
    }

    /// <summary>Asks for a code. Returns null if the dialog was cancelled.</summary>
    public static Task<TeleportCodeChoice?> AskAsync(
        Window owner,
        int screen,
        string? existing,
        Func<string, string?> describeProblem)
    {
        var dialog = new TeleportCodeDialog(screen, existing, describeProblem);

        return dialog.ShowDialog<TeleportCodeChoice?>(owner);
    }

    /// <summary>
    ///     Selects the booth's existing code on opening, so typing a new one replaces it without
    ///     clearing first.
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        _entry.SelectAll();
        _entry.Focus();
    }

    private Button CreateButton(string text, TeleportCodeChoice? choice)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 90,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        button.Click += (_, _) => Close(choice);

        return button;
    }

    /// <summary>
    ///     Forces what is being typed to upper case, which is how a code is stored and shown.
    ///     Assigning the text moves the caret to the end, so its position is restored afterwards.
    /// </summary>
    private void KeepUpperCase()
    {
        string typed = _entry.Text ?? string.Empty;
        string upperCase = typed.ToUpperInvariant();

        if (upperCase != typed)
        {
            int caret = _entry.CaretIndex;

            _entry.Text = upperCase;
            _entry.CaretIndex = caret;
        }

        ShowProblem();
    }

    private void ShowProblem()
    {
        string? problem = _describeProblem(_entry.Text ?? string.Empty);

        _problem.Text = problem ?? string.Empty;
        _set.IsEnabled = problem is null;
    }
}
