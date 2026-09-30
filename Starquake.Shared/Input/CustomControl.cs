// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;

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
/// </remarks>
public class CustomControl
{
    /// <summary>
    ///     How far the stick has to be pushed before it counts as a direction. The blob's movement is
    ///     digital - he is walking or he is not - so the stick is read as a fourth d-pad rather than as
    ///     an analogue axis, and this is the point at which a lean becomes a step.
    /// </summary>
    private const float StickThreshold = 0.5f;

    /// <summary>
    ///     How far an analogue stick has to be pushed before the blob steps up to the next speed. The
    ///     bands are deliberately uneven at the bottom so that a resting stick never nudges the blob.
    /// </summary>
    private const double SpeedBand1 = 0.2;

    /// <inheritdoc cref="SpeedBand1" />
    private const double SpeedBand2 = 0.4;

    /// <inheritdoc cref="SpeedBand1" />
    private const double SpeedBand3 = 0.6;

    /// <inheritdoc cref="SpeedBand1" />
    private const double SpeedBand4 = 0.8;

    /// <inheritdoc cref="SpeedBand1" />
    private const double VerticalDeadZone = 0.1;

    /// <summary>
    ///     The speeds those bands select, from a crawl up to the blob's full walking pace.
    /// </summary>
    private const int CrawlSpeed = 1;

    /// <inheritdoc cref="CrawlSpeed" />
    private const int SlowSpeed = 2;

    /// <inheritdoc cref="CrawlSpeed" />
    private const int BriskSpeed = 4;

    /// <inheritdoc cref="CrawlSpeed" />
    private const int FullSpeed = 8;

    private readonly Blob _blob;

    public CustomControl(PlayerKeySet playerKeys, Blob blob)
    {
        _blob = blob;
        PlayerKeySet = playerKeys;
    }

    public PlayerKeySet PlayerKeySet { get; set; }

    /// <summary>
    ///     Whether a pad is plugged in at all. The options screen asks so that it can say so, rather
    ///     than offering a control scheme that answers nothing.
    /// </summary>
    public static bool JoyPadConnected
        => GamePad.GetState(PlayerIndex.One).IsConnected;

    public bool MovedUp()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Up) ||
           PushedUp(GamePad.GetState(PlayerIndex.One));

    public bool MovedDown()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Down) ||
           PushedDown(GamePad.GetState(PlayerIndex.One));

    public bool MovedLeft()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Left) ||
           PushedLeft(GamePad.GetState(PlayerIndex.One));

    public bool MovedRight()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Right) ||
           PushedRight(GamePad.GetState(PlayerIndex.One));

    public bool HasFired()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Fire) ||
           GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed;

    /// <summary>
    ///     The d-pad and the left stick both count, so a pad can be held either way round. A pad that is
    ///     not plugged in reports every button released and both sticks centred, so these answer false
    ///     without needing to be asked whether one is there.
    /// </summary>
    private static bool PushedUp(GamePadState pad)
        => pad.DPad.Up == ButtonState.Pressed || pad.ThumbSticks.Left.Y > StickThreshold;

    /// <inheritdoc cref="PushedUp" />
    private static bool PushedDown(GamePadState pad)
        => pad.DPad.Down == ButtonState.Pressed || pad.ThumbSticks.Left.Y < -StickThreshold;

    /// <inheritdoc cref="PushedUp" />
    private static bool PushedLeft(GamePadState pad)
        => pad.DPad.Left == ButtonState.Pressed || pad.ThumbSticks.Left.X < -StickThreshold;

    /// <inheritdoc cref="PushedUp" />
    private static bool PushedRight(GamePadState pad)
        => pad.DPad.Right == ButtonState.Pressed || pad.ThumbSticks.Left.X > StickThreshold;

    private void SetBlobHSpeed(float inputSpeed)
    {
        if (inputSpeed < SpeedBand1)
        {
            _blob.BlobHSpeed = CrawlSpeed;
        }
        else if (inputSpeed < SpeedBand2 && inputSpeed >= SpeedBand1)
        {
            _blob.BlobHSpeed = SlowSpeed;
        }
        else if (inputSpeed < SpeedBand3 && inputSpeed >= SpeedBand2)
        {
        }
        else if (inputSpeed < SpeedBand4 && inputSpeed >= SpeedBand3)
        {
            _blob.BlobHSpeed = BriskSpeed;
        }
        else
        {
            _blob.BlobHSpeed = FullSpeed;
        }
    }

    private void SetBlobVSpeed(float inputSpeed)
    {
        if (inputSpeed < VerticalDeadZone)
        {
            _blob.BlobVSpeed = CrawlSpeed;
        }
        else if (inputSpeed < SpeedBand2 && inputSpeed >= SpeedBand1)
        {
            _blob.BlobVSpeed = SlowSpeed;
        }
        else if (inputSpeed < SpeedBand3 && inputSpeed >= SpeedBand2)
        {
        }
        else if (inputSpeed < SpeedBand4 && inputSpeed >= SpeedBand3)
        {
            _blob.BlobVSpeed = BriskSpeed;
        }
        else
        {
            _blob.BlobVSpeed = FullSpeed;
        }
    }
}
