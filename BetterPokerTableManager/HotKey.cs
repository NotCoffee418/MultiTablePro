using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace BetterPokerTableManager
{    
    internal class HotKey : IEquatable<HotKey>
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, StringBuilder receivingBuffer, int bufferSize, uint flags);

        public HotKey() { } // Empty constructor for json
        public HotKey(Keys key, ModifierKeys modifiers = ModifierKeys.None)
        {
            Key = key;
            Modifier = modifiers;
        }
        public HotKey(IntPtr lParam) // Constructor for lParam
        {
            uint param = (uint)lParam.ToInt64();
            Key = (Keys)((param & 0xffff0000) >> 16);
            Modifier = (ModifierKeys)(param & 0x0000ffff);
        }

        public Keys Key { get; set; }
        public ModifierKeys Modifier { get; set; }

        public bool Equals(HotKey other)
        {
            return Key == other.Key && Modifier == other.Modifier;
        }

        public override string ToString()
        {
            // Set modifier for output
            var keyboardState = new byte[256];
            switch (Modifier) // todo: this doesn't handle multiple eg: ctrl+alt+del
            {
                case ModifierKeys.Alt:
                    keyboardState[(int)Keys.Alt] = 0xff;
                    break;
                case ModifierKeys.Control:
                    keyboardState[(int)Keys.ControlKey] = 0xff;
                    break;
                case ModifierKeys.Shift:
                    keyboardState[(int)Keys.ShiftKey] = 0xff;
                    break;
                case ModifierKeys.Windows:
                    keyboardState[(int)Keys.LWin] = 0xff;
                    break;
            }

            // Get string from the hotkey
            var buf = new StringBuilder(256);
            ToUnicode((uint)Key, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }
    }

    internal class HotKeyEventArgs : EventArgs
    {
        public HotKeyEventArgs(HotKey hotkey, IntPtr windowHandle)
        {
            _hotKey = hotkey;
            _windowHandle = windowHandle;
        }

        private readonly HotKey _hotKey;
        private readonly IntPtr _windowHandle;

        public HotKey Hotkey
        {
            get { return _hotKey; }
        }
        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
        }
    }
}
