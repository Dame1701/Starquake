// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Windows.Input;

namespace Starquake.ScreenEditor.ViewModels;

/// <summary>A command that runs a delegate.</summary>
/// <remarks>
///     Every command in the editor does something now and greys out when that makes no sense, which
///     is too little to justify wiring each one up as an observable. The session calls
///     <see cref="RaiseCanExecuteChanged" />, because it is the one place that knows when anything
///     has changed.
/// </remarks>
internal sealed class RelayCommand : ICommand
{

    private readonly Func<bool>? _canExecute;

    private readonly Func<object?, Task> _execute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ =>
        {
            execute();
            return Task.CompletedTask;
        }, canExecute)
    {
    }

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute)
    {
    }

    private RelayCommand(Func<object?, Task> execute, Func<bool>? canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter) => await _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
