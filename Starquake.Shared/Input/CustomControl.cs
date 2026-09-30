// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;

namespace Starquake.Input;

/// <summary>
///     What the player is pressing. Every method here reads the keyboard.
/// </summary>
/// <remarks>
///     There was a joypad once. Each of the five input methods below carried its own commented-out
///     copy of the same block - read the stick, compare against zero, return - and the five said
///     nothing the one sentence above them does not. The speed bands and dead zone still named in
///     this class are what an analogue stick would feed, and are the only trace of it left.
/// </remarks>
public class CustomControl
{
    /// <summary>
    ///     The range a joystick axis is asked to report over, so that a centred stick reads zero and
    ///     the two extremes are symmetrical.
    /// </summary>
    private const int JoystickAxisRange = 100;

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


    //DirectInput directInputInstance;
    //Joystick joystick;

    private readonly Blob _blob;

    public CustomControl(PlayerKeySet playerKeys, Blob blob, bool useJoyPad = false)
    {
        _blob = blob;
        PlayerKeySet = playerKeys;
        /*
        if (UseJoyPad)
        {
            directInputInstance = new DirectInput();
            playerKeySet = PlayerKeys;
            InitialiseJoystick();
        }
        */
    }

    public bool UseJoyPad { get; set; } = true;

    public PlayerKeySet PlayerKeySet { get; set; }

    private void InitialiseJoystick()
    {
        /*
        // Search for device
        foreach (DeviceInstance device in directInputInstance.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
        {
            // Create device
            try
            {
                joystick = new Joystick(directInputInstance, device.InstanceGuid);
                break;
            }
            catch (DirectInputException)
            {
            }
        }

        if (joystick == null)
        {
            // do error for no joystick here

        }
        else
        {
            // get joystick device objects
            foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
            {
                if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                {
                    joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-JoystickAxisRange, JoystickAxisRange);
                }
            }

            // Acquire device
            joystick.Acquire();
        }
        */
    }

    public bool MovedUp()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Up);

    public bool MovedDown()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Down);

    public bool MovedLeft()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Left);

    public bool MovedRight()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Right);

    public bool HasFired()
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Fire);

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