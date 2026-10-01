// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Windows.Input;

namespace Starquake.ScreenEditor.ViewModels;

/// <summary>
///     A command that runs a delegate.
/// </summary>
/// <remarks>
///     The editor's commands are all "do this now, and be greyed out when it makes no sense", which
///     is little enough that wiring each one up as an observable would be more machinery than the
///     job needs. <see cref="RaiseCanExecuteChanged" /> is called from the one place that knows when
///     anything has changed - the session - rather than from each command separately.
/// </remarks>
public sealed class RelayCommand : ICommand
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

    public RelayCommand(Action<object?> execute, Func<bool>? canExecute = null)
        : this(parameter =>
        {
            execute(parameter);
            return Task.CompletedTask;
        }, canExecute)
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
