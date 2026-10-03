// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Dialogs;

/// <summary>
///     What somebody chose for a screen's signpost. A null <see cref="Screen" /> is a sign left
///     blank, which is different from the dialog being cancelled - that comes back as no choice.
/// </summary>
internal sealed record SignpostChoice(int? Screen);

/// <summary>
///     Points the signposts on a screen at the teleporter whose code they are to spell out.
/// </summary>
/// <remarks>
///     A sign is only ever pointed at a teleporter that already exists, so this offers the codes on
///     the map rather than a box to type one into. Typing one would let a sign advertise a code no
///     booth answers to, which is a dead end the player cannot tell from a live one until they have
///     walked to a booth and typed it in.
/// </remarks>
internal sealed class SignpostDialog : Window
{

    private readonly ListBox _destinations;

    private SignpostDialog(int screen, int? existing, IReadOnlyList<TeleportDestination> destinations)
    {
        Title = "Signpost";
        Width = 420;
        Height = 480;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(EditorColours.Panel);

        _destinations = new ListBox
        {
            ItemsSource = destinations,
            SelectedItem = destinations.FirstOrDefault(destination => destination.Screen == existing),
            Margin = new Thickness(0, 14, 0, 0),
            ItemTemplate = new FuncDataTemplate<TeleportDestination>((destination, _) =>
                new TextBlock
                {
                    Text = destination.Code + "   ·   screen " + destination.Screen,
                    FontSize = 15
                })
        };

        _destinations.DoubleTapped += (_, _) => Accept();

        var set = new Button
        {
            Content = "Put on sign",
            MinWidth = 100,
            IsDefault = true,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Classes =
            {
                "primary"
            }
        };

        set.Click += (_, _) => Accept();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 20, 0, 0)
        };

        buttons.Children.Add(CreateButton("Cancel", null));

        // Only offered when there is something to take off, so that the one destructive button here
        // is never the obvious thing to press on a sign that is still blank.
        if (existing is not null)
        {
            buttons.Children.Add(CreateButton("Leave blank", new SignpostChoice(null)));
        }

        buttons.Children.Add(set);

        var layout = new DockPanel
        {
            Margin = new Thickness(24)
        };

        var caption = new TextBlock
        {
            Text = destinations.Count == 0
                ? "No screen on the map has a teleport code yet, so there is nothing for the signpost on screen " + screen + " to say."
                : "The signpost on screen " + screen + " spells out the code of whichever teleporter it is pointed at.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = EditorColours.TextBrush
        };

        DockPanel.SetDock(caption, Dock.Top);
        DockPanel.SetDock(buttons, Dock.Bottom);

        layout.Children.Add(caption);
        layout.Children.Add(buttons);
        layout.Children.Add(_destinations);

        Content = layout;
    }

    /// <summary>
    ///     Asks which teleporter the sign points at, coming back with null if the dialog was
    ///     cancelled.
    /// </summary>
    public static Task<SignpostChoice?> AskAsync(
        Window owner,
        int screen,
        int? existing,
        IReadOnlyList<TeleportDestination> destinations)
    {
        var dialog = new SignpostDialog(screen, existing, destinations);

        return dialog.ShowDialog<SignpostChoice?>(owner);
    }

    /// <summary>
    ///     Opens with whichever teleporter the sign already names in view, so that a map with a
    ///     hundred codes on it does not have to be scrolled to see what the sign says now.
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_destinations.SelectedItem is not null)
        {
            _destinations.ScrollIntoView(_destinations.SelectedItem);
        }

        _destinations.Focus();
    }

    /// <summary>
    ///     Closing with nothing chosen would be the cancel button by another name, so a dialog with
    ///     no selection stays open.
    /// </summary>
    private void Accept()
    {
        if (_destinations.SelectedItem is TeleportDestination destination)
        {
            Close(new SignpostChoice(destination.Screen));
        }
    }

    private Button CreateButton(string text, SignpostChoice? choice)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        button.Click += (_, _) => Close(choice);

        return button;
    }
}
