// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;
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
    /// <summary>
    ///     The pieces that can be bent while they play, held as sound effects rather than songs
    ///     because <c>MediaPlayer</c> offers nothing but a volume. See <see cref="Distort" />.
    /// </summary>
    private readonly Dictionary<GameScreenType, SoundEffectInstance> _distortablePieces = new();

    /// <summary>Everything else, streamed from disc through <c>MediaPlayer</c>.</summary>
    private readonly Dictionary<GameScreenType, Song> _streamedPieces = new();

    /// <summary>
    ///     The screen this last acted on, which is not necessarily a screen with music: a screen
    ///     without any is remembered too, so the stop happens once rather than every frame.
    /// </summary>
    private GameScreenType _following = GameScreenType.None;

    /// <summary>
    ///     Whether the screen now showing has a piece still to start, and how many frames of
    ///     <see cref="MusicSettings.StartDelayFrames" /> are left. Cleared once it has been started,
    ///     so a piece plays through its loop rather than being restarted every frame.
    /// </summary>
    private bool _waitingToPlay;

    /// <inheritdoc cref="_waitingToPlay" />
    private int _framesUntilPlay;

    /// <summary>
    ///     Opens one piece of music. A streamed piece decodes as it plays, so this reads enough of
    ///     the file to know its length and no more; a distortable one is decoded whole and stays in
    ///     memory, which is why only a piece something distorts is registered as one. Either way it
    ///     is safe on the background load.
    /// </summary>
    public void LoadSong(MusicDescription musicDesc)
    {
        if (!musicDesc.CanBeDistorted)
        {
            _streamedPieces.Add(musicDesc.Screen, content.Load<Song>(musicDesc.FilePath));

            return;
        }

        SoundEffectInstance piece = content.Load<SoundEffect>(musicDesc.FilePath).CreateInstance();
        piece.IsLooped = true;

        _distortablePieces.Add(musicDesc.Screen, piece);
    }

    /// <summary>
    ///     Plays the piece belonging to the screen now showing, or stops the music if that screen has
    ///     none. Called once a logic frame, which is what the start delay is counted in.
    /// </summary>
    public void FollowTheScreen(GameScreenType screen)
    {
        if (screen != _following)
        {
            // Whatever the last screen was playing stops now rather than at the end of the delay,
            // so the gap belongs to the new screen instead of being the old screen running on.
            Stop(_following);

            _following = screen;
            _waitingToPlay = _streamedPieces.ContainsKey(screen) || _distortablePieces.ContainsKey(screen);
            _framesUntilPlay = MusicSettings.StartDelayFrames;
        }

        if (!_waitingToPlay)
        {
            return;
        }

        if (_framesUntilPlay > 0)
        {
            _framesUntilPlay--;

            return;
        }

        Play(_following);
        _waitingToPlay = false;
    }

    /// <summary>
    ///     Bends the piece the screen now showing is playing. Does nothing on a screen whose piece
    ///     is not a distortable one.
    /// </summary>
    /// <param name="pitch">
    ///     How far off true it plays, minus one being an octave down and one an octave up. The
    ///     playback speed goes with it, so a piece pulled flat drags as well.
    /// </param>
    /// <param name="volume">How loud, from nought to one.</param>
    /// <param name="pan">Which speaker, from minus one for the left to one for the right.</param>
    public void Distort(float pitch, float volume, float pan)
    {
        if (!_distortablePieces.TryGetValue(_following, out SoundEffectInstance? piece))
        {
            return;
        }

        piece.Pitch = pitch;
        piece.Volume = volume;
        piece.Pan = pan;
    }

    /// <summary>
    ///     Starts the given screen's piece. Each is a couple of minutes against a screen a player
    ///     can sit on for as long as they like, so every one of them loops.
    /// </summary>
    private void Play(GameScreenType screen)
    {
        if (_distortablePieces.TryGetValue(screen, out SoundEffectInstance? piece))
        {
            piece.Play();

            return;
        }

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Play(_streamedPieces[screen]);
    }

    /// <summary>
    ///     Silences the given screen's piece. <c>MediaPlayer</c> is stopped whatever that screen was
    ///     playing, because it is static and nothing else here knows what reached it.
    /// </summary>
    private void Stop(GameScreenType screen)
    {
        MediaPlayer.Stop();

        if (_distortablePieces.TryGetValue(screen, out SoundEffectInstance? piece))
        {
            piece.Stop();
        }
    }
}
