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
using MultiTablePro.Data;

namespace MultiTablePro
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
        private static Dictionary<int, HotKey> idMemory = new Dictionary<int, HotKey>();
        private static int _lastRegisterId = 0;
        private static bool _handlingKeyPress = false;

        private static int LastRegisterId
        {
            get { return _lastRegisterId; }
            set { _lastRegisterId = value; }
        }
        private static IntPtr OurWindowHandle { get; set; }

        internal static void HotkeyPressed(ref MSG m, ref bool handled)
        {
            // Only accept hotkey messages
            if (m.message != WM_HOTKEY || _handlingKeyPress)
                return;

            // Indicate that we're currently processing a HotKeyPressed event & lock it
            // Hopefully prevents unknown flooding issue #53
            _handlingKeyPress = true;

            // Find the targeted table, if any
            Table table = FindTableUnderMouse();
            bool wasRelevant = table == null || table.IsVirtual ? false : true;

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
                    // Eats some CPU but at least it redirects
                    UnregisterHotKey(foundHotkey);
                    InputSender.RedirectHotkey(foundHotkey);
                    RegisterHotKey(foundHotkey, OurWindowHandle);
                }
            }

            // Unlock the event handler
            _handlingKeyPress = false;
        }

        public static void RegisterHotKey(HotKey hotKey, IntPtr windowHandle)
        {
            // Store the receiving window handle
            if (OurWindowHandle == null)
                OurWindowHandle = windowHandle;

            // Don't register already registered hotkeys
            if (idMemory.Where(e => e.Value.Equals(hotKey)).Count() > 0)
                return;

            // Grab a new ID
            int newId = ++LastRegisterId;

            // Attempt to register the hotkey
            if (!RegisterHotKey(OurWindowHandle, newId, (uint)hotKey.Modifier, (uint)hotKey.Key))
                Logger.Log($"HotKeyHandler: RegisterHotKey: Error {Marshal.GetLastWin32Error()}", Logger.Status.Error);
            else
            {
                Logger.Log($"HotKeyHandler: RegisterHotKey: Registering Hotkey ({hotKey})");
                idMemory.Add(newId, hotKey); // Remember the registered hotkey
            }
        }

        public static void UnregisterHotKey(HotKey hotKey)
        {
            int id = 0;
            try
            {
                // Find the ID of the hotkey
                id = idMemory.Where(e => e.Value.Equals(hotKey)).First().Key;
            }
            catch
            {
                Logger.Log("Attempting to unregister a hotkey that was never registered.", Logger.Status.Error);
                return;
            }

            // Unregister the hotkey
            if (UnregisterHotKey(OurWindowHandle, id))
                Logger.Log($"Unregistered hotkey ({hotKey}) from table {OurWindowHandle}");
            else
                Logger.Log($"HotKeyHandler: UnregisterHotKey: Error {Marshal.GetLastWin32Error()}", Logger.Status.Error);

            // Remove the hotkey from idMemory
            idMemory.Remove(id);
        }

        public static void UnregisterAllHotkeys()
        {
            while (idMemory.Count > 0)
                UnregisterHotKey(idMemory.First().Value);
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
