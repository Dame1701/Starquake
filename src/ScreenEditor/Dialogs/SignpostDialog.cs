// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.Styles;

namespace Starquake.ScreenEditor.Dialogs;

/// <summary>
///     What somebody chose for a screen's signpost. A null <see cref="Screen" /> means the sign was
///     left blank. Cancelling the dialog comes back as no choice at all.
/// </summary>
internal sealed record SignpostChoice(int? Screen);

/// <summary>
///     Points the signposts on a screen at the teleporter whose code they are to spell out.
/// </summary>
/// <remarks>
///     Offers the codes already on the map rather than a box to type one into. A typed code could
///     advertise a destination no booth answers to, and the player cannot tell that dead end from a
///     live one until they have walked to a booth and entered it.
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

        // Only offered when there is something to remove, so the destructive button is never the
        // obvious thing to press on a sign that is still blank.
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
    ///     Asks which teleporter the sign points at. Returns null if the dialog was cancelled.
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
    ///     Scrolls the teleporter the sign already names into view, so a map with a hundred codes on
    ///     it does not have to be scrolled by hand.
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
    ///     Keeps the dialog open when nothing is selected. Closing with no choice would just be the
    ///     cancel button by another name.
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
