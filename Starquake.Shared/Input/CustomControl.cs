// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.Engines;

namespace StarquakeXNA.Input;

public class PlayerKeySet
{
    public Keys Down;
    public Keys Fire;
    public Keys Left;

    public Keys Pause;
    public Keys Quit;
    public Keys Right;
    public Keys Up;
}

public class CustomControl
{

    //DirectInput directInputInstance;
    //Joystick joystick;

    private readonly GameEngine _gameEngine;

    public CustomControl(PlayerKeySet playerKeys, GameEngine gameEngine, bool useJoyPad = false)
    {
        _gameEngine = gameEngine;
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
                    joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-100, 100);
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
        if (inputSpeed < 0.2)
        {
            _gameEngine.Player.BlobHSpeed = 1;
        }
        else if (inputSpeed < 0.4 && inputSpeed >= 0.2)
        {
            _gameEngine.Player.BlobHSpeed = 2;
        }
        else if (inputSpeed < 0.6 && inputSpeed >= 0.4)
        {
        }
        else if (inputSpeed < 0.8 && inputSpeed >= 0.6)
        {
            _gameEngine.Player.BlobHSpeed = 4;
        }
        else
        {
            _gameEngine.Player.BlobHSpeed = 8;
        }
    }

    private void SetBlobVSpeed(float inputSpeed)
    {
        if (inputSpeed < 0.1)
        {
            _gameEngine.Player.BlobVSpeed = 1;
        }
        else if (inputSpeed < 0.4 && inputSpeed >= 0.2)
        {
            _gameEngine.Player.BlobVSpeed = 2;
        }
        else if (inputSpeed < 0.6 && inputSpeed >= 0.4)
        {
        }
        else if (inputSpeed < 0.8 && inputSpeed >= 0.6)
        {
            _gameEngine.Player.BlobVSpeed = 4;
        }
        else
        {
            _gameEngine.Player.BlobVSpeed = 8;
        }
    }
}