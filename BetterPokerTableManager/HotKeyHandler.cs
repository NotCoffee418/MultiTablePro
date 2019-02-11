using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;

namespace BetterPokerTableManager
{
    internal class HotKeyHandler
    {
        [DllImport("user32", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(Point p);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        public const int WM_HOTKEY = 0x0312; // Definition for hotkey MSG
        private static List<Tuple<int, HotKey, IntPtr>> idMemory = new List<Tuple<int, HotKey, IntPtr>>();
        private static int _lastRegisterId = -1;

        private static int LastRegisterId
        {
            get
            {
                // Not sure if starting at 0 is the best idea for hooking into external applications.
                // Starting at a unique value instead (our MainWindowHandle)
                if (_lastRegisterId == -1)
                    _lastRegisterId = Process.GetCurrentProcess().MainWindowHandle.ToInt32();
                return _lastRegisterId;
            }
            set { _lastRegisterId = value; }
        }

        public static void StartListener()
        {
            ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(HotkeyPressed);
        }

        public static void StopListener()
        {
            ComponentDispatcher.ThreadFilterMessage -= HotkeyPressed;
        }

        public static void RegisterHotKey(HotKey hotKey, IntPtr windowHandle)
        {
            int newId = ++LastRegisterId;
            if (!RegisterHotKey(windowHandle, newId, (uint)hotKey.Modifier, (uint)hotKey.Key))
                Logger.Log($"HotKeyHandler: RegisterHotKey: Error {Marshal.GetLastWin32Error()}", Logger.Status.Error);

            // Remember the registered hotkey
            idMemory.Add(new Tuple<int, HotKey, IntPtr>(newId, hotKey, windowHandle));
        }
        
        internal static void HotkeyPressed(ref MSG m, ref bool handled)
        {
            if (m.message != WM_HOTKEY)
                return;
            
            // Find the targeted table, if any
            Table table = FindTableUnderMouse();
            bool wasRelevant = table == null ? false : true;

            // Handle table related hotkeys
            HotKey foundHotkey = new HotKey(m.lParam);
            if (wasRelevant)
            {
                if (foundHotkey.Equals(Config.Active.AsideHotKey))
                    table.IsAside = !table.IsAside;
                // More hotkeys go here
            }
            else // if (!wasRelevant)
            {
                // Hotkey press was irrelevant for us. Redirect it to foreground window
                IntPtr foregroundHandle = GetForegroundWindow();
                if (foregroundHandle != null)
                {
                    /* This just gets picked up as a hotkey again regardless... Ree.
                    StopListener();
                    InputSender.RedirectHotkey(foundHotkey);
                    StartListener();
                    */
                }
            }
        }

        public static void UnregisterHotKey(HotKey hotKey, IntPtr windowHandle)
        {
            int id = 0;
            try
            {
                // Find the ID of the hotkey
                id = idMemory.Where(t => t.Item2.Equals(hotKey) && t.Item3 == windowHandle).First().Item1;
            }
            catch
            {
                Logger.Log("Attempting to unregister a hotkey that was never registered.", Logger.Status.Error);
                return;
            }

            // Unregister the hotkey
            if (UnregisterHotKey(windowHandle, id))
                Logger.Log($"Unregistered hotkey {hotKey} from table {windowHandle}");
            else
                Logger.Log($"HotKeyHandler: UnregisterHotKey: Error {Marshal.GetLastWin32Error()}", Logger.Status.Error);

            // Remove the hotkey from idMemory
            idMemory.RemoveAll(h => h.Item2.Equals(hotKey) && h.Item3 == windowHandle);
        }

        public static void UnregisterAllHotkeys()
        {
            while (idMemory.Count > 0)
                UnregisterHotKey(idMemory[0].Item2, idMemory[0].Item3);
            Logger.Log("All hotkeys unregistered.");
        }

        public static Table FindTableUnderMouse()
        {
            Point p;
            if (GetCursorPos(out p))
            {
                IntPtr hWnd = WindowFromPoint(p);
                if (hWnd != null)
                    return Table.Find(hWnd, registerMissing: false);
            }
            return null;
        }
    }
}
