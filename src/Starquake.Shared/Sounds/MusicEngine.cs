// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Starquake.Core;
using Starquake.Tuning;

namespace Starquake.Sounds;

/// <summary>
///     The music, one piece per screen, started and stopped by following whichever screen is showing.
///     <para>
///         Asked every frame what screen the game is on, and acts only when the answer changes: on a
///         screen with music it starts that piece looping once
///         <see cref="MusicSettings.StartDelayFrames" /> have passed, and on a screen without it
///         stops whatever was playing. That is why it remembers the last screen it was told -
///         <c>MediaPlayer</c> is static and <c>Play</c> restarts from the beginning, so asking for
///         the same piece every frame would hold it on its first fraction of a second.
///     </para>
///     <para>
///         Following the screen rather than being started by each screen is deliberate. Screens hand
///         over by naming one another, from a dozen places, and the game boots straight into the
///         options screen without handing over at all; a piece started where a screen is entered
///         would have to be stopped at every one of those points and would never play on the boot.
///     </para>
/// </summary>
internal sealed class MusicEngine(ContentManager content)
{
    private readonly Dictionary<GameScreenType, Song> _songs = new();

    /// <summary>
    ///     The screen this last acted on, which is not necessarily a screen with music: a screen
    ///     without any is remembered too, so the stop happens once rather than every frame.
    /// </summary>
    private GameScreenType _following = GameScreenType.None;

    /// <summary>
    ///     The piece the screen now showing wants, while it waits out
    ///     <see cref="MusicSettings.StartDelayFrames" />, and how many of those frames are left.
    ///     Null once it has been started, so a piece plays through its loop rather than being
    ///     restarted every frame.
    /// </summary>
    private Song? _waitingToPlay;

    /// <inheritdoc cref="_waitingToPlay" />
    private int _framesUntilPlay;

    /// <summary>
    ///     Opens one piece of music. The song decodes as it plays, so this reads enough of the file to
    ///     know its length and no more - which is also why it is safe on the background load.
    /// </summary>
    public void LoadSong(MusicDescription musicDesc)
        => _songs.Add(musicDesc.Screen, content.Load<Song>(musicDesc.FilePath));

    /// <summary>
    ///     Plays the piece belonging to the screen now showing, or stops the music if that screen has
    ///     none. Called once a logic frame, which is what the start delay is counted in.
    /// </summary>
    public void FollowTheScreen(GameScreenType screen)
    {
        if (screen != _following)
        {
            _following = screen;
            _waitingToPlay = null;

            // Whatever the last screen was playing stops now rather than at the end of the delay,
            // so the gap belongs to the new screen instead of being the old screen running on.
            MediaPlayer.Stop();

            if (!_songs.TryGetValue(screen, out Song? song))
            {
                return;
            }

            _waitingToPlay = song;
            _framesUntilPlay = MusicSettings.StartDelayFrames;
        }

        if (_waitingToPlay is null)
        {
            return;
        }

        if (_framesUntilPlay > 0)
        {
            _framesUntilPlay--;

            return;
        }

        // Each piece is a couple of minutes against a screen a player can sit on for as long as they
        // like, so every one of them loops.
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Play(_waitingToPlay);

        _waitingToPlay = null;
    }
}
