using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace BetterPokerTableManager
{    
    internal class HotKey : IEquatable<HotKey>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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

        Keys _key;
        ModifierKeys _modifier;

        public Keys Key
        {
            get { return _key; }
            set {
                _key = value;
                RaisePropertyChanged("Keys");
            }
        }
        public ModifierKeys Modifier
        {
            get { return _modifier; }
            set
            {
                _modifier = value;
                RaisePropertyChanged("Modifier");
            }
        }

        public bool Equals(HotKey other)
        {
            return Key == other.Key && Modifier == other.Modifier;
        }

        public override string ToString()
        {
            // Set modifier for output
            string modifierText = "";
            switch (Modifier)
            {
                case ModifierKeys.Alt:
                    modifierText = "Alt + ";
                    break;
                case ModifierKeys.Control:
                    modifierText = "Ctrl + ";
                    break;
                case ModifierKeys.Shift:
                    modifierText = "Shift + ";
                    break;
                case ModifierKeys.Windows:
                    modifierText = "Win + ";
                    break;
            }

            // Get string from the hotkey
            return modifierText + Key.ToString().ToUpper();
        }

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
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
