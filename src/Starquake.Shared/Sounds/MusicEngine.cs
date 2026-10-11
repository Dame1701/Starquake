// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Starquake.Constants;
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
///     <para>
///         It also counts how far through its piece the screen now showing is, because three of the
///         screens hand over when their music ends rather than waiting for a key. See
///         <see cref="HasPlayedThrough" />.
///     </para>
///     <para>
///         A screen that is showing something other than itself can hold its piece back until it is;
///         see <see cref="HoldBack" />. That is a screen saying when it is ready rather than a screen
///         starting its own music, so the one place the music is played from is still here.
///     </para>
/// </summary>
internal sealed class MusicEngine(ContentManager content)
{
    /// <summary>
    ///     The pieces that can be bent while they play, held as sound effects rather than songs
    ///     because <c>MediaPlayer</c> offers nothing but a volume. See <see cref="Distort" />.
    /// </summary>
    private readonly Dictionary<GameScreenType, SoundEffectInstance> _distortablePieces = new();

    /// <summary>
    ///     How long each piece runs for, in logic frames, whichever of the two ways it is played.
    ///     Read off the piece as it loads rather than written down anywhere, so replacing a piece
    ///     with a longer or shorter one needs nothing said here.
    /// </summary>
    private readonly Dictionary<GameScreenType, int> _pieceLengths = new();

    /// <summary>Everything else, streamed from disc through <c>MediaPlayer</c>.</summary>
    private readonly Dictionary<GameScreenType, Song> _streamedPieces = new();

    /// <summary>
    ///     The screen this last acted on, which is not necessarily a screen with music: a screen
    ///     without any is remembered too, so the stop happens once rather than every frame.
    /// </summary>
    private GameScreenType _following = GameScreenType.None;

    /// <summary>
    ///     The screen whose piece is being held back, or <see cref="GameScreenType.None" /> when
    ///     none is. Named rather than held as a flag so a hold left behind can only ever silence the
    ///     screen that asked for it. See <see cref="HoldBack" />.
    /// </summary>
    private GameScreenType _heldScreen = GameScreenType.None;

    /// <summary>
    ///     Whether the screen now showing has a piece still to start, and how many frames of
    ///     <see cref="MusicSettings.StartDelayFrames" /> are left. Cleared once it has been started,
    ///     so a piece plays through its loop rather than being restarted every frame.
    /// </summary>
    private bool _waitingToPlay;

    /// <inheritdoc cref="_waitingToPlay" />
    private int _framesUntilPlay;

    /// <summary>
    ///     How many frames of the pass the piece is making are left, counted down from the piece's
    ///     own length. Nought means no pass is running: either nothing has started yet, or the pass
    ///     has finished and <see cref="HasPlayedThrough" /> is the answer to read instead.
    /// </summary>
    private int _framesLeftOfPiece;

    /// <summary>
    ///     Whether the piece belonging to the screen now showing has run its full length, which is
    ///     what the screens that hand over on their own music ending wait for. Stays false on a
    ///     screen without a piece, there being nothing for it to be the end of.
    ///     <para>
    ///         Every piece loops, so this says a pass has finished rather than that the music has
    ///         stopped, and it is the first pass: a screen handed over on the end of its music never
    ///         reaches a second one.
    ///     </para>
    /// </summary>
    public bool HasPlayedThrough { get; private set; }

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
            Song song = content.Load<Song>(musicDesc.FilePath);

            _streamedPieces.Add(musicDesc.Screen, song);
            _pieceLengths.Add(musicDesc.Screen, LogicClock.Seconds(song.Duration.TotalSeconds));
            return;
        }

        SoundEffect effect = content.Load<SoundEffect>(musicDesc.FilePath);
        SoundEffectInstance piece = effect.CreateInstance();
        piece.IsLooped = true;

        _distortablePieces.Add(musicDesc.Screen, piece);
        _pieceLengths.Add(musicDesc.Screen, LogicClock.Seconds(effect.Duration.TotalSeconds));
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
            _framesLeftOfPiece = 0;
            HasPlayedThrough = false;
        }

        // Held screens are left where they are rather than counted down, so a released piece still
        // has its whole opening delay ahead of it and its whole length after that.
        if (_following == _heldScreen)
        {
            return;
        }

        if (_waitingToPlay)
        {
            StartOnceTheDelayIsUp();
            return;
        }

        CountOffThePass();
    }

    /// <summary>
    ///     Keeps the given screen's piece from starting, for a screen that arrives showing something
    ///     other than itself: the hi-scores open on a name to be typed rather than on the table, and
    ///     the table is what its music is for. Set as the screen is handed control, which is before
    ///     this has followed the hand-over, so the hold is not cleared by one.
    /// </summary>
    /// <param name="screen">The screen whose piece is to wait.</param>
    public void HoldBack(GameScreenType screen)
        => _heldScreen = screen;

    /// <summary>
    ///     Lets a held piece start, from the same opening delay as any other, so the music arrives
    ///     after the screen has settled into what it was waiting to show.
    /// </summary>
    public void StopHoldingBack()
        => _heldScreen = GameScreenType.None;

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
    ///     Starts the screen's piece when the last of its opening delay has gone by, and sets its
    ///     pass running from that frame.
    /// </summary>
    private void StartOnceTheDelayIsUp()
    {
        if (_framesUntilPlay > 0)
        {
            _framesUntilPlay--;
            return;
        }

        Play(_following);

        _framesLeftOfPiece = _pieceLengths[_following];
        _waitingToPlay = false;
    }

    /// <summary>
    ///     Counts off the pass the piece is making, and marks it played through at the end of it. The
    ///     count stops there rather than starting again with the loop, so the answer holds for the
    ///     rest of the time the screen is up however late it is read.
    /// </summary>
    private void CountOffThePass()
    {
        if (_framesLeftOfPiece == 0)
        {
            return;
        }

        _framesLeftOfPiece--;

        if (_framesLeftOfPiece == 0)
        {
            HasPlayedThrough = true;
        }
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
