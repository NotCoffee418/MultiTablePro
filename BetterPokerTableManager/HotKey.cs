using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterPokerTableManager
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000 // ???
    }
    
    internal class HotKey : IEquatable<HotKey>
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, StringBuilder receivingBuffer, int bufferSize, uint flags);

        public HotKey() { } // Empty constructor for json
        public HotKey(Keys key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Key = key;
            Modifiers = modifiers;
        }
        public HotKey(IntPtr hotKeyParam)
        {
            uint param = (uint)hotKeyParam.ToInt64();
            Key = (Keys)((param & 0xffff0000) >> 16);
            Modifiers = (KeyModifiers)(param & 0x0000ffff);
        }

        public Keys Key { get; set; }
        public KeyModifiers Modifiers { get; set; }

        public bool Equals(HotKey other)
        {
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override string ToString()
        {
            // Set modifier for output
            var keyboardState = new byte[256];
            switch (Modifiers) // todo: this doesn't handle multiple eg: ctrl+alt+del
            {
                case KeyModifiers.Alt:
                    keyboardState[(int)Keys.Alt] = 0xff;
                    break;
                case KeyModifiers.Control:
                    keyboardState[(int)Keys.ControlKey] = 0xff;
                    break;
                case KeyModifiers.Shift:
                    keyboardState[(int)Keys.ShiftKey] = 0xff;
                    break;
                case KeyModifiers.Windows:
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
