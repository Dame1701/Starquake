// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.Engines;

namespace Starquake.Input;

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

    private readonly GameEngine _gameEngine;

    public CustomControl(PlayerKeySet playerKeys, GameEngine gameEngine, bool useJoyPad = false)
    {
        _gameEngine = gameEngine;
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
        /*
        if (useJoyPad && joystick !=null)
        {
            if (joystick.GetCurrentState().Y < 0)
            {
                return true;
            }

            return false;
        }
        */
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Up);

    public bool MovedDown()
        /*
        if (useJoyPad && joystick != null)
        {
            if (joystick.GetCurrentState().Y > 0)
            {
                return true;
            }

            return false;
        }
        */
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Down);

    public bool MovedLeft()
        /*
        if (useJoyPad && joystick != null)
        {
            if (joystick.GetCurrentState().X < 0)
            {
                return true;
            }

            return false;
        }
        */
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Left);

    public bool MovedRight()
        /*
        if (useJoyPad && joystick != null)
        {
            if (joystick.GetCurrentState().X > 0)
            {
                return true;
            }

            return false;
        }
        */
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Right);

    public bool HasFired()
        /*
        if (useJoyPad && joystick != null)
        {
            if (joystick.GetCurrentState().IsPressed(0) ||
                joystick.GetCurrentState().IsPressed(1))
            {
                return true;
            }

            return false;
        }
        */
        => Keyboard.GetState().IsKeyDown(PlayerKeySet.Fire);

    private void SetBlobHSpeed(float inputSpeed)
    {
        if (inputSpeed < SpeedBand1)
        {
            _gameEngine.Player.BlobHSpeed = CrawlSpeed;
        }
        else if (inputSpeed < SpeedBand2 && inputSpeed >= SpeedBand1)
        {
            _gameEngine.Player.BlobHSpeed = SlowSpeed;
        }
        else if (inputSpeed < SpeedBand3 && inputSpeed >= SpeedBand2)
        {
        }
        else if (inputSpeed < SpeedBand4 && inputSpeed >= SpeedBand3)
        {
            _gameEngine.Player.BlobHSpeed = BriskSpeed;
        }
        else
        {
            _gameEngine.Player.BlobHSpeed = FullSpeed;
        }
    }

    private void SetBlobVSpeed(float inputSpeed)
    {
        if (inputSpeed < VerticalDeadZone)
        {
            _gameEngine.Player.BlobVSpeed = CrawlSpeed;
        }
        else if (inputSpeed < SpeedBand2 && inputSpeed >= SpeedBand1)
        {
            _gameEngine.Player.BlobVSpeed = SlowSpeed;
        }
        else if (inputSpeed < SpeedBand3 && inputSpeed >= SpeedBand2)
        {
        }
        else if (inputSpeed < SpeedBand4 && inputSpeed >= SpeedBand3)
        {
            _gameEngine.Player.BlobVSpeed = BriskSpeed;
        }
        else
        {
            _gameEngine.Player.BlobVSpeed = FullSpeed;
        }
    }
}