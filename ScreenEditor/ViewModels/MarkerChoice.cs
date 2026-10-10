// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Material.Icons;
using Starquake.ScreenEditor.Editing;
using Starquake.Structure;

namespace Starquake.ScreenEditor.ViewModels;

/// <summary>One marker as the toolbar offers it.</summary>
/// <remarks>
///     The buttons are generated from <see cref="MarkerCatalogue" /> rather than written out, so a
///     button's icon is always the icon drawn on the square it paints. Writing them out would name
///     each icon twice, here and on the canvas, and let the two disagree.
/// </remarks>
internal sealed class MarkerChoice : ViewModelBase
{

    private readonly EditorSession _session;

    public MarkerChoice(EditorSession session, MarkerTile marker, MarkerFace face)
    {
        _session = session;
        Marker = marker;
        Face = face;
    }

    private MarkerTile Marker { get; }

    private MarkerFace Face { get; }

    public MaterialIconKind Icon => Face.Icon;

    public string Title => Face.Title;

    public string Description => Face.Description;

    /// <summary>
    ///     Whether this is the marker in hand. Setting it picks the marker up. Unchecking is ignored,
    ///     because the only thing that unchecks one is another being checked, and acting on that would
    ///     undo the change that caused it.
    /// </summary>
    public bool IsSelected
    {
        get => _session.SelectedMarker == Marker;
        set
        {
            if (value)
            {
                _session.SelectedMarker = Marker;
            }
        }
    }

    /// <summary>
    ///     Announces that what is in hand has changed. Called for every button on any selection
    ///     change, because a tile picked from the palette unchecks them all and no button can tell
    ///     that from its own state.
    /// </summary>
    public void NotifySelectionChanged() => RaisePropertyChanged(nameof(IsSelected));
}
