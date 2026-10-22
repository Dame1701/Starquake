// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Starquake.ScreenEditor.Editing;
using Starquake.ScreenEditor.ViewModels;

namespace Starquake.ScreenEditor.Views;

internal partial class MainWindow : Window
{

    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AttachOwner(this);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        (DataContext as MainWindowViewModel)?.LoadStartingContent();
    }

    /// <summary>
    ///     The single key shortcuts, handled here rather than as key bindings on the window.
    /// </summary>
    /// <remarks>
    ///     A key binding fires wherever the focus is, which would make the tile filter box impossible
    ///     to type in. These are the plain letters, and they stand aside while text is being entered.
    ///     Anything with a modifier is bound in the markup.
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        EditorSession? session = (DataContext as MainWindowViewModel)?.Session;

        if (e.Handled || session is null || e.KeyModifiers != KeyModifiers.None || e.Source is TextBox)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.B:
                session.Tool = EditorTool.Paint;
                break;

            case Key.E:
                session.Tool = EditorTool.Erase;
                break;

            case Key.F:
                session.Tool = EditorTool.Fill;
                break;

            case Key.I:
                session.Tool = EditorTool.Pick;
                break;

            case Key.G:
                session.View.ShowGrid = !session.View.ShowGrid;
                break;

            case Key.C:
                session.View.ShowCollision = !session.View.ShowCollision;
                break;

            case Key.T:
                session.View.ShowTileNumbers = !session.View.ShowTileNumbers;
                break;

            case Key.N:
                session.View.ShowNeighbours = !session.View.ShowNeighbours;
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    /// <summary>
    ///     Holds the window open long enough to ask about unsaved work. The prompt is asynchronous and
    ///     closing is not, so the first attempt is always cancelled and a second one does the closing.
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_closeConfirmed || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;

        _ = ConfirmThenCloseAsync(viewModel);
    }

    private async Task ConfirmThenCloseAsync(MainWindowViewModel viewModel)
    {
        if (!await viewModel.ConfirmCloseAsync())
        {
            return;
        }

        _closeConfirmed = true;
        Close();
    }
}
