using System;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace StarquakeXNA
{
    public class PlayerKeySet
    {
        public Microsoft.Xna.Framework.Input.Keys Up;
        public Microsoft.Xna.Framework.Input.Keys Down;
        public Microsoft.Xna.Framework.Input.Keys Left;
        public Microsoft.Xna.Framework.Input.Keys Right;
        public Microsoft.Xna.Framework.Input.Keys Fire;

        public Microsoft.Xna.Framework.Input.Keys Pause;
        public Microsoft.Xna.Framework.Input.Keys Quit;
    }

    public class CustomControl
    {
        bool useJoyPad = true;
        public bool UseJoyPad
        {
            get { return useJoyPad; }
            set { useJoyPad = value; }
        }

        //DirectInput directInputInstance;
        //Joystick joystick;

        GameEngine gameEngine;
        PlayerKeySet playerKeySet = null;

        public PlayerKeySet PlayerKeySet
        {
            get { return playerKeySet; }
            set { playerKeySet = value; }
        }

        public CustomControl(PlayerKeySet PlayerKeys, GameEngine GameEngine, bool UseJoyPad = false)
        {
            gameEngine = GameEngine;
            /*
            if (UseJoyPad)
            {
                directInputInstance = new DirectInput();
                playerKeySet = PlayerKeys;
                InitialiseJoystick();
            }
            */
        }

        void InitialiseJoystick()
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
        {
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
            return Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(playerKeySet.Up);
        }

        public bool MovedDown()
        {
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
            return Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(playerKeySet.Down);
        }

        public bool MovedLeft()
        {
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
            return Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(playerKeySet.Left);
        }

        public bool MovedRight()
        {
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
            return Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(playerKeySet.Right);
        }

        public bool HasFired()
        {
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
            return Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(playerKeySet.Fire);
        }

        private void SetBlobHSpeed(float InputSpeed)
        {
            if (InputSpeed < 0.2)
            {
                gameEngine.Player.BlobHSpeed = 1;
            }
            else if (InputSpeed < 0.4 && InputSpeed >= 0.2)
            {
                gameEngine.Player.BlobHSpeed = 2;
            }
            else if (InputSpeed < 0.6 && InputSpeed >= 0.4)
            {
            }
            else if (InputSpeed < 0.8 && InputSpeed >= 0.6)
            {
                gameEngine.Player.BlobHSpeed = 4;
            }
            else
            {
                gameEngine.Player.BlobHSpeed = 8;
            }
        }

        private void SetBlobVSpeed(float InputSpeed)
        {
            if (InputSpeed < 0.1)
            {
                gameEngine.Player.BlobVSpeed = 1;
            }
            else if (InputSpeed < 0.4 && InputSpeed >= 0.2)
            {
                gameEngine.Player.BlobVSpeed = 2;
            }
            else if (InputSpeed < 0.6 && InputSpeed >= 0.4)
            {
            }
            else if (InputSpeed < 0.8 && InputSpeed >= 0.6)
            {
                gameEngine.Player.BlobVSpeed = 4;
            }
            else
            {
                gameEngine.Player.BlobVSpeed = 8;
            }
        }
    }
}
