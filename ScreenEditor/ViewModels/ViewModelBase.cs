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
///     Two methods rather than a reactive framework. The commands are plain
///     <see cref="RelayCommand" />s and the drawn surfaces repaint from the session's own events
///     rather than by observing properties, so there is nothing else to gain from one.
/// </remarks>
internal abstract class ViewModelBase : INotifyPropertyChanged
{

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///     Assigns a backing field and announces the change. Does nothing if the value is unchanged.
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
