// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     A control's hold on whichever <see cref="EditorSession" /> it is currently showing. The
///     session raises <see cref="EditorSession.Invalidated" /> when anything it holds changes, and a
///     control that draws the session has to repaint when it does - which means subscribing when the
///     session arrives and, more easily forgotten, unsubscribing from the one it replaces.
/// </summary>
/// <remarks>
///     This was eighteen lines written twice, character for character, in <c>ScreenCanvas</c> and
///     <c>CollisionMaskEditor</c>. It is small, and the reason it is worth having once is that the
///     mistake it is easy to make here is silent: a swap that subscribes without unsubscribing
///     leaves the old session holding the control alive and repainting it for edits it is no longer
///     showing. Nothing would look wrong until two canvases were open.
/// </remarks>
/// <param name="repaint">
///     What to do when the session says it has changed, and once more when the session itself is
///     swapped - a control's <c>InvalidateVisual</c>.
/// </param>
internal sealed class SessionSubscription(Action repaint)
{
    private EditorSession? _attached;

    /// <summary>
    ///     Lets go of the session being shown, takes up <paramref name="session" />, and repaints.
    ///     Null detaches without taking anything up, which is a control whose session has been
    ///     cleared rather than replaced.
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
