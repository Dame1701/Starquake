// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     A control's hold on whichever <see cref="EditorSession" /> it is showing. The session raises
///     <see cref="EditorSession.Invalidated" /> when anything changes, so a control drawing it must
///     subscribe when a session arrives and unsubscribe from the one it replaces.
/// </summary>
/// <remarks>
///     Kept in one place because the mistake here is silent. Subscribing without unsubscribing leaves
///     the old session holding the control alive and repainting it for edits it no longer shows, and
///     nothing looks wrong until two canvases are open.
/// </remarks>
/// <param name="repaint">
///     What to call when the session changes and when the session itself is swapped, typically a
///     control's <c>InvalidateVisual</c>.
/// </param>
internal sealed class SessionSubscription(Action repaint)
{
    private EditorSession? _attached;

    /// <summary>
    ///     Releases the current session, takes up <paramref name="session" />, and repaints. Null
    ///     detaches without taking anything up, for a control whose session was cleared rather than
    ///     replaced.
    /// </summary>
    public void Attach(EditorSession? session)
    {
        if (_attached is not null)
        {
            _attached.Invalidated -= OnSessionInvalidated;
        }

        _attached = session;

        if (_attached is not null)
        {
            _attached.Invalidated += OnSessionInvalidated;
        }

        repaint();
    }

    private void OnSessionInvalidated(object? sender, EventArgs e) => repaint();
}
