// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Starquake.ScreenEditor.Styles;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Dialogs;

/// <summary>
///     What somebody chose for a screen's teleport code. A null <see cref="Code" /> is the booth
///     left with no code at all, which is different from the dialog being cancelled - that comes
///     back as no choice.
/// </summary>
internal sealed record TeleportCodeChoice(string? Code);

/// <summary>
///     Sets the destination code of one screen's teleport booth.
/// </summary>
/// <remarks>
///     The code is what has to be typed into any other booth on the planet to arrive at this screen,
///     so the two things worth saying while it is being typed are whether the booth could spell it at
///     all and whether another screen already answers to it. Both are the session's to answer - it is
///     the one holding the map - so this takes a function and reports whatever comes back.
/// </remarks>
internal sealed class TeleportCodeDialog : Window
{

    private readonly TextBox _entry;

    private readonly TextBlock _problem;

    private readonly Button _set;

    private readonly Func<string, string?> _describeProblem;

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
            // Held open whether there is a problem to report or not: the window sizes itself to its
            // content, and a line that comes and goes while somebody is typing takes the buttons
            // with it.
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

        // Only offered when there is something to remove, so that the one destructive button here is
        // never the obvious thing to press on a booth that has no code yet.
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

    /// <summary>
    ///     Asks for a code, coming back with null if the dialog was cancelled.
    /// </summary>
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
    ///     Opens with whatever code the booth already had selected, so that typing a new one over it
    ///     needs no clearing out first.
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
    ///     Holds what is being typed in upper case, which is how a code is stored and how the booth
    ///     spells it out on its monitor. Assigning the text moves the caret to the end, so where it
    ///     was is put back.
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
