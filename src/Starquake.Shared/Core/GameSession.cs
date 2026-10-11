// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.Input;

namespace Starquake.Core;

/// <summary>
///     How the game is being played: whether it is paused, and the keys it is played with.
///     <para>
///         Neither is plain state. Setting the pause starts and stops the background rumble, and
///         setting the keys hands them to Blob's control and chooses keyboard against joypad, but
///         neither the sound engine nor Blob exists when a session is made. So the session holds the
///         state and raises <see cref="OnPausedChanged" /> and <see cref="OnPlayerKeysChanged" />,
///         and the engine, which knows about both, acts on them.
///     </para>
///     <para>
///         <c>StatusPanel</c> is deliberately not here. It needs the engine to construct, so holding
///         it would make this untestable.
///     </para>
/// </summary>
internal sealed class GameSession
{
    private bool _paused;

    private PlayerKeySet _playerKeys = new()
    {
        Up = Keys.Up,
        Down = Keys.Down,
        Left = Keys.Left,
        Right = Keys.Right,

        // Alt rather than Ctrl, which macOS reserves. Ctrl with any arrow key is a Mission Control
        // shortcut and the window server takes it before the game sees the key. Firing while moving
        // is ordinary, so the authentic Ctrl-to-fire makes the default scheme unplayable there.
        Fire = Keys.LeftAlt
    };

    /// <summary>
    ///     Whether play is suspended. Blob and the nasties stop stepping and the paused banner is
    ///     drawn over the game.
    /// </summary>
    public bool Paused
    {
        get => _paused;
        set
        {
            _paused = value;
            OnPausedChanged?.Invoke(value);
        }
    }

    /// <summary>
    ///     The keys the game is played with. Starts on the cursor keys with left Alt to fire.
    /// </summary>
    public PlayerKeySet PlayerKeys
    {
        get => _playerKeys;
        set
        {
            _playerKeys = value;
            OnPlayerKeysChanged?.Invoke(value);
        }
    }

    /// <summary>
    ///     Whether how far the left stick leans sets how fast Blob walks. F3 switches it, and it is
    ///     off unless the player has switched it on. See <c>AnalogueWalk</c>.
    /// </summary>
    public bool AnalogueWalk { get; set; }

    /// <summary>
    ///     Raised when <see cref="Paused" /> is set, with the value it was set to. The engine answers
    ///     by stopping and restarting the periodic background sounds.
    /// </summary>
    public event Action<bool>? OnPausedChanged;

    /// <summary>
    ///     Raised when <see cref="PlayerKeys" /> is set, with the new set. The engine answers by
    ///     handing it to Blob's control and deciding keyboard against joypad.
    /// </summary>
    public event Action<PlayerKeySet>? OnPlayerKeysChanged;
}
