using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Input;
using TOURMALINE.Common.Input;
using Game = Tourmaline.Viewer3D.Processes.Game;

namespace Tourmaline.Viewer3D
{
    public static class UserInput
    {
        public static bool ComposingMessage;
        static KeyboardState KeyboardState;
        static MouseState MouseState;
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;
        static bool MouseButtonsSwapped;
        public static int MouseSpeedX;
        public static int MouseSpeedY;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys key);

        public static void Update(Game game)
        {
            LastKeyboardState = KeyboardState;
            LastMouseState = MouseState;
            // Make sure we have an "idle" (everything released) keyboard and mouse state if the window isn't active.
            KeyboardState = game.IsActive ? new KeyboardState(GetKeysWithPrintScreenFix(Keyboard.GetState())) : new KeyboardState();
            MouseState = game.IsActive ? Mouse.GetState() : new MouseState(0, 0, LastMouseState.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            MouseButtonsSwapped = System.Windows.Forms.SystemInformation.MouseButtonsSwapped;

            MouseSpeedX = Math.Abs(MouseState.X - LastMouseState.X);
            MouseSpeedY = Math.Abs(MouseState.Y - LastMouseState.Y);

#if DEBUG_RAW_INPUT
            for (Keys key = 0; key <= Keys.OemClear; key++)
                if (LastKeyboardState[key] != KeyboardState[key])
                    Console.WriteLine("Keyboard {0} changed to {1}", key, KeyboardState[key]);
            if (LastMouseState.LeftButton != MouseState.LeftButton)
                Console.WriteLine("Mouse left button changed to {0}", MouseState.LeftButton);
            if (LastMouseState.MiddleButton != MouseState.MiddleButton)
                Console.WriteLine("Mouse middle button changed to {0}", MouseState.MiddleButton);
            if (LastMouseState.RightButton != MouseState.RightButton)
                Console.WriteLine("Mouse right button changed to {0}", MouseState.RightButton);
            if (LastMouseState.XButton1 != MouseState.XButton1)
                Console.WriteLine("Mouse X1 button changed to {0}", MouseState.XButton1);
            if (LastMouseState.XButton2 != MouseState.XButton2)
                Console.WriteLine("Mouse X2 button changed to {0}", MouseState.XButton2);
            if (LastMouseState.ScrollWheelValue != MouseState.ScrollWheelValue)
                Console.WriteLine("Mouse scrollwheel changed by {0}", MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue);
#endif
#if DEBUG_INPUT
            var newKeys = GetPressedKeys();
            var oldKeys = GetPreviousPressedKeys();
            foreach (var newKey in newKeys)
                if (!oldKeys.Contains(newKey))
                    Console.WriteLine("Keyboard {0} pressed", newKey);
            foreach (var oldKey in oldKeys)
                if (!newKeys.Contains(oldKey))
                    Console.WriteLine("Keyboard {0} released", oldKey);
            if (IsMouseLeftButtonPressed)
                Console.WriteLine("Mouse left button pressed");
            if (IsMouseLeftButtonReleased)
                Console.WriteLine("Mouse left button released");
            if (IsMouseMiddleButtonPressed)
                Console.WriteLine("Mouse middle button pressed");
            if (IsMouseMiddleButtonReleased)
                Console.WriteLine("Mouse middle button released");
            if (IsMouseRightButtonPressed)
                Console.WriteLine("Mouse right button pressed");
            if (IsMouseRightButtonReleased)
                Console.WriteLine("Mouse right button released");
            if (IsMouseWheelChanged)
                Console.WriteLine("Mouse scrollwheel changed by {0}", MouseWheelChange);
#endif
#if DEBUG_USER_INPUT
            foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
            {
                if (UserInput.IsPressed(command))
                    Console.WriteLine("Pressed  {0} - {1}", command, InputSettings.Commands[(int)command]);
                if (UserInput.IsReleased(command))
                    Console.WriteLine("Released {0} - {1}", command, InputSettings.Commands[(int)command]);
            }
#endif
        }

        static Keys[] GetKeysWithPrintScreenFix(KeyboardState keyboardState)
        {
            // When running in fullscreen, Win32's GetKeyboardState (the API behind Keyboard.GetState()) never returns
            // the print screen key as being down. Something is eating it or something. So here we simply query that
            // key directly and forcibly add it to the list of pressed keys.
            var keys = new List<Keys>(keyboardState.GetPressedKeys());
            if ((GetAsyncKeyState(Keys.PrintScreen) & 0x8000) != 0)
                keys.Add(Keys.PrintScreen);
            return keys.ToArray();
        }

        public static void Handled()
        {
        }
        internal static bool commandKeyDown(UserCommand command, KeyboardState state)
        {
            //Devuelve True si la tecla (o teclas) pulsada/s se corresponden con este comando
            switch (command)
            {
                case UserCommand.CameraReset:
                    if (state.IsKeyDown(Keys.Escape)) return true; break;                    
                case UserCommand.CameraMoveFast:
                    if(state.IsKeyDown(Keys.LeftShift)) return true; break;
                case UserCommand.CameraMoveSlow:
                    if(state.IsKeyDown(Keys.RightShift))return true; break;
                case UserCommand.CameraPanLeft:
                    if (state.IsKeyDown(Keys.O)) return true;break;
                case UserCommand.CameraPanRight:
                    if (state.IsKeyDown(Keys.P)) return true; break;
                case UserCommand.CameraPanUp:
                    if (state.IsKeyDown(Keys.Q)) return true; break;
                case UserCommand.CameraPanDown:
                    if (state.IsKeyDown(Keys.A)) return true; break;
                case UserCommand.CameraZoomIn:
                    if (state.IsKeyDown(Keys.PageUp)) return true; break;
                case UserCommand.CameraZoomOut:
                    if (state.IsKeyDown(Keys.PageDown)) return true; break;
                case UserCommand.CameraRotateLeft:
                    if (state.IsKeyDown(Keys.Left)) return true; break;
                case UserCommand.CameraRotateRight:
                    if (state.IsKeyDown(Keys.Right)) return true; break;
                case UserCommand.CameraRotateUp:
                    if (state.IsKeyDown(Keys.Up)) return true; break;
                case UserCommand.CameraRotateDown:
                    if (state.IsKeyDown(Keys.Down)) return true; break;
            }
            return false;
        }
        internal static bool commandKeyUp(UserCommand command, KeyboardState state)
        {
            //Devuelve True si la tecla (o teclas) pulsada/s se corresponden con este comando
            switch (command)
            {
                case UserCommand.CameraReset:
                    if (state.IsKeyUp(Keys.Escape)) return true; break;
                case UserCommand.CameraMoveFast:
                    if (state.IsKeyUp(Keys.LeftShift)) return true; break;
                case UserCommand.CameraMoveSlow:
                    if (state.IsKeyUp(Keys.RightShift)) return true; break;
                case UserCommand.CameraPanLeft:
                    if (state.IsKeyUp(Keys.O)) return true; break;
                case UserCommand.CameraPanRight:
                    if (state.IsKeyUp(Keys.P)) return true; break;
                case UserCommand.CameraPanUp:
                    if (state.IsKeyUp(Keys.Q)) return true; break;
                case UserCommand.CameraPanDown:
                    if (state.IsKeyUp(Keys.A)) return true; break;
                case UserCommand.CameraZoomIn:
                    if (state.IsKeyUp(Keys.PageUp)) return true; break;
                case UserCommand.CameraZoomOut:
                    if (state.IsKeyUp(Keys.PageDown)) return true; break;
                case UserCommand.CameraRotateLeft:
                    if (state.IsKeyUp(Keys.Left)) return true; break;
                case UserCommand.CameraRotateRight:
                    if (state.IsKeyUp(Keys.Right)) return true; break;
                case UserCommand.CameraRotateUp:
                    if (state.IsKeyUp(Keys.Up)) return true; break;
                case UserCommand.CameraRotateDown:
                    if (state.IsKeyUp(Keys.Down)) return true; break;
            }
            return false;
        }

        public static bool IsPressed(UserCommand command)
        {
            if (ComposingMessage) return false;
            return commandKeyDown(command, KeyboardState) && !commandKeyUp(command, LastKeyboardState);
        }

        public static bool IsReleased(UserCommand command)
        {
            if (ComposingMessage) return false;
            return commandKeyUp(command, KeyboardState) && !commandKeyDown(command, LastKeyboardState);
        }

        public static bool IsDown(UserCommand command)
        {
            if (ComposingMessage) return false;
            return commandKeyDown(command, KeyboardState);
        }

        public static Keys[] GetPressedKeys() { return KeyboardState.GetPressedKeys(); }
        public static Keys[] GetPreviousPressedKeys() { return LastKeyboardState.GetPressedKeys(); }

        public static bool IsMouseMoved { get { return MouseState.X != LastMouseState.X || MouseState.Y != LastMouseState.Y; } }
        public static int MouseMoveX { get { return MouseState.X - LastMouseState.X; } }
        public static int MouseMoveY { get { return MouseState.Y - LastMouseState.Y; } }
        public static bool MouseMovedUp { get { return MouseState.Y < LastMouseState.Y; } }
        public static bool MouseMovedDown { get { return MouseState.Y > LastMouseState.Y; } }
        public static bool MouseMovedLeft { get { return MouseState.X < LastMouseState.X; } }
        public static bool MouseMovedRight { get { return MouseState.X > LastMouseState.X; } }
        public static int MouseX { get { return MouseState.X; } }
        public static int MouseY { get { return MouseState.Y; } }

        public static bool IsMouseWheelChanged { get { return MouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue; } }
        public static int MouseWheelChange { get { return MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue; } }

        public static bool IsMouseLeftButtonDown { get { return MouseButtonsSwapped ? MouseState.RightButton == ButtonState.Pressed : MouseState.LeftButton == ButtonState.Pressed; } }
        public static bool IsMouseLeftButtonPressed { get { return MouseButtonsSwapped ? MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released : MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released; } }
        public static bool IsMouseLeftButtonReleased { get { return MouseButtonsSwapped ? MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed : MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed; } }

        public static bool IsMouseMiddleButtonDown { get { return MouseState.MiddleButton == ButtonState.Pressed; } }
        public static bool IsMouseMiddleButtonPressed { get { return MouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released; } }
        public static bool IsMouseMiddleButtonReleased { get { return MouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed; } }

        public static bool IsMouseRightButtonDown { get { return MouseButtonsSwapped ? MouseState.LeftButton == ButtonState.Pressed : MouseState.RightButton == ButtonState.Pressed; } }
        public static bool IsMouseRightButtonPressed { get { return MouseButtonsSwapped ? MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released : MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released; } }
        public static bool IsMouseRightButtonReleased { get { return MouseButtonsSwapped ? MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed : MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed; } }
    }
}
