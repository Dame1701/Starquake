// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;

namespace Starquake.Input;

/// <summary>
///     What the player is pressing. Every method here reads the keyboard and the first gamepad, and
///     answers true if either of them says so.
/// </summary>
/// <remarks>
///     There was a joypad once, reached through DirectInput, and it never worked: the options screen
///     offered it, the code behind it was commented out, and choosing it left a player who could pause
///     and quit and nothing else. DesktopGL reaches a pad through SDL instead, so the five methods
///     below read one now. It is read alongside the keyboard rather than instead of it, so a pad works
///     whichever key preset is chosen, and the joystick preset is simply the one that binds no keys.
///     <para>
///         Both come from the one snapshot the frame began with rather than being polled here, so
///         the five methods cannot disagree with each other or with ToggleKeys inside a frame.
///     </para>
/// </remarks>
internal class CustomControl(PlayerKeySet playerKeys, ToggleKeys input)
{
    /// <summary>
    ///     How far the stick has to be pushed before it counts as a direction. Blob's movement is
    ///     digital - he is walking or he is not - so the stick is read as a fourth d-pad rather than as
    ///     an analogue axis, and this is the point at which a lean becomes a step.
    /// </summary>
    private const float StickThreshold = 0.5f;

    public PlayerKeySet PlayerKeySet { get; set; } = playerKeys;

    public bool MovedUp() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Up) ||
        PushedUp(input.GamePadState);

    public bool MovedDown() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Down) ||
        PushedDown(input.GamePadState);

    public bool MovedLeft() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Left) ||
        PushedLeft(input.GamePadState);

    public bool MovedRight() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Right) ||
        PushedRight(input.GamePadState);

    public bool HasFired() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Fire) ||
        input.GamePadState.Buttons.A == ButtonState.Pressed;

    /// <summary>
    ///     The d-pad and the left stick both count, so a pad can be held either way round. A pad that is
    ///     not plugged in reports every button released and both sticks centred, so these answer false
    ///     without needing to be asked whether one is there.
    /// </summary>
    private static bool PushedUp(GamePadState pad) =>
        pad.DPad.Up == ButtonState.Pressed || pad.ThumbSticks.Left.Y > StickThreshold;

    /// <inheritdoc cref="PushedUp" />
    private static bool PushedDown(GamePadState pad) =>
        pad.DPad.Down == ButtonState.Pressed || pad.ThumbSticks.Left.Y < -StickThreshold;

    /// <inheritdoc cref="PushedUp" />
    private static bool PushedLeft(GamePadState pad) =>
        pad.DPad.Left == ButtonState.Pressed || pad.ThumbSticks.Left.X < -StickThreshold;

    /// <inheritdoc cref="PushedUp" />
    private static bool PushedRight(GamePadState pad) =>
        pad.DPad.Right == ButtonState.Pressed || pad.ThumbSticks.Left.X > StickThreshold;
}
