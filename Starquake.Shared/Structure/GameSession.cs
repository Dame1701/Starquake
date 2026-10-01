// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.Input;

namespace Starquake.Structure;

/// <summary>
///     How the game is being played: whether it is paused, and the keys it is played with.
///     <para>
///         Neither is plain state. Setting the pause has to start and stop the background rumble,
///         and setting the keys has to hand them to the blob's control and choose keyboard against
///         joypad - and neither the sound engine nor the blob exists when a session is first made.
///         So the session holds the state and raises <see cref="OnPausedChanged" /> and
///         <see cref="OnPlayerKeysChanged" />, and the engine, the one thing that knows about both,
///         acts on them. That is the shape the nasties and the electricity engine already use to
///         report back.
///     </para>
///     <para>
///         <c>StatusPanel</c> is deliberately not here, though it is the third of the members the
///         plan grouped as the session. It needs the engine to construct, so holding it would make
///         this untestable - and it would only be re-homing a reference, which narrows nothing.
///     </para>
/// </summary>
public sealed class GameSession
{
    private bool _paused;

    private PlayerKeySet _playerKeys = new()
    {
        Up = Keys.Up,
        Down = Keys.Down,
        Left = Keys.Left,
        Right = Keys.Right,
        Fire = Keys.LeftControl
    };

    /// <summary>
    ///     Whether play is suspended. The blob and the nasties stop stepping, and the paused banner
    ///     is drawn over the game.
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
    ///     The keys the game is being played with. Starts on the cursor keys with left control to
    ///     fire, which is one of the four layouts the options screen offers.
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
    ///     Raised whenever <see cref="Paused" /> is set, with the value it was set to. The engine
    ///     answers it by stopping and restarting the periodic background sounds.
    /// </summary>
    public event Action<bool>? OnPausedChanged;

    /// <summary>
    ///     Raised whenever <see cref="PlayerKeys" /> is set, with the new set. The engine answers it
    ///     by handing the set to the blob's control and deciding keyboard against joypad.
    /// </summary>
    public event Action<PlayerKeySet>? OnPlayerKeysChanged;
}
