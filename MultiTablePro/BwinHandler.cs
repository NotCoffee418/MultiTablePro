using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTablePro
{
    class BwinHandler
    {
        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);


        private static bool? IsRunning { get; set; }


        public static void Start()
        {
            if (IsRunning == null) // First call to start
            {
                // Regularly looks for new log files & starts watching any found 
                IsRunning = true;
                Timer timer = new Timer(FindNewTables, null, 0, 1000);
            }
            else if (IsRunning == false) // Restart
            {
                // Restarts watch on all known log files
                IsRunning = true;
            }
        }

        public static void Stop()
        {
            // Loop theads will stop, effectively ending the watch
            IsRunning = false;
        }
        
        private static void FindNewTables(object state)
        {
            // Kill if bwin is not running
            Process[] p = Process.GetProcessesByName("bwinbe"); // todo: broaden this with PP and bwin wildcard searches
            if (p.Count() == 0)
            {
                Logger.Log("Bwin client not running or closed");
                return;
            }

            // Find all poker tables (bwin table class is #32770 - window title contains $ (other applications also use this class name)
            var tableHandles = EnumAllWindows(IntPtr.Zero, "#32770").Where(hWnd => GetWindowTitle(hWnd).Contains("$"));
            lock (Table.KnownTables)
            {
                // Register any new tables & start watching them
                Logger.Log(tableHandles.Count().ToString());
                foreach (var handle in tableHandles.Where(h => Table.Find(h, false) == null))
                {
                    // Verify that it is indeed a table window (tourney lobbeys will also be found
                    // Check for random element that only exists in tables
                    if (EnumAllWindows(handle, "Static").Where(hWnd => GetWindowTitle(hWnd).Contains("Fold to any bet")).Count() > 0)
                    {
                        // Register the table
                        string winTitle = GetWindowTitle(handle);
                        Logger.Log($"Registering new bwin table: {handle} - " + winTitle);
                        Table table = new Table(handle);
                        //new Thread(() => WatchTable(table)).Start();
                    }
                }
            }            
        }
        
        private static void WatchTable(Table table)
        {
            throw new NotImplementedException();
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            list.Add(handle);
            return true;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        public static string GetWinClass(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;
            StringBuilder classname = new StringBuilder(100);
            IntPtr result = GetClassName(hwnd, classname, classname.Capacity);
            if (result != IntPtr.Zero)
                return classname.ToString();
            return null;
        }

        public static IEnumerable<IntPtr> EnumAllWindows(IntPtr hwnd, string childClassName)
        {
            List<IntPtr> children = GetChildWindows(hwnd);
            if (children == null)
                yield break;
            foreach (IntPtr child in children)
            {
                if (GetWinClass(child) == childClassName)
                    yield return child;
                foreach (var childchild in EnumAllWindows(child, childClassName))
                    yield return childchild;
            }
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            int textLength = GetWindowTextLength(hWnd);
            StringBuilder outText = new StringBuilder(textLength + 1);
            int a = GetWindowText(hWnd, outText, outText.Capacity);
            return outText.ToString();
        }
    }
}
