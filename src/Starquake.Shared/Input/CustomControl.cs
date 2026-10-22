// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Starquake.Core;

namespace Starquake.Input;

/// <summary>
///     What the player is pressing. Every method here reads the keyboard and the first gamepad, and
///     answers true if either of them says so.
/// </summary>
/// <remarks>
///     The pad is read alongside the keyboard rather than instead of it, so a pad works whichever key
///     preset is chosen and the joystick preset is simply the one that binds no keys.
///     <para>
///         Both come from the one snapshot the frame began with rather than being polled here, so
///         these methods cannot disagree with each other or with <c>ToggleKeys</c> inside a frame.
///     </para>
/// </remarks>
internal class CustomControl(PlayerKeySet playerKeys, ToggleKeys input, GameSession session)
{
    /// <summary>
    ///     How far the stick must be pushed to count as a direction. Blob's movement is digital, so
    ///     the stick is read as a second d-pad rather than an analogue axis, and this is where a lean
    ///     becomes a step. With <see cref="GameSession.AnalogueWalk" /> on, the stick is read as eight
    ///     directions as well; see <see cref="Leans" />.
    /// </summary>
    private const float StickThreshold = 0.5f;

    public PlayerKeySet PlayerKeySet { get; set; } = playerKeys;

    private Vector2 Stick => input.GamePadState.ThumbSticks.Left;

    private bool PadAskedForPlatform => input.GamePadState.Buttons.B == ButtonState.Pressed;

    private bool PadAskedForInventory => input.GamePadState.Buttons.X == ButtonState.Pressed;

    public bool MovedUp() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Up) ||
        PushedUp(input.GamePadState) ||
        Leans(Stick.Y, Stick.X);

    public bool MovedDown() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Down) ||
        PushedDown(input.GamePadState) ||
        Leans(-Stick.Y, Stick.X);

    public bool MovedLeft() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Left) ||
        PushedLeft(input.GamePadState) ||
        Leans(-Stick.X, Stick.Y);

    public bool MovedRight() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Right) ||
        PushedRight(input.GamePadState) ||
        Leans(Stick.X, Stick.Y);

    /// <summary>
    ///     How far Blob walks this frame from <paramref name="xPos" />, given the full step. Always
    ///     the full step with <see cref="GameSession.AnalogueWalk" /> off. With it on, the lean sets
    ///     the speed when only the stick is walking, and every walk stops on each cell edge it
    ///     passes; see <see cref="AnalogueWalk" />.
    /// </summary>
    /// <remarks>
    ///     The cell edges are left alone with the mode off, because the game has never kept Blob on
    ///     them: a booth puts Blob down between them, and the full step has always walked on from there.
    /// </remarks>
    public int WalkStep(int fullStep, int xPos, bool leftwards)
    {
        if (!session.AnalogueWalk)
        {
            return fullStep;
        }

        GamePadState pad = input.GamePadState;

        bool walkingDigitally =
            input.KeyboardState.IsKeyDown(PlayerKeySet.Left) ||
            input.KeyboardState.IsKeyDown(PlayerKeySet.Right) ||
            pad.DPad.Left == ButtonState.Pressed ||
            pad.DPad.Right == ButtonState.Pressed;

        return AnalogueWalk.StepFor(walkingDigitally ? 1f : Stick.X, xPos, leftwards, fullStep);
    }

    public bool HasFired() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Fire) ||
        input.PadFireHeld;

    /// <summary>
    ///     Laying a platform, or jumping with none left, held down. Down on the keyboard, as it always
    ///     was. On the pad it is B, circle on a PlayStation pad, the button beside fire, rather than
    ///     down, so the stick or the d-pad leaning down never lays one by accident. Down on the pad
    ///     still flies the shuttle down.
    /// </summary>
    public bool AskedForPlatform() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Down) || PadAskedForPlatform;

    /// <summary>
    ///     Whether a platform may be laid now. The pad's button works on the move. Down on the
    ///     keyboard waits for Blob to stop walking, as it always has, because it is also half of a
    ///     walking diagonal.
    /// </summary>
    public bool LaysPlatform() =>
        PadAskedForPlatform || input.KeyboardState.IsKeyDown(PlayerKeySet.Down) && !Walking();

    /// <summary>
    ///     Swapping with the inventory, or with the pyramid, held down. Up on the keyboard, as it
    ///     always was. On the pad it is X, square on a PlayStation pad, rather than up, for the same
    ///     reason as <see cref="AskedForPlatform" />. Up on the pad still boards the shuttle and flies
    ///     it up.
    /// </summary>
    public bool AskedForInventory() =>
        input.KeyboardState.IsKeyDown(PlayerKeySet.Up) || PadAskedForInventory;

    /// <summary>Whether a swap may happen now. See <see cref="LaysPlatform" />.</summary>
    public bool SwapsInventory() =>
        PadAskedForInventory || input.KeyboardState.IsKeyDown(PlayerKeySet.Up) && !Walking();

    private bool Walking() => MovedLeft() || MovedRight();

    /// <summary>
    ///     The d-pad and the left stick both count, so a pad can be used either way. A pad that is not
    ///     plugged in reports every button released and both sticks centred, so these answer false
    ///     without being asked whether one is there.
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

    /// <summary>
    ///     A lean the digital stick would miss that counts under <see cref="GameSession.AnalogueWalk" />,
    ///     such as a diagonal a little shallower than 45°, which falls short of
    ///     <see cref="StickThreshold" /> on its lesser axis. See <see cref="AnalogueWalk.Leans" />.
    /// </summary>
    private bool Leans(float along, float across) =>
        session.AnalogueWalk && AnalogueWalk.Leans(along, across);
}
