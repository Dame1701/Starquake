// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Starquake.ScreenEditor.ViewModels;

/// <summary>
///     The change notification every bindable object in the editor needs, and nothing else.
/// </summary>
/// <remarks>
///     This used to be ReactiveUI's <c>ReactiveObject</c>, but the editor only ever wanted the two
///     methods below from it: the commands are plain <see cref="RelayCommand" />s and the drawn
///     surfaces repaint from the session's own events rather than by observing properties. A whole
///     reactive framework for two methods is a dependency that has to be kept in step with Avalonia
///     for nothing, so it is these two methods instead.
/// </remarks>
public abstract class ViewModelBase : INotifyPropertyChanged
{

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///     Assigns a backing field and announces the change, doing nothing if the value is already
    ///     what it was.
    /// </summary>
    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        RaisePropertyChanged(propertyName);
    }
}
